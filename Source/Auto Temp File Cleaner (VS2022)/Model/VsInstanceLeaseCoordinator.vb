Option Strict On
Option Explicit On
Option Infer Off

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Threading

''' <summary>
''' Provides cross-process coordination to detect when the last running instance
''' of Visual Studio exits.
''' </summary>
''' 
''' <remarks>
''' This coordinator uses two mechanisms:
''' <list type="bullet">
''' <item>A named <see cref="Mutex"/> to synchronize access between processes.</item>
''' <item>A shared instance file that stores identifiers for all active Visual Studio processes.</item>
''' </list>
''' 
''' Each Visual Studio instance registers a unique lease when starting and unregisters it when closing.
''' 
''' The lease file is kept open while an instance is active using <see cref="FileShare.ReadWrite"/>.
''' This allows multiple instances to coexist while preventing the file from being manually deleted
''' or renamed as long as at least one instance still holds an open handle.
''' 
''' The lease identifier includes both:
''' <list type="bullet">
''' <item>Process ID</item>
''' <item>Process start time</item>
''' </list>
''' 
''' This prevents PID reuse from causing incorrect lease validation.
''' 
''' Stale leases are automatically purged when detected.
''' </remarks>
Friend NotInheritable Class VsInstanceLeaseCoordinator

#Region " Fields "

    ''' <summary>
    ''' Name of the system-wide mutex used to synchronize lease updates.
    ''' </summary>
    Private Shared ReadOnly LeaseMutexName As String =
        "ElektroStudios.AutoTempFileCleaner.InstanceMutex"

    ''' <summary>
    ''' Directory that stores lease tracking data.
    ''' </summary>
    Private Shared ReadOnly LeaseFolderPath As String =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ElektroStudios",
            "AutoTempFileCleaner")

    ''' <summary>
    ''' File containing all active instance identifiers.
    ''' </summary>
    Private Shared ReadOnly LeaseFilePath As String =
        Path.Combine(LeaseFolderPath, "Visual Studio Process Instances.tmp")

    ''' <summary>
    ''' Unique identifier representing the current process instance.
    ''' </summary>
    Private ReadOnly leaseKey As String

    ''' <summary>
    ''' Keeps the instance file open while Visual Studio is running.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' The file is opened with <see cref="FileShare.ReadWrite"/>, which allows multiple
    ''' Visual Studio instances to keep their own handles open simultaneously while still
    ''' preventing external deletion or renaming of the file.
    ''' </remarks>
    Private leaseFileHandle As FileStream

    ''' <summary>
    ''' Synchronizes access to the local file handle inside the current process.
    ''' </summary>
    Private ReadOnly handleLock As New Object()

#End Region

#Region " Constructors "

    ''' <summary>
    ''' Initializes a new instance of the <see cref="VsInstanceLeaseCoordinator"/> class.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' The lease key is generated using:
    ''' <list type="bullet">
    ''' <item>Process ID</item>
    ''' <item>Process start time (UTC ticks)</item>
    ''' </list>
    ''' 
    ''' This ensures uniqueness even if the operating system later reuses the same PID.
    ''' </remarks>
    Public Sub New()
        Dim currentProcess As Process = Process.GetCurrentProcess()
        Dim processStartTicks As Long = currentProcess.StartTime.ToUniversalTime().Ticks

        Me.leaseKey = $"{currentProcess.Id};{processStartTicks}"
    End Sub

#End Region

#Region " Public Methods "

    ''' <summary>
    ''' Registers the current process as an active instance.
    ''' </summary>
    ''' 
    ''' <returns>
    ''' <see langword="True"/> if, after registration, this process is the only active instance;
    ''' otherwise, <see langword="False"/>.
    ''' </returns>
    ''' 
    ''' <remarks>
    ''' This method should typically be called during Visual Studio package initialization.
    ''' </remarks>
    Public Function RegisterLease() As Boolean

        Dim mutex As New Mutex(initiallyOwned:=False, name:=LeaseMutexName)
        Dim hasLock As Boolean = False

        Try
            Try
                hasLock = mutex.WaitOne(TimeSpan.FromSeconds(10))
            Catch ex As AbandonedMutexException
                hasLock = True
            End Try

            If Not hasLock Then
                Return False
            End If

            SyncLock Me.handleLock
                Me.EnsureLeaseFileHandleOpen()
            End SyncLock

            Dim leases As HashSet(Of String) = Me.LoadLeases()
            Me.PurgeDeadLeases(leases)

            leases.Add(Me.leaseKey)

            Me.SaveLeases(leases)

            Return leases.Count = 1

        Catch
            Me.ReleaseLeaseFileHandle()
            Return False

        Finally
            If hasLock Then
                mutex.ReleaseMutex()
            End If

            mutex.Dispose()
        End Try

    End Function

    ''' <summary>
    ''' Unregisters the current process instance and determines whether this was the final active instance.
    ''' </summary>
    ''' 
    ''' <returns>
    ''' <see langword="True"/> if no remaining instances exist, indicating that this was the last running instance;
    ''' otherwise, <see langword="False"/>.
    ''' </returns>
    ''' 
    ''' <remarks>
    ''' This method should be called during shutdown, for example inside <c>QueryClose</c>.
    ''' </remarks>
    Public Function UnregisterLeaseAndCheckIfLast() As Boolean

        Dim mutex As New Mutex(initiallyOwned:=False, name:=LeaseMutexName)
        Dim hasLock As Boolean = False

        Try
            Try
                hasLock = mutex.WaitOne(TimeSpan.FromSeconds(10))
            Catch ex As AbandonedMutexException
                hasLock = True
            End Try

            If Not hasLock Then
                Return False
            End If

            Dim leases As HashSet(Of String) = Me.LoadLeases()
            Me.PurgeDeadLeases(leases)

            leases.Remove(Me.leaseKey)

            Me.SaveLeases(leases)

            Dim isLastInstance As Boolean = (leases.Count = 0)

            Return isLastInstance

        Catch
            Return False

        Finally
            Me.ReleaseLeaseFileHandle()

            If hasLock Then
                mutex.ReleaseMutex()
            End If

            mutex.Dispose()
        End Try

    End Function

#End Region

#Region " Private Methods "

    ''' <summary>
    ''' Ensures that the instance file is open and locked by the current process.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' The file is opened with <see cref="FileShare.ReadWrite"/> so multiple Visual Studio processes
    ''' can hold their own handles at the same time. Since delete sharing is not granted, the file cannot
    ''' be manually deleted while any process keeps its handle open.
    ''' </remarks>
    Private Sub EnsureLeaseFileHandleOpen()

        If Me.leaseFileHandle IsNot Nothing Then
            Return
        End If

        Directory.CreateDirectory(LeaseFolderPath)

        Me.leaseFileHandle =
            New FileStream(
                LeaseFilePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.ReadWrite)

    End Sub

    ''' <summary>
    ''' Releases the current file handle, if any.
    ''' </summary>
    Private Sub ReleaseLeaseFileHandle()

        SyncLock Me.handleLock
            If Me.leaseFileHandle IsNot Nothing Then
                Me.leaseFileHandle.Dispose()
                Me.leaseFileHandle = Nothing
            End If
        End SyncLock

    End Sub

    ''' <summary>
    ''' Loads all existing instance identifiers from disk.
    ''' </summary>
    ''' 
    ''' <returns>
    ''' A <see cref="HashSet(Of String)"/> containing all known instance identifiers.
    ''' </returns>
    Private Function LoadLeases() As HashSet(Of String)

        Dim leases As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        If Not File.Exists(LeaseFilePath) Then
            Return leases
        End If

        Using fs As New FileStream(LeaseFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Using reader As New StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks:=True)

                While Not reader.EndOfStream
                    Dim line As String = reader.ReadLine()

                    If line Is Nothing Then
                        Continue While
                    End If

                    Dim trimmed As String = line.Trim()

                    If trimmed.Length = 0 Then
                        Continue While
                    End If

                    leases.Add(trimmed)
                End While

            End Using
        End Using

        Return leases

    End Function

    ''' <summary>
    ''' Saves the specified instance identifiers to disk atomically within the current open handle.
    ''' </summary>
    ''' 
    ''' <param name="leases">
    ''' The instance identifiers to persist.
    ''' </param>
    Private Sub SaveLeases(leases As IEnumerable(Of String))

        SyncLock Me.handleLock

            If Me.leaseFileHandle Is Nothing Then
                Me.EnsureLeaseFileHandleOpen()
            End If

            Me.leaseFileHandle.Seek(0, SeekOrigin.Begin)
            Me.leaseFileHandle.SetLength(0)

            Using writer As New StreamWriter(Me.leaseFileHandle, Encoding.UTF8, bufferSize:=1024, leaveOpen:=True)
                For Each lease As String In leases.OrderBy(Function(value) value)
                    writer.WriteLine(lease)
                Next
                writer.Flush()
            End Using

            Me.leaseFileHandle.Flush()

        End SyncLock

    End Sub

    ''' <summary>
    ''' Removes leases associated with processes that are no longer running.
    ''' </summary>
    ''' 
    ''' <param name="leases">
    ''' The lease collection to validate and clean.
    ''' </param>
    ''' 
    ''' <remarks>
    ''' A lease is considered invalid if:
    ''' <list type="bullet">
    ''' <item>The process no longer exists.</item>
    ''' <item>The PID was reused.</item>
    ''' <item>The stored format is invalid.</item>
    ''' </list>
    ''' </remarks>
    Private Sub PurgeDeadLeases(leases As HashSet(Of String))

        Dim expiredLeases As New List(Of String)()

        For Each lease As String In leases

            Dim parts As String() = lease.Split(";"c)

            If parts.Length <> 2 Then
                expiredLeases.Add(lease)
                Continue For
            End If

            Dim processId As Integer
            Dim startTicks As Long

            If Not Integer.TryParse(parts(0), processId) Then
                expiredLeases.Add(lease)
                Continue For
            End If

            If Not Long.TryParse(parts(1), startTicks) Then
                expiredLeases.Add(lease)
                Continue For
            End If

            Try
                Dim process As Process = Process.GetProcessById(processId)

                If process.StartTime.ToUniversalTime().Ticks <> startTicks Then
                    expiredLeases.Add(lease)
                End If
            Catch
                expiredLeases.Add(lease)
            End Try

        Next

        For Each lease As String In expiredLeases
            leases.Remove(lease)
        Next

    End Sub

#End Region

End Class
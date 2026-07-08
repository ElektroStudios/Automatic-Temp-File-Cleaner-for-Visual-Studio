' ***********************************************************************
' Author   : ElektroStudios
' Modified : 19-April-2026
' ***********************************************************************

#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Threading

Imports Task = System.Threading.Tasks.Task

#End Region

#Region " AutoTempFileCleanerVS2022 "

''' <summary>
''' This is the class that implements the package exposed by this assembly.
''' </summary>
''' 
''' <remarks>
''' The minimum requirement for a class to be considered a valid package for Visual Studio
''' Is to implement the <see cref="IVsPackage"/> interface And register itself with the shell.
''' <para></para>
''' This package uses the helper classes defined inside the Managed Package Framework (MPF)
''' to do it: it derives from the Package Class that provides the implementation Of the 
''' <see cref="IVsPackage"/> interface And uses the registration attributes defined in the framework to 
''' register itself And its components with the shell. These attributes tell the pkgdef creation
''' utility what data to put into .pkgdef file.
''' <para></para>
''' To get loaded into VS, the package must be referred by 
''' &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; 
''' in .vsixmanifest file.
''' </remarks>
<InstalledProductRegistration("#110", "#112", "1.0", IconResourceID:=400)>
<ProvideService((GetType(STextWriterService)), IsAsyncQueryable:=True)>
<ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)>
<PackageRegistration(UseManagedResourcesOnly:=True, AllowsBackgroundLoading:=True)>
<Guid(AutoTempFileCleanerVS2022Package.PackageGuidString)>
Public NotInheritable Class AutoTempFileCleanerVS2022Package : Inherits AsyncPackage

#Region " Fields "

    ''' <summary>
    ''' Package guid
    ''' </summary>
    Public Const PackageGuidString As String = "2a7a33bf-2fe1-4dfd-9865-a392972e6f73"

    ''' <summary>
    ''' Points to the system's default temporary directory.
    ''' </summary>
    Private ReadOnly tempDir As New DirectoryInfo(Path.GetTempPath())

    ''' <summary>
    ''' Full path of the log file.
    ''' </summary>
    Private ReadOnly logFilePath As String =
        $"{Me.tempDir}\AutoTempFileCleaner_VS2022_{Date.Now.ToFileTime()}.log"

    ''' <summary>
    ''' Text service used to create and write the log file pointed by <see cref="logFilePath"/>.
    ''' </summary>
    Private textService As ITextWriterService

    ''' <summary>
    ''' Coordinates cross-process instance tracking to determine
    ''' when the last running Visual Studio instance exits.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' This coordinator maintains a shared lease registry
    ''' across all Visual Studio processes, including different
    ''' installed versions (for example VS2022, VS2026, etc.).
    ''' 
    ''' Each Visual Studio instance registers itself during
    ''' package initialization and unregisters during shutdown.
    ''' 
    ''' When the final lease is removed, the current instance
    ''' is considered the last running instance and is allowed
    ''' to execute global cleanup logic.
    ''' 
    ''' This mechanism prevents cleanup routines from executing
    ''' prematurely when multiple Visual Studio instances are
    ''' running simultaneously.
    ''' </remarks>
    Private leaseCoordinator As VsInstanceLeaseCoordinator

#End Region

#Region " Constructors "

    ''' <summary>
    ''' Default constructor of the package.
    ''' <para></para>
    ''' Inside this method you can place any initialization code that does not require 
    ''' any Visual Studio service because at this point the package object is created but 
    ''' not sited yet inside Visual Studio environment. 
    ''' <para></para>
    ''' The place to do all the other initialization is the <see cref="Initialize"/> method.
    ''' </summary>
    Public Sub New()
    End Sub

#End Region

#Region " Public Methods "

    ''' <summary>
    ''' Initialization of the package; this method is called right after the package is sited, so this is the place
    ''' where you can put all the initialization code that rely on services provided by VisualStudio.
    ''' </summary>
    ''' 
    ''' <param name="cancellationToken">
    ''' A cancellation token to monitor for initialization cancellation, 
    ''' which can occur when VS is shutting down.
    ''' </param>
    ''' 
    ''' <param name="progress">
    ''' A provider for progress updates.
    ''' </param>
    ''' 
    ''' <returns>
    ''' A <see cref="Task"/> representing the async work of package initialization, 
    ''' or an already completed task if there is none. 
    ''' <para></para>
    ''' Do not return null from this method.
    ''' </returns>
    Protected Overrides Async Function InitializeAsync(cancellationToken As CancellationToken, progress As IProgress(Of ServiceProgressData)) As Task
        ' When initialized asynchronously, the current thread may be a background thread at this point.
        ' Do any initialization that requires the UI thread after switching to the UI thread.
        Await Me.JoinableTaskFactory.SwitchToMainThreadAsync()

        Await MyBase.InitializeAsync(cancellationToken, progress)
        Me.AddService(GetType(STextWriterService), AddressOf Me.CreateTextWriterServiceAsync, promote:=False)
        Me.textService = TryCast(Await Me.GetServiceAsync(GetType(STextWriterService)), ITextWriterService)


        Me.leaseCoordinator = New VsInstanceLeaseCoordinator()
        Call Me.leaseCoordinator.RegisterLease()
    End Function

    ''' <summary>
    ''' Create and return a new instance of <see cref="TextWriterService"/>.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' <see href="https://docs.microsoft.com/en-us/visualstudio/extensibility/how-to-provide-an-asynchronous-visual-studio-service?view=vs-2019"/>
    ''' </remarks>
    ''' 
    ''' <param name="container">
    ''' The service container.
    ''' </param>
    ''' 
    ''' <param name="cancellationToken">
    ''' The cancellation token.
    ''' </param>
    ''' 
    ''' <param name="serviceType">
    ''' The type of the service.
    ''' </param>
    ''' 
    ''' <returns>
    ''' A <see cref="Task"/> that returns the service.
    ''' </returns>
    Public Async Function CreateTextWriterServiceAsync(container As IAsyncServiceContainer, cancellationToken As CancellationToken, serviceType As Type) As Tasks.Task(Of Object)

        Dim service As New TextWriterService(Me)
        Await service.InitializeAsync(cancellationToken)
        Return service
    End Function

    ''' <summary>
    ''' Called to ask the package whether Visual Studio can be closed.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.package.queryclose"/>
    ''' </remarks>
    ''' 
    ''' <param name="refCanClose">
    ''' Set <paramref name="refCanClose"/> to <see langword="False"/> if you want to prevent Visual Studio from closing; 
    ''' otherwise, set <paramref name="refCanClose"/> to <see langword="True"/>.
    ''' </param>
    ''' 
    ''' <returns>
    ''' By default this function sets <paramref name="refCanClose"/> as <see langword="True"/>, 
    ''' and returns <see cref="VSConstants.S_OK"/>.
    ''' <para></para>
    ''' The return value is of type HRESULT.
    ''' </returns>
    ''' 
    Protected Overrides Function QueryClose(<Out> ByRef refCanClose As Boolean) As Integer

        Dim shouldRunCleanup As Boolean = False

        If Me.leaseCoordinator IsNot Nothing Then
            shouldRunCleanup = Me.leaseCoordinator.UnregisterLeaseAndCheckIfLast()
        End If

        If shouldRunCleanup Then
            Me.DeleteTempFiles()
        End If

        refCanClose = True
        Return VSConstants.S_OK

    End Function

#End Region

#Region "Private Methods"

    ''' <summary>
    ''' Delete temporary files and directories generated by Visual Studio.
    ''' </summary>
    Private Sub DeleteTempFiles()
        Try

            ' Auto Temp File Cleaner (old log files)
            Dim autoTempFileCleaner_VS2022Items As IEnumerable(Of FileSystemInfo) =
            From f As FileInfo In Me.tempDir.EnumerateFiles("AutoTempFileCleaner_VS2022_*.log", SearchOption.TopDirectoryOnly)
            Where (f.Name Like "AutoTempFileCleaner_VS2022_##################.log" AndAlso
               Not f.FullName.Equals(Me.logFilePath, StringComparison.OrdinalIgnoreCase))

            ' BACKGROUND DOWNLOAD
            Dim bckgDdItems As IEnumerable(Of FileSystemInfo) =
                Me.tempDir.EnumerateFiles("dd_*.log", SearchOption.TopDirectoryOnly)

            ' DIAGNOSTIC TOOLS
            Dim diagItems As IEnumerable(Of FileSystemInfo) =
                From d As DirectoryInfo In Me.tempDir.EnumerateDirectories("Report.*", SearchOption.TopDirectoryOnly)
                Where d.Name Like "Report.????????-????-????-????-????????????"
            diagItems = diagItems.Concat(
                From f As FileInfo In Me.tempDir.EnumerateFiles("Report*.diagsession", SearchOption.TopDirectoryOnly)
                Where f.Name Like "Report########-####.diagsession")

            ' NUGET
            Dim nuGetItems As IEnumerable(Of FileSystemInfo) = {
                New DirectoryInfo($"{Me.tempDir}\NuGetScratch")
            }

            ' SETTING LOGS
            Dim settingsItems As IEnumerable(Of FileSystemInfo) = {
                New DirectoryInfo($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Microsoft\VisualStudio\SettingsLogs")
            }

            ' MSBuildTemp
            Dim msBuildItems As IEnumerable(Of FileSystemInfo) =
                From d As DirectoryInfo In Me.tempDir.EnumerateDirectories("MSBuildTemp*", SearchOption.TopDirectoryOnly)
                Where d.Name Like "MSBuildTemp????????????????????????????????"

            ' pkgs
            Dim pkgItems As IEnumerable(Of FileSystemInfo) =
                From d As DirectoryInfo In Me.tempDir.EnumerateDirectories("pkg-*", SearchOption.TopDirectoryOnly)
                Where d.Name Like "pkg-[A-z0-9][A-z0-9][A-z0-9][A-z0-9][A-z0-9][A-z0-9]"

            ' STARTUP
            Dim startupItems As IEnumerable(Of FileSystemInfo) =
                From f As FileInfo In Me.tempDir.EnumerateFiles("startup*", SearchOption.AllDirectories)
                Where f.Name Like "startup##########*"

            ' TELEMETRY
            Dim telemetryItems As IEnumerable(Of FileSystemInfo) = {
                New DirectoryInfo($"{Me.tempDir}\VSFeedbackIntelliCodeLogs")
            }


            ' VS
            Dim vsItems As IEnumerable(Of FileSystemInfo) = {
                New DirectoryInfo($"{Me.tempDir}\VS")
            }

            '' VSIX packages
            '
            ' ⚠️ This logic has been disabled due to a reproducible issue reported by @Caslav Pavlovic:
            '     https://marketplace.visualstudio.com/items?itemName=elektroHacker.AutoTempFileCleanerVS2022&ssr=false#review-details
            '
            'Dim vsixItems As IEnumerable(Of FileSystemInfo) =
            '    From d As DirectoryInfo In Me.tempDir.EnumerateDirectories("*-*-*-*-*", SearchOption.TopDirectoryOnly)
            '    Where d.Name Like "????????-????-????-????-????????????*"
            'vsixItems = vsixItems.Concat(
            '    From f As FileInfo In Me.tempDir.EnumerateFiles("VSIX*.vsix", SearchOption.TopDirectoryOnly)
            '    Where f.Name Like "VSIX[a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9].vsix")

            ' VSFeedbackIntelliCodeLogs
            Dim vsFeedbackIntelliCodeLogsItems As IEnumerable(Of FileSystemInfo) = {
                New DirectoryInfo($"{Me.tempDir}\VSFeedbackIntelliCodeLogs")
            }

            ' VSLogs
            Dim vsLogsItems As IEnumerable(Of FileSystemInfo) = {
                New DirectoryInfo($"{Me.tempDir}\VSLogs")
            }

            ' VsTempFiles
            Dim vsTempFilesItems As IEnumerable(Of FileSystemInfo) = {
                New DirectoryInfo($"{Me.tempDir}\VsTempFiles")
            }

            ' WPF
            Dim wpfItems As IEnumerable(Of FileSystemInfo) = {
                New DirectoryInfo($"{Me.tempDir}\WPF")
            }

            ' OTHER
            Dim otherItems As IEnumerable(Of FileSystemInfo) =
                From d As DirectoryInfo In Me.tempDir.EnumerateDirectories("*.*", SearchOption.TopDirectoryOnly)
                Where d.Name Like "[a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9].[a-z0-9][a-z0-9][a-z0-9]"
            otherItems = otherItems.Concat(
                From f As FileInfo In Me.tempDir.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly)
                Where f.Name Like "[a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9].[a-z0-9][a-z0-9][a-z0-9]")
            otherItems = otherItems.Concat(
                From f As FileInfo In Me.tempDir.EnumerateFiles("dev*.tmp", SearchOption.TopDirectoryOnly)
                Where f.Name Like "dev[A-F0-9][A-F0-9]*[A-F0-9].tmp")
            otherItems = otherItems.Concat(
                From f As FileInfo In Me.tempDir.EnumerateFiles("TFR*.tmp", SearchOption.TopDirectoryOnly)
                Where f.Name Like "TFR[A-F0-9][A-F0-9]*[A-F0-9].tmp")
            otherItems = otherItems.Concat(
                From d As DirectoryInfo In Me.tempDir.EnumerateDirectories("vs.mcj*", SearchOption.TopDirectoryOnly)
                Where d.Name Like "vs.mcj##########")
            otherItems = otherItems.Concat(
                From d As DirectoryInfo In Me.tempDir.EnumerateDirectories("VS.{*}", SearchOption.TopDirectoryOnly)
                Where d.Name Like "VS.{????????-????-????-????-????????????}")
            otherItems = otherItems.Concat(
                From f As FileInfo In Me.tempDir.EnumerateFiles("Microsoft.CodeAnalysi*.dll", SearchOption.AllDirectories))
            otherItems = otherItems.Concat({
                  New DirectoryInfo($"{Me.tempDir}\Roslyn\AnalyzerAssemblyLoader"),
                  New DirectoryInfo($"{Me.tempDir}\TFSTemp"),
                  New DirectoryInfo($"{Me.tempDir}\servicehub"),
                  New DirectoryInfo($"{Me.tempDir}\SymbolCache"),
                  New DirectoryInfo($"{Me.tempDir}\VBCSCompiler"),
                  New DirectoryInfo($"{Me.tempDir}\VisualStudioSourceGeneratedDocuments"),
                  New DirectoryInfo($"{Me.tempDir}\VSGitHubCopilotLogs"),
                  New DirectoryInfo($"{Me.tempDir}\VisualStudio\copilot-vs")})

            Task.WaitAll(
                Me.DeleteFileSystemItemsAsync(autoTempFileCleaner_VS2022Items, "Auto Temp File Cleaner (Previous log files)"),
                Me.DeleteFileSystemItemsAsync(bckgDdItems, "Background Download"),
                Me.DeleteFileSystemItemsAsync(diagItems, "Diagnostic Tools"),
                Me.DeleteFileSystemItemsAsync(msBuildItems, "MS Build"),
                Me.DeleteFileSystemItemsAsync(nuGetItems, "NuGet"),
                Me.DeleteFileSystemItemsAsync(pkgItems, "Pkg"),
                Me.DeleteFileSystemItemsAsync(settingsItems, "Setting Logs"),
                Me.DeleteFileSystemItemsAsync(startupItems, "Startup"),
                Me.DeleteFileSystemItemsAsync(telemetryItems, "Telemetry"),
                Me.DeleteFileSystemItemsAsync(vsItems, "VS"), ' Me.DeleteFileSystemItemsAsync(vsixItems, "VSIX"),
                Me.DeleteFileSystemItemsAsync(vsFeedbackIntelliCodeLogsItems, "VSFeedbackIntelliCodeLogsItems"),
                Me.DeleteFileSystemItemsAsync(vsLogsItems, "VSLogs"),
                Me.DeleteFileSystemItemsAsync(vsTempFilesItems, "VsTempFiles"),
                Me.DeleteFileSystemItemsAsync(wpfItems, "WPF"),
                Me.DeleteFileSystemItemsAsync(otherItems, "Other files")
            )

        Catch ex As Exception
            Me.textService.WriteLineAsync(Me.logFilePath, $"ERROR: {ex.Message}").Wait()

        End Try

    End Sub

    ''' <summary>
    ''' Deletes a <see cref="FileSystemInfo"/> from disk.
    ''' </summary>
    ''' 
    ''' <param name="items">
    ''' The file system items (files and/or directories) to delete from disk.
    ''' </param>
    ''' 
    ''' <param name="description">
    ''' A description or category of the file system items to write it 
    ''' in the log file pointed by <see cref="logFilePath"/>.
    ''' </param>
    Private Async Function DeleteFileSystemItemsAsync(items As IEnumerable(Of FileSystemInfo), description As String) As Task

        If items Is Nothing OrElse
           items.Count = 0 OrElse
           Not items.Any(Function(i) i.Exists) Then

            Return
        End If

        Await Me.textService.WriteLineAsync(Me.logFilePath, description)
        Await Me.textService.WriteLineAsync(Me.logFilePath, New String("-"c, description.Length))
        Await Me.textService.WriteLineAsync(Me.logFilePath, String.Empty)

        For Each item As FileSystemInfo In items

            If Not item.Exists Then
                Continue For
            End If

            ' DELETE FILE
            ' -----------
            If File.Exists(item.FullName) Then
                Dim file As FileInfo = DirectCast(item, FileInfo)
                Dim status As String = String.Empty
                Dim detail As String = String.Empty

                Try
                    file.Delete()
                    status = "SUCCESS"
                    detail = item.FullName

                Catch ex As IOException When IsFileLocked(ex)
                    status = "LOCKED BY PROCESS"
                    detail = item.FullName

                Catch ex As UnauthorizedAccessException
                    status = "ACCESS DENIED"
                    detail = item.FullName

                Catch ex As Exception
                    status = "ERROR"
                    detail = $"{item.FullName} | {ex.Message}"

                End Try

                Dim logMessage As String = $"{status}: {detail}"
                Await Me.textService.WriteLineAsync(Me.logFilePath, logMessage)

                ' DELETE PARENT DIR IF EMPTY
                Dim directory As DirectoryInfo = file?.Directory

                If NativeMethods.PathIsDirectoryEmpty(directory?.FullName) Then
                    Dim dirStatus As String
                    Dim dirDetail As String

                    Try
                        directory.Delete(recursive:=False)
                        dirStatus = "SUCCESS"
                        dirDetail = directory.FullName

                    Catch ex As IOException When IsFileLocked(ex)
                        dirStatus = "LOCKED BY PROCESS"
                        dirDetail = directory.FullName

                    Catch ex As UnauthorizedAccessException
                        dirStatus = "ACCESS DENIED"
                        dirDetail = directory.FullName

                    Catch ex As Exception
                        dirStatus = "ERROR"
                        dirDetail = $"{directory.FullName} | {ex.Message}"

                    End Try

                    Dim dirLogMessage As String = $"{dirStatus}: {dirDetail}"
                    Await Me.textService.WriteLineAsync(Me.logFilePath, dirLogMessage)

                End If

            End If

            ' ----------------------------------------------------------------------------

            ' DELETE DIR RECURSIVELY
            ' ----------------------
            If Directory.Exists(item.FullName) Then

                Dim dir As DirectoryInfo = DirectCast(item, DirectoryInfo)

                ' DELETE FILES FIRST
                For Each fi As FileInfo In dir.EnumerateFiles("*", SearchOption.AllDirectories)

                    Dim fileStatus As String
                    Dim fileDetail As String

                    Try
                        fi.Delete()
                        fileStatus = "SUCCESS"
                        fileDetail = fi.FullName

                    Catch ex As IOException When IsFileLocked(ex)
                        fileStatus = "LOCKED BY PROCESS"
                        fileDetail = fi.FullName

                    Catch ex As UnauthorizedAccessException
                        fileStatus = "ACCESS DENIED"
                        fileDetail = fi.FullName

                    Catch ex As Exception
                        fileStatus = "ERROR"
                        fileDetail = $"{fi.FullName} | {ex.Message}"

                    End Try

                    Dim fileLog As String = $"{fileStatus}: {fileDetail}"
                    Await Me.textService.WriteLineAsync(Me.logFilePath, fileLog)
                Next fi

                ' DELETE DIRECTORY
                Dim dirStatus As String
                Dim dirDetail As String

                Try
                    dir.Delete(recursive:=True)
                    dirStatus = "SUCCESS"
                    dirDetail = dir.FullName

                Catch ex As IOException When IsFileLocked(ex)
                    dirStatus = "LOCKED BY PROCESS"
                    dirDetail = dir.FullName

                Catch ex As UnauthorizedAccessException
                    dirStatus = "ACCESS DENIED"
                    dirDetail = dir.FullName

                Catch ex As Exception
                    dirStatus = "ERROR"
                    dirDetail = $"{dir.FullName} | {ex.Message}"

                End Try

                Dim dirLog As String = $"{dirStatus}: {dirDetail}"
                Await Me.textService.WriteLineAsync(Me.logFilePath, dirLog)

            End If

        Next

        Await Me.textService.WriteLineAsync(Me.logFilePath, String.Empty)

    End Function

    ''' <summary>
    ''' Determines whether the specified <see cref="IOException"/> was caused by a file being locked
    ''' or in use by another process.
    ''' </summary>
    ''' 
    ''' <param name="ex">
    ''' The <see cref="IOException"/> instance to evaluate.
    ''' </param>
    ''' 
    ''' <returns>
    ''' <see langword="True"/> if the exception corresponds to a file sharing or lock violation;
    ''' otherwise, <see langword="False"/>.
    ''' </returns>
    ''' 
    ''' <remarks>
    ''' This method inspects the Win32 error code embedded in the exception's HRESULT value.
    ''' Common cases include:
    ''' <list type="bullet">
    ''' <item><c>32</c> - ERROR_SHARING_VIOLATION (file is being used by another process)</item>
    ''' <item><c>33</c> - ERROR_LOCK_VIOLATION (file is locked)</item>
    ''' </list>
    ''' 
    ''' These errors typically occur when attempting to delete or modify files that are
    ''' currently opened by Visual Studio, the operating system, or other external tools.
    ''' </remarks>
    Private Shared Function IsFileLocked(ex As IOException) As Boolean

        Dim errorCode As Integer = Marshal.GetHRForException(ex) And &HFFFF

        Return errorCode = 32 OrElse errorCode = 33

    End Function

#End Region

End Class

#End Region

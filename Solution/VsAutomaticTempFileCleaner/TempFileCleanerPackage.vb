' ***********************************************************************
' Author   : ElektroStudios
' Modified : 14-July-2021
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

#Region " TempFileCleanerPackage "

''' ----------------------------------------------------------------------------------------------------
''' <summary>
''' This is the class that implements the package exposed by this assembly.
''' </summary>
''' ----------------------------------------------------------------------------------------------------
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
''' ----------------------------------------------------------------------------------------------------
<InstalledProductRegistration("#110", "#112", "1.0", IconResourceID:=400)>
<ProvideService((GetType(STextWriterService)), IsAsyncQueryable:=True)>
<ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)>
<PackageRegistration(UseManagedResourcesOnly:=True, AllowsBackgroundLoading:=True)>
<Guid(TempFileCleanerPackage.PackageGuidString)>
Public NotInheritable Class TempFileCleanerPackage : Inherits AsyncPackage

#Region " Fields "

    ''' <summary>
    ''' Package guid
    ''' </summary>
    Public Const PackageGuidString As String = "1a7a33bf-2fe1-4dfd-9865-a392972e6f73"

    ''' <summary>
    ''' Points to the system's default temporary directory.
    ''' </summary>
    Private ReadOnly tempDir As New DirectoryInfo(Path.GetTempPath())

    ''' <summary>
    ''' Full path of the log file.
    ''' </summary>
    Private ReadOnly logFilePath As String =
        $"{Me.tempDir}\VsAutomaticTempFileCleaner_{Date.Now.ToFileTime()}.log"

    ''' <summary>
    ''' Text service used to create and write the log file pointed by <see cref="logFilePath"/>.
    ''' </summary>
    Private textService As ITextWriterService

#End Region

#Region " Constructors "

    ''' ----------------------------------------------------------------------------------------------------
    ''' <summary>
    ''' Default constructor of the package.
    ''' <para></para>
    ''' Inside this method you can place any initialization code that does not require 
    ''' any Visual Studio service because at this point the package object is created but 
    ''' not sited yet inside Visual Studio environment. 
    ''' <para></para>
    ''' The place to do all the other initialization is the <see cref="Initialize"/> method.
    ''' </summary>
    ''' ----------------------------------------------------------------------------------------------------
    Public Sub New()
    End Sub

#End Region

#Region " Public Methods "

    ''' ----------------------------------------------------------------------------------------------------
    ''' <summary>
    ''' Initialization of the package; this method is called right after the package is sited, so this is the place
    ''' where you can put all the initialization code that rely on services provided by VisualStudio.
    ''' </summary>
    ''' ----------------------------------------------------------------------------------------------------
    ''' <param name="cancellationToken">
    ''' A cancellation token to monitor for initialization cancellation, 
    ''' which can occur when VS is shutting down.
    ''' </param>
    ''' 
    ''' <param name="progress">
    ''' A provider for progress updates.
    ''' </param>
    ''' ----------------------------------------------------------------------------------------------------
    ''' <returns>
    ''' A <see cref="Task"/> representing the async work of package initialization, 
    ''' or an already completed task if there is none. 
    ''' <para></para>
    ''' Do not return null from this method.
    ''' </returns>
    ''' ----------------------------------------------------------------------------------------------------
    Protected Overrides Async Function InitializeAsync(cancellationToken As CancellationToken, progress As IProgress(Of ServiceProgressData)) As Task
        ' When initialized asynchronously, the current thread may be a background thread at this point.
        ' Do any initialization that requires the UI thread after switching to the UI thread.
        Await Me.JoinableTaskFactory.SwitchToMainThreadAsync()

        Await MyBase.InitializeAsync(cancellationToken, progress)
        Me.AddService(GetType(STextWriterService), AddressOf Me.CreateTextWriterService, promote:=False)
        Me.textService = TryCast(Await Me.GetServiceAsync(GetType(STextWriterService)), ITextWriterService)
    End Function

    ''' ----------------------------------------------------------------------------------------------------
    ''' <summary>
    ''' Create and return a new instance of <see cref="TextWriterService"/>.
    ''' </summary>
    ''' ----------------------------------------------------------------------------------------------------
    ''' <remarks>
    ''' <see href="https://docs.microsoft.com/en-us/visualstudio/extensibility/how-to-provide-an-asynchronous-visual-studio-service?view=vs-2019"/>
    ''' </remarks>
    ''' ----------------------------------------------------------------------------------------------------
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
    ''' ----------------------------------------------------------------------------------------------------
    ''' <returns>
    ''' A <see cref="Task"/> that returns the service.
    ''' </returns>
    ''' ----------------------------------------------------------------------------------------------------
    Public Async Function CreateTextWriterService(container As IAsyncServiceContainer, cancellationToken As CancellationToken, serviceType As Type) As Tasks.Task(Of Object)
        Dim service As New TextWriterService(Me)
        Await service.InitializeAsync(cancellationToken)
        Return service
    End Function

    ''' ----------------------------------------------------------------------------------------------------
    ''' <summary>
    ''' Called to ask the package whether Visual Studio can be closed.
    ''' </summary>
    ''' ----------------------------------------------------------------------------------------------------
    ''' <remarks>
    ''' <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.package.queryclose"/>
    ''' </remarks>
    ''' ----------------------------------------------------------------------------------------------------
    ''' <param name="refCanClose">
    ''' Set <paramref name="refCanClose"/> to <see langword="False"/> if you want to prevent Visual Studio from closing; 
    ''' otherwise, set <paramref name="refCanClose"/> to <see langword="True"/>.
    ''' </param>
    ''' ----------------------------------------------------------------------------------------------------
    ''' <returns>
    ''' By default this function sets <paramref name="refCanClose"/> as <see langword="True"/>, 
    ''' and returns <see cref="VSConstants.S_OK"/>.
    ''' <para></para>
    ''' The return value is of type HRESULT.
    ''' </returns>
    ''' ----------------------------------------------------------------------------------------------------
    Protected Overrides Function QueryClose(<Out> ByRef refCanClose As Boolean) As Integer
        Me.DeleteTempFiles()

        refCanClose = True
        Return VSConstants.S_OK
    End Function

#End Region

#Region "Private Methods"

    ''' ----------------------------------------------------------------------------------------------------
    ''' <summary>
    ''' Delete temporary files and directories generated by Visual Studio.
    ''' </summary>
    ''' ----------------------------------------------------------------------------------------------------
    Private Sub DeleteTempFiles()

        ' Vs Automatic Temp File Cleaner (old log files)
        Dim vsAutomaticTempFileCleanerItems As IEnumerable(Of FileSystemInfo) =
            From f As FileInfo In Me.tempDir.EnumerateFiles("VsAutomaticTempFileCleaner_*.log", SearchOption.TopDirectoryOnly)
            Where (f.Name Like "VsAutomaticTempFileCleaner_##################.log" AndAlso
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

        ' STARTUP
        Dim startupItems As IEnumerable(Of FileSystemInfo) =
            From f As FileInfo In Me.tempDir.EnumerateFiles("startup*", SearchOption.AllDirectories)
            Where f.Name Like "startup##########*"

        ' TELEMETRY
        Dim telemetryItems As IEnumerable(Of FileSystemInfo) = {
            New DirectoryInfo($"{Me.tempDir}\VSFeedbackIntelliCodeLogs")
        }

        ' VSIX
        Dim vsixItems As IEnumerable(Of FileSystemInfo) =
            From d As DirectoryInfo In Me.tempDir.EnumerateDirectories("*-*-*-*-*", SearchOption.TopDirectoryOnly)
            Where d.Name Like "????????-????-????-????-????????????*"
        vsixItems = vsixItems.Concat(
            From f As FileInfo In Me.tempDir.EnumerateFiles("VSIX*.vsix", SearchOption.TopDirectoryOnly)
            Where f.Name Like "VSIX[a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9].vsix")

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
              New DirectoryInfo($"{Me.tempDir}\servicehub"),
              New DirectoryInfo($"{Me.tempDir}\SymbolCache"),
              New DirectoryInfo($"{Me.tempDir}\VBCSCompiler"),
              New DirectoryInfo($"{Me.tempDir}\VisualStudioSourceGeneratedDocuments")})

        Me.DeleteFileSystemItems(vsAutomaticTempFileCleanerItems, "Vs Automatic Temp File Cleaner (Old log files)")
        Me.DeleteFileSystemItems(bckgDdItems, "Background Download")
        Me.DeleteFileSystemItems(diagItems, "Diagnostic Tools")
        Me.DeleteFileSystemItems(nuGetItems, "NuGet")
        Me.DeleteFileSystemItems(settingsItems, "Setting Logs")
        Me.DeleteFileSystemItems(startupItems, "Startup")
        Me.DeleteFileSystemItems(telemetryItems, "Telemetry")
        Me.DeleteFileSystemItems(vsixItems, "VSIX")
        Me.DeleteFileSystemItems(wpfItems, "WPF")
        Me.DeleteFileSystemItems(otherItems, "Other")

    End Sub

    ''' ----------------------------------------------------------------------------------------------------
    ''' <summary>
    ''' Deletes a <see cref="FileSystemInfo"/> from disk.
    ''' </summary>
    ''' ----------------------------------------------------------------------------------------------------
    ''' <param name="items">
    ''' The file system items (files and/or directories) to delete from disk.
    ''' </param>
    ''' 
    ''' <param name="description">
    ''' A description or category of the file system items to write it 
    ''' in the log file pointed by <see cref="logFilePath"/>.
    ''' </param>
    ''' ----------------------------------------------------------------------------------------------------
    Private Async Sub DeleteFileSystemItems(items As IEnumerable(Of FileSystemInfo), description As String)

        Await Me.textService.WriteLineAsync(Me.logFilePath, description)
        Await Me.textService.WriteLineAsync(Me.logFilePath, New String("-"c, description.Length))
        Await Me.textService.WriteLineAsync(Me.logFilePath, String.Empty)

        For Each item As FileSystemInfo In items

            If Not item.Exists Then
                Continue For
            End If

            ' DELETE FILE
            If File.Exists(item.FullName) Then
                Dim file As FileInfo = DirectCast(item, FileInfo)
                Try
                    file.Delete()
                    Await Me.textService.WriteLineAsync(Me.logFilePath, $"{item.FullName}")
                Catch
                End Try

                ' DELETE DIR If EMPTY
                If NativeMethods.PathIsDirectoryEmpty(file.Directory.FullName) Then
                    Try
                        file.Directory.Delete(recursive:=False)
                        Await Me.textService.WriteLineAsync(Me.logFilePath, $"{file.Directory.FullName}")
                    Catch
                    End Try
                End If

            End If

            ' DELETE DIR RECURSIVELY
            If Directory.Exists(item.FullName) Then
                Dim dir As DirectoryInfo = DirectCast(item, DirectoryInfo)
                For Each fi As FileInfo In dir.EnumerateFiles("*", SearchOption.AllDirectories)
                    Try
                        fi.Delete()
                        Await Me.textService.WriteLineAsync(Me.logFilePath, $"{fi.FullName}")
                    Catch
                    End Try
                Next

                Try
                    dir.Delete(recursive:=True)
                    Await Me.textService.WriteLineAsync(Me.logFilePath, $"{dir.FullName}")
                Catch
                End Try
            End If

        Next

        Await Me.textService.WriteLineAsync(Me.logFilePath, String.Empty)

    End Sub

#End Region

End Class

#End Region

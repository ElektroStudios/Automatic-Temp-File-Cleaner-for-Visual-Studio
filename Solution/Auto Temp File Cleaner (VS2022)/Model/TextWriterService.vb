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

Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Threading
Imports System
Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks

Imports IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider
Imports Task = System.Threading.Tasks.Task

#End Region

#Region " TextWriterService "

''' ----------------------------------------------------------------------------------------------------
''' <summary>
''' A class that implements both the service and the service interface.
''' </summary>
''' ----------------------------------------------------------------------------------------------------
''' <remarks>
''' <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.asyncservicecreatorcallback?view=visualstudiosdk-2019"/>
''' </remarks>
''' ----------------------------------------------------------------------------------------------------
Public Class TextWriterService : Implements STextWriterService, ITextWriterService

    Private ReadOnly asyncServiceProvider As IAsyncServiceProvider

    Public Sub New(provider As IAsyncServiceProvider)
        ' constructor should only be used for simple initialization
        ' any usage of Visual Studio service, expensive background operations should happen in the
        ' asynchronous InitializeAsync method for best performance
        Me.asyncServiceProvider = provider
    End Sub

    Public Async Function InitializeAsync(ByVal cancellationToken As CancellationToken) As Task
        Await TaskScheduler.Default
        ' Do background operations that involve IO or other async methods

        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken)
        ' Query Visual Studio services on main thread unless they are documented as free threaded explicitly.
        ' The reason for this is the final cast to service interface (such as IVsShell) may involve COM operations to add/release references.

        Dim vsShell As IVsShell = TryCast(Me.asyncServiceProvider.GetServiceAsync(GetType(SVsShell)), IVsShell)
        ' Use Visual Studio services to continue initialization
    End Function

    Public Async Function WriteLineAsync(path As String, line As String) As Task Implements ITextWriterService.WriteLineAsync
        Try
            Using writer As New StreamWriter(path, append:=True)
                Await writer.WriteLineAsync(line)
            End Using
        Catch ex As Exception
        End Try
    End Function

End Class

''' ----------------------------------------------------------------------------------------------------
''' <summary>
''' An empty interface that identifies the service. 
''' <para></para>
''' This have no methods defined as it is only used for querying the service.
''' </summary>
''' ----------------------------------------------------------------------------------------------------
Public Interface STextWriterService
End Interface

''' ----------------------------------------------------------------------------------------------------
''' <summary>
''' An interface that describes the service interface. 
''' <para></para>
''' This interface defines the methods to be implemented.
''' </summary>
''' ----------------------------------------------------------------------------------------------------
Public Interface ITextWriterService
    Function WriteLineAsync(path As String, line As String) As Task
End Interface

#End Region

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

Imports System
Imports System.Diagnostics
Imports System.Runtime.InteropServices

#End Region

#Region " NativeMethods "

' ReSharper disable once CheckNamespace

''' ----------------------------------------------------------------------------------------------------
''' <summary>
''' Platform Invocation methods (P/Invoke), access unmanaged code.
''' <para></para>
''' This class does not suppress stack walks for unmanaged code permission.
''' <see cref="Security.SuppressUnmanagedCodeSecurityAttribute"/> must not be applied to this class.
''' <para></para>
''' This class is for methods that can be used anywhere because a stack walk will be performed.
''' </summary>
''' ----------------------------------------------------------------------------------------------------
''' <remarks>
''' <see href="https://msdn.microsoft.com/en-us/library/ms182161.aspx"/>
''' </remarks>
''' ----------------------------------------------------------------------------------------------------
Friend NotInheritable Class NativeMethods

#Region " Constructors "

        ''' ----------------------------------------------------------------------------------------------------
        ''' <summary>
        ''' Prevents a default instance of the <see cref="NativeMethods"/> class from being created.
        ''' </summary>
        ''' ----------------------------------------------------------------------------------------------------
        <DebuggerNonUserCode>
        Private Sub New()
        End Sub

#End Region

#Region " Shlwapi.dll "

        ''' ----------------------------------------------------------------------------------------------------
        ''' <summary>
        ''' Determines whether a specified path is an empty directory.
        ''' </summary>
        ''' ----------------------------------------------------------------------------------------------------
        ''' <remarks>
        ''' <see href="https://docs.microsoft.com/en-us/windows/desktop/api/shlwapi/nf-shlwapi-pathisdirectoryemptya"/>
        ''' </remarks>
        ''' ----------------------------------------------------------------------------------------------------
        ''' <param name="path">
        ''' A pointer to a null-terminated string of maximum length MAX_PATH that contains the path to be tested.
        ''' </param>
        ''' ----------------------------------------------------------------------------------------------------
        ''' <returns>
        ''' Returns <see langword="True"/> if <paramref name="path"/> is an empty directory.
        ''' <para></para>
        ''' Returns <see langword="False"/> if <paramref name="path"/> is not a directory,
        ''' or if it contains at least one file other than "." or "..".
        ''' </returns>
        ''' ----------------------------------------------------------------------------------------------------
        <DllImport("ShlwApi.dll", SetLastError:=True, CharSet:=CharSet.Auto, BestFitMapping:=False, ThrowOnUnmappableChar:=True)>
        Public Shared Function PathIsDirectoryEmpty(path As String
        ) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

#End Region

    End Class

#End Region

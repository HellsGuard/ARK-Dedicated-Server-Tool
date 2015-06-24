Imports System
Imports System.IO
Imports System.IO.Compression
Imports System.IO.Compression.ZipArchive
Imports System.Deployment
Imports System.Deployment.Application
Imports System.Diagnostics
Imports System.Threading
Imports System.Windows.Forms
Imports System.Configuration
Imports System.ComponentModel
Imports System.Net

Public Class AutoUpdate
    Private Sub AutoUpdate_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim zipPath As String = "update.zip"
        Dim extractPath As String = ".\"

        'Clean up
        If My.Computer.FileSystem.FileExists("ARK Dedicated Server Tool.exe") Then
            My.Computer.FileSystem.DeleteFile("ARK Dedicated Server Tool.exe")
        End If

        Do Until My.Computer.FileSystem.FileExists("ARK Dedicated Server Tool.exe")
            If Not My.Computer.FileSystem.FileExists("update.zip") Then
                My.Computer.Network.DownloadFile("http://hellsguard.site11.com/ARK_Server_Tool/update.zip", "update.zip", True, 500)
            End If

            If My.Computer.FileSystem.FileExists("update.zip") Then
                ZipFile.ExtractToDirectory(zipPath, extractPath)
            End If
        Loop

        'Clean up
        If My.Computer.FileSystem.FileExists("update.zip") And My.Computer.FileSystem.FileExists("ARK Dedicated Server Tool.exe") Then
            My.Computer.FileSystem.DeleteFile("update.zip")
        End If

        If My.Computer.FileSystem.FileExists("updater.zip") And My.Computer.FileSystem.FileExists("ARK Dedicated Server Tool.exe") Then
            My.Computer.FileSystem.DeleteFile("updater.zip")
        End If

        MsgBox("Auto Update complete, restarting Server Tool.", , "Auto Update")
        Process.Start("ARK Dedicated Server Tool.exe")
        Me.Close()
    End Sub
End Class

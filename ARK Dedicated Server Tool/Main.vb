Imports System
Imports System.IO
Imports System.IO.Compression
Imports System.IO.Compression.ZipArchive
Imports System.Deployment
Imports System.Deployment.Application
Imports System.Threading

Public Class Main
    ' Vars
    Dim INI_File As New IniFile("ark_server\ShooterGame\Saved\Config\WindowsServer\GameUserSettings.ini")
    Dim INI_Settings As New IniFile(".\ServerToolSettings.ini")
    Dim path As String = "ark_server\ShooterGame\Saved\Config\WindowsServer\"
    Dim versionNum As String = My.Application.Info.Version.ToString
    Dim updateThread As New System.Threading.Thread(AddressOf ScheduledUpdate)

    'Main
    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'Setup Config Dir
        ConfigDir()

        'Set Version Number
        lblVersion.Text = "Version: " + versionNum

        'Load Configs
        LoadConfig()

        'Scheduled Update - Disabled for Current Version
        'updateThread.Start()
    End Sub

    'Launch Server Control
    Private Sub btnLaunchServer_Click(sender As Object, e As EventArgs) Handles btnLaunchServer.Click
        Dim parameters As String
        parameters = "TheIsland?QueryPort=" + txtServerPort.Text + "?MaxPlayers=" + txtMaxPlayers.Text + "?listen -nosteamclient -game -server -log"
        Process.Start("ARK_Server\ShooterGame\Binaries\Win64\ShooterGameServer.exe", parameters)
        'MsgBox(parameters, , "Debug")
    End Sub

    'Stop Server Control
    Private Sub btnStopServer_Click(sender As Object, e As EventArgs) Handles btnStopServer.Click
        MsgBox("Be sure to use the saveworld command in your in-game console before proceeding.", , "Warning")
        Dim p() As Process
        p = Process.GetProcessesByName("ShooterGameServer")

        If p.Count > 0 Then
            Process.GetProcessesByName("ShooterGameServer")(0).Kill()
        End If
    End Sub

    'Update Server Control
    Private Sub btnUpdateServer_Click(sender As Object, e As EventArgs) Handles btnUpdateServer.Click
        MsgBox("Be sure your server is stopped before pressing OK.", , "Warning")

        Dim parameters As String
        parameters = "+login anonymous +force_install_dir ..\ARK_Server +app_update 376030 validate +quit"
        Process.Start("SteamCMD\steamcmd.exe", parameters)
    End Sub

    'Install SteamCMD Control
    Private Sub btnInstallSteamCMD_Click(sender As Object, e As EventArgs) Handles btnInstallSteamCMD.Click
        Dim startPath As String = "SteamCMD"
        Dim zipPath As String = "steamCMD.zip"
        Dim extractPath As String = "SteamCMD"
        Dim parameters As String

        Do Until My.Computer.FileSystem.FileExists("SteamCMD\SteamCMD.exe")
            'Download SteamCMD if doesn't exist
            If Not My.Computer.FileSystem.FileExists("SteamCMD.zip") Then
                My.Computer.Network.DownloadFile("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip", "steamCMD.zip", True, 500)
            End If

            'Extract SteamCMD
            If My.Computer.FileSystem.FileExists("SteamCMD.zip") Then
                ZipFile.ExtractToDirectory(zipPath, extractPath)
            End If
        Loop

        parameters = "+login anonymous +force_install_dir ..\ARK_Server +app_update 376030 validate +quit"
        Process.Start("SteamCMD\steamcmd.exe", parameters)
        MsgBox("SteamCMD is Installing or Updating, it will auto-close", , "SteamCMD")
    End Sub

    'Load All Configs
    Private Sub LoadConfig()
        'Get & Set all Boolean Types
        chkShowPlayerMarker.Checked = INI_File.GetString("ServerSettings", "ShowMapPlayerLocation", False)
        chkServerHardcore.Checked = INI_File.GetString("ServerSettings", "ServerHardcore", False)
        chkGlobalVoiceChat.Checked = INI_File.GetString("ServerSettings", "GlobalVoiceChat", False)
        chkProximityChat.Checked = INI_File.GetString("ServerSettings", "ProximityChat", False)
        chkAllowTributeDownloads.Checked = INI_File.GetString("ServerSettings", "NoTributeDownloads", False)
        chkAllowThirdPersonPlayer.Checked = INI_File.GetString("ServerSettings", "AllowThirdPersonPlayer", False)
        chkAlwaysNotifyPlayerJoined.Checked = INI_File.GetString("ServerSettings", "AlwaysNotifyPlayerLeft", False)
        chkDontAlwaysNotifyPlayerJoined.Checked = INI_File.GetString("ServerSettings", "DontAlwaysNotifyPlayerJoined", False)
        chkServerPVE.Checked = INI_File.GetString("ServerSettings", "ServerPVE", False)
        chkServerCrosshair.Checked = INI_File.GetString("ServerSettings", "ServerCrosshair", False)
        chkServerForceNoHUD.Checked = INI_File.GetString("ServerSettings", "ServerForceNoHUD", False)

        'Get & Set Program Settings - Disabled for Current Version
        'chkScheduledUpdate.Checked = INI_Settings.GetString("Settings", "ScheduledUpdate", False)
        'txtTime.Text = INI_Settings.GetString("Settings", "Time", "12:00")
        'cmbTimeAMPM.Text = INI_Settings.GetString("Settings", "TimeAMPM", "AM")

        'Get & Set all String Types
        txtMessageOfTheDay.Text = INI_File.GetString("MessageOfTheDay", "Message", "")
        txtServerPassword.Text = INI_File.GetString("ServerSettings", "ServerPassword", "")
        txtAdminPassword.Text = INI_File.GetString("ServerSettings", "ServerAdminPassword", "")
        txtServerName.Text = INI_File.GetString("SessionSettings", "SessionName", "")
        txtServerPort.Text = INI_File.GetString("SessionSettings", "QueryPort", "27015")
        txtDifficultyOffset.Text = INI_File.GetString("ServerSettings", "DifficultyOffset", "0.200000")

        'Get & Set all Integer Types
        txtMaxPlayers.Text = INI_File.GetString("/Script/Engine.GameSession", "MaxPlayers", 70)
    End Sub

    'Save All Configs
    Private Sub btnSaveConfig_Click(sender As Object, e As EventArgs) Handles btnSaveConfig.Click
        'Write all Boolean Type Values
        INI_File.WriteBoolean("ServerSettings", "ShowMapPlayerLocation", chkShowPlayerMarker.Checked)
        INI_File.WriteBoolean("ServerSettings", "ServerHardcore", chkServerHardcore.Checked)
        INI_File.WriteBoolean("ServerSettings", "GlobalVoiceChat", chkGlobalVoiceChat.Checked)
        INI_File.WriteBoolean("ServerSettings", "ProximityChat", chkProximityChat.Checked)
        INI_File.WriteBoolean("ServerSettings", "NoTributeDownloads", chkAllowTributeDownloads.Checked)
        INI_File.WriteBoolean("ServerSettings", "AllowThirdPersonPlayer", chkAllowThirdPersonPlayer.Checked)
        INI_File.WriteBoolean("ServerSettings", "AlwaysNotifyPlayerLeft", chkAlwaysNotifyPlayerJoined.Checked)
        INI_File.WriteBoolean("ServerSettings", "DontAlwaysNotifyPlayerJoined", chkDontAlwaysNotifyPlayerJoined.Checked)
        INI_File.WriteBoolean("ServerSettings", "ServerPVE", chkServerPVE.Checked)
        INI_File.WriteBoolean("ServerSettings", "ServerCrosshair", chkServerCrosshair.Checked)
        INI_File.WriteBoolean("ServerSettings", "ServerForceNoHUD", chkServerForceNoHUD.Checked)

        'Save Program Settings - Disabled for Current Version
        'INI_Settings.WriteBoolean("Settings", "ScheduledUpdate", chkScheduledUpdate.Checked)
        'INI_Settings.WriteString("Settings", "Time", txtTime.Text)
        'INI_Settings.WriteString("Settings", "TimeAMPM", cmbTimeAMPM.Text)

        'Write all String Type Values
        INI_File.WriteString("MessageOfTheDay", "Message", txtMessageOfTheDay.Text)
        INI_File.WriteString("ServerSettings", "ServerPassword", txtServerPassword.Text)
        INI_File.WriteString("ServerSettings", "ServerAdminPassword", txtAdminPassword.Text)
        INI_File.WriteString("SessionSettings", "SessionName", txtServerName.Text)
        INI_File.WriteString("SessionSettings", "QueryPort", txtServerPort.Text)
        INI_File.WriteString("ServerSettings", "DifficultyOffset", txtDifficultyOffset.Text)

        'Write all Integer Type Values
        INI_File.WriteString("/Script/Engine.GameSession", "MaxPlayers", txtMaxPlayers.Text)

        'Config Saved
        MsgBox("Config has been saved.", , "Config")
    End Sub

    'Setup Directories
    Private Sub ConfigDir()
        Try
            ' Determine whether the directory exists. 
            If Directory.Exists(path) Then
                Console.WriteLine("That path exists already.")
            End If

            ' Try to create the directory. 
            Dim di As DirectoryInfo = Directory.CreateDirectory(path)
            Console.WriteLine("The directory was created successfully at {0}.", Directory.GetCreationTime(path))

        Catch e As Exception
            Console.WriteLine("The process failed: {0}.", e.ToString())
        End Try
    End Sub

    'Scheduled Update Check
    Private Sub ScheduledUpdate()
        Dim updateTime As DateTime
        updateTime = txtTime.Text + cmbTimeAMPM.Text
        Dim parameters As String
        parameters = "+login anonymous +force_install_dir ..\ARK_Server +app_update 376030 validate +quit"

        Do While chkScheduledUpdate.Checked = True
            If TimeOfDay = updateTime Then
                'DEBUGGING / TESTING ONLY
                MsgBox(updateTime + " " + TimeOfDay, , "DEBUG")

                'Disabled until fully functional
                'Process.Start("SteamCMD\steamcmd.exe", parameters)
            End If
        Loop
    End Sub
End Class

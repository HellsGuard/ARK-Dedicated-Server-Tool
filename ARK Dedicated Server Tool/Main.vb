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

Public Class Main
    ' Vars
    Dim configINI As New IniFile
    Dim serversINI As New IniFile
    Dim configFileName As String = "GameUserSettings.ini"
    Dim serversFileName As String = ".\Servers.ini"
    Dim path As String
    Dim versionNum As String = My.Application.Info.Version.ToString
    Dim updateThread As New System.Threading.Thread(AddressOf ScheduledUpdate)

    'Main
    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        lblVersion.Text = "Version: " + versionNum
        lblCurrentServer.Text = "Current Server: " + My.Settings.currentServer

        If My.Settings.firstTimeRan = True Then
            If My.Settings.defaultSetup = True Then
                txtMultiServerName.Enabled = False
                lbServerSelect.Enabled = False
                btnSaveMultiServer.Enabled = False
                Label11.Enabled = False
                Label10.Enabled = False
            End If

            'Load Config
            LoadConfig()

            'Set Version
            lblVersion.Text = "Version: " + versionNum

            'Scheduled Update - Disabled for Current Version
            'updateThread.Start()

            MultiServerPopulate()
        Else
            FirstSetup.ShowDialog()
        End If
    End Sub

    'Launch Server Control
    Private Sub btnLaunchServer_Click(sender As Object, e As EventArgs) Handles btnLaunchServer.Click
        Dim parameters As String

        If chkMultiHome.Checked = True Then
            parameters = "TheIsland?QueryPort=" + txtServerPort.Text + "?MaxPlayers=" + txtMaxPlayers.Text + "?MultiHome=" + txtMultiHome.Text + "?listen -nosteamclient -game -server -log"
        Else
            parameters = "TheIsland?QueryPort=" + txtServerPort.Text + "?MaxPlayers=" + txtMaxPlayers.Text + "?listen -nosteamclient -game -server -log"
        End If

        If My.Settings.defaultSetup = True Then
            Process.Start(My.Settings.runningServerDirectory + "ShooterGameServer.exe", parameters)
        Else
            Process.Start(My.Settings.runningServerDirectory + "ShooterGameServer.exe", parameters)
        End If
    End Sub

    'Stop Server Control
    Private Sub btnStopServer_Click(sender As Object, e As EventArgs) Handles btnStopServer.Click
        'MsgBox("Be sure to use the saveworld command in your in-game console before proceeding.", , "Warning")
        'Dim p() As Process
        'p = Process.GetProcessesByName("ShooterGameServer")
        '
        'If p.Count > 0 Then
        '    Process.GetProcessesByName("ShooterGameServer")(0).Kill()
        'Else
        '    MsgBox("Running server not found.", , "Error")
        'End If

        For Each p As Process In Process.GetProcesses
            Dim file As String
            Try
                file = p.Modules(0).FileName
            Catch ex As Win32Exception
                file = "n/a"
            End Try

            If p.ProcessName = "ShooterGameServer" Then
                MsgBox(String.Format("Process {0}: {1}", p.ProcessName, file))
            End If
        Next
    End Sub

    'Update Server Control
    Private Sub btnUpdateServer_Click(sender As Object, e As EventArgs) Handles btnUpdateServer.Click
        MsgBox("Be sure your server is stopped before pressing OK.", , "Warning")

        Dim parameters As String

        If My.Settings.defaultSetup = True Then
            parameters = "+login anonymous +force_install_dir ..\ARK_Server +app_update 376030 validate +quit"
            Process.Start("SteamCMD\steamcmd.exe", parameters)
        Else
            parameters = "+login anonymous +force_install_dir " + Chr(34) + My.Settings.advancedInstallDirectory + Chr(34) + " +app_update 376030 validate +quit"
            Process.Start(My.Settings.advancedInstallDirectory + "\SteamCMD\steamcmd.exe", parameters)
        End If

    End Sub

    'Install SteamCMD Control
    Private Sub btnInstallSteamCMD_Click(sender As Object, e As EventArgs) Handles btnInstallSteamCMD.Click
        Dim zipPath As String = "steamCMD.zip"
        Dim extractPath As String = My.Settings.advancedInstallDirectory + "\SteamCMD"
        Dim parameters As String

        Do Until My.Computer.FileSystem.FileExists(My.Settings.advancedInstallDirectory + "\SteamCMD\SteamCMD.exe")
            'Download SteamCMD if doesn't exist
            If Not My.Computer.FileSystem.FileExists("SteamCMD.zip") Then
                My.Computer.Network.DownloadFile("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip", "steamCMD.zip", True, 500)
            End If

            'Extract SteamCMD
            If My.Computer.FileSystem.FileExists("SteamCMD.zip") Then
                ZipFile.ExtractToDirectory(zipPath, extractPath)
            End If
        Loop

        'Clean up
        If My.Computer.FileSystem.FileExists("SteamCMD.zip") And My.Computer.FileSystem.FileExists(My.Settings.advancedInstallDirectory + "\SteamCMD\SteamCMD.exe") Then
            My.Computer.FileSystem.DeleteFile("SteamCMD.zip")
        End If

        parameters = "+login anonymous +quit"
        Process.Start(My.Settings.advancedInstallDirectory + "\SteamCMD\steamcmd.exe", parameters)
        MsgBox("SteamCMD is Installing, it will close itself and you can move on.", , "SteamCMD")
    End Sub

    'Save Config
    Private Sub btnSaveConfig_Click(sender As Object, e As EventArgs) Handles btnSaveConfig.Click
        My.Settings.Reload()

        SaveConfig()
        MsgBox("Config has been saved.", , "Config")
    End Sub

    'Setup Directories
    Public Sub ConfigDir(path As String)
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

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Application.Exit()
    End Sub

    Public Sub DefaultSetup()
        lblVersion.Text = "Version: " + versionNum

        My.Settings.currentServer = "Default"

        path = My.Settings.userSetDirectory
        ConfigDir(path)
        SaveConfig()
        serversINI.Save(serversFileName)
        Process.Start("ARK Dedicated Server Tool.exe")
        Me.Close()
    End Sub

    Public Sub AdvancedSetup()
        lblVersion.Text = "Version: " + versionNum
        MsgBox("Select your current Server Folder, or Create a new one.", , "Initial Setup")
        If setDirectoryDialog.ShowDialog() = DialogResult.OK Then
            My.Settings.userSetDirectoryBool = True
            My.Settings.userSetDirectory = setDirectoryDialog.SelectedPath & "\ShooterGame\Saved\Config\WindowsServer\"
            My.Settings.runningServerDirectory = setDirectoryDialog.SelectedPath & "\ShooterGame\Binaries\Win64\"
            My.Settings.advancedInstallDirectory = setDirectoryDialog.SelectedPath
            My.Settings.currentServer = "Initial Server"
            My.Settings.Save()

            path = My.Settings.userSetDirectory
            ConfigDir(path)
            SaveConfig()

            serversINI.AddSection("Initial Server").AddKey("Name").Value = "Initial Server"
            serversINI.AddSection("Initial Server").AddKey("Directory").Value = path

            serversINI.Save(serversFileName)
            Process.Start("ARK Dedicated Server Tool.exe")
            Me.Close()
        ElseIf DialogResult.Cancel Then
            MsgBox("You must select a directory.", , "Error")
            'Application.Exit()
        End If

        LoadConfig()
    End Sub

    'Save Config Function
    Public Sub SaveConfig()
        If My.Computer.FileSystem.FileExists(My.Settings.userSetDirectory + configFileName) Then
            configINI.Load(My.Settings.userSetDirectory + configFileName)
        Else
            'Nothing, carry on.
        End If

        'Write all Boolean Type Values
        configINI.AddSection("ServerSettings").AddKey("ShowMapPlayerLocation").Value = chkShowPlayerMarker.Checked
        configINI.AddSection("ServerSettings").AddKey("ServerHardcore").Value = chkServerHardcore.Checked
        configINI.AddSection("ServerSettings").AddKey("GlobalVoiceChat").Value = chkGlobalVoiceChat.Checked
        configINI.AddSection("ServerSettings").AddKey("ProximityChat").Value = chkProximityChat.Checked
        configINI.AddSection("ServerSettings").AddKey("NoTributeDownloads").Value = chkAllowTributeDownloads.Checked
        configINI.AddSection("ServerSettings").AddKey("AllowThirdPersonPlayer").Value = chkAllowThirdPersonPlayer.Checked
        configINI.AddSection("ServerSettings").AddKey("AlwaysNotifyPlayerLeft").Value = chkAlwaysNotifyPlayerJoined.Checked
        configINI.AddSection("ServerSettings").AddKey("DontAlwaysNotifyPlayerJoined").Value = chkDontAlwaysNotifyPlayerJoined.Checked
        configINI.AddSection("ServerSettings").AddKey("ServerPVE").Value = chkServerPVE.Checked
        configINI.AddSection("ServerSettings").AddKey("ServerCrosshair").Value = chkServerCrosshair.Checked
        configINI.AddSection("ServerSettings").AddKey("ServerForceNoHUD").Value = chkServerForceNoHUD.Checked

        'Write all String Type Values
        configINI.AddSection("MessageOfTheDay").AddKey("Message").Value = txtMessageOfTheDay.Text
        configINI.AddSection("ServerSettings").AddKey("ServerPassword").Value = txtServerPassword.Text
        configINI.AddSection("ServerSettings").AddKey("ServerAdminPassword").Value = txtAdminPassword.Text
        configINI.AddSection("SessionSettings").AddKey("SessionName").Value = txtServerName.Text
        configINI.AddSection("SessionSettings").AddKey("QueryPort").Value = txtServerPort.Text
        configINI.AddSection("ServerSettings").AddKey("DifficultyOffset").Value = txtDifficultyOffset.Text

        If chkMultiHome.Checked = True Then
            configINI.AddSection("SessionSettings").AddKey("MultiHome").Value = txtMultiHome.Text
        End If

        serversINI.AddSection("MultiHome").AddKey("MultiHome").Value = chkMultiHome.Checked

        'Write all Integer Type Values
        configINI.AddSection("/Script/Engine.GameSession").AddKey("MaxPlayers").Value = txtMaxPlayers.Text

        configINI.Save(My.Settings.userSetDirectory + configFileName)
        serversINI.Save(serversFileName)
    End Sub

    'Load Config Function
    Private Sub LoadConfig()
        configINI.Load(My.Settings.userSetDirectory + configFileName)
        serversINI.Load(serversFileName)

        chkShowPlayerMarker.Checked = configINI.GetSection("ServerSettings").GetKey("ShowMapPlayerLocation").GetValue()

        'Get & Set all Boolean Types
        chkShowPlayerMarker.Checked = configINI.GetSection("ServerSettings").GetKey("ShowMapPlayerLocation").GetValue()
        chkServerHardcore.Checked = configINI.GetSection("ServerSettings").GetKey("ServerHardcore").GetValue()
        chkGlobalVoiceChat.Checked = configINI.GetSection("ServerSettings").GetKey("GlobalVoiceChat").GetValue()
        chkProximityChat.Checked = configINI.GetSection("ServerSettings").GetKey("ProximityChat").GetValue()
        chkAllowTributeDownloads.Checked = configINI.GetSection("ServerSettings").GetKey("NoTributeDownloads").GetValue()
        chkAllowThirdPersonPlayer.Checked = configINI.GetSection("ServerSettings").GetKey("AllowThirdPersonPlayer").GetValue()
        chkAlwaysNotifyPlayerJoined.Checked = configINI.GetSection("ServerSettings").GetKey("AlwaysNotifyPlayerLeft").GetValue()
        chkDontAlwaysNotifyPlayerJoined.Checked = configINI.GetSection("ServerSettings").GetKey("DontAlwaysNotifyPlayerJoined").GetValue()
        chkServerPVE.Checked = configINI.GetSection("ServerSettings").GetKey("ServerPVE").GetValue()
        chkServerCrosshair.Checked = configINI.GetSection("ServerSettings").GetKey("ServerCrosshair").GetValue()
        chkServerForceNoHUD.Checked = configINI.GetSection("ServerSettings").GetKey("ServerForceNoHUD").GetValue()

        'Get & Set all String Types
        txtMessageOfTheDay.Text = configINI.GetSection("MessageOfTheDay").GetKey("Message").GetValue()
        txtServerPassword.Text = configINI.GetSection("ServerSettings").GetKey("ServerPassword").GetValue()
        txtAdminPassword.Text = configINI.GetSection("ServerSettings").GetKey("ServerAdminPassword").GetValue()
        txtServerName.Text = configINI.GetSection("SessionSettings").GetKey("SessionName").GetValue()
        txtServerPort.Text = configINI.GetSection("SessionSettings").GetKey("QueryPort").GetValue()
        txtDifficultyOffset.Text = configINI.GetSection("ServerSettings").GetKey("DifficultyOffset").GetValue()

        chkMultiHome.Checked = serversINI.GetSection("MultiHome").GetKey("MultiHome").GetValue()
        If chkMultiHome.Checked = True Then
            txtMultiHome.Text = configINI.GetSection("SessionSettings").GetKey("MultiHome").GetValue()
        End If

        'Get & Set all Integer Types
        txtMaxPlayers.Text = configINI.GetSection("/Script/Engine.GameSession").GetKey("MaxPlayers").GetValue()
    End Sub

    Private Sub lbServerSelect_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lbServerSelect.SelectedIndexChanged
        serversINI.Load(serversFileName)
        Dim curSelection As String = lbServerSelect.SelectedItem.ToString()
        Dim serverName As String
        Dim serverDirectory As String

        serverName = serversINI.GetSection(curSelection).GetKey(curSelection).GetValue
        serverDirectory = serversINI.GetSection(curSelection).GetKey("Directory").GetValue

        My.Settings.userSetDirectory = serverDirectory
        My.Settings.runningServerDirectory = serverDirectory
        My.Settings.Save()
        'Process.Start("ARK Dedicated Server Tool.exe")
        'Me.Close()

        MsgBox(curSelection)
    End Sub

    Private Sub btnSaveMultiServer_Click(sender As Object, e As EventArgs) Handles btnSaveMultiServer.Click
        Dim response = MsgBox("Are you sure you want to add the server named" & vbCrLf & vbCrLf & txtMultiServerName.Text, MsgBoxStyle.YesNo, "Multi Server")

        If response = MsgBoxResult.Yes Then
            If setDirectoryDialog.ShowDialog() = DialogResult.OK Then
                My.Settings.userSetDirectoryBool = True
                My.Settings.userSetDirectory = setDirectoryDialog.SelectedPath & "\ShooterGame\Saved\Config\WindowsServer\"
                My.Settings.Save()

                path = My.Settings.userSetDirectory
                ConfigDir(path)

                serversINI.AddSection(txtMultiServerName.Text).AddKey("Name").Value = txtMultiServerName.Text
                serversINI.AddSection(txtMultiServerName.Text).AddKey("Directory").Value = path
                serversINI.Save(serversFileName)
                lbServerSelect.Items.Add(txtMultiServerName.Text)
                My.Settings.currentServer = txtMultiServerName.Text

                txtMultiServerName.Text = ""
                MsgBox("Server Added Successfully", , "Multi Server")
                'Process.Start("ARK Dedicated Server Tool.exe")
                'Me.Close()
            ElseIf DialogResult.Cancel Then
                MsgBox("You must select a directory.", , "Error")
                'Application.Exit()
            End If
        ElseIf response = MsgBoxResult.No Then
            'Nothin
        End If
    End Sub

    Private Sub MultiServerPopulate()
        Dim ini As New IniFile
        ini.Load(serversFileName)

        For Each s As IniFile.IniSection In ini.Sections
            If Not s.Name = "MultiHome" Then
                lbServerSelect.Items.Add(s.Name)
            End If
        Next
    End Sub
End Class

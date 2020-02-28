﻿using QueryMaster;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Reflection;
using WPFSharp.Globalizer;
using ARK_Server_Manager.Lib.Utils;
using System.Net.Mail;
using ArkServerManager.Plugin.Common;

namespace ARK_Server_Manager.Lib
{
    internal class ServerApp
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private readonly PluginHelper _pluginHelper = PluginHelper.Instance;

        public enum ServerProcessType
        {
            Unknown = 0,
            AutoBackup,
            AutoUpdate,
            AutoShutdown1,
            AutoShutdown2,
            Backup,
            Shutdown,
            Restart,
        }

        public const int MUTEX_TIMEOUT = 5;         // 5 minutes
        public const int MUTEX_ATTEMPTDELAY = 5000; // 5 seconds
        private const int WRITELOG_ERRORRETRYDELAY = 2000; // 2 seconds
        private const int BACKUP_DELETEINTERVAL = 7; // 7 days

        private const int STEAM_MAXRETRIES = 10;
        private const int RCON_MAXRETRIES = 3;
        private const int FILECOPY_MAXRETRIES = 3;

        public const int EXITCODE_NORMALEXIT = 0;
        private const int EXITCODE_EXITWITHERRORS = 98;
        public const int EXITCODE_CANCELLED = 99;
        // generic codes
        private const int EXITCODE_UNKNOWNERROR = 991;
        private const int EXITCODE_UNKNOWNTHREADERROR = 992;
        private const int EXITCODE_BADPROFILE = 993;
        private const int EXITCODE_PROFILENOTFOUND = 994;
        private const int EXITCODE_BADARGUMENT = 995;

        private const int EXITCODE_AUTOUPDATENOTENABLED = 1001;
        private const int EXITCODE_AUTOSHUTDOWNNOTENABLED = 1002;
        private const int EXITCODE_AUTOBACKUPNOTENABLED = 1003;

        private const int EXITCODE_PROCESSALREADYRUNNING = 1011;
        private const int EXITCODE_INVALIDDATADIRECTORY = 1012;
        private const int EXITCODE_INVALIDCACHEDIRECTORY = 1013;
        private const int EXITCODE_CACHENOTFOUND = 1005;
        private const int EXITCODE_STEAMCMDNOTFOUND = 1006;
        // update cache codes
        private const int EXITCODE_CACHESERVERUPDATEFAILED = 2001;

        private const int EXITCODE_CACHEMODUPDATEFAILED = 2101;
        private const int EXITCODE_CACHEMODDETAILSDOWNLOADFAILED = 2102;
        // update file codes
        private const int EXITCODE_SERVERUPDATEFAILED = 3001;
        private const int EXITCODE_MODUPDATEFAILED = 3002;
        // shutdown codes
        private const int EXITCODE_SHUTDOWN_GETCMDLINEFAILED = 4001;
        private const int EXITCODE_SHUTDOWN_TIMEOUT = 4002;
        private const int EXITCODE_SHUTDOWN_BADENDPOINT = 4003;
        private const int EXITCODE_SHUTDOWN_SERVERNOTFOUND = 4004;
        // restart code
        private const int EXITCODE_RESTART_FAILED = 5001;
        private const int EXITCODE_RESTART_BADLAUNCHER = 5002;

        public const string LOGPREFIX_AUTOBACKUP = "#AutoBackupLogs";
        public const string LOGPREFIX_AUTOSHUTDOWN = "#AutoShutdownLogs";
        public const string LOGPREFIX_AUTOUPDATE = "#AutoUpdateLogs";

        private const int DIRECTORIES_PER_LINE = 200;

        private static readonly object LockObjectMessage = new object();
        private static readonly object LockObjectBranchMessage = new object();
        private static readonly object LockObjectProfileMessage = new object();
        private static DateTime _startTime = DateTime.Now;
        private static string _logPrefix = "";
        private static Dictionary<ServerProfileSnapshot, ServerProfile> _profiles = null;

        private ServerProfileSnapshot _profile = null;
        private Rcon _rconConsole = null;
        private bool _serverRunning = false;

        public bool BackupWorldFile = true;
        public bool DeleteOldServerBackupFiles = false;
        public int ExitCode = EXITCODE_NORMALEXIT;
        public bool OutputLogs = true;
        public bool SendAlerts = false;
        public bool SendEmails = false;
        public string ShutdownReason = null;
        public string UpdateReason = null;
        public ServerProcessType ServerProcess = ServerProcessType.Unknown;
        public int ShutdownInterval = Config.Default.ServerShutdown_GracePeriod;
        public ProgressDelegate ProgressCallback = null;
        public ProcessWindowStyle SteamCMDProcessWindowStyle = ProcessWindowStyle.Minimized;

        public ServerApp(bool resetStartTime = false)
        {
            if (resetStartTime)
                _startTime = DateTime.Now;
        }

        private void BackupServer()
        {
            if (_profile == null || _profile.SotFEnabled)
            {
                ExitCode = EXITCODE_BADPROFILE;
                return;
            }

            var emailMessage = new StringBuilder();
            var sent = false;

            LogProfileMessage("------------------------");
            LogProfileMessage("Started server backup...");
            LogProfileMessage("------------------------");
            LogProfileMessage($"Server Manager version: {App.Version}");

            emailMessage.AppendLine("Server Manager Backup Summary:");
            emailMessage.AppendLine();
            emailMessage.AppendLine($"Server Manager version: {App.Version}");

            // Find the server process.
            Process process = GetServerProcess();
            if (process != null)
            {
                _serverRunning = true;
                LogProfileMessage($"Server process found PID {process.Id}.");
            }

            if (_serverRunning)
            {
                // check if RCON is enabled
                if (_profile.RCONEnabled)
                {
                    try
                    {
                        try
                        {
                            emailMessage.AppendLine();

                            // perform a world save
                            if (!string.IsNullOrWhiteSpace(Config.Default.ServerBackup_WorldSaveMessage))
                            {
                                ProcessAlert(AlertType.Backup, Config.Default.ServerBackup_WorldSaveMessage);
                                sent = SendMessage(Config.Default.ServerBackup_WorldSaveMessage, CancellationToken.None);
                                if (sent)
                                {
                                    emailMessage.AppendLine("sent worldsave message.");
                                }
                            }

                            sent = SendCommand("saveworld", false);
                            if (sent)
                            {
                                emailMessage.AppendLine("sent saveworld command.");
                                Task.Delay(Config.Default.ServerShutdown_WorldSaveDelay * 1000).Wait();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"RCON> saveworld command.\r\n{ex.Message}");
                        }
                    }
                    finally
                    {
                        CloseRconConsole();
                    }
                }
                else
                {
                    LogProfileMessage("RCON not enabled.");
                }
            }

            if (ExitCode != EXITCODE_NORMALEXIT)
                return;

            // make a backup of the current profile and config files.
            CreateProfileBackupArchiveFile(_profile);

            if (ExitCode != EXITCODE_NORMALEXIT)
                return;

            // make a backup of the current world file.
            CreateServerBackupArchiveFile(emailMessage, _profile);

            if (ExitCode != EXITCODE_NORMALEXIT)
                return;

            if (Config.Default.EmailNotify_AutoBackup)
            {
                emailMessage.AppendLine();
                emailMessage.AppendLine("See attached log file more details.");
                SendEmail($"{_profile.ProfileName} auto backup finished", emailMessage.ToString(), true);
            }

            LogProfileMessage("-----------------------");
            LogProfileMessage("Finished server backup.");
            LogProfileMessage("-----------------------");

            ExitCode = EXITCODE_NORMALEXIT;
        }

        private void ShutdownServer(bool restartServer, bool updateServer, CancellationToken cancellationToken)
        {
            if (_profile == null)
            {
                ExitCode = EXITCODE_BADPROFILE;
                return;
            }

            if (restartServer)
            {
                LogProfileMessage("-------------------------");
                LogProfileMessage("Started server restart...");
                LogProfileMessage("-------------------------");
            }
            else
            {
                LogProfileMessage("--------------------------");
                LogProfileMessage("Started server shutdown...");
                LogProfileMessage("--------------------------");
            }
            LogProfileMessage($"Server Manager version: {App.Version}");

            // stop the server
            StopServer(cancellationToken);

            if (ExitCode != EXITCODE_NORMALEXIT)
                return;
            if (cancellationToken.IsCancellationRequested)
            {
                ExitCode = EXITCODE_CANCELLED;
                return;
            }

            // make a backup of the current profile and config files.
            CreateProfileBackupArchiveFile(_profile);

            if (ExitCode != EXITCODE_NORMALEXIT)
                return;

            if (BackupWorldFile)
            {
                // make a backup of the current world file.
                CreateServerBackupArchiveFile(null, _profile);

                if (ExitCode != EXITCODE_NORMALEXIT)
                    return;
            }

            if (updateServer)
            {
                UpgradeLocal(true, cancellationToken);
            }

            if (ExitCode != EXITCODE_NORMALEXIT)
                return;

            // check if this is a shutdown only, or a shutdown and restart.
            if (restartServer)
            {
                StartServer();

                if (ExitCode != EXITCODE_NORMALEXIT)
                    return;

                LogProfileMessage("------------------------");
                LogProfileMessage("Finished server restart.");
                LogProfileMessage("------------------------");
            }
            else
            {
                LogProfileMessage("-------------------------");
                LogProfileMessage("Finished server shutdown.");
                LogProfileMessage("-------------------------");
            }

            ExitCode = EXITCODE_NORMALEXIT;
        }

        private void StartServer()
        {
            if (_profile == null)
            {
                ExitCode = EXITCODE_BADPROFILE;
                return;
            }

            // check if the server was previously running before the update.
            if (!_serverRunning && !_profile.AutoRestartIfShutdown)
            {
                LogProfileMessage("Server was not running, server will not be started.");

                ExitCode = EXITCODE_NORMALEXIT;
                return;
            }
            if (!_serverRunning && _profile.AutoRestartIfShutdown)
            {
                LogProfileMessage("Server was not running, server will be started as the setting to restart if shutdown is TRUE.");
            }

            // Find the server process.
            Process process = GetServerProcess();

            if (process == null)
            {
                LogProfileMessage("");
                LogProfileMessage("Starting server...");

                CheckServerWorldFileExists(_profile);

                var startInfo = new ProcessStartInfo()
                {
                    FileName = GetLauncherFile(),
                    UseShellExecute = true,
                };

                process = Process.Start(startInfo);
                if (process == null)
                {
                    LogProfileError("Starting server failed.");
                    ExitCode = EXITCODE_RESTART_FAILED;
                    return;
                }

                LogProfileMessage("Started server successfully.");
                LogProfileMessage("");

                if (Config.Default.EmailNotify_ShutdownRestart)
                    SendEmail($"{_profile.ProfileName} server started", Config.Default.Alert_ServerStartedMessage, false);

                ProcessAlert(AlertType.Startup, Config.Default.Alert_ServerStartedMessage);
                if (_profile.ForceRespawnDinos)
                    ProcessAlert(AlertType.Startup, Config.Default.Alert_ForceRespawnDinos);
            }
            ExitCode = EXITCODE_NORMALEXIT;
        }

        private void StopServer(CancellationToken cancellationToken)
        {
            _serverRunning = false;

            if (_profile == null)
            {
                ExitCode = EXITCODE_BADPROFILE;
                return;
            }

            // Find the server process.
            Process process = GetServerProcess();

            // check if the process was found
            if (process == null)
            {
                LogProfileMessage("Server process not found, server not started.");

                // process not found, server is not running
                ExitCode = EXITCODE_NORMALEXIT;
                return;
            }

            _serverRunning = true;
            LogProfileMessage($"Server process found PID {process.Id}.");

            QueryMaster.Server gameServer = null;
            bool sent = false;

            try
            {
                // create a connection to the server
                var endPoint = new IPEndPoint(IPAddress.Parse(_profile.ServerIP), _profile.QueryPort);
                gameServer = ServerQuery.GetServerInstance(EngineType.Source, endPoint);

                // check if there is a shutdown reason
                if (!string.IsNullOrWhiteSpace(ShutdownReason) && !Config.Default.ServerShutdown_AllMessagesShowReason)
                {
                    LogProfileMessage("Sending shutdown reason...");

                    ProcessAlert(AlertType.ShutdownReason, ShutdownReason);
                    SendMessage(ShutdownReason, cancellationToken);
                }

                LogProfileMessage("Starting shutdown timer...");

                var minutesLeft = ShutdownInterval;
                while (minutesLeft > 0)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LogProfileMessage("Cancelling shutdown...");

                        if (!string.IsNullOrWhiteSpace(Config.Default.ServerShutdown_CancelMessage))
                        {
                            ProcessAlert(AlertType.Shutdown, Config.Default.ServerShutdown_CancelMessage);
                            SendMessage(Config.Default.ServerShutdown_CancelMessage, CancellationToken.None);
                        }

                        ExitCode = EXITCODE_CANCELLED;
                        return;
                    }

                    try
                    {
                        var playerInfo = gameServer?.GetPlayers()?.Where(p => !string.IsNullOrWhiteSpace(p.Name?.Trim())).ToList();

                        // check if anyone is logged into the server
                        var playerCount = playerInfo?.Count ?? -1;
                        if (playerCount <= 0)
                        {
                            LogProfileMessage("No online players, shutdown timer cancelled.");
                            break;
                        }

                        LogProfileMessage($"Online players: {playerCount}.");
                        if (playerInfo != null)
                        {
                            foreach (var player in playerInfo)
                            {
                                LogProfileMessage($"{player.Name}; joined {player.Time} ago");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error getting/displaying online players.\r\n{ex.Message}");
                    }

                    var message = string.Empty;
                    if (minutesLeft >= 5)
                    {
                        // check if the we have just started the countdown
                        if (minutesLeft == ShutdownInterval)
                        {
                            message = Config.Default.ServerShutdown_GraceMessage1.Replace("{minutes}", minutesLeft.ToString());
                            if (!string.IsNullOrWhiteSpace(UpdateReason))
                                message += $"\n\n{UpdateReason}";
                        }
                        else
                        {
                            int remainder;
                            Math.DivRem(minutesLeft, 5, out remainder);

                            if (remainder == 0)
                            {
                                message = Config.Default.ServerShutdown_GraceMessage1.Replace("{minutes}", minutesLeft.ToString());
                                if (!string.IsNullOrWhiteSpace(UpdateReason))
                                    message += $"\n\n{UpdateReason}";
                            }
                        }
                    }
                    else if (minutesLeft > 1)
                    {
                        message = Config.Default.ServerShutdown_GraceMessage1.Replace("{minutes}", minutesLeft.ToString());
                        if (!string.IsNullOrWhiteSpace(UpdateReason))
                            message += $"\n\n{UpdateReason}";
                    }
                    else
                    {
                        message = Config.Default.ServerShutdown_GraceMessage2;
                        if (!string.IsNullOrWhiteSpace(UpdateReason))
                            message += $"\n\n{UpdateReason}";
                    }

                    sent = false;
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        ProcessAlert(AlertType.ShutdownMessage, message);

                        // check if there is a shutdown reason
                        if (!string.IsNullOrWhiteSpace(ShutdownReason) && Config.Default.ServerShutdown_AllMessagesShowReason)
                        {
                            ProcessAlert(AlertType.ShutdownReason, ShutdownReason);

                            message = $"{message}\r\n{ShutdownReason}";
                        }

                        sent = SendMessage(message, cancellationToken);
                    }

                    minutesLeft--;
                    try
                    {
                        var delay = sent ? 60000 - Config.Default.SendMessageDelay : 60000;
                        Task.Delay(delay, cancellationToken).Wait(cancellationToken);
                    }
                    catch { }
                }

                // check if we need to perform a world save (not required for SotF servers)
                if (Config.Default.ServerShutdown_EnableWorldSave && !_profile.SotFEnabled)
                {
                    try
                    {
                        // perform a world save
                        if (!string.IsNullOrWhiteSpace(Config.Default.ServerShutdown_WorldSaveMessage))
                        {
                            LogProfileMessage(Config.Default.ServerShutdown_WorldSaveMessage);
                            ProcessAlert(AlertType.ShutdownMessage, Config.Default.ServerShutdown_WorldSaveMessage);
                            SendMessage(Config.Default.ServerShutdown_WorldSaveMessage, cancellationToken);
                        }

                        if (SendCommand("saveworld", false))
                        {
                            try
                            {
                                Task.Delay(Config.Default.ServerShutdown_WorldSaveDelay * 1000, cancellationToken).Wait(cancellationToken);
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"RCON> saveworld command.\r\n{ex.Message}");
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    LogProfileMessage("Cancelling shutdown...");

                    if (!string.IsNullOrWhiteSpace(Config.Default.ServerShutdown_CancelMessage))
                    {
                        ProcessAlert(AlertType.Shutdown, Config.Default.ServerShutdown_CancelMessage);
                        SendMessage(Config.Default.ServerShutdown_CancelMessage, CancellationToken.None);
                    }

                    ExitCode = EXITCODE_CANCELLED;
                    return;
                }

                // send the final shutdown message
                if (!string.IsNullOrWhiteSpace(Config.Default.ServerShutdown_GraceMessage3))
                {
                    var message = Config.Default.ServerShutdown_GraceMessage3;
                    ProcessAlert(AlertType.ShutdownMessage, message);

                    // check if there is a shutdown reason
                    if (!string.IsNullOrWhiteSpace(ShutdownReason) && Config.Default.ServerShutdown_AllMessagesShowReason)
                    {
                        ProcessAlert(AlertType.ShutdownReason, ShutdownReason);

                        message = $"{message}\r\n{ShutdownReason}";
                    }

                    SendMessage(message, cancellationToken);
                }
            }
            finally
            {
                CloseRconConsole();

                if (gameServer != null)
                {
                    gameServer.Dispose();
                    gameServer = null;
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                LogProfileMessage("Cancelling shutdown...");

                if (!string.IsNullOrWhiteSpace(Config.Default.ServerShutdown_CancelMessage))
                {
                    ProcessAlert(AlertType.Shutdown, Config.Default.ServerShutdown_CancelMessage);
                    SendMessage(Config.Default.ServerShutdown_CancelMessage, CancellationToken.None);
                }

                CloseRconConsole();

                ExitCode = EXITCODE_CANCELLED;
                return;
            }

            try
            {
                // Stop the server
                LogProfileMessage("");
                LogProfileMessage("Stopping server...");
                ProcessAlert(AlertType.Shutdown, Config.Default.Alert_ServerShutdownMessage);

                TaskCompletionSource<bool> ts = new TaskCompletionSource<bool>();
                EventHandler handler = (s, e) => ts.TrySetResult(true);
                process.EnableRaisingEvents = true;
                process.Exited += handler;

                // Method 1 - RCON Command
                if (_profile.RCONEnabled && Config.Default.ServerShutdown_UseDoExit)
                {
                    try
                    {
                        sent = SendCommand("doexit", false);
                        if (sent)
                        {
                            Task.Delay(10000).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"RCON> doexit command.\r\n{ex.Message}");
                    }

                    if (sent && !process.HasExited)
                    {
                        ts.Task.Wait(60000);   // 1 minute
                    }

                    if (process.HasExited)
                    {
                        LogProfileMessage($"Exited server successfully.");
                        LogProfileMessage("");
                        ExitCode = EXITCODE_NORMALEXIT;
                        return;
                    }

                    LogProfileMessage("Exiting server timed out, attempting to close the server.");
                }

                CloseRconConsole();

                // Method 2 - Close the process
                sent = process.CloseMainWindow();

                if (sent && !process.HasExited)
                {
                    ts.Task.Wait(60000);   // 1 minute
                }

                if (process.HasExited)
                {
                    LogProfileMessage("Closed server successfully.");
                    LogProfileMessage("");
                    ExitCode = EXITCODE_NORMALEXIT;
                    return;
                }

                // Attempt 3 - Send CNTL-C
                LogProfileMessage("Closing server timed out, attempting to stop the server.");

                ProcessUtils.SendStop(process).DoNotWait();

                if (!process.HasExited)
                {
                    ts.Task.Wait(60000);   // 1 minute
                }

                if (ts.Task.Result)
                {
                    LogProfileMessage("Stopped server successfully.");
                    LogProfileMessage("");
                    ExitCode = EXITCODE_NORMALEXIT;
                    return;
                }

                // Attempt 4 - Kill the process
                LogProfileMessage("Stopping server timed out, attempting to kill the server.");

                // try to kill the server
                process.Kill();

                if (!process.HasExited)
                {
                    ts.Task.Wait(60000);   // 1 minute
                }

                if (ts.Task.Result)
                {
                    LogProfileMessage("Killed server successfully.");
                    LogProfileMessage("");
                    ExitCode = EXITCODE_NORMALEXIT;
                    return;
                }
            }
            finally
            {
                if (process.HasExited)
                {
                    process.Close();

                    CheckServerWorldFileExists(_profile);

                    if (Config.Default.EmailNotify_ShutdownRestart)
                        SendEmail($"{_profile.ProfileName} server shutdown", $"The server has been shutdown to perform the {ServerProcess.ToString()} process.", false);
                }
            }

            // killing the server did not work, cancel the update
            LogProfileError("Killing server timed out.");
            ExitCode = EXITCODE_SHUTDOWN_TIMEOUT;
        }

        private void UpgradeLocal(bool validate, CancellationToken cancellationToken)
        {
            if (_profile == null)
            {
                ExitCode = EXITCODE_BADPROFILE;
                return;
            }

            try
            {
                var steamCmdFile = SteamCmdUpdater.GetSteamCmdFile();
                if (string.IsNullOrWhiteSpace(steamCmdFile) || !File.Exists(steamCmdFile))
                {
                    LogProfileError($"SteamCMD could not be found. Expected location is {steamCmdFile}");
                    ExitCode = EXITCODE_STEAMCMDNOTFOUND;
                    return;
                }

                // record the start time of the process, this is used to determine if any files changed in the download process.
                var startTime = DateTime.Now;

                var gotNewVersion = false;
                var downloadSuccessful = false;
                var success = false;

                // *********************
                // Server Update Section
                // *********************

                LogProfileMessage("\r\n");
                LogProfileMessage("Starting server update.");
                LogProfileMessage("Updating server from steam.\r\n");

                downloadSuccessful = !Config.Default.SteamCmdRedirectOutput;
                DataReceivedEventHandler serverOutputHandler = (s, e) =>
                {
                    var dataValue = e.Data ?? string.Empty;
                    LogProfileMessage(dataValue);
                    if (!gotNewVersion && dataValue.Contains("downloading,"))
                    {
                        gotNewVersion = true;
                    }
                    if (dataValue.StartsWith("Success!"))
                    {
                        downloadSuccessful = true;
                    }
                };

                var steamCmdInstallServerArgsFormat = _profile.SotFEnabled ? Config.Default.SteamCmdInstallServerArgsFormat_SotF : Config.Default.SteamCmdInstallServerArgsFormat;
                var steamCmdArgs = String.Format(steamCmdInstallServerArgsFormat, _profile.InstallDirectory, string.Empty, validate ? "validate" : string.Empty);

                success = ServerUpdater.UpgradeServerAsync(steamCmdFile, steamCmdArgs, _profile.InstallDirectory, Config.Default.SteamCmdRedirectOutput ? serverOutputHandler : null, cancellationToken, SteamCMDProcessWindowStyle).Result;
                if (success && downloadSuccessful)
                {
                    LogProfileMessage("Finished server update.");

                    if (Directory.Exists(_profile.InstallDirectory))
                    {
                        if (!Config.Default.SteamCmdRedirectOutput)
                            // check if any of the server files have changed.
                            gotNewVersion = HasNewServerVersion(_profile.InstallDirectory, startTime);

                        LogProfileMessage($"New server version - {gotNewVersion.ToString().ToUpperInvariant()}.");
                    }

                    LogProfileMessage("\r\n");
                }
                else
                {
                    success = false;
                    LogProfileMessage("****************************");
                    LogProfileMessage("ERROR: Failed server update.");
                    LogProfileMessage("****************************\r\n");

                    if (Config.Default.SteamCmdRedirectOutput)
                        LogProfileMessage($"If the server update keeps failing try disabling the '{_globalizer.GetResourceString("GlobalSettings_SteamCmdRedirectOutputLabel")}' option in the settings window.\r\n");

                    ExitCode = EXITCODE_SERVERUPDATEFAILED;
                }

                if (success)
                {
                    // ******************
                    // Mod Update Section
                    // ******************

                    // build a list of mods to be processed
                    var modIdList = new List<string>();
                    if (!string.IsNullOrWhiteSpace(_profile.ServerMapModId))
                        modIdList.Add(_profile.ServerMapModId);
                    if (!string.IsNullOrWhiteSpace(_profile.TotalConversionModId))
                        modIdList.Add(_profile.TotalConversionModId);
                    modIdList.AddRange(_profile.ServerModIds);

                    modIdList = ModUtils.ValidateModList(modIdList);

                    // get the details of the mods to be processed.
                    var modDetails = SteamUtils.GetSteamModDetails(modIdList);

                    // check if the mod details were retrieved
                    if (modDetails == null && Config.Default.ServerUpdate_ForceUpdateModsIfNoSteamInfo)
                    {
                        modDetails = new Model.PublishedFileDetailsResponse();
                    }

                    if (modDetails != null)
                    {
                        // create a new list for any failed mod updates
                        var failedMods = new List<string>(modIdList.Count);

                        for (var index = 0; index < modIdList.Count; index++)
                        {
                            var modId = modIdList[index];
                            var modTitle = modId;
                            var modSuccess = false;
                            gotNewVersion = false;
                            downloadSuccessful = false;

                            LogProfileMessage($"Started processing mod {index + 1} of {modIdList.Count}.");
                            LogProfileMessage($"Mod {modId}.");

                            // check if the steam information was downloaded
                            var modDetail = modDetails.publishedfiledetails?.FirstOrDefault(m => m.publishedfileid.Equals(modId, StringComparison.OrdinalIgnoreCase));
                            modTitle = $"{modId} - {modDetail?.title ?? "<unknown>"}";

                            if (modDetail != null)
                                LogProfileMessage($"{modDetail.title}.\r\n");

                            var modCachePath = ModUtils.GetModCachePath(modId, _profile.SotFEnabled);
                            var cacheTimeFile = ModUtils.GetLatestModCacheTimeFile(modId, _profile.SotFEnabled);
                            var modPath = ModUtils.GetModPath(_profile.InstallDirectory, modId);
                            var modTimeFile = ModUtils.GetLatestModTimeFile(_profile.InstallDirectory, modId);

                            var modCacheLastUpdated = 0;
                            var downloadMod = true;
                            var copyMod = true;
                            var updateError = false;

                            if (downloadMod)
                            {
                                // check if the mod needs to be downloaded, or force the download.
                                if (Config.Default.ServerUpdate_ForceUpdateMods)
                                {
                                    LogProfileMessage("Forcing mod download - ASM setting is TRUE.");
                                }
                                else if (modDetail == null)
                                {
                                    if (Config.Default.ServerUpdate_ForceUpdateModsIfNoSteamInfo)
                                    {
                                        LogProfileMessage("Forcing mod download - Mod details not available and ASM setting is TRUE.");
                                    }
                                    else
                                    {
                                        // no steam information downloaded, display an error, mod might no longer be available
                                        LogProfileMessage("*******************************************************************");
                                        LogProfileMessage("ERROR: Mod cannot be updated, unable to download steam information.");
                                        LogProfileMessage("*******************************************************************");

                                        LogProfileMessage($"If the mod update keeps failing try enabling the '{_globalizer.GetResourceString("GlobalSettings_ForceUpdateModsIfNoSteamInfoLabel")}' option in the settings window.\r\n");

                                        downloadMod = false;
                                        copyMod = false;
                                        updateError = true;
                                    }
                                }
                                else
                                {
                                    // check if the mod detail record is valid (private mod).
                                    if (modDetail.time_updated <= 0)
                                    {
                                        LogProfileMessage("Forcing mod download - mod is private.");
                                    }
                                    else
                                    {
                                        modCacheLastUpdated = ModUtils.GetModLatestTime(cacheTimeFile);
                                        if (modCacheLastUpdated <= 0)
                                        {
                                            LogProfileMessage("Forcing mod download - mod cache is not versioned.");
                                        }
                                        else
                                        {
                                            var steamLastUpdated = modDetail.time_updated;
                                            if (steamLastUpdated <= modCacheLastUpdated)
                                            {
                                                LogProfileMessage("Skipping mod download - mod cache has the latest version.");
                                                downloadMod = false;
                                            }
                                        }
                                    }
                                }

                                if (downloadMod)
                                {
                                    // mod will be downloaded
                                    downloadSuccessful = !Config.Default.SteamCmdRedirectOutput;
                                    DataReceivedEventHandler modOutputHandler = (s, e) =>
                                    {
                                        var dataValue = e.Data ?? string.Empty;
                                        LogProfileMessage(dataValue);
                                        if (dataValue.StartsWith("Success."))
                                        {
                                            downloadSuccessful = true;
                                        }
                                    };

                                    LogProfileMessage("Starting mod download.\r\n");

                                    steamCmdArgs = string.Empty;
                                    if (_profile.SotFEnabled)
                                    {
                                        if (Config.Default.SteamCmd_UseAnonymousCredentials)
                                            steamCmdArgs = string.Format(Config.Default.SteamCmdInstallModArgsFormat_SotF, Config.Default.SteamCmd_AnonymousUsername, modId);
                                        else
                                            steamCmdArgs = string.Format(Config.Default.SteamCmdInstallModArgsFormat_SotF, Config.Default.SteamCmd_Username, modId);
                                    }
                                    else
                                    {
                                        if (Config.Default.SteamCmd_UseAnonymousCredentials)
                                            steamCmdArgs = string.Format(Config.Default.SteamCmdInstallModArgsFormat, Config.Default.SteamCmd_AnonymousUsername, modId);
                                        else
                                            steamCmdArgs = string.Format(Config.Default.SteamCmdInstallModArgsFormat, Config.Default.SteamCmd_Username, modId);
                                    }

                                    modSuccess = ServerUpdater.UpgradeModsAsync(steamCmdFile, steamCmdArgs, Config.Default.SteamCmdRedirectOutput ? modOutputHandler : null, cancellationToken, SteamCMDProcessWindowStyle).Result;
                                    if (modSuccess && downloadSuccessful)
                                    {
                                        LogProfileMessage("Finished mod download.");
                                        copyMod = true;

                                        if (Directory.Exists(modCachePath))
                                        {
                                            // check if any of the mod files have changed.
                                            gotNewVersion = new DirectoryInfo(modCachePath).GetFiles("*.*", SearchOption.AllDirectories).Any(file => file.LastWriteTime >= startTime);

                                            LogProfileMessage($"New mod version - {gotNewVersion.ToString().ToUpperInvariant()}.");

                                            var steamLastUpdated = modDetail?.time_updated.ToString() ?? string.Empty;
                                            if (modDetail == null || modDetail.time_updated <= 0)
                                            {
                                                // get the version number from the steamcmd workshop file.
                                                steamLastUpdated = ModUtils.GetSteamWorkshopLatestTime(ModUtils.GetSteamWorkshopFile(_profile.SotFEnabled), modId).ToString();
                                            }

                                            // update the last updated file with the steam updated time.
                                            File.WriteAllText(cacheTimeFile, steamLastUpdated);

                                            LogProfileMessage($"Mod Cache version: {steamLastUpdated}\r\n");
                                        }
                                    }
                                    else
                                    {
                                        modSuccess = false;
                                        LogProfileMessage("***************************");
                                        LogProfileMessage("ERROR: Mod download failed.");
                                        LogProfileMessage("***************************\r\n");

                                        if (Config.Default.SteamCmdRedirectOutput)
                                            LogProfileMessage($"If the mod update keeps failing try disabling the '{_globalizer.GetResourceString("GlobalSettings_SteamCmdRedirectOutputLabel")}' option in the settings window.\r\n");
                                        copyMod = false;

                                        ExitCode = EXITCODE_MODUPDATEFAILED;
                                    }
                                }
                                else
                                    modSuccess = !updateError;
                            }
                            else
                                modSuccess = !updateError;

                            if (copyMod)
                            {
                                // check if the mod needs to be copied, or force the copy.
                                if (Config.Default.ServerUpdate_ForceCopyMods)
                                {
                                    LogProfileMessage("Forcing mod copy - ASM setting is TRUE.");
                                }
                                else
                                {
                                    // check the mod version against the cache version.
                                    var modLastUpdated = ModUtils.GetModLatestTime(modTimeFile);
                                    if (modLastUpdated <= 0)
                                    {
                                        LogProfileMessage("Forcing mod copy - mod is not versioned.");
                                    }
                                    else
                                    {
                                        modCacheLastUpdated = ModUtils.GetModLatestTime(cacheTimeFile);
                                        if (modCacheLastUpdated <= modLastUpdated)
                                        {
                                            LogProfileMessage("Skipping mod copy - mod has the latest version.");
                                            LogProfileMessage($"Mod version: {modLastUpdated}");
                                            copyMod = false;
                                        }
                                    }
                                }

                                if (copyMod)
                                {
                                    try
                                    {
                                        if (Directory.Exists(modCachePath))
                                        {
                                            LogProfileMessage("Started mod copy.");
                                            int count = 0;
                                            Task.Run(() => ModUtils.CopyMod(modCachePath, modPath, modId, (p, m, n) =>
                                            {
                                                count++;
                                                ProgressCallback?.Invoke(0, ".", count % DIRECTORIES_PER_LINE == 0);
                                            }), cancellationToken).Wait();
                                            LogProfileMessage("\r\n");
                                            LogProfileMessage("Finished mod copy.");

                                            var modLastUpdated = ModUtils.GetModLatestTime(modTimeFile);
                                            LogProfileMessage($"Mod version: {modLastUpdated}");
                                        }
                                        else
                                        {
                                            modSuccess = false;
                                            LogProfileMessage("****************************************************");
                                            LogProfileMessage("ERROR: Mod cache was not found, mod was not updated.");
                                            LogProfileMessage("****************************************************");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        modSuccess = false;
                                        LogProfileMessage("***********************");
                                        LogProfileMessage($"ERROR: Failed mod copy.\r\n{ex.Message}");
                                        LogProfileMessage("***********************");
                                    }
                                }
                            }

                            if (!modSuccess)
                            {
                                success = false;
                                failedMods.Add($"{index + 1} of {modIdList.Count} - {modTitle}");

                                ExitCode = EXITCODE_MODUPDATEFAILED;
                            }

                            LogProfileMessage($"Finished processing mod {modId}.\r\n");
                        }

                        if (failedMods.Count > 0)
                        {
                            LogProfileMessage("**************************************************************************");
                            LogProfileMessage("ERROR: The following mods failed the update, check above for more details.");
                            foreach (var failedMod in failedMods)
                                LogProfileMessage(failedMod);
                            LogProfileMessage("**************************************************************************");
                        }
                    }
                    else
                    {
                        success = false;
                        // no steam information downloaded, display an error
                        LogProfileMessage("********************************************************************");
                        LogProfileMessage("ERROR: Mods cannot be updated, unable to download steam information.");
                        LogProfileMessage("********************************************************************\r\n");

                        if (!Config.Default.ServerUpdate_ForceUpdateModsIfNoSteamInfo)
                            LogProfileMessage($"If the mod update keeps failing try enabling the '{_globalizer.GetResourceString("GlobalSettings_ForceUpdateModsIfNoSteamInfoLabel")}' option in the settings window.\r\n");

                        ExitCode = EXITCODE_MODUPDATEFAILED;
                    }
                }
                else
                {
                    LogProfileMessage("***********************************************************");
                    LogProfileMessage("ERROR: Mods were not processed as server update had errors.");
                    LogProfileMessage("***********************************************************\r\n");

                    ExitCode = EXITCODE_SERVERUPDATEFAILED;
                }

                LogProfileMessage("Finished upgrade process.");
            }
            catch (TaskCanceledException)
            {
                ExitCode = EXITCODE_CANCELLED;
            }
        }

        private void UpdateFiles()
        {
            if (_profile == null)
            {
                ExitCode = EXITCODE_BADPROFILE;
                return;
            }

            var alertMessage = new StringBuilder();
            var emailMessage = new StringBuilder();

            LogProfileMessage("------------------------");
            LogProfileMessage("Started server update...");
            LogProfileMessage("------------------------");
            LogProfileMessage($"Server Manager version: {App.Version}");
            LogProfileMessage($"Server branch: {GetBranchName(_profile.BranchName)}");

            // check if the server needs to be updated
            var serverCacheLastUpdated = GetServerLatestTime(GetServerCacheTimeFile(_profile?.BranchName));
            var serverLastUpdated = GetServerLatestTime(GetServerTimeFile());
            var updateServer = serverCacheLastUpdated > serverLastUpdated;

            // check if any of the mods need to be updated
            var updateModIds = new List<string>();
            var modIdList = GetModList();

            // cycle through each mod.
            foreach (var modId in modIdList)
            {
                // check if the mod needs to be updated.
                var modCacheLastUpdated = ModUtils.GetModLatestTime(ModUtils.GetLatestModCacheTimeFile(modId, false));
                var modLastUpdated = ModUtils.GetModLatestTime(ModUtils.GetLatestModTimeFile(_profile.InstallDirectory, modId));
                if (modCacheLastUpdated > modLastUpdated || modLastUpdated == 0)
                    updateModIds.Add(modId);
            }

            if (ExitCode != EXITCODE_NORMALEXIT)
                return;

            if (updateServer || updateModIds.Count > 0)
            {
                var modDetails = SteamUtils.GetSteamModDetails(updateModIds);

                UpdateReason = string.Empty;
                if (Config.Default.AutoUpdate_ShowUpdateReason)
                {
                    var delimiter = string.Empty;

                    // create the update message to broadcast 
                    if (!string.IsNullOrWhiteSpace(Config.Default.AutoUpdate_UpdateReasonPrefix))
                    {
                        UpdateReason += $"{Config.Default.AutoUpdate_UpdateReasonPrefix.Trim()}";
                        delimiter = " ";
                    }

                    if (updateServer)
                    {
                        UpdateReason += $"{delimiter}Ark Server";
                        delimiter = ", ";
                    }
                    if (updateModIds.Count > 0)
                    {
                        for (var index = 0; index < updateModIds.Count; index++)
                        {
                            if (index == 5)
                            {
                                if (updateModIds.Count - index == 1)
                                    UpdateReason += $" and 1 other mod...";
                                else
                                    UpdateReason += $" and {updateModIds.Count - index} other mods...";
                                break;
                            }

                            var modId = updateModIds[index];
                            var modName = modDetails?.publishedfiledetails?.FirstOrDefault(m => m.publishedfileid == modId)?.title ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(modName))
                                UpdateReason += $"{delimiter}{modId}";
                            else
                                UpdateReason += $"{delimiter}{modName}";
                            delimiter = ", ";
                        }
                    }
                }

                // stop the server
                StopServer(CancellationToken.None);

                if (ExitCode != EXITCODE_NORMALEXIT)
                    return;

                emailMessage.AppendLine("Update Summary:");
                emailMessage.AppendLine();
                emailMessage.AppendLine($"Server Manager version: {App.Version}");

                // make a backup of the current profile and config files.
                CreateProfileBackupArchiveFile(_profile);

                if (ExitCode != EXITCODE_NORMALEXIT)
                    return;

                if (BackupWorldFile)
                {
                    // make a backup of the current world file.
                    CreateServerBackupArchiveFile(emailMessage, _profile);

                    if (ExitCode != EXITCODE_NORMALEXIT)
                        return;
                }

                Mutex mutex = null;
                bool createdNew = false;

                alertMessage.AppendLine();
                if (!string.IsNullOrWhiteSpace(Config.Default.Alert_UpdateResults))
                    alertMessage.AppendLine(Config.Default.Alert_UpdateResults);

                // check if the server needs to be updated
                if (updateServer)
                {
                    Task.Delay(5000).Wait();

                    LogProfileMessage("Updating server from cache...");

                    emailMessage.AppendLine();
                    emailMessage.AppendLine("Game Server Update:");

                    try
                    {
                        var cacheFolder = GetServerCacheFolder(_profile?.BranchName);

                        if (Directory.Exists(cacheFolder))
                        {
                            LogProfileMessage($"Smart cache copy: {Config.Default.AutoUpdate_UseSmartCopy}.");

                            // update the server files from the cache.
                            DirectoryCopy(cacheFolder, _profile.InstallDirectory, true, Config.Default.AutoUpdate_UseSmartCopy, null);

                            LogProfileMessage("Updated server from cache. See ARK patch notes.");
                            LogProfileMessage(Config.Default.ArkSE_PatchNotesUrl);

                            if (!string.IsNullOrWhiteSpace(Config.Default.Alert_ServerUpdate))
                                alertMessage.AppendLine(Config.Default.Alert_ServerUpdate);

                            emailMessage.AppendLine();
                            emailMessage.AppendLine("Updated server from cache. See ARK patch notes.");
                            emailMessage.AppendLine(Config.Default.ArkSE_PatchNotesUrl);

                            _profile.ServerUpdated = true;
                        }
                        else
                        {
                            LogProfileMessage("Server cache was not found, server was not updated from cache.");
                            ExitCode = EXITCODE_SERVERUPDATEFAILED;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogProfileError($"Unable to update the server from cache.\r\n{ex.Message}");
                        ExitCode = EXITCODE_SERVERUPDATEFAILED;
                    }
                }
                else
                {
                    LogProfileMessage("Server is already up to date, no update required.");
                }

                if (ExitCode != EXITCODE_NORMALEXIT)
                    return;

                // check if the mods need to be updated
                if (updateModIds.Count > 0)
                {
                    Task.Delay(5000).Wait();

                    LogProfileMessage($"Updating {updateModIds.Count} mods from cache...");

                    emailMessage.AppendLine();
                    emailMessage.AppendLine("Mod Updates:");

                    try
                    {
                        // update the mod files from the cache.
                        for (var index = 0; index < updateModIds.Count; index++)
                        {
                            var modId = updateModIds[index];
                            var modCachePath = ModUtils.GetModCachePath(modId, false);
                            var modPath = GetModPath(modId);
                            var modName = modDetails?.publishedfiledetails?.FirstOrDefault(m => m.publishedfileid == modId)?.title ?? string.Empty;

                            try
                            {
                                if (Directory.Exists(modCachePath))
                                {
                                    // try to establish a mutex for the mod cache.
                                    mutex = new Mutex(true, GetMutexName(modCachePath), out createdNew);
                                    if (!createdNew)
                                        createdNew = mutex.WaitOne(new TimeSpan(0, MUTEX_TIMEOUT, 0));

                                    // check if the mutex was established
                                    if (createdNew)
                                    {
                                        LogProfileMessage($"Started mod update from cache {index + 1} of {updateModIds.Count}...");
                                        LogProfileMessage($"Mod Name: {modName} (Mod ID: {modId})");

                                        alertMessage.AppendLine($"{modName} ({modId})");

                                        emailMessage.AppendLine();
                                        emailMessage.AppendLine($"{modName} ({modId})");

                                        ModUtils.CopyMod(modCachePath, modPath, modId, null);

                                        var modLastUpdated = ModUtils.GetModLatestTime(ModUtils.GetLatestModTimeFile(_profile.InstallDirectory, modId));
                                        LogProfileMessage($"Mod {modId} version: {modLastUpdated}.");

                                        LogProfileMessage($"Workshop page: http://steamcommunity.com/sharedfiles/filedetails/?id={modId}");
                                        LogProfileMessage($"Change notes: http://steamcommunity.com/sharedfiles/filedetails/changelog/{modId}");

                                        emailMessage.AppendLine($"Workshop page: http://steamcommunity.com/sharedfiles/filedetails/?id={modId}");
                                        emailMessage.AppendLine($"Change notes: http://steamcommunity.com/sharedfiles/filedetails/changelog/{modId}");

                                        LogProfileMessage($"Finished mod {modId} update from cache.");
                                    }
                                    else
                                    {
                                        ExitCode = EXITCODE_PROCESSALREADYRUNNING;
                                        LogProfileMessage("Mod not updated, could not lock mod cache.");
                                    }
                                }
                                else
                                {
                                    LogProfileError($"Mod {modId} cache was not found, mod was not updated from cache.");
                                    ExitCode = EXITCODE_MODUPDATEFAILED;
                                }
                            }
                            catch (Exception ex)
                            {
                                LogProfileError($"Unable to update mod {modId} from cache.\r\n{ex.Message}");
                                ExitCode = EXITCODE_MODUPDATEFAILED;
                            }
                            finally
                            {
                                if (mutex != null)
                                {
                                    if (createdNew)
                                    {
                                        mutex.ReleaseMutex();
                                        mutex.Dispose();
                                    }
                                    mutex = null;
                                }
                            }
                        }

                        if (ExitCode == EXITCODE_NORMALEXIT)
                            LogProfileMessage($"Updated {updateModIds.Count} mods from cache.");
                        else
                            LogProfileMessage($"Updated {updateModIds.Count} mods from cache BUT there were errors.");
                    }
                    catch (Exception ex)
                    {
                        LogProfileError($"Unable to update the mods from cache.\r\n{ex.Message}");
                        ExitCode = EXITCODE_MODUPDATEFAILED;
                    }
                }
                else
                {
                    LogProfileMessage("Mods are already up to date, no updates required.");
                }

                if (ExitCode != EXITCODE_NORMALEXIT)
                    return;

                if (Config.Default.AutoUpdate_OverrideServerStartup)
                {
                    if (_serverRunning)
                        LogProfileMessage("The auto-update override server startup option is enabled, server will not be restarted.");
                    else
                        LogProfileMessage("The auto-update override server startup option is enabled, server will not be started.");
                }
                else
                {
                    // restart the server
                    StartServer();
                }

                if (Config.Default.EmailNotify_AutoUpdate)
                {
                    emailMessage.AppendLine();
                    emailMessage.AppendLine("See attached log file more details.");
                    SendEmail($"{_profile.ProfileName} auto update finished", emailMessage.ToString(), true);
                }

                ProcessAlert(AlertType.UpdateResults, alertMessage.ToString());
            }
            else
            {
                if (updateModIds.Count > 0)
                    LogProfileMessage("The server and mods files are already up to date, no updates required.");
                else
                    LogProfileMessage("The server files are already up to date, no updates required.");

                _serverRunning = GetServerProcess() != null;

                if (Config.Default.AutoUpdate_OverrideServerStartup)
                {
                    if (!_serverRunning)
                        LogProfileMessage("The auto-update override server startup option is enabled, server will not be started.");
                }
                else
                {
                    // restart the server
                    StartServer();
                }
            }

            if (ExitCode != EXITCODE_NORMALEXIT)
                return;

            LogProfileMessage("-----------------------");
            LogProfileMessage("Finished server update.");
            LogProfileMessage("-----------------------");

            ExitCode = EXITCODE_NORMALEXIT;
        }

        private void UpdateModCache()
        {
            // get a list of mods to be processed
            var modIdList = GetModList();

            // check if there are any mods to be processed
            if (modIdList.Count == 0)
            {
                ExitCode = EXITCODE_NORMALEXIT;
                return;
            }

            LogMessage("");
            LogMessage("----------------------------");
            LogMessage("Starting mod cache update...");
            LogMessage("----------------------------");
            LogMessage($"Server Manager version: {App.Version}");

            LogMessage($"Downloading mod information for {modIdList.Count} mods from steam.");

            // get the details of the mods to be processed.
            var modDetails = SteamUtils.GetSteamModDetails(modIdList);
            if (modDetails == null)
            {
                if (!Config.Default.ServerUpdate_ForceUpdateModsIfNoSteamInfo)
                {
                    LogError("Mods cannot be updated, unable to download steam information.");
                    LogMessage($"If the mod update keeps failing try enabling the '{_globalizer.GetResourceString("GlobalSettings_ForceUpdateModsIfNoSteamInfoLabel")}' option in the settings window.");
                    ExitCode = EXITCODE_CACHEMODDETAILSDOWNLOADFAILED;
                    return;
                }
            }

            LogMessage($"Downloaded mod information for {modIdList.Count} mods from steam.");
            LogMessage("");

            // cycle through each mod finding which needs to be updated.
            var updateModIds = new List<string>();
            if (modDetails == null)
            {
                if (Config.Default.ServerUpdate_ForceUpdateModsIfNoSteamInfo)
                {
                    LogMessage("All mods will be updated - unable to download steam information and force mod update is TRUE.");

                    updateModIds.AddRange(modIdList);
                    modDetails = new Model.PublishedFileDetailsResponse();
                }
            }
            else
            {
                if (Config.Default.ServerUpdate_ForceUpdateMods)
                {
                    LogMessage("All mods will be updated - force mod update is TRUE.");
                    updateModIds.AddRange(modIdList);
                }
                else
                {
                    LogMessage("Mods will be selectively updated - force mod update is FALSE.");

                    foreach (var modId in modIdList)
                    {
                        var modDetail = modDetails.publishedfiledetails?.FirstOrDefault(m => m.publishedfileid.Equals(modId, StringComparison.OrdinalIgnoreCase));
                        if (modDetail == null)
                        {
                            LogMessage($"Mod {modId} will not be updated - unable to download steam information.");
                            continue;
                        }

                        if (modDetail.time_updated == 0)
                        {
                            LogMessage($"Mod {modId} will be updated - mod is private.");
                            updateModIds.Add(modId);
                        }
                        else
                        {
                            var cacheTimeFile = ModUtils.GetLatestModCacheTimeFile(modId, false);

                            // check if the mod needs to be updated
                            var steamLastUpdated = modDetail.time_updated;
                            var modCacheLastUpdated = ModUtils.GetModLatestTime(cacheTimeFile);
                            if (steamLastUpdated > modCacheLastUpdated)
                            {
                                LogMessage($"Mod {modId} will be updated - new version found.");
                                updateModIds.Add(modId);
                            }
                            else if (modCacheLastUpdated == 0)
                            {
                                LogMessage($"Mod {modId} will be updated - cache not versioned.");
                                updateModIds.Add(modId);
                            }
                            else
                            {
                                LogMessage($"Mod {modId} update skipped - cache contains the latest version.");
                            }
                        }
                    }
                }
            }

            var steamCmdFile = SteamCmdUpdater.GetSteamCmdFile();
            if (string.IsNullOrWhiteSpace(steamCmdFile) || !File.Exists(steamCmdFile))
            {
                LogError($"SteamCMD could not be found. Expected location is {steamCmdFile}");
                ExitCode = EXITCODE_STEAMCMDNOTFOUND;
                return;
            }

            // cycle through each mod id.
            for (var index = 0; index < updateModIds.Count; index++)
            {
                var modId = updateModIds[index];
                var modDetail = modDetails.publishedfiledetails?.FirstOrDefault(m => m.publishedfileid.Equals(modId, StringComparison.OrdinalIgnoreCase));

                var cacheTimeFile = ModUtils.GetLatestModCacheTimeFile(modId, false);
                var modCachePath = ModUtils.GetModCachePath(modId, false);

                var downloadSuccessful = false;

                DataReceivedEventHandler modOutputHandler = (s, e) =>
                {
                    var dataValue = e.Data ?? string.Empty;
                    LogMessage(dataValue);
                    if (dataValue.StartsWith("Success."))
                    {
                        downloadSuccessful = true;
                    }
                };

                LogMessage("");
                LogMessage($"Started mod cache update {index + 1} of {updateModIds.Count}");
                LogMessage($"{modId} - {modDetail?.title ?? "<unknown>"}");

                var attempt = 0;
                while (true)
                {
                    attempt++;
                    downloadSuccessful = !Config.Default.SteamCmdRedirectOutput;

                    // update the mod cache
                    var steamCmdArgs = string.Empty;
                    if (Config.Default.SteamCmd_UseAnonymousCredentials)
                        steamCmdArgs = string.Format(Config.Default.SteamCmdInstallModArgsFormat, Config.Default.SteamCmd_AnonymousUsername, modId);
                    else
                        steamCmdArgs = string.Format(Config.Default.SteamCmdInstallModArgsFormat, Config.Default.SteamCmd_Username, modId);
                    var success = ServerUpdater.UpgradeModsAsync(steamCmdFile, steamCmdArgs, Config.Default.SteamCmdRedirectOutput ? modOutputHandler : null, CancellationToken.None, SteamCMDProcessWindowStyle).Result;
                    if (success && downloadSuccessful)
                        // download was successful, exit loop and continue.
                        break;

                    // download was not successful, log a failed attempt.
                    var logError = $"Mod {modId} cache update failed";
                    if (Config.Default.AutoUpdate_RetryOnFail)
                        logError += $" - attempt {attempt}.";
                    LogError(logError);

                    // check if we have reached the max failed attempt limit.
                    if (!Config.Default.AutoUpdate_RetryOnFail || attempt >= STEAM_MAXRETRIES)
                    {
                        // failed max limit reached
                        if (Config.Default.SteamCmdRedirectOutput)
                            LogMessage($"If the mod cache update keeps failing try disabling the '{_globalizer.GetResourceString("GlobalSettings_SteamCmdRedirectOutputLabel")}' option in the ASM settings window.");

                        ExitCode = EXITCODE_CACHEMODUPDATEFAILED;
                        return;
                    }

                    Task.Delay(5000).Wait();
                }

                // check if any of the mod files have changed.
                if (Directory.Exists(modCachePath))
                {
                    var gotNewVersion = new DirectoryInfo(modCachePath).GetFiles("*.*", SearchOption.AllDirectories).Any(file => file.LastWriteTime >= _startTime);

                    if (gotNewVersion)
                        LogMessage("***** New version downloaded. *****");
                    else
                        LogMessage("No new version.");

                    var steamLastUpdated = modDetail?.time_updated.ToString() ?? string.Empty;
                    if (modDetail == null || modDetail.time_updated <= 0)
                    {
                        // get the version number from the steamcmd workshop file.
                        steamLastUpdated = ModUtils.GetSteamWorkshopLatestTime(ModUtils.GetSteamWorkshopFile(false), modId).ToString();
                    }

                    File.WriteAllText(cacheTimeFile, steamLastUpdated);
                    LogMessage($"Mod {modId} cache version: {steamLastUpdated}");
                }
                else
                    LogMessage($"Mod {modId} cache does not exist.");

                LogMessage($"Finished mod {modId} cache update.");
            }

            LogMessage("---------------------------");
            LogMessage("Finished mod cache update.");
            LogMessage("---------------------------");
            LogMessage("");
            ExitCode = EXITCODE_NORMALEXIT;
        }

        private void UpdateServerCache(string branchName, string branchPassword)
        {
            LogBranchMessage(branchName, "-------------------------------");
            LogBranchMessage(branchName, "Starting server cache update...");
            LogBranchMessage(branchName, "-------------------------------");
            LogBranchMessage(branchName, $"Server Manager version: {App.Version}");
            LogBranchMessage(branchName, $"Server branch: {GetBranchName(branchName)}");

            var gotNewVersion = false;
            var downloadSuccessful = false;

            var steamCmdFile = SteamCmdUpdater.GetSteamCmdFile();
            if (string.IsNullOrWhiteSpace(steamCmdFile) || !File.Exists(steamCmdFile))
            {
                LogBranchError(branchName, $"SteamCMD could not be found. Expected location is {steamCmdFile}");
                ExitCode = EXITCODE_STEAMCMDNOTFOUND;
                return;
            }

            DataReceivedEventHandler serverOutputHandler = (s, e) =>
            {
                var dataValue = e.Data ?? string.Empty;
                LogBranchMessage(branchName, dataValue);
                if (!gotNewVersion && dataValue.Contains("downloading,"))
                {
                    gotNewVersion = true;
                }
                if (dataValue.StartsWith("Success!"))
                {
                    downloadSuccessful = true;
                }
            };

            // create the branch arguments
            var steamCmdInstallServerBetaArgs = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(branchName))
            {
                steamCmdInstallServerBetaArgs.AppendFormat(Config.Default.SteamCmdInstallServerBetaNameArgsFormat, branchName);
                if (!string.IsNullOrWhiteSpace(branchPassword))
                {
                    steamCmdInstallServerBetaArgs.Append(" ");
                    steamCmdInstallServerBetaArgs.AppendFormat(Config.Default.SteamCmdInstallServerBetaPasswordArgsFormat, branchPassword);
                }
            }

            var cacheFolder = GetServerCacheFolder(branchName);

            LogBranchMessage(branchName, "Server update started.");

            var attempt = 0;
            while (true)
            {
                attempt++;
                downloadSuccessful = !Config.Default.SteamCmdRedirectOutput;
                gotNewVersion = false;

                // update the server cache
                var validate = Config.Default.AutoUpdate_ValidateServerFiles;
                var steamCmdInstallServerArgsFormat = Config.Default.SteamCmdInstallServerArgsFormat;
                var steamCmdArgs = String.Format(steamCmdInstallServerArgsFormat, cacheFolder, steamCmdInstallServerBetaArgs, validate ? "validate" : string.Empty);

                var success = ServerUpdater.UpgradeServerAsync(steamCmdFile, steamCmdArgs, cacheFolder, Config.Default.SteamCmdRedirectOutput ? serverOutputHandler : null, CancellationToken.None, SteamCMDProcessWindowStyle).Result;
                if (success && downloadSuccessful)
                    // download was successful, exit loop and continue.
                    break;

                // download was not successful, log a failed attempt.
                var logError = "Server cache update failed";
                if (Config.Default.AutoUpdate_RetryOnFail)
                    logError += $" - attempt {attempt}.";
                LogBranchError(branchName, logError);

                // check if we have reached the max failed attempt limit.
                if (!Config.Default.AutoUpdate_RetryOnFail || attempt >= STEAM_MAXRETRIES)
                {
                    // failed max limit reached
                    if (Config.Default.SteamCmdRedirectOutput)
                        LogBranchMessage(branchName, $"If the server cache update keeps failing try disabling the '{_globalizer.GetResourceString("GlobalSettings_SteamCmdRedirectOutputLabel")}' option in the ASM settings window.");

                    ExitCode = EXITCODE_CACHESERVERUPDATEFAILED;
                    return;
                }

                Task.Delay(5000).Wait();
            }

            if (Directory.Exists(cacheFolder))
            {
                if (!Config.Default.SteamCmdRedirectOutput)
                    // check if any of the server files have changed.
                    gotNewVersion = HasNewServerVersion(cacheFolder, _startTime);

                if (gotNewVersion)
                {
                    LogBranchMessage(branchName, "***** New version downloaded. *****");

                    var latestCacheTimeFile = GetServerCacheTimeFile(branchName);
                    File.WriteAllText(latestCacheTimeFile, _startTime.ToString("o", CultureInfo.CurrentCulture));
                }
                else
                    LogBranchMessage(branchName, "No new version.");
            }
            else
                LogBranchMessage(branchName, $"Server cache does not exist.");

            LogBranchMessage(branchName, "-----------------------------");
            LogBranchMessage(branchName, "Finished server cache update.");
            LogBranchMessage(branchName, "-----------------------------");
            LogBranchMessage(branchName, "");
            ExitCode = EXITCODE_NORMALEXIT;
        }

        private void CloseRconConsole()
        {
            if (_rconConsole != null)
            {
                _rconConsole.Dispose();
                _rconConsole = null;

                Task.Delay(1000).Wait();
            }
        }

        public void CheckServerWorldFileExists(ServerProfileSnapshot profile = null)
        {
            // do nothing if profile is null or SotF
            if (profile == null || profile.SotFEnabled)
                return;

            var oldProfile = _profile;

            try
            {
                _profile = profile;

                // check if the server save folder exists
                var saveFolder = GetServerSaveFolder();
                if (!Directory.Exists(saveFolder))
                    // save folder does not exist, exit
                    return;

                // check if the server save file exists
                var worldFile = GetServerWorldFile();
                if (File.Exists(worldFile))
                    // save file exists, exit
                    return;

                // save file does not exist, check for temp save file
                var tempWorldFile = Path.ChangeExtension(worldFile, ".tmp");
                if (!File.Exists(tempWorldFile))
                    // temp save file does not exist, exit
                    return;

                // temp save file exists, rename temp file to server save file
                File.Move(tempWorldFile, worldFile);
                LogProfileMessage("Server save file restored from temporary file (.tmp).");
            }
            finally
            {
                _profile = oldProfile;
            }
        }

        public void CreateProfileBackupArchiveFile(ServerProfileSnapshot profile = null)
        {
            // do nothing if profile is null
            if (profile == null)
                return;

            var oldProfile = _profile;

            try
            {
                _profile = profile;

                // create the backup file.
                try
                {
                    LogProfileMessage("Back up profile and config files started...");

                    var backupFolder = GetProfileBackupFolder(_profile.ProfileName);
                    var backupFileName = $"{_startTime.ToString("yyyyMMdd_HHmmss")}{Config.Default.BackupExtension}";
                    var backupFile = IOUtils.NormalizePath(Path.Combine(backupFolder, backupFileName));

                    var profileFile = IOUtils.NormalizePath(Path.Combine(Config.Default.ConfigDirectory, $"{_profile.ProfileName}{Config.Default.ProfileExtension}"));
                    var gameIniFile = IOUtils.NormalizePath(Path.Combine(_profile.InstallDirectory, Config.Default.ServerConfigRelativePath, Config.Default.ServerGameConfigFile));
                    var gusIniFile = IOUtils.NormalizePath(Path.Combine(_profile.InstallDirectory, Config.Default.ServerConfigRelativePath, Config.Default.ServerGameUserSettingsConfigFile));
                    var launcherFile = GetLauncherFile();

                    if (!Directory.Exists(backupFolder))
                        Directory.CreateDirectory(backupFolder);

                    if (File.Exists(backupFile))
                        File.Delete(backupFile);

                    var files = new List<string>();
                    if (File.Exists(profileFile))
                        files.Add(profileFile);

                    if (File.Exists(gameIniFile))
                        files.Add(gameIniFile);

                    if (File.Exists(gusIniFile))
                        files.Add(gusIniFile);

                    if (File.Exists(launcherFile))
                        files.Add(launcherFile);

                    var comment = new StringBuilder();
                    comment.AppendLine($"Windows Platform: {Environment.OSVersion.Platform}");
                    comment.AppendLine($"Windows Version: {Environment.OSVersion.VersionString}");
                    comment.AppendLine($"ASM Version: {App.Version}");
                    comment.AppendLine($"Config Directory: {Config.Default.ConfigDirectory}");
                    comment.AppendLine($"Server Directory: {_profile.InstallDirectory}");
                    comment.AppendLine($"Profile Name: {_profile.ProfileName}");
                    comment.AppendLine($"SotF Server: {_profile.SotFEnabled}");
                    comment.AppendLine($"PGM Server: {_profile.PGM_Enabled}");
                    comment.AppendLine($"Process: {ServerProcess}");

                    ZipUtils.ZipFiles(backupFile, files.ToArray(), comment.ToString(), false);

                    LogProfileMessage($"Backup file created - {backupFile}");
                }
                catch (Exception ex)
                {
                    LogProfileError($"Error backing up profile and config files.\r\n{ex.Message}", false);
                }
                finally
                {
                    LogProfileMessage("Back up profile and config files finished.");
                }

                // delete the old backup files
                try
                {
                    LogProfileMessage("Delete old profile backup files started...");

                    var backupFolder = GetProfileBackupFolder(_profile.ProfileName);
                    var backupFileFilter = $"*{Config.Default.BackupExtension}";
                    var backupDateFilter = DateTime.Now.AddDays(-BACKUP_DELETEINTERVAL);

                    var backupFiles = new DirectoryInfo(backupFolder).GetFiles(backupFileFilter).Where(f => f.LastWriteTime < backupDateFilter);
                    foreach (var backupFile in backupFiles)
                    {
                        try
                        {
                            LogProfileMessage($"{backupFile.Name} was deleted, last updated {backupFile.CreationTime.ToString()}.");
                            backupFile.Delete();
                        }
                        catch
                        {
                            // if unable to delete, do not bother
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogProfileError($"Error deleting old profile backup files.\r\n{ex.Message}", false);
                }
                finally
                {
                    LogProfileMessage("Delete old profile backup files finished.");
                }

                // cleanup any backup folders from old backup process
                try
                {
                    var backupFolder = GetProfileBackupFolder(_profile.ProfileName);

                    var oldBackupFolders = new DirectoryInfo(backupFolder).GetDirectories();
                    foreach (var oldBackupFolder in oldBackupFolders)
                    {
                        oldBackupFolder.Delete(true);
                    }
                }
                catch
                {
                    // if unable to delete, do not bother
                }
            }
            finally
            {
                _profile = oldProfile;
            }
        }

        public void CreateServerBackupArchiveFile(StringBuilder emailMessage, ServerProfileSnapshot profile = null)
        {
            // do nothing if profile is null or SotF
            if (profile == null || profile.SotFEnabled)
                return;

            var oldProfile = _profile;

            try
            {
                _profile = profile;

                // check if the servers save folder exists
                var saveFolder = GetServerSaveFolder();
                if (Directory.Exists(saveFolder))
                {
                    // make a backup of the current world file.
                    var worldFile = GetServerWorldFile();
                    if (File.Exists(worldFile))
                    {
                        try
                        {
                            LogProfileMessage("Back up world files started...");

                            var backupFolder = GetServerBackupFolder(_profile.ProfileName);
                            var mapName = ServerProfile.GetProfileMapFileName(_profile.ServerMap, _profile.PGM_Enabled, _profile.PGM_Name);
                            var backupFileName = $"{mapName}_{_startTime.ToString("yyyyMMdd_HHmmss")}{Config.Default.BackupExtension}";
                            var backupFile = IOUtils.NormalizePath(Path.Combine(backupFolder, backupFileName));

                            if (!Directory.Exists(backupFolder))
                                Directory.CreateDirectory(backupFolder);

                            if (File.Exists(backupFile))
                                File.Delete(backupFile);

                            var files = new List<string>();
                            files.Add(worldFile);

                            // get the player files
                            var saveFolderInfo = new DirectoryInfo(saveFolder);
                            var playerFileFilter = $"*{Config.Default.PlayerFileExtension}";
                            var playerFiles = saveFolderInfo.GetFiles(playerFileFilter, SearchOption.TopDirectoryOnly);
                            foreach (var file in playerFiles)
                            {
                                files.Add(file.FullName);
                            }

                            // get the tribe files
                            var tribeFileFilter = $"*{Config.Default.TribeFileExtension}";
                            var tribeFiles = saveFolderInfo.GetFiles(tribeFileFilter, SearchOption.TopDirectoryOnly);
                            foreach (var file in tribeFiles)
                            {
                                files.Add(file.FullName);
                            }

                            // get the tribute tribe files
                            var tributeTribeFileFilter = $"*{Config.Default.TributeTribeFileExtension}";
                            var tributeTribeFiles = saveFolderInfo.GetFiles(tributeTribeFileFilter, SearchOption.TopDirectoryOnly);
                            foreach (var file in tributeTribeFiles)
                            {
                                files.Add(file.FullName);
                            }

                            //var playerImageFileFilter = $"*{Config.Default.PlayerImageFileExtension}";
                            //var playerImageFiles = saveFolderInfo.GetFiles(playerImageFileFilter, SearchOption.TopDirectoryOnly);
                            //foreach (var file in playerImageFiles)
                            //{
                            //    files.Add(file.FullName);
                            //}

                            var comment = new StringBuilder();
                            comment.AppendLine($"Windows Platform: {Environment.OSVersion.Platform}");
                            comment.AppendLine($"Windows Version: {Environment.OSVersion.VersionString}");
                            comment.AppendLine($"ASM Version: {App.Version}");
                            comment.AppendLine($"Config Directory: {Config.Default.ConfigDirectory}");
                            comment.AppendLine($"Server Directory: {_profile.InstallDirectory}");
                            comment.AppendLine($"Profile Name: {_profile.ProfileName}");
                            comment.AppendLine($"SotF Server: {_profile.SotFEnabled}");
                            comment.AppendLine($"PGM Server: {_profile.PGM_Enabled}");
                            comment.AppendLine($"Process: {ServerProcess}");

                            ZipUtils.ZipFiles(backupFile, files.ToArray(), comment.ToString(), false);

                            LogProfileMessage($"Backed up world files - {saveFolder}");
                            LogProfileMessage($"Backup file created - {backupFile}");

                            emailMessage?.AppendLine();
                            emailMessage?.AppendLine("Backed up world files.");
                            emailMessage?.AppendLine(saveFolder);

                            emailMessage?.AppendLine();
                            emailMessage?.AppendLine("Backup file created.");
                            emailMessage?.AppendLine(backupFile);
                        }
                        catch (Exception ex)
                        {
                            LogProfileError($"Error backing up world files.\r\n{ex.Message}", false);

                            emailMessage?.AppendLine();
                            emailMessage?.AppendLine("Error backing up world files.");
                            emailMessage?.AppendLine(ex.Message);
                        }
                        finally
                        {
                            LogProfileMessage("Back up world files finished.");
                        }
                    }
                    else
                    {
                        LogProfileMessage($"Server save file does not exist or could not be found '{worldFile}'.");
                        LogProfileMessage($"Backup not performed.");

                        emailMessage?.AppendLine();
                        emailMessage?.AppendLine($"Server save file does not exist or could not be found.");
                        emailMessage?.AppendLine(worldFile);

                        emailMessage?.AppendLine();
                        emailMessage?.AppendLine("Backup not performed.");
                    }
                }
                else
                {
                    LogProfileMessage($"Server save folder does not exist or could not be found '{saveFolder}'.");
                    LogProfileMessage($"Backup not performed.");

                    emailMessage?.AppendLine();
                    emailMessage?.AppendLine($"Server save folder does not exist or could not be found.");
                    emailMessage?.AppendLine(saveFolder);

                    emailMessage?.AppendLine();
                    emailMessage?.AppendLine("Backup not performed.");
                }

                // delete the old backup files
                if (DeleteOldServerBackupFiles)
                {
                    try
                    {
                        var deleteInterval = Config.Default.AutoBackup_EnableBackup ? Config.Default.AutoBackup_DeleteInterval : BACKUP_DELETEINTERVAL;

                        LogProfileMessage("Delete old server backup files started...");

                        var backupFolder = GetServerBackupFolder(_profile.ProfileName);
                        var mapName = ServerProfile.GetProfileMapFileName(_profile.ServerMap, _profile.PGM_Enabled, _profile.PGM_Name);
                        var backupFileFilter = $"{mapName}_*{Config.Default.BackupExtension}";
                        var backupDateFilter = DateTime.Now.AddDays(-deleteInterval);

                        var backupFiles = new DirectoryInfo(backupFolder).GetFiles(backupFileFilter).Where(f => f.LastWriteTime < backupDateFilter);
                        foreach (var backupFile in backupFiles)
                        {
                            try
                            {
                                LogProfileMessage($"{backupFile.Name} was deleted, last updated {backupFile.CreationTime.ToString()}.");
                                backupFile.Delete();
                            }
                            catch
                            {
                                // if unable to delete, do not bother
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogProfileError($"Error deleting old server backup files.\r\n{ex.Message}", false);
                    }
                    finally
                    {
                        LogProfileMessage("Delete old server backup files finished.");
                    }
                }
            }
            finally
            {
                _profile = oldProfile;
            }
        }

        public static void DirectoryCopy(string sourceFolder, string destinationFolder, bool copySubFolders, bool useSmartCopy, ProgressDelegate progressCallback)
        {
            var directory = new DirectoryInfo(sourceFolder);
            if (!directory.Exists)
                return;

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubFolders)
            {
                var subDirectories = directory.GetDirectories();

                foreach (var subDirectory in subDirectories)
                {
                    var tempDirectory = Path.Combine(destinationFolder, subDirectory.Name);
                    DirectoryCopy(subDirectory.FullName, tempDirectory, copySubFolders, useSmartCopy, progressCallback);
                }
            }

            progressCallback?.Invoke(0, directory.FullName);

            // Get the files in the directory and copy them to the new location.
            var files = directory.GetFiles();

            foreach (var file in files)
            {
                if (!file.Exists)
                    continue;

                // check if the destination file is newer
                var destFile = new FileInfo(Path.Combine(destinationFolder, file.Name));
                if (useSmartCopy && destFile.Exists && destFile.LastWriteTime >= file.LastWriteTime && destFile.Length == file.Length)
                    continue;

                // destination file does not exist, or is older. Override with the source file.
                while (true)
                {
                    var retries = 0;
                    try
                    {
                        file.CopyTo(destFile.FullName, true);
                        break;
                    }
                    catch (IOException)
                    {
                        retries++;
                        if (retries >= FILECOPY_MAXRETRIES) throw;
                        Task.Delay(5000).Wait();
                    }
                }
            }
        }

        public static string GetBranchName(string branchName) => string.IsNullOrWhiteSpace(branchName) ? Config.Default.DefaultServerBranchName : branchName;

        public static string GetBranchLogFile(string branchName) => IOUtils.NormalizePath(Path.Combine(SteamCmdUpdater.GetLogFolder(), _logPrefix, $"{_startTime.ToString("yyyyMMdd_HHmmss")}{(string.IsNullOrWhiteSpace(branchName) ? string.Empty : $"_{branchName}")}.log"));

        private string GetLauncherFile() => IOUtils.NormalizePath(Path.Combine(_profile.InstallDirectory, Config.Default.ServerConfigRelativePath, Config.Default.LauncherFile));

        private static string GetLogFile() => IOUtils.NormalizePath(Path.Combine(SteamCmdUpdater.GetLogFolder(), _logPrefix, $"{_startTime.ToString("yyyyMMdd_HHmmss")}.log"));

        private List<string> GetModList()
        {
            var modIdList = new List<string>();

            // check if we need to update the mods.
            if (Config.Default.ServerUpdate_UpdateModsWhenUpdatingServer)
            {
                if (_profile == null)
                {
                    // get all the mods for all the profiles.
                    foreach (var profile in _profiles.Keys)
                    {
                        // check if the profile is included int he auto update.
                        if (!profile.EnableAutoUpdate)
                            continue;

                        if (!string.IsNullOrWhiteSpace(profile.ServerMapModId))
                            modIdList.Add(profile.ServerMapModId);

                        if (!string.IsNullOrWhiteSpace(profile.TotalConversionModId))
                            modIdList.Add(profile.TotalConversionModId);

                        modIdList.AddRange(profile.ServerModIds);
                    }
                }
                else
                {
                    // get all the mods for only the specified profile.
                    if (!string.IsNullOrWhiteSpace(_profile.ServerMapModId))
                        modIdList.Add(_profile.ServerMapModId);

                    if (!string.IsNullOrWhiteSpace(_profile.TotalConversionModId))
                        modIdList.Add(_profile.TotalConversionModId);

                    modIdList.AddRange(_profile.ServerModIds);
                }
            }

            return ModUtils.ValidateModList(modIdList);
        }

        private static string GetProfileBackupFolder(string profileName)
        {
            if (string.IsNullOrWhiteSpace(Config.Default.BackupPath))
                return IOUtils.NormalizePath(Path.Combine(Config.Default.ConfigDirectory, Config.Default.BackupDir, profileName));

            return IOUtils.NormalizePath(Path.Combine(Config.Default.BackupPath, Config.Default.ProfilesDir, profileName));
        }

        private string GetProfileLogFile() => _profile != null ? IOUtils.NormalizePath(Path.Combine(SteamCmdUpdater.GetLogFolder(), _profile.ProfileName, _logPrefix, $"{_startTime.ToString("yyyyMMdd_HHmmss")}.log")) : GetLogFile();

        private string GetModPath(string modId) => IOUtils.NormalizePath(Path.Combine(_profile.InstallDirectory, Config.Default.ServerModsRelativePath, modId));

        public static string GetMutexName(string directory)
        {
            using (var hashAlgo = MD5.Create())
            {
                StringBuilder builder = new StringBuilder();

                var hashStr = Encoding.UTF8.GetBytes(directory ?? Assembly.GetExecutingAssembly().Location);
                var hash = hashAlgo.ComputeHash(hashStr);
                foreach (var b in hash)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        public static string GetServerBackupFolder(string profileName)
        {
            if (string.IsNullOrWhiteSpace(Config.Default.BackupPath))
                return IOUtils.NormalizePath(Path.Combine(Config.Default.DataDir, Config.Default.ServersInstallDir, Config.Default.BackupDir, profileName));

            return IOUtils.NormalizePath(Path.Combine(Config.Default.BackupPath, Config.Default.ServersInstallDir, profileName));
        }

        private static string GetServerCacheFolder(string branchName) => IOUtils.NormalizePath(Path.Combine(Config.Default.AutoUpdate_CacheDir, $"{Config.Default.ServerBranchFolderPrefix}{GetBranchName(branchName)}"));

        private static string GetServerCacheTimeFile(string branchName) => IOUtils.NormalizePath(Path.Combine(GetServerCacheFolder(branchName), Config.Default.LastUpdatedTimeFile));

        private string GetServerExecutableFile() => IOUtils.NormalizePath(Path.Combine(_profile.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerExe));

        private DateTime GetServerLatestTime(string timeFile)
        {
            try
            {
                if (!File.Exists(timeFile))
                    return DateTime.MinValue;

                var value = File.ReadAllText(timeFile);
                return DateTime.Parse(value, CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private Process GetServerProcess()
        {
            // Find the server process.
            var expectedPath = GetServerExecutableFile();
            var runningProcesses = Process.GetProcessesByName(Config.Default.ServerProcessName);

            Process process = null;
            foreach (var runningProcess in runningProcesses)
            {
                var runningPath = ProcessUtils.GetMainModuleFilepath(runningProcess.Id);
                if (string.Equals(expectedPath, runningPath, StringComparison.OrdinalIgnoreCase))
                {
                    process = runningProcess;
                    break;
                }
            }

            return process;
        }

        private string GetServerTimeFile() => IOUtils.NormalizePath(Path.Combine(_profile.InstallDirectory, Config.Default.LastUpdatedTimeFile));

        private string GetServerSaveFolder() => IOUtils.NormalizePath(ServerProfile.GetProfileSavePath(_profile.InstallDirectory, _profile.AltSaveDirectoryName, _profile.PGM_Enabled, _profile.PGM_Name));

        private string GetServerWorldFile()
        {
            var profileSaveFolder = GetServerSaveFolder();
            var mapName = ServerProfile.GetProfileMapFileName(_profile.ServerMap, _profile.PGM_Enabled, _profile.PGM_Name);
            return IOUtils.NormalizePath(Path.Combine(profileSaveFolder, $"{mapName}{Config.Default.MapExtension}"));
        }

        public static bool HasNewServerVersion(string directory, DateTime checkTime)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return false;

            // check if any of the files have changed in the root folder.
            var hasNewVersion = new DirectoryInfo(directory).GetFiles("*.*", SearchOption.TopDirectoryOnly).Any(file => file.LastWriteTime >= checkTime);
            if (!hasNewVersion)
            {
                // get a list of the sub folders.
                var folders = new DirectoryInfo(directory).GetDirectories();
                foreach (var folder in folders)
                {
                    // do not include the steamapps folder in the check
                    if (folder.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase))
                        continue;

                    hasNewVersion = folder.GetFiles("*.*", SearchOption.AllDirectories).Any(file => file.LastWriteTime >= checkTime);
                    if (hasNewVersion)
                        break;
                }
            }

            return hasNewVersion;
        }

        private static void LoadProfiles()
        {
            if (_profiles != null)
            {
                _profiles.Clear();
                _profiles = null;
            }

            _profiles = new Dictionary<ServerProfileSnapshot, ServerProfile>();

            foreach (var profileFile in Directory.EnumerateFiles(Config.Default.ConfigDirectory, "*" + Config.Default.ProfileExtension))
            {
                try
                {
                    var profile = ServerProfile.LoadFrom(profileFile);
                    _profiles.Add(ServerProfileSnapshot.Create(profile), profile);
                }
                catch (Exception ex)
                {
                    LogMessage($"The profile at {profileFile} failed to load.\r\n{ex.Message}\r\n{ex.StackTrace}");
                }
            }
        }

        private static void LogError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return;

            LogMessage($"***** ERROR: {error}");
        }

        private static void LogMessage(string message)
        {
            message = message ?? string.Empty;

            var logFile = GetLogFile();
            lock (LockObjectMessage)
            {
                if (!Directory.Exists(Path.GetDirectoryName(logFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(logFile));

                int retries = 0;
                while (retries < 3)
                {
                    try
                    {
                        File.AppendAllLines(logFile, new[] { $"{DateTime.Now.ToString("o", CultureInfo.CurrentCulture)}: {message}" }, Encoding.Unicode);
                        break;
                    }
                    catch (IOException)
                    {
                        retries++;
                        Task.Delay(WRITELOG_ERRORRETRYDELAY).Wait();
                    }
                }
            }

            Debug.WriteLine(message);
        }

        private void LogBranchError(string branchName, string error, bool includeProgressCallback = true)
        {
            if (string.IsNullOrWhiteSpace(error))
                return;

            LogBranchMessage(branchName, $"***** ERROR: {error}", includeProgressCallback);
        }

        private void LogBranchMessage(string branchName, string message, bool includeProgressCallback = true)
        {
            message = message ?? string.Empty;

            if (OutputLogs)
            {
                var logFile = GetBranchLogFile(branchName);
                lock (LockObjectBranchMessage)
                {
                    if (!Directory.Exists(Path.GetDirectoryName(logFile)))
                        Directory.CreateDirectory(Path.GetDirectoryName(logFile));

                    int retries = 0;
                    while (retries < 3)
                    {
                        try
                        {
                            File.AppendAllLines(logFile, new[] { $"{DateTime.Now.ToString("o", CultureInfo.CurrentCulture)}: {message}" }, Encoding.Unicode);
                            break;
                        }
                        catch (IOException)
                        {
                            retries++;
                            Task.Delay(WRITELOG_ERRORRETRYDELAY).Wait();
                        }
                    }
                }
            }

            if (includeProgressCallback)
                ProgressCallback?.Invoke(0, message);

            if (_profile != null)
                Debug.WriteLine($"[Branch {GetBranchName(branchName) ?? "unknown"}] {message}");
            else
                Debug.WriteLine(message);
        }

        private void LogProfileError(string error, bool includeProgressCallback = true)
        {
            if (string.IsNullOrWhiteSpace(error))
                return;

            LogProfileMessage($"***** ERROR: {error}", includeProgressCallback);
        }

        private void LogProfileMessage(string message, bool includeProgressCallback = true)
        {
            message = message ?? string.Empty;

            if (OutputLogs)
            {
                var logFile = GetProfileLogFile();
                lock (LockObjectProfileMessage)
                {
                    if (!Directory.Exists(Path.GetDirectoryName(logFile)))
                        Directory.CreateDirectory(Path.GetDirectoryName(logFile));

                    int retries = 0;
                    while (retries < 3)
                    {
                        try
                        {
                            File.AppendAllLines(logFile, new[] { $"{DateTime.Now.ToString("o", CultureInfo.CurrentCulture)}: {message}" }, Encoding.Unicode);
                            break;
                        }
                        catch (IOException)
                        {
                            retries++;
                            Task.Delay(WRITELOG_ERRORRETRYDELAY).Wait();
                        }
                    }
                }
            }

            if (includeProgressCallback)
                ProgressCallback?.Invoke(0, message);

            if (_profile != null)
                Debug.WriteLine($"[Profile {_profile?.ProfileName ?? "unknown"}] {message}");
            else
                Debug.WriteLine(message);
        }

        private void ProcessAlert(AlertType alertType, string alertMessage)
        {
            if (_pluginHelper == null || !SendAlerts || string.IsNullOrWhiteSpace(alertMessage))
                return;

            if (_pluginHelper.ProcessAlert(alertType, _profile?.ProfileName ?? String.Empty, alertMessage))
            {
                LogProfileMessage($"Alert message sent - {alertType}: {alertMessage}", false);
            }
        }

        private bool SendCommand(string command, bool retryIfFailed)
        {
            if (_profile == null || !_profile.RCONEnabled)
                return false;
            if (string.IsNullOrWhiteSpace(command))
                return false;

            int retries = 0;
            int rconRetries = 0;
            int maxRetries = retryIfFailed ? RCON_MAXRETRIES : 1;

            while (retries < maxRetries && rconRetries < RCON_MAXRETRIES)
            {
                SetupRconConsole();

                if (_rconConsole == null)
                {
                    LogProfileMessage($"RCON> {command} - attempt {rconRetries + 1} (a).", false);
#if DEBUG
                    LogProfileMessage("RCON connection not created.", false);
#endif
                    rconRetries++;
                }
                else
                {
                    rconRetries = 0;
                    try
                    {
                        _rconConsole.SendCommand(command);
                        LogProfileMessage($"RCON> {command}");

                        return true;
                    }
                    catch (Exception ex)
                    {
                        LogProfileMessage($"RCON> {command} - attempt {retries + 1} (b).", false);
#if DEBUG
                        LogProfileMessage($"{ex.Message}", false);
#endif
                    }

                    retries++;
                }
            }

            return false;
        }

        private bool SendMessage(string message, CancellationToken token)
        {
            var sent = SendCommand($"broadcast {message}", false);

            if (sent)
            {
                try
                {
                    Task.Delay(Config.Default.SendMessageDelay, token).Wait(token);
                }
                catch { }
            }

            return sent;
        }

        private void SendEmail(string subject, string body, bool includeLogFile, bool isBodyHtml = false)
        {
            if (!SendEmails)
                return;

            try
            {
                var email = new EmailUtil()
                {
                    EnableSsl = Config.Default.Email_UseSSL,
                    MailServer = Config.Default.Email_Host,
                    Port = Config.Default.Email_Port,
                    UseDefaultCredentials = Config.Default.Email_UseDetaultCredentials,
                    Credentials = Config.Default.Email_UseDetaultCredentials ? null : new NetworkCredential(Config.Default.Email_Username, Config.Default.Email_Password),
                };

                StringBuilder messageBody = new StringBuilder(body);
                Attachment attachment = null;

                if (includeLogFile)
                {
                    var logFile = GetProfileLogFile();
                    if (!string.IsNullOrWhiteSpace(logFile) && File.Exists(logFile))
                    {
                        attachment = new Attachment(logFile);
                    }
                }

                email.SendEmail(Config.Default.Email_From, Config.Default.Email_To?.Split(','), subject, messageBody.ToString(), isBodyHtml, new[] { attachment });

                LogProfileMessage($"Email Sent - {subject}\r\n{body}");
            }
            catch (Exception ex)
            {
                LogProfileError($"Unable to send email.\r\n{ex.Message}", false);
            }
        }

        private void SetupRconConsole()
        {
            CloseRconConsole();

            if (_profile == null || !_profile.RCONEnabled)
                return;

            try
            {
                var endPoint = new IPEndPoint(IPAddress.Parse(_profile.ServerIP), _profile.RCONPort);
                var server = ServerQuery.GetServerInstance(EngineType.Source, endPoint, sendTimeOut: 10000, receiveTimeOut: 10000);
                if (server == null)
                {
#if DEBUG
                    LogProfileMessage($"FAILED: {nameof(SetupRconConsole)} - ServerQuery could not be created.", false);
#endif
                    return;
                }

#if DEBUG
                LogProfileMessage($"SUCCESS: {nameof(SetupRconConsole)} - ServerQuery was created.", false);
#endif

                Task.Delay(1000).Wait();

                _rconConsole = server.GetControl(_profile.AdminPassword);
                if (_rconConsole == null)
                {
#if DEBUG
                    LogProfileMessage($"FAILED: {nameof(SetupRconConsole)} - RconConsole could not be created ({_profile.AdminPassword}).", false);
#endif
                    return;
                }

#if DEBUG
                LogProfileMessage($"SUCCESS: {nameof(SetupRconConsole)} - RconConsole was created ({_profile.AdminPassword}).", false);
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                LogProfileMessage($"ERROR: {nameof(SetupRconConsole)}\r\n{ex.Message}", false);
#endif
            }
        }

        public int PerformProfileBackup(ServerProfileSnapshot profile)
        {
            _profile = profile;

            if (_profile == null)
                return EXITCODE_NORMALEXIT;

            if (_profile.SotFEnabled)
                return EXITCODE_NORMALEXIT;

            ExitCode = EXITCODE_NORMALEXIT;

            Mutex mutex = null;
            var createdNew = false;

            try
            {
                // try to establish a mutex for the profile.
                mutex = new Mutex(true, GetMutexName(_profile.InstallDirectory), out createdNew);
                if (!createdNew)
                    createdNew = mutex.WaitOne(new TimeSpan(0, MUTEX_TIMEOUT, 0));

                // check if the mutex was established
                if (createdNew)
                {
                    BackupServer();

                    if (ExitCode != EXITCODE_NORMALEXIT)
                    {
                        if (Config.Default.EmailNotify_AutoBackup)
                            SendEmail($"{_profile.ProfileName} server backup", Config.Default.Alert_BackupProcessError, true);
                        ProcessAlert(AlertType.Error, Config.Default.Alert_BackupProcessError);
                    }
                }
                else
                {
                    ExitCode = EXITCODE_PROCESSALREADYRUNNING;
                    LogProfileMessage("Cancelled server backup process, could not lock server.");
                }
            }
            catch (Exception ex)
            {
                LogProfileError(ex.Message);
                if (ex.InnerException != null)
                    LogProfileMessage($"InnerException - {ex.InnerException.Message}");
                LogProfileMessage($"StackTrace\r\n{ex.StackTrace}");

                if (Config.Default.EmailNotify_AutoBackup)
                    SendEmail($"{_profile.ProfileName} server update", Config.Default.Alert_BackupProcessError, true);
                ProcessAlert(AlertType.Error, Config.Default.Alert_BackupProcessError);
                ExitCode = EXITCODE_UNKNOWNTHREADERROR;
            }
            finally
            {
                if (mutex != null)
                {
                    if (createdNew)
                    {
                        mutex.ReleaseMutex();
                        mutex.Dispose();
                    }
                    mutex = null;
                }
            }

            LogProfileMessage($"Exitcode = {ExitCode}");
            return ExitCode;
        }

        public int PerformProfileShutdown(ServerProfileSnapshot profile, bool performRestart, bool performUpdate, CancellationToken cancellationToken)
        {
            _profile = profile;

            if (_profile == null)
                return EXITCODE_NORMALEXIT;

            ExitCode = EXITCODE_NORMALEXIT;

            Mutex mutex = null;
            var createdNew = false;

            try
            {
                // try to establish a mutex for the profile.
                mutex = new Mutex(true, GetMutexName(_profile.InstallDirectory), out createdNew);
                if (!createdNew)
                    createdNew = mutex.WaitOne(new TimeSpan(0, MUTEX_TIMEOUT, 0));

                // check if the mutex was established
                if (createdNew)
                {
                    ShutdownServer(performRestart, performUpdate, cancellationToken);

                    if (ExitCode != EXITCODE_NORMALEXIT)
                    {
                        if (Config.Default.EmailNotify_AutoRestart)
                        {
                            if (performRestart)
                                SendEmail($"{_profile.ProfileName} server restart", Config.Default.Alert_RestartProcessError, true);
                            else
                                SendEmail($"{_profile.ProfileName} server shutdown", Config.Default.Alert_ShutdownProcessError, true);
                        }
                        if (performRestart)
                            ProcessAlert(AlertType.Error, Config.Default.Alert_RestartProcessError);
                        else
                            ProcessAlert(AlertType.Error, Config.Default.Alert_ShutdownProcessError);
                    }
                }
                else
                {
                    ExitCode = EXITCODE_PROCESSALREADYRUNNING;
                    if (performRestart)
                        LogProfileMessage("Cancelled server restart process, could not lock server.");
                    else
                        LogProfileMessage("Cancelled server shutdown process, could not lock server.");
                }
            }
            catch (Exception ex)
            {
                LogProfileError(ex.Message);
                if (ex.InnerException != null)
                    LogProfileMessage($"InnerException - {ex.InnerException.Message}");
                LogProfileMessage($"StackTrace\r\n{ex.StackTrace}");

                if (Config.Default.EmailNotify_AutoRestart)
                {
                    if (performRestart)
                        SendEmail($"{_profile.ProfileName} server restart", Config.Default.Alert_RestartProcessError, true);
                    else
                        SendEmail($"{_profile.ProfileName} server shutdown", Config.Default.Alert_ShutdownProcessError, true);
                }
                if (performRestart)
                    ProcessAlert(AlertType.Error, Config.Default.Alert_RestartProcessError);
                else
                    ProcessAlert(AlertType.Error, Config.Default.Alert_ShutdownProcessError);
                ExitCode = EXITCODE_UNKNOWNTHREADERROR;
            }
            finally
            {
                if (mutex != null)
                {
                    if (createdNew)
                    {
                        mutex.ReleaseMutex();
                        mutex.Dispose();
                    }
                    mutex = null;
                }
            }

            LogProfileMessage($"Exitcode = {ExitCode}");
            return ExitCode;
        }

        public int PerformProfileUpdate(ServerBranchSnapshot branch, ServerProfileSnapshot profile)
        {
            _profile = profile;

            if (_profile == null)
                return EXITCODE_NORMALEXIT;

            if (_profile.SotFEnabled)
                return EXITCODE_NORMALEXIT;

            ExitCode = EXITCODE_NORMALEXIT;

            Mutex mutex = null;
            var createdNew = false;

            try
            {
                LogBranchMessage(branch.BranchName, $"[{_profile.ProfileName}] Started server update process.");

                // try to establish a mutex for the profile.
                mutex = new Mutex(true, GetMutexName(_profile.InstallDirectory), out createdNew);
                if (!createdNew)
                    createdNew = mutex.WaitOne(new TimeSpan(0, MUTEX_TIMEOUT, 0));

                // check if the mutex was established
                if (createdNew)
                {
                    UpdateFiles();

                    LogBranchMessage(branch.BranchName, $"[{_profile.ProfileName}] Finished server update process.");

                    if (ExitCode != EXITCODE_NORMALEXIT)
                    {
                        if (Config.Default.EmailNotify_AutoUpdate)
                            SendEmail($"{_profile.ProfileName} server update", Config.Default.Alert_UpdateProcessError, true);
                        ProcessAlert(AlertType.Error, Config.Default.Alert_UpdateProcessError);
                    }
                }
                else
                {
                    ExitCode = EXITCODE_PROCESSALREADYRUNNING;
                    LogBranchMessage(branch.BranchName, $"[{_profile.ProfileName}] Cancelled server update process, could not lock server.");
                }
            }
            catch (Exception ex)
            {
                LogProfileError(ex.Message);
                LogProfileError(ex.GetType().ToString());
                if (ex.InnerException != null)
                {
                    LogProfileMessage($"InnerException - {ex.InnerException.Message}");
                    LogProfileMessage(ex.InnerException.GetType().ToString());
                }
                LogProfileMessage($"StackTrace\r\n{ex.StackTrace}");

                if (Config.Default.EmailNotify_AutoUpdate)
                    SendEmail($"{_profile.ProfileName} server update", Config.Default.Alert_UpdateProcessError, true);
                ProcessAlert(AlertType.Error, Config.Default.Alert_UpdateProcessError);
                ExitCode = EXITCODE_UNKNOWNTHREADERROR;
            }
            finally
            {
                if (mutex != null)
                {
                    if (createdNew)
                    {
                        mutex.ReleaseMutex();
                        mutex.Dispose();
                    }
                    mutex = null;
                }
            }

            LogProfileMessage($"Exitcode = {ExitCode}");
            return ExitCode;
        }

        public int PerformServerBranchUpdate(ServerBranchSnapshot branch)
        {
            if (branch == null)
                return EXITCODE_NORMALEXIT;

            ExitCode = EXITCODE_NORMALEXIT;

            Mutex mutex = null;
            var createdNew = false;

            try
            {
                LogBranchMessage(branch.BranchName, $"Started branch update process.");

                var cacheFolder = GetServerCacheFolder(branch.BranchName);

                // try to establish a mutex for the profile.
                mutex = new Mutex(true, GetMutexName(cacheFolder), out createdNew);
                if (!createdNew)
                    createdNew = mutex.WaitOne(new TimeSpan(0, MUTEX_TIMEOUT, 0));

                // check if the mutex was established
                if (createdNew)
                {
                    // update the server cache for the branch
                    UpdateServerCache(branch.BranchName, branch.BranchPassword);

                    if (ExitCode != EXITCODE_NORMALEXIT)
                    {
                        if (Config.Default.EmailNotify_AutoUpdate)
                            SendEmail($"{GetBranchName(branch.BranchName)} branch update", Config.Default.Alert_UpdateProcessError, true);
                        ProcessAlert(AlertType.Error, Config.Default.Alert_UpdateProcessError);
                    }

                    if (ExitCode == EXITCODE_NORMALEXIT)
                    {
                        // get the profile associated with the branch
                        var profiles = _profiles.Keys.Where(p => p.EnableAutoUpdate && p.BranchName.Equals(branch.BranchName, StringComparison.OrdinalIgnoreCase)).ToArray();
                        var profileExitCodes = new ConcurrentDictionary<ServerProfileSnapshot, int>();

                        if (Config.Default.AutoUpdate_ParallelUpdate)
                        {
                            Parallel.ForEach(profiles, profile =>
                            {
                                var app = new ServerApp();
                                app.SendAlerts = true;
                                app.SendEmails = true;
                                app.ServerProcess = ServerProcess;
                                app.SteamCMDProcessWindowStyle = ProcessWindowStyle.Hidden;
                                profileExitCodes.TryAdd(profile, app.PerformProfileUpdate(branch, profile));
                            });
                        }
                        else
                        {
                            foreach (var profile in _profiles.Keys.Where(p => p.EnableAutoUpdate))
                            {
                                var app = new ServerApp();
                                app.SendAlerts = true;
                                app.SendEmails = true;
                                app.ServerProcess = ServerProcess;
                                app.SteamCMDProcessWindowStyle = ProcessWindowStyle.Hidden;
                                profileExitCodes.TryAdd(profile, app.PerformProfileUpdate(branch, profile));
                            }
                        }

                        if (profileExitCodes.Any(c => !c.Value.Equals(EXITCODE_NORMALEXIT)))
                            ExitCode = EXITCODE_EXITWITHERRORS;
                    }

                    LogBranchMessage(branch.BranchName, $"Finished branch update process.");
                }
                else
                {
                    ExitCode = EXITCODE_PROCESSALREADYRUNNING;
                    LogBranchMessage(branch.BranchName, "Cancelled branch update process, could not lock branch folder.");
                }
            }
            catch (Exception ex)
            {
                LogBranchError(branch.BranchName, ex.Message);
                LogBranchError(branch.BranchName, ex.GetType().ToString());
                if (ex.InnerException != null)
                {
                    LogBranchMessage(branch.BranchName, $"InnerException - {ex.InnerException.Message}");
                    LogBranchMessage(branch.BranchName, ex.InnerException.GetType().ToString());
                }
                LogBranchMessage(branch.BranchName, $"StackTrace\r\n{ex.StackTrace}");

                if (Config.Default.EmailNotify_AutoUpdate)
                    SendEmail($"{GetBranchName(branch.BranchName)} branch update", Config.Default.Alert_UpdateProcessError, true);
                ProcessAlert(AlertType.Error, Config.Default.Alert_UpdateProcessError);
                ExitCode = EXITCODE_UNKNOWNTHREADERROR;
            }
            finally
            {
                if (mutex != null)
                {
                    if (createdNew)
                    {
                        mutex.ReleaseMutex();
                        mutex.Dispose();
                    }
                    mutex = null;
                }
            }

            LogBranchMessage(branch.BranchName, $"Exitcode = {ExitCode}");
            return ExitCode;
        }

        public static int PerformAutoBackup()
        {
            _logPrefix = LOGPREFIX_AUTOBACKUP;

            int exitCode = EXITCODE_NORMALEXIT;

            try
            {
                // check if a data directory has been setup.
                if (string.IsNullOrWhiteSpace(Config.Default.DataDir))
                    return EXITCODE_INVALIDDATADIRECTORY;

                // load all the profiles, do this at the very start in case the user changes one or more while the process is running.
                LoadProfiles();

                var exitCodes = new ConcurrentDictionary<ServerProfileSnapshot, int>();

                Parallel.ForEach(_profiles.Keys.Where(p => p.EnableAutoBackup), profile => {
                    var app = new ServerApp();
                    app.DeleteOldServerBackupFiles = Config.Default.AutoBackup_DeleteOldFiles;
                    app.SendAlerts = true;
                    app.SendEmails = true;
                    app.ServerProcess = ServerProcessType.AutoBackup;
                    exitCodes.TryAdd(profile, app.PerformProfileBackup(profile));
                });

                foreach (var profile in _profiles.Keys)
                {
                    if (profile.ServerUpdated)
                    {
                        profile.Update(_profiles[profile]);
                        _profiles[profile].SaveProfile();
                    }
                }

                if (exitCodes.Any(c => !c.Value.Equals(EXITCODE_NORMALEXIT)))
                    exitCode = EXITCODE_EXITWITHERRORS;
            }
            catch (Exception)
            {
                exitCode = EXITCODE_UNKNOWNERROR;
            }

            return exitCode;
        }

        public static int PerformAutoShutdown(string argument, ServerProcessType type)
        {
            _logPrefix = LOGPREFIX_AUTOSHUTDOWN;

            int exitCode = EXITCODE_NORMALEXIT;

            try
            {
                // check if a data directory has been setup.
                if (string.IsNullOrWhiteSpace(Config.Default.DataDir))
                    return EXITCODE_INVALIDDATADIRECTORY;

                if (string.IsNullOrWhiteSpace(argument) || (!argument.StartsWith(App.ARG_AUTOSHUTDOWN1) && !argument.StartsWith(App.ARG_AUTOSHUTDOWN2)))
                    return EXITCODE_BADARGUMENT;

                // load all the profiles, do this at the very start in case the user changes one or more while the process is running.
                LoadProfiles();

                var profileKey = string.Empty;
                switch (type)
                {
                    case ServerProcessType.AutoShutdown1:
                        profileKey = argument?.Substring(App.ARG_AUTOSHUTDOWN1.Length) ?? string.Empty;
                        break;
                    case ServerProcessType.AutoShutdown2:
                        profileKey = argument?.Substring(App.ARG_AUTOSHUTDOWN2.Length) ?? string.Empty;
                        break;
                    default:
                        return EXITCODE_BADARGUMENT;
                }

                var profile = _profiles?.Keys.FirstOrDefault(p => p.SchedulerKey.Equals(profileKey, StringComparison.Ordinal));
                if (profile == null)
                    return EXITCODE_PROFILENOTFOUND;

                var enableAutoShutdown = false;
                var performRestart = false;
                var performUpdate = false;
                switch (type)
                {
                    case ServerProcessType.AutoShutdown1:
                        enableAutoShutdown = profile.EnableAutoShutdown1;
                        performRestart = profile.RestartAfterShutdown1;
                        performUpdate = profile.UpdateAfterShutdown1;
                        break;
                    case ServerProcessType.AutoShutdown2:
                        enableAutoShutdown = profile.EnableAutoShutdown2;
                        performRestart = profile.RestartAfterShutdown2;
                        performUpdate = profile.UpdateAfterShutdown2;
                        break;
                    default:
                        return EXITCODE_BADARGUMENT;
                }

                if (!enableAutoShutdown)
                    return EXITCODE_AUTOSHUTDOWNNOTENABLED;

                var app = new ServerApp();
                app.SendAlerts = true;
                app.SendEmails = true;
                app.ServerProcess = type;
                app.SteamCMDProcessWindowStyle = ProcessWindowStyle.Hidden;
                exitCode = app.PerformProfileShutdown(profile, performRestart, performUpdate, CancellationToken.None);

                if (profile.ServerUpdated)
                {
                    profile.Update(_profiles[profile]);
                    _profiles[profile].SaveProfile();
                }
            }
            catch (Exception)
            {
                exitCode = EXITCODE_UNKNOWNERROR;
            }

            return exitCode;
        }

        public static int PerformAutoUpdate()
        {
            _logPrefix = LOGPREFIX_AUTOUPDATE;

            int exitCode = EXITCODE_NORMALEXIT;

            Mutex mutex = null;
            bool createdNew = false;

            try
            {
                // check if a data directory has been setup.
                if (string.IsNullOrWhiteSpace(Config.Default.DataDir))
                    return EXITCODE_INVALIDDATADIRECTORY;

                // check if the server cache folder has been set.
                if (string.IsNullOrWhiteSpace(Config.Default.AutoUpdate_CacheDir))
                    return EXITCODE_INVALIDCACHEDIRECTORY;

                // try to establish a mutex for the application.
                mutex = new Mutex(true, GetMutexName(Config.Default.DataDir), out createdNew);
                if (!createdNew)
                    createdNew = mutex.WaitOne(new TimeSpan(0, MUTEX_TIMEOUT, 0));

                // check if the mutex was established.
                if (createdNew)
                {
                    // load all the profiles, do this at the very start in case the user changes one or more while the process is running.
                    LoadProfiles();

                    // update the mods - needs to be done before the server cache updates
                    ServerApp app = new ServerApp();
                    app.ServerProcess = ServerProcessType.AutoUpdate;
                    app.SteamCMDProcessWindowStyle = ProcessWindowStyle.Hidden;
                    app.UpdateModCache();
                    exitCode = app.ExitCode;

                    if (exitCode == EXITCODE_NORMALEXIT)
                    {
                        var branches = _profiles.Keys.Where(p => p.EnableAutoUpdate).Select(p => new ServerBranchSnapshot() { BranchName = p.BranchName, BranchPassword = p.BranchPassword}).Distinct(new ServerBranchSnapshotComparer()).ToArray();
                        var exitCodes = new ConcurrentDictionary<ServerBranchSnapshot, int>();

                        // update the server cache for each branch
                        if (Config.Default.AutoUpdate_ParallelUpdate)
                        {
                            Parallel.ForEach(branches, branch => {
                                app = new ServerApp();
                                app.ServerProcess = ServerProcessType.AutoUpdate;
                                app.SteamCMDProcessWindowStyle = ProcessWindowStyle.Hidden;
                                app.PerformServerBranchUpdate(branch);
                                exitCodes.TryAdd(branch, app.ExitCode);
                            });
                        }
                        else
                        {
                            foreach (var branch in branches)
                            {
                                app = new ServerApp();
                                app.ServerProcess = ServerProcessType.AutoUpdate;
                                app.SteamCMDProcessWindowStyle = ProcessWindowStyle.Hidden;
                                app.PerformServerBranchUpdate(branch);
                                exitCodes.TryAdd(branch, app.ExitCode);
                            }
                        }

                        foreach (var profile in _profiles.Keys)
                        {
                            if (profile.ServerUpdated)
                            {
                                profile.Update(_profiles[profile]);
                                _profiles[profile].SaveProfile();
                            }
                        }

                        if (exitCodes.Any(c => !c.Value.Equals(EXITCODE_NORMALEXIT)))
                            exitCode = EXITCODE_EXITWITHERRORS;
                    }
                }
                else
                {
                    LogMessage("Cancelled auto update process, could not lock application.");
                    return EXITCODE_PROCESSALREADYRUNNING;
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                LogError(ex.GetType().ToString());
                if (ex.InnerException != null)
                {
                    LogMessage($"InnerException - {ex.InnerException.Message}");
                    LogMessage(ex.InnerException.GetType().ToString());
                }
                LogMessage($"StackTrace\r\n{ex.StackTrace}");
                exitCode = EXITCODE_UNKNOWNERROR;
            }
            finally
            {
                if (mutex != null)
                {
                    if (createdNew)
                    {
                        mutex.ReleaseMutex();
                        mutex.Dispose();
                    }
                    mutex = null;
                }
            }

            LogMessage("");
            LogMessage($"Exitcode = {exitCode}");
            return exitCode;
        }
    }
}

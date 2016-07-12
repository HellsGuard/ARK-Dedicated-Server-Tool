using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;

namespace ARK_Server_Manager.Lib
{
    public delegate void ProgressDelegate(int progress, string message);

    /// <summary>
    /// Checks for an updates this program
    /// </summary>
    class Updater
    {
        public const string OUTPUT_PREFIX = "[UPDATER]";

        public struct Update
        {
            public Update(string statusKey, float completionPercent)
            {
                this.StatusKey = statusKey;
                this.CompletionPercent = completionPercent;
                this.Cancelled = false;
                this.FailureText = null;
            }

            public Update SetFailed(string failureText)
            {
                this.FailureText = failureText;
                return this;
            }

            public static Update AsCompleted(string statusKey)
            {
                return new Update { StatusKey = statusKey, CompletionPercent = 100, Cancelled = false };
            }

            public static Update AsCancelled(string statusKey)
            {
                return new Update { StatusKey = statusKey, CompletionPercent = 100, Cancelled = true };
            }

            public string StatusKey;
            public float CompletionPercent;
            public bool Cancelled;
            public string FailureText;
        }

        enum Status
        {
            CheckForNewServerVersion,
            DownloadNewServerVersion,
            DownloadNewServerComplete,
            CleaningSteamCmd,
            DownloadingSteamCmd,
            UnzippingSteamCmd,
            RunningSteamCmd,
            InstallSteamCmdComplete,
            Complete,
            Cancelled
        }

        Dictionary<Status, Update> statuses = new Dictionary<Status, Update>()
        {
           { Status.CleaningSteamCmd, new Update("AutoUpdater_Status_CleaningSteamCmd", 0) },
           { Status.DownloadingSteamCmd, new Update("AutoUpdater_Status_DownloadingSteamCmd", 0) },
           { Status.UnzippingSteamCmd, new Update("AutoUpdater_Status_UnzippingSteamCmd", 20) },
           { Status.RunningSteamCmd, new Update("AutoUpdater_Status_RunningSteamCmd", 40) },
           { Status.InstallSteamCmdComplete, new Update("AutoUpdater_Status_InstallSteamCmdComplete", 50) },
           { Status.CheckForNewServerVersion, new Update("AutoUpdater_Status_CheckForNewServerVersion", 50) },
           { Status.DownloadNewServerVersion, new Update("AutoUpdater_Status_DownloadNewServerVersion", 60) },
           { Status.DownloadNewServerComplete, new Update("AutoUpdater_Status_DownloadNewServerComplete", 90) },
           { Status.Complete, Update.AsCompleted("AutoUpdater_Status_Complete") },
           { Status.Cancelled, Update.AsCancelled("AutoUpdater_Status_Cancelled") }
        };

        public static bool IsAutoUpdateCacheEnabled => Config.Default.AutoUpdate_EnableUpdate && Config.Default.AutoUpdate_UpdatePeriod >= 0 && !string.IsNullOrWhiteSpace(Config.Default.AutoUpdate_CacheDir) && Directory.Exists(Config.Default.AutoUpdate_CacheDir);

        public static string GetLogFolder() => Updater.NormalizePath(Path.Combine(Config.Default.DataDir, Config.Default.LogsDir));

        public static Version GetServerVersion(string versionFile)
        {
            if (!string.IsNullOrWhiteSpace(versionFile) && File.Exists(versionFile))
            {
                var fileValue = File.ReadAllText(versionFile);

                if (!string.IsNullOrWhiteSpace(fileValue))
                {
                    string versionString = fileValue.ToString();
                    if (versionString.IndexOf('.') == -1)
                        versionString = versionString + ".0";

                    Version version;
                    if (Version.TryParse(versionString, out version))
                        return version;
                }
            }

            return new Version(0, 0);
        }

        public static string GetSteamCmdFile() => NormalizePath(Path.Combine(Config.Default.DataDir, Config.Default.SteamCmdDir, Config.Default.SteamCmdExe));

        public static string NormalizePath(string path) => Path.GetFullPath(new Uri(path).LocalPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();

        #region SteamCMD
        public async Task ReinstallSteamCmdAsync(IProgress<Update> reporter, CancellationToken cancellationToken)
        {
            try
            {
                reporter?.Report(statuses[Status.CleaningSteamCmd]);

                string steamCmdDirectory = Path.Combine(Config.Default.DataDir, Config.Default.SteamCmdDir);
                if (Directory.Exists(steamCmdDirectory))
                {
                    Directory.Delete(steamCmdDirectory, true);
                }

                await Task.Delay(5000);
                await InstallSteamCmdAsync(reporter, cancellationToken);

                reporter?.Report(statuses[Status.InstallSteamCmdComplete]);
                reporter?.Report(statuses[Status.Complete]);
            }
            catch (TaskCanceledException)
            {
                reporter?.Report(statuses[Status.Cancelled]);
            }
            catch (Exception ex)
            {
                reporter?.Report(statuses[Status.Complete].SetFailed(ex.ToString()));
            }
        }

        private async Task InstallSteamCmdAsync(IProgress<Update> reporter, CancellationToken cancellationToken)
        {
            string steamCmdDirectory = Path.Combine(Config.Default.DataDir, Config.Default.SteamCmdDir);
            if (!Directory.Exists(steamCmdDirectory))
            {
                Directory.CreateDirectory(steamCmdDirectory);
            }

            reporter?.Report(statuses[Status.DownloadingSteamCmd]);

            // Get SteamCmd.exe if necessary
            string steamCmdPath = Path.Combine(steamCmdDirectory, Config.Default.SteamCmdExe);
            if (!File.Exists(steamCmdPath))
            {
                // download the SteamCMD zip file
                var steamZipPath = Path.Combine(steamCmdDirectory, Config.Default.SteamCmdZip);
                using (var webClient = new WebClient())
                {
                    using (var cancelRegistration = cancellationToken.Register(webClient.CancelAsync))
                    {
                        await webClient.DownloadFileTaskAsync(Config.Default.SteamCmdUrl, steamZipPath);
                    }
                }

                // Unzip the downloaded file
                reporter?.Report(statuses[Status.UnzippingSteamCmd]);

                ZipFile.ExtractToDirectory(steamZipPath, steamCmdDirectory);
                File.Delete(steamZipPath);

                // Run the SteamCmd updater
                reporter?.Report(statuses[Status.RunningSteamCmd]);

                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = steamCmdPath,
                    Arguments = Config.Default.SteamCmdInstallArgs,
                    UseShellExecute = false,
                };

                var process = Process.Start(startInfo);
                process.EnableRaisingEvents = true;

                var ts = new TaskCompletionSource<bool>();
                using (var cancelRegistration = cancellationToken.Register(() => 
                    {
                        try
                        {
                            process.Kill();
                        }
                        finally
                        {
                            ts.TrySetCanceled();
                        }
                    }))
                {
                    process.Exited += (s, e) => 
                    {
                        ts.TrySetResult(process.ExitCode == 0);
                    };
                    await ts.Task;
                }
            }

            return;
        }

        public async void UpdateSteamCmdAsync(IProgress<Update> reporter, CancellationToken cancellationToken)
        {
            try
            {
                await InstallSteamCmdAsync(reporter, cancellationToken);

                reporter?.Report(statuses[Status.InstallSteamCmdComplete]);
                reporter?.Report(statuses[Status.Complete]);
            }
            catch (TaskCanceledException)
            {
                reporter?.Report(statuses[Status.Cancelled]);
            }
            catch(Exception ex)
            {
                reporter?.Report(statuses[Status.Complete].SetFailed(ex.ToString()));
            }
        }
        #endregion

        #region ASM Update
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string name);

        private static bool Unblock(string fileName)
        {
            return DeleteFile(fileName + ":Zone.Identifier");
        }

        public static void UpdateASM()
        {
            var applicationZip = Path.Combine(Path.GetTempPath(), "ASMLatest.zip");
            var extractPath = Path.Combine(Path.GetTempPath(), "ASMLatest");
            var updateFilePath = Path.Combine(Path.GetTempPath(), "ASMUpdate.cmd");
            var currentInstallPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var backupPath = currentInstallPath + "_bak";

            // Grab the latest bits
            using (var client = new WebClient())
            {
                client.DownloadFile(Config.Default.ASMDownloadUrl, applicationZip);
                Unblock(applicationZip);
            }

            // Extract them
            try { Directory.Delete(extractPath, true); } catch { }
            ZipFile.ExtractToDirectory(applicationZip, extractPath);

            // Replace the current installation
            var script = new StringBuilder();
            
            script.AppendLine("timeout 2");
            script.AppendLine($"rmdir /s /q {backupPath.AsQuoted()}");
            script.AppendLine($"rename {currentInstallPath.AsQuoted()} {Path.GetFileName(backupPath).AsQuoted()}");
            script.AppendLine($"xcopy /e /y {(extractPath + "\\*.*").AsQuoted()} {(currentInstallPath + "\\").AsQuoted()}");
            script.AppendLine($"start \"\" {Assembly.GetExecutingAssembly().Location.AsQuoted()}");
            script.AppendLine("exit");

            ScriptUtils.RunShellScript(nameof(UpdateASM), script.ToString(), withElevation: false, waitForExit: false);

            Application.Current.Shutdown(0);
        }
        #endregion
    }
}

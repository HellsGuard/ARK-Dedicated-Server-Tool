using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.CodeDom;
using Microsoft.CSharp;
using System.Windows;
using System.Runtime.InteropServices;

namespace ARK_Server_Manager.Lib
{
    /// <summary>
    /// Checks for an updates this program
    /// </summary>
    class Updater
    {

        enum Status
        {
            CheckForNewServerVersion,
            DownloadNewServerVersion,
            DownloadNewServerComplete,
            DownloadingSteamCmd,
            UnzippingSteamCmd,
            RunningSteamCmd,
            InstallSteamCmdComplete,
            Complete,
            Cancelled
        }

        Dictionary<Status, Update> statuses = new Dictionary<Status, Update>()
        {
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

        public async void UpdateAsync(IProgress<Update> reporter, CancellationToken cancellationToken)
        {
            try
            {
                await InstallSteamCmdAsync(reporter, cancellationToken);
                reporter.Report(statuses[Status.InstallSteamCmdComplete]);

                reporter.Report(statuses[Status.Complete]);
            }
            catch (TaskCanceledException)
            {
                reporter.Report(statuses[Status.Cancelled]);
            }

            catch(Exception ex)
            {
                reporter.Report(statuses[Status.Complete].SetFailed(ex.ToString()));
                return;
            }
        }

        private async Task InstallSteamCmdAsync(IProgress<Update> reporter, CancellationToken cancellationToken)
        {
            string steamCmdDirectory = Path.Combine(Config.Default.DataDir, Config.Default.SteamCmdDir);
            if(!Directory.Exists(steamCmdDirectory))
            {
                Directory.CreateDirectory(steamCmdDirectory);
            }

            reporter.Report(statuses[Status.DownloadingSteamCmd]);

            // Get SteamCmd.exe if necessary
            string steamCmdPath = Path.Combine(steamCmdDirectory, Config.Default.SteamCmdExe);
            if (!File.Exists(steamCmdPath))
            {
                var steamZipPath = Path.Combine(steamCmdDirectory, Config.Default.SteamCmdZip);
                using (var webClient = new WebClient())
                {
                    using (var cancelRegistration = cancellationToken.Register(webClient.CancelAsync))
                    {
                        await webClient.DownloadFileTaskAsync(Config.Default.SteamCmdUrl, steamZipPath);
                    }
                }

                reporter.Report(statuses[Status.UnzippingSteamCmd]);
                ZipFile.ExtractToDirectory(steamZipPath, steamCmdDirectory);
                File.Delete(steamZipPath);

                // Run the SteamCmd updater
                reporter.Report(statuses[Status.RunningSteamCmd]);
                var process = Process.Start(steamCmdPath, Config.Default.SteamCmdInstallArgs);
                process.EnableRaisingEvents = true;
                var ts = new TaskCompletionSource<bool>();
                using (var cancelRegistration = cancellationToken.Register(() => { try { process.CloseMainWindow(); } finally { ts.TrySetCanceled(); } }))
                {
                    process.Exited += (s, e) =>
                        {
                            ts.TrySetResult(process.ExitCode == 0);
                        };
                    process.ErrorDataReceived += (s, e) =>
                        {
                            ts.TrySetException(new Exception(e.Data));
                        };
                    await ts.Task;
                }
            }

            return;
        }

        public static bool IsServerCacheAutoUpdateEnabled => Config.Default.GLOBAL_EnableServerCache && Config.Default.ServerCacheUpdatePeriod != 0 && Directory.Exists(Config.Default.ServerCacheDir);

        public static string GetSteamCMDPath()
        {
            var steamCmdPath = System.IO.Path.Combine(Config.Default.DataDir, Config.Default.SteamCmdDir, Config.Default.SteamCmdExe);
            return steamCmdPath;
        }

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

        public static void GenerateServerUpdater()
        {
            
        }
    }
}

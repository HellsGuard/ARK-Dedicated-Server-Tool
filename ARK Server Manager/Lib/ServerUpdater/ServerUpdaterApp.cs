using QueryMaster;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    partial class ServerUpdaterApp
    { 
        static QueryMaster.Server server;

        static void Main(string[] args)
        {
            bool noDelay = false;
            if (args.Length == 0)
            {
                WriteHelpAndExit();
            }
            else if(String.Equals(args[0], "Cache"))
            {
                var result = UpdateCache();
                Environment.Exit(result ? 0 : -1);

            }
            else if (String.Equals(args[0], "Auto", StringComparison.OrdinalIgnoreCase))
            {
                // Run the update using the values compiled in.
                if(String.Equals(args[1], "NoDelay", StringComparison.OrdinalIgnoreCase))
                {
                    noDelay = true;
                }
            }
            else if (args[0] == "Manual")
            {
                if (args.Length < 6)
                {
                    WriteHelpAndExit();
                }

                ServerIP = args[1];
                RCONPort = Int32.Parse(args[2]);
                AdminPass = args[3];
                InstallDirectory = args[4];
                SteamCmdPath = args[5];
                noDelay = true;
            }

            RunUpdate(noDelay);
        }

        private static void RunUpdate(bool noDelay)
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(ServerIP), RCONPort);
            server = ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, endpoint);

            //
            // Find the process
            //
            var runningProcesses = Process.GetProcessesByName("ShooterGameServer.exe");
            var expectedExePath = ServerUpdater.NormalizePath(Path.Combine(InstallDirectory, "ShooterGame", "Binaries", "Win64", "ShooterGameServer.exe"));
            Process process = null;
            foreach (var runningProcess in runningProcesses)
            {
                var runningPath = ServerUpdater.NormalizePath(runningProcess.Modules[0].FileName);
                if (String.Equals(expectedExePath, runningPath))
                {
                    process = runningProcess;
                    break;
                }
            }

            if (process == null)
            {
                Console.WriteLine("ERROR: Unable to find process for " + InstallDirectory);
                Environment.Exit(-1);
            }

            var serverCommandLine = ServerUpdater.GetCommandLineForProcess(process.Id);
            if (String.IsNullOrEmpty(serverCommandLine))
            {
                Console.WriteLine("ERROR: Unable to retrieve command-line for process " + process.Id);
                Environment.Exit(-1);
            }

            //
            // Stop the server
            //
            Console.WriteLine("Stopping the server...");
            process.EnableRaisingEvents = true;
            if (process.ProcessName == "ShooterGameServer.exe")
            {
                TaskCompletionSource<bool> ts = new TaskCompletionSource<bool>();
                EventHandler handler = (s, e) => ts.TrySetResult(true);
                process.Exited += handler;
                process.CloseMainWindow();
                if (!process.HasExited)
                {
                    ts.Task.Wait();
                }
            }

            if(!UpdateCache())
            {
                Environment.Exit(-1);
            }

            Console.WriteLine("Re-launching the server");
            process = Process.Start(serverCommandLine);
            if (process == null)
            {
                Console.WriteLine("Failed to restart server.");
                Environment.Exit(-1);
            }

            //
            // Re-schedule updater
            //
            Console.WriteLine("TODO: Reschedule update");

            Environment.Exit(0);
        }

        private static bool UpdateCache()
        {
            //
            // Run the update.
            //
            Console.WriteLine("Running the update");
            bool gotNewVersion = false;
            bool downloadSuccessful = false;
            DataReceivedEventHandler outputHandler = (s, e) =>
            {
                Console.WriteLine(e.Data);
                if(!gotNewVersion && e.Data.Contains("downloading,"))
                {
                    gotNewVersion = true;
                }

                if(e.Data.StartsWith("Success!"))
                {
                    downloadSuccessful = true;
                }
            };

            var result = ServerUpdater.UpgradeServerAsync(false, InstallDirectory, SteamCmdPath, "+login anonymous +force_install_dir \"{0}\"  \"+app_update 376030 {1}\" +quit", null, CancellationToken.None).Result;
            if (!result || !downloadSuccessful)
            {
                Console.WriteLine("Failed to update.");
                return false;
            }
            else
            {
                File.WriteAllText(GetLatestCacheTimeFile(), DateTime.Now.ToString("o", CultureInfo.CurrentCulture));
                return true;
            }
        }

        private static DateTime GetLatestCacheTime()
        {
            try
            {
                var time = File.ReadAllText(GetLatestCacheTimeFile());
                return DateTime.Parse(time, CultureInfo.CurrentCulture , DateTimeStyles.RoundtripKind);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static string GetLatestCacheTimeFile()
        {
            return Path.Combine(ServerCacheDir, "LastUpdatedTime.txt");
        }

        private static void WriteHelpAndExit()
        {
            Console.WriteLine(@"
Usage:
ServerUpdater.exe Auto [NoDelay]
    Automatically updates the server using the compiled-in values after the timeout, unless NoDelay is specified

ServerUpdater.exe Manual <serverIp> <rconPort> <adminPass> <processId> <installDirectory> <steamCmdPath>
    Updates the server immediately using the specified values
");
            Environment.Exit(-1);
        }

        public static void SendMessage(string message)
        {
            var console = server.GetControl(AdminPass);
            var response = console.SendCommand("broadcast Time To update!");
            Console.WriteLine(String.Format("Response was {0}", response));
        }
    }
}

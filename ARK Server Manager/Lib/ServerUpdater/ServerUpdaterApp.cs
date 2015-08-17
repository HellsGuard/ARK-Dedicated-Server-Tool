using QueryMaster;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    static class ServerUpdaterApp
    {

        static string adminPass;
        static QueryMaster.Server server;
        static int processId;

        static void Main(string[] args)
        {
            var serverIP = args[0];
            var rconPort = args[1];
            adminPass = args[2];
            processId = Int32.Parse(args[3]);
            var installDirectory = args[4];
            var steamCmdPath = args[5];

            var endpoint = new IPEndPoint(IPAddress.Parse(serverIP), Int32.Parse(rconPort));
            server = ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, endpoint);

            //
            // Stop the server
            //
            var process = Process.GetProcessById(processId);
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

            //
            // Run the update.
            //
            var result = ServerUpdater.UpgradeServerAsync(false, installDirectory, steamCmdPath, "+login anonymous +force_install_dir \"{0}\"  \"+app_update 376030 {1}\" +quit", CancellationToken.None).Result;
            Environment.Exit(result ? 0 : -1);
        }

        public static void SendMessage(string message)
        {
            var console = server.GetControl(adminPass);
            var response = console.SendCommand("broadcast Time To update!");
            Console.WriteLine(String.Format("Response was {0}", response));
        }
    }
}

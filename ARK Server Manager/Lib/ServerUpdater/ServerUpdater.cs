using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public static class ServerUpdater
    {
        public static Task<bool> UpgradeServerAsync(bool validate, string serverInstallDirectory, string steamCmdPath, string steamCmdArgsFormat, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(serverInstallDirectory);
            var steamArgs = String.Format(steamCmdArgsFormat, serverInstallDirectory, validate ? "validate" : String.Empty);

            var process = Process.Start(steamCmdPath, steamArgs);
            process.EnableRaisingEvents = true;
            var ts = new TaskCompletionSource<bool>();
            using (var cancelRegistration = cancellationToken.Register(() => { try { process.CloseMainWindow(); } finally { ts.TrySetCanceled(); } }))
            {
                process.Exited += (s, e) => ts.TrySetResult(process.ExitCode == 0);
                process.ErrorDataReceived += (s, e) => ts.TrySetException(new Exception(e.Data));
                return ts.Task;
            }
        }
    }
}

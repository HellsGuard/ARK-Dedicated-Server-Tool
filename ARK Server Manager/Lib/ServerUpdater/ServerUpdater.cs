using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public static class ServerUpdater
    {
        public static Task<bool> UpgradeServerAsync(bool validate, string serverInstallDirectory, string steamCmdPath, string steamCmdArgsFormat, DataReceivedEventHandler outputHandler, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(serverInstallDirectory);
            var steamArgs = String.Format(steamCmdArgsFormat, serverInstallDirectory, validate ? "validate" : String.Empty);

            var startInfo = new ProcessStartInfo()
            {
                FileName = steamCmdPath,
                Arguments = steamArgs,
                UseShellExecute = false,
                RedirectStandardOutput = outputHandler != null,
            };
            
            var process = Process.Start(startInfo);
            process.EnableRaisingEvents = true;
            if (outputHandler != null)
            {
                process.OutputDataReceived += outputHandler;
                process.BeginOutputReadLine();
            }

            var ts = new TaskCompletionSource<bool>(); 
            using (var cancelRegistration = cancellationToken.Register(() => { try { process.CloseMainWindow(); } finally { ts.TrySetCanceled(); } }))
            {
                process.Exited += (s, e) => ts.TrySetResult(process.ExitCode == 0);
                process.ErrorDataReceived += (s, e) => ts.TrySetException(new Exception(e.Data));
                return ts.Task;
            }
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public static string GetCommandLineForProcess(int processId)
        {
            var commandLineBuilder = new StringBuilder();
            
            using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + processId))
            {
                foreach (var @object in searcher.Get())
                {
                    commandLineBuilder.Append(@object["CommandLine"] + " ");
                }
            }

            var commandLine = commandLineBuilder.ToString();
            return commandLine;
        }

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }
    }
}

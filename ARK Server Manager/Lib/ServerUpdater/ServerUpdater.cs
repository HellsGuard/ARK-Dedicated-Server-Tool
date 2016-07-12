using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public static class ServerUpdater
    {
        public static Task<bool> UpgradeServerAsync(string steamCmdFile, string steamCmdArgsFormat, string serverInstallDirectory, bool validate, DataReceivedEventHandler outputHandler, CancellationToken cancellationToken, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal)
        {
            Directory.CreateDirectory(serverInstallDirectory);

            var steamCmdArgs = String.Format(steamCmdArgsFormat, serverInstallDirectory, validate ? "validate" : String.Empty);
            return ProcessUtils.RunProcessAsync(steamCmdFile, steamCmdArgs, string.Empty, outputHandler, cancellationToken, windowStyle);
        }

        public static Task<bool> UpgradeModsAsync(string steamCmdFile, string steamCmdArgsFormat, string modId, DataReceivedEventHandler outputHandler, CancellationToken cancellationToken, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal)
        {
            var steamCmdArgs = String.Format(steamCmdArgsFormat, modId);
            return ProcessUtils.RunProcessAsync(steamCmdFile, steamCmdArgs, string.Empty, outputHandler, cancellationToken, windowStyle);
        }
    }
}

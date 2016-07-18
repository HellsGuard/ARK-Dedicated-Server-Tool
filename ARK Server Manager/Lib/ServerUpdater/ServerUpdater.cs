using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public static class ServerUpdater
    {
        public static Task<bool> UpgradeServerAsync(string steamCmdFile, string steamCmdArgs, string serverInstallDirectory, DataReceivedEventHandler outputHandler, CancellationToken cancellationToken, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal)
        {
            Directory.CreateDirectory(serverInstallDirectory);

            return ProcessUtils.RunProcessAsync(steamCmdFile, steamCmdArgs, string.Empty, outputHandler, cancellationToken, windowStyle);
        }

        public static Task<bool> UpgradeModsAsync(string steamCmdFile, string steamCmdArgs, DataReceivedEventHandler outputHandler, CancellationToken cancellationToken, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal)
        {
            return ProcessUtils.RunProcessAsync(steamCmdFile, steamCmdArgs, string.Empty, outputHandler, cancellationToken, windowStyle);
        }
    }
}

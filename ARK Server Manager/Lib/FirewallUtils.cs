using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace ARK_Server_Manager.Lib
{
    /// <summary>
    /// Code for dealing with firewalls
    /// </summary>
    /// <remarks>
    /// Sources: http://stackoverflow.com/questions/1663960/c-sharp-api-to-test-if-a-network-adapter-is-firewalled
    /// </remarks>
    public static class FirewallUtils
    {
        public static bool EnsurePortsOpen(string exeName, int[] ports, string ruleName)
        {
            string rulesCommand = String.Format(@"
netsh advfirewall firewall delete rule all program = ""{0}""
netsh advfirewall firewall add rule name = ""{1} TCP"" action = allow program = ""{0}"" enable = yes localport={2} remoteport = any protocol = tcp dir = in
IF ERRORLEVEL 1 EXIT 1
netsh advfirewall firewall add rule name = ""{1} UDP"" action = allow program = ""{0}"" enable = yes localport={2} remoteport = any protocol = udp dir = in
IF ERRORLEVEL 1 EXIT 1
", exeName, ruleName, String.Join(",", ports));

            string tempPath = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), ".cmd"));
            try
            {
                File.WriteAllText(tempPath, rulesCommand);
                return RunElevatedShellScript(tempPath);
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        private static bool RunElevatedShellScript(string scriptName)
        {
            try
            {
                ProcessStartInfo psInfo = new ProcessStartInfo()
                {
                    FileName = scriptName,
                    Verb = "runas"
                };

                var process = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo = psInfo
                };

                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Firewall process failed to start: {0}", ex.Message);
                return false;
            }
        }
    }
}

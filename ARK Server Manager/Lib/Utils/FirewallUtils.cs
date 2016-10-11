using System;
using System.Text;

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
            StringBuilder rulesCommand = new StringBuilder();
            rulesCommand.AppendFormat("netsh advfirewall firewall delete rule all program = \"{0}\"", exeName);
            rulesCommand.AppendLine();
            rulesCommand.AppendFormat("netsh advfirewall firewall add rule name = \"{0} TCP\" action = allow program = \"{1}\" enable = yes localport={2} remoteport = any protocol = tcp dir = in", ruleName, exeName, String.Join(",", ports));
            rulesCommand.AppendLine();
            rulesCommand.AppendFormat("IF ERRORLEVEL 1 EXIT 1");
            rulesCommand.AppendLine();
            rulesCommand.AppendFormat("netsh advfirewall firewall add rule name = \"{0} UDP\" action = allow program = \"{1}\" enable = yes localport={2} remoteport = any protocol = udp dir = in", ruleName, exeName, String.Join(",", ports));
            rulesCommand.AppendLine();
            rulesCommand.AppendFormat("IF ERRORLEVEL 1 EXIT 1");
            rulesCommand.AppendLine();

            return ScriptUtils.RunElevatedShellScript(nameof(EnsurePortsOpen), rulesCommand.ToString());           
        }       
    }
}

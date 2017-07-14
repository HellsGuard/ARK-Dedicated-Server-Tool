using ArkServerManager.Plugin.Common;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace ArkServerManager.Plugin.Test
{
    public class TestAlertPlugin : IAlertPlugin
    {
        public bool Enabled => true;

        public string PluginCode => "TestAlertPlugin";

        public string PluginName => "Test Alert Plugin";

        public bool HasConfigForm => false;

        public void HandleAlert(AlertType alertType, string profileName, string alertMessage)
        {
            var installPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var logFile = Path.Combine(installPath, $"alerts_{profileName}.log");

            File.AppendAllText(logFile, $"{DateTime.Now}: {alertType} - {alertMessage}{Environment.NewLine}");
        }

        public void OpenConfigForm(Window owner)
        {
            return;
        }
    }
}

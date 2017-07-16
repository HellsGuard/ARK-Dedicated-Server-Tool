using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace ArkServerManager.Plugin.Common
{
    public sealed class PluginHelper
    {
        private const string PLUGINFILE_FOLDER = "Plugins";
        private const string PLUGINFILE_PREFIX = "";
        private const string PLUGINFILE_EXTENSION = "dll";

        public static PluginHelper Instance = new PluginHelper();

        private Object lockObject = new Object();

        public PluginHelper()
        {
            Plugins = new ObservableCollection<PluginItem>();
        }

        public ObservableCollection<PluginItem> Plugins
        {
            get;
            private set;
        }

        public void AddPlugin(string folder, string pluginFile)
        {
            if (!CheckPluginFile(pluginFile))
                throw new PluginException("The selected file does not contain ASM plugins.");

            var pluginFolder = Path.Combine(folder, PLUGINFILE_FOLDER);
            if (!Directory.Exists(pluginFolder))
                Directory.CreateDirectory(pluginFolder);

            var newPluginFile = Path.Combine(pluginFolder, $"{PLUGINFILE_PREFIX}{Path.GetFileName(pluginFile)}");
            if (File.Exists(newPluginFile))
                throw new PluginException("A file with the same name already exists.");

            File.Copy(pluginFile, newPluginFile, true);

            LoadPlugin(newPluginFile);
        }

        public bool CheckPluginFile(string pluginFile)
        {
            if (string.IsNullOrWhiteSpace(pluginFile))
                return false;
            if (!File.Exists(pluginFile))
                return false;

            Assembly assembly = Assembly.Load(File.ReadAllBytes(pluginFile));
            if (assembly == null)
                return false;

            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch
            {
                return false;
            }

            if (types.Length == 0)
                return false;

            // check if the file contains a plugin
            foreach (Type type in types)
            {
                if (type.GetInterface(typeof(IPlugin).Name) != null)
                    return true;
            }

            return false;
        }

        public void DeleteAllPlugins()
        {
            for (int index = Plugins.Count - 1; index >= 0; index--)
            {
                var pluginFile = Plugins[index].PluginFile;

                Plugins.RemoveAt(index);

                if (File.Exists(pluginFile))
                    File.Delete(pluginFile);
            }
        }

        public void DeletePlugin(string pluginFile)
        {
            if (string.IsNullOrWhiteSpace(pluginFile))
                return;

            for (int index = Plugins.Count - 1; index >= 0; index--)
            {
                if (Plugins[index].PluginFile.Equals(pluginFile, StringComparison.OrdinalIgnoreCase))
                    Plugins.RemoveAt(index);
            }

            if (File.Exists(pluginFile))
                File.Delete(pluginFile);
        }

        public void LoadPlugin(string pluginFile)
        {
            if (string.IsNullOrWhiteSpace(pluginFile))
                return;
            if (!File.Exists(pluginFile))
                return;

            Assembly assembly = Assembly.Load(File.ReadAllBytes(pluginFile));
            if (assembly == null)
                return;

            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch
            {
                return;
            }

            if (types.Length == 0)
                return;

            // check if the file contains one or more plugins
            foreach (Type type in types)
            {
                if (type.GetInterface(typeof(IAlertPlugin).Name) != null)
                {
                    var plugin = assembly.CreateInstance(type.FullName) as IAlertPlugin;
                    if (plugin != null && plugin.Enabled)
                    {
                        Plugins.Add(new PluginItem { Plugin = plugin, PluginFile = pluginFile, PluginType = nameof(IAlertPlugin) });
                    }
                }
            }
        }

        public void LoadPlugins(string folder, bool ClearExisting)
        {
            if (ClearExisting)
                Plugins.Clear();

            var pluginFolder = Path.Combine(folder, PLUGINFILE_FOLDER);
            if (string.IsNullOrWhiteSpace(pluginFolder))
                return;
            if (!Directory.Exists(pluginFolder))
                return;

            var pluginFiles = Directory.GetFiles(pluginFolder, $"{PLUGINFILE_PREFIX}*.{PLUGINFILE_EXTENSION}");
            foreach (var pluginFile in pluginFiles)
            {
                LoadPlugin(pluginFile);
            }
        }

        public void OpenConfigForm(string pluginCode, Window owner)
        {
            if (Plugins == null)
                return;

            var pluginItem = Plugins.FirstOrDefault(p => p.Plugin.PluginCode.Equals(pluginCode, StringComparison.OrdinalIgnoreCase));
            OpenConfigForm(pluginItem.Plugin, owner);
        }

        public void OpenConfigForm(IPlugin plugin, Window owner)
        {
            if (plugin == null || !plugin.Enabled || !plugin.HasConfigForm)
                return;

            plugin.OpenConfigForm(owner);
        }

        public void ProcessAlert(AlertType alertType, string profileName, string alertMessage)
        {
            if (Plugins == null || Plugins.Count == 0 || string.IsNullOrWhiteSpace(alertMessage))
                return;

            lock (lockObject)
            {
                var plugins = Plugins.Where(p => (p.PluginType is nameof(IAlertPlugin)) && (p.Plugin?.Enabled ?? false));
                if (plugins.Count() == 0)
                    return;

                foreach (var pluginItem in plugins)
                {
                    ((IAlertPlugin)pluginItem.Plugin).HandleAlert(alertType, profileName, alertMessage);
                }
            }
        }

        public static string PluginFolder
        {
            get
            {
                var folder = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? Environment.CurrentDirectory);
                return Path.Combine(folder, PLUGINFILE_FOLDER);
            }
        }
    }
}

using System.Windows;

namespace ArkServerManager.Plugin.Common
{
    public class PluginItem
    {
        public IPlugin Plugin
        {
            get;
            set;
        }

        public string PluginFile
        {
            get;
            set;
        }

        public string PluginType
        {
            get;
            set;
        }
    }
}

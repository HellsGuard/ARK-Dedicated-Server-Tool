namespace ArkServerManager.Plugin.Common
{
    public sealed class PluginItem
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

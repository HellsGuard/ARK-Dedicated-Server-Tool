using System;
using System.Runtime.Serialization;

namespace ArkServerManager.Plugin.Discord
{
    [DataContract]
    internal sealed class DiscordPluginConfig : Bindable
    {
        public const string CONFIG_FILENAME = "_asmdiscordplugin.cfg";

        public DiscordPluginConfig()
            : base()
        {
            LastCallHome = DateTime.MinValue;
            ConfigProfiles = new ObservableList<ConfigProfile>();
        }

        [DataMember]
        public DateTime LastCallHome
        {
            get;
            set;
        }

        [DataMember]
        public ObservableList<ConfigProfile> ConfigProfiles
        {
            get { return Get<ObservableList<ConfigProfile>>(); }
            set { Set(value); }
        }

        public override bool HasAnyChanges
        {
            get => base.HasChanges || (ConfigProfiles?.HasAnyChanges ?? false);
        }
    }
}

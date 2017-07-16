using System.Runtime.Serialization;

namespace ArkServerManager.Plugin.Discord
{
    [DataContract]
    internal sealed class ConfigProfile : Bindable
    {
        public ConfigProfile()
            : base()
        {
            Name = "New Discord Profile";
            ProfileNames = new ProfileNameValueList();
            AlertTypes = new AlertTypeValueList();
            DiscordWebhookUrl = string.Empty;
            DiscordBotName = string.Empty;
            DiscordUseTTS = false;
            PrefixMessageWithProfileName = false;
        }

        [DataMember]
        public string Name
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        [DataMember]
        public ProfileNameValueList ProfileNames
        {
            get { return Get<ProfileNameValueList>(); }
            set { Set(value); }
        }

        [DataMember]
        public AlertTypeValueList AlertTypes
        {
            get { return Get<AlertTypeValueList>(); }
            set { Set(value); }
        }

        [DataMember]
        public string DiscordWebhookUrl
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        [DataMember]
        public string DiscordBotName
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        [DataMember]
        public bool DiscordUseTTS
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [DataMember]
        public bool PrefixMessageWithProfileName
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public ConfigProfile Clone()
        {
            var clone = new ConfigProfile();
            clone.Name = this.Name;

            foreach (var profileName in this.ProfileNames)
            {
                clone.ProfileNames.Add(new ProfileNameValue(profileName.Value, profileName.OriginalValue) { HasChanges = profileName.HasChanges });
            }
            clone.ProfileNames.HasChanges = this.ProfileNames.HasChanges;
            foreach (var alertType in this.AlertTypes)
            {
                clone.AlertTypes.Add(new AlertTypeValue(alertType.Value, alertType.OriginalValue) { HasChanges = alertType.HasChanges });
            }
            clone.AlertTypes.HasChanges = this.AlertTypes.HasChanges;

            clone.DiscordWebhookUrl = this.DiscordWebhookUrl;
            clone.DiscordBotName = this.DiscordBotName;
            clone.DiscordUseTTS = this.DiscordUseTTS;
            clone.PrefixMessageWithProfileName = this.PrefixMessageWithProfileName;
            clone.HasChanges = this.HasChanges;
            return clone;
        }

        public void CopyFrom(ConfigProfile source)
        {
            if (source == null)
                return;

            try
            {
                this.BeginUpdate();

                this.Name = source.Name;

                this.ProfileNames.BeginUpdate();
                this.ProfileNames.Clear();
                foreach (var profileName in source.ProfileNames)
                {
                    this.ProfileNames.Add(new ProfileNameValue(profileName.Value, profileName.OriginalValue));
                }
                if (source.ProfileNames.HasChanges)
                    this.ProfileNames.HasChanges = true;
                this.ProfileNames.EndUpdate();

                this.AlertTypes.BeginUpdate();
                this.AlertTypes.Clear();
                foreach (var alertType in source.AlertTypes)
                {
                    this.AlertTypes.Add(new AlertTypeValue(alertType.Value, alertType.OriginalValue));
                }
                if (source.AlertTypes.HasChanges)
                    this.AlertTypes.HasChanges = true;
                this.AlertTypes.EndUpdate();

                this.DiscordWebhookUrl = source.DiscordWebhookUrl;
                this.DiscordBotName = source.DiscordBotName;
                this.DiscordUseTTS = source.DiscordUseTTS;
                this.PrefixMessageWithProfileName = source.PrefixMessageWithProfileName;

                if (source.HasChanges)
                    this.HasChanges = true;
            }
            finally
            {
                this.EndUpdate();
            }
        }
    }
}

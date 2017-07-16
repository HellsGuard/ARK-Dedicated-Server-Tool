using ArkServerManager.Plugin.Common;
using ArkServerManager.Plugin.Discord.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;

namespace ArkServerManager.Plugin.Discord
{
    public sealed class DiscordPlugin : IAlertPlugin
    {
        private const int MAX_MESSAGE_LENGTH = 2000;

        private Object lockObject = new Object();

        public DiscordPlugin()
        {
            LoadConfig();
        }

        private DiscordPluginConfig Config
        {
            get;
            set;
        }

        public bool Enabled => true;

        public string PluginCode => "_ASMDiscordPlugin";

        public string PluginName => "Discord Plugin (ASM)";

        public bool HasConfigForm => true;

        public void HandleAlert(AlertType alertType, string profileName, string alertMessage)
        {
            if (string.IsNullOrWhiteSpace(alertMessage))
                return;

            lock (lockObject)
            {
                var configProfile = Config.ConfigProfiles.FirstOrDefault(cp => cp.AlertTypes.Any(pn => pn.Value.Equals(alertType)) && cp.ProfileNames.Any(pn => pn.Value.Equals(profileName, StringComparison.OrdinalIgnoreCase)));
                if (configProfile == null)
                    return;

                if (!string.IsNullOrWhiteSpace(configProfile.DiscordWebhookUrl))
                {
                    var postData = String.Empty;
                    if (configProfile.DiscordUseTTS)
                        postData += $"&tts={configProfile.DiscordUseTTS}";
                    if (!string.IsNullOrWhiteSpace(configProfile.DiscordBotName))
                        postData += $"&username={configProfile.DiscordBotName.Replace("&", "_")}";
                    postData += $"&content=";
                    if (configProfile.PrefixMessageWithProfileName && !string.IsNullOrWhiteSpace(profileName))
                        postData += $"({profileName.Replace("&", "_")}) ";
                    postData += $"{alertMessage.Replace("&", "_")}";
                    if (postData.Length > MAX_MESSAGE_LENGTH)
                        postData = $"{postData.Substring(0, MAX_MESSAGE_LENGTH - 3)}...";

                    try
                    {
                        var data = Encoding.ASCII.GetBytes(postData);

                        var httpRequest = WebRequest.Create(configProfile.DiscordWebhookUrl);
                        httpRequest.Timeout = 30000;
                        httpRequest.Method = "POST";
                        httpRequest.ContentType = "application/x-www-form-urlencoded";
                        httpRequest.ContentLength = data.Length;

                        using (var stream = httpRequest.GetRequestStream())
                        {
                            stream.Write(data, 0, data.Length);
                        }

                        var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                        var responseString = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ERROR: {nameof(HandleAlert)}\r\n{ex.Message}");
                    }
                }
            }
        }

        private void LoadConfig()
        {
            try
            {
                Config = null;

                var configFile = Path.Combine(PluginHelper.PluginFolder, DiscordPluginConfig.CONFIG_FILENAME);
                Config = JsonUtils.DeserializeFromFile<DiscordPluginConfig>(configFile);

                if ((Config?.ConfigProfiles?.Count ?? 0) == 0)
                {
                    Config = new DiscordPluginConfig();

                    SaveConfig();
                }

                Config?.CommitChanges();
            }
            catch (Exception ex)
            {
                Config = new DiscordPluginConfig();
                Debug.WriteLine($"ERROR: {nameof(LoadConfig)}\r\n{ex.Message}");
            }
        }

        public void OpenConfigForm(Window owner)
        {
            var window = new ConfigWindow(this.Config);
            window.Owner = owner;

            var dialogResult = window.ShowDialog();
            if (dialogResult.HasValue && dialogResult.Value)
            {
                SaveConfig();
                LoadConfig();
            }
        }

        private void SaveConfig()
        {
            try
            {
                var configFile = Path.Combine(PluginHelper.PluginFolder, DiscordPluginConfig.CONFIG_FILENAME);
                JsonUtils.SerializeToFile(Config, configFile);
                Config?.CommitChanges();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(SaveConfig)}\r\n{ex.Message}");
            }
        }
    }
}

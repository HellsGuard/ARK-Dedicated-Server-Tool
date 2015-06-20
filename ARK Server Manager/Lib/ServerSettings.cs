using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ARK_Server_Manager.Lib
{
    public interface ISettingsBag
    {
        object this[string propertyName] { get; set; }
    }

    public class ServerSettings : ISettingsBag
    {
        public const string ProfileNameProperty = "ProfileName";

        public string ProfileName = Config.Default.DefaultServerProfileName;        
        public bool EnableGlobalVoiceChat = true;
        public bool EnableProximityChat = true;
        public bool EnableTributeDownloads = false;
        public bool EnableFlyerCarry = true;
        public bool EnableStructureDecay = false;
        public bool EnablePlayerLeaveNotifications = true;
        public bool EnablePlayerJoinedNotifications = true;
        public bool EnableHardcore = false;
        public bool EnablePVP = false;
        public bool AllowCrosshair = false;
        public bool AllowHUD = true;
        public bool AllowThirdPersonView = false;
        public bool AllowMapPlayerLocation = true;
        public string ServerPassword = "";
        public string AdminPassword = "";
        public int MaxPlayers = 5;
        public float DifficultyOffset = 0.25f;
        public float MaxStructuresVisible = 1300;
        public string ServerName = Config.Default.DefaultServerName;
        public int ServerPort = 27015;
        public string ServerIP = String.Empty;
        public string MOTD = String.Empty;

        public ServerSettings()
        {
            ServerPassword = PasswordUtils.GeneratePassword(16);
            AdminPassword = PasswordUtils.GeneratePassword(16);
        }

        public bool IsDirty
        {
            get;
            set;
        }

        public object this[string propertyName]
        {
            get { return this.GetType().GetField(propertyName).GetValue(this); }
            set { this.GetType().GetField(propertyName).SetValue(this, value); this.IsDirty = true; }
        }
    }

    public class ServerSettingsViewModel : ViewModelBase
    {
        ServerSettings settings;

        public ServerSettingsViewModel(ServerSettings settings)
        {
            this.settings = settings;
        }

        public string ProfileName {
            get { return Get<string>(settings); }
            set { Set(settings, value); }
        }
        public string ServerPassword
        {
            get { return Get<string>(settings); }
            set { Set(settings, value); }
        }

        public string AdminPassword
        {
            get { return Get<string>(settings); }
            set { Set(settings, value); }
        }

        public string ServerName
        {
            get { return Get<string>(settings); }
            set { Set(settings, value); }
        }

        public int ServerPort
        {
            get { return Get<int>(settings); }
            set { Set(settings, value); }
        }

        public string ServerIP
        {
            get { return Get<string>(settings); }
            set { Set(settings, value); }
        }
        public string MOTD
        {
            get { return Get<string>(settings); }
            set { Set(settings, value); }
        }
        public int MaxPlayers
        {
            get { return Get<int>(settings); }
            set { Set(settings, value); }
        }

        public float DifficultyOffset
        {
            get { return Get<float>(settings); }
            set { Set(settings, value); }
        }

        public float MaxStructuresVisible
        {
            get { return Get<float>(settings); }
            set { Set(settings, value); }
        }

        public bool ShowMapPlayerLocation
        {
            get { return Get<bool>(settings); }
            set { Set(settings, value); }
        }

        public bool EnableGlobalVoiceChat
        {
            get { return Get<bool>(settings); }
            set { Set(settings, value); }
        }

        public bool EnableProximityChat
        {
            get { return Get<bool>(settings); }
            set { Set(settings, value); }
        }

        public bool EnableTributeDownloads
        {
            get { return Get<bool>(settings); }
            set { Set(settings, value); }
        }

        public bool EnableFlyerCarry
        {
            get { return Get<bool>(settings); }
            set { Set(settings, value); }
        }

        public bool EnableStructureDecay
        {
            get { return Get<bool>(settings); }
            set { Set(settings, value); }
        }

        public bool EnablePlayerLeaveNotifications
        {
            get { return Get<bool>(settings); }
            set { Set(settings, value); }
        }
        public bool EnablePlayerJoinedNotifications
        {
            get { return Get<bool>(settings); }
            set { Set(settings, value); }
        }
        public bool EnableHardcore
        {
            get { return Get<bool>(settings); }
            set { Set(settings, value); }
        }
        public bool EnablePVP
        {
            get { return Get<bool>(settings); }
            set { Set(settings, value); }
        }
        public bool AllowCrosshair
        {
            get { return Get<bool>(settings); }
            set { Set(settings, value); }
        }
        public bool AllowHUD
        {
            get { return Get<bool>(settings); }
            set { Set(settings, value); }
        }
        public bool AllowThirdPersonView
        {
            get { return Get<bool>(settings); }
            set { Set(settings, value); }
        }

    }

    public class DifficultyOffsetValueConverter : IValueConverter
    {
        public const double MinValue = 50;
        public const double MaxValue = 300;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double scaledValue = System.Convert.ToDouble(value); ;
            var sliderValue = MinValue + (scaledValue * (MaxValue - MinValue));
            sliderValue = Math.Max(MinValue, Math.Min(MaxValue, sliderValue));
            return sliderValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var sliderValue = System.Convert.ToDouble(value);
            sliderValue = (double)sliderValue - (double)MinValue;
            var scaledValue = sliderValue / (double)(MaxValue - MinValue);
            scaledValue = Math.Max(0, Math.Min(1.0f, scaledValue));
            return scaledValue;
        }
    }
}

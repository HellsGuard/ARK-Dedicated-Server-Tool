using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Xml.Serialization;

namespace ARK_Server_Manager.Lib
{
    public interface ISettingsBag
    {
        object this[string propertyName] { get; set; }
    }

    [XmlRoot("ArkServerProfile")]
    [Serializable()]
    public class ServerSettings : ISettingsBag
    {
        public const string ProfileNameProperty = "ProfileName";

        public string ProfileName = Config.Default.DefaultServerProfileName;

        #region Server properties
        
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "GlobalVoiceChat")]
        public bool EnableGlobalVoiceChat = true;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ProximityVoiceChat")]
        public bool EnableProximityChat = true;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "NoTributeDownloads", InvertBoolean = true)]
        public bool EnableTributeDownloads = false;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "bAllowFlyerCarryPVE")]
        public bool EnableFlyerCarry = true;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "bDisableStructureDecayPVE", InvertBoolean = true)]
        public bool EnableStructureDecay = false;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "AlwaysNotifyPlayerLeft")]
        public bool EnablePlayerLeaveNotifications = true;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "DontAlwaysNotifyPlayerJoined", InvertBoolean = true)]
        public bool EnablePlayerJoinedNotifications = true;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerHardcore")]        
        public bool EnableHardcore = false;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerPVE", InvertBoolean = true)]        
        public bool EnablePVP = false;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerCrosshair")]        
        public bool AllowCrosshair = false;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerForceNoHud", InvertBoolean = true)]        
        public bool AllowHUD = true;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "AllowThirdPersonPlayer")]        
        public bool AllowThirdPersonView = false;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ShowMapPlayerLocation")]        
        public bool AllowMapPlayerLocation = true;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "EnablePVPGamma")]        
        public bool AllowPVPGamma = false;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerPassword")]        
        public string ServerPassword = "";

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerAdminPassword")]        
        public string AdminPassword = "";

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.GameSession, "MaxPlayers")]        
        public int MaxPlayers = 5;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "DifficultyOffset")]        
        public float DifficultyOffset = 0.25f;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "MaxStructuresInRange")]  
        public float MaxStructuresVisible = 1300;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.SessionSettings, "SessionName")]  
        public string ServerName = Config.Default.DefaultServerName;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.SessionSettings, "QueryPort")]  
        public int ServerPort = 27015;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.SessionSettings, "MultiHome")]
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.MultiHome, "MultiHome", WriteBoolValueIfNonEmpty = true)]
        public string ServerIP = String.Empty;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.MessageOfTheDay, "Message")]
        public string MOTD = String.Empty;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float HarvestAmountMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float PlayerCharacterWaterDrainMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float PlayerCharacterFoodDrainMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DinoCharacterFoodDrainMultiplier = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float PlayerCharacterStaminaDrainMultiplier = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DinoCharacterStaminaDrainMultiplier = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float PlayerCharacterHealthRecoveryMultiplier = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DinoCharacterHealthRecoveryMultiplier = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DinoCountMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float HarvestHealthMultiplier = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float PvEStructureDecayDestructionPeriod = 0;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DayCycleSpeedScale=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float NightTimeSpeedScale = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DayTimeSpeedScale=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DinoDamageMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float TamedDinoDamageMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float PlayerDamageMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float StructureDamageMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float PlayerResistanceMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DinoResistanceMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float TamedDinoResistanceMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float StructureResistanceMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float XPMultiplier = 1;

        public string SaveDirectory = String.Empty;
        public string InstallDirectory = String.Empty;


        public string ServerMap = Config.Default.DefaultServerMap;
        
        #endregion

        [XmlIgnore()]
        public bool IsDirty = true;

        public ServerSettings()
        {
            ServerPassword = PasswordUtils.GeneratePassword(16);
            AdminPassword = PasswordUtils.GeneratePassword(16);
            GetDefaultDirectories();
        }

        public static ServerSettings LoadFrom(string path)
        {
            if (Path.GetExtension(path) == Config.Default.ProfileExtension)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ServerSettings));
                using (var reader = File.OpenRead(path))
                {
                    var settings = (ServerSettings)serializer.Deserialize(reader);
                    settings.IsDirty = false;
                    return settings;
                }
            }
            else
            {
                IniFile iniFile = new IniFile(Path.GetDirectoryName(path));
                ServerSettings settings = new ServerSettings();
                iniFile.Deserialize(settings);
                settings.InstallDirectory = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(path)))));
                return settings;
            }
        }

        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(this.GetType());
            using (var writer = new StreamWriter(GetProfilePath()))
            {
                serializer.Serialize(writer, this);
            }

            WriteINIFile();
        }

        private string GetProfilePath()
        {
            return Path.Combine(Config.Default.ConfigDirectory, Path.ChangeExtension(this.ProfileName, Config.Default.ProfileExtension));
        }

        public void WriteINIFile()
        {
            string configDir = Path.Combine(this.InstallDirectory, Config.Default.ServerConfigRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(configDir));

            var iniFile = new IniFile(configDir);            
            iniFile.Serialize(this);

        }
        public string GetServerArgs()
        {
            var serverArgs = new StringBuilder();
            serverArgs.Append(this.ServerMap);

            // These are used to match the server to the profile.
            serverArgs.Append("?QueryPort=").Append(this.ServerPort);
            if (!String.IsNullOrEmpty(this.ServerIP))
            {
                serverArgs.Append("?MultiHome=").Append(this.ServerIP);
            }

#if false
            serverArgs.Append("?GlobalVoiceChat=").Append(this.EnableGlobalVoiceChat);
            serverArgs.Append("?ProximityChat=").Append(this.EnableProximityChat);
            serverArgs.Append("?NoTributeDownloads=").Append(!this.EnableTributeDownloads);
            serverArgs.Append("?bAllowFlyerCarryPVE=").Append(this.EnableFlyerCarry);
            serverArgs.Append("?bDisableStructureDecayPVE=").Append(!this.EnableStructureDecay);
            serverArgs.Append("?AlwaysNotifyPlayerLeft=").Append(this.EnablePlayerLeaveNotifications);
            serverArgs.Append("?DontAlwaysNotifyPlayerJoined=").Append(!this.EnablePlayerJoinedNotifications);
            serverArgs.Append("?ServerHardcore=").Append(this.EnableHardcore);           
            serverArgs.Append("?ServerPVE=").Append(!this.EnablePVP);

            serverArgs.Append("?ServerCrosshair=").Append(this.AllowCrosshair);
            serverArgs.Append("?ServerForceNoHud=").Append(!this.AllowHUD);
            serverArgs.Append("?AllowThirdPersonPlayer=").Append(this.AllowThirdPersonView);
            serverArgs.Append("?ShowMapPlayerLocation=").Append(this.AllowMapPlayerLocation);
            serverArgs.Append("?EnablePVPGamma=").Append(this.AllowPVPGamma);


            serverArgs.Append("?ServerPassword=").Append(this.ServerPassword);
            serverArgs.Append("?ServerAdminPassword=").Append(this.AdminPassword);
            serverArgs.Append("?MaxPlayers=").Append(this.MaxPlayers);
            serverArgs.Append("?DifficultyOffset=").Append(this.DifficultyOffset);
            serverArgs.Append("?MaxStructuresInRange=").Append(this.MaxStructuresVisible);
            
            serverArgs.Append("?SessionName=").Append('"').Append(this.ServerName).Append('"');
            serverArgs.Append("?QueryPort=").Append(this.ServerPort);
            if(!String.IsNullOrWhiteSpace(this.ServerIP))
            {
                serverArgs.Append("?MultiHome=").Append(this.ServerIP);
            }
            
            if(!String.IsNullOrWhiteSpace(this.SaveDirectory))
            {
                // TODO: This doesn't appear to work 
                serverArgs.Append("?\"AltSaveDirectoryName=").Append(this.SaveDirectory).Append('"');
            }

            if(!String.IsNullOrWhiteSpace(this.MOTD))
            {
                // TODO: This needs to go into the MessageOfTheDay INI file section
                serverArgs.Append("?MOTD=").Append('"').Append(this.MOTD).Append('"');
            }
#endif

            serverArgs.Append("?listen");
            serverArgs.Append(' ');
            serverArgs.Append(Config.Default.ServerCommandLineStandardArgs);

            return serverArgs.ToString();
        }

        public object this[string propertyName]
        {
            get { return this.GetType().GetField(propertyName).GetValue(this); }
            set { this.GetType().GetField(propertyName).SetValue(this, value); this.IsDirty = true; }
        }

        private void GetDefaultDirectories()
        {
            if (String.IsNullOrWhiteSpace(SaveDirectory))
            {
                SaveDirectory = Path.IsPathRooted(Config.Default.ServersInstallDir) ? Path.Combine(Config.Default.ServersInstallDir, Config.Default.ServerConfigRelativePath)
                                                                                    : Path.Combine(Config.Default.DataDir, Config.Default.ServersInstallDir, Config.Default.ServerConfigRelativePath);
            }

            if (String.IsNullOrWhiteSpace(InstallDirectory))
            {
                InstallDirectory = Path.IsPathRooted(Config.Default.ServersInstallDir) ? Path.Combine(Config.Default.ServersInstallDir)
                                                                                       : Path.Combine(Config.Default.DataDir, Config.Default.ServersInstallDir);
            }
        }
    }

    public class ServerSettingsViewModel : ViewModelBase
    {
        ServerSettings model;

        public ServerSettings Model
        {
            get { return model; }
        }

        public ServerSettingsViewModel(ServerSettings settings)
        {
            this.model = settings;
        }

        public bool IsDirty
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public string ProfileName {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }
        public string ServerPassword
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }

        public string AdminPassword
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }

        public string ServerName
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }

        public int ServerPort
        {
            get { return Get<int>(model); }
            set { Set(model, value); }
        }

        public string ServerIP
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }
        public string SaveDirectory
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }
        public string InstallDirectory
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }

        public string MOTD
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }
        public int MaxPlayers
        {
            get { return Get<int>(model); }
            set { Set(model, value); }
        }

        public float DifficultyOffset
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }

        public float MaxStructuresVisible
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }

        public bool ShowMapPlayerLocation
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }

        public bool EnableGlobalVoiceChat
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }

        public bool EnableProximityChat
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }

        public bool EnableTributeDownloads
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }

        public bool EnableFlyerCarry
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }

        public bool EnableStructureDecay
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }

        public bool EnablePlayerLeaveNotifications
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public bool EnablePlayerJoinedNotifications
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public bool EnableHardcore
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public bool EnablePVP
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public bool AllowCrosshair
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public bool AllowHUD
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public bool AllowThirdPersonView
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public bool AllowMapPlayerLocation
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
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

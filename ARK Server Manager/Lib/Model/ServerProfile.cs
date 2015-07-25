using ARK_Server_Manager.Lib.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Xml;
using System.Xml.Serialization;

namespace ARK_Server_Manager.Lib
{
    public interface ISettingsBag
    {
        object this[string propertyName] { get; set; }
    }

    [XmlRoot("ArkServerProfile")]
    [Serializable()]
    public class ServerProfile : DependencyObject
    {

        // Using a DependencyProperty as the backing store for ProfileName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProfileNameProperty = DependencyProperty.Register("ProfileName", typeof(string), typeof(ServerProfile), new PropertyMetadata(Config.Default.DefaultServerProfileName));

        public string ProfileName
        {
            get { return (string)GetValue(ProfileNameProperty); }
            set { SetValue(ProfileNameProperty, value); }
        }

        #region Server properties

        public static readonly DependencyProperty EnableGlobalVoiceChatProperty = DependencyProperty.Register("EnableGlobalVoiceChat", typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        public static readonly DependencyProperty EnableProximityChatProperty = DependencyProperty.Register("EnableProximityChat", typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        public static readonly DependencyProperty EnableTributeDownloadsProperty = DependencyProperty.Register("EnableTributeDownloads", typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public static readonly DependencyProperty EnableFlyerCarryProperty = DependencyProperty.Register("EnableFlyerCarry", typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        public static readonly DependencyProperty EnableStructureDecayProperty = DependencyProperty.Register("EnableStructureDecay", typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public static readonly DependencyProperty EnablePlayerLeaveNotificationsProperty = DependencyProperty.Register("EnablePlayerLeaveNotifications", typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        public static readonly DependencyProperty EnablePlayerJoinedNotificationsProperty = DependencyProperty.Register("EnablePlayerJoinedNotifications", typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        public static readonly DependencyProperty EnableHardcoreProperty = DependencyProperty.Register("EnableHardcore", typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public static readonly DependencyProperty EnablePVPProperty = DependencyProperty.Register("EnablePVP", typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public static readonly DependencyProperty AllowCrosshairProperty = DependencyProperty.Register("AllowCrosshair", typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public static readonly DependencyProperty AllowHUDProperty = DependencyProperty.Register("AllowHUD", typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        public static readonly DependencyProperty AllowThirdPersonViewProperty = DependencyProperty.Register("AllowThirdPersonView", typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public static readonly DependencyProperty AllowMapPlayerLocationProperty = DependencyProperty.Register("AllowMapPlayerLocation", typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        public static readonly DependencyProperty AllowPVPGammaProperty = DependencyProperty.Register("AllowPVPGamma", typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public static readonly DependencyProperty ServerPasswordProperty = DependencyProperty.Register("ServerPassword", typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty AdminPasswordProperty = DependencyProperty.Register("AdminPassword", typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty MaxPlayersProperty = DependencyProperty.Register("MaxPlayers", typeof(int), typeof(ServerProfile), new PropertyMetadata(5));
        public static readonly DependencyProperty DifficultyOffsetProperty = DependencyProperty.Register("DifficultyOffset", typeof(float), typeof(ServerProfile), new PropertyMetadata(0.25f));
        public static readonly DependencyProperty MaxStructuresVisibleProperty = DependencyProperty.Register("MaxStructuresVisible", typeof(float), typeof(ServerProfile), new PropertyMetadata(1300f));
        public static readonly DependencyProperty ServerNameProperty = DependencyProperty.Register("ServerName", typeof(string), typeof(ServerProfile), new PropertyMetadata(Config.Default.DefaultServerName));
        public static readonly DependencyProperty ServerPortProperty = DependencyProperty.Register("ServerPort", typeof(int), typeof(ServerProfile), new PropertyMetadata(27015));
        public static readonly DependencyProperty ServerConnectionPortProperty = DependencyProperty.Register("ServerConnectionPort", typeof(int), typeof(ServerProfile), new PropertyMetadata(7777));
        public static readonly DependencyProperty ServerIPProperty = DependencyProperty.Register("ServerIP", typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty MOTDProperty = DependencyProperty.Register("MOTD", typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty MOTDDurationProperty = DependencyProperty.Register("MOTDDuration", typeof(int), typeof(ServerProfile), new PropertyMetadata(20));
        public static readonly DependencyProperty EnableKickIdlePlayersProperty = DependencyProperty.Register("EnableKickIdlePlayers", typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public static readonly DependencyProperty KickIdlePlayersPeriodProperty = DependencyProperty.Register("KickIdlePlayersPeriod", typeof(float), typeof(ServerProfile), new PropertyMetadata(2400.0f));
        public static readonly DependencyProperty AutoSavePeriodMinutesProperty = DependencyProperty.Register("AutoSavePeriodMinutes", typeof(float), typeof(ServerProfile), new PropertyMetadata(15.0f));
        public static readonly DependencyProperty TamingSpeedMultiplierProperty = DependencyProperty.Register("TamingSpeedMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty HarvestAmountMultiplierProperty = DependencyProperty.Register("HarvestAmountMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty PlayerCharacterWaterDrainMultiplierProperty = DependencyProperty.Register("PlayerCharacterWaterDrainMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty PlayerCharacterFoodDrainMultiplierProperty = DependencyProperty.Register("PlayerCharacterFoodDrainMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty DinoCharacterFoodDrainMultiplierProperty = DependencyProperty.Register("DinoCharacterFoodDrainMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty PlayerCharacterStaminaDrainMultiplierProperty = DependencyProperty.Register("PlayerCharacterStaminaDrainMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty DinoCharacterStaminaDrainMultiplierProperty = DependencyProperty.Register("DinoCharacterStaminaDrainMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty PlayerCharacterHealthRecoveryMultiplierProperty = DependencyProperty.Register("PlayerCharacterHealthRecoveryMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty DinoCharacterHealthRecoveryMultiplierProperty = DependencyProperty.Register("DinoCharacterHealthRecoveryMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty DinoCountMultiplierProperty = DependencyProperty.Register("DinoCountMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty HarvestHealthMultiplierProperty = DependencyProperty.Register("HarvestHealthMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty PvEStructureDecayDestructionPeriodProperty = DependencyProperty.Register("PvEStructureDecayDestructionPeriod", typeof(float), typeof(ServerProfile), new PropertyMetadata(0f));
        public static readonly DependencyProperty PvEStructureDecayPeriodMultiplierProperty = DependencyProperty.Register("PvEStructureDecayPeriodMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty ResourcesRespawnPeriodMultiplierProperty = DependencyProperty.Register("ResourcesRespawnPeriodMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty ClampResourceHarvestDamageProperty = DependencyProperty.Register("ClampResourceHarvestDamage", typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public static readonly DependencyProperty DayCycleSpeedScaleProperty = DependencyProperty.Register("DayCycleSpeedScale", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty NightTimeSpeedScaleProperty = DependencyProperty.Register("NightTimeSpeedScale", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty DayTimeSpeedScaleProperty = DependencyProperty.Register("DayTimeSpeedScale", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty DinoDamageMultiplierProperty = DependencyProperty.Register("DinoDamageMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty TamedDinoDamageMultiplierProperty = DependencyProperty.Register("TamedDinoDamageMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty PlayerDamageMultiplierProperty = DependencyProperty.Register("PlayerDamageMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty StructureDamageMultiplierProperty = DependencyProperty.Register("StructureDamageMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty PlayerResistanceMultiplierProperty = DependencyProperty.Register("PlayerResistanceMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty DinoResistanceMultiplierProperty = DependencyProperty.Register("DinoResistanceMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty TamedDinoResistanceMultiplierProperty = DependencyProperty.Register("TamedDinoResistanceMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty StructureResistanceMultiplierProperty = DependencyProperty.Register("StructureResistanceMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty XPMultiplierProperty = DependencyProperty.Register("XPMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty EnableDinoSpawnsProperty = DependencyProperty.Register("EnableDinoSpawns", typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public static readonly DependencyProperty DinoSpawnsProperty = DependencyProperty.Register("DinoSpawns", typeof(DinoSpawnList), typeof(ServerProfile), new PropertyMetadata(null));
        public static readonly DependencyProperty EnableLevelProgressionsProperty = DependencyProperty.Register("EnableLevelProgressions", typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public static readonly DependencyProperty PlayerLevelsProperty = DependencyProperty.Register("PlayerLevels", typeof(LevelList), typeof(ServerProfile), new PropertyMetadata());
        public static readonly DependencyProperty DinoLevelsProperty = DependencyProperty.Register("DinoLevels", typeof(LevelList), typeof(ServerProfile), new PropertyMetadata());
        public static readonly DependencyProperty InstallDirectoryProperty = DependencyProperty.Register("InstallDirectory", typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty LastInstalledVersionProperty = DependencyProperty.Register("LastInstalledVersion", typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty AdditionalArgsProperty = DependencyProperty.Register("AdditionalArgs", typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty RCONEnabledProperty = DependencyProperty.Register("RCONEnabled", typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public static readonly DependencyProperty RCONPortProperty = DependencyProperty.Register("RCONPort", typeof(int), typeof(ServerProfile), new PropertyMetadata(32330));
        public static readonly DependencyProperty ServerMapProperty = DependencyProperty.Register("ServerMap", typeof(string), typeof(ServerProfile), new PropertyMetadata(Config.Default.DefaultServerMap));
        public static readonly DependencyProperty IsDirtyProperty = DependencyProperty.Register("IsDirty", typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public static readonly DependencyProperty GlobalSpoilingTimeMultiplierProperty = DependencyProperty.Register("GlobalSpoilingTimeMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty GlobalCorpseDecompositionTimeMultiplierProperty = DependencyProperty.Register("GlobalCorpseDecompositionTimeMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty OverrideMaxExperiencePointsDinoProperty = DependencyProperty.Register("OverrideMaxExperiencePointsDino", typeof(int), typeof(ServerProfile), new PropertyMetadata(100000));
        public static readonly DependencyProperty OverrideMaxExperiencePointsPlayerProperty = DependencyProperty.Register("OverrideMaxExperiencePointsPlayer", typeof(int), typeof(ServerProfile), new PropertyMetadata(100000));
        public static readonly DependencyProperty GlobalItemDecompositionTimeMultiplierProperty = DependencyProperty.Register("GlobalItemDecompositionTimeMultiplier", typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "GlobalVoiceChat")]
        public bool EnableGlobalVoiceChat
        {
            get { return (bool)GetValue(EnableGlobalVoiceChatProperty); }
            set { SetValue(EnableGlobalVoiceChatProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ProximityVoiceChat")]
        public bool EnableProximityChat
        {
            get { return (bool)GetValue(EnableProximityChatProperty); }
            set { SetValue(EnableProximityChatProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "NoTributeDownloads", InvertBoolean = true)]
        public bool EnableTributeDownloads
        {
            get { return (bool)GetValue(EnableTributeDownloadsProperty); }
            set { SetValue(EnableTributeDownloadsProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "bAllowFlyerCarryPVE")]
        public bool EnableFlyerCarry
        {
            get { return (bool)GetValue(EnableFlyerCarryProperty); }
            set { SetValue(EnableFlyerCarryProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "bDisableStructureDecayPVE", InvertBoolean = true)]
        public bool EnableStructureDecay
        {
            get { return (bool)GetValue(EnableStructureDecayProperty); }
            set { SetValue(EnableStructureDecayProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "AlwaysNotifyPlayerLeft")]
        public bool EnablePlayerLeaveNotifications
        {
            get { return (bool)GetValue(EnablePlayerLeaveNotificationsProperty); }
            set { SetValue(EnablePlayerLeaveNotificationsProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "DontAlwaysNotifyPlayerJoined", InvertBoolean = true)]
        public bool EnablePlayerJoinedNotifications
        {
            get { return (bool)GetValue(EnablePlayerJoinedNotificationsProperty); }
            set { SetValue(EnablePlayerJoinedNotificationsProperty, value); }
        }
        
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerHardcore")]        
        public bool EnableHardcore
        {
            get { return (bool)GetValue(EnableHardcoreProperty); }
            set { SetValue(EnableHardcoreProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerPVE", InvertBoolean = true)]
        public bool EnablePVP
        {
            get { return (bool)GetValue(EnablePVPProperty); }
            set { SetValue(EnablePVPProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerCrosshair")]
        public bool AllowCrosshair
        {
            get { return (bool)GetValue(AllowCrosshairProperty); }
            set { SetValue(AllowCrosshairProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerForceNoHud", InvertBoolean = true)]
        public bool AllowHUD
        {
            get { return (bool)GetValue(AllowHUDProperty); }
            set { SetValue(AllowHUDProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "AllowThirdPersonPlayer")]
        public bool AllowThirdPersonView
        {
            get { return (bool)GetValue(AllowThirdPersonViewProperty); }
            set { SetValue(AllowThirdPersonViewProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ShowMapPlayerLocation")]
        public bool AllowMapPlayerLocation
        {
            get { return (bool)GetValue(AllowMapPlayerLocationProperty); }
            set { SetValue(AllowMapPlayerLocationProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "EnablePVPGamma")]
        public bool AllowPVPGamma
        {
            get { return (bool)GetValue(AllowPVPGammaProperty); }
            set { SetValue(AllowPVPGammaProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerPassword")]        
        public string ServerPassword
        {
            get { return (string)GetValue(ServerPasswordProperty); }
            set { SetValue(ServerPasswordProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerAdminPassword")]
        public string AdminPassword
        {
            get { return (string)GetValue(AdminPasswordProperty); }
            set { SetValue(AdminPasswordProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.GameSession, "MaxPlayers")]        
        public int MaxPlayers
        {
            get { return (int)GetValue(MaxPlayersProperty); }
            set { SetValue(MaxPlayersProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "DifficultyOffset")]
        public float DifficultyOffset
        {
            get { return (float)GetValue(DifficultyOffsetProperty); }
            set { SetValue(DifficultyOffsetProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "MaxStructuresInRange")]
        public float MaxStructuresVisible
        {
            get { return (float)GetValue(MaxStructuresVisibleProperty); }
            set { SetValue(MaxStructuresVisibleProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.SessionSettings, "SessionName")]  
        public string ServerName
        {
            get { return (string)GetValue(ServerNameProperty); }
            set { SetValue(ServerNameProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.SessionSettings, "QueryPort")]
        public int ServerPort
        {
            get { return (int)GetValue(ServerPortProperty); }
            set { SetValue(ServerPortProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.SessionSettings, "Port")]
        public int ServerConnectionPort
        {
            get { return (int)GetValue(ServerConnectionPortProperty); }
            set { SetValue(ServerConnectionPortProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.SessionSettings, "MultiHome")]
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.MultiHome, "MultiHome", WriteBoolValueIfNonEmpty = true)]
        public string ServerIP
        {
            get { return (string)GetValue(ServerIPProperty); }
            set { SetValue(ServerIPProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.MessageOfTheDay, "Message", ClearSection = true)]
        public string MOTD
        {
            get { return (string)GetValue(MOTDProperty); }
            set { SetValue(MOTDProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.MessageOfTheDay, "Duration")]
        public int MOTDDuration
        {
            get { return (int)GetValue(MOTDDurationProperty); }
            set { SetValue(MOTDDurationProperty, value); }
        }

        public bool EnableKickIdlePlayers
        {
            get { return (bool)GetValue(EnableKickIdlePlayersProperty); }
            set { SetValue(EnableKickIdlePlayersProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, ConditionedOn = "EnableKickIdlePlayers")]
        public float KickIdlePlayersPeriod
        {
            get { return (float)GetValue(KickIdlePlayersPeriodProperty); }
            set { SetValue(KickIdlePlayersPeriodProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float AutoSavePeriodMinutes
        {
            get { return (float)GetValue(AutoSavePeriodMinutesProperty); }
            set { SetValue(AutoSavePeriodMinutesProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float TamingSpeedMultiplier
        {
            get { return (float)GetValue(TamingSpeedMultiplierProperty); }
            set { SetValue(TamingSpeedMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float HarvestAmountMultiplier
        {
            get { return (float)GetValue(HarvestAmountMultiplierProperty); }
            set { SetValue(HarvestAmountMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float PlayerCharacterWaterDrainMultiplier
        {
            get { return (float)GetValue(PlayerCharacterWaterDrainMultiplierProperty); }
            set { SetValue(PlayerCharacterWaterDrainMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float PlayerCharacterFoodDrainMultiplier
        {
            get { return (float)GetValue(PlayerCharacterFoodDrainMultiplierProperty); }
            set { SetValue(PlayerCharacterFoodDrainMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float DinoCharacterFoodDrainMultiplier
        {
            get { return (float)GetValue(DinoCharacterFoodDrainMultiplierProperty); }
            set { SetValue(DinoCharacterFoodDrainMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float PlayerCharacterStaminaDrainMultiplier
        {
            get { return (float)GetValue(PlayerCharacterStaminaDrainMultiplierProperty); }
            set { SetValue(PlayerCharacterStaminaDrainMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float DinoCharacterStaminaDrainMultiplier
        {
            get { return (float)GetValue(DinoCharacterStaminaDrainMultiplierProperty); }
            set { SetValue(DinoCharacterStaminaDrainMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float PlayerCharacterHealthRecoveryMultiplier
        {
            get { return (float)GetValue(PlayerCharacterHealthRecoveryMultiplierProperty); }
            set { SetValue(PlayerCharacterHealthRecoveryMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float DinoCharacterHealthRecoveryMultiplier
        {
            get { return (float)GetValue(DinoCharacterHealthRecoveryMultiplierProperty); }
            set { SetValue(DinoCharacterHealthRecoveryMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float DinoCountMultiplier
        {
            get { return (float)GetValue(DinoCountMultiplierProperty); }
            set { SetValue(DinoCountMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float HarvestHealthMultiplier
        {
            get { return (float)GetValue(HarvestHealthMultiplierProperty); }
            set { SetValue(HarvestHealthMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float PvEStructureDecayDestructionPeriod
        {
            get { return (float)GetValue(PvEStructureDecayDestructionPeriodProperty); }
            set { SetValue(PvEStructureDecayDestructionPeriodProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float PvEStructureDecayPeriodMultiplier
        {
            get { return (float)GetValue(PvEStructureDecayPeriodMultiplierProperty); }
            set { SetValue(PvEStructureDecayPeriodMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float ResourcesRespawnPeriodMultiplier
        {
            get { return (float)GetValue(ResourcesRespawnPeriodMultiplierProperty); }
            set { SetValue(ResourcesRespawnPeriodMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public bool ClampResourceHarvestDamage
        {
            get { return (bool)GetValue(ClampResourceHarvestDamageProperty); }
            set { SetValue(ClampResourceHarvestDamageProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float DayCycleSpeedScale
        {
            get { return (float)GetValue(DayCycleSpeedScaleProperty); }
            set { SetValue(DayCycleSpeedScaleProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float NightTimeSpeedScale
        {
            get { return (float)GetValue(NightTimeSpeedScaleProperty); }
            set { SetValue(NightTimeSpeedScaleProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float DayTimeSpeedScale
        {
            get { return (float)GetValue(DayTimeSpeedScaleProperty); }
            set { SetValue(DayTimeSpeedScaleProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float DinoDamageMultiplier
        {
            get { return (float)GetValue(DinoDamageMultiplierProperty); }
            set { SetValue(DinoDamageMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float TamedDinoDamageMultiplier
        {
            get { return (float)GetValue(TamedDinoDamageMultiplierProperty); }
            set { SetValue(TamedDinoDamageMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float PlayerDamageMultiplier
        {
            get { return (float)GetValue(PlayerDamageMultiplierProperty); }
            set { SetValue(PlayerDamageMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float StructureDamageMultiplier
        {
            get { return (float)GetValue(StructureDamageMultiplierProperty); }
            set { SetValue(StructureDamageMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float PlayerResistanceMultiplier
        {
            get { return (float)GetValue(PlayerResistanceMultiplierProperty); }
            set { SetValue(PlayerResistanceMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float DinoResistanceMultiplier
        {
            get { return (float)GetValue(DinoResistanceMultiplierProperty); }
            set { SetValue(DinoResistanceMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float TamedDinoResistanceMultiplier
        {
            get { return (float)GetValue(TamedDinoResistanceMultiplierProperty); }
            set { SetValue(TamedDinoResistanceMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float StructureResistanceMultiplier
        {
            get { return (float)GetValue(StructureResistanceMultiplierProperty); }
            set { SetValue(StructureResistanceMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float XPMultiplier
        {
            get { return (float)GetValue(XPMultiplierProperty); }
            set { SetValue(XPMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.Game, IniFileSections.GameMode)]
        public float GlobalSpoilingTimeMultiplier
        {
            get { return (float)GetValue(GlobalSpoilingTimeMultiplierProperty); }
            set { SetValue(GlobalSpoilingTimeMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.Game, IniFileSections.GameMode)]
        public float GlobalItemDecompositionTimeMultiplier
        {
            get { return (float)GetValue(GlobalItemDecompositionTimeMultiplierProperty); }
            set { SetValue(GlobalItemDecompositionTimeMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.Game, IniFileSections.GameMode)]
        public float GlobalCorpseDecompositionTimeMultiplier
        {
            get { return (float)GetValue(GlobalCorpseDecompositionTimeMultiplierProperty); }
            set { SetValue(GlobalCorpseDecompositionTimeMultiplierProperty, value); }
        }

        [IniFileEntry(IniFiles.Game, IniFileSections.GameMode)]
        public int OverrideMaxExperiencePointsPlayer
        {
            get { return (int)GetValue(OverrideMaxExperiencePointsPlayerProperty); }
            set { SetValue(OverrideMaxExperiencePointsPlayerProperty, value); }
        }

        [IniFileEntry(IniFiles.Game, IniFileSections.GameMode)]
        public int OverrideMaxExperiencePointsDino
        {
            get { return (int)GetValue(OverrideMaxExperiencePointsDinoProperty); }
            set { SetValue(OverrideMaxExperiencePointsDinoProperty, value); }
        }             
        
        public bool EnableDinoSpawns
        {
            get { return (bool)GetValue(EnableDinoSpawnsProperty); }
            set { SetValue(EnableDinoSpawnsProperty, value); }
        }

        [IniFileEntry(IniFiles.Game, IniFileSections.GameMode, "DinoSpawnWeightMultipliers", ConditionedOn = "EnableDinoSpawns")]
        public DinoSpawnList DinoSpawns
        {
            get { return (DinoSpawnList)GetValue(DinoSpawnsProperty); }
            set { SetValue(DinoSpawnsProperty, value); }
        }

        public bool EnableLevelProgressions
        {
            get { return (bool)GetValue(EnableLevelProgressionsProperty); }
            set { SetValue(EnableLevelProgressionsProperty, value); }
        }

        public LevelList PlayerLevels
        {
            get { return (LevelList)GetValue(PlayerLevelsProperty); }
            set { SetValue(PlayerLevelsProperty, value); }
        }

        public LevelList DinoLevels
        {
            get { return (LevelList)GetValue(DinoLevelsProperty); }
            set { SetValue(DinoLevelsProperty, value); }
        }

        public string InstallDirectory
        {
            get { return (string)GetValue(InstallDirectoryProperty); }
            set { SetValue(InstallDirectoryProperty, value); }
        }

        public string LastInstalledVersion
        {
            get { return (string)GetValue(LastInstalledVersionProperty); }
            set { SetValue(LastInstalledVersionProperty, value); }
        }

        public string AdditionalArgs
        {
            get { return (string)GetValue(AdditionalArgsProperty); }
            set { SetValue(AdditionalArgsProperty, value); }
        }

        public bool RCONEnabled
        {
            get { return (bool)GetValue(RCONEnabledProperty); }
            set { SetValue(RCONEnabledProperty, value); }
        }

        public int RCONPort
        {
            get { return (int)GetValue(RCONPortProperty); }
            set { SetValue(RCONPortProperty, value); }
        }

        public string ServerMap
        {
            get { return (string)GetValue(ServerMapProperty); }
            set { SetValue(ServerMapProperty, value); }
        }
        
        #endregion

        [XmlIgnore()]
        public bool IsDirty
        {
            get { return (bool)GetValue(IsDirtyProperty); }
            set { SetValue(IsDirtyProperty, value); }
        }

        [XmlIgnore()]
        private string LastSaveLocation = String.Empty;

        private ServerProfile()
        {
            ServerPassword = PasswordUtils.GeneratePassword(16);
            AdminPassword = PasswordUtils.GeneratePassword(16);
            this.DinoLevels = new LevelList();
            this.PlayerLevels = new LevelList();
            this.DinoSpawns = new DinoSpawnList();
            GetDefaultDirectories();
        }

        public void ResetDinoSpawnsToDefault()
        {            
            this.DinoSpawns.Clear();
            this.DinoSpawns.AddRange(GameData.DinoSpawns);
        }

        public enum LevelProgression
        {
            Player,
            Dino
        };

        public void ResetLevelProgressionToDefault(LevelProgression levelProgression)
        {
            LevelList list = GetLevelList(levelProgression);

            list.Clear();
            list.AddRange(GameData.LevelProgression);
        }

        public void ClearLevelProgression(LevelProgression levelProgression)
        {
            var list = GetLevelList(levelProgression);
            list.Clear();
            list.Add(new Level { LevelIndex = 0, XPRequired = 1, EngramPoints = 0 });
            list.UpdateTotals();
        }

        private LevelList GetLevelList(LevelProgression levelProgression)
        {
            LevelList list = null;
            switch (levelProgression)
            {
                case LevelProgression.Player:
                    list = this.PlayerLevels;
                    break;

                case LevelProgression.Dino:
                    list = this.DinoLevels;
                    break;

                default:
                    throw new ArgumentException("Invalid level progression type specified.");
            }
            return list;
        }

        public static ServerProfile LoadFrom(string path)
        {
            ServerProfile settings;
            if (Path.GetExtension(path) == Config.Default.ProfileExtension)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ServerProfile));
                
                using (var reader = File.OpenRead(path))
                {
                    var streamReader = new StreamReader(reader, System.Text.Encoding.UTF8);
                    settings = (ServerProfile)serializer.Deserialize(streamReader);
                    settings.IsDirty = false;
                }

                if (settings.DinoSpawns.Count == 0)
                {
                    settings.ResetDinoSpawnsToDefault();
                    settings.EnableDinoSpawns = false;
                }

                if (settings.PlayerLevels.Count == 0)
                {
                    settings.ResetLevelProgressionToDefault(LevelProgression.Player);
                    settings.ResetLevelProgressionToDefault(LevelProgression.Dino);
                    settings.EnableLevelProgressions = false;
                }

                //
                // Since these are not inserted the normal way, we force a recomputation here.
                //
                settings.PlayerLevels.UpdateTotals();
                settings.DinoLevels.UpdateTotals();
            }
            else
            {
                settings = LoadFromINIFiles(path);

                if (settings.DinoSpawns.Count == 0)
                {
                    settings.ResetDinoSpawnsToDefault();
                    settings.EnableDinoSpawns = false;
                }
                else
                {
                    settings.EnableDinoSpawns = true;
                }

                if (settings.PlayerLevels.Count == 0)
                {
                    settings.ResetLevelProgressionToDefault(LevelProgression.Player);
                    settings.ResetLevelProgressionToDefault(LevelProgression.Dino);
                    settings.EnableLevelProgressions = false;
                }
                else
                {
                    settings.EnableLevelProgressions = true;
                }
            }


            settings.LastSaveLocation = path;
            return settings;
        }

        private static ServerProfile LoadFromINIFiles(string path)
        {
            var file = IniFile.ReadFromFile(new IniDefinition(), path);
            ServerProfile settings;
            SystemIniFile iniFile = new SystemIniFile(Path.GetDirectoryName(path));
            settings = new ServerProfile();
            iniFile.Deserialize(settings);

            var strings = iniFile.IniReadSection(IniFileSections.GameMode, IniFiles.Game);

            // 
            // Levels
            //
            var levelRampOverrides = strings.Where(s => s.StartsWith("LevelExperienceRampOverrides=")).ToArray();
            var engramPointOverrides = strings.Where(s => s.StartsWith("OverridePlayerLevelEngramPoints="));
            if (levelRampOverrides.Length > 0)
            {
                settings.PlayerLevels = LevelList.FromINIValues(levelRampOverrides[0], engramPointOverrides);

                if(levelRampOverrides.Length > 1)
                {
                    settings.DinoLevels = LevelList.FromINIValues(levelRampOverrides[1], null);
                }
            }
          
            settings.InstallDirectory = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(path)))));
            return settings;
        }

        public void Save()
        {           
            //
            // Save the profile
            //
            XmlSerializer serializer = new XmlSerializer(this.GetType());
            using (var writer = new StreamWriter(File.Open(GetProfilePath(), FileMode.Create), System.Text.Encoding.UTF8))
            {
                serializer.Serialize(writer, this);
            }

            //
            // Write the INI files
            //
            SaveINIFiles();

            //
            // If this was a rename, remove the old profile after writing the new one.
            //
            if(!String.Equals(GetProfilePath(), this.LastSaveLocation))
            {
                try
                {
                    if (File.Exists(this.LastSaveLocation))
                    {
                        File.Delete(this.LastSaveLocation);
                    }
                }
                catch(IOException)
                {
                    // We tried...
                }

                this.LastSaveLocation = GetProfilePath();
            }
        }

        public string GetProfilePath()
        {
            return Path.Combine(Config.Default.ConfigDirectory, Path.ChangeExtension(this.ProfileName, Config.Default.ProfileExtension));
        }

        public void SaveINIFiles()
        {
            string configDir = Path.Combine(this.InstallDirectory, Config.Default.ServerConfigRelativePath);
            Directory.CreateDirectory(configDir);

            var iniFile = new SystemIniFile(configDir);            
            iniFile.Serialize(this);

            var values = iniFile.IniReadSection(IniFileSections.GameMode, IniFiles.Game);
            var filteredValues = values.Where(s => !s.StartsWith("LevelExperienceRampOverrides=") &&
                                                   !s.StartsWith("OverridePlayerLevelEngramPoints=")).ToList();
            if(this.EnableLevelProgressions)            
            {
                //
                // These must be added in this order: Player, then Dinos, per the ARK INI file format.
                //
                filteredValues.Add(this.PlayerLevels.ToINIValueForXP());
                filteredValues.Add(this.DinoLevels.ToINIValueForXP());
                filteredValues.AddRange(this.PlayerLevels.ToINIValuesForEngramPoints());
            }

            iniFile.IniWriteSection(IniFileSections.GameMode, filteredValues.ToArray(), IniFiles.Game);
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

            if(this.RCONEnabled)
            {
                serverArgs.Append("?RCONEnabled=true");
                serverArgs.Append("?RCONPort=").Append(this.RCONPort);
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

            // Currently this setting does not seem to get picked up from the INI file.
            serverArgs.Append("?MaxPlayers=").Append(this.MaxPlayers);
            serverArgs.Append("?Port=").Append(this.ServerConnectionPort);

            serverArgs.Append("?listen");

            serverArgs.Append(this.AdditionalArgs);

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
            if (String.IsNullOrWhiteSpace(InstallDirectory))
            {
                InstallDirectory = Path.IsPathRooted(Config.Default.ServersInstallDir) ? Path.Combine(Config.Default.ServersInstallDir)
                                                                                       : Path.Combine(Config.Default.DataDir, Config.Default.ServersInstallDir);
            }
        }

        internal static ServerProfile FromDefaults()
        {
            var settings = new ServerProfile();
            settings.ResetDinoSpawnsToDefault();
            settings.ResetLevelProgressionToDefault(LevelProgression.Player);
            settings.ResetLevelProgressionToDefault(LevelProgression.Dino);
            return settings;
        }
    }
}

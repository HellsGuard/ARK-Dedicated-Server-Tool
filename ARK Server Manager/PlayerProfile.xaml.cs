using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ARK_Server_Manager.Lib.ViewModel;
using ARK_Server_Manager.Lib.ViewModel.RCON;
using ArkData;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for PlayerProfile.xaml
    /// </summary>
    public partial class PlayerProfile : Window
    {
        public PlayerProfile(PlayerInfo player, String serverFolder)
        {
            InitializeComponent();

            this.Player = player;
            this.ServerFolder = serverFolder;
            this.DataContext = this;
        }

        public PlayerInfo Player
        {
            get;
            private set;
        }

        public String ServerFolder
        {
            get;
            private set;
        }

        public Player ArkDataPlayer => Player?.ArkData;

        public Tribe ArkDataTribe => Player?.ArkData?.Tribe;

        public String CreatedDate => ArkDataPlayer?.FileCreated.ToString("G");

        public Boolean IsTribeOwner => ArkDataPlayer != null && ArkDataTribe != null && ArkDataTribe.OwnerId == ArkDataPlayer.Id;

        public String PlayerLink => String.IsNullOrWhiteSpace(ServerFolder) ? null : $"/select, {Path.Combine(ServerFolder, Config.Default.SavedArksRelativePath, $"{Player.SteamId}.arkprofile")}";

        public String ProfileLink => ArkDataPlayer?.ProfileUrl;

        public String TribeLink => String.IsNullOrWhiteSpace(ServerFolder) || ArkDataTribe == null ? null : $"/select, {Path.Combine(ServerFolder, Config.Default.SavedArksRelativePath, $"{ArkDataTribe.Id}.arktribe")}";

        public String TribeOwner => ArkDataTribe != null && ArkDataTribe.Owner != null ? $"{ArkDataTribe.Owner.SteamName} ({ArkDataTribe.Owner.CharacterName})" : null;

        public String UpdatedDate => ArkDataPlayer?.FileUpdated.ToString("G");

        public String WindowTitle => $"Player Profile - {Player.SteamName}";

        public ICommand DirectLinkCommand
        {
            get
            {
                return new RelayCommand<String>(
                    execute: (action) =>
                    {
                        if (String.IsNullOrWhiteSpace(action)) return;
                        Process.Start(action);
                    },
                    canExecute: (action) => true
                );
            }
        }

        public ICommand ExplorerLinkCommand
        {
            get
            {
                return new RelayCommand<String>(
                    execute: (action) =>
                    {
                        if (String.IsNullOrWhiteSpace(action)) return;
                        Process.Start("explorer.exe", action);
                    },
                    canExecute: (action) => true
                );
            }
        }
    }
}

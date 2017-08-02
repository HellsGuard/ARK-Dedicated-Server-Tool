using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ARK_Server_Manager.Lib;
using ARK_Server_Manager.Lib.ViewModel;
using ARK_Server_Manager.Lib.ViewModel.RCON;
using ArkData;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for PlayerProfileWindow.xaml
    /// </summary>
    public partial class PlayerProfileWindow : Window
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public PlayerProfileWindow(PlayerInfo player, String serverFolder)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

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

        public Player PlayerData => Player?.PlayerData;

        public Tribe TribeData => Player?.PlayerData?.Tribe;

        public String CreatedDate => PlayerData?.FileCreated.ToString("G");

        public Boolean IsTribeOwner => PlayerData != null && TribeData != null && TribeData.OwnerId == PlayerData.Id;

        public String PlayerLink => String.IsNullOrWhiteSpace(ServerFolder) ? null : $"/select, {Path.Combine(ServerFolder, $"{Player.SteamId}{Config.Default.PlayerFileExtension}")}";

        public String ProfileLink => PlayerData?.ProfileUrl;

        public String TribeLink => String.IsNullOrWhiteSpace(ServerFolder) || TribeData == null ? null : $"/select, {Path.Combine(ServerFolder, $"{TribeData.Id}{Config.Default.TribeFileExtension}")}";

        public String TribeOwner => TribeData != null && TribeData.Owner != null ? $"{TribeData.Owner.SteamName} ({TribeData.Owner.CharacterName})" : null;

        public String UpdatedDate => PlayerData?.FileUpdated.ToString("G");

        public String WindowTitle => String.Format(_globalizer.GetResourceString("Profile_WindowTitle_Player"), Player.SteamName);

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

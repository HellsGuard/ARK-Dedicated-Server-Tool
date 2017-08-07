using ArkData;
using NLog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ARK_Server_Manager.Lib.ViewModel.RCON
{
    public class PlayerInfo : DependencyObject
    {
        private static readonly ConcurrentDictionary<long, BitmapImage> avatarImages = new ConcurrentDictionary<long, BitmapImage>();

        private Logger _logger;
        private bool _dataUpdated = true;

        public PlayerInfo(Logger logger)
        {
            _logger = logger;
        }

        public static readonly DependencyProperty SteamIDProperty = DependencyProperty.Register(nameof(SteamId), typeof(long), typeof(PlayerInfo), new PropertyMetadata(0L));
        public static readonly DependencyProperty SteamNameProperty = DependencyProperty.Register(nameof(SteamName), typeof(string), typeof(PlayerInfo), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty CharacterNameProperty = DependencyProperty.Register(nameof(CharacterName), typeof(string), typeof(PlayerInfo), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty AvatarImageProperty = DependencyProperty.Register(nameof(AvatarImage), typeof(ImageSource), typeof(PlayerInfo), new PropertyMetadata(null));
        public static readonly DependencyProperty IsOnlineProperty = DependencyProperty.Register(nameof(IsOnline), typeof(bool), typeof(PlayerInfo), new PropertyMetadata(false));
        public static readonly DependencyProperty IsBannedProperty = DependencyProperty.Register(nameof(IsBanned), typeof(bool), typeof(PlayerInfo), new PropertyMetadata(false));
        public static readonly DependencyProperty IsWhitelistedProperty = DependencyProperty.Register(nameof(IsWhitelisted), typeof(bool), typeof(PlayerInfo), new PropertyMetadata(false));
        public static readonly DependencyProperty TribeNameProperty = DependencyProperty.Register(nameof(TribeName), typeof(string), typeof(PlayerInfo), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty LastUpdatedProperty = DependencyProperty.Register(nameof(LastUpdated), typeof(DateTime), typeof(PlayerInfo), new PropertyMetadata(DateTime.MinValue));
        public static readonly DependencyProperty HasBanProperty = DependencyProperty.Register(nameof(HasBan), typeof(bool), typeof(PlayerInfo), new PropertyMetadata(false));
        public static readonly DependencyProperty IsValidProperty = DependencyProperty.Register(nameof(IsValid), typeof(bool), typeof(PlayerInfo), new PropertyMetadata(true));
        public static readonly DependencyProperty PlayerDataProperty = DependencyProperty.Register(nameof(PlayerData), typeof(Player), typeof(PlayerInfo), new PropertyMetadata(null));

        public long SteamId
        {
            get { return (long)GetValue(SteamIDProperty); }
            set { SetValue(SteamIDProperty, value); }
        }
        public string SteamName
        {
            get { return (string)GetValue(SteamNameProperty); }
            set
            {
                SetValue(SteamNameProperty, value);

                SteamNameFilterString = value?.ToLower();
            }
        }
        public string SteamNameFilterString
        {
            get;
            private set;
        }
        public string CharacterName
        {
            get { return (string)GetValue(CharacterNameProperty); }
            set
            {
                SetValue(CharacterNameProperty, value);

                CharacterNameFilterString = value?.ToLower();
            }
        }
        public string CharacterNameFilterString
        {
            get;
            private set;
        }
        public ImageSource AvatarImage
        {
            get { return (ImageSource)GetValue(AvatarImageProperty); }
            set { SetValue(AvatarImageProperty, value); }
        }
        public bool IsOnline
        {
            get { return (bool)GetValue(IsOnlineProperty); }
            set { SetValue(IsOnlineProperty, value); }
        }
        public bool IsBanned
        {
            get { return (bool)GetValue(IsBannedProperty); }
            set { SetValue(IsBannedProperty, value); }
        }
        public bool IsWhitelisted
        {
            get { return (bool)GetValue(IsWhitelistedProperty); }
            set { SetValue(IsWhitelistedProperty, value); }
        }
        public string TribeName
        {
            get { return (string)GetValue(TribeNameProperty); }
            set
            {
                SetValue(TribeNameProperty, value);

                TribeNameFilterString = value?.ToLower();
            }
        }
        public string TribeNameFilterString
        {
            get;
            private set;
        }
        public DateTime LastUpdated
        {
            get { return (DateTime)GetValue(LastUpdatedProperty); }
            set { SetValue(LastUpdatedProperty, value); }
        }
        public bool HasBan
        {
            get { return (bool)GetValue(HasBanProperty); }
            set { SetValue(HasBanProperty, value); }
        }
        public bool IsValid
        {
            get { return (bool)GetValue(IsValidProperty); }
            set { SetValue(IsValidProperty, value); }
        }
        public Player PlayerData
        {
            get { return (Player)GetValue(PlayerDataProperty); }
            set { SetValue(PlayerDataProperty, value); }
        }

        internal async Task UpdateDataAsync(Player playerData)
        {
            if (!_dataUpdated)
                return;
            _dataUpdated = false;

            try
            {
                this.PlayerData = playerData;
                this.LastUpdated = playerData.FileUpdated;
                this.TribeName = playerData.Tribe?.Name;
                this.CharacterName = playerData.CharacterName;
                this.HasBan = playerData.CommunityBanned || playerData.VACBanned;

                if (PlayerInfo.avatarImages.TryGetValue(this.SteamId, out BitmapImage avatarImage))
                {
                    _logger?.Debug($"Avatar image for {this.SteamId} found.");
                }
                else
                {
                    var localImageFile = Path.Combine(Path.GetTempPath(), $"ASM.{this.SteamId}.tmp");

                    // check for a valid URL.
                    if (!String.IsNullOrWhiteSpace(playerData.AvatarUrl))
                    {
                        try
                        {
                            using (var client = new WebClient())
                            {
                                await client.DownloadFileTaskAsync(playerData.AvatarUrl, localImageFile);
                            }
                            _logger.Debug($"{nameof(UpdateDataAsync)} - downloaded avatar image for {this.SteamId} from {playerData.AvatarUrl}.");
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"{nameof(UpdateDataAsync)} - failed to download avatar image for {this.SteamId} from {playerData.AvatarUrl}. {ex.Message}\r\n{ex.StackTrace}");
                        }
                    }

                    if (File.Exists(localImageFile))
                    {
                        avatarImage = new BitmapImage(new Uri(localImageFile, UriKind.Absolute));
                        PlayerInfo.avatarImages[this.SteamId] = avatarImage;
                        _logger.Debug($"Avatar image for {this.SteamId} found and added.");
                    }
                    else
                    {
                        _logger.Debug($"Avatar image for {this.SteamId} not found.");
                    }
                }

                this.AvatarImage = avatarImage;
            }
            finally
            {
                _dataUpdated = true;
            }
        }
    }
}

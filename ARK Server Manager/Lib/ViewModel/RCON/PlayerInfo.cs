using ArkData;
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
        static internal readonly ConcurrentDictionary<long, BitmapImage> avatarImages = new ConcurrentDictionary<long, BitmapImage>();

        public long SteamId
        {
            get { return (long)GetValue(SteamIDProperty); }
            set { SetValue(SteamIDProperty, value); }
        }

        public static readonly DependencyProperty SteamIDProperty = DependencyProperty.Register(nameof(SteamId), typeof(long), typeof(PlayerInfo), new PropertyMetadata(0L));

        public string SteamName
        {
            get { return (string)GetValue(SteamNameProperty); }
            set { SetValue(SteamNameProperty, value); }
        }

        public static readonly DependencyProperty SteamNameProperty = DependencyProperty.Register(nameof(SteamName), typeof(string), typeof(PlayerInfo), new PropertyMetadata(String.Empty));

        public ImageSource AvatarImage
        {
            get { return (ImageSource)GetValue(AvatarImageProperty); }
            set { SetValue(AvatarImageProperty, value); }
        }

        public static readonly DependencyProperty AvatarImageProperty = DependencyProperty.Register(nameof(AvatarImage), typeof(ImageSource), typeof(PlayerInfo), new PropertyMetadata(null));

        public bool IsOnline
        {
            get { return (bool)GetValue(IsOnlineProperty); }
            set { SetValue(IsOnlineProperty, value); }
        }

        public static readonly DependencyProperty IsOnlineProperty = DependencyProperty.Register(nameof(IsOnline), typeof(bool), typeof(PlayerInfo), new PropertyMetadata(false));

        public bool IsBanned
        {
            get { return (bool)GetValue(IsBannedProperty); }
            set { SetValue(IsBannedProperty, value); }
        }

        public static readonly DependencyProperty IsBannedProperty = DependencyProperty.Register(nameof(IsBanned), typeof(bool), typeof(PlayerInfo), new PropertyMetadata(false));

        public bool IsWhitelisted
        {
            get { return (bool)GetValue(IsWhitelistedProperty); }
            set { SetValue(IsWhitelistedProperty, value); }
        }

        public static readonly DependencyProperty IsWhitelistedProperty = DependencyProperty.Register(nameof(IsWhitelisted), typeof(bool), typeof(PlayerInfo), new PropertyMetadata(false));

        public string TribeName
        {
            get { return (string)GetValue(TribeNameProperty); }
            set { SetValue(TribeNameProperty, value); }
        }

        public static readonly DependencyProperty TribeNameProperty = DependencyProperty.Register(nameof(TribeName), typeof(string), typeof(PlayerInfo), new PropertyMetadata(String.Empty));

        public DateTime LastUpdated
        {
            get { return (DateTime)GetValue(LastUpdatedProperty); }
            set { SetValue(LastUpdatedProperty, value); }
        }

        public static readonly DependencyProperty LastUpdatedProperty = DependencyProperty.Register(nameof(LastUpdated), typeof(DateTime), typeof(PlayerInfo), new PropertyMetadata(DateTime.MinValue));

        public Player ArkData
        {
            get { return (Player)GetValue(ArkDataProperty); }
            set { SetValue(ArkDataProperty, value); }
        }

        public static readonly DependencyProperty ArkDataProperty = DependencyProperty.Register(nameof(ArkData), typeof(Player), typeof(PlayerInfo), new PropertyMetadata(null));

        public bool HasBan
        {
            get { return (bool)GetValue(HasBanProperty); }
            set { SetValue(HasBanProperty, value); }
        }

        public static readonly DependencyProperty HasBanProperty = DependencyProperty.Register(nameof(HasBan), typeof(bool), typeof(PlayerInfo), new PropertyMetadata(false));

        internal async Task UpdateArkDataAsync(Player arkData)
        {
            this.ArkData = arkData;
            this.LastUpdated = arkData.FileUpdated;
            this.TribeName = arkData.Tribe?.Name;
            this.HasBan = arkData.CommunityBanned || arkData.VACBanned;

            BitmapImage avatarImage;
            if (!PlayerInfo.avatarImages.TryGetValue(this.SteamId, out avatarImage))
            {
                // check for a valid URL.
                if (!String.IsNullOrWhiteSpace(arkData.AvatarUrl))
                {
                    var localFile = Path.Combine(Path.GetTempPath(), $"ASM.{this.SteamId}.tmp");
                    try
                    {
                        using (var client = new WebClient())
                        {
                            await client.DownloadFileTaskAsync(arkData.AvatarUrl, localFile);
                            avatarImage = new BitmapImage(new Uri(localFile, UriKind.Absolute));
                            PlayerInfo.avatarImages[this.SteamId] = avatarImage;
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugUtils.WriteFormatThreadSafeAsync($"Failed to get avatar image from {arkData.AvatarUrl}: {ex.Message}: {ex.StackTrace}").DoNotWait();
                    }
                }
            }

            this.AvatarImage = avatarImage;
        }
    }
}

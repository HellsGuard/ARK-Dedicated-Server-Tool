using ArkData;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ARK_Server_Manager.Lib.ViewModel.RCON
{
    public class PlayerInfo : INotifyPropertyChanged
    {
        private readonly ConcurrentDictionary<long, BitmapImage> _avatarImages = new ConcurrentDictionary<long, BitmapImage>();

        private Logger _logger;

        public PlayerInfo(Logger logger)
        {
            _logger = logger;

            SteamId = 0L;
            SteamName = string.Empty;
            CharacterName = string.Empty;
            AvatarImage = null;
            IsOnline = false;
            IsAdmin = false;
            IsBanned = false;
            IsWhitelisted = false;
            TribeName = string.Empty;
            LastUpdated = DateTime.MinValue;
            HasBan = false;
            IsValid = true;
            PlayerData = null;
        }

        public long SteamId
        {
            get { return Get<long>(); }
            set { Set(value); }
        }
        public string SteamName
        {
            get { return Get<string>(); }
            set
            {
                Set(value);

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
            get { return Get<string>(); }
            set
            {
                Set(value);

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
            get { return Get<ImageSource>(); }
            set { Set(value); }
        }
        public bool IsOnline
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
        public bool IsAdmin
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
        public bool IsBanned
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
        public bool IsWhitelisted
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
        public string TribeName
        {
            get { return Get<string>(); }
            set
            {
                Set(value);

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
            get { return Get<DateTime>(); }
            set { Set(value); }
        }
        public bool HasBan
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
        public bool IsValid
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
        public PlayerData PlayerData
        {
            get { return Get<PlayerData>(); }
            set { Set(value); }
        }

        public async Task UpdateAvatarImageAsync(string imageSavePath)
        {
            // check if an image path was passed in
            if (string.IsNullOrWhiteSpace(imageSavePath))
            {
                // no image path, use the default temporary path
                imageSavePath = Path.GetTempPath();
            }
            // check if the image path exists
            if (!Directory.Exists(imageSavePath))
            {
                // create the image path
                Directory.CreateDirectory(imageSavePath);
            }

            // check if the avatar image exists in the collection
            if (_avatarImages.TryGetValue(this.SteamId, out BitmapImage avatarImage))
            {
                _logger.Debug($"Avatar image for {this.SteamId} found.");
            }
            else
            {
                var localImageFile = Path.Combine(imageSavePath, $"{this.SteamId}{Config.Default.PlayerImageFileExtension}");
                var localImageFileInfo = new FileInfo(localImageFile);

                // check if the image file does not exists or is older than a day
                if (!localImageFileInfo.Exists || localImageFileInfo.LastWriteTimeUtc.AddDays(1) <= DateTime.UtcNow)
                {
                    // check for a valid URL.
                    if (!String.IsNullOrWhiteSpace(PlayerData?.AvatarUrl))
                    {
                        try
                        {
                            using (var client = new WebClient())
                            {
                                await client.DownloadFileTaskAsync(PlayerData.AvatarUrl, localImageFile);
                            }
                            _logger.Debug($"{nameof(UpdateAvatarImageAsync)} - downloaded avatar image for {this.SteamId} from {PlayerData.AvatarUrl}.");
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"{nameof(UpdateAvatarImageAsync)} - failed to download avatar image for {this.SteamId} from {PlayerData.AvatarUrl}. {ex.Message}\r\n{ex.StackTrace}");
                        }
                    }
                }

                if (localImageFileInfo.Exists)
                {
                    avatarImage = new BitmapImage(new Uri(localImageFile, UriKind.Absolute));
                    _avatarImages.TryAdd(this.SteamId, avatarImage);
                    _logger.Debug($"Avatar image for {this.SteamId} found and added.");
                }
                else
                {
                    _logger.Debug($"Avatar image for {this.SteamId} not found.");
                }
            }

            this.AvatarImage = avatarImage;
        }

        public void UpdateData(PlayerData playerData, DateTime lastSteamUpdateUtc)
        {
            this.PlayerData = playerData;
            this.CharacterName = playerData?.CharacterName;
            this.TribeName = playerData?.Tribe?.Name;
            this.LastUpdated = playerData?.FileUpdated ?? DateTime.MinValue;

            if (playerData.LastSteamUpdateUtc.Equals(lastSteamUpdateUtc))
            {
                this.HasBan = (playerData?.CommunityBanned ?? false) || (playerData?.VACBanned ?? false);
            }
        }

        public void UpdateSteamData(PlayerData playerData)
        {
            if (playerData == null)
                return;

            playerData.AvatarUrl = PlayerData?.AvatarUrl;
            playerData.CommunityBanned = PlayerData?.CommunityBanned ?? false;
            playerData.DaysSinceLastBan = PlayerData?.DaysSinceLastBan ?? 0;
            playerData.LastSteamUpdateUtc = PlayerData?.LastSteamUpdateUtc ?? DateTime.MinValue;
            playerData.NumberOfGameBans = PlayerData?.NumberOfGameBans ?? 0;
            playerData.NumberOfVACBans = PlayerData?.NumberOfVACBans ?? 0;
            playerData.ProfileUrl = PlayerData?.ProfileUrl;
            playerData.SteamName = PlayerData?.SteamName;
            playerData.VACBanned = PlayerData?.VACBanned ?? false;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private Dictionary<string, object> _properties = new Dictionary<string, object>();

        protected T Get<T>([CallerMemberName] string name = null)
        {
            object value = null;
            if (_properties?.TryGetValue(name, out value) ?? false)
                return value == null ? default(T) : (T)value;
            return default(T);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void Set<T>(T value, [CallerMemberName] string name = null)
        {
            if (Equals(value, Get<T>(name)))
                return;
            if (_properties == null)
                _properties = new Dictionary<string, object>();
            _properties[name] = value;
            OnPropertyChanged(name);
        }
        #endregion
    }
}

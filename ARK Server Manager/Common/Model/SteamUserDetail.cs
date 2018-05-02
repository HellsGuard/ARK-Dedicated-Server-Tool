using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;

namespace ARK_Server_Manager.Lib.Model
{
    public class SteamUserDetailResult
    {
        public SteamUserDetailResponse response { get; set; }
    }

    public class SteamUserDetailResponse
    {
        public List<SteamUserDetail> players { get; set; }
    }

    public class SteamUserDetail
    {
        public string steamid { get; set; }

        public int communityvisibilitystate { get; set; }

        public int profilestate { get; set; }

        public string personaname { get; set; }

        public int lastlogoff { get; set; }

        public string profileurl { get; set; }

        public string avatar { get; set; }

        public string avatarmedium { get; set; }

        public string avatarfull { get; set; }

        public int personastate { get; set; }
    }

    [DataContract]
    public class SteamUserList : ObservableCollection<SteamUserItem>
    {
        public void AddRange(SteamUserList list)
        {
            if (list == null)
                return;

            foreach (var item in list)
            {
                if (!this.Any(i => i.SteamId.Equals(item.SteamId)))
                    this.Add(item);
            }
        }

        public static SteamUserList GetList(SteamUserDetailResponse response, string[] steamIds)
        {
            var result = new SteamUserList();
            if (steamIds != null)
            {
                foreach (var steamId in steamIds)
                {
                    result.Add(new SteamUserItem()
                    {
                        SteamId = steamId,
                        SteamName = "<not available>",
                        ProfileUrl = string.Empty,
                });
                }
            }

            if (response?.players != null)
            {
                foreach (var detail in response.players)
                {
                    var item = result.FirstOrDefault(i => i.SteamId == detail.steamid);
                    if (item == null)
                        result.Add(SteamUserItem.GetItem(detail));
                    else
                    {
                        item.SteamId = detail.steamid;
                        item.SteamName = detail.personaname ?? string.Empty;
                        item.ProfileUrl = detail.profileurl ?? string.Empty;
                    }
                }
            }
            return result;
        }

        public void Remove(string steamId)
        {
            var items = this.Where(i => i.SteamId.Equals(steamId, System.StringComparison.OrdinalIgnoreCase)).ToArray();
            foreach (var item in items)
            {
                this.Remove(item);
            }
        }

        public string[] ToArray()
        {
            return this.Select(i => i.SteamId).ToArray();
        }

        public string ToDelimitedString(string delimiter)
        {
            return string.Join(delimiter, this.Select(i => i.SteamId));
        }

        public override string ToString()
        {
            return $"{nameof(SteamUserList)} - {Count}";
        }
    }

    [DataContract]
    public class SteamUserItem : DependencyObject
    {
        public static readonly DependencyProperty SteamIdProperty = DependencyProperty.Register(nameof(SteamId), typeof(string), typeof(SteamUserItem), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty SteamNameProperty = DependencyProperty.Register(nameof(SteamName), typeof(string), typeof(SteamUserItem), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty ProfileUrlProperty = DependencyProperty.Register(nameof(ProfileUrl), typeof(string), typeof(SteamUserItem), new PropertyMetadata(string.Empty));

        [DataMember]
        public string SteamId
        {
            get { return (string)GetValue(SteamIdProperty); }
            set { SetValue(SteamIdProperty, value); }
        }

        [DataMember]
        public string SteamName
        {
            get { return (string)GetValue(SteamNameProperty); }
            set { SetValue(SteamNameProperty, value); }
        }

        public string ProfileUrl
        {
            get { return (string)GetValue(ProfileUrlProperty); }
            set { SetValue(ProfileUrlProperty, value); }
        }

        public static SteamUserItem GetItem(SteamUserDetail detail)
        {
            if (string.IsNullOrWhiteSpace(detail.steamid))
                return null;

            var result = new SteamUserItem();
            result.SteamId = detail.steamid;
            result.SteamName = detail.personaname ?? string.Empty;
            result.ProfileUrl = detail.profileurl ?? string.Empty;

            return result;
        }

        public override string ToString()
        {
            return $"{SteamId} - {SteamName}";
        }
    }
}

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        public static SteamUserList GetList(SteamUserDetailResponse response)
        {
            if (response == null)
                return new SteamUserList();

            var result = new SteamUserList();
            if (response.players != null)
            {
                foreach (var detail in response.players)
                {
                    result.Add(SteamUserItem.GetItem(detail));
                }
            }
            return result;
        }

        public string[] ToArray()
        {
            return this.Select(i => i.SteamId).ToArray();
        }

        public override string ToString()
        {
            return $"{nameof(SteamUserList)} - {Count}";
        }
    }

    public class SteamUserItem : DependencyObject
    {
        public static readonly DependencyProperty SteamIdProperty = DependencyProperty.Register(nameof(SteamId), typeof(string), typeof(SteamUserItem), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty SteamNameProperty = DependencyProperty.Register(nameof(SteamName), typeof(string), typeof(WorkshopFileItem), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty ProfileUrlProperty = DependencyProperty.Register(nameof(ProfileUrl), typeof(string), typeof(WorkshopFileItem), new PropertyMetadata(string.Empty));

        public string SteamId
        {
            get { return (string)GetValue(SteamIdProperty); }
            set { SetValue(SteamIdProperty, value); }
        }

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

        public static SteamUserItem GetItem(SteamUserDetail item)
        {
            if (string.IsNullOrWhiteSpace(item.steamid))
                return null;

            var result = new SteamUserItem();
            result.SteamId = item.steamid;
            result.SteamName = item.personaname ?? string.Empty;
            result.ProfileUrl = item.profileurl ?? string.Empty;

            return result;
        }

        public override string ToString()
        {
            return $"{SteamId} - {SteamName}";
        }
    }
}

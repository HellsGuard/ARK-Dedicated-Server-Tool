using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ARK_Server_Manager.Lib.ViewModel.RCON
{
    public class PlayerInfo : DependencyObject
    {
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

        internal void GetSteamInfoAsync()
        {
            // Do nothing for now.
        }
    }
}

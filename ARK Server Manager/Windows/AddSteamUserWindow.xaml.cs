using System.Windows;
using ARK_Server_Manager.Lib;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for AddSteamUserWindow.xaml
    /// </summary>
    public partial class AddSteamUserWindow : Window
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public AddSteamUserWindow()
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            this.DataContext = this;
        }

        public static readonly DependencyProperty SteamUsersProperty = DependencyProperty.Register(nameof(SteamUsers), typeof(string), typeof(AddSteamUserWindow), new PropertyMetadata(string.Empty));
        public string SteamUsers
        {
            get { return (string)GetValue(SteamUsersProperty); }
            set { SetValue(SteamUsersProperty, value); }
        }

        private void Process_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

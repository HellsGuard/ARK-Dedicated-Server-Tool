using System.Windows;
using System.Windows.Controls;
using ARK_Server_Manager.Lib;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for CustomConfigDataWindow.xaml
    /// </summary>
    public partial class CustomConfigDataWindow : Window
    {
        public CustomConfigDataWindow()
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            this.DataContext = this;
        }

        public static readonly DependencyProperty ConfigDataProperty = DependencyProperty.Register(nameof(ConfigData), typeof(string), typeof(CustomConfigDataWindow), new PropertyMetadata(string.Empty));
        public string ConfigData
        {
            get { return (string)GetValue(ConfigDataProperty); }
            set { SetValue(ConfigDataProperty, value); }
        }

        public TextWrapping ConfigDataTextWrapping
        {
            get { return ConfigDataTextBox.TextWrapping; }
            set { ConfigDataTextBox.TextWrapping = value; }
        }

        private void Process_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

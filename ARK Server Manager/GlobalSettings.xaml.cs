using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for GlobalSettings.xaml
    /// </summary>
    public partial class GlobalSettings : UserControl
    {
        public string Version
        {
            get;
            set;
        }

        public Config CurrentConfig
        {
            get;
            set;
        }

        public GlobalSettings()
        {
            this.Version = Assembly.GetEntryAssembly().FullName;            
            this.CurrentConfig = Config.Default;
            this.DataContext = this;

            InitializeComponent();
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            Config.Default.Save();
        }
    }
}

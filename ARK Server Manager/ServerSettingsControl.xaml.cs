using ARK_Server_Manager.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for ServerSettings.xaml
    /// </summary>
    partial class ServerSettingsControl : UserControl
    {
        ServerSettingsViewModel settingsViewModel;
        ServerRuntimeViewModel runtimeViewModel;

        public ServerSettingsViewModel Settings
        {
            get { return this.settingsViewModel; }
        }

        public ServerRuntimeViewModel Runtime
        {
            get { return this.runtimeViewModel; }
        }

        internal ServerSettingsControl(ServerSettingsViewModel viewModel)
        {
            InitializeComponent();
            this.settingsViewModel = viewModel;
            this.runtimeViewModel = new ServerRuntimeViewModel(settingsViewModel.Model);
            this.DataContext = this;
        }
    }
}

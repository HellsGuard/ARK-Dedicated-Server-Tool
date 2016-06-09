using System.Windows;
using ARK_Server_Manager.Lib;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for CommandLine.xaml
    /// </summary>
    public partial class CommandLine : Window
    {
        private GlobalizedApplication _globalizedApplication = GlobalizedApplication.Instance;

        public CommandLine(string commandLine)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            this.DataContext = commandLine;
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Clipboard.SetText(this.DataContext as string);
                MessageBox.Show(_globalizedApplication.GetResourceString("CommandLine_CopyButton_ConfirmLabel"), _globalizedApplication.GetResourceString("CommandLine_CopyButton_ConfirmTitle"), MessageBoxButton.OK);
            }
            catch
            {
                MessageBox.Show(_globalizedApplication.GetResourceString("CommandLine_CopyButton_ErrorLabel"), _globalizedApplication.GetResourceString("CommandLine_CopyButton_ErrorTitle"), MessageBoxButton.OK);
            }            
        }
    }
}

using ARK_Server_Manager.Lib;
using System;
using System.Threading;
using System.Windows;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for AutoUpdateWindow.xaml
    /// </summary>
    public partial class AutoUpdateWindow : Window
    {
        private GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        private SteamCmdUpdater updater = new SteamCmdUpdater();
        private CancellationTokenSource cancelSource;

        public AutoUpdateWindow()
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cancelSource = new CancellationTokenSource();
            updater.UpdateSteamCmdAsync(new Progress<SteamCmdUpdater.Update>(async u =>
                {
                    var message = string.IsNullOrWhiteSpace(u.StatusKey) ? string.Empty : _globalizer.GetResourceString(u.StatusKey) ?? u.StatusKey;
                    this.StatusLabel.Content = message;
                    this.CompletionProgress.Value = u.CompletionPercent;

                    if(u.FailureText != null)
                    {
                        // TODO: Report error through UI
                        throw new Exception(u.FailureText);
                    }

                    if (u.CompletionPercent >= 100 || u.Cancelled)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                var mainWindow = new MainWindow();
                                mainWindow.Show();
                                this.Close();
                            });
                    }
                }), cancelSource.Token);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (cancelSource != null)
                cancelSource.Cancel();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (cancelSource != null)
                cancelSource.Cancel();
        }
    }
}

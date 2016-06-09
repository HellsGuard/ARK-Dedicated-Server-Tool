using ARK_Server_Manager.Lib;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for AutoUpdate.xaml
    /// </summary>
    public partial class AutoUpdate : Window
    {
        Updater updater = new Updater();
        CancellationTokenSource cancelSource;
        public AutoUpdate()
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cancelSource = new CancellationTokenSource();
            updater.UpdateAsync(new Progress<Updater.Update>(async u =>
                {
                    this.StatusLabel.Content = this.FindResource(u.StatusKey);
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
            cancelSource.Cancel();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cancelSource.Cancel();
        }
    }
}

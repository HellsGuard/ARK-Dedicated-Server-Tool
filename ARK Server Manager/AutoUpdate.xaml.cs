using ARK_Server_Manager.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for AutoUpdate.xaml
    /// </summary>
    public partial class AutoUpdate : Window
    {
        AutoUpdater updater = new AutoUpdater();
        CancellationTokenSource cancelSource;
        public AutoUpdate()
        {
            InitializeComponent();
        }
      
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cancelSource = new CancellationTokenSource();
            updater.UpdateAsync(new Progress<AutoUpdater.Update>(async u =>
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
                        await Task.Delay(1000);
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

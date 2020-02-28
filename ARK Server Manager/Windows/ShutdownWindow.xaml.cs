﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ARK_Server_Manager.Lib;
using WPFSharp.Globalizer;
using static ARK_Server_Manager.Lib.ServerApp;
using ArkServerManager.Plugin.Common;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for ShutdownWindow.xaml
    /// </summary>
    public partial class ShutdownWindow : Window
    {
        private static readonly List<Server> Instances = new List<Server>();

        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private CancellationTokenSource _shutdownCancellationSource = null;

        public static readonly DependencyProperty BackupWorldFileProperty = DependencyProperty.Register(nameof(BackupWorldFile), typeof(bool), typeof(ShutdownWindow), new PropertyMetadata(true));
        public static readonly DependencyProperty RestartServerProperty = DependencyProperty.Register(nameof(RestartServer), typeof(bool), typeof(ShutdownWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty ShowMessageOutputProperty = DependencyProperty.Register(nameof(ShowMessageOutput), typeof(bool), typeof(ShutdownWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty ShutdownIntervalProperty = DependencyProperty.Register(nameof(ShutdownInterval), typeof(int), typeof(ShutdownWindow), new PropertyMetadata(Config.Default.ServerShutdown_GracePeriod));
        public static readonly DependencyProperty ShutdownStartedProperty = DependencyProperty.Register(nameof(ShutdownStarted), typeof(bool), typeof(ShutdownWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty ShutdownTypeProperty = DependencyProperty.Register(nameof(ShutdownType), typeof(int), typeof(ShutdownWindow), new PropertyMetadata(0));
        public static readonly DependencyProperty ServerProperty = DependencyProperty.Register(nameof(Server), typeof(Server), typeof(ShutdownWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty ShutdownReasonProperty = DependencyProperty.Register(nameof(ShutdownReason), typeof(string), typeof(ShutdownWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty UpdateServerProperty = DependencyProperty.Register(nameof(UpdateServer), typeof(bool), typeof(ShutdownWindow), new PropertyMetadata(false));

        protected ShutdownWindow(Server server)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            Server = server;
            BackupWorldFile = !server?.Profile?.SOTF_Enabled ?? false;
            this.Title = string.Format(_globalizer.GetResourceString("ShutdownWindow_ProfileTitle"), server?.Profile?.ProfileName);

            this.DataContext = this;
        }

        public bool BackupWorldFile
        {
            get { return (bool)GetValue(BackupWorldFileProperty); }
            set { SetValue(BackupWorldFileProperty, value); }
        }
        public bool RestartServer
        {
            get { return (bool)GetValue(RestartServerProperty); }
            set { SetValue(RestartServerProperty, value); }
        }
        public bool ShowMessageOutput
        {
            get { return (bool)GetValue(ShowMessageOutputProperty); }
            set { SetValue(ShowMessageOutputProperty, value); }
        }
        public int ShutdownInterval
        {
            get { return (int)GetValue(ShutdownIntervalProperty); }
            set { SetValue(ShutdownIntervalProperty, value); }
        }
        public bool ShutdownStarted
        {
            get { return (bool)GetValue(ShutdownStartedProperty); }
            set { SetValue(ShutdownStartedProperty, value); }
        }
        public int ShutdownType
        {
            get { return (int)GetValue(ShutdownTypeProperty); }
            set
            {
                SetValue(ShutdownTypeProperty, value);
                ShowMessageOutput = value == 1;
                ShutdownStarted = value > 0;
            }
        }
        public Server Server
        {
            get { return (Server)GetValue(ServerProperty); }
            set { SetValue(ServerProperty, value); }
        }
        public string ShutdownReason
        {
            get { return (string)GetValue(ShutdownReasonProperty); }
            set { SetValue(ShutdownReasonProperty, value); }
        }
        public bool UpdateServer
        {
            get { return (bool)GetValue(UpdateServerProperty); }
            set { SetValue(UpdateServerProperty, value); }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (ShutdownStarted)
                e.Cancel = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Instances.Remove(Server);
            Server = null;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (ShutdownStarted)
                return;

            // close the form.
            this.Close();
        }

        private void CancelShutdown_Click(object sender, RoutedEventArgs e)
        {
            if (_shutdownCancellationSource == null || _shutdownCancellationSource.IsCancellationRequested)
                return;

            _shutdownCancellationSource.Cancel();
        }

        private async void Shutdown_Click(object sender, RoutedEventArgs e)
        {
            if (ShutdownStarted)
                return;

            try
            {
                MessageOutput.Clear();

                ShutdownType = 1;
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                Application.Current.Dispatcher.Invoke(() => this.CancelShutdownButton.Cursor = System.Windows.Input.Cursors.Arrow);

                var app = new ServerApp(true)
                {
                    BackupWorldFile = this.BackupWorldFile,
                    ShutdownInterval = this.ShutdownInterval,
                    ShutdownReason = this.ShutdownReason,
                    OutputLogs = false,
                    SendAlerts = true,
                    ServerProcess = RestartServer ? ServerProcessType.Restart : ServerProcessType.Shutdown,
                    ProgressCallback = (p, m, n) => { TaskUtils.RunOnUIThreadAsync(() => { this.AddMessage(m, n); }).DoNotWait(); },
                };

                var profile = ServerProfileSnapshot.Create(Server.Profile);
                var restartServer = RestartServer;
                var updateServer = UpdateServer;

                _shutdownCancellationSource = new CancellationTokenSource();

                var exitCode = await Task.Run(() => app.PerformProfileShutdown(profile, restartServer, updateServer, _shutdownCancellationSource.Token));
                if (exitCode != ServerApp.EXITCODE_NORMALEXIT && exitCode != ServerApp.EXITCODE_CANCELLED)
                    throw new ApplicationException($"An error occured during the shutdown process - ExitCode: {exitCode}");

                ShutdownType = 0;
                // if restarting or updating the server after the shutdown, delay the form closing
                if (restartServer || updateServer)
                    await Task.Delay(5000);

                if (Config.Default.CloseShutdownWindowWhenFinished)
                    this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_ShutdownServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                ShutdownType = 0;
            }
            finally
            {
                _shutdownCancellationSource = null;
                Application.Current.Dispatcher.Invoke(() => this.Cursor = null);
                Application.Current.Dispatcher.Invoke(() => this.CancelShutdownButton.Cursor = null);
            }
        }

        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (ShutdownStarted)
                return;

            try
            {
                ShutdownType = 2;
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);

                PluginHelper.Instance.ProcessAlert(AlertType.Shutdown, Server.Profile.ProfileName, Config.Default.Alert_ServerStopMessage);
                await Task.Delay(2000);

                await this.Server.StopAsync();

                ShutdownType = 0;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_StopServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                ShutdownType = 0;
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = null);
            }
        }

        public void AddMessage(string message, bool includeNewLine = true)
        {
            MessageOutput.AppendText(message);
            if (includeNewLine)
                MessageOutput.AppendText(Environment.NewLine);
            MessageOutput.ScrollToEnd();

            Debug.WriteLine(message);
        }

        public static bool HasInstance(Server server)
        {
            return Instances.Contains(server);
        }

        public static ShutdownWindow OpenShutdownWindow(Server server)
        {
            if (HasInstance(server))
                return null;

            Instances.Add(server);
            return new ShutdownWindow(server);
        }
    }
}

using System;
using System.Diagnostics;
using System.Windows;
using ARK_Server_Manager.Lib;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private bool _allowClose = false;

        public ProgressWindow(string windowTitle)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            if (!string.IsNullOrWhiteSpace(windowTitle))
                Title = windowTitle;
            Application.Current.Dispatcher.Invoke(() => MessageOutput.Cursor = System.Windows.Input.Cursors.Wait);

            _allowClose = false;
            this.DataContext = this;
        }

        public void AddMessage(string message)
        {
            MessageOutput.AppendText(message);
            MessageOutput.AppendText(Environment.NewLine);
            MessageOutput.ScrollToEnd();

            Debug.WriteLine(message);
        }

        public void CloseWindow()
        {
            Application.Current.Dispatcher.Invoke(() => MessageOutput.Cursor = null);

            _allowClose = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_allowClose)
                e.Cancel = true;
        }
    }
}

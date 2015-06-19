using System.Windows;

namespace ARK_Server_Manager.Example
{
    /// <summary>
    /// Interaction logic for GlobalizedWindow.xaml
    /// </summary>
    public partial class GlobalizedWindow
    {
        public GlobalizedWindow()
        {
            InitializeComponent();
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            FirstNameTextBox.Text = LastNameTextBox.Text = AgeTextBox.Text = string.Empty;
        }

    }
}

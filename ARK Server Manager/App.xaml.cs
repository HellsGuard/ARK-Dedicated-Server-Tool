using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : GlobalizedApplication
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            base.OnStartup(e);
            
            // Initial configuration setting
            if(String.IsNullOrWhiteSpace(Config.Default.DataDir))
            {
                Config.Default.DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Config.Default.DefaultDataDir);
                Config.Default.ConfigDirectory = Path.Combine(Config.Default.DataDir, Config.Default.ProfilesDir);
                System.IO.Directory.CreateDirectory(Config.Default.ConfigDirectory);
            }
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Oops!  Bad news everyone - the app is going down!
            // Write out a log file with all the details so users can send us the info...

            string file = Path.GetTempFileName();
            var exception = e.ExceptionObject as Exception;
            var details = new StringBuilder();
            details.AppendLine("ARK Server Manager Crash Report");
            details.AppendLine("Please report this crash to ChronosWS or HellsGuard on Steam").AppendLine();
            details.Append("Assembly: ").Append(Assembly.GetExecutingAssembly().ToString()).AppendLine();
            details.AppendLine("Exception Message:");
            details.AppendLine(exception.Message).AppendLine();
            details.AppendLine("Stack Trace:");
            details.AppendLine(exception.StackTrace);
            File.WriteAllText(file, details.ToString());

            var result = MessageBox.Show(String.Format(@"
OOPS!  ARK Server Manager has suffered from an internal error and must shut down.
This is probably a bug and should be reported.  The error is logged here:
{0}
Please send this file to ChronosWS or HellsGuard on Steam.  Would you like
to view the error log now?
", file), "ARK Server Manager crashed", MessageBoxButton.YesNo);

            if(result == MessageBoxResult.Yes)
            {
                Process.Start("notepad.exe", file);
            }
        }
    }
}

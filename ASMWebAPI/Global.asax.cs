using System;
using System.IO;
using NLog;
using NLog.Targets;

namespace ASMWebAPI
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            ReconfigureLogging();
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }

        private static void ReconfigureLogging()
        {
            var logDir = System.Web.Hosting.HostingEnvironment.MapPath(Config.Default.LogsDir);

            if (string.IsNullOrWhiteSpace(logDir))
                return;

            Directory.CreateDirectory(logDir);
            if (!Directory.Exists(logDir))
                return;

            LogManager.Configuration.Variables["logDir"] = logDir;

            var target = (FileTarget)LogManager.Configuration.FindTargetByName("debugFile");
            target.FileName = Path.Combine(logDir, "ASM_Debug.log");
            target.ArchiveFileName = Path.Combine(logDir, "ASM_Debug.{#}.log");

            target = (FileTarget)LogManager.Configuration.FindTargetByName("serverFile");
            target.FileName = Path.Combine(logDir, "ASM_Server.log");
            target.ArchiveFileName = Path.Combine(logDir, "ASM_Server.{#}.log");

            LogManager.ReconfigExistingLoggers();
        }
    }
}
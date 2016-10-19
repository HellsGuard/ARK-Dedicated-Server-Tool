using System.IO;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using NLog;
using NLog.Targets;

namespace ASMWebAPI
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            ReconfigureLogging();

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

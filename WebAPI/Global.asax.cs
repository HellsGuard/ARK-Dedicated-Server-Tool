using NLog;
using NLog.Targets;
using System.IO;
using System.Linq;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

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

            var fileTargets = LogManager.Configuration.AllTargets.OfType<FileTarget>();
            foreach (var fileTarget in fileTargets)
            {
                var fileName = Path.GetFileNameWithoutExtension(fileTarget.FileName.ToString());
                fileTarget.FileName = Path.Combine(logDir, $"{fileName}.log");
                fileTarget.ArchiveFileName = Path.Combine(logDir, $"{fileName}.{{#}}.log");
            }

            LogManager.ReconfigExistingLoggers();
        }
    }
}

using NLog;
using System.Web.Http;
using System.Web.Mvc;

namespace ASMWebAPI.Controllers
{
    public class PluginController : ApiController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // GET: api/plugin/call/192.168.1.1/30842B29-4839-47D5-9501-835DEA2C70EE
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/plugin/call/{ipString}/{pluginId}")]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public bool Call(string ipString, string pluginId)
        {
            try
            {
                Logger.Info($"{ipString}:{pluginId}");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

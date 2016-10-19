using System;
using System.Net;
using System.Web.Http;
using System.Web.Mvc;
using NLog;

namespace ASMWebAPI.Controllers
{
    public class ServerController : ApiController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // GET: api/Server/192.168.1.1/27017
        [System.Web.Http.Route("api/Server/{ipString}/{port}")]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public CheckServerResult Get(string ipString, int port)
        {
            var result = CheckServerStatusB(ipString, port).ToString();
            return new CheckServerResult {
                       ipstring = ipString,
                       port = port.ToString(),
                       available = result
            };
        }

        private static bool CheckServerStatusA(IPEndPoint endpoint)
        {
            try
            {
                using (var server = QueryMaster.ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, endpoint))
                {
                    var serverInfo = server.GetInfo();
                    Logger.Info($"Check server status requested for {endpoint.Address}:{endpoint.Port}");
                    return serverInfo != null;
                }
            }
            catch (Exception)
            {
                Logger.Debug($"Exception checking server status for {endpoint.Address}:{endpoint.Port}");
                return false;
            }
        }

        private static bool CheckServerStatusB(string ipString, int port)
        {
            try
            {
                IPAddress ipAddress;
                if (!IPAddress.TryParse(ipString, out ipAddress))
                    return false;
                var endpoint = new IPEndPoint(ipAddress, port);

                return CheckServerStatusA(endpoint);
            }
            catch (Exception)
            {
                Logger.Debug($"Exception checking server status for {ipString}:{port}");
                return false;
            }
        }

        public class CheckServerResult
        {
            public string ipstring = string.Empty;
            public string port = string.Empty;
            public string available = false.ToString();
        }
    }
}

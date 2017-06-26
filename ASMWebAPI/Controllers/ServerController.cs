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

        // GET: api/Server/call/192.168.1.1/27017
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Server/call/{ipString}/{port}")]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public bool Call(string ipString, int port)
        {
            try
            {
                Logger.Trace($"{ipString}:{port}; 0; 0");
                return true;
            }
            catch
            {
                return false;
            }
        }

        // GET: api/Server/call/192.168.1.1/27017/asmid/profileid
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Server/call/{ipString}/{port}/{asmId}/{profileId}")]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public bool Call(string ipString, int port, string asmId, string profileId)
        {
            try
            {
                Logger.Trace($"{ipString}:{port}; {asmId}; {profileId}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        // GET: api/Server/192.168.1.1/27017
        [System.Web.Http.HttpGet]
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
                    Logger.Info($"{endpoint.Address}:{endpoint.Port}");
                    return serverInfo != null;
                }
            }
            catch (Exception)
            {
                //Logger.Warn($"{endpoint.Address}:{endpoint.Port}; {ex.Message}");
                Logger.Warn($"{endpoint.Address}:{endpoint.Port}");
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
                //Logger.Warn($"{ipString}:{port}; {ex.Message}");
                Logger.Warn($"{ipString}:{port}");
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

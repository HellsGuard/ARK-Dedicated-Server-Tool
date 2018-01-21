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

        // GET: api/server/call/192.168.1.1/27017/managerid/profileid
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/server/call/{ipString}/{port}/{managerid}/{profileId}")]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public bool Call(string ipString, int port, string managerId, string profileId)
        {
            return Call(Guid.Empty.ToString(), ipString, port, managerId, profileId);
        }

        // GET: api/server/call/managerCode/192.168.1.1/27017/managerid/profileid
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/server/call/{managerCode}/{ipString}/{port}/{managerId}/{profileId}")]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public bool Call(string managerCode, string ipString, int port, string managerId, string profileId)
        {
            try
            {
                Logger.Trace($"{managerCode}; {ipString}:{port}; {managerId}; {profileId}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        // GET: api/server/192.168.1.1/27017
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/server/{ipString}/{port}")]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public CheckServerResult Get(string ipString, int port)
        {
            return Get(Guid.Empty.ToString(), ipString, port);
        }

        // GET: api/server/managerCode/192.168.1.1/27017
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/server/{managerCode}/{ipString}/{port}")]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public CheckServerResult Get(string managerCode, string ipString, int port)
        {
            var result = CheckServerStatusB(managerCode, ipString, port).ToString();
            return new CheckServerResult
            {
                ipstring = ipString,
                port = port.ToString(),
                available = result
            };
        }

        private static bool CheckServerStatusA(string managerCode, IPEndPoint endpoint)
        {
            if (string.IsNullOrWhiteSpace(managerCode))
                return false;
            if (!managerCode.Equals(Guid.Empty) && !managerCode.Equals(Config.Default.ServerManagerCode1))
                return false;

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
                Logger.Warn($"{managerCode}; {endpoint.Address}:{endpoint.Port}");
                return false;
            }
        }

        private static bool CheckServerStatusB(string managerCode, string ipString, int port)
        {
            try
            {
                IPAddress ipAddress;
                if (!IPAddress.TryParse(ipString, out ipAddress))
                    return false;
                var endpoint = new IPEndPoint(ipAddress, port);

                return CheckServerStatusA(managerCode, endpoint);
            }
            catch (Exception)
            {
                Logger.Warn($"{managerCode}; {ipString}:{port}");
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

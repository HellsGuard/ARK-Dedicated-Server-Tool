using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ASMWeb.Controllers.AppServices
{
    [RoutePrefix("api/connection")]
    [Route("{action=index}")]
    public class ConnectionServicesController : ApiController
    {
        // GET: api/ConnectionServices
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/ConnectionServices/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/ConnectionServices
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/ConnectionServices/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/ConnectionServices/5
        public void Delete(int id)
        {
        }
    }
}

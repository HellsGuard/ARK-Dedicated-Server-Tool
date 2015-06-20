using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ARK_Server_Manager.Lib
{
    public class ServerRuntime
    {

    }

    public class ServerRuntimeViewModel : ViewModelBase
    {
        private ServerSettings serverSettings;
        private ServerRuntime model;

        public ServerRuntime Model
        {
            get { return this.model; }
        }

        public ServerRuntimeViewModel(ServerSettings serverSettings)
        {            
            this.serverSettings = serverSettings;            
        }
    }
}

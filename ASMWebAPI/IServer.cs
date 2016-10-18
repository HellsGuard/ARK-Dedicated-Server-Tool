using System.Net;
using System.ServiceModel;
using ASMWebAPI.Models;

namespace ASMWebAPI
{
    [ServiceContract]
    public interface IServer
    {
        [OperationContract]
        bool CheckServerStatusA(IPEndPoint endpoint);

        [OperationContract]
        bool CheckServerStatusB(string ipString, int port);

        [OperationContract]
        ServerInfo GetServerInfoA(IPEndPoint endpoint);

        [OperationContract]
        ServerInfo GetServerInfoB(string ipString, int port);
    }
}

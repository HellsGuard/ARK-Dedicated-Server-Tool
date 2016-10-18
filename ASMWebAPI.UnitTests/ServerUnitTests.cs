using System.Diagnostics;
using System.Net;
using ASMWebAPI.UnitTests.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASMWebAPI.UnitTests
{
    [TestClass]
    public class ServerUnitTests
    {
        [TestMethod]
        public void CheckServerStatusATest()
        {
            var server = new ServerClient();
            var result = server.CheckServerStatusA(new IPEndPoint(IPAddress.Parse("101.165.222.249"), 27022));
            Debug.WriteLine($"result is {result}");
        }

        [TestMethod]
        public void CheckServerStatusBTest()
        {
            var server = new ServerClient();
            var result = server.CheckServerStatusB("101.165.222.249", 27022);
            Debug.WriteLine($"result is {result}");
        }

        [TestMethod]
        public void GetServerInfoATest()
        {
            var server = new ServerClient();
            var result = server.GetServerInfoA(new IPEndPoint(IPAddress.Parse("101.165.222.249"), 27022));
            Debug.WriteLine($"server name is {result.Name}");
            Debug.WriteLine($"version is {result.Version}");
            Debug.WriteLine($"map name is {result.Map}");
            Debug.WriteLine($"players {result.PlayerCount}/{result.MaxPlayers}");

            if (result.PlayerCount > 0)
            {
                foreach (var player in result.Players)
                {
                    Debug.WriteLine($"{player.Name}; {player.Time}");
                }
            }
        }

        [TestMethod]
        public void GetServerInfoBTest()
        {
            var server = new ServerClient();
            var result = server.GetServerInfoB("101.165.222.249", 27022);
            Debug.WriteLine($"server name is {result.Name}");
            Debug.WriteLine($"version is {result.Version}");
            Debug.WriteLine($"map name is {result.Map}");
            Debug.WriteLine($"players {result.PlayerCount}/{result.MaxPlayers}");

            if (result.PlayerCount > 0)
            {
                foreach (var player in result.Players)
                {
                    Debug.WriteLine($"{player.Name}; {player.Time}");
                }
            }
        }
    }
}

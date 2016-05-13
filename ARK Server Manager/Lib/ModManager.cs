using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;

namespace ARK_Server_Manager.Lib
{
#if false
    public class ModManager
    {
        private readonly SteamClient client;
        private readonly CallbackManager callbackManager;
        private ModManager()
        {
            // create our steamclient instance
            var steamClient = new SteamClient(System.Net.Sockets.ProtocolType.Tcp);
            // create the callback manager which will route callbacks to function calls
            var manager = new CallbackManager(steamClient);

            // get the steamuser handler, which is used for logging on after successfully connecting
            steamUser = steamClient.GetHandler<SteamUser>();
            // get the steam friends handler, which is used for interacting with friends on the network after logging on
            steamFriends = steamClient.GetHandler<SteamFriends>();

            // register a few callbacks we're interested in
            // these are registered upon creation to a callback manager, which will then route the callbacks
            // to the functions specified
            new Callback<SteamClient.ConnectedCallback>(OnConnected, manager);
            new Callback<SteamClient.DisconnectedCallback>(OnDisconnected, manager);

            new Callback<SteamUser.LoggedOnCallback>(OnLoggedOn, manager);
            new Callback<SteamUser.LoggedOffCallback>(OnLoggedOff, manager);

            // we use the following callbacks for friends related activities
            new Callback<SteamUser.AccountInfoCallback>(OnAccountInfo, manager);
            new Callback<SteamFriends.FriendsListCallback>(OnFriendsList, manager);
            new Callback<SteamFriends.PersonaStateCallback>(OnPersonaState, manager);
            new Callback<SteamFriends.FriendAddedCallback>(OnFriendAdded, manager);

            isRunning = true;

            Console.WriteLine("Connecting to Steam...");

            // initiate the connection
            steamClient.Connect();

            // create our callback handling loop
            while (isRunning)
            {
                // in order for the callbacks to get routed, they need to be handled by the manager
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }

        public static Task<ModManager> LogonAsync(string user, string pass)
        {

        }
    }
#endif
}

using System.Collections.Generic;

namespace ARK_Server_Manager.Lib.Model
{
    public class SteamServerDetailResult
    {
        public SteamServerDetailResponse response { get; set; }
    }

    public class SteamServerDetailResponse
    {
        public string success { get; set; }

        public List<SteamServerDetail> servers { get; set; }
    }

    public class SteamServerDetail
    {
        public string addr { get; set; }

        public int gmsindex { get; set; }

        public string appid { get; set; }

        public string gamedir { get; set; }

        public int region { get; set; }

        public string secure { get; set; }

        public string lan { get; set; }

        public int gameport { get; set; }

        public int specport { get; set; }
    }
}

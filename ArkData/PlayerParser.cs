using System;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace ArkData
{
    internal partial class Parser
    {
        private static ulong GetId(byte[] data)
        {
            byte[] bytes1 = Encoding.Default.GetBytes("PlayerDataID");
            byte[] bytes2 = Encoding.Default.GetBytes("UInt64Property");
            int offset = Extensions.LocateFirst(data, bytes1, 0);
            int num = Extensions.LocateFirst(data, bytes2, offset);

            return BitConverter.ToUInt64(data, num + bytes2.Length + 9);
        }

        private static string GetSteamId(byte[] data)
        {
            byte[] bytes1 = Encoding.Default.GetBytes("UniqueNetIdRepl");
            int num = Extensions.LocateFirst(data, bytes1, 0);
            byte[] bytes2 = new byte[17];

            Array.Copy((Array)data, num + bytes1.Length + 9, (Array)bytes2, 0, 17);
            return Encoding.Default.GetString(bytes2);
        }

        public static Player ParsePlayer(string fileName)
        {
            FileInfo fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
                return null;
            byte[] data = File.ReadAllBytes(fileName);

            return new Player()
            {
                Id = Convert.ToInt64(GetId(data)),
                SteamId = GetSteamId(data),
                SteamName = Helpers.GetString(data, "PlayerName"),
                CharacterName = Helpers.GetString(data, "PlayerCharacterName"),
                TribeId = Helpers.GetInt(data, "TribeID"),
                Level = (short)(1 + Convert.ToInt32(Helpers.GetUInt16(data, "CharacterStatusComponent_ExtraCharacterLevel"))),

                FileCreated = fileInfo.CreationTime,
                FileUpdated = fileInfo.LastWriteTime
            };
        }

        public static Task<Player> ParsePlayerAsync(string fileName)
        {
            return Task.Run<Player>(() =>
            {
                return ParsePlayer(fileName);
            });
        }
    }
}

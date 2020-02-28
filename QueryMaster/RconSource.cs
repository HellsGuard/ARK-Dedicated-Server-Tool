﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace QueryMaster
{
    class RconSource : Rcon
    {
        internal TcpQuery socket;
        private RconSource(IPEndPoint address)
        {
            socket = new TcpQuery(address, 3000, 3000);
        }

        internal static Rcon Authorize(IPEndPoint address, string msg)
        {
            RconSource obj = new RconSource(address);
            byte[] recvData = new byte[50];
            RconSrcPacket packet = new RconSrcPacket() { Body = msg, Id = (int)PacketId.ExecCmd, Type = (int)PacketType.Auth };
            recvData = obj.socket.GetResponse(RconUtil.GetBytes(packet));
            int header;
            try
            {
                header = BitConverter.ToInt32(recvData, 4);
            }
            catch (Exception e)
            {
                e.Data.Add("ReceivedData", recvData);
                throw;
            }
            if (header != -1)
            {
                return obj;
            }
            obj.socket.Dispose();
            return obj;
        }

        public override string SendCommand(string command)
        {
            RconSrcPacket senPacket = new RconSrcPacket() { Body = command, Id = (int)PacketId.ExecCmd, Type = (int)PacketType.Exec };
            List<byte[]> recvData = socket.GetMultiPacketResponse(RconUtil.GetBytes(senPacket));
            StringBuilder str = new StringBuilder();
            try
            {
                for (int i = 0; i < recvData.Count; i++)
                {
                    //consecutive rcon command replies start with an empty packet 
                    if (BitConverter.ToInt32(recvData[i], 4) == (int)PacketId.Empty)
                        continue;
                    //if (recvData[i].Length - BitConverter.ToInt32(recvData[i], 0) == 4)
                    //{
                    str.Append(RconUtil.ProcessPacket(recvData[i]).Body);
                    //}
                    //else
                    //{
                    //    str.Append(RconUtil.ProcessPacket(recvData[i]).Body + Util.BytesToString(recvData[++i].Take(recvData[i].Length - 2).ToArray()));
                    //}
                }
            }
            catch (Exception e)
            {
                e.Data.Add("ReceivedData", recvData.SelectMany(x => x).ToArray());
                throw;
            }
            return str.ToString();
        }

        public override void AddlogAddress(string ip, ushort port)
        {
            SendCommand("logaddress_add " + ip + ":" + port);
        }

        public override void RemovelogAddress(string ip, ushort port)
        {
            SendCommand("logaddress_del " + ip + ":" + port);
        }
        public override void Dispose()
        {
            if (socket != null)
                socket.Dispose();
        }
    }
}

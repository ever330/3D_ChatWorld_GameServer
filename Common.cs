using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public struct PacketBase
    {
        public static readonly int HEADERSIZE = 4;
    }

    public enum PacketId : short
    {
        ClientConnect = 1000,

        ReqCreateRoom = 1100,
        ResCreateRoom = 1101,
        ReqEnterRoom = 1102,
        ResEnterRoom = 1103,
        ReqRoomPlayers = 1104,
        ResRoomPlayers = 1105,

        S2CNewPlayer = 1300,

        C2SPlayerInfo = 1500,
        S2CPlayerInfo = 1501,

        C2SEchoChat = 1600,
        S2CEchoChat = 1601,

        C2SVoice = 1700,
        S2CVoice = 1701,

        ClientDisconnect = 1900
    }

    class Vector3
    {
        public float x;
        public float y;
        public float z;
    }

    class Player
    {
        public string NickName;
        public Vector3 Position;
        public Vector3 Forward;
        public IPEndPoint IpEP;
    }
}

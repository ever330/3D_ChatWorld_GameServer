using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    /// <summary>
    /// todo
    /// 클라이언트와 주고받게 될 패킷을 구현할 부분
    /// </summary>
    public struct Packet<T> where T : struct
    {
        public byte[] Serialize()
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] array = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            try
            {
                Marshal.StructureToPtr(this.Data, ptr, true);
                Marshal.Copy(ptr, array, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return array;
        }

        public static T Deserialize(byte[] array)
        {
            T result;
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);

            try
            {
                Marshal.Copy(array, 0, ptr, size);
                result = (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return result;
        }

        public Packet(T data)
        {
            this = new Packet<T>();
            this.Data = data;
        }

        public T Data { get; set; }
    }


    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ClientConnectPacket
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string Nickname;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ClientDisconnectPacket
    {

    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ReqCreateRoomPacket
    {

    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ResCreateRoomPacket
    {
        [MarshalAs(UnmanagedType.I4)]
        public int RoomNum;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ReqEnterRoomPacket
    {
        [MarshalAs(UnmanagedType.I4)]
        public int RoomNum;
    }


    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ResEnterRoomPacket
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool Result;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct C2SEchoChat
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string Chat;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct S2CEchoChat
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public byte[] Nickname;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public byte[] Chat;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct S2CNewPlayerPacket
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public byte[] Nickname;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ReqRoomPlayersPacket
    {
        [MarshalAs(UnmanagedType.I4)]
        public int RoomNum;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ResRoomPlayersPacket
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public byte[] Nickname;
        [MarshalAs(UnmanagedType.R4)]
        public float PosX;
        [MarshalAs(UnmanagedType.R4)]
        public float PosY;
        [MarshalAs(UnmanagedType.R4)]
        public float PosZ;
        [MarshalAs(UnmanagedType.R4)]
        public float ForX;
        [MarshalAs(UnmanagedType.R4)]
        public float ForY;
        [MarshalAs(UnmanagedType.R4)]
        public float ForZ;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct C2SPlayerInfoPacket
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string Nickname;
        [MarshalAs(UnmanagedType.I4)]
        public int RoomNum;
        [MarshalAs(UnmanagedType.R4)]
        public float PosX;
        [MarshalAs(UnmanagedType.R4)]
        public float PosY;
        [MarshalAs(UnmanagedType.R4)]
        public float PosZ;
        [MarshalAs(UnmanagedType.R4)]
        public float ForX;
        [MarshalAs(UnmanagedType.R4)]
        public float ForY;
        [MarshalAs(UnmanagedType.R4)]
        public float ForZ;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct S2CPlayerInfoPacket
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string Nickname;
        [MarshalAs(UnmanagedType.R4)]
        public float PosX;
        [MarshalAs(UnmanagedType.R4)]
        public float PosY;
        [MarshalAs(UnmanagedType.R4)]
        public float PosZ;
        [MarshalAs(UnmanagedType.R4)]
        public float ForX;
        [MarshalAs(UnmanagedType.R4)]
        public float ForY;
        [MarshalAs(UnmanagedType.R4)]
        public float ForZ;
    }
}

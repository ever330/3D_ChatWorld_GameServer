using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class Room
    {
        public int PlayerCount;
        public Dictionary<int, Player> Players;     // 플레이어 ID로 관리
    }
    class RoomManager
    {
        private static RoomManager instance = null;

        public Dictionary<int, Room> RoomList;

        private Random randomRoomNum;
        private BlancGameServer mainForm;

        private int ipNum = 1;

        public static RoomManager Instance
        {
            get
            {
                if (null == instance)
                {
                    instance = new RoomManager();
                }
                return instance;
            }
        }

        public void Init(BlancGameServer form)
        {
            RoomList = new Dictionary<int, Room>();
            randomRoomNum = new Random();
            mainForm = form;
        }
        
        public int CreateRoom(int playerId, string nickname)
        {
            Room newRoom = new Room();
            newRoom.PlayerCount = 1;
            newRoom.Players = new Dictionary<int, Player>();

            int newRoomNum = randomRoomNum.Next(100000, 999999);

            while (RoomList.ContainsKey(newRoomNum))
            {
                newRoomNum = randomRoomNum.Next(100000, 999999);
            }

            RoomList.Add(newRoomNum, newRoom);

            Player newPlayer = new Player();
            newPlayer.NickName = nickname;
            newPlayer.Position = new Vector3 { x = 1.2f, y = 5.5f, z = 39f };
            newPlayer.Forward = new Vector3 { x = 0, y = 0, z = 1 };
            newPlayer.IpEP = new IPEndPoint(IPAddress.Any, 0);

            newRoom.Players.Add(playerId, newPlayer);

            ipNum++;

            return newRoomNum;
        }
        
        public void EnterRoom(int roomNum, int playerId, string nickname)
        {
            Player newPlayer = new Player();
            newPlayer.Position = new Vector3 { x = 1.2f, y = 5.5f, z = 39f };
            newPlayer.Forward = new Vector3 { x = 0, y = 0, z = 1 };
            newPlayer.NickName = nickname;
            newPlayer.IpEP = new IPEndPoint(IPAddress.Any, 0);

            RoomList[roomNum].PlayerCount++;
            RoomList[roomNum].Players.Add(playerId, newPlayer);
        }

        public void PlayerSetting(int roomNum, string nickname, Vector3 pos, Vector3 forward)
        {
            Room tempRoom = RoomList[roomNum];
            Player tempPlayer = tempRoom.Players.FirstOrDefault(x => x.Value.NickName == nickname).Value;
            tempPlayer.NickName = nickname;
            tempPlayer.Position = pos;
            tempPlayer.Forward = forward;
        }

        public void GetOutRoom(int roomNum, int playerId)
        {
            if (!RoomList.ContainsKey(roomNum))
                return;

            if (!RoomList[roomNum].Players.ContainsKey(playerId))
                return;

            RoomList[roomNum].Players.Remove(playerId);
            RoomList[roomNum].PlayerCount--;

            if (RoomList[roomNum].PlayerCount == 0)
            {
                RoomList.Remove(roomNum);
                mainForm.DeleteRoomQ.Enqueue(roomNum);
            }
        }
    }
}

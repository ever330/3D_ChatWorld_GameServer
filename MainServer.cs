using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameServer
{
    class MainServer
    {
        public ServerNetwork ServerNetwork { get; private set; }
        private Thread packetAnalysisThread;        // 연결된 클라이언트들에게서 온 패킷을 분석할 스레드
        private RoomManager roomManager;
        private BlancGameServer mainForm;

        public void InitServer(int port, int backlog, BlancGameServer form)
        {
            ServerNetwork = new ServerNetwork();
            ServerNetwork.Init(IPAddress.Any, port, backlog);

            roomManager = RoomManager.Instance;
            roomManager.Init(mainForm);

            packetAnalysisThread = new Thread(PacketAnalysis);
            packetAnalysisThread.Start();

            mainForm = form;
        }

        private void PacketAnalysis()
        {
            while (ServerNetwork != null)
            {
                foreach (Session tempSession in ServerNetwork.ClientSessions.Values)
                {
                    if (tempSession.RecvQ.dataCnt == 0)
                        continue;

                    byte[] totalData = tempSession.RecvQ.ReadData();

                    int packetSize = BitConverter.ToInt32(totalData, 0);
                    short packetId = BitConverter.ToInt16(totalData, 4);

                    // 패킷 크기만큼의 데이터를 추출
                    byte[] packetData = new byte[packetSize];
                    Buffer.BlockCopy(totalData, 6, packetData, 0, packetSize);

                    switch ((PacketId)packetId)
                    {
                        case PacketId.ClientConnect:
                            ClientConnectPacket conPac = Packet<ClientConnectPacket>.Deserialize(packetData);
                            tempSession.Nickname = conPac.Nickname;
                            ServerNetwork.NetworkMessage.Enqueue(string.Format("{0} : {1} 접속", tempSession.Id, tempSession.Nickname));
                            mainForm.AddUserQ.Enqueue(new KeyValuePair<int, string>(tempSession.Id, tempSession.Nickname));
                            break;

                        case PacketId.ClientDisconnect:
                            ClientDisconnectPacket disconPac = Packet<ClientDisconnectPacket>.Deserialize(packetData);
                            ServerNetwork.NetworkMessage.Enqueue(string.Format("{0} : {1} 접속 종료", tempSession.Id, tempSession.Nickname));
                            mainForm.DeleteUserQ.Enqueue(new KeyValuePair<int, string>(tempSession.Id, tempSession.Nickname));
                            ServerNetwork.ClientSessions.Remove(tempSession.Id);
                            Socket tempSocket = ServerNetwork.ClientSockets.FirstOrDefault(x => x.Value == tempSession.Id).Key;
                            ServerNetwork.ClientSockets.Remove(tempSocket);
                            tempSocket.Close();
                            tempSocket.Dispose();
                            break;

                        case PacketId.ReqCreateRoom:
                            int newRoomNum = roomManager.CreateRoom(tempSession.Id, tempSession.Nickname);
                            ServerNetwork.NetworkMessage.Enqueue(string.Format("{0}(id : {1}) 이(가) {2}번 방 생성", tempSession.Nickname, tempSession.Id, newRoomNum));
                            tempSession.RoomNum = newRoomNum;
                            ResCreateRoomPacket result = new ResCreateRoomPacket();
                            result.RoomNum = newRoomNum;
                            byte[] sendData = new Packet<ResCreateRoomPacket>(result).Serialize();

                            ServerNetwork.PacketSend(tempSession.Id, sendData, PacketId.ResCreateRoom);

                            mainForm.AddRoomQ.Enqueue(newRoomNum);
                            break;

                        case PacketId.ReqEnterRoom:
                            ReqEnterRoomPacket enterPac = Packet<ReqEnterRoomPacket>.Deserialize(packetData);

                            ResEnterRoomPacket enterRes = new ResEnterRoomPacket();
                            if (RoomManager.Instance.RoomList.ContainsKey(enterPac.RoomNum))
                            {
                                tempSession.RoomNum = enterPac.RoomNum;
                                Room tempRoom = RoomManager.Instance.RoomList[enterPac.RoomNum];

                                S2CNewPlayerPacket playerPac = new S2CNewPlayerPacket();
                                playerPac.Nickname = tempSession.Nickname;
                                byte[] newPlayerData = new Packet<S2CNewPlayerPacket>(playerPac).Serialize();

                                foreach (int id in tempRoom.Players.Keys)
                                {
                                    ServerNetwork.PacketSend(id, newPlayerData, PacketId.S2CNewPlayer);
                                }

                                RoomManager.Instance.EnterRoom(enterPac.RoomNum, tempSession.Id, tempSession.Nickname);
                                enterRes.EnterResult = true;
                            }
                            else
                            {
                                enterRes.EnterResult = false;
                            }
                            byte[] enterData = new Packet<ResEnterRoomPacket>(enterRes).Serialize();
                            ServerNetwork.PacketSend(tempSession.Id, enterData, PacketId.ResEnterRoom);

                            break;

                        case PacketId.ReqRoomPlayers:
                            ReqRoomPlayersPacket reqPlayers = Packet<ReqRoomPlayersPacket>.Deserialize(packetData);

                            foreach (Player player in RoomManager.Instance.RoomList[reqPlayers.RoomNum].Players.Values)
                            {
                                if (tempSession.Nickname != player.NickName)
                                {
                                    ResRoomPlayersPacket s2cInfoPac = new ResRoomPlayersPacket();
                                    s2cInfoPac.Nickname = player.NickName;
                                    s2cInfoPac.PosX = player.Position.x;
                                    s2cInfoPac.PosY = player.Position.y;
                                    s2cInfoPac.PosZ = player.Position.z;
                                    s2cInfoPac.ForX = player.Forward.x;
                                    s2cInfoPac.ForY = player.Forward.y;
                                    s2cInfoPac.ForZ = player.Forward.z;
                                    byte[] s2cInfoData = new Packet<ResRoomPlayersPacket>(s2cInfoPac).Serialize();
                                    ServerNetwork.PacketSend(tempSession.Id, s2cInfoData, PacketId.ResRoomPlayers);
                                }
                            }
                            break;

                        case PacketId.C2SEchoChat:
                            int playerRoomNum = tempSession.RoomNum;
                            S2CEchoChat echoPacket = new S2CEchoChat();
                            echoPacket.Nickname = tempSession.Nickname;
                            echoPacket.Chat = packetData;
                            byte[] chatData = new Packet<S2CEchoChat>(echoPacket).Serialize();

                            foreach (int id in RoomManager.Instance.RoomList[playerRoomNum].Players.Keys)
                            {
                                ServerNetwork.PacketSend(id, chatData, PacketId.S2CEchoChat);
                            }
                            break;

                        case PacketId.C2SPlayerInfo:
                            C2SPlayerInfoPacket infoPac = Packet<C2SPlayerInfoPacket>.Deserialize(packetData);
                            Vector3 position = new Vector3 { x = infoPac.PosX, y = infoPac.PosY, z = infoPac.PosZ };
                            Vector3 forward = new Vector3 { x = infoPac.ForX, y = infoPac.ForY, z = infoPac.ForZ };
                            RoomManager.Instance.PlayerSetting(tempSession.RoomNum, tempSession.Id, tempSession.Nickname, position, forward);

                            S2CPlayerInfoPacket resPlayerPac = new S2CPlayerInfoPacket();
                            resPlayerPac.Nickname = tempSession.Nickname;
                            resPlayerPac.PosX = infoPac.PosX;
                            resPlayerPac.PosY = infoPac.PosY;
                            resPlayerPac.PosZ = infoPac.PosZ;
                            resPlayerPac.ForX = infoPac.ForX;
                            resPlayerPac.ForY = infoPac.ForY;
                            resPlayerPac.ForZ = infoPac.ForZ;
                            byte[] resPlayerData = new Packet<S2CPlayerInfoPacket>(resPlayerPac).Serialize();

                            Room room = RoomManager.Instance.RoomList[tempSession.RoomNum];
                            foreach (int id in room.Players.Keys)
                            {
                                if (id != tempSession.Id)
                                    ServerNetwork.PacketSend(id, resPlayerData, PacketId.S2CPlayerInfo);
                            }

                            break;

                        default:
                            break;
                    }
                }

                Thread.Sleep(100);
            }
        }

        public void CloseServer()
        {
            if (ServerNetwork == null)
                return;

            ServerNetwork.Close();
            packetAnalysisThread.Join();
        }
    }
}

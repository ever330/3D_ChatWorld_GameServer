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

namespace GameServer
{
    public class Session
    {
        public int Id;
        public int RoomNum;
        public string Nickname;
        public StreamQueue SendQ;
        public StreamQueue RecvQ;
    }

    public class ServerNetwork
    {
        private Socket serverTcpSocket;
        private UdpClient serverUdp;

        public Dictionary<int, Session> ClientSessions;
        public Dictionary<Socket, int> ClientSockets;   // 클라이언트 소켓과 index 매칭

        public Queue<string> NetworkMessage;

        private Queue<int> clientIndexQ;                // 클라이언트에게 부여해 줄 index
        private Queue<int> disconnetIndexQ;             // 접속을 종료한 클라이언트에게서 받은 index (큐가 비어있지 않을 시, 통합 index보다 먼저 부여)

        private int maxClientCount = 100;               // 최대 접속받을 클라이언트 수

        private Thread udpReceiveThread;


        // 서버 소켓 생성 및 클라이언트 소켓 바인드 비동기 대기
        public void Init(IPAddress hostAddress, int tcpPort, int udpPort, int backlog)
        {
            ClientSessions = new Dictionary<int, Session>();
            ClientSockets = new Dictionary<Socket, int>();
            NetworkMessage = new Queue<string>();

            serverTcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipEp1 = new IPEndPoint(hostAddress, tcpPort);
            serverTcpSocket.Bind(ipEp1);
            serverTcpSocket.Listen(backlog);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptCompleted);
            serverTcpSocket.AcceptAsync(args);

            clientIndexQ = new Queue<int>();
            disconnetIndexQ = new Queue<int>();

            for (var count = 1; count <= maxClientCount; count++)
            {
                clientIndexQ.Enqueue(count);
            }

            serverUdp = new UdpClient(udpPort);
            udpReceiveThread = new Thread(UdpReceiveLoop);
            udpReceiveThread.Start();
        }

        public void Close()
        {
            if (serverTcpSocket != null)
            {
                serverTcpSocket.Close();
                serverTcpSocket.Dispose();
            }

            foreach (Socket soc in ClientSockets.Keys)
            {
                soc.Close();
                soc.Dispose();
            }
            ClientSockets.Clear();
        }

        // 클라이언트 접속 수락 callback 함수
        private void AcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            Socket clientSocket = e.AcceptSocket;

            if (clientSocket == null)
            {
                NetworkMessage.Enqueue("클라이언트 소켓 오류");
                return;
            }

            if (NetworkMessage == null)
            {
                return;
            }
            NetworkMessage.Enqueue(clientSocket.RemoteEndPoint.ToString() + "접속");


            Session newClient = new Session();
            newClient.RecvQ = new StreamQueue(1024);
            newClient.SendQ = new StreamQueue(1024);
            newClient.RoomNum = 0;

            if (disconnetIndexQ.Count > 0)
            {
                newClient.Id = disconnetIndexQ.Dequeue();
            }
            else
            {
                newClient.Id = clientIndexQ.Dequeue();
            }
            NetworkMessage.Enqueue("아이디 : " + newClient.Id + "부여");

            ClientSessions.Add(newClient.Id, newClient);
            ClientSockets.Add(clientSocket, newClient.Id);

            if (ClientSessions != null)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                byte[] data = new byte[1024];
                args.SetBuffer(data, 0, 1024);
                args.UserToken = args.AcceptSocket;
                args.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted);
                clientSocket.ReceiveAsync(args);
            }


            e.AcceptSocket = null;
            serverTcpSocket.AcceptAsync(e);
        }

        // 데이터 수신 callback 함수
        public void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            Socket clientSocket = (Socket)sender;
            int tempId = ClientSockets[clientSocket];
            Session tempSession = ClientSessions[tempId];

            if (clientSocket.Connected && e.BytesTransferred > 0)
            {
                tempSession.RecvQ.WriteData(e.Buffer, e.BytesTransferred);

                e.SetBuffer(new byte[1024], 0, 1024);
                clientSocket.ReceiveAsync(e);
            }
            else
            {
                clientSocket.Disconnect(false);
                ClientSockets.Remove(clientSocket);
                ClientSessions.Remove(tempId);
                disconnetIndexQ.Enqueue(tempId);
            }
        }

        public void PacketSend(int id, byte[] packet, PacketId packetId)
        {
            SocketAsyncEventArgs sendEventArgs = new SocketAsyncEventArgs();

            sendEventArgs.Completed += (sender, e) => { SendCompleted(sender, e, id); };
            sendEventArgs.UserToken = this;

            // 패킷의 총 사이즈를 바이트 배열로 변환
            byte[] sizeBytes = BitConverter.GetBytes(packet.Length);
            // 패킷의 아이디를 바이트 배열로 변환
            byte[] idBytes = BitConverter.GetBytes((short)packetId);

            // 총 사이즈와 내용물을 합칠 바이트 배열
            byte[] sendData = new byte[sizeBytes.Length + idBytes.Length + packet.Length];

            // 맨 앞에 사이즈 추가
            Array.Copy(sizeBytes, 0, sendData, 0, sizeBytes.Length);
            // 아이디 추가
            Buffer.BlockCopy(idBytes, 0, sendData, sizeBytes.Length, idBytes.Length);
            // 그 다음에 패킷 내용물 추가
            Buffer.BlockCopy(packet, 0, sendData, sizeBytes.Length + idBytes.Length, packet.Length);

            sendEventArgs.SetBuffer(sendData, 0, sendData.Length);

            try
            {
                Socket target = ClientSockets.FirstOrDefault(x => x.Value == id).Key;
                if (target == null)
                {
                    NetworkMessage.Enqueue($"{id}의 소켓이 NULL 입니다");

                    if (ClientSessions.ContainsKey(id))
                    {
                        int roomNum = ClientSessions[id].RoomNum;
                        RoomManager.Instance.GetOutRoom(roomNum, id);
                        ClientSessions.Remove(id);
                        target.Close();
                        target.Dispose();
                        ClientSockets.Remove(target);
                    }
                    return;
                }
                bool pending = target.SendAsync(sendEventArgs);
                // 보낸 결과를 확인하려면 이벤트 핸들러에서 처리해야 합니다.
                if (!pending)
                {
                    SendCompleted(null, sendEventArgs, id);
                }
            }
            catch (SocketException ex)
            {
                // 예외 처리: 소켓 통신 중에 오류가 발생했을 때 실행됩니다.
                NetworkMessage.Enqueue($"{id} : SocketException occurred: " + ex.Message);
                // 추가적인 예외 처리 또는 로깅을 여기에 수행할 수 있습니다.
            }

            //Socket target = ClientSockets.FirstOrDefault(x => x.Value == id).Key;
            //bool pending = target.SendAsync(sendEventArgs);

            //if (!pending)
            //{
            //    SendCompleted(null, sendEventArgs, id);
            //}
        }

        private void SendCompleted(object sender, SocketAsyncEventArgs e, int id)
        {
            NetworkMessage.Enqueue($"{id}으로 패킷 전송에 성공하였습니다.");
        }

        private void UdpReceiveLoop()
        {
            while (true)
            {
                try
                {
                    byte[] udpData;
                    IPEndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);
                    udpData = serverUdp.Receive(ref clientEP);

                    if (udpData.Length == 0)
                        continue;

                    int packetSize = BitConverter.ToInt32(udpData, 0);
                    short packetId = BitConverter.ToInt16(udpData, 4);

                    switch ((PacketId)packetId)
                    {
                        case PacketId.C2SPlayerInfo:
                            // 패킷 크기만큼의 데이터를 추출
                            byte[] packetData = new byte[packetSize];
                            Buffer.BlockCopy(udpData, 6, packetData, 0, packetSize);

                            C2SPlayerInfoPacket packet = Packet<C2SPlayerInfoPacket>.Deserialize(packetData);

                            Room tempRoom = RoomManager.Instance.RoomList[packet.RoomNum];

                            S2CPlayerInfoPacket sendPacket = new S2CPlayerInfoPacket();
                            sendPacket.Nickname = packet.Nickname;
                            sendPacket.PosX = packet.PosX;
                            sendPacket.PosY = packet.PosY;
                            sendPacket.PosZ = packet.PosZ;
                            sendPacket.ForX = packet.ForX;
                            sendPacket.ForY = packet.ForY;
                            sendPacket.ForZ = packet.ForZ;

                            byte[] sendData = new Packet<S2CPlayerInfoPacket>(sendPacket).Serialize();

                            // 패킷의 총 사이즈를 바이트 배열로 변환
                            byte[] sizeBytes = BitConverter.GetBytes(sendData.Length);
                            // 패킷의 아이디를 바이트 배열로 변환
                            byte[] idBytes = BitConverter.GetBytes((short)PacketId.S2CPlayerInfo);

                            // 총 사이즈와 내용물을 합칠 바이트 배열
                            byte[] totalData = new byte[sizeBytes.Length + idBytes.Length + sendData.Length];

                            // 맨 앞에 사이즈 추가
                            Array.Copy(sizeBytes, 0, totalData, 0, sizeBytes.Length);
                            // 아이디 추가
                            Buffer.BlockCopy(idBytes, 0, totalData, sizeBytes.Length, idBytes.Length);
                            // 그 다음에 패킷 내용물 추가
                            Buffer.BlockCopy(sendData, 0, totalData, sizeBytes.Length + idBytes.Length, sendData.Length);

                            foreach (Player player in tempRoom.Players.Values)
                            {
                                if (player.NickName == packet.Nickname)
                                {
                                    player.IpEP.Address = clientEP.Address;
                                    player.IpEP.Port = clientEP.Port + 1;
                                }
                                else
                                {
                                    if (player.IpEP.Port == 0)
                                        continue;

                                    serverUdp.Send(totalData, totalData.Length, player.IpEP);
                                }
                            }

                            break;

                        case PacketId.C2SVoice:
                            int roomNum = BitConverter.ToInt32(udpData, 6);
                            Room temp = RoomManager.Instance.RoomList[roomNum];
                            int nicknameLength = udpData.Length - packetSize - 10;
                            byte[] nicknameBytes = new byte[nicknameLength];

                            Array.Copy(udpData, 10, nicknameBytes, 0, udpData.Length - packetSize - 10);
                            string nick = Encoding.UTF8.GetString(nicknameBytes);

                            // 패킷 크기만큼의 데이터를 추출
                            byte[] voiceData = new byte[packetSize];
                            Buffer.BlockCopy(udpData, 10 + nicknameLength, voiceData, 0, packetSize);

                            // 패킷의 총 사이즈를 바이트 배열로 변환
                            byte[] size = BitConverter.GetBytes(packetSize);
                            // 패킷의 아이디를 바이트 배열로 변환
                            byte[] id = BitConverter.GetBytes((short)PacketId.S2CVoice);

                            // 총 사이즈와 내용물을 합칠 바이트 배열
                            byte[] total = new byte[size.Length + id.Length + nicknameBytes.Length + voiceData.Length];

                            // 맨 앞에 사이즈 추가
                            Array.Copy(size, 0, total, 0, size.Length);
                            // 아이디 추가
                            Buffer.BlockCopy(id, 0, total, size.Length, id.Length);
                            // 닉네임 추가
                            Buffer.BlockCopy(nicknameBytes, 0, total, size.Length + id.Length, nicknameBytes.Length);
                            // 그 다음에 패킷 내용물 추가
                            Buffer.BlockCopy(voiceData, 0, total, size.Length + id.Length + nicknameBytes.Length, voiceData.Length);

                            foreach (Player player in temp.Players.Values)
                            {
                                if (player.NickName == nick)
                                {
                                    player.IpEP.Address = clientEP.Address;
                                    player.IpEP.Port = clientEP.Port + 1;
                                }
                                else
                                {
                                    if (player.IpEP.Port == 0)
                                        continue;

                                    serverUdp.Send(total, total.Length, player.IpEP);
                                }
                            }

                            break;
                    }
                }
                catch (Exception e)
                {
                    NetworkMessage.Enqueue("UDP 수신 에러" + e.ToString());
                }
            }
        }

        private void RoomBroadCast()
        {

        }
    }
}

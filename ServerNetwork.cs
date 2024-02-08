using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
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
        public Session ServerSession;
        public Socket ServerSocket;

        public Dictionary<int, Session> ClientSessions;
        public Dictionary<Socket, int> ClientSockets;   // 클라이언트 소켓과 index 매칭

        public Queue<string> NetworkMessage;
        
        private Queue<int> clientIndexQ;                // 클라이언트에게 부여해 줄 index
        private Queue<int> disconnetIndexQ;             // 접속을 종료한 클라이언트에게서 받은 index (큐가 비어있지 않을 시, 통합 index보다 먼저 부여)

        private int maxClientCount = 100;               // 최대 접속받을 클라이언트 수


        // 서버 소켓 생성 및 클라이언트 소켓 바인드 비동기 대기
        public void Init(IPAddress hostAddress, int port, int backlog)
        {
            ClientSessions = new Dictionary<int, Session>();
            ClientSockets = new Dictionary<Socket, int>();
            NetworkMessage = new Queue<string>();

            ServerSession = new Session();
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint ipEp = new IPEndPoint(hostAddress, port);

            ServerSocket.Bind(ipEp);
            ServerSocket.Listen(backlog);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptCompleted);
            ServerSocket.AcceptAsync(args);

            ServerSession.Id = 0;
            ServerSession.RecvQ = new StreamQueue(1024);
            ServerSession.SendQ = new StreamQueue(1024);

            clientIndexQ = new Queue<int>();
            disconnetIndexQ = new Queue<int>();

            for (var count = 1; count <= maxClientCount; count++)
            {
                clientIndexQ.Enqueue(count);
            }
        }

        public void Close()
        {
            if (ServerSocket != null)
            {
                ServerSocket.Close();
                ServerSocket.Dispose();
            }

            foreach(Socket soc in ClientSockets.Keys)
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
            if (ClientSessions != null)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                byte[] data = new byte[1024];
                args.SetBuffer(data, 0, 1024);
                args.UserToken = args.AcceptSocket;
                args.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted);
                clientSocket.ReceiveAsync(args);
            }

            if (clientSocket == null)
            {
                NetworkMessage.Enqueue("클라이언트 소켓 오류");
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

            e.AcceptSocket = null;
            ServerSocket.AcceptAsync(e);
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
                        RoomManager.Instance.GoOutRoom(roomNum, id);
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



        public byte[] Serialize(object data)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(1024))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, data);
                    return ms.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }

        public object Deserialize(byte[] data)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    object obj = bf.Deserialize(ms);
                    return obj;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Serialization error: {ex.Message}");
                return null;
            }
        }
    }
}

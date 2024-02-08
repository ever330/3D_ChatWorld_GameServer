using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameServer
{
    public partial class BlancGameServer : Form
    {
        private MainServer mainServer;
        public Queue<KeyValuePair<int, string>> AddUserQ;
        public Queue<KeyValuePair<int, string>> DeleteUserQ;
        public Queue<int> AddRoomQ;
        public Queue<int> DeleteRoomQ;

        public BlancGameServer()
        {
            InitializeComponent();
            AddUserQ = new Queue<KeyValuePair<int, string>>();
            DeleteUserQ = new Queue<KeyValuePair<int, string>>();
            AddRoomQ = new Queue<int>();
            DeleteRoomQ = new Queue<int>();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            mainServer = new MainServer();
        }

        private void OpenBtn_Click(object sender, EventArgs e)
        {
            if (portTextBox.Text == "")
            {
                WriteToRichTextBox("포트번호를 입력해주세요.");
            }
            else
            {
                if (mainServer.ServerNetwork == null)
                {
                    mainServer.InitServer(Int32.Parse(portTextBox.Text), 10, this);

                    IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                    string localIP = string.Empty;

                    for (int i = 0; i < host.AddressList.Length; i++)
                    {
                        if (host.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                        {
                            localIP = host.AddressList[i].ToString();
                            break;
                        }
                    }

                    //IPTextBox.Text = localIP;

                    WriteToRichTextBox("서버가 오픈되었습니다.");
                }
                else
                {
                    WriteToRichTextBox("서버가 이미 오픈되어있습니다.");
                }
            }
        }

        private void WriteToRichTextBox(string text)
        {
            serverLogRTB.AppendText(text);
            serverLogRTB.AppendText("\n");
            serverLogRTB.ScrollToCaret();
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            if (mainServer.ServerNetwork == null)
                return;

            if (mainServer.ServerNetwork.NetworkMessage.Count > 0)
            {
                WriteToRichTextBox(mainServer.ServerNetwork.NetworkMessage.Dequeue());
            }

            if (AddUserQ.Count != 0)
            {
                KeyValuePair<int, string> tempPair = AddUserQ.Dequeue();
                userListBox.Items.Add(tempPair.Key + ":" + tempPair.Value);
            }

            if (DeleteUserQ.Count != 0)
            {
                KeyValuePair<int, string> tempPair = DeleteUserQ.Dequeue();
                userListBox.Items.Remove(tempPair.Key + ":" + tempPair.Value);
            }

            if (AddRoomQ.Count != 0)
            {
                roomListBox.Items.Add(AddRoomQ.Dequeue());
            }

            if (DeleteRoomQ.Count != 0)
            {
                roomListBox.Items.Remove(DeleteRoomQ.Dequeue());
            }
        }

        private void GameServer_Closing(object sender, FormClosingEventArgs e)
        {
            mainServer.CloseServer();
        }

        private void userListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            string[] words = ((string)userListBox.SelectedItem).Split(':');
            sb.AppendLine("유저 닉네임 : " + words[1]);
            int roomNum = mainServer.ServerNetwork.ClientSessions[Convert.ToInt32(words[0])].RoomNum;
            if (roomNum == 0)
            {
                sb.AppendLine("현재 위치 : Title");
            }
            else
            {
                sb.AppendLine("현재 위치 : " + roomNum);
            }
            userInfoBox.Text = sb.ToString();
        }

        private void roomListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Room tempRoom = RoomManager.Instance.RoomList[(int)roomListBox.SelectedItem];

            StringBuilder sb = new StringBuilder();
            foreach (Player player in tempRoom.Players.Values)
            {
                sb.AppendLine("유저 닉네임 : " + player.NickName);
            }
            roomUsersBox.Text = sb.ToString();
        }
    }
}

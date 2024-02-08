
namespace GameServer
{
    partial class BlancGameServer
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.serverLogRTB = new System.Windows.Forms.RichTextBox();
            this.logLabel = new System.Windows.Forms.Label();
            this.OpenBtn = new System.Windows.Forms.Button();
            this.portTextBox = new System.Windows.Forms.TextBox();
            this.portLabel = new System.Windows.Forms.Label();
            this.updateTimer = new System.Windows.Forms.Timer(this.components);
            this.userListBox = new System.Windows.Forms.ListBox();
            this.users = new System.Windows.Forms.Label();
            this.userInfo = new System.Windows.Forms.Label();
            this.roomListBox = new System.Windows.Forms.ListBox();
            this.rooms = new System.Windows.Forms.Label();
            this.roomUsers = new System.Windows.Forms.Label();
            this.userInfoBox = new System.Windows.Forms.RichTextBox();
            this.roomUsersBox = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // serverLogRTB
            // 
            this.serverLogRTB.Location = new System.Drawing.Point(12, 24);
            this.serverLogRTB.Name = "serverLogRTB";
            this.serverLogRTB.ReadOnly = true;
            this.serverLogRTB.Size = new System.Drawing.Size(369, 362);
            this.serverLogRTB.TabIndex = 0;
            this.serverLogRTB.Text = "";
            // 
            // logLabel
            // 
            this.logLabel.AutoSize = true;
            this.logLabel.Location = new System.Drawing.Point(12, 9);
            this.logLabel.Name = "logLabel";
            this.logLabel.Size = new System.Drawing.Size(57, 12);
            this.logLabel.TabIndex = 1;
            this.logLabel.Text = "서버 로그";
            // 
            // OpenBtn
            // 
            this.OpenBtn.Location = new System.Drawing.Point(177, 388);
            this.OpenBtn.Name = "OpenBtn";
            this.OpenBtn.Size = new System.Drawing.Size(75, 23);
            this.OpenBtn.TabIndex = 2;
            this.OpenBtn.Text = "서버오픈";
            this.OpenBtn.UseVisualStyleBackColor = true;
            this.OpenBtn.Click += new System.EventHandler(this.OpenBtn_Click);
            // 
            // portTextBox
            // 
            this.portTextBox.Location = new System.Drawing.Point(71, 390);
            this.portTextBox.Name = "portTextBox";
            this.portTextBox.Size = new System.Drawing.Size(100, 21);
            this.portTextBox.TabIndex = 3;
            // 
            // portLabel
            // 
            this.portLabel.AutoSize = true;
            this.portLabel.Location = new System.Drawing.Point(12, 395);
            this.portLabel.Name = "portLabel";
            this.portLabel.Size = new System.Drawing.Size(53, 12);
            this.portLabel.TabIndex = 4;
            this.portLabel.Text = "포트번호";
            // 
            // updateTimer
            // 
            this.updateTimer.Enabled = true;
            this.updateTimer.Interval = 500;
            this.updateTimer.Tick += new System.EventHandler(this.updateTimer_Tick);
            // 
            // userListBox
            // 
            this.userListBox.FormattingEnabled = true;
            this.userListBox.ItemHeight = 12;
            this.userListBox.Location = new System.Drawing.Point(400, 24);
            this.userListBox.Name = "userListBox";
            this.userListBox.Size = new System.Drawing.Size(156, 196);
            this.userListBox.TabIndex = 5;
            this.userListBox.SelectedIndexChanged += new System.EventHandler(this.userListBox_SelectedIndexChanged);
            // 
            // users
            // 
            this.users.AutoSize = true;
            this.users.Location = new System.Drawing.Point(398, 9);
            this.users.Name = "users";
            this.users.Size = new System.Drawing.Size(57, 12);
            this.users.TabIndex = 6;
            this.users.Text = "유저 목록";
            // 
            // userInfo
            // 
            this.userInfo.AutoSize = true;
            this.userInfo.Location = new System.Drawing.Point(595, 9);
            this.userInfo.Name = "userInfo";
            this.userInfo.Size = new System.Drawing.Size(57, 12);
            this.userInfo.TabIndex = 8;
            this.userInfo.Text = "유저 정보";
            // 
            // roomListBox
            // 
            this.roomListBox.FormattingEnabled = true;
            this.roomListBox.ItemHeight = 12;
            this.roomListBox.Location = new System.Drawing.Point(400, 262);
            this.roomListBox.Name = "roomListBox";
            this.roomListBox.Size = new System.Drawing.Size(156, 124);
            this.roomListBox.TabIndex = 9;
            this.roomListBox.SelectedIndexChanged += new System.EventHandler(this.roomListBox_SelectedIndexChanged);
            // 
            // rooms
            // 
            this.rooms.AutoSize = true;
            this.rooms.Location = new System.Drawing.Point(398, 247);
            this.rooms.Name = "rooms";
            this.rooms.Size = new System.Drawing.Size(45, 12);
            this.rooms.TabIndex = 10;
            this.rooms.Text = "방 목록";
            // 
            // roomUsers
            // 
            this.roomUsers.AutoSize = true;
            this.roomUsers.Location = new System.Drawing.Point(595, 247);
            this.roomUsers.Name = "roomUsers";
            this.roomUsers.Size = new System.Drawing.Size(101, 12);
            this.roomUsers.TabIndex = 12;
            this.roomUsers.Text = "방 참여 유저 목록";
            // 
            // userInfoBox
            // 
            this.userInfoBox.Location = new System.Drawing.Point(597, 24);
            this.userInfoBox.Name = "userInfoBox";
            this.userInfoBox.ReadOnly = true;
            this.userInfoBox.Size = new System.Drawing.Size(134, 196);
            this.userInfoBox.TabIndex = 13;
            this.userInfoBox.Text = "";
            // 
            // roomUsersBox
            // 
            this.roomUsersBox.Location = new System.Drawing.Point(597, 262);
            this.roomUsersBox.Name = "roomUsersBox";
            this.roomUsersBox.ReadOnly = true;
            this.roomUsersBox.Size = new System.Drawing.Size(134, 124);
            this.roomUsersBox.TabIndex = 14;
            this.roomUsersBox.Text = "";
            // 
            // BlancGameServer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.roomUsersBox);
            this.Controls.Add(this.userInfoBox);
            this.Controls.Add(this.roomUsers);
            this.Controls.Add(this.rooms);
            this.Controls.Add(this.roomListBox);
            this.Controls.Add(this.userInfo);
            this.Controls.Add(this.users);
            this.Controls.Add(this.userListBox);
            this.Controls.Add(this.portLabel);
            this.Controls.Add(this.portTextBox);
            this.Controls.Add(this.OpenBtn);
            this.Controls.Add(this.logLabel);
            this.Controls.Add(this.serverLogRTB);
            this.Name = "BlancGameServer";
            this.Text = "BlancGameServer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.GameServer_Closing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox serverLogRTB;
        private System.Windows.Forms.Label logLabel;
        private System.Windows.Forms.Button OpenBtn;
        private System.Windows.Forms.TextBox portTextBox;
        private System.Windows.Forms.Label portLabel;
        private System.Windows.Forms.Timer updateTimer;
        private System.Windows.Forms.ListBox userListBox;
        private System.Windows.Forms.Label users;
        private System.Windows.Forms.Label userInfo;
        private System.Windows.Forms.ListBox roomListBox;
        private System.Windows.Forms.Label rooms;
        private System.Windows.Forms.Label roomUsers;
        private System.Windows.Forms.RichTextBox userInfoBox;
        private System.Windows.Forms.RichTextBox roomUsersBox;
    }
}


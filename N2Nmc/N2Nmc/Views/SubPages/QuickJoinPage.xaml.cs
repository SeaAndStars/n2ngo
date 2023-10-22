using HandyControl.Controls;
using HandyControl.Data;
using N2Nmc.UtilsClass;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static N2Nmc.UtilsClass.SharedData;

namespace N2Nmc.Views.SubPages
{
    /// <summary>
    /// Page4.xaml 的交互逻辑
    /// </summary>
    public partial class QuickJoinPage : Page
    {
        public QuickJoinPage()
        {
            InitializeComponent();
        }

        ~QuickJoinPage()
        {
        }

        public static async void Join(bool needPassword, string roomCode, string roomPassword)
        {
            SharedData.KillEdge();

            roomCode = roomCode.Trim();
            roomPassword = roomPassword.Trim();

            if (string.IsNullOrEmpty(roomCode))
            {
                Growl.ErrorGlobal("房间代码不能为空！");
                return;
            }

            if (needPassword == true && string.IsNullOrEmpty(roomPassword))
            {
                Growl.ErrorGlobal("密码不能为空！");
                return;
            }

            Growl.InfoGlobal("正在进入房间,请稍后...");

            ExecLog execLog = new ExecLog();

            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (_, __) =>
            {
                if (execLog.LogOut.Contains("[OK] edge <<< ================ >>> supernode"))
                {
                    GetMainView.EnterRoom(roomCode);
                    timer.Stop();
                }
                if (execLog.LogOut.Contains("No Windows tap devices found, did you run tapinstall.exe?"))
                {
                    Growl.WarningGlobal("请检测虚拟网卡是否安装成功，或者虚拟网卡是否正在使用(查看资源管理器是否有edge.exe正在运行)");
                    timer.Stop();
                }
            };

            timer.Start();

            string cmd = edgePath + " -c " + roomCode + " -k " + (needPassword == true ? roomPassword : SharedData.DefaultRoomPasswd) + " -l " + SharedData.n2nServerIPP;
            EdgeConnectionInfo.CurrentRoomCode = roomCode;

            execLog.SetCommand(cmd);
            int r = await execLog.ExecuteAsync();
            if (r == -21)
            {
                // No Windows tap
                Growl.WarningGlobal("请检测虚拟网卡是否安装成功，或者虚拟网卡是否正在使用(查看资源管理器是否有edge.exe正在运行)");
                timer.Stop();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Join(CheckIsPasswdNeeded.IsChecked == true, RoomConnectText.Text, RoomPasswordText.Text);
        }

    }
}

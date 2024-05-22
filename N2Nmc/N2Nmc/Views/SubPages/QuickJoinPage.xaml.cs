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
        public readonly static string GrowlToken = "QuickJoinPageGrowl";
        public QuickJoinPage()
        {
            InitializeComponent();
            Growl.Register(GrowlToken, PanelMsg);

            CheckIsPasswdNeeded_Click(null,null);
        }

        ~QuickJoinPage()
        {
            Growl.Unregister(GrowlToken, PanelMsg);
        }

        public static async void Join(bool needPassword, string roomCode, string roomPassword)
        {
            SharedData.KillEdge();

            roomCode = roomCode.Trim();
            roomPassword = roomPassword.Trim();

            if (string.IsNullOrEmpty(roomCode))
            {
                Growl.Error("代码不能为空！", GrowlToken);
                return;
            }

            if (needPassword == true && string.IsNullOrEmpty(roomPassword.Trim()))
            {
                Growl.Error("密码不能为空！", GrowlToken);
                return;
            }

            Growl.Info("正在进入房间,请稍后...", GrowlToken);

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

            //EdgeInvoker edgeInvoker = new EdgeInvoker();
            //edgeInvoker.PushArgs(" -c " + roomCode + " -k " + (needPassword == true ? roomPassword : SharedData.DefaultRoomPasswd) + " -l " + SharedData.n2nServerIPP);
            //edgeInvoker.Call();

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

        private void CheckIsPasswdNeeded_Click(object? sender, RoutedEventArgs? e)
        {
            if (CheckIsPasswdNeeded.IsChecked!=null)
            {
                GroupPasswordInput.IsEnabled = CheckIsPasswdNeeded.IsChecked.Value;
            }
        }
    }
}

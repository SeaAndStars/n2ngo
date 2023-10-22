using HandyControl.Controls;
using N2Nmc.UtilsClass;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static N2Nmc.UtilsClass.SharedData;
using MessageBox = HandyControl.Controls.MessageBox;

namespace N2Nmc.Views.SubPages
{
    /// <summary>
    /// Page2.xaml 的交互逻辑
    /// </summary>
    public partial class RoomingPage : Page
    {
        //文本对比,用于检测n2n是否启动成功
        private DispatcherTimer timer;

        public readonly static string GrowlToken = "RoomingPageGrowl";

        public RoomingPage()
        {
            InitializeComponent();

            Growl.Register(GrowlToken, PanelMsg);

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
        }

        ~RoomingPage()
        {
            timer.Stop();

            Growl.Unregister(GrowlToken, PanelMsg);
        }

        private void ButtonCreateRoom_Click(object sender, RoutedEventArgs e)
        {
            string name = roomname.Text;
            string password = roompassword.Text;
            bool needPassword = CheckIsRoomPasswordNeeded.IsChecked == true;
            bool roomInvisible = CheckIsRoomInvisible.IsChecked == true;

            if (!needPassword)
                password = SharedData.DefaultRoomPasswd;

            Growl.Info("正在创建房间中请稍后...",GrowlToken);

            if (needPassword && string.IsNullOrEmpty(roompassword.Text.Trim()))
            {
                Growl.Warning("密码不能为空！",GrowlToken);
                return;
            }

            Growl.Success("房间已创建", GrowlToken);

            string code = CreateRoom(name, roomInvisible, needPassword, password);

            //ButtonCloseRoom.IsEnabled = true;
            var r = MessageBox.Show("房间创建成功，是否立即加入？("+ code + ")", "房间已创建", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (r == MessageBoxResult.Yes)
                QuickJoinPage.Join(needPassword, code, password);
        }

        //private void ButtonCloseRoom_Click(object sender, RoutedEventArgs e)
        //{
        //    ButtonCloseRoom.IsEnabled = false;

        //    SharedData.CloseRoom(RoomCodeOut.Text);

        //    Growl.Success("房间已关闭");
        //}

        private void CheckIsRoomInvisible_Click(object sender, RoutedEventArgs e)
        {
            if (CheckIsRoomInvisible.IsChecked != null)
            {
                Growl.Clear(GrowlToken);
                Growl.Info("房间" + roomname.Text + "将" + ((bool)CheckIsRoomInvisible.IsChecked ? "不" : null) + "会发布到联机大厅", GrowlToken);
            }
        }
    }
}

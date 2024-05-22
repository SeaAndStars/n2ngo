using HandyControl.Controls;
using N2Nmc.UtilsClass;
using N2Nmc.Views.SubPages.Dialogs;
using N2Nmc.Views.SubPages.Dialogs.MessageDialogs;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        public RoomingPage()
        {
            InitializeComponent();

            CheckIsRoomPasswordNeeded_Click(null, null);

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
        }

        ~RoomingPage()
        {
            timer.Stop();
        }

        private void ButtonCreateRoom_Click(object sender, RoutedEventArgs e)
        {
            string name = roomname.Text;
            string password = roompassword.Text;
            bool needPassword = CheckIsRoomPasswordNeeded.IsChecked == true;
            bool roomInvisible = CheckIsRoomInvisible.IsChecked == true;
            var brushMain = (SolidColorBrush)SetTheme.Background;
            var brushMinor = (SolidColorBrush)SetMinor.Background;

            if (!needPassword)
                password = SharedData.DefaultRoomPasswd;

            if (needPassword && string.IsNullOrEmpty(password.Trim()))
            {
                SharedData.GetMainView.DoMessageDialog("密码不能为空！", "无法创建房间");
                return;
            }

            if (false&&string.IsNullOrEmpty(name.Trim()))   // Disabled
            {
                SharedData.GetMainView.DoMessageDialog("房间名不能为空！", "无法创建房间");
                return;
            }

            new N2Nmc_Protocol.Objects.RoomColor(brushMain.Color.R, brushMain.Color.G, brushMain.Color.B);
            var r = CreateRoom(name, roomInvisible, needPassword, password,
                new N2Nmc_Protocol.Objects.RoomColor(brushMain.Color.R, brushMain.Color.G, brushMain.Color.B).data,
                new N2Nmc_Protocol.Objects.RoomColor(brushMinor.Color.R, brushMinor.Color.G, brushMinor.Color.B).data);
            if (r == null || r[0] == null || r[1] == null)
            {
                SharedData.GetMainView.DoMessageDialog("创建房间时出现异常。", "无法创建房间");

                return;
            }

            //ButtonCloseRoom.IsEnabled = true;
            SharedData.GetMainView.DoMessageYesNoDialog(string.Format("房间创建成功，是否立即加入？\n名称：{0}\n代码：{1}\n管理员密钥：{2}",name, r[0], r[1]), "房间已创建", new List<Action<object>> { (_) => { if ((((_ as DialogMessage).MessageContent) as DialogYesNo).YesNo == DialogYesNo.YesNoE.Yes) QuickJoinPage.Join(needPassword, r[0], password); } });
        }

        private void CheckIsRoomInvisible_Click(object sender, RoutedEventArgs e)
        {
            if (false&&CheckIsRoomInvisible.IsChecked != null)  // Disabled
            {
                SharedData.GetMainView.DoMessageDialog("房间" + roomname.Text + "将" + ((bool)CheckIsRoomInvisible.IsChecked ? "不" : null) + "会发布到联机大厅", "提示");
            }
        }

        private void CheckIsRoomPasswordNeeded_Click(object? sender, RoutedEventArgs? e)
        {
            if (CheckIsRoomPasswordNeeded.IsChecked != null)
            {
                var b = CheckIsRoomPasswordNeeded.IsChecked.Value;
                GroupPasswordInput.IsEnabled = b;
            }
        }

        private void SetTheme_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SetMinor_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

using N2NGO.UtilsClass;
using N2NGO.Views.SubPages.Dialogs;
using N2NGO.Views.SubPages.Dialogs.MessageDialogs;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace N2NGO.Views.SubPages
{
    /// <summary>
    /// RoomingPage.xaml 的交互逻辑
    /// </summary>
    public partial class RoomingPage : Page
    {
        private readonly DispatcherTimer timer;

        public RoomingPage()
        {
            InitializeComponent();

            CheckIsRoomPasswordNeeded_Click(null, null);

            timer = new() { Interval = TimeSpan.FromSeconds(1) };
        }

        ~RoomingPage()
        {
            timer.Stop();
        }

        private void ButtonCreateRoom_Click(object sender, RoutedEventArgs e)
        {
            string name = RoomNameInput.Text;
            string password = RoomPasswordInput.Text;
            bool needPassword = CheckIsRoomPasswordNeeded.IsChecked == true;
            bool roomInvisible = CheckIsRoomInvisible.IsChecked == true;
            var brushMain = (SolidColorBrush)SetMain.Background;
            var brushMinor = (SolidColorBrush)SetMinor.Background;

            if (!needPassword)
                password = SharedData.DefaultRoomPassword;

            if (needPassword && string.IsNullOrEmpty(password.Trim()))
            {
                SharedData.CurrentApp.MainView.DoMessageDialog("密码不能为空！", "无法创建房间");
                return;
            }

            if (false && string.IsNullOrEmpty(name.Trim()))   // Disabled
            {
                SharedData.CurrentApp.MainView.DoMessageDialog("房间名不能为空！", "无法创建房间");
                return;
            }

            var roomCreateResult = SharedData.CurrentApp.N2NGOServerConnection.CreateRoom(name, roomInvisible, needPassword, password,
                new N2NGO_Core.Objects.RoomColor(brushMain.Color.R, brushMain.Color.G, brushMain.Color.B).data,
                new N2NGO_Core.Objects.RoomColor(brushMinor.Color.R, brushMinor.Color.G, brushMinor.Color.B).data);

            if (roomCreateResult.Status!=SharedData.N2NGOServerConnection.ProtocolOperationReturnStatus.Success)
            {
                return;
            }

            var roomCreate = roomCreateResult.Value;
            if (roomCreate == null || roomCreate[0] == null || roomCreate[1] == null)
            {
                SharedData.CurrentApp.MainView.DoMessageDialog("@LOCALE_DialogCreateRoom_Fail_Invalid_String_Array_Content", "@LOCALE_DialogCreateRoom_Title");

                return;
            }

            //ButtonCloseRoom.IsEnabled = true;
            SharedData.CurrentApp.MainView.DoMessageYesNoDialog(string.Format("房间创建成功，是否立即加入？\n名称：{0}\n代码：{1}\n管理员密钥：{2}", name, roomCreate[0], roomCreate[1]), "房间已创建",
                new List<Action<object>>
                {
                    (_) =>
                    {
                        if (_ is not DialogMessage dialogMessage || dialogMessage.MessageContent is not DialogYesNo dialogYesNo)
                            throw new Exception("Cannot get DialogYesNo");

                        if (dialogYesNo.YesNo == DialogYesNo.YesNoE.Yes)
                        {
                            QuickJoinPage.Join(needPassword, roomCreate[0], password);
                        }
                    }
                }
            );
        }

        private void CheckIsRoomPasswordNeeded_Click(object? sender, RoutedEventArgs? e)
        {
            if (CheckIsRoomPasswordNeeded.IsChecked != null)
            {
                var bNeedPassword = CheckIsRoomPasswordNeeded.IsChecked.Value;
                GroupPasswordInput.IsEnabled = bNeedPassword;
                if (bNeedPassword)
                {
                    SharedData.CurrentApp.MainView.DoMessageDialog("@LOCALE_DialogCreateRoomSecurity_Content", "@LOCALE_DialogCreateRoomSecurity_Title");
                }
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

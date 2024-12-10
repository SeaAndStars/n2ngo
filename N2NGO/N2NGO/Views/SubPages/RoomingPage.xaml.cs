using N2NGO.Utils;
using N2NGO.Views.SubPages.Dialogs;
using N2NGO.Views.SubPages.Dialogs.MessageDialogs;
using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace N2NGO.Views.SubPages
{
    public partial class RoomingPage : Page
    {
        private SolidColorBrush _mainColor = new(), _minorColor = new();
        internal SolidColorBrush MainColor { get => _mainColor; set { _mainColor = value; PreviewMainColor.DataContext = _mainColor; } } 
        internal SolidColorBrush MinorColor { get => _minorColor; set { _minorColor = value; PreviewMinorColor.DataContext = _minorColor; } } 

        public RoomingPage()
        {
            InitializeComponent();

            MainColor = new(Color.FromArgb(255, 0x64, 0x95, 0xED));
            MinorColor = new(Color.FromArgb(255, 0xF5, 0xDE, 0xB3));

            CheckIsRoomPasswordNeeded_Click(null, null);
        }

        private void ButtonCreateRoom_Click(object sender, RoutedEventArgs e)
        {
            string name = RoomNameInput.Text;
            string password = RoomPasswordInput.Text;
            bool needPassword = CheckIsRoomPasswordNeeded.IsChecked == true;
            bool roomInvisible = CheckIsRoomInvisible.IsChecked == true;
            var colorMain = MainColor.Color;
            var colorMinor = MinorColor.Color;
            double initialLifetime;
            try
            {
                initialLifetime = TimeSpan.Parse(RoomInitialLifetimeInput.Text).TotalMilliseconds;
            }
            catch 
            {
                var defaultLifetime = TimeSpan.FromMinutes(10);
                initialLifetime = defaultLifetime.TotalMilliseconds;
                RoomInitialLifetimeInput.Text = defaultLifetime.ToString();
            }

            if (!needPassword)
                password = Globals.DefaultRoomPassword;

            if (needPassword && string.IsNullOrEmpty(password.Trim()))
            {
                Globals.CurrentApp.MainWindow.DoMessageDialog("@LOCALE_DialogCreateRoom_Failure_Invalid_RoomPasswordInput_Content", "@LOCALE_DialogCreateRoom_Failure_Title");
                return;
            }

            if (false && string.IsNullOrEmpty(name.Trim()))   // Disabled
            {
                Globals.CurrentApp.MainWindow.DoMessageDialog("@LOCALE_DialogCreateRoom_Failure_Invalid_RoomNameInput_Content", "@LOCALE_DialogCreateRoom_Failure_Title");
                return;
            }

            var roomCreateResult = Globals.CurrentApp.N2NGOServerConnection.CreateRoom(name, roomInvisible, needPassword, password,
                new N2NGOCore.Objects.RoomColor(colorMain.R, colorMain.G, colorMain.B).data,
                new N2NGOCore.Objects.RoomColor(colorMinor.R, colorMinor.G, colorMinor.B).data,
                (ulong)initialLifetime);

            if (roomCreateResult.Status != N2NGOServerConnection.ProtocolOperationReturnStatus.Success)
            {
                return;
            }

            var roomCreate = roomCreateResult.Value;
            if (roomCreate == null || roomCreate[0] == null || roomCreate[1] == null)
            {
                Globals.CurrentApp.MainWindow.DoMessageDialog("@LOCALE_DialogCreateRoom_Failure_Invalid_String_Array_Content", "@LOCALE_DialogCreateRoom_Title");

                return;
            }

            //ButtonCloseRoom.IsEnabled = true;
            Globals.CurrentApp.MainWindow.DoMessageYesNoDialog(
                string.Format("房间创建成功，是否立即加入？\n名称：{0}\n代码：{1}\n管理员密钥：{2}", name, roomCreate[0], roomCreate[1]),
                "房间已创建",
                new List<Action<object>>
                {
                    (dialog) =>
                    {
                        if (dialog is not DialogMessage dialogMessage || dialogMessage.MessageContent is not DialogYesNo dialogYesNo)
                            throw new Exception("Cannot get DialogYesNo");

                        if (dialogYesNo.YesNo == DialogYesNo.YesNoE.Yes)
                        {
                            _ = Globals.CurrentApp.JoinRoomAsync(needPassword, roomCreate[0], password);
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
                    Globals.CurrentApp.MainWindow.DoMessageDialog("@LOCALE_DialogCreateRoomSecurity_Content", "@LOCALE_DialogCreateRoomSecurity_Title");
                }
            }
        }

        private void SetMain_Click(object sender, RoutedEventArgs e)
        {
            Globals.CurrentApp.MainWindow.DoMessagePickBrushDialog("@LOCALE_DialogSetMainColor_Content", "@LOCALE_DialogSetMainColor_Title"
                , new List<Action<object>> {
                    (_)=> {
                        if (_ is not DialogMessage dialogMessage)
                            throw new ArgumentException("Argument is not DialogMessage", nameof(_));

                        if (dialogMessage.MessageContent is not DialogPickBrush dialogPickBrush)
                            throw new Exception("dialogMessage.MessageContent is not DialogPickBrush dialogPickBrush");

                          MainColor = dialogPickBrush.GetColorBrush();
                    } },

                (_) =>
                {
                    if (_ is not DialogMessage dialogMessage)
                        throw new ArgumentException("Argument is not DialogMessage", nameof(_));

                    if (dialogMessage.MessageContent is not DialogPickBrush dialogPickBrush)
                        throw new Exception("dialogMessage.MessageContent is not DialogPickBrush dialogPickBrush");

                    dialogPickBrush.ColorAlphaInput.Text = MainColor.Color.A.ToString();
                    dialogPickBrush.ColorRedInput.Text = MainColor.Color.R.ToString();
                    dialogPickBrush.ColorGreenInput.Text = MainColor.Color.G.ToString();
                    dialogPickBrush.ColorBlueInput.Text = MainColor.Color.B.ToString();
                });
        }

        private void SetMinor_Click(object sender, RoutedEventArgs e)
        {
            Globals.CurrentApp.MainWindow.DoMessagePickBrushDialog("@LOCALE_DialogSetMinorColor_Content", "@LOCALE_DialogSetMinorColor_Title"
                , new List<Action<object>> {
                    (_)=> {
                        if (_ is not DialogMessage dialogMessage)
                            throw new ArgumentException("Argument is not DialogMessage", nameof(_));

                        if (dialogMessage.MessageContent is not DialogPickBrush dialogPickBrush)
                            throw new Exception("dialogMessage.MessageContent is not DialogPickBrush dialogPickBrush");

                          MinorColor = dialogPickBrush.GetColorBrush();
                    } },

                (_) =>
                {
                    if (_ is not DialogMessage dialogMessage)
                        throw new ArgumentException("Argument is not DialogMessage", nameof(_));

                    if (dialogMessage.MessageContent is not DialogPickBrush dialogPickBrush)
                        throw new Exception("dialogMessage.MessageContent is not DialogPickBrush dialogPickBrush");

                    dialogPickBrush.ColorAlphaInput.Text = MinorColor.Color.A.ToString();
                    dialogPickBrush.ColorRedInput.Text = MinorColor.Color.R.ToString();
                    dialogPickBrush.ColorGreenInput.Text = MinorColor.Color.G.ToString();
                    dialogPickBrush.ColorBlueInput.Text = MinorColor.Color.B.ToString();
                });
        }
    }
}

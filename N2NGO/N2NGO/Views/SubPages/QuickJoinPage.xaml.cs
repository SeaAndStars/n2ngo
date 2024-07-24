using N2NGO.UtilsClass;
using N2NGO.Views.SubPages.Dialogs;
using N2NGO.Views.SubPages.Dialogs.MessageDialogs;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace N2NGO.Views.SubPages
{
    /// <summary>
    /// QuickJoinPage.xaml 的交互逻辑
    /// </summary>
    public partial class QuickJoinPage : Page
    {
        public QuickJoinPage()
        {
            InitializeComponent();

            IsPasswdNeeded_Click(null, null);
        }

        public static async void Join(bool needPassword, string roomCode, string roomPassword)
        {
            SharedData.CurrentApp.LeaveRoom();

            roomCode = roomCode.Trim();
            roomPassword = roomPassword.Trim();
            roomPassword = needPassword ? roomPassword : SharedData.DefaultRoomPassword;

            if (string.IsNullOrEmpty(roomCode))
            {
                SharedData.CurrentApp.MainView.DoMessageDialog("@LOCALE_DialogJoinRoom_Fail_Code_Empty_Content", "@LOCALE_DialogJoinRoom_Title");
                return;
            }

            if (needPassword == true && string.IsNullOrEmpty(roomPassword.Trim()))
            {
                SharedData.CurrentApp.MainView.DoMessageDialog("@LOCALE_DialogJoinRoom_Fail_Passowrd_Empty_Content", "@LOCALE_DialogJoinRoom_Title");
                return;
            }

            DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += async (_, __) =>
            {
                if (SharedData.N2NEdgeLogHelper.IsEdgeConnectedToSupernode(SharedData.CurrentApp.N2NExecLog.LogOut))
                {
                    timer.Stop();

                    if (string.IsNullOrEmpty(roomCode))
                    {
                        SharedData.CurrentApp.Dispatcher.InvokeAsync(() => SharedData.CurrentApp.MainView.DoMessageDialog("未获取到房间号", "进入房间失败"));
                        SharedData.CurrentApp.LeaveRoom();
                    }
                    else
                    {
                        var enterRoomResult = await Task.Run(() => SharedData.CurrentApp.EnterRoom(roomCode, roomPassword));
                        if (!enterRoomResult)
                        {
                            SharedData.CurrentApp.Dispatcher.InvokeAsync(() => SharedData.CurrentApp.MainView.DoMessageDialog("进入房间失败", "进入房间失败"));
                        }
                    }
                }
            };

            timer.Start();

            string cmd = SharedData.EdgePath + " -c " + roomCode + " -k " + roomPassword + " -l " + $"{SharedData.CurrentApp.N2NGOServerConnection.ServerIPEndPoint.Address}:{SharedData.CurrentApp.N2NGOServerConnection.ServerSupernodePort}";

            //EdgeInvoker edgeInvoker = new();
            //edgeInvoker.PushArgs(" -c " + roomCode + " -k " + (needPassword ? roomPassword : SharedData.DefaultRoomPassword) + " -l " + SharedData.n2nServerIPP);
            //edgeInvoker.Call();


            if (await SharedData.CurrentApp.N2NExecLog.ExecuteAsync(cmd, true) is null)
            {
                SharedData.CurrentApp.Dispatcher.Invoke(() => SharedData.CurrentApp.MainView.DoMessageDialog("您当前仍有其他正在进入房间的任务，请查看日志", "进入房间终止"));

                timer.Stop();
                return;
            }

            if (SharedData.CurrentApp.N2NExecLog.LogOut.Contains("No Windows tap devices found, did you run tapinstall.exe?"))
            {
                SharedData.CurrentApp.MainView.DoMessageYesNoDialog("我们无法找到可用的Tap设备，请检查并安装Tap虚拟网卡驱动，要现在安装吗？", "进入房间失败",
                    new()
                    {
                        (_) =>
                        {
                            if (_ is not DialogMessage dialogMessage || dialogMessage.MessageContent is not DialogYesNo dialogYesNo)
                                throw new Exception("Cannot get DialogYesNo");

                            if (dialogYesNo.YesNo == DialogYesNo.YesNoE.Yes)
                                SharedData.CurrentApp.Dispatcher.Invoke(()=>SharedData.CurrentApp.MainView.PageSettings.InstallTapButton_Click(null, null));
                        }
                    }
                    );
                timer.Stop();
            }

            timer.Stop();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogJoiningRoom.Opacity = 0;
            DialogJoiningRoom.Visibility = Visibility.Visible;
            TaskCompletionSource<object> animationCompletedTask = new();
            DoubleAnimation _loadingDialogFadeIn = new() { To = 1, Duration = TimeSpan.FromSeconds(0.20), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            _loadingDialogFadeIn.Completed += (s, _) =>
            {
                animationCompletedTask.SetResult(0);
            };
            Dispatcher.Invoke(() => DialogJoiningRoom.BeginAnimation(OpacityProperty, _loadingDialogFadeIn));

            await Task.Run(() =>
            {
                animationCompletedTask.Task.Wait();

                Dispatcher.Invoke(() => Join(IsPasswdNeeded.IsChecked == true, RoomConnectText.Text, RoomPasswordText.Text));
            });

            TaskCompletionSource<object> animationCompletedTask1 = new();
            DoubleAnimation _loadingDialogFadeOut = new() { To = 0, Duration = TimeSpan.FromSeconds(0.45), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            _loadingDialogFadeOut.Completed += (s, _) =>
            {
                animationCompletedTask1.SetResult(0);
            };
            DialogJoiningRoom.BeginAnimation(OpacityProperty, _loadingDialogFadeOut);

            await Task.Run(() =>
            {
                animationCompletedTask1.Task.Wait();

                Dispatcher.Invoke(() =>
                {
                    DialogJoiningRoom.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void IsPasswdNeeded_Click(object? sender, RoutedEventArgs? e)
        {
            if (IsPasswdNeeded.IsChecked != null)
            {
                bool bNeedPassword = IsPasswdNeeded.IsChecked.Value;
                GroupPasswordInput.IsEnabled = bNeedPassword;
                if (bNeedPassword)
                {
                    SharedData.CurrentApp.MainView.DoMessageDialog("@LOCALE_DialogCreateRoomSecurity_Content", "@LOCALE_DialogCreateRoomSecurity_Title");
                }
            }
        }
    }
}

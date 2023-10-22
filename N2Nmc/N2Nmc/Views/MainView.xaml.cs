using HandyControl.Tools.Extension;
using Microsoft.Win32;
using N2Nmc.Resources;
using N2Nmc.UtilsClass;
using N2Nmc.Views.SubPages;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using N2Nmc_Protocol;
using System.Collections.Generic;
using HandyControl.Controls;
using MessageBox = HandyControl.Controls.MessageBox;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

namespace N2Nmc.Views
{

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>

    public partial class MainView : System.Windows.Window
    {
        public readonly Brush BrushButtonUnselected = new SolidColorBrush(Color.FromRgb(211, 211, 211));
        public readonly Brush BrushButtonSelected = new SolidColorBrush(Color.FromRgb(211, 229, 240));

        public static bool EnableAnimation = true;

        public RoomsPage? pageRooms { get; private set; } = null;
        public RoomingPage? pageRooming { get; private set; } = null;
        public QuickJoinPage? pageQuickJoin { get; private set; } = null;
        public LogPage? pageLog { get; private set; } = null;
        public SettingsPage? pageSettings { get; private set; } = null;
        public DefaultPage? pageDefault { get; private set; } = null;
        public RoomPage? pageRoom { get; private set; } = null;

        // 淡入动画
        DoubleAnimation fadeInAnimation = new DoubleAnimation { From = 0.15, To = 1, Duration = TimeSpan.FromSeconds(0.4) };
        DoubleAnimation fadeInAnimationSlowEx = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.5), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        // 淡出动画
        DoubleAnimation fadeOutAnimationEx = new DoubleAnimation { From = 1, To = 0, Duration = TimeSpan.FromSeconds(0.3), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        // 滑入动画
        DoubleAnimation slideInAnimation = new DoubleAnimation { From = 80, To = 800, Duration = TimeSpan.FromSeconds(0.5), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
        DoubleAnimation slideInNewAnimation = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.45), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };

        // 其它动画
        DoubleAnimation biggerInd = new DoubleAnimation { To = 60, Duration = TimeSpan.FromSeconds(0.5) };
        DoubleAnimation smallerInd = new DoubleAnimation { To = 40, Duration = TimeSpan.FromSeconds(0.5) };
        DoubleAnimation blurIn = new DoubleAnimation { To = 8, Duration = TimeSpan.FromSeconds(0.25), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        DoubleAnimation blurOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.55), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        DoubleAnimation blurInFast = new DoubleAnimation { To = 8, Duration = TimeSpan.FromSeconds(0.25) };
        DoubleAnimation blurOutFast = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.20) };
        ColorAnimation btnDownAnimation = new ColorAnimation { To = Color.FromArgb(0x48, 0x80, 0x80, 0x80), Duration = TimeSpan.FromMilliseconds(100) };
        ColorAnimation btnUpAnimation = new ColorAnimation { To = Color.FromArgb(0x0f, 0xb3, 0xb3, 0xb3), Duration = TimeSpan.FromMilliseconds(300) };
        ColorAnimation btnEnterAnimation = new ColorAnimation { To = Color.FromArgb(0x50, 0xe0, 0xe0, 0xe0), Duration = TimeSpan.FromMilliseconds(120) };
        ColorAnimation btnLeaveAnimation = new ColorAnimation { To = Color.FromArgb(0x0f, 0xd3, 0xd3, 0xd3), Duration = TimeSpan.FromMilliseconds(170) };

        DispatcherTimer n2nmc_server_reconnect_timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };

        bool canClose = false;
        bool NoBlurOut = false;

        public MainView()
        {
            InitializeComponent();

            SharedData.configFile = new EasyConfig("Data/config.ini");
            SharedData.EdgeConnectionInfo.IsConnectedToEdge = false;

            ButtonRoom.Visibility = Visibility.Collapsed;
            HandyControl.Controls.Growl.GrowlPanel = PanelMsg;

            Opacity = 0;
            EnableAnimation = SharedData.configFile.Get("DisableAnimation", "0") == "0" ? true : false; // 读取配置：是否启用动画

            foreach (Border btn in Buttons1.Children)
            {
                btn.MouseEnter += Btn_MouseEnter;
                btn.MouseLeave += Btn_MouseLeave;
                btn.MouseLeftButtonDown += Btn_MouseDown;
                btn.MouseLeftButtonUp += Btn_MouseUp;

                btn.Background = new SolidColorBrush(Color.FromArgb(0x0f, 0xd3, 0xd3, 0xd3)); // reinit
            }
            foreach (Border btn in Buttons2.Children)
            {
                btn.MouseEnter += Btn_MouseEnter;
                btn.MouseLeave += Btn_MouseLeave;
                btn.MouseLeftButtonDown += Btn_MouseDown;
                btn.MouseLeftButtonUp += Btn_MouseUp;

                btn.Background = new SolidColorBrush(Color.FromArgb(0x0f, 0xd3, 0xd3, 0xd3)); // reinit
            }

            n2nmc_server_reconnect_timer.Tick += (_, __) =>
            {
                if (_ == null)
                    throw new ArgumentNullException(nameof(n2nmc_server_reconnect_timer), "n2nmc_server_reconnect_timer.Tick Arg object is null.");

                lock (SharedData.NM_Connection)
                {
                    if (SharedData.NM_Connection.IsConnected())
                    {
                        SharedData.NM_Connection.LockingVars.INC();
                        ((DispatcherTimer)_).Stop();
                        return;
                    }


                    SharedData.NM_Connection.disconnectedGrowlInfo.Message = "是否要尝试重新连接？";
                    HandyControl.Controls.Growl.Ask(SharedData.NM_Connection.disconnectedGrowlInfo);
                    SharedData.NM_Connection.LockingVars.DEC();
                }

                ((DispatcherTimer)_).Stop();
                return;
            };

            lock (SharedData.NM_Connection)
            {
                SharedData.NM_Connection.disconnectedGrowlInfo.ActionBeforeClose = (bool b) =>
                {
                    lock (SharedData.NM_Connection.LockingVars)
                    {
                        if (SharedData.NM_Connection.LockingVars.disconnectedGrowlInfoAction > 0)
                            return true;

                        SharedData.NM_Connection.LockingVars.INC();
                    }
                    if (b)
                        Task.Run(() => SharedData.ConnectAndPeek());
                    else
                    {
                        n2nmc_server_reconnect_timer.Start();
                    }

                    return true;
                };
            }

            // 初始化页面实例
            pageRooms = new RoomsPage();
            pageRooming = new RoomingPage();
            pageQuickJoin = new QuickJoinPage();
            pageLog = new LogPage();
            pageSettings = new SettingsPage();
            pageDefault = new DefaultPage();
            pageRoom = new RoomPage();

        }

        public void EnterRoom(string node)
        {
            if (pageRoom == null)
                throw new NullReferenceException(nameof(pageRoom));
            pageRoom.EnterRoom(node);

            ButtonRooms.Visibility = Visibility.Collapsed;
            ButtonRooming.Visibility = Visibility.Collapsed;
            ButtonQuickJoin.Visibility = Visibility.Collapsed;
            ButtonRoom.Visibility = Visibility.Visible;
            NavigatePage(pageRoom, null);
        }

        public void LeaveRoom()
        {
            if (pageRoom == null)
                throw new NullReferenceException(nameof(pageRoom));

            ButtonRooms.Visibility = Visibility.Visible;
            ButtonRooming.Visibility = Visibility.Visible;
            ButtonQuickJoin.Visibility = Visibility.Visible;
            ButtonRoom.Visibility = Visibility.Collapsed;
            pageRoom.ExitRoom();
            NavigatePage(pageDefault, null);
        }

        private void Btn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((Border)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, btnUpAnimation);
        }

        private void Btn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((Border)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, btnDownAnimation);
        }

        private void Btn_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, btnLeaveAnimation);
        }

        private void Btn_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, btnEnterAnimation);
        }

        string lastIndPname = "";
        private async void NavigatePage(Page? page, object? btn)
        {
            if (page != null)
            {
                if (Framez.Content == page)
                    return;

                // 计算指示器动画的逻辑
                ThicknessAnimation? thicknessAnimation = null;
                if (btn != null)
                {
                    PageIndicator.Visibility = Visibility.Visible;
                    StackPanel btnParent = ((StackPanel)((Border)btn).Parent);

                    if (lastIndPname != btnParent.Name)
                    {
                        PageIndicator.BeginAnimation(HeightProperty, btnParent.Name == "Buttons1" ? biggerInd : smallerInd);
                    }

                    lastIndPname = btnParent.Name;

                    Thickness CurrentIndMargin = (Thickness)PageIndicator.GetValue(MarginProperty);
                    Point BtnPos = ((Border)btn).TranslatePoint(new Point(0, 0), Buttons1);
                    thicknessAnimation = new ThicknessAnimation
                    {
                        From = new Thickness(0, CurrentIndMargin.Top, 0, 0),
                        To = new Thickness(0, ((Border)btn).GetValidHeight()/2+BtnPos.Y- (btnParent.Name == "Buttons1" ? biggerInd.To==null?2:biggerInd.To.Value : smallerInd.To == null ? 2 : smallerInd.To.Value) / 2, 0, 0),
                        Duration = TimeSpan.FromSeconds(0.25),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    PageIndicator.Visibility = Visibility.Visible;
                }
                else
                    PageIndicator.Visibility = Visibility.Collapsed;

                if (!EnableAnimation)
                {
                    Framez.Navigate(page);
                    PageIndicator.Visibility = Visibility.Collapsed;
                    return;
                }

                // 开始动画
                page.Opacity = 0;
                FrameOverlay.Height = FramezGrid.GetValidHeight();

                DoubleAnimation blurIn = new DoubleAnimation { To = 8, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
                var animationCompletedTask = new TaskCompletionSource<object>();
                blurIn.Completed += (_, __) =>
                {
                    animationCompletedTask.SetResult(0);
                };

                BlurFramez.BeginAnimation(BlurEffect.RadiusProperty, blurIn);
                if (thicknessAnimation != null)
                    PageIndicator.BeginAnimation(MarginProperty, thicknessAnimation);

                await Task.Run(() =>
                {
                    animationCompletedTask.Task.Wait();

                    Dispatcher.BeginInvoke(() =>
                    {
                        Framez.Navigate(page);
                        page.BeginAnimation(OpacityProperty, fadeInAnimation);
                        FrameOverlay.BeginAnimation(OpacityProperty, fadeOutAnimationEx);
                        BlurFramez.BeginAnimation(BlurEffect.RadiusProperty, blurOut);

                        NoBlurOut = true;
                    });
                });
            }
        }
        private void RootPageBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigatePage(pageDefault, sender);
        }
        private void ButtonRooms_Click(object sender, RoutedEventArgs e)
        {
            NavigatePage(pageRooms, sender);
        }
        private void ButtonRooming_Click(object sender, RoutedEventArgs e)
        {
            NavigatePage(pageRooming, sender);
        }
        private void ButtonQuickJoin_Click(object sender, RoutedEventArgs e)
        {
            NavigatePage(pageQuickJoin, sender);
        }
        private void ButtonLog_Click(object sender, RoutedEventArgs e)
        {
            NavigatePage(pageLog, sender);
        }
        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            NavigatePage(pageSettings, sender);
        }
        private void ButtonRoom_Click(object sender, MouseButtonEventArgs e)
        {
            NavigatePage(pageRoom, sender);
        }

        static string Run(string command, bool noWindow = false)
        {
            Process process = new Process();

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/c " + command;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = noWindow;

            process.StartInfo = startInfo;

            // 启动进程
            process.Start();

            // 等待进程结束
            process.WaitForExit();

            // 读取并返回输出结果
            return process.StandardOutput.ReadToEnd();
        }

        private async void CloseExit()
        {
            canClose = false;
            if (SharedData.configFile?.Get("NeedUpdate", "0") == "1")
                HandyControl.Controls.Growl.SuccessGlobal("开始退出并更新 N2Nmc ...");
            else
                HandyControl.Controls.Growl.SuccessGlobal("开始退出 N2Nmc ...");
            SharedData.NM_Connection.Close();

            TaskCompletionSource<object> animationCompletedTask = new TaskCompletionSource<object>();
            fadeOutAnimationEx.Completed += (s, _) =>
            {
                animationCompletedTask.SetResult(0);
            };

            BeginAnimation(OpacityProperty, fadeOutAnimationEx);

            await Task.Run(() =>
            {
                // 等待动画完成
                animationCompletedTask.Task.Wait();

                Dispatcher.Invoke(() =>
                {
                    SharedData.ExitRoom();

                    SharedData.configFile?.SaveConfigDataToFile();

                    if (File.Exists(SharedData.updateClientExecFile))
                        try { File.Delete(SharedData.updateClientExecFile); } catch { }

                    File.Copy(SharedData.updateClientPath, SharedData.updateClientExecFile);
                    if (SharedData.configFile?.Get("NeedUpdate", "0") == "1")
                    {
                        try
                        {
                            new Process { StartInfo = new ProcessStartInfo { FileName = SharedData.updateClientExecFile, Arguments = Process.GetCurrentProcess().Id.ToString() } }.Start();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("（请尝试下次以管理员运行N2Nmc用于更新。）\n在启动升级程序时发生异常："+ex.Message,"升级", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    HandyControl.Controls.Growl.SuccessGlobal("N2Nmc 已退出...");
                    canClose = true;
                    Close();
                });
            });
        }

        //关闭按钮
        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            CloseExit();
        }
        //窗口移动
        private void WinMove_main(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
        //最小化窗口
        private void ButtonWindowMin_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private async void AsyncLoading()
        {
            NavigatePage(pageDefault, null);

            await Task.Run(() =>
            {
                if (pageRooms == null)
                    throw new NullReferenceException(nameof(pageRooms));
                SharedData.ConnectAndPeek();
                Dispatcher.Invoke(()=> pageRooms.Refresh());

                if (SharedData.configFile?.Get("FirstRun", "1") == "1")
                {
                    Dispatcher.BeginInvoke(() => HandyControl.Controls.Growl.Ask(new HandyControl.Data.GrowlInfo { CancelStr = "", Type = HandyControl.Data.InfoType.Info, ActionBeforeClose = (bool b) => { return b ? b : b; }, ShowCloseButton = false, Message = "N2Nmc需要配合Tap虚拟网卡来使用，如果您未安装，请前往设置-安装Tap驱动。" }));
                    SharedData.configFile.Set("FirstRun", "0");
                }

                if (SharedData.configFile?.Get("NeedUpdate", "0") == "1")
                {
                    Growl.Warning("N2Nmc 上一次更新未成功，将会在本次关闭后重新尝试。");
                }
                else
                    SharedData.CheckN2NClientUpdate();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    Run("Data/BinRef/Windows/WinIPBroadcast/WinIPBroadcast.exe run", true); // 运行WinIPBroadcast（数据转发到虚拟网卡）
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BeginAnimation(OpacityProperty, fadeInAnimationSlowEx);
            AsyncLoading();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!canClose)
            {
                CloseExit();
                e.Cancel = true;
                return;
            }
        }

        private void GridLeft_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!EnableAnimation)
                return;

            GridLeft.BeginAnimation(WidthProperty, new DoubleAnimation { To = 138, Duration = TimeSpan.FromSeconds(0.30), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });

            if (NoBlurOut)
            {
                NoBlurOut = false;
                return;
            }

            BlurFramez.BeginAnimation(BlurEffect.RadiusProperty, blurInFast);
        }

        private void GridLeft_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!EnableAnimation)
                return;

            GridLeft.BeginAnimation(WidthProperty, new DoubleAnimation { To = 65, Duration = TimeSpan.FromSeconds(0.25), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });

            if (NoBlurOut)
            {
                NoBlurOut = false;
                return;
            }

            BlurFramez.BeginAnimation(BlurEffect.RadiusProperty, blurOutFast);
        }

        //Point ___tmp_pos = new Point();
        //private void Window_MouseMove(object sender, MouseEventArgs e)
        //{
        //    ___tmp_pos = e.GetPosition(this);
        //    ___tmp_pos.X = ___tmp_pos.X / ImageBackgroundImage.RenderSize.Width /2;
        //    ___tmp_pos.Y = ___tmp_pos.Y / ImageBackgroundImage.RenderSize.Height/2;

        //    ImageBackgroundImage.RenderTransformOrigin = ___tmp_pos;
        //}
    }
}

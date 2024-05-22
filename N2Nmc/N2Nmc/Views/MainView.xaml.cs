using HandyControl.Tools.Extension;
using N2Nmc.UtilsClass;
using N2Nmc.Views.SubPages;
using N2Nmc.Views.SubPages.Dialogs;
using N2Nmc.Views.SubPages.Dialogs.MessageDialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MessageBox = HandyControl.Controls.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

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
        public IndexPage? pageIndex { get; private set; } = null;
        public RoomPage? pageRoom { get; private set; } = null;
        public InfoPage? pageInfo { get; private set; } = null;

        // 淡入动画
        DoubleAnimation fadeInAnimation = new DoubleAnimation { From = 0.15, To = 1, Duration = TimeSpan.FromSeconds(0.4) };
        DoubleAnimation fadeInAnimationEx = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(1.5), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
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

        DispatcherTimer n2nmc_server_reconnect_timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };

        bool canClose = false;

        Dictionary<string, BitmapImage?> imgIcons = new Dictionary<string, BitmapImage?>();

        public static void PrintMemSet(string? Tag = null)
        {
            Process currentProcess = Process.GetCurrentProcess();

            long memoryUsage = currentProcess.WorkingSet64;
            double memoryUsageInMB = memoryUsage / (1024 * 1024);

            Console.WriteLine(string.Format("({1})Memory usage: {0} MBytes", memoryUsageInMB, Tag));
        }

        void ShaderImages()
        {
            PrintMemSet("Before Image Proc");
            Stopwatch sw = Stopwatch.StartNew();

            imgIcons["DayNight_Light"] = null;
            imgIcons["Join_Light"] = null;
            imgIcons["Log_Light"] = null;
            imgIcons["Room_Light"] = null;
            imgIcons["Rooming_Light"] = null;
            imgIcons["Rooms_Light"] = null;
            imgIcons["Root_Light"] = null;
            imgIcons["Settings_Light"] = null;
            imgIcons["Info_Light"] = null;
            imgIcons["Wnd_Btn_Close_Light"] = null;
            imgIcons["Wnd_Btn_Max_Light"] = null;
            imgIcons["Wnd_Btn_Normal_Light"] = null;
            imgIcons["Wnd_Btn_Min_Light"] = null;
            imgIcons["Wnd_Btn_Back_Light"] = null;

            imgIcons["DayNight_Dark"] = null;
            imgIcons["Join_Dark"] = null;
            imgIcons["Log_Dark"] = null;
            imgIcons["Room_Dark"] = null;
            imgIcons["Rooming_Dark"] = null;
            imgIcons["Rooms_Dark"] = null;
            imgIcons["Root_Dark"] = null;
            imgIcons["Settings_Dark"] = null;
            imgIcons["Info_Dark"] = null;
            imgIcons["Wnd_Btn_Close_Dark"] = null;
            imgIcons["Wnd_Btn_Max_Dark"] = null;
            imgIcons["Wnd_Btn_Normal_Dark"] = null;
            imgIcons["Wnd_Btn_Min_Dark"] = null;
            imgIcons["Wnd_Btn_Back_Dark"] = null;

            if (!Directory.Exists(SharedData.ShaderCacheDir))
            {
                Directory.CreateDirectory(SharedData.ShaderCacheDir);
            }
            string hashFileExt = ".md5Hash";

            foreach (var icon in imgIcons)
            {
                string PathIconShaderCache = Path.Combine(SharedData.ShaderCacheDir, icon.Key + ".png");
                string PathIconShaderCacheHash = PathIconShaderCache + hashFileExt;
                if (File.Exists(Path.Combine(PathIconShaderCache)) && File.Exists(Path.Combine(PathIconShaderCacheHash)))
                {
                    Console.WriteLine("[Shader] Image shader cache hit! ({0})", PathIconShaderCache);
                    var IconData = File.ReadAllBytes(PathIconShaderCache);

                    var hashNow = MD5.HashData(IconData);
                    var hashOri = File.ReadAllBytes(PathIconShaderCacheHash);
                    if (MemoryExtensions.SequenceEqual(hashNow.AsSpan(), hashOri))
                    {
                        var s = new MemoryStream(IconData, false);
                        BitmapImage b = new BitmapImage();
                        b.BeginInit();
                        b.CacheOption = BitmapCacheOption.OnLoad;
                        b.StreamSource = s;
                        b.EndInit();
                        s.DisposeAsync();
                        b.Freeze();

                        //Dispatcher.InvokeAsync(() =>
                        //{
                        imgIcons[icon.Key] = b;
                        //});
                        continue;
                    }
                    else
                        Console.WriteLine("[Shader] Image shader cache error: {0} ! Shading.");
                }
                else
                    Console.WriteLine("[Shader] Image shader cache: {0} not found! Shading.", PathIconShaderCache);

                ImageProcessor.ImageFactory imageFactory = new ImageProcessor.ImageFactory();

                var path = new Uri("/Data/image/icon/" + icon.Key.Replace(icon.Key.Contains("_Light") ? "_Light" : icon.Key.Contains("_Dark") ? "_Dark" : throw new Exception("Cannot find valid img"), "").ToLower() + ".png", UriKind.Relative);
                var resc = App.GetResourceStream(path).Stream;
                imageFactory.Load(resc);

                if (icon.Key.Contains("_Light"))
                {
                    //imageFactory.ReplaceColor(System.Drawing.Color.FromArgb(255, 0, 0, 0), System.Drawing.Color.FromArgb(255, 0x6A, 0x6A, 0x6A), 20);
                    imageFactory.Tint(System.Drawing.Color.FromArgb(220, 76, 76, 76));
                }
                if (icon.Key.Contains("_Dark"))
                {
                    //imageFactory.ReplaceColor(System.Drawing.Color.FromArgb(255, 0, 0, 0), System.Drawing.Color.FromArgb(255, 0xDF, 0xDF, 0xDF), 20);
                    imageFactory.Tint(System.Drawing.Color.FromArgb(188, 159, 169, 237));
                }
                var stream = new MemoryStream();
                imageFactory.Save(stream);

                imageFactory.Save(PathIconShaderCache);
                File.WriteAllBytesAsync(PathIconShaderCacheHash, MD5.HashData(stream.ToArray()));

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                stream.DisposeAsync();
                bitmapImage.Freeze();

                //Dispatcher.Invoke(() =>
                //{
                imgIcons[icon.Key] = bitmapImage;

                //new DialogImageView(bitmapImage, string.Format("{0} - [{1}]", icon.Key, path)).Show();
                //});


                // conti:
                imageFactory.Dispose();
                continue;
            }
            sw.Stop();

            PrintMemSet("After Image Proc");
            Console.WriteLine("[Shader] Total time: {0}ms", sw.ElapsedMilliseconds);
            Dispatcher.Invoke(() => SwitchColorNightDay(SharedData.configFile?.Get("DayNightColorMode", "0") == "1"));

            return;
        }


        public MainView()
        {
            InitializeComponent();
            SharedData.configFile = new EasyConfig("Data/config.ini");  // Do this first

            // 初始化页面实例
            pageRooms = new RoomsPage();
            pageRooming = new RoomingPage();
            pageQuickJoin = new QuickJoinPage();
            pageLog = new LogPage();
            pageSettings = new SettingsPage();
            pageIndex = new IndexPage();
            pageRoom = new RoomPage();
            pageInfo = new InfoPage();

            Dispatcher.Invoke(() => SwitchColorNightDay(SharedData.configFile?.Get("DayNightColorMode", "1") == "1"));

            var r = App.Current.Resources;
            var MainColorBrush = (SolidColorBrush)r["MainColorSolidBrush"];

            Task.Run(() => ShaderImages());

            SharedData.EdgeConnectionInfo.IsConnectedToEdge = false;

            //BlurFramez.Radius = 0;
            HandyControl.Controls.Growl.GrowlPanel = PanelMsg;

            Opacity = 0;
            EnableAnimation = SharedData.configFile.Get("DisableAnimation", "0") == "0" ? true : false; // 读取配置：是否启用动画
            SharedData.configFile.Get("UserNickname", "NewToGO");

            int _alpha = 255;
            var s = SharedData.configFile.Get("WindowBackgroundAlpha", "245");  // 245 190 198
            int.TryParse(s, out _alpha);
            SetBackColor((byte?)_alpha);
            pageSettings.BackgroundOSlider.Value = _alpha;

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
        }

        public void EnterRoom(string node)
        {
            if (pageRoom == null)
                throw new NullReferenceException(nameof(pageRoom));
            pageRoom.EnterRoom(node);

            // TODO: Remaster
            //NavigatePage(pageRoom, ButtonRoom);
        }

        public void LeaveRoom()
        {
            if (pageRoom == null)
                throw new NullReferenceException(nameof(pageRoom));

            pageRoom.ExitRoom();
            // TODO: Remaster
            //NavigatePage(pageDefault, RootPageBtn);
        }

        public void RefreshUIAnimations()
        {
            if (pageRooms == null ||
                pageRooming == null ||
                pageQuickJoin == null ||
                pageLog == null ||
                pageSettings == null ||
                pageIndex == null ||
                pageRoom == null ||
                pageInfo == null
                )
                throw new NullReferenceException("Refreshing Buttons");

            {
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>(this));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)pageRooms.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)pageRooming.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)pageQuickJoin.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)pageLog.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)pageSettings.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)pageIndex.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)pageRoom.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)pageInfo.Content));


                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>(this));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)pageRooms.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)pageRooming.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)pageQuickJoin.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)pageLog.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)pageSettings.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)pageIndex.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)pageRoom.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)pageInfo.Content));


                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>(this));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)pageRooms.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)pageRooming.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)pageQuickJoin.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)pageLog.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)pageSettings.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)pageIndex.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)pageRoom.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)pageInfo.Content));
            }

            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>(this));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)pageRooms.Content));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)pageRooming.Content));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)pageQuickJoin.Content));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)pageLog.Content));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)pageSettings.Content));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)pageIndex.Content));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)pageRoom.Content));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)pageInfo.Content));
        }

        public void DoMessageInputDialog(string MessageText = "", string? MessageTitle = null, List<Action<object>>? ActsRet = null, Action<object>? ActPreRun = null)
        {
            Frame? m = null;

            if (ActsRet == null)
                ActsRet = new List<Action<object>>();
            ActsRet.Add((_) =>
            {
                if (m == null)
                    throw new NullReferenceException("MessageInputDialog Callbacks");

                Dispatcher.Invoke(() =>
                {
                    MessageDialogs.Children.Remove(m);
                });
            });
            var dm = new DialogMessage(MessageText, MessageTitle, DialogMessage.DialogType.InputOK, ActsRet);
            m = new Frame { Content = dm, BorderThickness = new Thickness(0), BorderBrush = null };

            if (ActPreRun != null)
                ActPreRun(dm);

            MessageDialogs.Children.Add(m);
        }
        public void DoMessageYesNoDialog(string MessageText = "", string? MessageTitle = null, List<Action<object>>? ActsRet = null, Action<object>? ActPreRun = null)
        {
            Frame? m = null;

            if (ActsRet == null)
                ActsRet = new List<Action<object>>();
            ActsRet.Add((_) =>
            {
                if (m == null)
                    throw new NullReferenceException("MessageYesNoDialog Callbacks");

                Dispatcher.Invoke(() =>
                {
                    MessageDialogs.Children.Remove(m);
                });
            });
            var dm = new DialogMessage(MessageText, MessageTitle, DialogMessage.DialogType.YesNo, ActsRet);
            m = new Frame { Content = dm, BorderThickness = new Thickness(0), BorderBrush = null };

            if (ActPreRun != null)
                ActPreRun(dm);

            MessageDialogs.Children.Add(m);
        }
        public void DoMessageDialog(string MessageText = "", string? MessageTitle = null, List<Action<object>>? ActsRet = null, Action<object>? ActPreRun = null)
        {
            Frame? m = null;

            if (ActsRet == null)
                ActsRet = new List<Action<object>>();
            ActsRet.Add((_) =>
            {
                if (m == null)
                    throw new NullReferenceException("MessageDialog Callbacks");

                Dispatcher.Invoke(() =>
                {
                    MessageDialogs.Children.Remove(m);
                });
            });
            var dm = new DialogMessage(MessageText, MessageTitle, DialogMessage.DialogType.OK, ActsRet);
            m = new Frame { Content = dm, BorderThickness = new Thickness(0), BorderBrush = null };

            if (ActPreRun != null)
                ActPreRun(dm);

            MessageDialogs.Children.Add(m);
        }


        //string lastIndPname = "";
        object? ButtonLastBack = null;
        public void NavigatePage(Page? page)
        {
            if (page == null)
            {
                if (Framez.CanGoBack)
                    Framez.GoBack();
                return;
            }

            if (Framez.Content == page)
                return;


            if (!EnableAnimation)
            {
                Framez.Navigate(page);
                return;
            }

            // 开始动画
            page.Opacity = 0;
            FrameOverlay.Height = FramezGrid.GetValidHeight();

            Framez.Navigate(page);
            page.BeginAnimation(OpacityProperty, fadeInAnimation);
            FrameOverlay.BeginAnimation(OpacityProperty, fadeOutAnimationEx);
        }
        private void RootPageBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigatePage(pageIndex);
        }
        private void ButtonRooms_Click(object sender, RoutedEventArgs e)
        {
            pageRooms?.Refresh();
            NavigatePage(pageRooms);
        }
        private void ButtonRooming_Click(object sender, RoutedEventArgs e)
        {
            NavigatePage(pageRooming);
        }
        private void ButtonQuickJoin_Click(object sender, RoutedEventArgs e)
        {
            NavigatePage(pageQuickJoin);
        }
        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            NavigatePage(pageSettings);
        }
        private void ButtonRoom_Click(object sender, RoutedEventArgs e)
        {
            NavigatePage(pageRoom);
        }
        private void ButtonInfo_Click(object sender, RoutedEventArgs e)
        {
            NavigatePage(pageInfo);
            pageInfo?.PlayLogoAnimation();
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

            var at = new TaskCompletionSource<object>();
            fadeOutAnimationEx.Completed += (_, _) => at.SetResult(0);

            var rt = (this.RenderTransform as ScaleTransform);
            if (rt != null)
            {
                rt.CenterY = 0;
                rt.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation { To = 5.5, Duration = TimeSpan.FromSeconds(0.55), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });
            }
            BeginAnimation(OpacityProperty, fadeOutAnimationEx);

            await Task.Run(() =>
            {
                at.Task.Wait();

                Dispatcher.Invoke(() =>
                {
                    SharedData.ExitRoom();

                    SharedData.configFile?.SaveConfigDataToFile();
                    Console.WriteLine("(Exit) Config Wrote");

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
                            MessageBox.Show("（请尝试下次以管理员运行N2Nmc用于更新。）\n在启动升级程序时发生异常：" + ex.Message, "升级", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    HandyControl.Controls.Growl.SuccessGlobal("N2Nmc 已退出...");
                    canClose = true;
                    Close();
                });
            });
        }

        private void SetWindowMaxNormalButtonImage()
        {
            LBImage_Wnd_Btn_MaxNormal.Source = imgIcons["Wnd_Btn_" + (WindowState == WindowState.Maximized ? "Normal" : "Max") + (IsColorDay ? "_Light" : "_Dark")]; ;
        }
        public void SetBackColor(byte? a = null, byte? r = null, byte? g = null, byte? b = null)
        {
            var c = ((SolidColorBrush)App.Current.Resources["MainColorSolidBrush"]).Color;
            RootControl.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                a == null ? c.A : a.Value,
                r == null ? c.R : r.Value,
                g == null ? c.G : g.Value,
                b == null ? c.B : b.Value));
        }
        public bool IsColorDay { get; private set; } = false;
        public void SwitchColorNightDay(bool? manualDayNight = null)
        {
            /*
             * <LinearGradientBrush x:Key="MainColorBrush"/>
             * <LinearGradientBrush x:Key="MainColorAlphaBrush"/>
             * <SolidColorBrush x:Key="MainColorSolidBrush" />
             * <SolidColorBrush x:Key="MainColorSolidAlphaBrush"/>
             * <SolidColorBrush x:Key="MainColorSolidFontBrush"/>
             * <SolidColorBrush x:Key="MainColorHighLightSolidAlphaBrush"/>
             */
            var r = App.Current.Resources;
            var MainColorBrush = (LinearGradientBrush)r["MainColorBrush"];
            var MainColorAlphaBrush = (LinearGradientBrush)r["MainColorAlphaBrush"];
            var MainColorSolidBrush = (SolidColorBrush)r["MainColorSolidBrush"];
            var MainColorSolidAlphaBrush = (SolidColorBrush)r["MainColorSolidAlphaBrush"];
            var MainColorSolidFontBrush = (SolidColorBrush)r["MainColorSolidFontBrush"];
            var MainColorSolidForeBrush = (SolidColorBrush)r["MainColorSolidForeBrush"];
            var MainColorHighLightSolidAlphaBrush = (SolidColorBrush)r["MainColorHighLightSolidAlphaBrush"];
            Color? MainColorPrimaryColor = (Color)r["MainColorPrimaryColor"];
            Color? MainColorSecondaryColor = (Color)r["MainColorSecondaryColor"];

            //var orig_MainColorBrush = new LinearGradientBrush {  };
            //var orig_MainColorAlphaBrush = new LinearGradientBrush { };
            //var orig_MainColorSolidBrush = new SolidColorBrush { Color= MainColorSolidBrush.Color };
            //var orig_MainColorSolidAlphaBrush = new SolidColorBrush { };
            //var orig_MainColorSolidFontBrush = new SolidColorBrush { };
            //var orig_MainColorSolidForeBrush = new SolidColorBrush { };
            //var orig_MainColorHighLightSolidAlphaBrush = new SolidColorBrush { };

            if (MainColorBrush == null ||
                MainColorAlphaBrush == null ||
                MainColorSolidBrush == null ||
                MainColorSolidAlphaBrush == null ||
                MainColorSolidFontBrush == null ||
                MainColorSolidForeBrush == null ||
                MainColorHighLightSolidAlphaBrush == null ||
                MainColorPrimaryColor == null ||
                MainColorSecondaryColor == null
                )
                throw new NullReferenceException("SwitchColorNightDay Resources null");

            if (manualDayNight == null)
                IsColorDay = !IsColorDay;
            else
            {
                IsColorDay = manualDayNight.Value;
            }
            SharedData.configFile?.Set("DayNightColorMode", IsColorDay ? "1" : "0");

            if (IsColorDay)
            {
                MainColorBrush = (LinearGradientBrush)r["MainColorLightBrush"];
                MainColorAlphaBrush = (LinearGradientBrush)r["MainColorLightAlphaBrush"];
                MainColorSolidBrush = (SolidColorBrush)r["MainColorLightSolidBrush"];
                MainColorSolidAlphaBrush = (SolidColorBrush)r["MainColorLightSolidAlphaBrush"];
                MainColorSolidFontBrush = (SolidColorBrush)r["MainColorLightSolidFontBrush"];
                MainColorSolidForeBrush = (SolidColorBrush)r["MainColorLightSolidForeBrush"];
                MainColorHighLightSolidAlphaBrush = (SolidColorBrush)r["MainColorLightHighLightSolidAlphaBrush"];
                MainColorPrimaryColor = (Color)r["MainColorLightPrimaryColor"];
                MainColorSecondaryColor = (Color)r["MainColorLightSecondaryColor"];
            }
            else
            {
                MainColorBrush = (LinearGradientBrush)r["MainColorDarkBrush"];
                MainColorAlphaBrush = (LinearGradientBrush)r["MainColorDarkAlphaBrush"];
                MainColorSolidBrush = (SolidColorBrush)r["MainColorDarkSolidBrush"];
                MainColorSolidAlphaBrush = (SolidColorBrush)r["MainColorDarkSolidAlphaBrush"];
                MainColorSolidFontBrush = (SolidColorBrush)r["MainColorDarkSolidFontBrush"];
                MainColorSolidForeBrush = (SolidColorBrush)r["MainColorDarkSolidForeBrush"];
                MainColorHighLightSolidAlphaBrush = (SolidColorBrush)r["MainColorDarkHighLightSolidAlphaBrush"];
                MainColorPrimaryColor = (Color)r["MainColorDarkPrimaryColor"];
                MainColorSecondaryColor = (Color)r["MainColorDarkSecondaryColor"];
            }

            var imgs = SharedData.FindVisualChildren<Image>(this);
            foreach (Image cimg in imgs)
            {
                var n = cimg.Name;

                if (n == LBImage_Wnd_Btn_MaxNormal.Name)
                {
                    SetWindowMaxNormalButtonImage();
                    continue;
                }

                if (n.Contains("LBImage_"))
                {
                    n = n.Replace("LBImage_", "");

                    var img = imgIcons[n + (IsColorDay ? "_Light" : "_Dark")];
                    if (!(img == null))
                        cimg.Dispatcher.BeginInvoke(() => cimg.Source = img);
                }

                continue;
            }

            //r["MainColorBrush"] = orig_MainColorBrush;
            //r["MainColorAlphaBrush"] = orig_MainColorAlphaBrush;
            //r["MainColorSolidBrush"] = orig_MainColorSolidBrush;
            //r["MainColorSolidAlphaBrush"] = orig_MainColorSolidAlphaBrush;
            //r["MainColorSolidFontBrush"] = orig_MainColorSolidFontBrush;
            //r["MainColorHighLightSolidAlphaBrush"] = orig_MainColorHighLightSolidAlphaBrush;

            r["MainColorBrush"] = MainColorBrush;
            r["MainColorAlphaBrush"] = MainColorAlphaBrush;
            r["MainColorSolidBrush"] = MainColorSolidBrush;
            r["MainColorSolidAlphaBrush"] = MainColorSolidAlphaBrush;
            r["MainColorSolidFontBrush"] = MainColorSolidFontBrush;
            r["MainColorSolidForeBrush"] = MainColorSolidForeBrush;
            r["MainColorHighLightSolidAlphaBrush"] = MainColorHighLightSolidAlphaBrush;
            r["MainColorPrimaryColor"] = MainColorPrimaryColor;
            r["MainColorSecondaryColor"] = MainColorSecondaryColor;

            //var ScrollViewerStyle = (Style)r["ScrollViewerStyle"];
            //if (ScrollViewerStyle != null)
            //{
            //    ((SolidColorBrush)ScrollViewerStyle.Resources["PrimaryTextBrush"]) = new SolidColorBrush(r["MainColorPrimaryColor"]);
            //    ((SolidColorBrush)ScrollViewerStyle.Resources["SecondaryTextBrush"]) = new SolidColorBrush;
            //}

            SetBackColor(pageSettings != null ? (byte)pageSettings.BackgroundOSlider.Value : null);
            SharedData.UIAnimation.Refresh();
            RefreshUIAnimations();

            // orig_MainColorSolidBrush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation { To = MainColorSolidBrush.Color, EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });

            return;
        }

        private void ButtonUsername_Click(object sender, RoutedEventArgs e)
        {
            DoMessageInputDialog("编辑用户名：", "当前用户名"
                , new List<Action<object>> {
                    (_)=> {
                        var dm = _ as DialogMessage;
                        if (dm != null)
                        {
                            var di = dm.MessageContent as DialogInput;
                            if (di != null)
                            {
                                var newName = di.InputBox.Text;
                                SharedData.configFile?.Set("UserNickname", newName);
                            }
                        }
                    } },

                (_) =>
                {
                    var dm = _ as DialogMessage;
                    if (dm != null)
                    {
                        var di = dm.MessageContent as DialogInput;
                        if (di != null)
                        {
                            di.InputBox.Text = SharedData.configFile?.Get("UserNickname", null);
                            di.InputBox.SelectAll();
                        }
                    }
                });
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            CloseExit();
        }
        private void ButtonWindowMin_Click(object sender, RoutedEventArgs e)
        {
            var rt = (this.RenderTransform as ScaleTransform);
            if (rt != null)
            {
                var anim1 = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
                var at1 = new TaskCompletionSource<object>();
                anim1.Completed += (s, _) => at1.SetResult(0);

                rt.CenterY = 0;

                Task.Run(() =>
                {
                    at1.Task.Wait();
                    Dispatcher.Invoke(() => WindowState = WindowState.Minimized);
                });
                rt.BeginAnimation(ScaleTransform.ScaleYProperty, anim1);
            }
            else
                WindowState = WindowState.Minimized;
        }
        private void ButtonWindowMaxNormal_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            SetWindowMaxNormalButtonImage();
        }
        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            NavigatePage(null);
        }
        private void ButtonWindowColorSwitch_Click(object sender, RoutedEventArgs e)
        {
            SwitchColorNightDay();
        }
        private void ButtonLog_Click(object sender, RoutedEventArgs e)
        {
            NavigatePage(pageLog);
        }
        bool bLogShow = false;
        private void TitleIconButton_Click(object sender, RoutedEventArgs e)
        {
            bLogShow = !bLogShow;

            if (bLogShow)
                NavigatePage(pageLog);
            else
                NavigatePage(null);

        }
        private void WinMove_main(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private async void AsyncLoading()
        {
            var rt = (this.RenderTransform as ScaleTransform);
            if (rt != null)
            {
                rt.CenterY = this.Height;
                rt.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.55), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });
            }

            BeginAnimation(OpacityProperty, fadeInAnimationEx);


            NavigatePage(pageIndex);

            RefreshUIAnimations();

            DoMessageDialog("Hi！这里是N2N GO！", "欢迎！", new List<Action<object>>() { (_) => DoMessageDialog("当您看见这个，代表您正使用Dev开发版本。\n我们推荐您使用正式版本，您可以选择手动下载并切换，或者使用检测更新功能。", "欢迎！") });

            await Task.Run(() =>
            {
                if (pageRooms == null)
                    throw new NullReferenceException(nameof(pageRooms));
                SharedData.ConnectAndPeek();
                //Dispatcher.Invoke(() => pageRooms.Refresh());

                if (SharedData.configFile?.Get("FirstRun", "1") == "1")
                {
                    //Dispatcher.BeginInvoke(() => HandyControl.Controls.Growl.Ask(new HandyControl.Data.GrowlInfo { CancelStr = "", Type = HandyControl.Data.InfoType.Info, ActionBeforeClose = (bool b) => { return b ? b : b; }, ShowCloseButton = false, Message = "N2Nmc需要配合Tap虚拟网卡来使用，如果您未安装，请前往设置-安装Tap驱动。" }));
                    Dispatcher.InvokeAsync(() => DoMessageDialog("如果您未安装Tap驱动，请前往设置页面安装，如果您是第一次使用N2N GO，我们强烈建议您安装一次。", "首次运行"));
                    SharedData.configFile.Set("FirstRun", "0");
                }

                if (SharedData.configFile?.Get("NeedUpdate", "0") == "1")
                {
                    DoMessageDialog("N2Nmc 上一次更新未成功，将会在本次关闭后重新尝试。");
                }
                else
                    SharedData.CheckN2NClientUpdate(Dispatcher);

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

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Minimized)
            {
                var rt = (this.RenderTransform as ScaleTransform);
                if (rt != null)
                {
                    var anim1 = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };

                    rt.CenterY = 0;
                    rt.BeginAnimation(ScaleTransform.ScaleYProperty, anim1);
                }
            }
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

using CT.WPF.MagicEffects;
using HandyControl.Tools.Extension;
using N2Nmc.UtilsClass;
using N2Nmc.Views.SubPages;
using N2Nmc.Views.SubPages.Dialogs;
using N2Nmc.Views.SubPages.Dialogs.MessageDialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

        public bool isInitialized { get; private set; } = false;
        bool _canClose = false;

        public Visibility LogButtonVisibility { get => ButtonLog.Visibility; set => ButtonLog.Visibility = value; }


        public static void PrintMemSet(string? Tag = null)
        {
            Process currentProcess = Process.GetCurrentProcess();

            long memoryUsage = currentProcess.WorkingSet64;
            double memoryUsageInMB = memoryUsage / (1024 * 1024);

            Console.WriteLine(string.Format("({1})Memory usage: {0} MBytes", memoryUsageInMB, Tag));
        }

        public MainView()
        {
            Stopwatch swbm = Stopwatch.StartNew();

            InitializeComponent();

            // 初始化页面实例
            pageRooms = new();
            pageRooming = new();
            pageQuickJoin = new();
            pageLog = new();
            pageSettings = new();
            pageRoom = new();
            pageInfo = new();
            pageIndex = new();

            var r = App.Current.Resources;
            var MainColorBrush = (SolidColorBrush)r["MainColorSolidBrush"];

            SharedData.EdgeConnectionInfo.IsConnectedToEdge = false;

            //BlurFramez.Radius = 0;
            HandyControl.Controls.Growl.GrowlPanel = PanelMsg;

            Opacity = 0;
            SharedData.ConfigFile.Get("DisableAnimation", "0");
            SharedData.ConfigFile.Get("UserNickname", "NewToGO");

            if (SharedData.ConfigFile.Get("FirstRun", "1") == "1")
            {
                var culture = CultureInfo.CurrentCulture;
                var locale = culture.Name;
                var localeHead = SharedData.CurrentApp.Locale.ReadLocal(locale);
                if (localeHead == null)
                {
                    DoMessageDialog($"暂无针对您当前的地区语言的翻译副本({culture.NativeName})，我们将会为您启用默认语言", "抱歉");
                    SharedData.ConfigFile.Set("locale", "default");
                    locale = "default";
                }
                SharedData.CurrentApp.Locale.UpdateLocale(locale);
                pageSettings.LocaleSeletion.SelectedItem = locale;
            }
            else
            {
                var locale = SharedData.ConfigFile.Get("locale", "default");
                var localeHead = SharedData.CurrentApp.Locale.ReadLocal(locale);
                if (localeHead == null)
                {
                    DoMessageDialog($"暂无针对您当前使用的语言的翻译副本({locale})，我们将会为您启用默认语言", "抱歉");
                    SharedData.ConfigFile.Set("locale", "default");
                    locale = "default";
                }
                SharedData.CurrentApp.Locale.UpdateLocale(locale);
                pageSettings.LocaleSeletion.SelectedItem = locale;
            }

            int _alpha = 255;
            var s = SharedData.ConfigFile.Get("WindowBackgroundAlpha", "245");  // 245 190 198 209
            int.TryParse(s, out _alpha);
            SetBackColor((byte?)_alpha);
            SetWindowMaxNormalButtonImage();
            pageSettings.BackgroundOSlider.Value = _alpha;

            {
#if DEBUG
                LogButtonVisibility = Visibility.Visible;
                TestButton.Visibility = Visibility.Visible;
#else
                LogButtonVisibility = Visibility.Collapsed;
                TestButton.Visibility = Visibility.Collapsed;
#endif
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


                    SharedData.NM_Connection.DisconnectedGrowlInfo.Message = "是否要尝试重新连接？";
                    HandyControl.Controls.Growl.Ask(SharedData.NM_Connection.DisconnectedGrowlInfo);
                    SharedData.NM_Connection.LockingVars.DEC();
                }

                ((DispatcherTimer)_).Stop();
                return;
            };

            lock (SharedData.NM_Connection)
            {
                SharedData.NM_Connection.DisconnectedGrowlInfo.ActionBeforeClose = (bool b) =>
                {
                    lock (SharedData.NM_Connection.LockingVars)
                    {
                        if (SharedData.NM_Connection.LockingVars.DisconnectedGrowlInfoAction > 0)
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
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            isInitialized = true;

            swbm.Stop();
            Console.WriteLine($"AppMainView initialization finished({swbm.Elapsed.ToString()})");
        }

        UInt32 _frameCounter = 0;
        Stopwatch _stopwatch = new Stopwatch();
        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            if (_frameCounter++ == 0)
            {
                // Starting timing.
                _stopwatch.Start();
            }

            // Determine frame rate in fps (frames per second).
            if (_frameCounter >= 60)
            {
                long frameRate = (long)(_frameCounter / this._stopwatch.Elapsed.TotalSeconds);
                DebugLabel_FPS.Content = String.Format("FPS: {0}", frameRate);
                _frameCounter = 0;
                _stopwatch.Restart();
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
                ActsRet = new();
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
                ActsRet = new();
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
                ActsRet = new();
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
            Process process = new();

            ProcessStartInfo startInfo = new();
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
            _canClose = false;

            SharedData.ConfigFile.Set("locale", SharedData.CurrentApp.Locale.CurrentLocale);

            if (SharedData.ConfigFile.Get("NeedUpdate", "0") == "1")
                HandyControl.Controls.Growl.SuccessGlobal("开始退出并更新 N2Nmc ...");
            else
                HandyControl.Controls.Growl.SuccessGlobal("开始退出 N2Nmc ...");
            SharedData.NM_Connection.Close();

            var at = new TaskCompletionSource<object>();
            fadeOutAnimationEx.Completed += (_, _) => at.SetResult(0);

            var tg = (this.RenderTransform as TransformGroup);
            if (tg == null)
                throw new NullReferenceException("[MainView] 'this.RenderTransform as TransformGroup' gets null!");

            var tg_st = tg.Children[0] as ScaleTransform;
            if (tg_st == null)
                throw new NullReferenceException("[MainView] 'TransformGroup.Children[0] as ScaleTransform' gets null!");
            var tg_tt = tg.Children[1] as TranslateTransform;
            if (tg_tt == null)
                throw new NullReferenceException("[MainView] 'TransformGroup.Children[1] as TranslateTransform' gets null!");

            tg_tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation { To = 800, Duration = TimeSpan.FromSeconds(0.55), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });

            BeginAnimation(OpacityProperty, fadeOutAnimationEx);

            await Task.Run(() =>
            {
                at.Task.Wait();

                Dispatcher.Invoke(() =>
                {
                    SharedData.ExitRoom();

                    SharedData.ConfigFile.SaveConfigDataToFile();
                    Console.WriteLine("(Exit) Config Wrote");

                    if (File.Exists(SharedData.UpdateClientExecFile))
                        try { File.Delete(SharedData.UpdateClientExecFile); } catch { }

                    File.Copy(SharedData.UpdateClientPath, SharedData.UpdateClientExecFile);
                    if (SharedData.ConfigFile.Get("NeedUpdate", "0") == "1")
                    {
                        try
                        {
                            new Process { StartInfo = new ProcessStartInfo { FileName = SharedData.UpdateClientExecFile, Arguments = Process.GetCurrentProcess().Id.ToString() } }.Start();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("（请尝试下次以管理员运行N2Nmc用于更新。）\n在启动升级程序时发生异常：" + ex.Message, "升级", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    HandyControl.Controls.Growl.SuccessGlobal("N2Nmc 已退出...");
                    _canClose = true;
                    Close();
                });
            });
        }

        private void SetWindowMaxNormalButtonImage()
        {
            LBImage_Wnd_Btn_MaxNormal.Source = new BitmapImage(new Uri(String.Format("/Data/image/icon/wnd_btn_{0}.png", (WindowState == WindowState.Maximized ? "normal" : "max")), UriKind.Relative));
        }
        public void SetBackColor(byte? a = null, byte? r = null, byte? g = null, byte? b = null)
        {
            var c = ((SolidColorBrush)App.Current.Resources["MainColorSolidBrush"]).Color;
            ContentRoot.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                a == null ? c.A : a.Value,
                r == null ? c.R : r.Value,
                g == null ? c.G : g.Value,
                b == null ? c.B : b.Value));
        }

        public async void UpdateColorPalette(string? newColorPaletteName = null)
        {
            if (!isInitialized)
                return;

            var resCurrentColorPalette = App.Current.TryFindResource("CurrentColorPalette") as ResourceDictionary;
            if (resCurrentColorPalette == null)
                throw new NullReferenceException("Current Color Palette Resource null");

            if (newColorPaletteName != null)
            {
                var resNewColorPalette = App.Current.TryFindResource("BuiltinColorPalette_" + newColorPaletteName) as ResourceDictionary;
                if (resNewColorPalette == null)
                    throw new NullReferenceException(string.Format("Target new Color Palette({0}) Resource null", newColorPaletteName));

                foreach (var key in resCurrentColorPalette.Keys)
                {
                    if (resNewColorPalette.Contains(key))
                    {
                        resCurrentColorPalette[key] = resNewColorPalette[key];
                    }
                }

                App.Current.Resources["CurrentColorPalette"] = resCurrentColorPalette;
            }

            foreach (var key in resCurrentColorPalette.Keys)
            {
                var brush = new SolidColorBrush(((SolidColorBrush)App.Current.Resources[key]).Color);
                brush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation { To = (Color)resCurrentColorPalette[key], Duration = TimeSpan.FromSeconds(0.45) });
                App.Current.Resources[key] = brush;
            }

            {
                var imgs = SharedData.FindVisualChildren<Image>(this);
                foreach (Image cimg in imgs)
                {
                    var n = cimg.Name;

                    if (n.Contains("LBImage_"))
                    {
                        await cimg.Dispatcher.InvokeAsync(() =>
                        {
                            (cimg.Effect as TexturedColorReplaceEffect)?.BeginAnimation(TexturedColorReplaceEffect.ReplacementColorProperty,
                                new ColorAnimation { To = (Color)resCurrentColorPalette["Palette_50"], Duration = TimeSpan.FromSeconds(0.45) });
                        });
                    }

                    continue;
                }
                TitleBorder.Dispatcher.InvokeAsync(() =>
                {
                    var b = new SolidColorBrush(((SolidColorBrush)TitleBorder.Background).Color);
                    TitleBorder.Background = b;
                    b.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation { To = (Color)resCurrentColorPalette["Palette_400"], Duration = TimeSpan.FromSeconds(0.45) });
                });
            }

            SetBackColor(pageSettings != null ? (byte)pageSettings.BackgroundOSlider.Value : null);
            SharedData.UIAnimation.Refresh();
            RefreshUIAnimations();

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
                                SharedData.ConfigFile.Set("UserNickname", newName);
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
                            di.InputBox.Text = SharedData.ConfigFile.Get("UserNickname");
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
            var tg = (this.RenderTransform as TransformGroup);
            if (tg == null)
                throw new NullReferenceException("[MainView] 'this.RenderTransform as TransformGroup' gets null!");

            var tg_st = tg.Children[0] as ScaleTransform;
            if (tg_st == null)
                throw new NullReferenceException("[MainView] 'TransformGroup.Children[0] as ScaleTransform' gets null!");
            var tg_tt = tg.Children[1] as TranslateTransform;
            if (tg_tt == null)
                throw new NullReferenceException("[MainView] 'TransformGroup.Children[1] as TranslateTransform' gets null!");

            var anim1 = new DoubleAnimation { To = -800, Duration = TimeSpan.FromSeconds(0.45), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            var at1 = new TaskCompletionSource<object>();
            anim1.Completed += (s, _) => at1.SetResult(0);

            var anim2 = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.28), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            var at2 = new TaskCompletionSource<object>();
            anim2.Completed += (s, _) => at2.SetResult(0);

            Task.Run(() =>
            {
                at1.Task.Wait();
                at2.Task.Wait();
                Dispatcher.Invoke(() => WindowState = WindowState.Minimized);
            });
            tg_tt.BeginAnimation(TranslateTransform.YProperty, anim1);
            this.BeginAnimation(OpacityProperty, anim2);
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
        private void ButtonLog_Click(object sender, RoutedEventArgs e)
        {
            NavigatePage(pageLog);
        }
        bool _bLogShow = false;
        private void TitleIconButton_Click(object sender, RoutedEventArgs e)
        {
            _bLogShow = !_bLogShow;

            if (_bLogShow)
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
            Stopwatch swbm = Stopwatch.StartNew();

            var tg = (this.RenderTransform as TransformGroup);
            if (tg == null)
                throw new NullReferenceException("[MainView] 'this.RenderTransform as TransformGroup' gets null!");

            var tg_st = tg.Children[0] as ScaleTransform;
            if (tg_st == null)
                throw new NullReferenceException("[MainView] 'TransformGroup.Children[0] as ScaleTransform' gets null!");
            var tg_tt = tg.Children[1] as TranslateTransform;
            if (tg_tt == null)
                throw new NullReferenceException("[MainView] 'TransformGroup.Children[1] as TranslateTransform' gets null!");

            tg_tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation { From = 450, To = 0, Duration = TimeSpan.FromSeconds(0.55), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });

            this.BeginAnimation(OpacityProperty, fadeInAnimationEx);

            NavigatePage(pageIndex);

            RefreshUIAnimations();

            //DoMessageDialog("Hi！这里是N2N GO！", "欢迎！", new List<Action<object>>() { (_) => DoMessageDialog("当您看见这个，代表您正使用Dev开发版本。\n我们推荐您使用正式版本，您可以选择手动下载并切换，或者使用检测更新功能。", "欢迎！") });

            await Task.Run(() =>
            {
                if (SharedData.ConfigFile == null)
                    throw new NullReferenceException(nameof(SharedData.ConfigFile));
                if (pageRooms == null)
                    throw new NullReferenceException(nameof(pageRooms));
                if (pageSettings == null)
                    throw new NullReferenceException(nameof(pageSettings));
                SharedData.ConnectAndPeek();
                //Dispatcher.Invoke(() => pageRooms.Refresh());

                if (SharedData.ConfigFile.Get("FirstRun", "1") == "1")
                {
                    //Dispatcher.BeginInvoke(() => HandyControl.Controls.Growl.Ask(new HandyControl.Data.GrowlInfo { CancelStr = "", Type = HandyControl.Data.InfoType.Info, ActionBeforeClose = (bool b) => { return b ? b : b; }, ShowCloseButton = false, Message = "N2Nmc需要配合Tap虚拟网卡来使用，如果您未安装，请前往设置-安装Tap驱动。" }));
                    Dispatcher.InvokeAsync(() => DoMessageDialog("如果您未安装Tap驱动，请前往设置页面安装，如果您是第一次使用N2N GO，我们强烈建议您安装一次。", "首次运行"));
                    SharedData.ConfigFile.Set("FirstRun", "0");
                }

                if (SharedData.ConfigFile.Get("NeedUpdate", "0") == "1")
                {
                    DoMessageDialog("N2Nmc 上一次更新未成功，将会在本次关闭后重新尝试。");
                }
                else
                    SharedData.CheckN2NClientUpdate(Dispatcher);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    Run("Data/BinRef/Windows/WinIPBroadcast/WinIPBroadcast.exe run", true); // 运行WinIPBroadcast（数据转发到虚拟网卡）

                pageSettings.Dispatcher.Invoke(() => pageSettings.UpdateColorPaletteSelectionItems());
            });
            
            swbm.Stop();
            Console.WriteLine($"App asynchronous loading finished({swbm.Elapsed.ToString()})");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BeginAnimation(OpacityProperty, fadeInAnimationSlowEx);
            AsyncLoading();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_canClose)
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
                var tg = (this.RenderTransform as TransformGroup);
                if (tg == null)
                    throw new NullReferenceException("[MainView] 'this.RenderTransform as TransformGroup' gets null!");

                var tg_st = tg.Children[0] as ScaleTransform;
                if (tg_st == null)
                    throw new NullReferenceException("[MainView] 'TransformGroup.Children[0] as ScaleTransform' gets null!");
                var tg_tt = tg.Children[1] as TranslateTransform;
                if (tg_tt == null)
                    throw new NullReferenceException("[MainView] 'TransformGroup.Children[1] as TranslateTransform' gets null!");

                var anim1 = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.50), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
                var anim2 = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.40), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };

                tg_tt.BeginAnimation(TranslateTransform.YProperty, anim1);
                this.BeginAnimation(OpacityProperty, anim2);
            }
        }

        //Point ___tmp_pos = Point();
        //private void Window_MouseMove(object sender, MouseEventArgs e)
        //{
        //    ___tmp_pos = e.GetPosition(this);
        //    ___tmp_pos.X = ___tmp_pos.X / ImageBackgroundImage.RenderSize.Width /2;
        //    ___tmp_pos.Y = ___tmp_pos.Y / ImageBackgroundImage.RenderSize.Height/2;

        //    ImageBackgroundImage.RenderTransformOrigin = ___tmp_pos;
        //}
        private void LocaleSeletion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0)
                return;

            var combo = sender as HandyControl.Controls.ComboBox;
            if (combo == null)
                throw new ArgumentNullException("LocaleSeletion_SelectionChanged sender arg null");

            var selectedItem = e.AddedItems[0] as string;
            if (selectedItem != null)
                SharedData.CurrentApp.Locale.UpdateLocale(selectedItem);
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                var onlines = SharedData.NM_Connection.PullTotalOnlines();
                if (onlines)
                    Dispatcher.InvokeAsync(() => DoMessageDialog($"Onlines: {onlines.Value}", "Test"));
            });
        }
    }

    public class LocaleSelectionConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var objAsString = value as string;
            if (objAsString == null)
                return null;

            var res = SharedData.CurrentApp.Locale.ReadLocal(objAsString);
            if (res == null)
                return null;
            return res.Language;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}

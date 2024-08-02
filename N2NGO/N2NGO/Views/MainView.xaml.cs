using CT.WPF.MagicEffects;
using HandyControl.Tools.Extension;
using N2NGO.UtilsClass;
using N2NGO.Views.SubPages;
using N2NGO.Views.SubPages.Dialogs;
using N2NGO.Views.SubPages.Dialogs.MessageDialogs;
using N2NGO_Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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

namespace N2NGO.Views
{

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>

    public partial class MainView : System.Windows.Window
    {
        public readonly TerminalWindow DebugTerminalWindow;

        public RoomsPage PageRooms { get; private set; }
        public RoomingPage PageRooming { get; private set; }
        public QuickJoinPage PageQuickJoin { get; private set; }
        public LogPage PageLog { get; private set; }
        public SettingsPage PageSettings { get; private set; }
        public IndexPage PageIndex { get; private set; }
        public RoomPage PageRoom { get; private set; }
        public InfoPage PageInfo { get; private set; }


        private readonly DoubleAnimation _fadeInAnimation = new() { From = 0.15, To = 1, Duration = TimeSpan.FromSeconds(0.4) };
        private readonly DoubleAnimation _fadeInAnimationEx = new() { From = 0, To = 1, Duration = TimeSpan.FromSeconds(1.5), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        private readonly DoubleAnimation _fadeInAnimationSlowEx = new() { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.5), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };

        private readonly DoubleAnimation _fadeOutAnimationEx = new() { From = 1, To = 0, Duration = TimeSpan.FromSeconds(0.3), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };

#pragma warning disable IDE1006 // 命名样式
        public bool isInitialized { get; private set; } = false;
#pragma warning restore IDE1006 // 命名样式
        bool _canClose = false;

        public Visibility LogButtonVisibility { get => ButtonLog.Visibility; set => ButtonLog.Visibility = value; }

        public MainView()
        {
            Stopwatch swbm = Stopwatch.StartNew();

            SharedData.CurrentApp.PrintMemSet("MainView Initialization Begin");

            App.Current.MainWindow = this;
            InitializeComponent();

            PageRooms = new();
            PageRooming = new();
            PageQuickJoin = new();
            PageLog = new();
            PageSettings = new();
            PageRoom = new();
            PageInfo = new();
            PageIndex = new();
            SharedData.CurrentApp.PrintMemSet("Pages Initialized by MainView");

            var r = App.Current.Resources;
            var MainColorBrush = (SolidColorBrush)r["MainColorSolidBrush"];

            //BlurFramez.Radius = 0;
            HandyControl.Controls.Growl.GrowlPanel = PanelMsg;

            Opacity = 0;
            SharedData.CurrentApp.Config.Get("DisableAnimation", "0");
            SharedData.CurrentApp.Config.Get("UserNickname", "NewToGO");

            if (SharedData.CurrentApp.Config.Get("FirstRun", "1") == "1")
            {
                var culture = CultureInfo.CurrentCulture;
                var locale = culture.Name;
                var localeHead = SharedData.CurrentApp.Locale.ReadLocal(locale);
                if (localeHead == null)
                {
                    DoMessageDialog($"暂无针对您当前的地区语言的翻译副本({culture.NativeName})，我们将会为您启用默认语言", "抱歉");
                    SharedData.CurrentApp.Config.Set("locale", "default");
                    locale = "default";
                }
                SharedData.CurrentApp.Locale.UpdateLocale(locale);
                PageSettings.LocaleSeletion.SelectedItem = locale;
            }
            else
            {
                var locale = SharedData.CurrentApp.Config.Get("locale", "default");
                var localeHead = SharedData.CurrentApp.Locale.ReadLocal(locale);
                if (localeHead == null)
                {
                    DoMessageDialog($"暂无针对您当前使用的语言的翻译副本({locale})，我们将会为您启用默认语言", "抱歉");
                    SharedData.CurrentApp.Config.Set("locale", "default");
                    locale = "default";
                }
                SharedData.CurrentApp.Locale.UpdateLocale(locale);
                PageSettings.LocaleSeletion.SelectedItem = locale;
            }

            var s = SharedData.CurrentApp.Config.Get("WindowBackgroundAlpha", "245");  // 245 190 198 209
            if (!int.TryParse(s, out int alpha))
                alpha = 245;

            SetBackColor((byte?)alpha);
            SetWindowMaxNormalButtonImage();
            PageSettings.BackgroundOSlider.Value = alpha;

            Console.WriteLine("Console output will be redirected to Terminal Window!");
            DebugTerminalWindow = new()
            {
                Title = $"N2N GO({SharedData.VersionString}) Debug Console",
            };
            Console.SetOut(new TerminalWindowTextWriter(DebugTerminalWindow));

            {
#if DEBUG
                LogButtonVisibility = Visibility.Visible;
                TestButton.Visibility = Visibility.Visible;
                DebugOverlay.Visibility = Visibility.Visible;

                DebugTerminalWindow.Show();
#else
                LogButtonVisibility = Visibility.Collapsed;
                TestButton.Visibility = Visibility.Collapsed;
                DebugOverlay.Visibility = Visibility.Collapsed;
#endif
            }

            CompositionTarget.Rendering += CompositionTarget_Rendering;

            isInitialized = true;

            swbm.Stop();
            Console.WriteLine($"MainView initialization finished({swbm.Elapsed})");
            SharedData.CurrentApp.PrintMemSet("MainView Initialization End");
        }

        ulong __frameCounter = 0;
        readonly Stopwatch __stopwatch = new();
        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            if (__frameCounter++ == 0)
            {
                // Starting timing.
                __stopwatch.Start();
            }

            // Determine frame rate in fps (frames per second).
            if (__frameCounter >= 60)
            {
                long frameRate = (long)(__frameCounter / __stopwatch.Elapsed.TotalSeconds);
                DebugLabel_FPS.Content = $"FPS: {frameRate}";
                __frameCounter = 0;
                __stopwatch.Restart();
            }
        }

        public void RefreshUIAnimations()
        {
            {
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>(this));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)PageRooms.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)PageRooming.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)PageQuickJoin.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)PageLog.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)PageSettings.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)PageIndex.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)PageRoom.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)PageInfo.Content));


                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>(this));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)PageRooms.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)PageRooming.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)PageQuickJoin.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)PageLog.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)PageSettings.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)PageIndex.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)PageRoom.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>((Grid)PageInfo.Content));


                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>(this));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)PageRooms.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)PageRooming.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)PageQuickJoin.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)PageLog.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)PageSettings.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)PageIndex.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)PageRoom.Content));
                SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<CheckBox>((Grid)PageInfo.Content));
            }

            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>(this));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)PageRooms.Content));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)PageRooming.Content));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)PageQuickJoin.Content));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)PageLog.Content));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)PageSettings.Content));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)PageIndex.Content));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)PageRoom.Content));
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)PageInfo.Content));
        }

        public void DoMessageInputDialog(string MessageText = "", string? MessageTitle = null, List<Action<object>>? ActsRet = null, Action<object>? ActPreRun = null)
        {
            MainFrameBlurEffect.BeginAnimation(System.Windows.Media.Effects.BlurEffect.RadiusProperty, new DoubleAnimation(10, TimeSpan.FromSeconds(0.2)) { EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseIn } });
            Frame? m = null;

            ActsRet ??= new();
            ActsRet.Add((_) =>
            {
                if (m == null)
                    throw new NullReferenceException("MessageInputDialog Callbacks");

                Dispatcher.Invoke(() =>
                {
                    MessageDialogs.Children.Remove(m);
                    MainFrameBlurEffect.BeginAnimation(System.Windows.Media.Effects.BlurEffect.RadiusProperty, new DoubleAnimation(0, TimeSpan.FromSeconds(0.16)) { EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseIn } });
                });
            });
            var dm = new DialogMessage(MessageText, MessageTitle, DialogMessage.DialogType.InputOK, ActsRet);
            m = new Frame { Content = dm, BorderThickness = new Thickness(0), BorderBrush = null };

            ActPreRun?.Invoke(dm);

            MessageDialogs.Children.Add(m);
        }
        public void DispatcherDoMessageInputDialog(string MessageText = "", string? MessageTitle = null, List<Action<object>>? ActsRet = null, Action<object>? ActPreRun = null) => this.Dispatcher.Invoke(() => DoMessageInputDialog(MessageText, MessageTitle, ActsRet, ActPreRun));
        public void DoMessageYesNoDialog(string MessageText = "", string? MessageTitle = null, List<Action<object>>? ActsRet = null, Action<object>? ActPreRun = null)
        {
            MainFrameBlurEffect.BeginAnimation(System.Windows.Media.Effects.BlurEffect.RadiusProperty, new DoubleAnimation(10, TimeSpan.FromSeconds(0.2)) { EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseIn } });
            Frame? m = null;

            ActsRet ??= new();
            ActsRet.Add((_) =>
            {
                if (m == null)
                    throw new NullReferenceException("MessageYesNoDialog Callbacks");

                Dispatcher.Invoke(() =>
                {
                    MessageDialogs.Children.Remove(m);
                    MainFrameBlurEffect.BeginAnimation(System.Windows.Media.Effects.BlurEffect.RadiusProperty, new DoubleAnimation(0, TimeSpan.FromSeconds(0.16)) { EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseIn } });
                });
            });
            var dm = new DialogMessage(MessageText, MessageTitle, DialogMessage.DialogType.YesNo, ActsRet);
            m = new Frame { Content = dm, BorderThickness = new Thickness(0), BorderBrush = null };

            ActPreRun?.Invoke(dm);

            MessageDialogs.Children.Add(m);
        }
        public void DispatcherDoMessageYesNoDialog(string MessageText = "", string? MessageTitle = null, List<Action<object>>? ActsRet = null, Action<object>? ActPreRun = null) => this.Dispatcher.Invoke(() => DoMessageYesNoDialog(MessageText, MessageTitle, ActsRet, ActPreRun));
        public void DoMessageDialog(string MessageText = "", string? MessageTitle = null, List<Action<object>>? ActsRet = null, Action<object>? ActPreRun = null)
        {
            MainFrameBlurEffect.BeginAnimation(System.Windows.Media.Effects.BlurEffect.RadiusProperty, new DoubleAnimation(10, TimeSpan.FromSeconds(0.2)) { EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseIn } });
            Frame? m = null;

            ActsRet ??= new();
            ActsRet.Add((_) =>
            {
                if (m == null)
                    throw new NullReferenceException("MessageDialog Callbacks");

                Dispatcher.Invoke(() =>
                {
                    MessageDialogs.Children.Remove(m);
                    MainFrameBlurEffect.BeginAnimation(System.Windows.Media.Effects.BlurEffect.RadiusProperty, new DoubleAnimation(0, TimeSpan.FromSeconds(0.16)) { EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseIn } });
                });
            });
            var dm = new DialogMessage(MessageText, MessageTitle, DialogMessage.DialogType.OK, ActsRet);
            m = new Frame { Content = dm, BorderThickness = new Thickness(0), BorderBrush = null };

            ActPreRun?.Invoke(dm);

            MessageDialogs.Children.Add(m);
        }
        public void DispatcherDoMessageDialog(string MessageText = "", string? MessageTitle = null, List<Action<object>>? ActsRet = null, Action<object>? ActPreRun = null) => this.Dispatcher.Invoke(() => DoMessageDialog(MessageText, MessageTitle, ActsRet, ActPreRun));

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

            Framez.Navigate(page);
            page.BeginAnimation(OpacityProperty, _fadeInAnimation);
        }

        static async Task<string> Run(string command, bool noWindow = false)
        {
            Process process = new();

            ProcessStartInfo startInfo = new()
            {
                FileName = "cmd.exe",
                Arguments = "/c " + command,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = noWindow
            };

            process.StartInfo = startInfo;

            process.Start();
            await process.WaitForExitAsync();
            return process.StandardOutput.ReadToEnd();
        }

        private async void CloseExit()
        {
            _canClose = false;

            SharedData.CurrentApp.Config.Set("locale", SharedData.CurrentApp.Locale.CurrentLocale);

            await Task.Run(() => SharedData.CurrentApp.LeaveRoom());
            SharedData.CurrentApp.N2NGOServerConnection.Close();

            var at = new TaskCompletionSource<object>();
            _fadeOutAnimationEx.Completed += (_, _) => at.SetResult(0);

            var tg = (this.RenderTransform as TransformGroup) ?? throw new NullReferenceException("[MainView] 'this.RenderTransform as TransformGroup' gets null!");
            var tg_st = tg.Children[0] as ScaleTransform ?? throw new NullReferenceException("[MainView] 'TransformGroup.Children[0] as ScaleTransform' gets null!");
            var tg_tt = tg.Children[1] as TranslateTransform ?? throw new NullReferenceException("[MainView] 'TransformGroup.Children[1] as TranslateTransform' gets null!");
            tg_tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation { To = 800, Duration = TimeSpan.FromSeconds(0.55), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });

            BeginAnimation(OpacityProperty, _fadeOutAnimationEx);

            await Task.Run(() =>
            {
                at.Task.Wait();

                Dispatcher.Invoke(() =>
                {
                    SharedData.CurrentApp.Config.SaveConfigDataToFile();
                    Console.WriteLine("(Exit) Config Wrote");

                    if (SharedData.CurrentApp.Config.Get("NeedUpdate", "0") == "1")
                    {
                        try
                        {
                            Process.Start(UserDef.N2NGOUpdateInstallerFilePath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("在启动安装程序时发生异常：" + ex.Message, "升级", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    _canClose = true;
                    DebugTerminalWindow.Close();
                    Close();
                });
            });
        }

        private void SetWindowMaxNormalButtonImage()
        {
            LBImage_Wnd_Btn_MaxNormal.Source = new BitmapImage(new Uri(String.Format("/Data/Images/icon/wnd_btn_{0}.png", (WindowState == WindowState.Maximized ? "normal" : "max")), UriKind.Relative));
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

            var resCurrentColorPalette = App.Current.TryFindResource("CurrentColorPalette") as ResourceDictionary ?? throw new NullReferenceException("Current Color Palette Resource null");
            if (newColorPaletteName != null)
            {
                var resNewColorPalette = App.Current.TryFindResource("BuiltinColorPalette_" + newColorPaletteName) as ResourceDictionary ?? throw new NullReferenceException(string.Format("Target new Color Palette({0}) Resource null", newColorPaletteName));
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
            await TitleBorder.Dispatcher.InvokeAsync(() =>
            {
                var b = new SolidColorBrush(((SolidColorBrush)TitleBorder.Background).Color);
                TitleBorder.Background = b;
                b.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation { To = (Color)resCurrentColorPalette["Palette_400"], Duration = TimeSpan.FromSeconds(0.45) });
            });


            SetBackColor((byte)PageSettings.BackgroundOSlider.Value);
            SharedData.UIAnimation.Refresh();
            RefreshUIAnimations();

            return;
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            CloseExit();
        }
        private void ButtonWindowMin_Click(object sender, RoutedEventArgs e)
        {
            var tg = (this.RenderTransform as TransformGroup) ?? throw new NullReferenceException("[MainView] 'this.RenderTransform as TransformGroup' gets null!");
            var tg_st = tg.Children[0] as ScaleTransform ?? throw new NullReferenceException("[MainView] 'TransformGroup.Children[0] as ScaleTransform' gets null!");
            var tg_tt = tg.Children[1] as TranslateTransform ?? throw new NullReferenceException("[MainView] 'TransformGroup.Children[1] as TranslateTransform' gets null!");

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
            NavigatePage(PageLog);
        }
        bool _bLogShow = false;
        private void TitleIconButton_Click(object sender, RoutedEventArgs e)
        {
            _bLogShow = !_bLogShow;

            if (_bLogShow)
                NavigatePage(PageLog);
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

            SharedData.CurrentApp.PrintMemSet("MainView AsyncLoading Begin");
            PageRooms.Refresh();

            var tg = (this.RenderTransform as TransformGroup) ?? throw new NullReferenceException("[MainView] 'this.RenderTransform as TransformGroup' gets null!");
            var tg_st = tg.Children[0] as ScaleTransform ?? throw new NullReferenceException("[MainView] 'TransformGroup.Children[0] as ScaleTransform' gets null!");
            var tg_tt = tg.Children[1] as TranslateTransform ?? throw new NullReferenceException("[MainView] 'TransformGroup.Children[1] as TranslateTransform' gets null!");

            tg_tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation { From = 450, To = 0, Duration = TimeSpan.FromSeconds(0.55), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });

            this.BeginAnimation(OpacityProperty, _fadeInAnimationEx);

            NavigatePage(PageIndex);

            RefreshUIAnimations();

            await Task.Run(() =>
            {
                if (SharedData.CurrentApp.Config == null)
                    throw new NullReferenceException(nameof(SharedData.CurrentApp.Config));

                SharedData.CurrentApp.PrintMemSet("MainView AsyncLoading SubBegin");
                PageSettings.Dispatcher.Invoke(() => PageSettings.UpdateColorPaletteSelectionItems());

                if (SharedData.CurrentApp.Config.Get("FirstRun", "1") == "1")
                {
                    //Dispatcher.BeginInvoke(() => HandyControl.Controls.Growl.Ask(new HandyControl.Data.GrowlInfo { CancelStr = "", Type = HandyControl.Data.InfoType.Info, ActionBeforeClose = (bool b) => { return b ? b : b; }, ShowCloseButton = false, Message = "N2N GO 需要配合Tap虚拟网卡来使用，如果您未安装，请前往设置-安装Tap驱动。" }));
                    Dispatcher.InvokeAsync(() =>
                    DoMessageYesNoDialog("如果您未安装Tap驱动，请前往设置页面安装，如果您是第一次使用N2N GO，我们强烈建议您安装一次。\n需要立即安装吗？", "首次运行",
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
                    ));
                    SharedData.CurrentApp.Config.Set("FirstRun", "0");
                }

                var connected = SharedData.CurrentApp.ConnectAndPeek();

                if (SharedData.CurrentApp.Config.Get("NeedUpdate", "0") == "1")
                {
                    Dispatcher.InvokeAsync(() => DoMessageDialog("N2N GO 上一次更新未成功，将会在本次关闭后重新尝试。", "更新"));
                }
                else
                {
                    if (connected) SharedData.CurrentApp.CheckN2NGOClientUpdate();
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    Run("Data/BinRef/Windows/WinIPBroadcast/WinIPBroadcast.exe run", true); // 运行WinIPBroadcast（数据转发到虚拟网卡）

                SharedData.CurrentApp.PrintMemSet("MainView AsyncLoading Sub Finished");
            });

            swbm.Stop();
            Console.WriteLine($"App asynchronous loading finished({swbm.Elapsed})");
            SharedData.CurrentApp.PrintMemSet("MainView AsyncLoading End");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BeginAnimation(OpacityProperty, _fadeInAnimationSlowEx);
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
                var transformGroup = (this.RenderTransform as TransformGroup) ?? throw new NullReferenceException("[MainView] 'this.RenderTransform as TransformGroup' gets null!");
                foreach (var transform in transformGroup.Children)
                {
                    if (transform is TranslateTransform translateTransform)
                    {
                        translateTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.50), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });
                    }
                }

                this.BeginAnimation(OpacityProperty, new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.40), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
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

            if (e.AddedItems[0] is not string selectedItem)
                throw new Exception("Unsupported data type for locale selection item");

            SharedData.CurrentApp.Locale.UpdateLocale(selectedItem);
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                var onlines = SharedData.CurrentApp.N2NGOServerConnection.PullTotalOnlines();
                if (onlines.IsSuccessfulStatusCode)
                    Dispatcher.InvokeAsync(() => DoMessageDialog($"Onlines: {onlines.Value}", "Test"));
            });
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            var b = new SolidColorBrush(((SolidColorBrush)TitleBorder.Background).Color);
            TitleBorder.Background = b;
            b.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation { To = (Color)((ResourceDictionary)SharedData.CurrentApp.Get.Resources["CurrentColorPalette"])["Palette_400"], Duration = TimeSpan.FromSeconds(0.25) });

        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            var b = new SolidColorBrush(((SolidColorBrush)TitleBorder.Background).Color);
            TitleBorder.Background = b;
            b.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation { To = (Color)((ResourceDictionary)SharedData.CurrentApp.Get.Resources["CurrentColorPalette"])["Palette_200"], Duration = TimeSpan.FromSeconds(0.25) });
        }
    }

    public class LocaleSelectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string objAsString)
                return value;

            var res = SharedData.CurrentApp.Locale.ReadLocal(objAsString);
            if (res == null)
                return value;

            return res.Language;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}

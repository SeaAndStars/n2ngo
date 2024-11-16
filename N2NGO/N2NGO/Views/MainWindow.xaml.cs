using CT.WPF.MagicEffects;
using N2NGO.Utils;
using N2NGO.Views.IndexPages;
using N2NGO.Views.SubPages;
using N2NGO.Views.SubPages.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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

    public partial class MainWindow : System.Windows.Window
    {
        public readonly TerminalWindow DebugTerminalWindow;

        public RoomsPage PageRooms { get; private set; }
        public RoomingPage PageRooming { get; private set; }
        public QuickJoinPage PageQuickJoin { get; private set; }
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

        public MainWindow()
        {
            Stopwatch swbm = Stopwatch.StartNew();

            Globals.CurrentApp.PrintMemSet("MainWindow Initialization Begin");

            App.Current.MainWindow = this;
            InitializeComponent();

            PageRooms = new();
            PageRooming = new();
            PageQuickJoin = new();
            PageSettings = new();
            PageRoom = new();
            PageInfo = new();
            PageIndex = new();
            Globals.CurrentApp.PrintMemSet("Pages Initialized by MainWindow");

            var r = App.Current.Resources;
            var MainColorBrush = (SolidColorBrush)r["MainColorSolidBrush"];

            //BlurFramez.Radius = 0;

            Opacity = 0;
            Globals.CurrentApp.Config.Get("UserNickname", "NewToGO");

            if (Globals.CurrentApp.Config.Get("FirstRun_1", "1") == "1")
            {
                Globals.CurrentApp.Config.Set("IpGlobalServer", "43.143.37.61");
                Globals.CurrentApp.Config.Set("PortGlobalServer", "7476");
                Globals.CurrentApp.Config.Set("PortSupernodeServer", "7478");
                Globals.CurrentApp.Config.Set("FirstRun_1", "0");
            }
            if (Globals.CurrentApp.Config.Get("FirstRun", "1") == "1")
            {
                var culture = CultureInfo.CurrentCulture;
                var locale = culture.Name;
                var localeHead = Globals.CurrentApp.Locale.ReadLocal(locale);
                if (localeHead == null)
                {
                    DoMessageDialog($"暂无针对您当前的地区语言的翻译副本({culture.NativeName})，我们将会为您启用默认语言", "抱歉");
                    Globals.CurrentApp.Config.Set("locale", "default");
                    locale = "default";
                }
                Globals.CurrentApp.Locale.UpdateLocale(locale);
                PageSettings.LocaleSeletion.SelectedItem = locale;
            }
            else
            {
                var locale = Globals.CurrentApp.Config.Get("locale", "default");
                var localeHead = Globals.CurrentApp.Locale.ReadLocal(locale);
                if (localeHead == null)
                {
                    DoMessageDialog($"暂无针对您当前使用的语言的翻译副本({locale})，我们将会为您启用默认语言", "抱歉");
                    Globals.CurrentApp.Config.Set("locale", "default");
                    locale = "default";
                }
                Globals.CurrentApp.Locale.UpdateLocale(locale);
                PageSettings.LocaleSeletion.SelectedItem = locale;
            }

            var s = Globals.CurrentApp.Config.Get("WindowBackgroundAlpha", "245");  // 245 190 198 209
            if (!int.TryParse(s, out int alpha))
                alpha = 245;

            SetBackColor((byte?)alpha);
            SetWindowMaxNormalButtonImage();
            PageSettings.BackgroundOpacitySlider.Value = alpha;

            Globals.CurrentApp.Log.WriteLine("Console output will be redirected to Terminal Window!", Globals.CurrentApp.Log.Module.MainWindow);
            DebugTerminalWindow = new()
            {
                Title = $"N2N GO({Globals.VersionString}) Debug Console",
            };
            Console.SetOut(new TerminalWindowTextWriter(DebugTerminalWindow));

            {
#if DEBUG
                TestButton.Visibility = Visibility.Visible;
                DebugOverlay.Visibility = Visibility.Visible;

                DebugTerminalWindow.Show();
#else
                TestButton.Visibility = Visibility.Collapsed;
                DebugOverlay.Visibility = Visibility.Collapsed;
#endif
            }

            CompositionTarget.Rendering += CompositionTarget_Rendering;

            isInitialized = true;

            GC.Collect();
            swbm.Stop();
            Globals.CurrentApp.Log.WriteLine($"MainWindow initialization finished({swbm.Elapsed})", Globals.CurrentApp.Log.Module.MainWindow);
            Globals.CurrentApp.PrintMemSet("MainWindow Initialization End");
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
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<Label>(this));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<Label>((Grid)PageRooms.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<Label>((Grid)PageRooming.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<Label>((Grid)PageQuickJoin.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<Label>((Grid)PageSettings.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<Label>((Grid)PageIndex.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<Label>((Grid)PageRoom.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<Label>((Grid)PageInfo.Content));


                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<TextBox>(this));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<TextBox>((Grid)PageRooms.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<TextBox>((Grid)PageRooming.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<TextBox>((Grid)PageQuickJoin.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<TextBox>((Grid)PageSettings.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<TextBox>((Grid)PageIndex.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<TextBox>((Grid)PageRoom.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<TextBox>((Grid)PageInfo.Content));


                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<CheckBox>(this));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<CheckBox>((Grid)PageRooms.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<CheckBox>((Grid)PageRooming.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<CheckBox>((Grid)PageQuickJoin.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<CheckBox>((Grid)PageSettings.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<CheckBox>((Grid)PageIndex.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<CheckBox>((Grid)PageRoom.Content));
                CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<CheckBox>((Grid)PageInfo.Content));
            }

            CustomUI.InitButtons(CustomUIHelpers.FindVisualChildren<Button>(this));
            CustomUI.InitButtons(CustomUIHelpers.FindVisualChildren<Button>((Grid)PageRooms.Content));
            CustomUI.InitButtons(CustomUIHelpers.FindVisualChildren<Button>((Grid)PageRooming.Content));
            CustomUI.InitButtons(CustomUIHelpers.FindVisualChildren<Button>((Grid)PageQuickJoin.Content));
            CustomUI.InitButtons(CustomUIHelpers.FindVisualChildren<Button>((Grid)PageSettings.Content));
            CustomUI.InitButtons(CustomUIHelpers.FindVisualChildren<Button>((Grid)PageIndex.Content));
            CustomUI.InitButtons(CustomUIHelpers.FindVisualChildren<Button>((Grid)PageRoom.Content));
            CustomUI.InitButtons(CustomUIHelpers.FindVisualChildren<Button>((Grid)PageInfo.Content));
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


            var imgs = CustomUIHelpers.FindVisualChildren<Image>(this);
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


            SetBackColor((byte)PageSettings.BackgroundOpacitySlider.Value);
            CustomUI.Update();
            RefreshUIAnimations();

            return;
        }

        public void DoMessagePickBrushDialog(string MessageText = "", string? MessageTitle = null, List<Action<object>>? ActsRet = null, Action<object>? ActPreRun = null)
        {
            MainFrameBlurEffect.BeginAnimation(System.Windows.Media.Effects.BlurEffect.RadiusProperty, new DoubleAnimation(10, TimeSpan.FromSeconds(0.2)) { EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseIn } });
            Frame? m = null;

            ActsRet ??= new();
            ActsRet.Add((_) =>
            {
                if (m == null)
                    throw new NullReferenceException("MessagePickBrushDialog Callbacks");

                Dispatcher.Invoke(() =>
                {
                    MessageDialogs.Children.Remove(m);
                    MainFrameBlurEffect.BeginAnimation(System.Windows.Media.Effects.BlurEffect.RadiusProperty, new DoubleAnimation(0, TimeSpan.FromSeconds(0.16)) { EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseIn } });
                });
            });
            var dm = new DialogMessage(MessageText, MessageTitle, DialogMessage.DialogType.PickBrushDone, ActsRet);
            m = new Frame { Content = dm, BorderThickness = new Thickness(0), BorderBrush = null };

            ActPreRun?.Invoke(dm);

            MessageDialogs.Children.Add(m);
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
            var dm = new DialogMessage(MessageText, MessageTitle, DialogMessage.DialogType.InputConfirm, ActsRet);
            m = new Frame { Content = dm, BorderThickness = new Thickness(0), BorderBrush = null };

            ActPreRun?.Invoke(dm);

            MessageDialogs.Children.Add(m);
        }
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

        private async void CloseExit()
        {
            _canClose = false;

            Globals.CurrentApp.Config.Set("locale", Globals.CurrentApp.Locale.CurrentLocale);

            await Globals.CurrentApp.LeaveRoom();
            Globals.CurrentApp.N2NGOServerConnection.Close();

            var at = new TaskCompletionSource<object>();
            _fadeOutAnimationEx.Completed += (_, _) => at.SetResult(0);

            var tg = (this.RenderTransform as TransformGroup) ?? throw new NullReferenceException("[MainWindow] 'this.RenderTransform as TransformGroup' gets null!");
            var tg_st = tg.Children[0] as ScaleTransform ?? throw new NullReferenceException("[MainWindow] 'TransformGroup.Children[0] as ScaleTransform' gets null!");
            var tg_tt = tg.Children[1] as TranslateTransform ?? throw new NullReferenceException("[MainWindow] 'TransformGroup.Children[1] as TranslateTransform' gets null!");
            tg_tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation { To = 800, Duration = TimeSpan.FromSeconds(0.55), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });

            BeginAnimation(OpacityProperty, _fadeOutAnimationEx);

            await Task.Run(() =>
            {
                at.Task.Wait();

                Dispatcher.Invoke(() =>
                {
                    Globals.CurrentApp.Config.SaveConfigDataToFile();
                    Globals.CurrentApp.Log.WriteLine("(Exit) Config Wrote", Globals.CurrentApp.Log.Module.MainWindow);

                    if (Globals.CurrentApp.Config.Get("NeedUpdate", "0") == "1")
                    {
                        try
                        {
                            Process.Start(N2NGOCore.Vars.N2NGOUpdateInstallerFilePath);
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
        private bool FirstRun()
        {
            return true;
        }
        private async void AsyncLoading()
        {
            Stopwatch swbm = Stopwatch.StartNew();

            Globals.CurrentApp.PrintMemSet("MainWindow AsyncLoading Begin");
            PageRooms.Refresh();

            var tg = (this.RenderTransform as TransformGroup) ?? throw new NullReferenceException("[MainWindow] 'this.RenderTransform as TransformGroup' gets null!");
            var tg_st = tg.Children[0] as ScaleTransform ?? throw new NullReferenceException("[MainWindow] 'TransformGroup.Children[0] as ScaleTransform' gets null!");
            var tg_tt = tg.Children[1] as TranslateTransform ?? throw new NullReferenceException("[MainWindow] 'TransformGroup.Children[1] as TranslateTransform' gets null!");

            tg_tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation { From = 450, To = 0, Duration = TimeSpan.FromSeconds(0.55), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });

            this.BeginAnimation(OpacityProperty, _fadeInAnimationEx);

            NavigatePage(PageIndex);

            RefreshUIAnimations();

            await Task.Run(() =>
            {
                if (Globals.CurrentApp.Config == null)
                    throw new NullReferenceException(nameof(Globals.CurrentApp.Config));

                Globals.CurrentApp.PrintMemSet("MainWindow AsyncLoading SubBegin");
                PageSettings.Dispatcher.InvokeAsync(() => PageSettings.UpdateColorPaletteSelectionItems());
                PageRoom.Dispatcher.InvokeAsync(() =>
                {
                    PageRoom.AdminPanelMembers.InitializeWithCustomUI();
                    PageRoom.AdminPanelRuledMembers.InitializeWithCustomUI();
                });

                Globals.CurrentApp.ResetConnection();

                Task.Run(async () =>
                {
                    var url = $"https://mail.bestlgf.pro/N2NGO/UpdateInfo?raw=true&depth=50";
                    using var httpClient = new HttpClient();
                    try
                    {
                        HttpResponseMessage response = await httpClient.GetAsync(url);

                        response.EnsureSuccessStatusCode();

                        string responseBody = await response.Content.ReadAsStringAsync();

                        using var document = JsonDocument.Parse(responseBody);
                        JsonElement root = document.RootElement;

                        if (root.TryGetProperty("success", out JsonElement success) && success.GetBoolean())
                        {
                            StringBuilder stringBuilder = new();
                            stringBuilder.AppendLine("https://mail.bestlgf.pro/N2NGO/UpdateInfo\n");

                            foreach (JsonElement version in root.GetProperty("versions").EnumerateArray())
                            {
                                var updateInfo = new
                                {
                                    version = version.GetProperty("version").GetString(),
                                    detail = version.GetProperty("detail").GetString(),
                                    id = version.GetProperty("id").GetString(),
                                    updateLog = version.GetProperty("updateLog").GetString()
                                };

                                stringBuilder.AppendLine($"[{updateInfo.id}]{updateInfo.version} ({updateInfo.detail}):");
                                if (updateInfo.updateLog is not null)
                                    stringBuilder.AppendLine($"  {updateInfo.updateLog.Replace("\n", "\n  ")}:");
                                stringBuilder.AppendLine("");
                            }

                            Dispatcher.Invoke(() => { DoMessageDialog(stringBuilder.ToString(), "@LOCALE_DialogUpdateInfo_Title"); });
                        }
                        else
                        {
                            Globals.CurrentApp.Log.WriteLine($"[AsyncLoading] Failed to retrieve update info from '{url}'.", Globals.CurrentApp.Log.Module.MainWindow);
                        }
                    }
                    catch (Exception e)
                    {
                        Globals.CurrentApp.Log.WriteLine($"[AsyncLoading] Cannot get update info from '{url}': {e.Message}", Globals.CurrentApp.Log.Module.MainWindow);
                    }
                });

                if (Globals.CurrentApp.Config.Get("FirstRun", "1") == "1")
                {
                    if (FirstRun())
                        Globals.CurrentApp.Config.Set("FirstRun", "0");
                    else
                        Dispatcher.InvokeAsync(() => { DoMessageDialog("FirstRun Method returns false!"); });
                }

                var connected = Globals.CurrentApp.Peek();

                if (Globals.CurrentApp.Config.Get("NeedUpdate", "0") == "1")
                {
                    Dispatcher.InvokeAsync(() => DoMessageDialog("N2N GO 上一次更新未成功，将会在本次关闭后重新尝试。", "更新"));
                }
                else
                {
                    if (connected) Globals.CurrentApp.CheckN2NGOClientUpdate();
                }

                Globals.CurrentApp.PrintMemSet("MainWindow AsyncLoading Sub Finished");
            });

            swbm.Stop();
            Globals.CurrentApp.Log.WriteLine($"App asynchronous loading finished({swbm.Elapsed})", Globals.CurrentApp.Log.Module.MainWindow);
            Globals.CurrentApp.PrintMemSet("MainWindow AsyncLoading End");
        }
        private void SetWindowMaxNormalButtonImage()
        {
            LBImage_Wnd_Btn_MaxNormal.Source = new BitmapImage(new Uri(String.Format("/Data/Images/icon/wnd_btn_{0}.png", (WindowState == WindowState.Maximized ? "normal" : "max")), UriKind.Relative));
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            CloseExit();
        }
        private void ButtonWindowMin_Click(object sender, RoutedEventArgs e)
        {
            var tg = (this.RenderTransform as TransformGroup) ?? throw new NullReferenceException("[MainWindow] 'this.RenderTransform as TransformGroup' gets null!");
            var tg_st = tg.Children[0] as ScaleTransform ?? throw new NullReferenceException("[MainWindow] 'TransformGroup.Children[0] as ScaleTransform' gets null!");
            var tg_tt = tg.Children[1] as TranslateTransform ?? throw new NullReferenceException("[MainWindow] 'TransformGroup.Children[1] as TranslateTransform' gets null!");

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
        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
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
                var transformGroup = (this.RenderTransform as TransformGroup) ?? throw new NullReferenceException("[MainWindow] 'this.RenderTransform as TransformGroup' gets null!");
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

        private void Window_Activated(object sender, EventArgs e)
        {
            var b = new SolidColorBrush(((SolidColorBrush)TitleBorder.Background).Color);
            TitleBorder.Background = b;
            b.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation { To = (Color)((ResourceDictionary)Globals.CurrentApp.Get.Resources["CurrentColorPalette"])["Palette_400"], Duration = TimeSpan.FromSeconds(0.25) });

        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            var b = new SolidColorBrush(((SolidColorBrush)TitleBorder.Background).Color);
            TitleBorder.Background = b;
            b.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation { To = (Color)((ResourceDictionary)Globals.CurrentApp.Get.Resources["CurrentColorPalette"])["Palette_200"], Duration = TimeSpan.FromSeconds(0.25) });
        }

        private void WinMove_main(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
        }
    }

    public class LocaleSelectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string objAsString)
                return value;

            var res = Globals.CurrentApp.Locale.ReadLocal(objAsString);
            if (res == null)
                return value;

            return res.Language;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}

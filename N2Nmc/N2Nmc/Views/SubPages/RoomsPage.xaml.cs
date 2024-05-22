using HandyControl.Controls;
using HandyControl.Tools.Extension;
using Microsoft.Windows.Themes;
using N2Nmc.UtilsClass;
using N2Nmc.Views.SubPages.Dialogs;
using N2Nmc_Protocol;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using static N2Nmc.UtilsClass.SharedData;
using static N2Nmc_Protocol.Package;

namespace N2Nmc.Views.SubPages
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>

    public partial class RoomsPage : Page
    {
        public string SwitchFakeServerContenter { get => _FakeServer ? "虚拟列表" : "真实列表"; }

        private bool _FakeServer = false;
        public bool FakeServer
        {
            get => _FakeServer;

            set
            {
                bool? isRefreshing = null;
                Dispatcher.Invoke(() => isRefreshing = IsRefreshing);
                if (isRefreshing == true)
                    return;

                if (_FakeServer == value)
                    return;

                _FakeServer = value;

                Refresh();

                //SwitchFakeServer.IsChecked = value;
                //SwitchFakeServer.Content = SwitchFakeServerContenter;
            }
        }

        private bool IsRefreshing = false;

        private enum SortMode
        {
            None = 0,
            ByA_Z,
            ByZ_A,
            Handled = 10
        }

        //文本对比,用于检测n2n是否启动成功
        DispatcherTimer timer = new DispatcherTimer();

        //DispatcherTimer TimerSqlConnectionSaver = new DispatcherTimer(); //SQL连接守护

        DoubleAnimation smallerAnimation = new DoubleAnimation { To = 0.97, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        DoubleAnimation smallsmallerAnimation = new DoubleAnimation { To = 0.92, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        DoubleAnimation biggerAnimation = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.25), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };

        DoubleAnimation joinBtnFadeIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.25) };
        DoubleAnimation joinBtnFadeOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.45) };
        DoubleAnimation joinBtnLandIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
        DoubleAnimation joinBtnLandVIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.20), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        DoubleAnimation joinBtnLandOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.85), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };

        DoubleAnimation blurIn = new DoubleAnimation { To = 8, Duration = TimeSpan.FromSeconds(0.30), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        DoubleAnimation blurOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.45), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        DoubleAnimation fadeIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.20), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        DoubleAnimation fadeOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.45), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };

        DoubleAnimation loadingDialogFadeIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.20), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        DoubleAnimation loadingDialogFadeOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.45), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };

        List<Card> cards = new List<Card>();

        public readonly static string GrowlToken = "RoomsPageGrowl";

        DoubleAnimation ExpandGridFunc = new DoubleAnimation { Duration = TimeSpan.FromSeconds(0.2), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        DoubleAnimation CollapseGridFunc = new DoubleAnimation { Duration = TimeSpan.FromSeconds(0.3), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        bool GridFuncLS;

        public RoomsPage()
        {
            InitializeComponent();

            Growl.Register(GrowlToken, PanelMsg);

            CardDialogBorder.Visibility = Visibility.Collapsed;
            DialogLoadingRooms.Visibility = Visibility.Visible;

            CollapseGridFunc.To = GridFunc.MinWidth;
            ExpandGridFunc.To = GridFunc.MaxWidth;
            GridColDef2.Width = new GridLength(GridFunc.MinWidth);
            GridFuncLS = true;
            SwitchGridFunc(false);

            Cards.Items.Clear();
            timer.Interval = TimeSpan.FromSeconds(1);
        }

        ~RoomsPage()
        {
            timer.Stop();

            Growl.Unregister(GrowlToken, PanelMsg);
        }

        Thread? refreshThread = null;
        private void InitCard(object card)
        {
            if (!((Card)(((Grid)card).DataContext)).TransformInited)
            {
                ((Border)((Grid)card).Parent).RenderTransform = new TransformGroup { Children = new TransformCollection(new Transform[] { new ScaleTransform(1, 1, .5, .5), new SkewTransform(0, 0, 0, 0) }) };
                ((Card)(((Grid)card).DataContext)).TransformInited = true;

                Button btn = (Button)((Grid)card).Children[3];
                btn.Opacity = 0;
                SharedData.UIAnimation.InitButton(btn);
                //Button btn1 = (Button)((Grid)card).Children[2];
                //btn1.Opacity = 0;
                //btn.RenderTransform = new ScaleTransform(0,1, .5, .5);
            }
        }

        private void LoadingDialogOut(Dispatcher dispatcher)
        {
            dispatcher.Invoke(() =>
            {
                TaskCompletionSource<object> animationCompletedTask1 = new TaskCompletionSource<object>();
                DoubleAnimation _loadingDialogFadeOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.45), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
                _loadingDialogFadeOut.Completed += (s, _) =>
                {
                    animationCompletedTask1.SetResult(0);
                };
                DialogLoadingRooms.BeginAnimation(OpacityProperty, _loadingDialogFadeOut);

                Task.Run(() =>
                {
                    // 等待动画完成
                    animationCompletedTask1.Task.Wait();

                    dispatcher.Invoke(() =>
                    {
                        DialogLoadingRooms.Visibility = Visibility.Collapsed;

                        IsRefreshing = false;
                    });
                });
            });
        }

        public static string GetSortKey(string value)
        {
            StringBuilder sortKey = new StringBuilder();

            foreach (char c in value)
            {
                if (char.IsLetter(c))
                {
                    sortKey.Append(c.ToString().ToLower());
                }
                else if (char.IsDigit(c))
                {
                    sortKey.Append(c);
                }
            }

            return sortKey.ToString();
        }

        private void Filter_Sort(string? Key = null)
        {
            Dispatcher.Invoke(() => Cards.Items.Clear());

            //switch (Sort)
            //{
            //    default: throw new NotImplementedException();

            //        case SortMode.None: break;

            //    case SortMode.ByA_Z:
            //        {
            //            cards.OrderBy(r => GetSortKey(r.RoomName));

            //            break;
            //        }
            //    case SortMode.ByZ_A:
            //        {
            //            break;
            //        }
            //    case SortMode.Handled:
            //        {
            //            break;
            //        }
            //}

            cards = cards.OrderBy(r => GetSortKey(r.RoomName)).ToList(); // Sorting

            if (string.IsNullOrEmpty(Key))
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    cards[i].TransformInited = false;
                    if (!cards[i].IsRoomVisible)
                        continue;
                    Dispatcher.Invoke(() => Cards.Items.Add(cards[i]));
                }

                return;
            }
            else
            { // Else filte it.
                Key = Key.Trim().ToLower(); // Format key
                string[] keys = Key.Split(' ');

                for (int i = 0; i < cards.Count; i++)
                {
                    bool pairs = false;
                    foreach (string key in keys)
                    {
                        if (cards[i].RoomName.Contains(key))
                            pairs = true;
                        if (cards[i].RoomCode.Contains(key))
                            pairs = true;
                    }
                    if (!pairs)
                        continue;

                    cards[i].TransformInited = false;
                    if (!cards[i].IsRoomVisible)
                        continue;
                    Dispatcher.Invoke(() => Cards.Items.Add(cards[i]));
                }
            }
        }

        private void ShowCardInfoDialog()
        {
            CardDialogBorder.Opacity = 0;
            CardDialogBorder.Visibility = Visibility.Visible;

            CardDialogBorder.BeginAnimation(OpacityProperty, fadeIn);
            MainBlur.BeginAnimation(BlurEffect.RadiusProperty, blurIn);
        }

        private async void HideCardInfoDialog()
        {
            var animationCompletedTask = new TaskCompletionSource<object>();
            DoubleAnimation _blurOut = blurOut.Clone();
            _blurOut.Completed += (_, __) =>
            {
                animationCompletedTask.SetResult(0);
            };

            CardDialogBorder.BeginAnimation(OpacityProperty, fadeOut);
            MainBlur.BeginAnimation(BlurEffect.RadiusProperty, _blurOut);
            await Task.Run(() =>
            {
                animationCompletedTask.Task.Wait();

                Dispatcher.BeginInvoke(() =>
                {
                    CardDialogBorder.Visibility = Visibility.Collapsed;
                });
            });
        }

        public async void Refresh(ulong? page_index = null)
        {
            if (refreshThread != null && refreshThread.IsAlive)
                return;

            Dispatcher dispatcher = ((MainView)App.Current.MainWindow).Dispatcher;
            bool? isRefreshing = null;
            dispatcher.Invoke(() => isRefreshing = IsRefreshing);
            if (isRefreshing == true)
                return;

            Stopwatch sw = Stopwatch.StartNew();

            cards.Clear();

            DialogLoadingRooms.Opacity = 0;
            DialogLoadingRooms.Visibility = Visibility.Visible;
            TaskCompletionSource<object> animationCompletedTask = new TaskCompletionSource<object>();
            DoubleAnimation _loadingDialogFadeIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.20), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            _loadingDialogFadeIn.Completed += (s, _) =>
            {
                animationCompletedTask.SetResult(0);
            };
            dispatcher.InvokeAsync(() => DialogLoadingRooms.BeginAnimation(OpacityProperty, _loadingDialogFadeIn));

            await Task.Run(() =>
            {
                // 等待动画完成
                animationCompletedTask.Task.Wait();

                dispatcher.Invoke(() =>
                {
                    if (_FakeServer)
                    {
                        refreshThread = new Thread(() =>
                        {
                            dispatcher.Invoke(() => IsRefreshing = true);

                            int i = 5; // Cards to make
                            while (i-- > 0)
                            {
                                cards.Add(new Card { RoomName = "普通测试房间卡：" + i, RoomCode = SharedData.GetRoomCode("普通测试房间卡：" + i) });
                            }
                            cards.Add(new Card { RoomName = "隐藏测试房间卡", RoomCode = SharedData.GetRoomCode("隐藏测试房间卡"), IsRoomVisible = false });
                            cards.Add(new Card { RoomName = "有密码测试房间卡", RoomCode = SharedData.GetRoomCode("有密码测试房间卡"), IsRoomPasswordNeeded = true });

                            cards.AddRange(new Card[] {
                                new Card{RoomName="w"} ,
                                new Card{RoomName="W"} ,
                                new Card{RoomName="1"} ,
                                new Card{RoomName="2"} ,
                                new Card{RoomName="吧"} ,
                                new Card{RoomName="都"} ,
                                new Card{RoomName="去"} ,
                                new Card{RoomName="啊"} ,
                            });

                            Filter_Sort(default);


                            LoadingDialogOut(dispatcher);

                            IsRefreshing = false;
                            return;
                        }); refreshThread.Start();

                        return;
                    }

                    Random random = new Random(DateTime.Now.Millisecond);
                    refreshThread = new Thread(() =>
                    {
                        dispatcher.Invoke(() =>
                        {
                            IsRefreshing = true;
                            // Cards.Items.Add(new Card { Title = "点我刷新", IsFunctionButton = true, Text = "刷新" });
                        });

                        lock (NM_Connection)
                            if (NM_Connection == null)
                                throw new NullReferenceException(nameof(NM_Connection));

                        lock (NM_Connection)
                            try
                            {
                                if (!NM_Connection.IsConnected())
                                {
                                    for (int i = 0; i < 5; i++)
                                    {
                                        Thread.Sleep(1000);
                                        if (NM_Connection.IsConnected())
                                            break;
                                    }
                                    if (!NM_Connection.IsConnected())
                                    {
                                        Growl.Error("N2Nmc服务器未连接！", GrowlToken);
                                        LoadingDialogOut(dispatcher);
                                        return;
                                    }
                                }

                                if (page_index == null)
                                {
                                    NM_Connection.Send(MakePackage(Protocol.BaseHeader._rooms_pull_rooms_pages));
                                    Package? rooms_pages_pkg_get = NM_Connection.Receive();
                                    if (rooms_pages_pkg_get != null && (Protocol.BaseHeader)rooms_pages_pkg_get.Value.Header == Protocol.BaseHeader.msg_ulong && rooms_pages_pkg_get.Value.external_data != null)
                                    {
                                        ulong pages = MsgExternalData.Decode.MsgULong(rooms_pages_pkg_get.Value.external_data);

                                        var l = new List<ulong>();

                                        for (ulong i = 0; i < pages; i++)
                                            l.Add(i);

                                        Dispatcher.InvokeAsync(() =>
                                        {
                                            PageIndexSelectBoard.ItemsSource = l;
                                        });
                                    }
                                    else
                                        goto invalid;

                                    page_index = 0;
                                }

                                NM_Connection.Send(MakePackage(Protocol.BaseHeader._rooms_pull_rooms));
                                NM_Connection.Send(MakePackage(Protocol.BaseHeader.msg_ulong, Package.MsgExternalData.Encode.MsgULong((UInt32)page_index)));

                                // 开始查询
                                Package? rooms_count_pkg_get = NM_Connection.Receive();
                                if (rooms_count_pkg_get != null)
                                {
                                    if ((Protocol.BaseHeader)rooms_count_pkg_get.Value.Header == Protocol.BaseHeader.msg_ulong && rooms_count_pkg_get.Value.external_data != null)
                                    {
                                        uint roomCount = MsgExternalData.Decode.MsgULong(rooms_count_pkg_get.Value.external_data);

                                        while ((roomCount--) > 0)
                                        {
                                            Package[] roomPackage = new Package[5];
                                            for (int i = 0; i < roomPackage.Length; i++)
                                            {
                                                var pkg_get = NM_Connection.Receive();
                                                if (pkg_get == null || pkg_get.Value.external_data == null)
                                                    goto invalid;

                                                roomPackage[i] = pkg_get.Value;
                                            }

                                            string RoomCode;
                                            string RoomName;
                                            bool IsRoomPasswordNeeded;
                                            N2Nmc_Protocol.Objects.RoomColor ColorMajor;
                                            N2Nmc_Protocol.Objects.RoomColor ColorMinor;
                                            {
                                                byte[]? edata = roomPackage[0].external_data;
                                                if ((Protocol.BaseHeader)roomPackage[0].Header != Protocol.BaseHeader.msg_string || edata == null)
                                                    goto invalid;
                                                RoomCode = MsgExternalData.Decode.MsgString(edata);
                                            }
                                            {
                                                byte[]? edata = roomPackage[1].external_data;
                                                if ((Protocol.BaseHeader)roomPackage[1].Header != Protocol.BaseHeader.msg_string || edata == null)
                                                    goto invalid;
                                                RoomName = MsgExternalData.Decode.MsgString(edata);
                                            }
                                            {
                                                byte[]? edata = roomPackage[2].external_data;
                                                if ((Protocol.BaseHeader)roomPackage[2].Header != Protocol.BaseHeader.msg_byte || edata == null)
                                                    goto invalid;
                                                IsRoomPasswordNeeded = MsgExternalData.Decode.MsgByte(edata) == 1 ? true : false;
                                            }
                                            {
                                                byte[]? edata = roomPackage[3].external_data;
                                                if ((Protocol.BaseHeader)roomPackage[3].Header != Protocol.BaseHeader.msg_ulong || edata == null)
                                                    goto invalid;
                                                ColorMajor = new N2Nmc_Protocol.Objects.RoomColor(MsgExternalData.Decode.MsgULong(edata));
                                            }
                                            {
                                                byte[]? edata = roomPackage[4].external_data;
                                                if ((Protocol.BaseHeader)roomPackage[4].Header != Protocol.BaseHeader.msg_ulong || edata == null)
                                                    goto invalid;
                                                ColorMinor = new N2Nmc_Protocol.Objects.RoomColor(MsgExternalData.Decode.MsgULong(edata));
                                            }

                                            // Random Color
                                            //byte[] rgb = new byte[3];
                                            //random.NextBytes(rgb);
                                            //Dispatcher.InvokeAsync(() => cards.Add(new Card { RoomName = RoomName, RoomCode = RoomCode, IsRoomPasswordNeeded = IsRoomPasswordNeeded, ThemeBrushMinor = new SolidColorBrush(Color.FromArgb(0xFF, rgb[0], rgb[1], rgb[2])) }));

                                            Dispatcher.InvokeAsync(() => cards.Add(new Card { RoomName = RoomName, RoomCode = RoomCode, IsRoomPasswordNeeded = IsRoomPasswordNeeded,
                                                ThemeBrushMajor = new SolidColorBrush(Color.FromArgb(0xFF, ColorMajor.R, ColorMajor.G, ColorMajor.B)),
                                                ThemeBrushMinor = new SolidColorBrush(Color.FromArgb(0xFF, ColorMinor.R, ColorMinor.G, ColorMinor.B)) }));
                                        }
                                        ;

                                        goto done;
                                    }
                                }
                            invalid:
                                Growl.Error("无效 N2Nmc 服务器协议");
                                LoadingDialogOut(dispatcher);
                                return;
                            }
                            catch (Exception ex)
                            {
                                Growl.Error("Exception On Refreshing True Server:\n" + ex.ToString(), GrowlToken);
                                LoadingDialogOut(dispatcher);
                                return;
                            }
                        done:
                        dispatcher.InvokeAsync(() =>
                        {
                            if (!string.IsNullOrEmpty(RoomsFiltering.Text))
                                Filter_Sort(RoomsFiltering.Text);
                            else
                                Filter_Sort(default);

                            LoadingDialogOut(dispatcher);
                        });
                    });

                    refreshThread.Start();
                });
            });

            sw.Stop();
            Console.WriteLine("[Rooms Refresh] Total Time: {0}ms", sw.ElapsedMilliseconds);
        }

        private void TempGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var grid = ((Grid)sender);
            var gridp = grid.Parent as Border;
            if (gridp == null)
                throw new NullReferenceException(nameof(gridp));

            gridp.Background = new SolidColorBrush(Color.FromArgb(0x9F, 0xB3, 0xB3, 0xB3));

            TransformGroup TG = (TransformGroup)gridp.RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];

            st.BeginAnimation(ScaleTransform.ScaleXProperty, smallerAnimation);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, smallerAnimation);

            Button btn = (Button)grid.Children[3];
            btn.BeginAnimation(OpacityProperty, joinBtnFadeIn);
            //Button btn1 = (Button)((Grid)sender).Children[2];
            //btn1.BeginAnimation(OpacityProperty, joinBtnFadeIn);
            //btn.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, joinBtnLandIn);
            // btn.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, joinBtnLandVIn);
        }
        private void TempGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var grid = ((Grid)sender);
            var gridp = grid.Parent as Border;
            if (gridp == null)
                throw new NullReferenceException(nameof(gridp));

            gridp.Background = new SolidColorBrush(Color.FromArgb(0x5F, 0xB3, 0xB3, 0xB3));

            TransformGroup TG = (TransformGroup)gridp.RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];

            st.BeginAnimation(ScaleTransform.ScaleXProperty, biggerAnimation);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, biggerAnimation);

            Button btn = (Button)grid.Children[3];
            btn.BeginAnimation(OpacityProperty, joinBtnFadeOut);
            //Button btn1 = (Button)((Grid)sender).Children[2];
            //btn1.BeginAnimation(OpacityProperty, joinBtnFadeOut);
            //btn.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, joinBtnLandOut);
            // btn.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, joinBtnLandOut);
        }
        private void TempGrid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var grid = ((Grid)sender);
            var gridp = grid.Parent as Border;
            if (gridp == null)
                throw new NullReferenceException(nameof(gridp));

            TransformGroup TG = (TransformGroup)gridp.RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];

            st.BeginAnimation(ScaleTransform.ScaleXProperty, smallsmallerAnimation);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, smallsmallerAnimation);
        }
        private void TempGrid_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var grid = ((Grid)sender);
            var gridp = grid.Parent as Border;
            if (gridp == null)
                throw new NullReferenceException(nameof(gridp));

            TransformGroup TG = (TransformGroup)gridp.RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];

            st.BeginAnimation(ScaleTransform.ScaleXProperty, biggerAnimation);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, biggerAnimation);
        }
        private void TempGrid_Initialized(object sender, EventArgs e)
        {
            InitCard(sender);
        }

        private void TempGrid_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Click
            CardDialogBorderFrame.Navigate(new Dialogs.DialogRoomInfo((Card)(((Grid)sender).DataContext), async (_, __) =>
            {
                SharedData.KillEdge();

                Card card = (Card)((Grid)sender).DataContext;

                Growl.Warning("正在加入房间,请稍后...", GrowlToken);

                if (card.IsRoomPasswordNeeded)
                {
                    ExecLog execLog = new ExecLog();

                    string password = ((DialogRoomInfo)CardDialogBorderFrame.Content).TextBoxPasswd.Text;
                    if (string.IsNullOrEmpty(password.Trim()))
                    {
                        Growl.Warning("密码不能为空！", GrowlToken);
                        return;
                    }

                    timer.Stop();
                    timer = new DispatcherTimer();
                    timer.Tick += (_, __) =>
                    {
                        if (execLog.LogOut.Contains("[OK] edge <<< ================ >>> supernode"))
                        {
                            timer.Stop();

                            if (string.IsNullOrEmpty(card.RoomCode))
                            {
                                Growl.Error("未获取到房间号", GrowlToken);
                                ExitRoom();
                                timer.Stop();
                            }
                            else
                            {
                                if (GetMainView.pageRoom == null)
                                    throw new NullReferenceException(nameof(GetMainView.pageRoom));
                                GetMainView.EnterRoom(card.RoomCode);
                                timer.Stop();
                            }


                        }
                        if (execLog.LogOut.Contains("No Windows tap devices found, did you run tapinstall.exe?"))
                        {
                            Growl.Warning("请检测虚拟网卡是否安装成功，或者虚拟网卡是否正在使用(查看资源管理器是否有edge.exe正在运行)", GrowlToken);
                            timer.Stop();
                        }
                    };

                    timer.Start();

                    string cmd = edgePath + " -c " + card.RoomCode + " -k " + password + " -l " + SharedData.n2nServerIPP;
                    EdgeConnectionInfo.CurrentRoomCode = card.RoomCode;

                    execLog.SetCommand(cmd);
                    int r = await execLog.ExecuteAsync();
                    if (r == -21)
                    {
                        // No Windows tap
                        Growl.WarningGlobal("请检测虚拟网卡是否安装成功，或者虚拟网卡是否正在使用(查看资源管理器是否有edge.exe正在运行)");
                        timer.Stop();
                    }

                    HideCardInfoDialog();
                }
                else
                {
                    ExecLog execLog = new ExecLog();

                    timer.Stop();
                    timer = new DispatcherTimer();
                    timer.Tick += (_, __) =>
                    {
                        if (execLog.LogOut.Contains("[OK] edge <<< ================ >>> supernode"))
                        {
                            timer.Stop();
                            //启动房间窗口
                            if (string.IsNullOrEmpty(card.RoomCode))
                            {
                                Growl.Error("未获取到房间号", GrowlToken);
                                ExitRoom();
                                timer.Stop();
                            }
                            else
                            {
                                GetMainView.EnterRoom(card.RoomCode);
                                timer.Stop();
                            }


                        }
                        if (execLog.LogOut.Contains("No Windows tap devices found, did you run tapinstall.exe?"))
                        {
                            Growl.Warning("请检测虚拟网卡是否安装成功，或者虚拟网卡是否正在使用(查看资源管理器是否有edge.exe正在运行)", GrowlToken);
                            timer.Stop();
                        }
                    };

                    timer.Start();

                    string cmd = edgePath + " -c " + card.RoomCode + " -k " + SharedData.DefaultRoomPasswd + " -l " + SharedData.n2nServerIPP;
                    EdgeConnectionInfo.CurrentRoomCode = card.RoomCode;

                    execLog.SetCommand(cmd);
                    int r = await execLog.ExecuteAsync();
                    if (r == -21)
                    {
                        // No Windows tap
                        Growl.WarningGlobal("请检测虚拟网卡是否安装成功，或者虚拟网卡是否正在使用(查看资源管理器是否有edge.exe正在运行)");
                        timer.Stop();
                    }
                }
            }, (_, __) => HideCardInfoDialog()));

            ShowCardInfoDialog();
        }

        private void SwitchFakeServer_Checked(object sender, RoutedEventArgs e)
        {
            FakeServer = true;
        }

        private void SwitchFakeServer_Unchecked(object sender, RoutedEventArgs e)
        {
            FakeServer = false;
        }

        private void SwitchFakeServer_Initialized(object sender, EventArgs e)
        {
            ((ToggleButton)sender).IsChecked = FakeServer;
            ((ToggleButton)sender).Content = SwitchFakeServerContenter;
        }

        private void RoomsFiltering_SearchStarted(object sender, HandyControl.Data.FunctionEventArgs<string> e)
        {
            Filter_Sort(RoomsFiltering.Text);
        }

        private void RoomsFiltering_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RoomsFiltering.Text.Trim()))
                Filter_Sort(RoomsFiltering.Text);
        }

        private async void ButtonJoin_Click(object sender, RoutedEventArgs e)
        {
            SharedData.KillEdge();

            Card card = (Card)((Button)sender).DataContext;

            Growl.Warning("正在加入房间,请稍后...", GrowlToken);

            if (card.IsRoomPasswordNeeded)
            {
                ExecLog execLog = new ExecLog();

                Growl.Warning("房间需要密码，请输入密码", GrowlToken);

                DialogRoomInfo d = new Dialogs.DialogRoomInfo(((Card)(((Button)sender).DataContext)), async (_, __) =>
                {
                    string password = ((DialogRoomInfo)CardDialogBorderFrame.Content).TextBoxPasswd.Text;
                    if (string.IsNullOrEmpty(password.Trim()))
                    {
                        Growl.Warning("密码不能为空！", GrowlToken);
                        return;
                    }

                    timer.Stop();
                    timer = new DispatcherTimer();
                    timer.Tick += (_, __) =>
                    {
                        if (execLog.LogOut.Contains("[OK] edge <<< ================ >>> supernode"))
                        {
                            timer.Stop();

                            if (string.IsNullOrEmpty(card.RoomCode))
                            {
                                Growl.Error("未获取到房间号", GrowlToken);
                                ExitRoom();
                                timer.Stop();
                            }
                            else
                            {
                                GetMainView.EnterRoom(card.RoomCode);
                                timer.Stop();
                            }
                        }
                        if (execLog.LogOut.Contains("No Windows tap devices found, did you run tapinstall.exe?"))
                        {
                            Growl.Warning("请检测虚拟网卡是否安装成功，或者虚拟网卡是否正在使用(查看资源管理器是否有edge.exe正在运行)", GrowlToken);
                            timer.Stop();
                        }
                    };

                    timer.Start();

                    string cmd = edgePath + " -c " + card.RoomCode + " -k " + password + " -l " + SharedData.n2nServerIPP;
                    EdgeConnectionInfo.CurrentRoomCode = card.RoomCode;

                    execLog.SetCommand(cmd);
                    int r = await execLog.ExecuteAsync();
                    if (r == -21)
                    {
                        // No Windows tap
                        Growl.WarningGlobal("请检测虚拟网卡是否安装成功，或者虚拟网卡是否正在使用(查看资源管理器是否有edge.exe正在运行)");
                        timer.Stop();
                    }

                    HideCardInfoDialog();
                }, (_, __) => HideCardInfoDialog());

                CardDialogBorderFrame.Navigate(d);
                ShowCardInfoDialog();
            }
            else
            {
                ExecLog execLog = new ExecLog();

                timer.Stop();
                timer = new DispatcherTimer();
                timer.Tick += (_, __) =>
                {
                    if (execLog.LogOut.Contains("[OK] edge <<< ================ >>> supernode"))
                    {
                        timer.Stop();

                        if (string.IsNullOrEmpty(card.RoomCode))
                        {
                            Growl.Error("未获取到房间号", GrowlToken);
                            ExitRoom();
                            timer.Stop();
                        }
                        else
                        {
                            GetMainView.EnterRoom(card.RoomCode);
                            timer.Stop();
                        }


                    }
                    if (execLog.LogOut.Contains("No Windows tap devices found, did you run tapinstall.exe?"))
                    {
                        Growl.Warning("请检测虚拟网卡是否安装成功，或者虚拟网卡是否正在使用(查看资源管理器是否有edge.exe正在运行)", GrowlToken);
                        timer.Stop();
                    }
                };

                timer.Start();

                string cmd = edgePath + " -c " + card.RoomCode + " -k " + SharedData.DefaultRoomPasswd + " -l " + SharedData.n2nServerIPP;
                EdgeConnectionInfo.CurrentRoomCode = card.RoomCode;

                execLog.SetCommand(cmd);
                int r = await execLog.ExecuteAsync();
                if (r == -21)
                {
                    // No Windows tap
                    Growl.WarningGlobal("请检测虚拟网卡是否安装成功，或者虚拟网卡是否正在使用(查看资源管理器是否有edge.exe正在运行)");
                    timer.Stop();
                }
            }
        }


        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void ButtonCancelLoadingRooms_Click(object sender, RoutedEventArgs e)
        {
            LoadingDialogOut(((MainView)App.Current.MainWindow).Dispatcher);
        }

        private void PageIndexButton_Click(object sender, RoutedEventArgs e)
        {
            ulong a = (ulong)((Button)sender).DataContext;
            Refresh(a);
        }

        private void Button_Initialized(object sender, EventArgs e)
        {
            SharedData.UIAnimation.InitButton((Button)sender);
        }

        public void SwitchGridFunc(bool? set = null)
        {
            if (set != null)
                GridFuncLS = set.Value;

            GridFunc.BeginAnimation(WidthProperty, (GridFuncLS ? ExpandGridFunc : CollapseGridFunc));
            MainBlur.BeginAnimation(BlurEffect.RadiusProperty, GridFuncLS ? blurIn : blurOut);

            if (set == null)
                GridFuncLS = !GridFuncLS;
        }

        private void GridFunc_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SwitchGridFunc(true);
        }

        private void GridFunc_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SwitchGridFunc(false);
        }
    }

    public class Card
    {
        public bool IsFunctionButton { get; set; } = false;

        public bool TransformInited = false;

        public string BtnText { get; set; } = "加入";

        public Brush ThemeBrushMajor { get; set; } = new SolidColorBrush(Color.FromRgb(0xB3, 0xB3, 0xB3));
        public Brush ThemeBrushMinor { get; set; } = new SolidColorBrush(Colors.Red);

        public string RoomName { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;
        public bool IsRoomVisible { get; set; } = true;
        public bool IsRoomPasswordNeeded { get; set; } = false;
        public string IsRoomPasswordNeededText { get => IsRoomPasswordNeeded ? "有密码" : "公开"; }
        public int MembersCount { get; set; } = 0;
        public string MembersCountString
        {
            get
            {
                return MembersCount.ToString()+"人";
            }
        }
    }
}


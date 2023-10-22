using HandyControl.Controls;
using N2Nmc.UtilsClass;
using N2Nmc.Views.SubPages.Dialogs;
using N2Nmc_Protocol;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.PortableExecutable;
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

        DoubleAnimation blurIn = new DoubleAnimation { To = 8, Duration = TimeSpan.FromSeconds(0.60), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        DoubleAnimation blurOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.70), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        DoubleAnimation fadeIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.20), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        DoubleAnimation fadeOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.45), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };

        DoubleAnimation loadingDialogFadeIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.20), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        DoubleAnimation loadingDialogFadeOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.45), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };

        List<Card> cards = new List<Card>();

        public readonly static string GrowlToken = "RoomsPageGrowl";

        public RoomsPage()
        {
            InitializeComponent();

            Growl.Register(GrowlToken, PanelMsg);

            CardDialogBorder.Visibility = Visibility.Collapsed;

            Cards.Items.Clear();

            timer.Interval = TimeSpan.FromSeconds(1);

            DialogLoadingRooms.Visibility = Visibility.Visible;
        }

        ~RoomsPage()
        {
            timer.Stop();

            Growl.Unregister(GrowlToken, PanelMsg);
        }


        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    // 在这里添加按钮单击事件的处理逻辑

        //    // 判断按钮的 Command 是否等于集合中的某个数据，并返回数据的位置信息
        //    Button button = (Button)sender;
        //    string Command1 = button.Command.ToString();
        //    Console.WriteLine(Command1);
        //    Console.WriteLine(cardsall);
        //    //int index = cardsall.FindIndex(card => card == Command1);
        //    int index = 0;

        //    if (index != -1)
        //    {
        //        // 找到了匹配的数据
        //        // index 是数据在集合中的位置信息
        //        // 可以根据需要进行进一步处理
        //        Console.WriteLine($"找到了 Command 为 {button.Command} 的对象，位置为 {index}。");
        //    }
        //    else
        //    {
        //        // 没有找到匹配的数据
        //        Console.WriteLine($"未找到 Command 为 {button.Command} 的对象。");
        //    }
        //}

        Thread? refreshThread = null;
        private void InitCard(object card)
        {
            if (!((Card)(((Grid)card).DataContext)).TransformInited)
            {
                ((Grid)card).RenderTransform = new TransformGroup { Children = new TransformCollection(new Transform[] { new ScaleTransform(1, 1, .5, .5), new SkewTransform(0, 0, 0, 0) }) };
                ((Card)(((Grid)card).DataContext)).TransformInited = true;

                Button btn = (Button)((Grid)card).Children[1];
                btn.Opacity = 0;
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

        public async void Refresh()
        {
            if (refreshThread != null && refreshThread.IsAlive)
                return;

            Dispatcher dispatcher = ((MainView)App.Current.MainWindow).Dispatcher;
            bool? isRefreshing = null;
            dispatcher.Invoke(() => isRefreshing = IsRefreshing);
            if (isRefreshing == true)
                return;

            cards.Clear();

            DialogLoadingRooms.Opacity = 0;
            DialogLoadingRooms.Visibility = Visibility.Visible;
            TaskCompletionSource<object> animationCompletedTask = new TaskCompletionSource<object>();
            DoubleAnimation _loadingDialogFadeIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.20), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            _loadingDialogFadeIn.Completed += (s, _) =>
            {
                animationCompletedTask.SetResult(0);
            };
            dispatcher.Invoke(() => DialogLoadingRooms.BeginAnimation(OpacityProperty, _loadingDialogFadeIn));

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

                                NM_Connection.Send(MakePackage(Protocol.BaseHeader._room_pull_rooms));

                                // 开始查询
                                Package? rooms_count_pkg_get = NM_Connection.Receive();
                                if (rooms_count_pkg_get != null)
                                {
                                    if ((Protocol.BaseHeader)rooms_count_pkg_get.Value.Header == Protocol.BaseHeader.msg_ulong && rooms_count_pkg_get.Value.external_data != null)
                                    {
                                        uint roomCount = MsgExternalData.Decode.MsgULong(rooms_count_pkg_get.Value.external_data);

                                        while ((roomCount--) > 0)
                                        {
                                            Package[] roomPackage = new Package[3];
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

                                            cards.Add(new Card { RoomName = RoomName, RoomCode = RoomCode, IsRoomPasswordNeeded = IsRoomPasswordNeeded });
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
                        dispatcher.Invoke(() =>
                        {
                            if (!string.IsNullOrEmpty(RoomsSearching.Text))
                                Filter_Sort(RoomsSearching.Text);
                            else
                                Filter_Sort(default);

                            LoadingDialogOut(dispatcher);
                        });

                        Growl.Success("服务器列表获取成功", GrowlToken);
                    });

                    refreshThread.Start();
                });
            });
        }

        private void TempGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((Grid)sender).Background = new SolidColorBrush(Color.FromArgb(0x9F, 255, 255, 255));

            TransformGroup TG = (TransformGroup)((Grid)sender).RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];

            st.BeginAnimation(ScaleTransform.ScaleXProperty, smallerAnimation);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, smallerAnimation);

            Button btn = (Button)((Grid)sender).Children[1];
            btn.BeginAnimation(OpacityProperty, joinBtnFadeIn);
            //Button btn1 = (Button)((Grid)sender).Children[2];
            //btn1.BeginAnimation(OpacityProperty, joinBtnFadeIn);
            //btn.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, joinBtnLandIn);
            // btn.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, joinBtnLandVIn);
        }

        private void TempGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((Grid)sender).Background = new SolidColorBrush(Color.FromArgb(0x5F, 255, 255, 255));

            TransformGroup TG = (TransformGroup)((Grid)sender).RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];

            st.BeginAnimation(ScaleTransform.ScaleXProperty, biggerAnimation);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, biggerAnimation);

            Button btn = (Button)((Grid)sender).Children[1];
            btn.BeginAnimation(OpacityProperty, joinBtnFadeOut);
            //Button btn1 = (Button)((Grid)sender).Children[2];
            //btn1.BeginAnimation(OpacityProperty, joinBtnFadeOut);
            //btn.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, joinBtnLandOut);
            // btn.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, joinBtnLandOut);
        }

        private void TempGrid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TransformGroup TG = (TransformGroup)((Grid)sender).RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];

            st.BeginAnimation(ScaleTransform.ScaleXProperty, smallsmallerAnimation);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, smallsmallerAnimation);
        }

        private void TempGrid_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TransformGroup TG = (TransformGroup)((Grid)sender).RenderTransform;
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

                    string cmd = edgePath+" -c " + card.RoomCode + " -k " + password + " -l " + SharedData.n2nServerIPP;
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

                    string cmd = edgePath+" -c " + card.RoomCode + " -k " + SharedData.DefaultRoomPasswd + " -l " + SharedData.n2nServerIPP;
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

        private void RoomsSearching_SearchStarted(object sender, HandyControl.Data.FunctionEventArgs<string> e)
        {
            Filter_Sort(RoomsSearching.Text);
        }

        private void RoomsSearching_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RoomsSearching.Text.Trim()))
                Filter_Sort(RoomsSearching.Text);
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

                    string cmd = edgePath+" -c " + card.RoomCode + " -k " + password + " -l " + SharedData.n2nServerIPP;
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

                string cmd = edgePath+" -c " + card.RoomCode + " -k " + SharedData.DefaultRoomPasswd + " -l " + SharedData.n2nServerIPP;
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

        private void ButtonDel_Click(object sender, RoutedEventArgs e)
        {
            CloseRoom(((Card)((Button)sender).DataContext).RoomCode);
            Refresh();
        }

        private void ButtonCancelLoadingRooms_Click(object sender, RoutedEventArgs e)
        {
            LoadingDialogOut(((MainView)App.Current.MainWindow).Dispatcher);
        }
    }

    public class Card
    {
        public bool IsFunctionButton { get; set; } = false;

        public bool TransformInited = false;

        public string BtnText { get; set; } = "加入";

        public string RoomName { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;
        public bool IsRoomVisible { get; set; } = true;
        public bool IsRoomPasswordNeeded { get; set; } = false;
        public string IsRoomPasswordNeededText { get => IsRoomPasswordNeeded ? "有密码" : "公开"; }
    }
}


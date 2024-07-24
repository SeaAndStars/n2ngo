using N2NGO.UtilsClass;
using N2NGO.Views.SubPages.Dialogs;
using N2NGO.Views.SubPages.Dialogs.MessageDialogs;
using N2NGO_Core;
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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using static N2NGO_Core.Package;

namespace N2NGO.Views.SubPages
{
    /// <summary>
    /// RoomsPage.xaml 的交互逻辑
    /// </summary>

    public partial class RoomsPage : Page
    {
        private bool IsRefreshing = false;

        private enum SortMode
        {
            None = 0,
            ByA_Z,
            ByZ_A,
            Handled = 10
        }

        DispatcherTimer timer = new();

        private readonly DoubleAnimation smallerAnimation = new() { To = 0.97, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        private readonly DoubleAnimation smallsmallerAnimation = new() { To = 0.92, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        private readonly DoubleAnimation biggerAnimation = new() { To = 1, Duration = TimeSpan.FromSeconds(0.25), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };

        private readonly DoubleAnimation blurIn = new() { To = 8, Duration = TimeSpan.FromSeconds(0.30), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        private readonly DoubleAnimation blurOut = new() { To = 0, Duration = TimeSpan.FromSeconds(0.45), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        private readonly DoubleAnimation fadeIn = new() { To = 1, Duration = TimeSpan.FromSeconds(0.20), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        private readonly DoubleAnimation fadeOut = new() { To = 0, Duration = TimeSpan.FromSeconds(0.25), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };

        List<RoomsPageRoomCardModel> cards = new();

        private readonly DoubleAnimation ExpandGridFunc = new() { Duration = TimeSpan.FromSeconds(0.2), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        private readonly DoubleAnimation CollapseGridFunc = new() { Duration = TimeSpan.FromSeconds(0.3), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        bool GridFuncLS;

        public RoomsPage()
        {
            InitializeComponent();

            CardDialogBorder.Visibility = Visibility.Collapsed;
            DialogLoadingRooms.Visibility = Visibility.Collapsed;

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
        }

        Thread? refreshThread = null;
        private static void InitCard(object card)
        {
            if (!((RoomsPageRoomCardModel)(((Grid)card).DataContext)).TransformInited)
            {
                ((Border)((Grid)card).Parent).RenderTransform = new TransformGroup { Children = new TransformCollection(new Transform[] { new ScaleTransform(1, 1, .5, .5), new SkewTransform(0, 0, 0, 0) }) };
                ((RoomsPageRoomCardModel)(((Grid)card).DataContext)).TransformInited = true;
            }
        }

        private void DialogOut(Grid dialog, Dispatcher dispatcher)
        {
            dispatcher.InvokeAsync(() =>
            {
                TaskCompletionSource<object> animationCompletedTask1 = new();
                DoubleAnimation _loadingDialogFadeOut = new() { To = 0, Duration = TimeSpan.FromSeconds(0.45), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
                _loadingDialogFadeOut.Completed += (s, _) =>
                {
                    animationCompletedTask1.SetResult(0);
                };
                dialog.BeginAnimation(OpacityProperty, _loadingDialogFadeOut);

                Task.Run(() =>
                {
                    animationCompletedTask1.Task.Wait();

                    dispatcher.Invoke(() =>
                    {
                        dialog.Visibility = Visibility.Collapsed;

                        IsRefreshing = false;
                    });
                });
            });
        }

        public static string GetSortKey(string value)
        {
            StringBuilder sortKey = new();

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

        private void Filter_Sort()
        {
            Dispatcher.Invoke(() => Cards.Items.Clear());

            cards = cards.OrderBy(r => GetSortKey(r.RoomName)).ToList(); // Sorting

            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].TransformInited = false;
                if (!cards[i].IsRoomVisible)
                    continue;
                Dispatcher.Invoke(() => Cards.Items.Add(cards[i]));
            }

            Dispatcher.Invoke(() => LabelStatus.Content = Cards.Items.Count);
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

            bool? isRefreshing = null;
            Dispatcher.Invoke(() => isRefreshing = IsRefreshing);
            if (isRefreshing == true)
                return;

            cards.Clear();

            DialogLoadingRooms.Opacity = 0;
            DialogLoadingRooms.Visibility = Visibility.Visible;
            TaskCompletionSource<object> animationCompletedTask = new();
            DoubleAnimation _loadingDialogFadeIn = new() { To = 1, Duration = TimeSpan.FromSeconds(0.20), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            _loadingDialogFadeIn.Completed += (s, _) =>
            {
                animationCompletedTask.SetResult(0);
            };
            Dispatcher.Invoke(() => DialogLoadingRooms.BeginAnimation(OpacityProperty, _loadingDialogFadeIn));

            await Task.Run(() =>
            {
                animationCompletedTask.Task.Wait();

                Stopwatch sw = Stopwatch.StartNew();

                Dispatcher.Invoke(() =>
                {
                    Random random = new(DateTime.Now.Millisecond);
                    refreshThread = new Thread(() =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            IsRefreshing = true;
                            // Cards.Items.Add(new RoomCardModel { Title = "点我刷新", IsFunctionButton = true, Text = "刷新" });
                        });

                        lock (SharedData.CurrentApp.N2NGOServerConnection)
                            try
                            {
                                if (!SharedData.CurrentApp.N2NGOServerConnection.Peek())
                                    goto end;

                                if (page_index == null)
                                {
                                    SharedData.CurrentApp.N2NGOServerConnection.Send(MakePackage(Protocol.BaseHeader._rooms_pull_rooms_pages));
                                    Package? rooms_pages_pkg_get = SharedData.CurrentApp.N2NGOServerConnection.Receive();
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

                                SharedData.CurrentApp.N2NGOServerConnection.Send(MakePackage(Protocol.BaseHeader._rooms_pull_rooms));
                                SharedData.CurrentApp.N2NGOServerConnection.Send(MakePackage(Protocol.BaseHeader.msg_ulong, Package.MsgExternalData.Encode.MsgULong((UInt32)page_index)));

                                Package? rooms_count_pkg_get = SharedData.CurrentApp.N2NGOServerConnection.Receive();
                                if (rooms_count_pkg_get != null)
                                {
                                    if ((Protocol.BaseHeader)rooms_count_pkg_get.Value.Header == Protocol.BaseHeader.msg_ulong && rooms_count_pkg_get.Value.external_data != null)
                                    {
                                        uint roomCount = MsgExternalData.Decode.MsgULong(rooms_count_pkg_get.Value.external_data);

                                        while ((roomCount--) > 0)
                                        {
                                            Package[] roomPackage = new Package[6];
                                            for (int i = 0; i < roomPackage.Length; i++)
                                            {
                                                var pkg_get = SharedData.CurrentApp.N2NGOServerConnection.Receive();
                                                if (pkg_get == null || pkg_get.Value.external_data == null)
                                                    goto invalid;

                                                roomPackage[i] = pkg_get.Value;
                                            }

                                            string RoomCode;
                                            string RoomName;
                                            bool IsRoomPasswordNeeded;
                                            N2NGO_Core.Objects.RoomColor ColorMain;
                                            N2NGO_Core.Objects.RoomColor ColorMinor;
                                            uint MembersCount;
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
                                                IsRoomPasswordNeeded = MsgExternalData.Decode.MsgByte(edata) == 1;
                                            }
                                            {
                                                byte[]? edata = roomPackage[3].external_data;
                                                if ((Protocol.BaseHeader)roomPackage[3].Header != Protocol.BaseHeader.msg_ulong || edata == null)
                                                    goto invalid;
                                                ColorMain = new N2NGO_Core.Objects.RoomColor(MsgExternalData.Decode.MsgULong(edata));
                                            }
                                            {
                                                byte[]? edata = roomPackage[4].external_data;
                                                if ((Protocol.BaseHeader)roomPackage[4].Header != Protocol.BaseHeader.msg_ulong || edata == null)
                                                    goto invalid;
                                                ColorMinor = new N2NGO_Core.Objects.RoomColor(MsgExternalData.Decode.MsgULong(edata));
                                            }
                                            {
                                                byte[]? edata = roomPackage[5].external_data;
                                                if ((Protocol.BaseHeader)roomPackage[5].Header != Protocol.BaseHeader.msg_ulong || edata == null)
                                                    goto invalid;
                                                MembersCount = MsgExternalData.Decode.MsgULong(edata);
                                            }

                                            // Random Color
                                            //byte[] rgb = new byte[3];
                                            //random.NextBytes(rgb);
                                            //Dispatcher.InvokeAsync(() => cards.Add(new RoomCardModel { RoomName = RoomName, RoomCode = RoomCode, IsRoomPasswordNeeded = IsRoomPasswordNeeded, ThemeBrushMinor = new SolidColorBrush(Color.FromArgb(0xFF, rgb[0], rgb[1], rgb[2])) }));

                                            Dispatcher.Invoke(() => cards.Add(new RoomsPageRoomCardModel
                                            {
                                                RoomName = RoomName,
                                                RoomCode = RoomCode,
                                                IsRoomPasswordNeeded = IsRoomPasswordNeeded,
                                                ThemeBrushMain = new SolidColorBrush(Color.FromArgb(0xFF, ColorMain.R, ColorMain.G, ColorMain.B)),
                                                ThemeBrushMinor = new SolidColorBrush(Color.FromArgb(0xFF, ColorMinor.R, ColorMinor.G, ColorMinor.B)),
                                                MembersCount = MembersCount
                                            }));
                                        }
                                        ;

                                        goto done;
                                    }
                                }
                            invalid:
                                SharedData.CurrentApp.Dispatcher.InvokeAsync(() => SharedData.CurrentApp.MainView.DoMessageDialog("无效的N2N GO 服务器协议"));
                            end:
                                DialogOut(DialogLoadingRooms, Dispatcher);
                                return;
                            }
                            catch (Exception ex)
                            {
                                SharedData.CurrentApp.Dispatcher.InvokeAsync(() => SharedData.CurrentApp.MainView.DoMessageDialog($"在刷新房间列表时发生异常：{ex}"));
                                DialogOut(DialogLoadingRooms, Dispatcher);
                                return;
                            }
                        done:
                        Dispatcher.InvokeAsync(() =>
                        {
                            Filter_Sort();

                            DialogOut(DialogLoadingRooms, Dispatcher);
                        });
                    });

                    refreshThread.Start();
                });

                sw.Stop();
                Dispatcher.InvokeAsync(() => Console.WriteLine($"[Rooms Refresh] Total Time: {sw.ElapsedMilliseconds}ms({sw.ElapsedTicks}ticks)"));
            });
        }

        public async void Refresh(string search)
        {
            if (refreshThread != null && refreshThread.IsAlive)
                return;

            bool? isRefreshing = null;
            Dispatcher.Invoke(() => isRefreshing = IsRefreshing);
            if (isRefreshing == true)
                return;

            cards.Clear();

            DialogLoadingRooms.Opacity = 0;
            DialogLoadingRooms.Visibility = Visibility.Visible;
            TaskCompletionSource<object> animationCompletedTask = new();
            DoubleAnimation _loadingDialogFadeIn = new() { To = 1, Duration = TimeSpan.FromSeconds(0.20), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            _loadingDialogFadeIn.Completed += (s, _) =>
            {
                animationCompletedTask.SetResult(0);
            };
            Dispatcher.Invoke(() => DialogLoadingRooms.BeginAnimation(OpacityProperty, _loadingDialogFadeIn));

            await Task.Run(() =>
            {
                animationCompletedTask.Task.Wait();

                Stopwatch sw = Stopwatch.StartNew();

                Dispatcher.Invoke(() =>
                {
                    Random random = new(DateTime.Now.Millisecond);
                    refreshThread = new Thread(() =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            IsRefreshing = true;
                        });

                        lock (SharedData.CurrentApp.N2NGOServerConnection)
                            try
                            {
                                if (!SharedData.CurrentApp.N2NGOServerConnection.Peek())
                                    goto end;

                                SharedData.CurrentApp.N2NGOServerConnection.Send(MakePackage(Protocol.BaseHeader._rooms_pull_rooms_searched));
                                SharedData.CurrentApp.N2NGOServerConnection.Send(MakePackage(Protocol.BaseHeader.msg_string_long, MsgExternalData.Encode.MsgStringLong(search)));

                                Package? rooms_count_pkg_get = SharedData.CurrentApp.N2NGOServerConnection.Receive();
                                if (rooms_count_pkg_get != null)
                                {
                                    if ((Protocol.BaseHeader)rooms_count_pkg_get.Value.Header == Protocol.BaseHeader.msg_ulong && rooms_count_pkg_get.Value.external_data != null)
                                    {
                                        uint roomCount = MsgExternalData.Decode.MsgULong(rooms_count_pkg_get.Value.external_data);

                                        while ((roomCount--) > 0)
                                        {
                                            Package[] roomPackage = new Package[6];
                                            for (int i = 0; i < roomPackage.Length; i++)
                                            {
                                                var pkg_get = SharedData.CurrentApp.N2NGOServerConnection.Receive();
                                                if (pkg_get == null || pkg_get.Value.external_data == null)
                                                    goto invalid;

                                                roomPackage[i] = pkg_get.Value;
                                            }

                                            string RoomCode;
                                            string RoomName;
                                            bool IsRoomPasswordNeeded;
                                            N2NGO_Core.Objects.RoomColor ColorMain;
                                            N2NGO_Core.Objects.RoomColor ColorMinor;
                                            uint MembersCount;
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
                                                IsRoomPasswordNeeded = MsgExternalData.Decode.MsgByte(edata) == 1;
                                            }
                                            {
                                                byte[]? edata = roomPackage[3].external_data;
                                                if ((Protocol.BaseHeader)roomPackage[3].Header != Protocol.BaseHeader.msg_ulong || edata == null)
                                                    goto invalid;
                                                ColorMain = new N2NGO_Core.Objects.RoomColor(MsgExternalData.Decode.MsgULong(edata));
                                            }
                                            {
                                                byte[]? edata = roomPackage[4].external_data;
                                                if ((Protocol.BaseHeader)roomPackage[4].Header != Protocol.BaseHeader.msg_ulong || edata == null)
                                                    goto invalid;
                                                ColorMinor = new N2NGO_Core.Objects.RoomColor(MsgExternalData.Decode.MsgULong(edata));
                                            }
                                            {
                                                byte[]? edata = roomPackage[5].external_data;
                                                if ((Protocol.BaseHeader)roomPackage[5].Header != Protocol.BaseHeader.msg_ulong || edata == null)
                                                    goto invalid;
                                                MembersCount = MsgExternalData.Decode.MsgULong(edata);
                                            }

                                            // Random Color
                                            //byte[] rgb = new byte[3];
                                            //random.NextBytes(rgb);
                                            //Dispatcher.InvokeAsync(() => cards.Add(new RoomCardModel { RoomName = RoomName, RoomCode = RoomCode, IsRoomPasswordNeeded = IsRoomPasswordNeeded, ThemeBrushMinor = new SolidColorBrush(Color.FromArgb(0xFF, rgb[0], rgb[1], rgb[2])) }));

                                            Dispatcher.Invoke(() => cards.Add(new RoomsPageRoomCardModel
                                            {
                                                RoomName = RoomName,
                                                RoomCode = RoomCode,
                                                IsRoomPasswordNeeded = IsRoomPasswordNeeded,
                                                ThemeBrushMain = new SolidColorBrush(Color.FromArgb(0xFF, ColorMain.R, ColorMain.G, ColorMain.B)),
                                                ThemeBrushMinor = new SolidColorBrush(Color.FromArgb(0xFF, ColorMinor.R, ColorMinor.G, ColorMinor.B)),
                                                MembersCount = MembersCount
                                            }));
                                        }
                                        ;

                                        goto done;
                                    }
                                }
                            invalid:
                                SharedData.CurrentApp.Dispatcher.InvokeAsync(() => SharedData.CurrentApp.MainView.DoMessageDialog("无效的N2N GO 服务器协议"));
                            end:
                                DialogOut(DialogLoadingRooms, Dispatcher);
                                return;
                            }
                            catch (Exception ex)
                            {
                                SharedData.CurrentApp.Dispatcher.InvokeAsync(() => SharedData.CurrentApp.MainView.DoMessageDialog($"在刷新房间列表时发生异常：{ex}"));
                                DialogOut(DialogLoadingRooms, Dispatcher);
                                return;
                            }
                        done:
                        Dispatcher.InvokeAsync(() =>
                        {
                            Filter_Sort();

                            DialogOut(DialogLoadingRooms, Dispatcher);
                        });
                    });

                    refreshThread.Start();
                });

                sw.Stop();
                Dispatcher.InvokeAsync(() => Console.WriteLine($"[Rooms Refresh] Total Time: {sw.ElapsedMilliseconds}ms({sw.ElapsedTicks}ticks)"));
            });
        }

        private void TempGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var grid = ((Grid)sender);
            if (grid.Parent is not Border gridp)
                throw new NullReferenceException(nameof(gridp));

            if (grid.Children[0] is Canvas canvasBackground)
                canvasBackground.BeginAnimation(Canvas.OpacityProperty, new DoubleAnimation(0.7, TimeSpan.FromSeconds(0.2)));

            TransformGroup TG = (TransformGroup)gridp.RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];

            st.BeginAnimation(ScaleTransform.ScaleXProperty, smallerAnimation);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, smallerAnimation);

            //Button btn1 = (Button)((Grid)sender).Children[2];
            //btn1.BeginAnimation(OpacityProperty, joinBtnFadeIn);
            //btn.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, joinBtnLandIn);
            // btn.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, joinBtnLandVIn);
        }
        private void TempGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var grid = ((Grid)sender);
            if (grid.Parent is not Border gridp)
                throw new NullReferenceException(nameof(gridp));

            if (grid.Children[0] is Canvas canvasBackground)
                canvasBackground.BeginAnimation(Canvas.OpacityProperty, new DoubleAnimation(0.37, TimeSpan.FromSeconds(0.28)));

            TransformGroup TG = (TransformGroup)gridp.RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];

            st.BeginAnimation(ScaleTransform.ScaleXProperty, biggerAnimation);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, biggerAnimation);

            //Button btn1 = (Button)((Grid)sender).Children[2];
            //btn1.BeginAnimation(OpacityProperty, joinBtnFadeOut);
            //btn.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, joinBtnLandOut);
            // btn.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, joinBtnLandOut);
        }
        private void TempGrid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var grid = ((Grid)sender);
            if (grid.Parent is not Border gridp)
                throw new NullReferenceException(nameof(gridp));

            TransformGroup TG = (TransformGroup)gridp.RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];

            st.BeginAnimation(ScaleTransform.ScaleXProperty, smallsmallerAnimation);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, smallsmallerAnimation);
        }
        private void TempGrid_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var grid = ((Grid)sender);
            if (grid.Parent is not Border gridp)
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
            CardDialogBorderFrame.Navigate(new Dialogs.DialogRoomInfo((RoomsPageRoomCardModel)(((Grid)sender).DataContext), async (_, __) =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    DialogJoiningRoom.Opacity = 0;
                    DialogJoiningRoom.Visibility = Visibility.Visible;

                    TaskCompletionSource<object> animationCompletedTask = new();
                    DoubleAnimation _loadingDialogFadeIn = new() { To = 1, Duration = TimeSpan.FromSeconds(0.20), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
                    _loadingDialogFadeIn.Completed += (s, _) =>
                    {
                        animationCompletedTask.SetResult(0);
                    };
                    DialogJoiningRoom.BeginAnimation(OpacityProperty, _loadingDialogFadeIn);


                });

                SharedData.CurrentApp.LeaveRoom();

                RoomsPageRoomCardModel card = (RoomsPageRoomCardModel)((Grid)sender).DataContext;

                string roomCode = card.RoomCode;
                string roomPassword = (card.IsRoomPasswordNeeded ? ((DialogRoomInfo)CardDialogBorderFrame.Content).TextBoxPasswd.Text : SharedData.DefaultRoomPassword).Trim();

                if (card.IsRoomPasswordNeeded && string.IsNullOrEmpty(roomPassword))
                {
                    await SharedData.CurrentApp.Dispatcher.InvokeAsync(() => SharedData.CurrentApp.MainView.DoMessageDialog("密码不能为空！", "进入房间失败"));
                    DialogOut(DialogJoiningRoom, Dispatcher);
                    return;
                }

                string cmd = SharedData.EdgePath + " -c " + roomCode + " -k " + roomPassword + " -l " + $"{SharedData.CurrentApp.N2NGOServerConnection.ServerIPEndPoint.Address}:{SharedData.CurrentApp.N2NGOServerConnection.ServerSupernodePort}";

                timer.Stop();
                timer = new();
                timer.Tick += async (_, __) =>
                {
                    if (SharedData.N2NEdgeLogHelper.IsEdgeConnectedToSupernode(SharedData.CurrentApp.N2NExecLog.LogOut))
                    {
                        timer.Stop();

                        if (string.IsNullOrEmpty(roomCode))
                        {
                            await SharedData.CurrentApp.Dispatcher.InvokeAsync(() => SharedData.CurrentApp.MainView.DoMessageDialog("未获取到房间号", "进入房间失败"));
                            SharedData.CurrentApp.LeaveRoom();
                        }
                        else
                        {
                            var enterRoomResult = await Task.Run(() => SharedData.CurrentApp.EnterRoom(roomCode, roomPassword));
                            if (!enterRoomResult)
                            {
                                await SharedData.CurrentApp.Dispatcher.InvokeAsync(() => SharedData.CurrentApp.MainView.DoMessageDialog("进入房间失败", "进入房间失败"));
                            }
                        }

                        DialogOut(DialogJoiningRoom, Dispatcher);
                    }
                };
                timer.Start();


                if (await SharedData.CurrentApp.N2NExecLog.ExecuteAsync(cmd, true) is null)
                {
                    SharedData.CurrentApp.Dispatcher.Invoke(() => SharedData.CurrentApp.MainView.DoMessageDialog("您当前仍有其他正在进入房间的任务，请查看日志", "进入房间终止"));

                    timer.Stop();
                    DialogOut(DialogJoiningRoom, Dispatcher);
                    return;
                }

                if (SharedData.CurrentApp.N2NExecLog.LogOut.Contains("No Windows tap devices found, did you run tapinstall.exe?"))
                {
                    // No Windows tap
                    await SharedData.CurrentApp.Dispatcher.InvokeAsync(() => SharedData.CurrentApp.MainView.DoMessageYesNoDialog("我们无法找到可用的Tap设备，请检查并安装Tap虚拟网卡驱动，要现在安装吗？", "进入房间失败",
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
                    timer.Stop();
                }
                timer.Stop();

                DialogOut(DialogJoiningRoom, Dispatcher);
            }, (_, __) => HideCardInfoDialog()));

            ShowCardInfoDialog();
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void ButtonCancelLoadingRooms_Click(object sender, RoutedEventArgs e)
        {
            DialogOut(DialogLoadingRooms, ((MainView)App.Current.MainWindow).Dispatcher);
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

        private void RoomsSearching_SearchStarted(object sender, HandyControl.Data.FunctionEventArgs<string> e)
        {
            Refresh(e.Info);
        }
    }

    public class RoomsPageRoomCardModel
    {
        public bool IsFunctionButton { get; set; } = false;

        public bool TransformInited = false;

        public Brush ThemeBrushMain { get; set; } = new SolidColorBrush(Colors.Red);
        public Brush ThemeBrushMinor { get; set; } = new SolidColorBrush(Color.FromRgb(0xB3, 0xB3, 0xB3));

        public string RoomName { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;
        public bool IsRoomVisible { get; set; } = true;
        public bool IsRoomPasswordNeeded { get; set; } = false;
        public uint MembersCount { get; set; } = 0;
    }
}


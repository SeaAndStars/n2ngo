using N2NGO.UtilsClass;
using N2NGO.Views.SubPages.Dialogs;
using N2NGO.Views.SubPages.Dialogs.MessageDialogs;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

namespace N2NGO.Views.SubPages
{
    /// <summary>
    /// IndexPage.xaml 的交互逻辑
    /// </summary>
    public partial class IndexPage : BaseIndexPage
    {
        public readonly Index_CommunityPage CommunityIndexPage = new();
        public readonly Index_HostingPage HostingIndexPage = new();
        public readonly Index_ClientingPage ClientingIndexPage = new();
        public readonly Index_AdvancedPage AdvancedIndexPage = new();
        public readonly Index_AboutPage AboutIndexPage = new();

        public readonly Index_StoragePage StorageIndexPage = new();

        private readonly DispatcherTimer dispatcherTimerCurrentServerIndexerUpdater;
        private readonly DispatcherTimer dispatcherTimerCurrentRoomIndexerUpdater;
        private readonly DispatcherTimer dispatcherTimerCurrentUserIndexerUpdater;

        public IndexPage()
        {
            InitializeComponent();

            dispatcherTimerCurrentServerIndexerUpdater = new() { Interval = TimeSpan.FromSeconds(1.5) };
            dispatcherTimerCurrentServerIndexerUpdater.Tick += DispatcherTimerCurrentServerIndexerUpdater_Tick;
            dispatcherTimerCurrentServerIndexerUpdater.Start();

            dispatcherTimerCurrentRoomIndexerUpdater = new() { Interval = TimeSpan.FromSeconds(1.5) };
            dispatcherTimerCurrentRoomIndexerUpdater.Tick += DispatcherTimerCurrentRoomIndexerUpdater_Tick; ;
            dispatcherTimerCurrentRoomIndexerUpdater.Start();

            dispatcherTimerCurrentUserIndexerUpdater = new() { Interval = TimeSpan.FromSeconds(1) };
            dispatcherTimerCurrentUserIndexerUpdater.Tick += DispatcherTimerCurrentUserIndexerUpdater_Tick; ; ;
            dispatcherTimerCurrentUserIndexerUpdater.Start();
        }
        ~IndexPage()
        {
            dispatcherTimerCurrentServerIndexerUpdater.Stop();
            dispatcherTimerCurrentRoomIndexerUpdater.Stop();
            dispatcherTimerCurrentUserIndexerUpdater.Stop();
        }

        private void DispatcherTimerCurrentServerIndexerUpdater_Tick(object? sender, EventArgs e)
        {
            try { CurrentServerGlobalAddress.Text = $"{(SharedData.CurrentApp.N2NGOServerConnection.Client.Client.RemoteEndPoint ?? throw new NullReferenceException(nameof(SharedData.CurrentApp.N2NGOServerConnection.Client.Client.RemoteEndPoint))) as IPEndPoint}"; }
            catch (Exception) { }
            CurrentServerSupernodeAddress.Text = $"{SharedData.CurrentApp.N2NGOServerConnection.ServerIPEndPoint.Address}:{SharedData.CurrentApp.N2NGOServerConnection.ServerSupernodePort}";
            Task.Run(() =>
            {
                var connected = SharedData.CurrentApp.N2NGOServerConnection.IsConnected();
                Dispatcher.Invoke(() => CurrentServerStatus.SetResourceReference(Run.TextProperty, connected ? "LOCALE_Status_Connected" : "LOCALE_Status_Disconnected"));

                if (!SharedData.CurrentApp.N2NGOServerConnection.Peek(out var latency))
                    return;
                Dispatcher.Invoke(() => CurrentServerLatency.Text = $"{latency.Milliseconds}ms");

                var onlines = SharedData.CurrentApp.N2NGOServerConnection.PullTotalOnlines();
                if (onlines.IsSuccessfulStatusCode) Dispatcher.Invoke(() => CurrentServerOnlines.Text = $"{onlines.Value}");
                else Dispatcher.Invoke(() => CurrentServerOnlines.Text = $"{onlines.Status}");
            });
        }

        private async void DispatcherTimerCurrentRoomIndexerUpdater_Tick(object? sender, EventArgs e)
        {
            var isConnectedToRoom = SharedData.CurrentApp.RoomConnection.IsConnected;
            N2NGO_Core.Models.Room room = new() { RoomCode = null, RoomName = null, IsRoomInvisible = null, IsRoomPasswordNeeded = null, MainColor = null, MinorColor = null };

            var _currentCode = SharedData.CurrentApp.RoomConnection.CurrentRoomCode;
            if (_currentCode != string.Empty)
            {
                var roomPullResult = await Task.Run(() => SharedData.CurrentApp.N2NGOServerConnection.GetRoomByCode(_currentCode));
                if (roomPullResult.IsSuccessfulStatusCode)
                {
                    if ((roomPullResult.Value != null) && isConnectedToRoom)
                    {
                        var roomPull = roomPullResult.Value;

                        if (roomPull.RoomCode != null)
                            room.RoomCode = roomPull.RoomCode;
                        if (roomPull.RoomName != null)
                            room.RoomName = roomPull.RoomName;
                        if (roomPull.IsRoomInvisible != null)
                            room.IsRoomInvisible = roomPull.IsRoomInvisible;
                        if (roomPull.IsRoomPasswordNeeded != null)
                            room.IsRoomPasswordNeeded = roomPull.IsRoomPasswordNeeded;
                        if (roomPull.MainColor != null)
                            room.MainColor = roomPull.MainColor;
                        if (roomPull.MinorColor != null)
                            room.MinorColor = roomPull.MinorColor;

                        room.CB = roomPull.CB;
                        room.AccessMode = roomPull.AccessMode;
                    }
                }
                else
                {
                    SharedData.CurrentApp.LeaveRoom();
                }
            }

            CurrentRoomStatus.SetResourceReference(Run.TextProperty, isConnectedToRoom ? "LOCALE_Status_Connected" : "LOCALE_Status_Disconnected");

            CurrentRoomCode.Text = room.RoomCode ?? "null";
            CurrentRoomName.Text = room.RoomName ?? "null";
            CurrentRoomPasswordNeeded.SetResourceReference(Run.TextProperty, (room.IsRoomPasswordNeeded == null) ? "LOCALE_Unknown" : room.IsRoomPasswordNeeded.Value ? "LOCALE_CurrentRoomPasswordNeeded_Yes" : "LOCALE_CurrentRoomPasswordNeeded_No");
            CurrentRoomVisibility.SetResourceReference(Run.TextProperty, (room.IsRoomInvisible == null) ? "LOCALE_Unknown" : room.IsRoomInvisible.Value ? "LOCALE_CurrentRoomVisibility_Invisible" : "LOCALE_CurrentRoomVisibility_Visible");
            
            if (isConnectedToRoom)
            {
                var edgeDeviceAllocation = SharedData.N2NEdgeOutputHelper.GetEdgeDeviceAllocation(SharedData.CurrentApp.EdgeN2NExecutor.GetOutput);
                CurrentRoomIpAddress.Text = edgeDeviceAllocation[SharedData.N2NEdgeOutputHelper.EdgeDeviceAllocating.IP] ?? "...";
            }
            else
            {
                CurrentRoomIpAddress.SetResourceReference(Run.TextProperty, "LOCALE_Unknown");
            }

            SharedData.CurrentApp.MainWindow.PageRoom.UpdateRoomInfoWithLocal(room);
        }

        private void DispatcherTimerCurrentUserIndexerUpdater_Tick(object? sender, EventArgs e)
        {
            CurrentUserNickname.Text = SharedData.CurrentApp.Config.Get("UserNickname");
        }


        protected override void Indexer_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Click
            var indexer = (Indexer)((Border)sender).DataContext;

            switch (indexer.NagivKey)
            {
                default: break;

                case "SetUserNickname":
                    {
                        SharedData.CurrentApp.MainWindow.DoMessageInputDialog("@LOCALE_DialogEditNickname_Content", "@LOCALE_DialogEditNickname_Title"
                            , new List<Action<object>> {
                    (_)=> {
                        if (_ is not DialogMessage dialogMessage)
                            throw new ArgumentException("Argument is not DialogMessage", nameof(_));

                        if (dialogMessage.MessageContent is not DialogInput dialogInput)
                            throw new Exception("dialogMessage.MessageContent is not DialogInput");

                        SharedData.CurrentApp.Config.Set("UserNickname",  dialogInput.InputBox.Text);
                    } },

                            (_) =>
                            {
                                if (_ is not DialogMessage dialogMessage)
                                    throw new ArgumentException("Argument is not DialogMessage", nameof(_));

                                if (dialogMessage.MessageContent is not DialogInput dialogInput)
                                    throw new Exception("dialogMessage.MessageContent is not DialogInput");

                                dialogInput.InputBox.Text = SharedData.CurrentApp.Config.Get("UserNickname");
                                dialogInput.InputBox.SelectAll();
                            });

                        break;
                    }

                case "ResetConnection":
                    {
                        SharedData.CurrentApp.MainWindow.DoMessageYesNoDialog("@LOCALE_DialogResetConnectionDialog_Content", "@LOCALE_DialogResetConnectionDialog_Title",
                        new()
                        {
                            (_) =>
                            {
                                if (_ is not DialogMessage dialogMessage || dialogMessage.MessageContent is not Dialogs.MessageDialogs.DialogYesNo dialogYesNo)
                                    throw new Exception("Cannot get DialogYesNo");

                                if (dialogYesNo.YesNo == Dialogs.MessageDialogs.DialogYesNo.YesNoE.Yes)
                                    Task.Run(()=>SharedData.CurrentApp.ResetConnection(true));
                            }
                        }
                    );
                        break;
                    }

                case "EnterRoom":
                    {
                        if (!SharedData.CurrentApp.RoomConnection.IsConnected)
                        {
                            SharedData.CurrentApp.MainWindow.DoMessageDialog("@LOCALE_DialogEnterRoom_Fail_Not_Connected_Content", "@LOCALE_DialogEnterRoom_Title");
                            break;
                        }
                        NavigatePage(SharedData.CurrentApp.MainWindow.PageRoom);
                        break;
                    }

                case "Community":
                    {
                        NavigateIndexPage(CommunityIndexPage);
                        break;
                    }

                case "Hosting":
                    {
                        NavigateIndexPage(HostingIndexPage);
                        break;
                    }

                case "Clienting":
                    {
                        NavigateIndexPage(ClientingIndexPage);
                        break;
                    }

                case "Advanced":
                    {
                        NavigateIndexPage(AdvancedIndexPage);
                        break;
                    }

                case "About":
                    {
                        NavigateIndexPage(AboutIndexPage);
                        break;
                    }
            }
        }

        private bool _navigateScroller = true;
        protected override void Page_Loaded(object sender, RoutedEventArgs e)
        {
            base.Page_Loaded(sender, e);

            if (_navigateScroller)
            {
                var targetPosition = ExtendIndexer1.TransformToVisual(IndexersScrollViewer).Transform(new());
                IndexersScrollViewer.ScrollToHorizontalOffsetWithAnimation(targetPosition.X - 50, 2500);
                _navigateScroller = false;

            }
        }

        private void ButtonViewKey_Click(object sender, RoutedEventArgs e)
        {
            SharedData.CurrentApp.MainWindow.DoMessageYesNoDialog("@LOCALE_DialogViewUserKey_Description_Content", "@LOCALE_DialogViewUserKey_Title",
                        new()
                        {
                            (_) =>
                            {
                                if (_ is not DialogMessage dialogMessage || dialogMessage.MessageContent is not Dialogs.MessageDialogs.DialogYesNo dialogYesNo)
                                    throw new Exception("Cannot get DialogYesNo");

                                if (dialogYesNo.YesNo == Dialogs.MessageDialogs.DialogYesNo.YesNoE.Yes)
                                    Task.Run(()=>
                                    {
                                        var usrKeyResult = SharedData.CurrentApp.N2NGOServerConnection.GetUserKey();
                                        if (usrKeyResult.IsSuccessfulStatusCode)
                                        {
                                            if (usrKeyResult.Value is not null)
                                                Dispatcher.Invoke(()=>SharedData.CurrentApp.MainWindow.DoMessageDialog(usrKeyResult.Value, "@LOCALE_DialogViewUserKey_Title"));
                                        }
                                    });
                            }
                        }
                    );
        }
    }
}

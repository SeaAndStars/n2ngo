using HandyControl.Tools.Extension;
using N2NGO.UtilsClass;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace N2NGO.Views.SubPages
{
    /// <summary>
    /// RoomPage.xaml 的交互逻辑
    /// </summary>
    public partial class RoomPage : Page
    {
        private readonly DoubleAnimation _slideOutAnimation = new() { To = 0, Duration = TimeSpan.FromSeconds(0.5), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        private readonly DoubleAnimation _fadeInAnimation = new() { To = 1, Duration = TimeSpan.FromSeconds(0.3), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        private readonly DoubleAnimation _fadeOutAnimation = new() { To = 0, Duration = TimeSpan.FromSeconds(0.3), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };

        private readonly DispatcherTimer _refreshTimer;

        private Task? _refreshTask;

        public RoomPage()
        {
            InitializeComponent();

            AdminMembersList.Items.Clear();
            AdminRuledMembersList.Items.Clear();
            ItemsControl_Members.Items.Clear();

            _refreshTimer = new()
            {
                Interval = TimeSpan.FromSeconds(8)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;

            SwitchAdminPanel();
        }

        /// <summary>
        /// Push current details and pull others' from room
        /// </summary>
        private void Refresh()
        {
            if (!SharedData.CurrentApp.RoomConnection.IsConnected || SharedData.CurrentApp.RoomConnection.CurrentRoomCode == string.Empty)
            {
                return;
            }

            try
            {
                var edgeDeviceAllocation = SharedData.N2NEdgeOutputHelper.GetEdgeDeviceAllocation(SharedData.CurrentApp.EdgeN2NExecutor.GetOutput);

                var CurrentUser_UserNickname = SharedData.CurrentApp.Config.Get("UserNickname") ?? "null_local";
                var CurrentUser_IpAddress = edgeDeviceAllocation[SharedData.N2NEdgeOutputHelper.EdgeDeviceAllocating.IP] ?? "...";
                SharedData.CurrentApp.N2NGOServerConnection.MemberPush(CurrentUser_UserNickname, CurrentUser_IpAddress);

                var members = SharedData.CurrentApp.N2NGOServerConnection.MemberPull();

                if ((!members.IsSuccessfulStatusCode) || (members.Value is null))
                    throw new($"MemberPull fails with {members.Status}");

                List<RoomPageMemberModel> modelMembers = new();
                List<RoomPageRuledMemberModel> modelRuledMembers = new();

                bool imAdmin = false;
                foreach (var member in members.Value.Item1)
                {
                    var _member = new RoomPageMemberModel() { IsAdmin = member.IsAdmin ?? throw new("member.IsAdmin null"), ID = member.ID, Nickname = member.Nickname, IpAddress = member.IpAddress };

                    var ping = new System.Net.NetworkInformation.Ping();
                    try
                    {
                        var pingTest = ping.Send(member.IpAddress, 1000);

                        _member.PingStatus = pingTest.Status;
                        if (pingTest.Status == IPStatus.Success)
                        {
                            _member.PingLatency = $"{pingTest.RoundtripTime}ms";
                        }
                        else
                        {
                            _member.PingLatency = $"{pingTest.Status}";
                        }
                    }
                    catch
                    {
                        _member.PingLatency = "-1";
                    }
                    finally
                    {
                        ping.Dispose();
                    }
                    
                    modelMembers.Add(_member);

                    // Check permission
                    if (member.IsAdmin.Value && (member.ID == SharedData.CurrentApp.RoomConnection.MemberID))
                        imAdmin = true;
                }
                foreach (var ruledMember in members.Value.Item2)
                {
                    modelRuledMembers.Add(new() { RuledMemberBehaviour = ruledMember.Behaviour, ID = ruledMember.ID, Nickname = ruledMember.Nickname, IpAddress = ruledMember.IpAddress });
                }

                Dispatcher.Invoke(() =>
                {
                    if (imAdmin)
                    {
                        ButtonSwitchAdminPanel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ButtonSwitchAdminPanel.Visibility = Visibility.Collapsed;
                        SwitchAdminPanel(false);
                    }
                });

                Dispatcher.Invoke(() =>
                {
                    ItemsControl_Members.ItemsSource = modelMembers;
                    AdminMembersList.ItemsSource = modelMembers;
                    AdminRuledMembersList.ItemsSource = modelRuledMembers;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.InvokeAsync(() => SharedData.CurrentApp.Log.WriteLine($"An Exception occurred while pulling room data: {ex.Message}", SharedData.CurrentApp.Log.Module.MainWindow_RoomPage));
            }
        }

        public void InvokeRefresh()
        {
            if ((_refreshTask is not null) && (!_refreshTask.IsCompleted))
                return;

            _refreshTask = new(Refresh);
            _refreshTask.Start();
        }

        public void BeginRefresh()
        {
            _refreshTimer.IsEnabled = true;
            InvokeRefresh();
        }

        public void EndRefresh()
        {
            _refreshTimer.IsEnabled = false;
            _refreshTimer.Stop();
        }

        public void UpdateRoomInfoWithLocal(N2NGOCore.Models.Room roomInfo)
        {
            var isConnectedToRoom = SharedData.CurrentApp.RoomConnection.IsConnected;

            CurrentRoomCode.Text = roomInfo.RoomCode ?? "null";
            CurrentRoomName.Text = roomInfo.RoomName ?? "null";
            CurrentRoomPasswordNeeded.SetResourceReference(Run.TextProperty, (roomInfo.IsRoomPasswordNeeded == null) ? "LOCALE_Unknown" : roomInfo.IsRoomPasswordNeeded.Value ? "LOCALE_CurrentRoomPasswordNeeded_Yes" : "LOCALE_CurrentRoomPasswordNeeded_No");
            CurrentRoomVisibility.SetResourceReference(Run.TextProperty, (roomInfo.IsRoomInvisible == null) ? "LOCALE_Unknown" : roomInfo.IsRoomInvisible.Value ? "LOCALE_CurrentRoomVisibility_Invisible" : "LOCALE_CurrentRoomVisibility_Visible");

            if (isConnectedToRoom)
            {
                var edgeDeviceAllocation = SharedData.N2NEdgeOutputHelper.GetEdgeDeviceAllocation(SharedData.CurrentApp.EdgeN2NExecutor.GetOutput);
                CurrentRoomIpAddress.Text = edgeDeviceAllocation[SharedData.N2NEdgeOutputHelper.EdgeDeviceAllocating.IP] ?? "...";
            }
            else
            {
                CurrentRoomIpAddress.SetResourceReference(Run.TextProperty, "LOCALE_Unknown");
            }

            LifetimeValueText.Text = roomInfo.CB.ActivatedLifetime.ToString();
            ProgressBarRoomLifetime.Value = roomInfo.CB.ActivatedLifetime.TotalMinutes / 10 * 100;
        }

        private async void ButtonKnown_Click(object sender, RoutedEventArgs e)
        {
            _slideOutAnimation.From = tip.GetValidWidth();
            _slideOutAnimation.To = 0;

            tip.BeginAnimation(WidthProperty, _slideOutAnimation);
            tip.BeginAnimation(OpacityProperty, _fadeOutAnimation);

            await Task.Run(() =>
            {
                Thread.Sleep(1500);

                Dispatcher.Invoke(() =>
                {
                    tip.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            InvokeRefresh();
        }

        private async void ButtonExitRoom_Click(object? sender, RoutedEventArgs? e)
        {
            await SharedData.CurrentApp.LeaveRoom();
            SharedData.CurrentApp.MainWindow.NavigatePage(null);
        }

        private void IpAddressButton_Initialized(object sender, EventArgs e)
        {
            if (sender is Button button)
                SharedData.UIAnimation.InitButton(button);
        }

        private void MemberCard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is not Border border)
                return;
            if (border.Child is not Grid grid)
                return;
            if (grid.Name is not "MemberRootGrid")
                return;
            foreach (var item0 in grid.Children)
            {
                if (item0 is Grid subGrid && subGrid.Name == "MemberSubGrid")
                {
                    foreach (var item1 in subGrid.Children)
                    {
                        if (item1 is FrameworkElement label && label.Name == "NicknameLabel")
                        {
                            label.BeginAnimation(FrameworkElement.OpacityProperty, _fadeOutAnimation);
                            break;
                        }
                    }
                    break;
                }
            }
        }

        private void MemberCard_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is not Border border)
                return;
            if (border.Child is not Grid grid)
                return;
            if (grid.Name is not "MemberRootGrid")
                return;
            foreach (var item0 in grid.Children)
            {
                if (item0 is Grid subGrid && subGrid.Name == "MemberSubGrid")
                {
                    foreach (var item1 in subGrid.Children)
                    {
                        if (item1 is FrameworkElement label && label.Name == "NicknameLabel")
                        {
                            label.BeginAnimation(FrameworkElement.OpacityProperty, _fadeInAnimation);
                            break;
                        }
                    }
                    break;
                }
            }
        }

        private void IpAddressButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;
            if (button.DataContext is not RoomPageMemberModel member)
                return;

            Clipboard.SetDataObject(member.IpAddress);

            string copiedTipSource;
            switch (member.PingStatus)
            {
                default:
                    {
                        copiedTipSource = "@LOCALE_DialogRoomMemberIpAddressCopied_Unknown_Content";
                        break;
                    }

                case IPStatus.Success:
                    {
                        copiedTipSource = "@LOCALE_DialogRoomMemberIpAddressCopied_Content";
                        break;
                    }
                case IPStatus.TimedOut:
                    {
                        copiedTipSource = "@LOCALE_DialogRoomMemberIpAddressCopied_TimedOut_Content"; 
                        break;
                    }
                //case IPStatus.DestinationNetworkUnreachable:
                //    {
                //        break;
                //    }
                //case IPStatus.DestinationHostUnreachable:
                //    {
                //        break;
                //    }
                //case IPStatus.DestinationProhibited:
                //    {
                //        break;
                //    }
                //case IPStatus.DestinationPortUnreachable:
                //    {
                //        break;
                //    }
                //case IPStatus.NoResources:
                //    {
                //        break;
                //    }
                //case IPStatus.BadOption:
                //    {
                //        break;
                //    }
                //case IPStatus.HardwareError:
                //    {
                //        break;
                //    }
                //case IPStatus.PacketTooBig:
                //    {
                //        break;
                //    }
                //case IPStatus.BadRoute:
                //    {
                //        break;
                //    }
                //case IPStatus.TtlExpired:
                //    {
                //        break;
                //    }
                //case IPStatus.TtlReassemblyTimeExceeded:
                //    {
                //        break;
                //    }
                //case IPStatus.ParameterProblem:
                //    {
                //        break;
                //    }
                //case IPStatus.SourceQuench:
                //    {
                //        break;
                //    }
                //case IPStatus.BadDestination:
                //    {
                //        break;
                //    }
                //case IPStatus.DestinationUnreachable:
                //    {
                //        break;
                //    }
                //case IPStatus.TimeExceeded:
                //    {
                //        break;
                //    }
                //case IPStatus.BadHeader:
                //    {
                //        break;
                //    }
                //case IPStatus.UnrecognizedNextHeader:
                //    {
                //        break;
                //    }
                //case IPStatus.IcmpError:
                //    {
                //        break;
                //    }
                //case IPStatus.DestinationScopeMismatch:
                //    {
                //        break;
                //    }
            }

            SharedData.CurrentApp.MainWindow.DoMessageDialog(copiedTipSource, "@LOCALE_DialogRoomMemberIpAddressCopied_Title");
        }

        private bool _showAdminPanel = true;
        public void SwitchAdminPanel(bool? show = null)
        {
            if (PanelAdminControl.LayoutTransform is not ScaleTransform scaleTransform)
                throw new("PanelAdminControl.LayoutTransform is not ScaleTransform");

            if (show is null)
                _showAdminPanel = !_showAdminPanel;
            else
            {
                if (_showAdminPanel == show.Value)
                    return;
                else
                    _showAdminPanel = show.Value;
            }

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation() { To = _showAdminPanel ? 1 : 0, Duration = TimeSpan.FromSeconds(0.128), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });
        }

        private void ButtonSwitchAdminPanel_Click(object sender, RoutedEventArgs e)
        {
            SwitchAdminPanel();
        }

        private void ButtonAdminCloseRoom_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => { SharedData.CurrentApp.N2NGOServerConnection.AdminCloseRoom(); Dispatcher.Invoke(() => ButtonExitRoom_Click(null, null)); });
        }

        private void ButtonAdminActivateRoom_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => SharedData.CurrentApp.N2NGOServerConnection.AdminActivateRoom());
        }

        private void ButtonAdminKick_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonAdminRuleMemberNone_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonAdminRuleMemberAllow_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonAdminRuleMemberPrevent_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public class RoomPageMemberModel
    {
        public bool IsAdmin { get; set; } = false;
        public string ID { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string PingLatency { get; set; } = "Unknown";

        public IPStatus PingStatus = IPStatus.Unknown;
    }
    public class RoomPageRuledMemberModel : RoomPageMemberModel
    {
        public N2NGOCore.Models.Room.RuledMember.MemberBehaviour RuledMemberBehaviour { get; set; } = N2NGOCore.Models.Room.RuledMember.MemberBehaviour.None;
    }
    public class IsAdminBoolToMemberImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isAdmin = (bool)value;
            string imagePath = isAdmin ? "AdminMember.png" : "Member.png";
            return new BitmapImage(new($"/Data/Images/{imagePath}", UriKind.Relative));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


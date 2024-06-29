using HandyControl.Tools.Extension;
using N2NGO.UtilsClass;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
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

            ItemsControl_Members.Items.Clear();

            _refreshTimer = new()
            {
                Interval = TimeSpan.FromSeconds(6)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;
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
                var edgeDeviceAllocation = SharedData.N2NEdgeLogHelper.GetEdgeDeviceAllocation(SharedData.CurrentApp.N2NExecLog.LogOut);

                var CurrentUser_UserNickname = SharedData.CurrentApp.Config.Get("UserNickname") ?? "null_local";
                var CurrentUser_IpAddress = edgeDeviceAllocation[SharedData.N2NEdgeLogHelper.EdgeDeviceAllocating.IP] ?? "...";
                SharedData.CurrentApp.N2NGOServerConnection.MemberPush(CurrentUser_UserNickname, CurrentUser_IpAddress);

                List<RoomPageMemberModel> modelMembers = new();
                var members = SharedData.CurrentApp.N2NGOServerConnection.MemberPull();
                if (members.IsSuccessfulStatusCode && members.Value != null)
                {
                    foreach (var member in members.Value)
                    {
                        modelMembers.Add(new() { Nickname = member.Nickname, IpAddress = member.IpAddress });
                    }

                    Dispatcher.Invoke(() => ItemsControl_Members.ItemsSource = modelMembers);
                }

                // Members.ItemsSource;
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Console.WriteLine($"An Exception occurred while pulling room data: {ex.Message}"));
            }
        }

        public void InvokeRefresh()
        {
            if ((_refreshTask is not null) && (!_refreshTask.IsCompleted))
                return;

            _refreshTask = new(Refresh);
            _refreshTask.Start();
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

        private async void ButtonExitRoom_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(()=> SharedData.CurrentApp.LeaveRoom());
            SharedData.CurrentApp.MainView.NavigatePage(null);

            EndRefresh();
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
                        if (item1 is Label label && label.Name == "NicknameLabel")
                        {
                            label.BeginAnimation(Label.OpacityProperty, _fadeOutAnimation);
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
                        if (item1 is Label label && label.Name == "NicknameLabel")
                        {
                            label.BeginAnimation(Label.OpacityProperty, _fadeInAnimation);
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
            SharedData.CurrentApp.MainView.DoMessageDialog("@LOCALE_DialogRoomMemberIpAddressCopied_Content", "@LOCALE_DialogRoomMemberIpAddressCopied_Title");
        }

        private void Button_Initialized(object sender, EventArgs e)
        {
            if (sender is Button button)
                SharedData.UIAnimation.InitButton(button);
        }
    }

    public class RoomPageMemberModel
    {
        public string Nickname { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
    }
}


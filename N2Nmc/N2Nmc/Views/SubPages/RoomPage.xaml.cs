using HandyControl.Tools.Extension;
using N2Nmc.UtilsClass;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using HandyControl.Controls;

namespace N2Nmc.Views.SubPages
{
    /// <summary>
    /// room.xaml 的交互逻辑
    /// </summary>
    public partial class RoomPage : Page
    {
        DoubleAnimation slideOutAnimation = new DoubleAnimation { From = 0, To = 0, Duration = TimeSpan.FromSeconds(0.5), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        DoubleAnimation fadeOutAnimationEx = new DoubleAnimation { From = 1, To = 0, Duration = TimeSpan.FromSeconds(0.3), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };

        DispatcherTimer refreshTimer = new DispatcherTimer();

        Thread RefreshTask;

        public RoomPage()
        {
            InitializeComponent();

            RefreshTask=new Thread(_Refresh);
            refreshTimer.Interval = TimeSpan.FromSeconds(4);
            refreshTimer.Tick += RefreshTimer_Tick;
            
        }

        public void EnterRoom(string node)
        {
            SharedData.EdgeConnectionInfo.IsConnectedToEdge = true;

            Refresh();
            refreshTimer.IsEnabled = true;
            refreshTimer.Start();
        }


        private void _Refresh()
        {
            try
            {


                // Members.ItemsSource;
            }
            catch (Exception ex)
            {
                Console.WriteLine("发生异常: " + ex.Message);
                HandyControl.Controls.Growl.WarningGlobal("发生异常: " + ex.Message);
            }
        }

        public void Refresh()
        {
            if (RefreshTask.IsAlive)
                return;

            RefreshTask = new Thread(_Refresh);
            RefreshTask.Start();
        }

        private async void ButtonKnown_Click(object sender, RoutedEventArgs e)
        {
            slideOutAnimation.From = tip.GetValidWidth();
            slideOutAnimation.To = 0;

            tip.BeginAnimation(WidthProperty, slideOutAnimation);
            tip.BeginAnimation(OpacityProperty, fadeOutAnimationEx);

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
            Refresh();
        }


        public void ExitRoom()
        {
            SharedData.ExitRoom();
            refreshTimer.IsEnabled = false;
            refreshTimer.Stop();
        }

        private void ButtonExitRoom_Click(object sender, RoutedEventArgs e)
        {
            SharedData.GetMainView.LeaveRoom();
        }
    }

    public class RoomMemberDataItem
    {
        public string? UserName { get; set; }
        public string? AddrIPv4 { get; set; }
    }
}


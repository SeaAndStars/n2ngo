using HandyControl.Tools.Extension;
using N2Nmc.UtilsClass;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Controls;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Documents;
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

        private string? node = null;

        public RoomPage()
        {
            InitializeComponent();

            RefreshTask=new Thread(_Refresh);
            refreshTimer.Interval = TimeSpan.FromSeconds(4);
            refreshTimer.Tick += RefreshTimer_Tick;
        }

        public void EnterRoom(string node)
        {
            this.node = node;

            SharedData.EdgeConnectionInfo.IsConnectedToEdge = true;

            Refresh();
            refreshTimer.IsEnabled = true;
            refreshTimer.Start();
            Dispatcher.Invoke(() => Growl.Info("若联机失败，建议关闭防火墙"));
            Dispatcher.Invoke(()=> Growl.Success("房间加入成功"));
        }

        public class DataItem
        {
            public string? Community { get; set; }
            public string? Ip4Address { get; set; }
            public string? StuIda { get; set; }
        }
        public string? json;

        private async void _Refresh()
        {
            string url = "http://" + SharedData.n2nServerApiIPP + "/edges?" + node;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    json = await response.Content.ReadAsStringAsync();

                    // 在这里处理返回的 JSON 数据
                    Console.WriteLine(json);
                    //HandyControl.Controls.Growl.SuccessGlobal(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("发生异常: " + ex.Message);
                    HandyControl.Controls.Growl.WarningGlobal("发生异常: " + ex.Message);
                }
            }

            if (json != null)
            {
                JsonDocument jsonDocument = JsonDocument.Parse(json);
                var jsonArray = jsonDocument.RootElement.EnumerateArray();
                List<DataItem> dataItems = new List<DataItem>();
                int StuId = 0;
                foreach (JsonElement jsonObject in jsonArray)
                {
                    StuId += 1;
                    string? community = jsonObject.GetProperty("desc").GetString();
                    string? ip4addr = jsonObject.GetProperty("ip4addr").GetString();

                    string? ip4 = null;

                    if (ip4addr != null)
                    {
                        int index = ip4addr.IndexOf("/");
                        ip4 = ip4addr.Substring(0, index);
                    }

                    dataItems.Add(new DataItem { StuIda = StuId.ToString(), Community = community, Ip4Address = ip4 });
                }

                //if (DataGridroom.ItemsSource!=null&&((List<DataItem>)DataGridroom.ItemsSource).Count != dataItems.Count)
                //    return;

                Dispatcher.Invoke(()=> DataGridroom.ItemsSource = dataItems);
            }
            else
            {
                HandyControl.Controls.Growl.WarningGlobal("未获取到房间IP");
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
}


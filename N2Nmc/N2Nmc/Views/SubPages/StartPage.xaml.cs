using N2Nmc.UtilsClass;
using N2Nmc_Protocol;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace N2Nmc.Views.SubPages
{
    /// <summary>
    /// DefaultPage.xaml 的交互逻辑
    /// </summary>
    public partial class StartPage : Page
    {
        DispatcherTimer timerRefreshData = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(800)};

        public StartPage()
        {
            InitializeComponent();

            LabelVersion.Content += SharedData.versionString;
            //NewsOfflineBox.Text = File.ReadAllText("Data/NewsOffline.txt");

            timerRefreshData.Tick += TimerRefreshData_Tick;

            timerRefreshData.Start();
        }

        ~StartPage()
        {
            timerRefreshData.Stop();
            timerRefreshData.IsEnabled = false;
        }

        private int onlineTotal = 0;
        public int OnlineTotal
        {
            get => onlineTotal;
            set
            {
                onlineTotal = value;
                LabelOnlineTotal.Content = string.Format("当前 {0}人在线",value);
            }
        }

        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => SharedData.CheckN2NClientUpdate(Dispatcher));
        }

        private void ButtonDebug_ResetConnection_Click(object sender, RoutedEventArgs e)
        {
            SharedData.NM_Connection.Close();
            SharedData.NM_Connection = new SharedData.N2NmcServerConnection();
            SharedData.ConnectAndPeek();
        }

        private void LabelVersion_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Templated
        }

        private void ButtonForumCommit_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Send
            ForumCommitBox.Text = "";
        }

        private void TimerRefreshData_Tick(object? sender, System.EventArgs e)
        {
            Task.Run(() =>
            {

                if (SharedData.NM_Connection != null)
                {
                    if (SharedData.NM_Connection.IsConnected())
                    {
                        lock (SharedData.NM_Connection)
                        {
                            SharedData.NM_Connection.Send(Package.MakePackage(Protocol.BaseHeader._pull_online_total));
                            var p = SharedData.NM_Connection.Receive();

                            if (p != null)
                            {
                                var pp = p.Value;

                                if ((Protocol.BaseHeader)pp.Header == Protocol.BaseHeader.msg_string)
                                {
                                    if (pp.external_data != null)
                                    {
                                        var totalMembers = Package.MsgExternalData.Decode.MsgString(pp.external_data);
                                        try
                                        {
                                            int i = int.Parse(totalMembers);
                                            Dispatcher.InvokeAsync(()=>OnlineTotal = i);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("TimerRefreshData_Tick -> Protocol._pull_online_total -> Exception: \n{0}", ex.ToString());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });


        }
    }
}

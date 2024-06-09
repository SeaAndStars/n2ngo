using HandyControl.Controls;
using N2Nmc.UtilsClass;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace N2Nmc.Views.SubPages
{
    /// <summary>
    /// Page6.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        //private void SelectBackgroundImageButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var dialog = new OpenFileDialog();
        //    dialog.Filter = ".jpg|*.jpg|.png|*.png|.jpeg|*.jpeg|*|*.*";
        //    if (dialog.ShowDialog(App.Current.MainWindow) == false) return;
        //    BitmapImage tempImage = new BitmapImage();
        //    tempImage.BeginInit();
        //    tempImage.UriSource = new Uri(dialog.FileName, UriKind.RelativeOrAbsolute);
        //    tempImage.EndInit();

        //    ((MainView)App.Current.MainWindow).ImageBackgroundImage.Source = tempImage;
        //}

        private async void InstallTapButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Process.Start("Data/TapWindowsInstaller/9.21.2.exe", "/S /X").WaitForExitAsync();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => SharedData.GetMainView?.DoMessageDialog("在尝试安装Tap驱动时发生异常：" + ex.Message + "\n请尝试以管理员身份运行N2Nmc或\n手动安装该文件：Data/TapWindowsInstaller/9.21.2.exe", "Tap驱动安装"));
                return;
            }
            Dispatcher.Invoke(() => SharedData.GetMainView?.DoMessageDialog("Tap驱动已完成安装", "Tap驱动安装"));
        }

        private void ClearShaderCacheButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.Delete(SharedData.ShaderCacheDir, true);
            }
            catch (Exception) { }

            SharedData.GetMainView.DoMessageDialog("着色器缓存已清除，重新启动应用以重新着色", "清理着色器缓存");
        }

        int i = 0;
        private void BackgroundOSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = (Slider)sender;
            //OSliderInd.Content = string.Format("{0:0.00}% 背景不透明度", (e.NewValue / (slider.Maximum - slider.Minimum) * 100));
            OSliderInd.Content = string.Format("{0:0}% 背景不透明度", (e.NewValue / (slider.Maximum - slider.Minimum) * 100));

            if (i > 1)
            {
                SharedData.GetMainView.SetBackColor((byte)e.NewValue);
                SharedData.configFile?.Set("WindowBackgroundAlpha", ((int)e.NewValue).ToString());
            }
            else
                ++i;
        }

        private void ResetConfigButton_Click(object sender, RoutedEventArgs e)
        {
            SharedData.configFile?.Clear();
            SharedData.GetMainView.DoMessageDialog("重启应用以生效。", "设置");
        }

        private void ColorPaletteSeletion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0)
                return;

            var snder = sender as HandyControl.Controls.ComboBox;
            if (snder == null)
                throw new ArgumentNullException("ColorPaletteSeletion_SelectionChanged sender arg null");

            if (SharedData.GetMainView.isInitialized)
                SharedData.configFile?.Set("CurrentColorPalette", snder.SelectedIndex.ToString());

            var selectedItem = e.AddedItems[0] as string;
            if (selectedItem != null)
                SharedData.GetMainView.UpdateColorPalette(selectedItem);
        }

        /*
        private void xunifangjian_Click(object sender, RoutedEventArgs e)
        {
            //UDPMC();
            // 创建线程
            Thread UDPMCt = new Thread(new ThreadStart(UDPMC));

            // 启动线程
            UDPMCt.Start();

            Thread jiancet = new Thread(new ThreadStart(jiance));

            // 启动线程
            jiancet.Start();
            //jiance();

            //CheckMCServerOnline("127.0.0.1",3389);
        }
        
        static void UDPMC()
        {
            // 创建一个套接字
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // 设置多播TTL
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
            // 创建目标终结点
            IPAddress multicastAddress = IPAddress.Parse("224.0.2.60");
            IPEndPoint multicastEndpoint = new IPEndPoint(multicastAddress, 4445);
            IPAddress targetAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint targetEndpoint = new IPEndPoint(targetAddress, 12521);
            Console.WriteLine("开启局域网发现");
            while (true)
            {
                // 构造消息
                StringBuilder sb = new StringBuilder();
                sb.Append("[MOTD]§2§l[OPL]§b远程世界 §7-by N2Nmc[/MOTD][AD]");
                sb.Append(4445);
                sb.Append("[/AD]");
                string message = sb.ToString();
                byte[] sendBytes = Encoding.UTF8.GetBytes(message);
                // 发送消息到多播地址
                socket.SendTo(sendBytes, multicastEndpoint);
                
                //socket.Close();
                //UDPMC();
            }

            

            

            static bool CheckMCServerOnline(string serverIP, int serverPort)
            {
                try
                {
                    // 创建一个TcpClient对象
                    using (TcpClient client = new TcpClient())
                    {
                        // 尝试连接到服务器
                        client.Connect(serverIP, serverPort);
                        // 如果连接成功，则服务器在线
                        return client.Connected;
                    }
                }
                catch (SocketException ex)
                {
                    // 连接失败，服务器离线或不可达
                    Console.WriteLine($"连接失败: {ex.Message}");
                    return false;
                }
            }

        }

        static void jiance() 
        {
            int port = 4445; // 监听的端口号
            TcpListener server = null;
            try
            {
                // 创建一个TcpListener实例来监听指定的端口
                server = new TcpListener(IPAddress.Any, port);
                server.Start();
                Console.WriteLine("等待客户端连接...");
                while (true)
                {
                    // 开始异步等待客户端的连接
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("客户端已连接!");
                    // 为每个客户端连接创建一个新的线程来处理通信
                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                    clientThread.Start(client);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                if (server != null)
                {
                    server.Stop();
                }
            }
            Console.WriteLine("服务器已停止.");

        }
        static void HandleClient(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream stream = tcpClient.GetStream();
            // 在这里实现与客户端的通信逻辑
            // 例如，可以读取和发送数据
            byte[] recvBuffer = new byte[1024];
            int recvLength = 0;
            // 读取客户端发送的数据
            do
            {
                recvLength = stream.Read(recvBuffer, 0, recvBuffer.Length);
                if (recvLength > 0)
                {
                    // 将接收到的数据发送到远程服务器
                    byte[] sendBuffer = new byte[recvLength];
                    Array.Copy(recvBuffer, sendBuffer, recvLength);
                    SendToRemoteServer(sendBuffer);
                    // 从远程服务器获取数据并返回给客户端
                    byte[] confirmBuffer = new byte[1024];
                    int confirmLength = GetDataFromRemoteServer(confirmBuffer);
                    if (confirmLength > 0)
                    {
                        stream.Write(confirmBuffer, 0, confirmLength);
                    }
                }
            } while (recvLength > 0);
            // 处理完成后，关闭连接
            tcpClient.Close();
        }
        private static void SendToRemoteServer(byte[] data)
        {
            // 设置远程IP地址和端口号
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12521);
            // 创建一个TcpClient实例用于连接远程服务器
            using (TcpClient server = new TcpClient())
            {
                // 连接到远程服务器
                server.Connect(remoteEP);
                // 获取网络流
                NetworkStream stream = server.GetStream();
                // 发送数据到远程服务器
                stream.Write(data, 0, data.Length);
                // 接收远程服务器的确认
                byte[] confirmBuffer = new byte[1024];
                int confirmLength = stream.Read(confirmBuffer, 0, confirmBuffer.Length);
                // 确保关闭网络流和TcpClient
                stream.Close();
                server.Close();
            }
        }
        private static int GetDataFromRemoteServer(byte[] buffer)
        {
            // 设置远程IP地址和端口号
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12521);
            // 创建一个TcpClient实例用于连接远程服务器
            using (TcpClient server = new TcpClient())
            {
                // 连接到远程服务器
                server.Connect(remoteEP);
                // 获取网络流
                NetworkStream stream = server.GetStream();
                // 读取远程服务器发送的数据
                int confirmLength = stream.Read(buffer, 0, buffer.Length);
                // 确保关闭网络流和TcpClient
                stream.Close();
                server.Close();
                return confirmLength;
            }
        }
        */
    }
}


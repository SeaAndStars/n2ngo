using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools.Extension;
using N2Nmc.Views;
using N2Nmc.Views.SubPages;
using N2Nmc_Protocol;
using N2Nmc_Protocol.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static N2Nmc_Protocol.Protocol;

namespace N2Nmc.UtilsClass
{
    public static class SharedData
    {
        public static EasyConfig? configFile = null;
        public readonly static Version version = new Version(4, 0, 0);
        public readonly static string versionTag = "Dev";

        public static string n2nServerIPP = N2Nmc_Protocol.UserDef.GlobalServer_Ip + ':' + N2Nmc_Protocol.UserDef.ExternServerOptions.N2N_SuperNode_Server_Port;
        public static string n2nServerApiIPP = N2Nmc_Protocol.UserDef.GlobalServer_Ip + ':' + N2Nmc_Protocol.UserDef.ExternServerOptions.N2N_SuperNode_API_Server_Port;

        public static string DefaultRoomPasswd = "null";
        public static string versionString { get { return version.ToString() + ' ' + versionTag; } }


        public static string ShaderCacheDir = "Data/ShaderCache";
        public static string binRefDir
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Path.Combine("Data", "BinRef", "Windows") : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? Path.Combine("Data", "BinRef", "Linux") : throw new Exception("OS Platform not support");

        public static string edgeExecFile
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "edge.exe" : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "edge" : throw new Exception("OS Platform not support");
        public static string edgePath =>
            Path.Combine(binRefDir, "n2n", edgeExecFile);
        public static string updateClientExecFile
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "N2NmcClientUpdate.exe" : throw new NotImplementedException();
        public static string updateClientPath =>
            Path.Combine(binRefDir, updateClientExecFile);

        public static MainView GetMainView { get => (MainView)App.Current.MainWindow; }

        public class ExecLog
        {
            public string LogOut { get; private set; } = string.Empty;
            public Process? ProcessExecuteCommand;
            public void ClearLog()
            {
                LogOut = string.Empty;
            }
            public void SetCommand(string command)
            {
                add_log("Command: " + command + Environment.NewLine);
                ProcessExecuteCommand = new Process();

                ProcessExecuteCommand.StartInfo.FileName = "cmd.exe";
                ProcessExecuteCommand.StartInfo.Arguments = "/c " + command;
                ProcessExecuteCommand.StartInfo.UseShellExecute = false;
                ProcessExecuteCommand.StartInfo.RedirectStandardOutput = true;
                ProcessExecuteCommand.StartInfo.RedirectStandardError = true;
                ProcessExecuteCommand.StartInfo.CreateNoWindow = true;

                ProcessExecuteCommand.OutputDataReceived += OutputDataReceived;
                ProcessExecuteCommand.ErrorDataReceived += OutputDataReceived;
            }
            public async Task<int> ExecuteAsync()
            {
                if (ProcessExecuteCommand == null)
                    throw new NullReferenceException(nameof(ProcessExecuteCommand));

                ClearLog();

                ProcessExecuteCommand.Start();

                ProcessExecuteCommand.BeginOutputReadLine();
                ProcessExecuteCommand.BeginErrorReadLine();

                await ProcessExecuteCommand.WaitForExitAsync();
                return ProcessExecuteCommand.ExitCode;
            }
            private void add_log(string data)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    string newLog = data + Environment.NewLine;

                    LogOut += newLog;

                    // Output to LogPage
                    LogPage? lp = ((MainView)App.Current.MainWindow).pageLog;
                    if (lp != null)
                        lp.textlog.Text += newLog;
                });
            }
            private void OutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    add_log(e.Data);
                }
            }
        }

        public class N2NmcServerConnection
        {
            public enum n2nmc_server_disconnected_actions
            {
                ask = 0,
                reconnect,
                ignore
            }

            public string serverip = N2Nmc_Protocol.UserDef.GlobalServer_Ip;
            public int serverport = N2Nmc_Protocol.UserDef.ExternServerOptions.N2Nmc_Server_Port;
            public GrowlInfo disconnectedGrowlInfo = new GrowlInfo
            {
                CancelStr = "稍后询问我",
                ConfirmStr = "重新连接",
                Type = InfoType.Ask,
                ShowCloseButton = true
            };
            public class _lockingVars
            {
                public int disconnectedGrowlInfoAction { get; private set; } = 0;
                public void INC()
                {
                    lock (this)
                        disconnectedGrowlInfoAction++;
                }
                public void DEC()
                {
                    lock (this)
                        if (disconnectedGrowlInfoAction <= 0)
                            disconnectedGrowlInfoAction = 0;
                        else
                            disconnectedGrowlInfoAction--;
                }
            }
            public _lockingVars LockingVars = new _lockingVars();

            public n2nmc_server_disconnected_actions n2nmc_server_disconnected_action { get; private set; } = n2nmc_server_disconnected_actions.ask;
            private TcpClient? Client = new TcpClient();

            public static void ClientExHandler(Exception ex, GrowlInfo growlInfo)
            {
                growlInfo.Message = "与 N2Nmc 服务器通信断开或错误，信息：" + ex.Message;
                HandyControl.Controls.Growl.Ask(growlInfo);
            }

            public bool Connect()
            {
                lock (this)
                {
                    Close();
                    Client = new TcpClient();

                    try
                    {
                        Client.Connect(serverip, serverport);
                        if (Client.Connected) return true;
                    }
                    catch (Exception ex)
                    {
                        ClientExHandler(ex, disconnectedGrowlInfo);
                        return false;
                    }
                    return false;
                }
            }

            public bool Send(Package package, int? timeOut = null)
            {
                lock (this)
                {
                    if (Client == null)
                        throw new NullReferenceException(nameof(Client));

                    Package.IO_Tool iO_Tool = new Package.IO_Tool();
                    if (timeOut != null)
                    {
                        iO_Tool.WriteTimeOut = timeOut.Value;
                    }

                    if (!iO_Tool.Send(Client, package, timeOut != null))
                    {
                        if (iO_Tool.latestEx != null)
                            ClientExHandler(iO_Tool.latestEx, disconnectedGrowlInfo);
                        else
                            ClientExHandler(new NetworkInformationException(), disconnectedGrowlInfo);

                        return false;
                    }

                    return true;
                }
            }

            public Package? Receive(int? timeOut = null)
            {
                lock (this)
                {
                    if (Client == null)
                        throw new NullReferenceException(nameof(Client));

                    Package? package = null;

                    Package.IO_Tool iO_Tool = new Package.IO_Tool();
                    if (timeOut != null)
                    {
                        iO_Tool.ReadTimeOut = timeOut.Value;
                    }

                    package = iO_Tool.Receive(Client, null, timeOut != null);

                    if (package == null)
                        if (iO_Tool.latestEx != null)
                            ClientExHandler(iO_Tool.latestEx, disconnectedGrowlInfo);
                        else
                            ClientExHandler(new NullReferenceException(), disconnectedGrowlInfo);

                    return package;
                }
            }

            public void Flush()
            {
                lock (this)
                {
                    if (Client == null)
                        throw new NullReferenceException(nameof(Client));

                    Client?.GetStream().Flush();
                }
            }

            public bool Peek()
            {
                lock (this)
                {
                    if (Client == null)
                        throw new NullReferenceException(nameof(Client));

                    try
                    {
                        Package pkg = Package.MakePackage(Protocol.BaseHeader.peek);
                        Send(pkg);

                        byte[] bytes = new byte[1];
                        Client.GetStream().Read(bytes, 0, 1);
                        Package pkg_get = Package.ResolvePackage(bytes);
                        return (Protocol.BaseHeader)pkg_get.Header == Protocol.BaseHeader.peek_ok;
                    }
                    catch (Exception ex)
                    {
                        ClientExHandler(ex, disconnectedGrowlInfo);
                        return false;
                    }
                }
            }

            public bool IsConnected()
            {
                lock (this)
                    return Client == null ? false : Client.Connected;
            }

            public void Close()
            {
                if (Client != null)
                {
                    Client.Close();
                    Client.Dispose();
                    Client = null;
                }
            }

        }
        public static N2NmcServerConnection NM_Connection = new N2NmcServerConnection();

        public static class EdgeConnectionInfo
        {
            private static bool _IsConnectedToEdge = false;
            public static bool IsConnectedToEdge
            {
                get => _IsConnectedToEdge;

                set
                {
                    _IsConnectedToEdge = value;

                    if (value)
                    {
                        ((MainView)App.Current.MainWindow).LabelTitle.Content = "N2N 已连接至 " + CurrentRoomCode + " - " + GetRoomNameByCode(CurrentRoomCode) + " (N2Nmc)";
                        ((MainView)App.Current.MainWindow).LabelTitle.Style = (Style)Application.Current.FindResource("ConnectedTitleStyle");
                    }
                    else
                    {
                        ((MainView)App.Current.MainWindow).LabelTitle.Content = "N2N GO";
                        ((MainView)App.Current.MainWindow).LabelTitle.Style = (Style)Application.Current.FindResource("TitleStyle");
                    }
                }
            }
            public static string CurrentRoomCode = "";
        }

        public static class UIAnimation
        {
            public static void Refresh()
            {
                btnDownAnimation = new ColorAnimation { To = Color.FromArgb(0x48, 0x80, 0x80, 0x80), Duration = TimeSpan.FromMilliseconds(100) };
                btnUpAnimation = new ColorAnimation { To = ((SolidColorBrush)App.Current.Resources["MainColorSolidAlphaBrush"]).Color, Duration = TimeSpan.FromMilliseconds(300) };
                btnEnterAnimation = new ColorAnimation { To = Color.FromArgb(0x50, 0xf0, 0xf0, 0xf0), Duration = TimeSpan.FromMilliseconds(120) };
                btnLeaveAnimation = new ColorAnimation { To = ((SolidColorBrush)App.Current.Resources["MainColorSolidAlphaBrush"]).Color, Duration = TimeSpan.FromMilliseconds(170) };
            }


            public static ColorAnimation btnDownAnimation { get; private set; } = new ColorAnimation { To = Color.FromArgb(0x48, 0x80, 0x80, 0x80), Duration = TimeSpan.FromMilliseconds(100) };
            public static ColorAnimation btnUpAnimation { get; private set; } = new ColorAnimation { To = ((SolidColorBrush)App.Current.Resources["MainColorSolidAlphaBrush"]).Color, Duration = TimeSpan.FromMilliseconds(300) };
            public static ColorAnimation btnEnterAnimation { get; private set; } = new ColorAnimation { To = Color.FromArgb(0x50, 0xc0, 0xc0, 0xc0), Duration = TimeSpan.FromMilliseconds(120) };
            public static ColorAnimation btnLeaveAnimation { get; private set; } = new ColorAnimation { To = ((SolidColorBrush)App.Current.Resources["MainColorSolidAlphaBrush"]).Color, Duration = TimeSpan.FromMilliseconds(170) };

            public static DoubleAnimation smallerAnimation { get; private set; } = new DoubleAnimation { To = 0.97, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            public static DoubleAnimation smallsmallerAnimation { get; private set; } = new DoubleAnimation { To = 0.92, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            public static DoubleAnimation normalsizeAnimation { get; private set; } = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.25), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            public static DoubleAnimation biggerAnimation { get; private set; } = new DoubleAnimation { To = 1.03, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            public static DoubleAnimation bigbiggerAnimation { get; private set; } = new DoubleAnimation { To = 1.07, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };


            public static void Btn_MouseUp(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, btnUpAnimation);

                TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                ScaleTransform st = (ScaleTransform)TG.Children[0];

                st.BeginAnimation(ScaleTransform.ScaleXProperty, normalsizeAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, normalsizeAnimation);

                var ue = (UIElement)sender;
                var uers = ue.RenderSize;
                if (e != null)
                {
                    var rpos = e.GetPosition(ue);
                    var rpx = rpos.X; var rpy = rpos.Y;
                    if (!(
                        (rpx > uers.Width || rpy > uers.Height) ||
                        (rpx < 0 || rpy < 0)
                        ))
                    { Btn_MouseEnter(sender, e); }
                }
            }
            public static void Btn_MouseDown(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, btnDownAnimation);

                TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                ScaleTransform st = (ScaleTransform)TG.Children[0];

                if (e != null)
                {
                    var p = e.MouseDevice.GetPosition((Control)sender);

                    st.CenterX = p.X;
                    st.CenterY = p.Y;
                }

                st.BeginAnimation(ScaleTransform.ScaleXProperty, smallsmallerAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, smallsmallerAnimation);
            }
            public static void Btn_MouseLeave(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, btnLeaveAnimation);
            }
            public static void Btn_MouseEnter(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, btnEnterAnimation);
            }
            public static void Btn_MouseMove(object sender, MouseEventArgs? e)
            {
                TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                ScaleTransform st = (ScaleTransform)TG.Children[0];

                if (e != null)
                {
                    var p = e.MouseDevice.GetPosition((Control)sender);

                    // Inverted
                    st.CenterX = p.X;
                    st.CenterY = p.Y;
                }
            }
            public static void InitButton(Control button)
            {
                try
                {
                    button.Resources["UIA_Locked"] = false;
                    button.RenderTransform = new TransformGroup { Children = new TransformCollection(new Transform[] { new ScaleTransform(1, 1, 1, 1) }) };
                    var color = (SolidColorBrush)button.Background == null ? Colors.Transparent : ((SolidColorBrush)button.Background).Color;
                    button.Background = new SolidColorBrush(color); // reinit

                    button.MouseEnter += Btn_MouseEnter;
                    button.MouseLeave += Btn_MouseLeave;
                    button.PreviewMouseDown += Btn_MouseDown;
                    button.PreviewMouseUp += Btn_MouseUp;
                    button.PreviewMouseMove += Btn_MouseMove;

                    Btn_MouseLeave(button, null);
                }
                catch (Exception)
                {
                    InitCard(button);
                }
            }
            public static void InitButtons(IEnumerable<Button> buttons)
            {
                foreach (var button in buttons)
                {
                    InitButton(button);
                }
            }


            public static void Card_MouseUp(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                ScaleTransform st = (ScaleTransform)TG.Children[0];

                st.BeginAnimation(ScaleTransform.ScaleXProperty, normalsizeAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, normalsizeAnimation);
            }
            public static void Card_MouseDown(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                ScaleTransform st = (ScaleTransform)TG.Children[0];

                if (e != null)
                {
                    var p = e.MouseDevice.GetPosition((Control)sender);

                    st.CenterX = p.X;
                    st.CenterY = p.Y;
                }

                st.BeginAnimation(ScaleTransform.ScaleXProperty, smallerAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, smallerAnimation);
            }
            public static void Card_MouseLeave(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                ScaleTransform st = (ScaleTransform)TG.Children[0];

                st.BeginAnimation(ScaleTransform.ScaleXProperty, normalsizeAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, normalsizeAnimation);

                //var anim = btnLeaveAnimation;
                //anim.To = (Color)((Control)sender).Resources["_UIA_Color"];
                //((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, anim);
            }
            public static void Card_MouseEnter(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                ScaleTransform st = (ScaleTransform)TG.Children[0];

                if (e != null)
                {
                    var p = e.MouseDevice.GetPosition((Control)sender);

                    st.CenterX = p.X;
                    st.CenterY = p.Y;
                }

                st.BeginAnimation(ScaleTransform.ScaleXProperty, biggerAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, biggerAnimation);
                //((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, btnEnterAnimation);
            }
            public static void Card_MouseMove(object sender, MouseEventArgs? e)
            {
                TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                ScaleTransform st = (ScaleTransform)TG.Children[0];

                if (e != null)
                {
                    var p = e.MouseDevice.GetPosition((Control)sender);

                    st.CenterX = p.X;
                    st.CenterY = p.Y;
                }
            }
            public static void InitCard(Control card)
            {
                try
                {
                    //InitButton(card);
                    card.Resources["UIA_Locked"] = false;
                    card.RenderTransform = new TransformGroup { Children = new TransformCollection(new Transform[] { new ScaleTransform(1, 1, .5, .5) }) };
                    //var color = (SolidColorBrush)card.Background == null ? Colors.Transparent : ((SolidColorBrush)card.Background).Color;
                    //card.Background = new SolidColorBrush(color); // reinit

                    //card.Resources["_UIA_Color"] = color;
                    card.MouseEnter -= Card_MouseEnter;
                    card.MouseLeave -= Card_MouseLeave;
                    card.PreviewMouseDown -= Card_MouseDown;
                    card.PreviewMouseUp -= Card_MouseUp;
                    card.PreviewMouseMove -= Card_MouseMove;

                    card.MouseEnter += Card_MouseEnter;
                    card.MouseLeave += Card_MouseLeave;
                    card.PreviewMouseDown += Card_MouseDown;
                    card.PreviewMouseUp += Card_MouseUp;
                    card.PreviewMouseMove += Card_MouseMove;

                    Card_MouseLeave(card, null);
                }
                catch(Exception)
                { }
            }
            public static void InitCards(IEnumerable<Control> cards)
            {
                foreach (var card in cards)
                {
                    InitCard(card);
                }
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent != null)
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                    if (child is T typedChild)
                    {
                        yield return typedChild;
                    }

                    foreach (T foundChild in FindVisualChildren<T>(child))
                    {
                        yield return foundChild;
                    }
                }
            }
        }

        public static void InvalidConnectionHandler()
        {
            Growl.Error("无效的 N2Nmc 服务器");
        }


        public static bool Peek()
        {
            lock (NM_Connection)
            {
                if (!NM_Connection.IsConnected())
                    return false;

                return NM_Connection.Peek();
            }
        }

        /*
         * Coding Example: 
         *  Task.Run(() => SharedData.CheckN2NClientUpdate(Dispatcher));
         *  
         */
        public static void CheckN2NClientUpdate(Dispatcher? dispatcher = null)
        {
            //HandyControl.Controls.Growl.Info("正在从服务器获取更新");

            lock (NM_Connection)
                if (Peek())
                {
                    try
                    {
                        Package? _pkg_get = null;
                        lock (NM_Connection)
                        {
                            NM_Connection.Send(Package.MakePackage(BaseHeader._ver_check)); // send _ver_check ID

                            _pkg_get = NM_Connection.Receive();
                            if (_pkg_get == null || (BaseHeader)_pkg_get.Value.Header != BaseHeader.msg_string || _pkg_get.Value.external_data == null)
                            {
                                HandyControl.Controls.Growl.Error("无法从服务器获取更新：错误的服务器配置");
                                return;
                            }
                        }

                        Package pkg_get = _pkg_get.Value;

                        string versionString = Package.MsgExternalData.Decode.MsgString(pkg_get.external_data);
                        Version serverVersionGet = Version.Parse(versionString);
                        if (SharedData.version < serverVersionGet)
                        {
                            StringBuilder strNewVersionMsg = new StringBuilder();
                            strNewVersionMsg.AppendLine("发现新版本: " + serverVersionGet.ToString());
                            strNewVersionMsg.AppendLine("当前版本: " + SharedData.versionString);
                            strNewVersionMsg.AppendLine("是否要更新？");

                            GrowlInfo newVersionGrowlInfo = new GrowlInfo
                            {
                                Message = strNewVersionMsg.ToString(),
                                CancelStr = "忽略",
                                ConfirmStr = "更新至" + serverVersionGet.ToString(),
                                Type = InfoType.Success,
                                ShowCloseButton = false,
                                ActionBeforeClose = (_) =>
                                {
                                    if (_)
                                    {
                                        try
                                        {
                                            Task.Run(() =>
                                            {
                                                new WebClient().DownloadFile("http://" + N2Nmc_Protocol.UserDef.GlobalServer_Ip + ':' + N2Nmc_Protocol.UserDef.ExternServerOptions.N2Nmc_File_Server_Port + '/' + N2Nmc_Protocol.UserDef.UpdatePackageFileName, N2Nmc_Protocol.UserDef.UpdatePackageFileName);
                                                SharedData.configFile?.Set("NeedUpdate", "1");
                                                HandyControl.Controls.Growl.Success("N2Nmc更新包 已下载，将在关闭 N2Nmc 后升级。");
                                            });
                                        }
                                        catch (Exception ex)
                                        {
                                            HandyControl.Controls.Growl.Error("从服务器下载更新包时失败：" + ex.Message);
                                        }
                                    }

                                    return true;
                                }
                            };

                            HandyControl.Controls.Growl.Ask(newVersionGrowlInfo);
                        }
                        else
                            //HandyControl.Controls.Growl.Success("N2Nmc 已是最新版");
                            dispatcher?.Invoke(() => GetMainView?.DoMessageDialog("N2N GO 已是最新版", "更新"));
                    }
                    catch (Exception ex)
                    {
                        HandyControl.Controls.Growl.Error("无法从服务器获取更新：" + ex.Message);
                        return;
                    }
                }
                else
                {
                    N2NmcServerConnection.ClientExHandler(new Exception("检测更新时发生错误：未连接至N2Nmc服务器"), NM_Connection.disconnectedGrowlInfo);
                }
        }

        public static void ConnectAndPeek()
        {
            lock (NM_Connection)
            {
                if (NM_Connection.Connect())
                {
                    HandyControl.Controls.Growl.Success("成功连接到 N2Nmc 服务器");
                    Peek();
                }
                else
                {
                    HandyControl.Controls.Growl.Error("无法连接至 N2Nmc 服务器");
                    NM_Connection.disconnectedGrowlInfo.Message = "是否要尝试重新连接？";
                    HandyControl.Controls.Growl.Ask(NM_Connection.disconnectedGrowlInfo);
                }
            }
        }

        public static string? GetUserKey()
        {
            lock (NM_Connection)
            {
                var rpackage = NM_Connection.Receive(6000);
                if (rpackage == null)
                    return null;

                var package = rpackage.Value;
                if ((BaseHeader)package.Header != BaseHeader.msg_string || package.external_data == null)
                    return null;

                return Package.MsgExternalData.Decode.MsgString(package.external_data);
            }
        }

        public static string GetRoomCode(string input)
        {
            return new Random((int)DateTime.Now.Ticks).Next(0, 1000000).ToString("x");

            ////string baseNumber = "0123456789";
            //string generatedNumber = GenerateNumber(input);
            //string mixedNumber = MixNumber(generatedNumber);
            //string hexNumber = ToHex(mixedNumber);
            //return hexNumber;
        }

        public static bool CheckRoomExists(string RoomCode)
        {
            Package? _pkg_get = null;
            lock (NM_Connection)
            {
                NM_Connection.Send(Package.MakePackage(Protocol.BaseHeader._rooms_is_code_exists, Package.MsgExternalData.Encode.MsgString(RoomCode)));
                _pkg_get = NM_Connection.Receive();
            }

            if (_pkg_get != null)
            {
                Package pkg_get = _pkg_get.Value;
                if ((Protocol.BaseHeader)pkg_get.Header == Protocol.BaseHeader.msg_byte && pkg_get.external_data != null)
                {
                    return Package.MsgExternalData.Decode.MsgByte(pkg_get.external_data) == 0 ? false : true;
                }
                else
                {
                    InvalidConnectionHandler();
                    return false;
                }
            }

            return false;
        }

        public static string[]? CreateRoom(string RoomName, bool IsRoomInvisible, bool IsRoomPasswordNeeded, string RoomPassword, UInt32 MainColor, UInt32 MinorColor)
        {
            string RoomCode = GetRoomCode(RoomName);

            while (CheckRoomExists(RoomCode))
                RoomCode = GetRoomCode(RoomName);

            lock (NM_Connection)
            {
                if (NM_Connection.Send(Package.MakePackage(Protocol.BaseHeader._rooms_create)))
                    if (NM_Connection.Send(Package.MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(RoomCode))))
                        if (NM_Connection.Send(Package.MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(RoomName))))
                            if (NM_Connection.Send(Package.MakePackage(BaseHeader.msg_byte, Package.MsgExternalData.Encode.MsgByte((byte)(IsRoomInvisible ? 1 : 0)))))
                                if (NM_Connection.Send(Package.MakePackage(BaseHeader.msg_byte, Package.MsgExternalData.Encode.MsgByte((byte)(IsRoomPasswordNeeded ? 1 : 0)))))
                                    if (NM_Connection.Send(Package.MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(RoomPassword))))
                                        if (NM_Connection.Send(Package.MakePackage(BaseHeader.msg_ulong, Package.MsgExternalData.Encode.MsgULong(MainColor))))
                                            if (NM_Connection.Send(Package.MakePackage(BaseHeader.msg_ulong, Package.MsgExternalData.Encode.MsgULong(MinorColor))))
                                            {
                                                var rpackage = NM_Connection.Receive(6000);
                                                if (rpackage != null)
                                                {
                                                    var package = rpackage.Value;
                                                    if ((BaseHeader)package.Header == BaseHeader.msg_string_long && package.external_data != null)
                                                    {
                                                        return new string[] { RoomCode, Package.MsgExternalData.Decode.MsgStringLong(package.external_data) };
                                                    }
                                                }
                                            }
            }

            return null;
        }

        public static string? GetRoomNameByCode(string RoomCode)
        {
            string? name = null;

            if (!CheckRoomExists(RoomCode))
                return name;

            Package? _pkg_get = null;
            lock (NM_Connection)
            {
                NM_Connection.Send(Package.MakePackage(Protocol.BaseHeader._rooms_get_name));
                NM_Connection.Send(Package.MakePackage(Protocol.BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(RoomCode)));

                _pkg_get = NM_Connection.Receive();
            }
            if (_pkg_get != null)
            {
                Package pkg_get = _pkg_get.Value;
                if (pkg_get.Header != (byte)BaseHeader.msg_string || pkg_get.external_data == null)
                    return name;

                name = Package.MsgExternalData.Decode.MsgString(pkg_get.external_data);
            }

            return name;
        }

        public static string? JoinRoom(string RoomCode, string RoomPassword, string NickName)
        {
            lock (NM_Connection)
            {
                if (!NM_Connection.Send(Package.MakePackage(Protocol.BaseHeader._room_client_join)))
                    goto joinRoom_fail;
                if (!NM_Connection.Send(Package.MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(RoomCode))))
                    goto joinRoom_fail;
                if (!NM_Connection.Send(Package.MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(RoomPassword))))
                    goto joinRoom_fail;
                if (!NM_Connection.Send(Package.MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(NickName))))
                    goto joinRoom_fail;

                var rpackage = NM_Connection.Receive(6000);
                if (rpackage == null)
                    goto joinRoom_fail;
                var rpackagev = rpackage.Value;
                if ((BaseHeader)rpackagev.Header == BaseHeader._room_client_join_fail_0)
                {
                    GetMainView.DoMessageDialog("抱歉\n您只能在同一时间在一个房间内活动", "加入房间失败");
                    goto joinRoom_fail_msg_handled;
                }
                if ((BaseHeader)rpackagev.Header == BaseHeader._room_client_join_fail_1)
                {
                    GetMainView.DoMessageDialog("抱歉\n您所指定的房间不存在\n请稍后重试", "加入房间失败");
                    goto joinRoom_fail_msg_handled;
                }
                if ((BaseHeader)rpackagev.Header == BaseHeader._room_client_join_fail_2)
                {
                    GetMainView.DoMessageDialog("抱歉\n您当前加入的房间与您所提供的密码不匹配\n请稍后重试", "加入房间失败");
                    goto joinRoom_fail_msg_handled;
                }
                if ((BaseHeader)rpackagev.Header == BaseHeader._room_client_join_fail_00)
                {
                    var rrpackage = NM_Connection.Receive(6000);
                    if (rrpackage == null)
                        goto joinRoom_fail;
                    var rrpackagev = rrpackage.Value;

                    if ((BaseHeader)rrpackagev.Header != BaseHeader.msg_long || rrpackagev.external_data == null)
                        goto joinRoom_fail;

                    // Check 'N2Nmc_Protocol.Objects.Room.MemberJoin'
                    var rrr = Package.MsgExternalData.Decode.MsgLong(rrpackagev.external_data);

                    if (rrr == 1)

                    GetMainView.DoMessageDialog("抱歉\n您当前加入的房间\n请稍后重试", "加入房间失败");
                    goto joinRoom_fail_msg_handled;
                }
                if ((BaseHeader)rpackagev.Header != BaseHeader.msg_string || rpackagev.external_data == null)
                    goto joinRoom_fail;

                // Success
                return Package.MsgExternalData.Decode.MsgString(rpackagev.external_data);
            }

        joinRoom_fail:
            GetMainView.DoMessageDialog("抱歉\n您当前无法加入指定房间\n请稍后重试", "加入房间失败");
        joinRoom_fail_msg_handled:
            return null;
        }

        public static void ExitRoom()
        {
            KillEdge();
        }

        public static void KillEdge()
        {
            Process[] process = Process.GetProcessesByName("edge");
            if (process.Length > 0)
            {
                foreach (Process p in process)
                {
                    p.Kill();
                }
            }

            EdgeConnectionInfo.IsConnectedToEdge = false;
        }

        public static void MakeMessageBoxContent(string Content, string Caption)
        {
            if (System.Windows.MessageBox.Show(string.Format("{0}\n\n(复制内容到剪切板？)", Content), Caption, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                // WPF Error: OpenClipboard HRESULT:0x800401D0 (CLIPBRD_E_CANT_OPEN))
                // Clipboard.SetText(Content); 

                Clipboard.SetDataObject(Content);
            }
        }
    }
}

using HandyControl.Controls;
using N2Nmc.Views;
using N2Nmc.Views.SubPages;
using N2Nmc.Views.SubPages.Dialogs;
using N2Nmc.Views.SubPages.Dialogs.MessageDialogs;
using N2Nmc_Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        public readonly static EasyConfig ConfigFile = new("Data/config.ini");
        public readonly static Version Version = new(4, 0, 0);
        public readonly static string VersionTag = "Dev";

        public static string n2nServerIPP = N2Nmc_Protocol.UserDef.GlobalServer_Ip + ':' + N2Nmc_Protocol.UserDef.ExternServerOptions.N2N_SuperNode_Server_Port;
        public static string n2nServerApiIPP = N2Nmc_Protocol.UserDef.GlobalServer_Ip + ':' + N2Nmc_Protocol.UserDef.ExternServerOptions.N2N_SuperNode_API_Server_Port;

        public static string DefaultRoomPasswd = "null";
        public static string VersionString { get { return Version.ToString() + ' ' + VersionTag; } }


        public static string ShaderCacheDir = "Data/ShaderCache";
        public static string BinRefDir
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Path.Combine("Data", "BinRef", "Windows") : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? Path.Combine("Data", "BinRef", "Linux") : throw new Exception("OS Platform not support");

        public static string EdgeExecFile
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "edge.exe" : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "edge" : throw new Exception("OS Platform not support");
        public static string EdgePath =>
            Path.Combine(BinRefDir, "n2n", EdgeExecFile);
        public static string UpdateClientExecFile
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "N2NmcClientUpdate.exe" : throw new NotImplementedException();
        public static string UpdateClientPath =>
            Path.Combine(BinRefDir, UpdateClientExecFile);

        public static MainView GetMainView { get => (MainView)App.Current.MainWindow; }
        private static Dispatcher? _mainViewDispatcher;
        public static Dispatcher MainViewDispatcher
        {
            get
            {
                if (_mainViewDispatcher == null)
                    throw new NullReferenceException(nameof(_mainViewDispatcher));

                return _mainViewDispatcher;
            }
            set => _mainViewDispatcher = value;
        }

        public static class CurrentApp
        {
            public static App Get
            {
                get
                {
                    var app = Application.Current as App;
                    if (app == null)
                        throw new NullReferenceException("Cannot get current app as App");
                    return app;
                }
            }

            public static class Locale
            {
                public class LocaleHead
                {
                    public string Name { get; set; } = "null";
                    public string Language { get; private set; } = "null";
                    public string ISOLanguageCode { get; private set; } = "null";
                    public string RegionCode { get; private set; } = "null";

                    public LocaleHead() { }
                    public LocaleHead(string Language) : this()
                    {
                        this.Language = Language;
                        this.Name = Language;
                    }
                    public LocaleHead(string Name, string Language, string ISOLanguageCode, string RegionCode) : this()
                    {
                        this.Name = Name;
                        this.Language = Language;
                        this.ISOLanguageCode = ISOLanguageCode;
                        this.RegionCode = RegionCode;
                    }
                }

                public static string CurrentLocale { get; private set; } = "default";

                const string _path = "ResourcesDictionaries/UI/Locales/";
                public static void UpdateLocale(string locale = "default")
                {
                    var ds = CurrentApp.Get.Resources.MergedDictionaries
                            .Cast<ResourceDictionary>()
                            .Where(_d => _d.Source.OriginalString.Contains(_path))
                            .ToList();
                    foreach (var d in ds)
                        CurrentApp.Get.Resources.MergedDictionaries.Remove(d);

                    CurrentApp.Get.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new($"{_path}{locale}.xaml", UriKind.Relative) });
                    CurrentLocale = locale;
                }

                public static LocaleHead? ReadLocal(string locale = "default")
                {
                    ResourceDictionary l;
                    try
                    {
                        l = new ResourceDictionary { Source = new($"{_path}{locale}.xaml", UriKind.Relative) };
                    }
                    catch (IOException)
                    {
                        return null;
                    }

                    return new((string)l["LHEAD_Name"], (string)l["LHEAD_Language"], (string)l["LHEAD_ISOLanguageCode"], (string)l["LHEAD_RegionCode"]);
                }
            }
        }

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
                ProcessExecuteCommand = new();

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
                    LogPage? lp = GetMainView.pageLog;
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
            public string ServerIP = N2Nmc_Protocol.UserDef.GlobalServer_Ip;
            public int ServerPort = N2Nmc_Protocol.UserDef.ExternServerOptions.N2Nmc_Server_Port;

            private TcpClient _client = new();

            public void ClientExHandler(Exception? ex = null)
            {
                MainViewDispatcher.Invoke(()=>GetMainView.DoMessageYesNoDialog($"我们与N2N GO 服务器通信断开或错误，信息：{ex?.Message}\n要重新连接吗？", "N2N GO 连接异常",
                    new() {
                        (_dialogMessage) =>
                            {
                                var dialogMessage = _dialogMessage as DialogMessage;
                                if (dialogMessage == null)
                                    throw new NullReferenceException(nameof(dialogMessage));
                                var rr = dialogMessage.MessageContent as DialogYesNo;
                                if ( rr == null || rr.YesNo != DialogYesNo.YesNoE.Yes)
                                    return;

                                Task.Run(() => ResetConnection(true));
                            }
                        }
                    ));
            }

            #region Base Operations
            /// <summary>
            /// <see cref="Package.IO_Tool.Send">Send</see> a <see cref="Package">package</see> through the <see cref="_client">Connection Client</see>
            /// </summary>
            public bool Connect()
            {
                lock (this)
                {
                    Close();
                    _client = new();

                    try
                    {
                        _client.Connect(ServerIP, ServerPort);
                        if (_client.Connected) return true;
                    }
                    catch (Exception ex)
                    {
                        ClientExHandler(ex);
                        return false;
                    }
                    return false;
                }
            }

            /// <summary>
            /// <see cref="Package.IO_Tool.Send">Send</see> a <see cref="Package">package</see> through the <see cref="_client">Connection Client</see>
            /// </summary>
            /// <param name="package">Package to be sent</param>
            /// <param name="timeOut">Sets <seealso cref="TcpClient.ReceiveTimeout"/></param>
            /// <returns>true if successful, otherwise false.</returns>
            public bool Send(Package package, int? timeOut = null)
            {
                lock (this)
                {
                    Package.IO_Tool iO_Tool = new();
                    if (timeOut != null)
                    {
                        iO_Tool.WriteTimeOut = timeOut.Value;
                    }

                    if (!iO_Tool.Send(_client, package, timeOut != null))
                    {
                        if (iO_Tool.latestEx != null)
                            ClientExHandler(iO_Tool.latestEx);
                        else
                            ClientExHandler(new("Unknown"));

                        return false;
                    }

                    return true;
                }
            }

            /// <summary>
            /// <see cref="Package.IO_Tool.Receive">Receive</see> a <see cref="Package">package</see> through the <see cref="_client">Connection Client</see>
            /// </summary>
            /// <param name="timeOut">Sets <seealso cref="TcpClient.ReceiveTimeout"/></param>
            /// <returns>Package</returns>
            public Package? Receive(int? timeOut = null)
            {
                lock (this)
                {
                    Package? package = null;

                    Package.IO_Tool iO_Tool = new();
                    if (timeOut != null)
                    {
                        iO_Tool.ReadTimeOut = timeOut.Value;
                    }

                    package = iO_Tool.Receive(_client, null, timeOut != null);

                    if (package == null)
                        if (iO_Tool.latestEx != null)
                            ClientExHandler(iO_Tool.latestEx);
                        else
                            ClientExHandler(new("Unknown"));

                    return package;
                }
            }

            /// <summary>
            ///  <see cref="System.Net.Sockets.NetworkStream.Flush">Flushes</see> data from the <see cref="System.Net.Sockets.TcpClient.GetStream">stream</see> of <see cref="_client">Connection Client</see>. This method is reserved for future use.
            /// </summary>
            public void Flush()
            {
                lock (this) _client.GetStream().Flush();
            }

            /// <summary>
            ///  Peek connection to server.
            /// </summary>
            /// <returns>true if successful, otherwise false.</returns>
            public bool Peek()
            {
                lock (this)
                {
                    if (!_client.Connected)
                        return false;

                    try
                    {
                        Package pkg = Package.MakePackage(Protocol.BaseHeader.peek);
                        Send(pkg);

                        byte[] bytes = new byte[1];
                        _client.GetStream().Read(bytes, 0, 1);
                        Package pkg_get = Package.ResolvePackage(bytes);
                        return (Protocol.BaseHeader)pkg_get.Header == Protocol.BaseHeader.peek_ok;
                    }
                    catch (Exception ex)
                    {
                        ClientExHandler(ex);
                        return false;
                    }
                }
            }

            /// <summary>
            ///  Check that the connection[<see cref="System.Net.Sockets.TcpClient.Connected"/>] to the server is valid.
            /// </summary>
            /// <returns>true if valid, otherwise false.</returns>
            public bool IsConnected()
            {
                lock (this) return _client.Connected;
            }

            /// <summary>
            ///  Close and dispose <see cref="_client">Connection Client</see>
            /// </summary>
            public void Close()
            {
                _client.Close();
                _client.Dispose();
            }
            #endregion

            public enum ProtocolOperationReturnStatus
            {
                None = 0, Success = 1,
                Fail_NotConnected,
                Fail_NullPackage,
                Fail_NullPackageExternalData,
                Fail_InvalidPackageHeader,
            }
            public struct ProtocolOperationReturnType<T>
            {
                public ProtocolOperationReturnStatus Status { get; private set; }
                public T? Value { get; private set; }

                public ProtocolOperationReturnType(T value)
                {
                    Value = value;
                    Status = ProtocolOperationReturnStatus.Success;
                }
                public ProtocolOperationReturnType(T? value, ProtocolOperationReturnStatus status)
                {
                    Value = value;
                    Status = status;
                }
                public ProtocolOperationReturnType(ProtocolOperationReturnStatus status)
                {
                    Status = status;
                    Value = default;
                }

                public static bool operator true(ProtocolOperationReturnType<T> inst) => inst.Status == ProtocolOperationReturnStatus.Success;
                public static bool operator false(ProtocolOperationReturnType<T> inst) => inst.Status != ProtocolOperationReturnStatus.Success;
            }
            #region Protocol Operations
            /// <summary>
            /// Get User Key<br/>
            /// <see cref="Protocol.BaseHeader._user_key_get"></see>
            /// </summary>
            public ProtocolOperationReturnType<string> GetUserKey()
            {
                if (!IsConnected())
                    return new(ProtocolOperationReturnStatus.Fail_NotConnected);

                var rpackage = Receive(6000);
                if (rpackage == null)
                    return new(ProtocolOperationReturnStatus.Fail_NullPackage);

                var package = rpackage.Value;
                if ((BaseHeader)package.Header != BaseHeader.msg_string_long)
                    return new(ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                if (package.external_data == null)
                    return new(ProtocolOperationReturnStatus.Fail_NullPackageExternalData);

                return new(Package.MsgExternalData.Decode.MsgStringLong(package.external_data));
            }
            /// <summary>
            /// Pull number of onlines<br/>
            /// <see cref="Protocol.BaseHeader._pull_online_total"></see>
            /// </summary>
            public ProtocolOperationReturnType<int> PullTotalOnlines()
            {
                if (!IsConnected())
                    return new(-1, ProtocolOperationReturnStatus.Fail_NotConnected);

                Package? p = null;
                lock (this)
                {
                    Send(Package.MakePackage(Protocol.BaseHeader._pull_online_total));
                    p = Receive();
                }

                if (p == null)
                    return new(-1, ProtocolOperationReturnStatus.Fail_NullPackage);

                var v = p.Value;
                if ((Protocol.BaseHeader)v.Header != Protocol.BaseHeader.msg_long)
                    return new(-1, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);

                if (v.external_data == null)
                    return new(-1, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);

                var totalMembers = Package.MsgExternalData.Decode.MsgLong(v.external_data);
                return new(totalMembers);
            }
            #endregion
        }
        public static N2NmcServerConnection NM_Connection = new();

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
                        GetMainView.LabelTitle.Content = "N2N 已连接至 " + CurrentRoomCode + " - " + GetRoomNameByCode(CurrentRoomCode) + " (N2Nmc)";
                        GetMainView.LabelTitle.Style = (Style)Application.Current.FindResource("ConnectedTitleStyle");
                    }
                    else
                    {
                        GetMainView.LabelTitle.Content = "N2N GO";
                        GetMainView.LabelTitle.Style = (Style)Application.Current.FindResource("TitleStyle");
                    }
                }
            }
            public static string CurrentRoomCode = "";
        }

        public static class UIAnimation
        {
            public static void Refresh()
            {
                ButtonDownAnimation = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_500"], Duration = TimeSpan.FromMilliseconds(100) };
                ButtonUpAnimation = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_300"], Duration = TimeSpan.FromMilliseconds(300) };
                ButtonEnterAnimation = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_300"], Duration = TimeSpan.FromMilliseconds(120) };
                ButtonLeaveAnimation = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_500"], Duration = TimeSpan.FromMilliseconds(170) };
            }


            public static ColorAnimation ButtonDownAnimation { get; private set; } = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_500"], Duration = TimeSpan.FromMilliseconds(100) };
            public static ColorAnimation ButtonUpAnimation { get; private set; } = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_300"], Duration = TimeSpan.FromMilliseconds(300) };
            public static ColorAnimation ButtonEnterAnimation { get; private set; } = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_200"], Duration = TimeSpan.FromMilliseconds(120) };
            public static ColorAnimation ButtonLeaveAnimation { get; private set; } = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_300"], Duration = TimeSpan.FromMilliseconds(170) };

            public static DoubleAnimation SmallerAnimation { get; private set; } = new() { To = 0.97, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            public static DoubleAnimation SmallSmallerAnimation { get; private set; } = new() { To = 0.92, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            public static DoubleAnimation NormalSizeAnimation { get; private set; } = new() { To = 1, Duration = TimeSpan.FromSeconds(0.25), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            public static DoubleAnimation BiggerAnimation { get; private set; } = new() { To = 1.03, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            public static DoubleAnimation BigBiggerAnimation { get; private set; } = new() { To = 1.07, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };


            public static void Button_MouseUp(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, ButtonUpAnimation);

                TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                ScaleTransform st = (ScaleTransform)TG.Children[0];

                st.BeginAnimation(ScaleTransform.ScaleXProperty, NormalSizeAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, NormalSizeAnimation);

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
                    { Button_MouseEnter(sender, e); }
                }
            }
            public static void Button_MouseDown(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, ButtonDownAnimation);

                TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                ScaleTransform st = (ScaleTransform)TG.Children[0];

                if (e != null)
                {
                    var p = e.MouseDevice.GetPosition((Control)sender);

                    st.CenterX = p.X;
                    st.CenterY = p.Y;
                }

                st.BeginAnimation(ScaleTransform.ScaleXProperty, SmallSmallerAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, SmallSmallerAnimation);
            }
            public static void Button_MouseLeave(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, ButtonLeaveAnimation);
            }
            public static void Button_MouseEnter(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, ButtonEnterAnimation);
            }
            public static void Button_MouseMove(object sender, MouseEventArgs? e)
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

                    button.MouseEnter += Button_MouseEnter;
                    button.MouseLeave += Button_MouseLeave;
                    button.PreviewMouseDown += Button_MouseDown;
                    button.PreviewMouseUp += Button_MouseUp;
                    button.PreviewMouseMove += Button_MouseMove;

                    Button_MouseLeave(button, null);
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

                st.BeginAnimation(ScaleTransform.ScaleXProperty, NormalSizeAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, NormalSizeAnimation);
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

                st.BeginAnimation(ScaleTransform.ScaleXProperty, SmallerAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, SmallerAnimation);
            }
            public static void Card_MouseLeave(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                ScaleTransform st = (ScaleTransform)TG.Children[0];

                st.BeginAnimation(ScaleTransform.ScaleXProperty, NormalSizeAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, NormalSizeAnimation);

                //var anim = ButtonLeaveAnimation;
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

                st.BeginAnimation(ScaleTransform.ScaleXProperty, BiggerAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, BiggerAnimation);
                //((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, ButtonEnterAnimation);
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
                catch (Exception)
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

        public static async void DownloadN2NGOUpdateAsync()
        {
            try
            {
                using (HttpClient client = new())
                {
                    using (HttpResponseMessage response = await
                        client.GetAsync($"http://{N2Nmc_Protocol.UserDef.GlobalServer_Ip}:{N2Nmc_Protocol.UserDef.ExternServerOptions.N2Nmc_File_Server_Port}/{N2Nmc_Protocol.UserDef.UpdatePackageFileName}")
                        )
                    {
                        try
                        {
                            response.EnsureSuccessStatusCode();
                        }
                        catch (HttpRequestException e)
                        {
                            Console.WriteLine($"{e.Message}");
                        }

                        using (FileStream fileStream = File.Create(N2Nmc_Protocol.UserDef.UpdatePackageFileName))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GetMainView.DispatcherDoMessageDialog($"从服务器下载更新包时失败：{ex.Message}", "更新N2N GO");
            }

            SharedData.ConfigFile.Set("NeedUpdate", "1");
            GetMainView.DispatcherDoMessageDialog("N2N GO 更新包 已下载，将在N2N GO 关闭后升级。", "更新N2N GO");
        }

        /// <summary>
        /// Check update for N2N GO client asynchronous
        /// Coding Example: <br/>
        /// <code>Task.Run(() => SharedData.CheckN2NGOClientUpdate());</code>
        /// </summary>
        public static void CheckN2NGOClientUpdate()
        {
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
                                MainViewDispatcher.Invoke(() => GetMainView.DispatcherDoMessageDialog("无法从服务器获取更新：错误的服务器配置", "更新N2N GO"));
                                return;
                            }
                        }

                        Package pkg_get = _pkg_get.Value;

                        string versionString = Package.MsgExternalData.Decode.MsgString(pkg_get.external_data);
                        Version serverVersionGet = Version.Parse(versionString);
                        if (SharedData.Version < serverVersionGet)
                        {
                            StringBuilder strNewVersionMsg = new();
                            strNewVersionMsg.AppendLine("发现新版本: " + serverVersionGet.ToString());
                            strNewVersionMsg.AppendLine($"{SharedData.VersionString} -> {serverVersionGet.ToString()}");
                            strNewVersionMsg.AppendLine("是否要更新？");

                            MainViewDispatcher.Invoke(() => GetMainView.DispatcherDoMessageYesNoDialog(strNewVersionMsg.ToString(), "发现新版本",
                                     new() {
                                        (_dialogMessage) =>
                                        {
                                            var dialogMessage = _dialogMessage as DialogMessage;
                                            if (dialogMessage == null)
                                                throw new NullReferenceException(nameof(dialogMessage));
                                            var rr = dialogMessage.MessageContent as DialogYesNo;
                                            if ( rr == null || rr.YesNo != DialogYesNo.YesNoE.Yes)
                                                return;

                                            DownloadN2NGOUpdateAsync();
                                        }
                                     }
                                 )
                            );
                        }
                        else
                            MainViewDispatcher.Invoke(() => GetMainView.DispatcherDoMessageDialog("N2N GO 已是最新版", "更新"));
                    }
                    catch (Exception ex)
                    {
                        MainViewDispatcher.Invoke(() => GetMainView.DispatcherDoMessageDialog($"无法从服务器获取更新：{ex.Message}"));
                        return;
                    }
                }
                else
                {
                    NM_Connection.ClientExHandler(new Exception("检测更新时发生错误：未连接至N2Nmc服务器"));
                }
        }

        public static void ConnectAndPeek(bool successEcho = false)
        {
            lock (NM_Connection)
            {
                if (NM_Connection.Connect())
                {
                    if (successEcho)
                        MainViewDispatcher.Invoke(()=>GetMainView.DoMessageDialog("成功连接到 N2N GO 服务器", "连接"));
                    Peek();
                }
            }
        }

        public static void ResetConnection(bool echo = false, string? serverIp = null, int? serverPort=null)
        {
            SharedData.NM_Connection.Close();
            SharedData.NM_Connection = new();
            if (serverIp != null)
                SharedData.NM_Connection.ServerIP = serverIp;
            if (serverPort != null)
                SharedData.NM_Connection.ServerPort = serverPort.Value;
            SharedData.ConnectAndPeek(echo);
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

        public static bool CheckRoomExists(string roomCode)
        {
            Package? _pkg_get = null;
            lock (NM_Connection)
            {
                NM_Connection.Send(Package.MakePackage(Protocol.BaseHeader._rooms_is_code_exists, Package.MsgExternalData.Encode.MsgString(roomCode)));
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

        public static string[]? CreateRoom(string roomName, bool isRoomInvisible, bool isRoomPasswordNeeded, string roomPassword, UInt32 mainColor, UInt32 minorColor)
        {
            string RoomCode = GetRoomCode(roomName);

            while (CheckRoomExists(RoomCode))
                RoomCode = GetRoomCode(roomName);

            lock (NM_Connection)
            {
                if (!NM_Connection.Send(Package.MakePackage(Protocol.BaseHeader._rooms_create)))
                    goto CreateRoom_Fail;
                if (!NM_Connection.Send(Package.MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(RoomCode))))
                    goto CreateRoom_Fail;
                if (!NM_Connection.Send(Package.MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(roomName))))
                    goto CreateRoom_Fail;
                if (!NM_Connection.Send(Package.MakePackage(BaseHeader.msg_byte, Package.MsgExternalData.Encode.MsgByte((byte)(isRoomInvisible ? 1 : 0)))))
                    goto CreateRoom_Fail;
                if (!NM_Connection.Send(Package.MakePackage(BaseHeader.msg_byte, Package.MsgExternalData.Encode.MsgByte((byte)(isRoomPasswordNeeded ? 1 : 0)))))
                    goto CreateRoom_Fail;
                if (!NM_Connection.Send(Package.MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(roomPassword))))
                    goto CreateRoom_Fail;
                if (!NM_Connection.Send(Package.MakePackage(BaseHeader.msg_ulong, Package.MsgExternalData.Encode.MsgULong(mainColor))))
                    goto CreateRoom_Fail;
                if (!NM_Connection.Send(Package.MakePackage(BaseHeader.msg_ulong, Package.MsgExternalData.Encode.MsgULong(minorColor))))
                    goto CreateRoom_Fail;

                var rpackage = NM_Connection.Receive(6000);
                if (rpackage != null)
                {
                    var package = rpackage.Value;
                    if ((BaseHeader)package.Header == BaseHeader.msg_string_long && package.external_data != null)
                    {
                        var rrpackage = NM_Connection.Receive(6000);
                        if (rrpackage != null)
                        {
                            if ((BaseHeader)rrpackage.Value.Header == BaseHeader.msg_ok)
                                return new string[] { RoomCode, Package.MsgExternalData.Decode.MsgStringLong(package.external_data) };
                        }
                    }
                }
            }

        CreateRoom_Fail:
            return null;
        }

        public static string? GetRoomNameByCode(string roomCode)
        {
            string? name = null;

            if (!CheckRoomExists(roomCode))
                return name;

            Package? _pkg_get = null;
            lock (NM_Connection)
            {
                NM_Connection.Send(Package.MakePackage(Protocol.BaseHeader._rooms_get_name));
                NM_Connection.Send(Package.MakePackage(Protocol.BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(roomCode)));

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

        public static string? JoinRoom(string roomCode, string roomPassword, string nickName)
        {
            lock (NM_Connection)
            {
                if (!NM_Connection.Send(Package.MakePackage(Protocol.BaseHeader._room_client_join)))
                    goto joinRoom_fail;
                if (!NM_Connection.Send(Package.MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(roomCode))))
                    goto joinRoom_fail;
                if (!NM_Connection.Send(Package.MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(roomPassword))))
                    goto joinRoom_fail;
                if (!NM_Connection.Send(Package.MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(nickName))))
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

        public static void MakeMessageBoxContent(string content, string caption)
        {
            if (System.Windows.MessageBox.Show(string.Format("{0}\n\n(复制内容到剪切板？)", content), caption, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                // WPF Error: OpenClipboard HRESULT:0x800401D0 (CLIPBRD_E_CANT_OPEN))
                // Clipboard.SetText(content); 

                Clipboard.SetDataObject(content);
            }
        }
    }
}

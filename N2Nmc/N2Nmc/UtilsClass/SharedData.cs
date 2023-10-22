using HandyControl.Controls;
using HandyControl.Data;
using N2Nmc.Views;
using N2Nmc.Views.SubPages;
using N2Nmc_Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using static N2Nmc_Protocol.Protocol;

namespace N2Nmc.UtilsClass
{
    public static class SharedData
    {
        public static EasyConfig? configFile = null;
        public readonly static Version version = new Version(3, 8,4);
        public readonly static string versionTag = "Dev";

        public static string n2nServerIPP = N2Nmc_Protocol.UserDef.GlobalServer_Ip + ':' + N2Nmc_Protocol.UserDef.ExternServerOptions.N2N_SuperNode_Server_Port;
        public static string n2nServerApiIPP = N2Nmc_Protocol.UserDef.GlobalServer_Ip + ':' + N2Nmc_Protocol.UserDef.ExternServerOptions.N2N_SuperNode_API_Server_Port;

        public static string DefaultRoomPasswd = "null";
        public static string versionString { get { return version.ToString() + ' ' + versionTag; } }

        public static string binRefDir
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?Path.Combine("Data","BinRef","Windows"): RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? Path.Combine("Data","BinRef","Linux"): throw new Exception("OS Platform not support");

        public static string edgeExecFile
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "edge.exe" : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "edge" : throw new Exception("OS Platform not support");
        public static string edgePath =>
            Path.Combine(binRefDir, "n2n", edgeExecFile);
        public static string updateClientExecFile
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "N2NmcClientUpdate.exe" : throw new NotImplementedException();
        public static string updateClientPath =>
            Path.Combine(binRefDir,updateClientExecFile);

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
                if (ProcessExecuteCommand ==null)
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

            public bool Send(Package package)
            {
                lock (this)
                {
                    if (Client == null)
                        throw new NullReferenceException(nameof(Client));

                    Package.IO_Tool iO_Tool = new Package.IO_Tool();
                    if (!iO_Tool.Send(Client, package))
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

            public Package? Receive()
            {
                lock (this)
                {
                    if (Client == null)
                        throw new NullReferenceException(nameof(Client));

                    Package? package = null;

                    Package.IO_Tool iO_Tool = new Package.IO_Tool();
                    package = iO_Tool.Receive(Client);

                    if (package == null)
                        if (iO_Tool.latestEx != null)
                            ClientExHandler(iO_Tool.latestEx, disconnectedGrowlInfo);
                        else
                            ClientExHandler(new NullReferenceException(), disconnectedGrowlInfo);

                    return package;
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
                        ((MainView)App.Current.MainWindow).LabelTitle.Content = "N2N 已连接至 " + CurrentRoomCode + " - " + GetRoomNameByCode(CurrentRoomCode)+ " (N2Nmc)";
                        ((MainView)App.Current.MainWindow).LabelTitle.Style = (Style)Application.Current.FindResource("ConnectedTitleStyle");
                    }
                    else
                    {
                        ((MainView)App.Current.MainWindow).LabelTitle.Content = "     "+"N2Nmc "+(Environment.Is64BitProcess ? "64-bit" : "32-bit") +" ("+RuntimeInformation.OSDescription+" - "+ RuntimeInformation .OSArchitecture+ ")"+"     ";
                        ((MainView)App.Current.MainWindow).LabelTitle.Style = (Style)Application.Current.FindResource("TitleStyle");
                    }
                }
            }
            public static string CurrentRoomCode = "";
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

        public static void CheckN2NClientUpdate()
        {
            HandyControl.Controls.Growl.Info("正在从服务器获取更新");

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
                            HandyControl.Controls.Growl.Success("N2Nmc 已是最新版");
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
                NM_Connection.Send(Package.MakePackage(Protocol.BaseHeader._room_is_code_exists, Package.MsgExternalData.Encode.MsgString(RoomCode)));
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

        public static string CreateRoom(string RoomName, bool IsRoomInvisible, bool IsRoomPasswordNeeded, string RoomPassword)
        {
            string RoomCode = GetRoomCode(RoomName);

            while (CheckRoomExists(RoomCode))
                RoomCode = GetRoomCode(RoomName);

            lock (NM_Connection)
            {
                if (NM_Connection.Send(Package.MakePackage(Protocol.BaseHeader._room_create)))
                    if (NM_Connection.Send(Package.MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(RoomCode))))
                        if (NM_Connection.Send(Package.MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(RoomName))))
                            if (NM_Connection.Send(Package.MakePackage(BaseHeader.msg_byte, Package.MsgExternalData.Encode.MsgByte((byte)(IsRoomInvisible ? 1 : 0)))))
                                if (NM_Connection.Send(Package.MakePackage(BaseHeader.msg_byte, Package.MsgExternalData.Encode.MsgByte((byte)(IsRoomPasswordNeeded ? 1 : 0)))))
                                    if (NM_Connection.Send(Package.MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(RoomPassword)))) Growl.Success("创建房间成功：" + RoomCode);
                //{
                //    Package? pkg_server_ret = N2NmcCSConnection.Receive();
                //    if (pkg_server_ret == null)
                //    {
                //        Growl.Error("创建房间失败: obj pkg_server_ret null");
                //        return RoomCode;
                //    }

                //    if (pkg_server_ret.Value.Header == (byte)BaseHeader.msg_ok)
                //        Growl.Success("创建房间成功：" + RoomCode);
                //}
            }

            return RoomCode;
        }

        public static void CloseRoom(string RoomCode)
        {
            if (!CheckRoomExists(RoomCode))
                return;

            lock (NM_Connection)
                NM_Connection.Send(Package.MakePackage(Protocol.BaseHeader._room_close));

        }

        public static string? GetRoomNameByCode(string RoomCode)
        {
            string? name = null;

            if (!CheckRoomExists(RoomCode))
                return name;

            Package? _pkg_get = null;
            lock (NM_Connection)
            {
                NM_Connection.Send(Package.MakePackage(Protocol.BaseHeader._room_get_name));
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
    }
}

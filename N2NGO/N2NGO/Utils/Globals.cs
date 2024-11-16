using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace N2NGO.Utils;

internal static class Globals
{
    public readonly static Version Version = new(4, 0, 1, 1);
    public readonly static string VersionTag = "Release";

    public readonly static string DefaultRoomPassword = "null";
    public static string VersionString { get { return $"{Version}-{VersionTag}"; } }

    public static string BinRefDir
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Path.Combine("Data", "BinRef", "Windows") : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? Path.Combine("Data", "BinRef", "Linux") : throw new Exception("OS Platform not support");

    public static string EdgeExecFile
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "edge.exe" : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "edge" : throw new Exception("OS Platform not support");
    public static string EdgePath =>
        Path.Combine(BinRefDir, "n2n", EdgeExecFile);

    public static class CurrentApp
    {
        public static App Get
        {
            get
            {
                App? app = Application.Current as App;
                return app ?? throw new NullReferenceException("Cannot get current app as App");
            }
        }
        public static Dispatcher Dispatcher => Get.Dispatcher;
        public static Views.MainWindow MainWindow => (Views.MainWindow)Application.Current.MainWindow;
        public static N2NGOCore.EasyConfig Config { get; } = new(Path.Combine(N2NGOCore.Vars.N2NGO_N2NGO_AppData_Path, "UserData/config.ini"));

        // 7476 & 7478 for release
        // 7477 & 7479 for alpha
        public static N2NGOServerConnection N2NGOServerConnection { get; set; } = new(new(IPAddress.Parse(Config.Get("IpGlobalServer", "43.143.37.61")), int.Parse(Config.Get("PortGlobalServer", "7476"))), int.Parse(Config.Get("PortSupernodeServer", "7478")));
        public static RoomConnection RoomConnection { get; set; } = new();

        public static BinExecutor EdgeN2NExecutor { get; } = new()
        {
            ActionOnOutput = (data) =>
            {
                Dispatcher.InvokeAsync(() => Log.WriteLine($"{data}", Log.Module.N2NEdge));
            },

            ActionOnError = (data) =>
            {
                Dispatcher.InvokeAsync(() => Log.WriteLine($"{data}", Log.Module.N2NEdge));
            }
        };

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
                    Name = Language;
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

            const string _path = "Resources/Dictionaries/UI/Locales/";
            public static void UpdateLocale(string locale = "default")
            {
                var ds = Get.Resources.MergedDictionaries
                        .Cast<ResourceDictionary>()
                        .Where(_d => _d.Source.OriginalString.Contains(_path))
                        .ToList();
                foreach (var d in ds)
                    Get.Resources.MergedDictionaries.Remove(d);

                Get.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new($"{_path}{locale}.xaml", UriKind.Relative) });
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

        public static class Log
        {
            public enum Module
            {
                None = 0,
                MainWindow,
                TerminalWindow,
                MainWindow_RoomPage,
                MainWindow_RoomsPage,
                N2NEdge
            }
            public static readonly Dictionary<Module, string> ModulePrefixes = new()
            {
                { Module.None, "[App]" },
                { Module.TerminalWindow, "[TerminalWindow]" },
                { Module.MainWindow, "[MainWindow]" },
                { Module.MainWindow_RoomPage, "[MainWindow.RoomPage]" },
                { Module.MainWindow_RoomsPage, "[MainWindow.RoomsPage]" },
                { Module.N2NEdge, "[edge - n2n]" }
            };

            public static void WriteLine(string message, Module module = Module.None) => Console.WriteLine($"{ModulePrefixes[module]} {message}");
        }

        public static bool EnterRoom(string roomCode, string roomPassword)
        {
            var joinRoomResult = N2NGOServerConnection.JoinRoom(roomCode, roomPassword);
            if (!joinRoomResult.IsSuccessfulStatusCode)
                return false;

            Dispatcher.Invoke(() => MainWindow.PageRoom.BeginRefresh());
            RoomConnection.MemberID = joinRoomResult.Value ?? throw new("Cannot get MemberID by invoking N2NGOServerConnection.JoinRoom");
            RoomConnection.CurrentRoomCode = roomCode;
            return RoomConnection.IsConnected = true;
        }
        public static async Task LeaveRoom(bool exHandler = true)
        {
            TerminateEdge();
            Dispatcher.Invoke(() => MainWindow.PageRoom.EndRefresh());
            RoomConnection.IsConnected = false;
            RoomConnection.CurrentRoomCode = string.Empty;
            RoomConnection.MemberID = string.Empty;
            N2NGOServerConnection.LeaveRoom(exHandler);
            if (!EdgeN2NExecutor.IsCompleted && EdgeN2NExecutor.BinProcessTask is not null)
                await EdgeN2NExecutor.BinProcessTask;
        }
        public static async Task<bool> JoinRoomAsync(bool needPassword, string roomCode, string roomPassword)
        {
            await LeaveRoom();

            roomCode = roomCode.Trim();
            roomPassword = roomPassword.Trim();
            roomPassword = needPassword ? roomPassword : DefaultRoomPassword;

            if (string.IsNullOrEmpty(roomCode))
            {
                Dispatcher.Invoke(() => MainWindow.DoMessageDialog("@LOCALE_DialogJoinRoom_Failure_RoomCodeEmptyInput_Description_Content", "@LOCALE_DialogJoinRoom_Title"));
                await LeaveRoom();
                return false;
            }

            if (needPassword && string.IsNullOrEmpty(roomPassword.Trim()))
            {
                Dispatcher.Invoke(() => MainWindow.DoMessageDialog("@LOCALE_DialogJoinRoom_Failure_RoomPassEmptyInput_Description_Content", "@LOCALE_DialogJoinRoom_Title"));
                await LeaveRoom();
                return false;
            }

            var enterRoomResult = await Task.Run(() => EnterRoom(roomCode, roomPassword));
            if (!enterRoomResult)
            {
                Dispatcher.Invoke(() => MainWindow.DoMessageDialog("@LOCALE_DialogJoinRoom_Failure_CheckInfo_Description_Content", "@LOCALE_DialogJoinRoom_Failure_Title"));
                return false;
            }

            TaskCompletionSource<bool> analysisTaskResult = new();

            DispatcherTimer timer = new(DispatcherPriority.Normal, Dispatcher) { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += async (_, __) =>
            {
                var analysis = N2NEdgeOutputHelper.Analyze(EdgeN2NExecutor.GetOutput);
                if (analysis == N2NEdgeOutputHelper.EdgeAnalysisResults.None)
                    return;
                if ((analysis & N2NEdgeOutputHelper.EdgeAnalysisResults.NoWindowsTapDevices) == N2NEdgeOutputHelper.EdgeAnalysisResults.NoWindowsTapDevices)
                {
                    Dispatcher.Invoke(() => MainWindow.DoMessageDialog("@LOCALE_DialogJoinRoom_Failure_NoWindowsTapDevices_Description_Content", "@LOCALE_DialogJoinRoom_Failure_Title"));
                    await LeaveRoom();
                    analysisTaskResult.SetResult(false);
                    timer.Stop();
                    return;
                }
                if ((analysis & N2NEdgeOutputHelper.EdgeAnalysisResults.DeviceOperationFailure) == N2NEdgeOutputHelper.EdgeAnalysisResults.DeviceOperationFailure)
                {
                    Dispatcher.Invoke(() => MainWindow.DoMessageDialog("@LOCALE_DialogJoinRoom_Failure_DeviceOperationFailure_Description_Content", "@LOCALE_DialogJoinRoom_Failure_Title"));
                    await LeaveRoom();
                    analysisTaskResult.SetResult(false);
                    timer.Stop();
                    return;
                }
                if ((analysis & N2NEdgeOutputHelper.EdgeAnalysisResults.Connected) == N2NEdgeOutputHelper.EdgeAnalysisResults.Connected)
                {
                }

                analysisTaskResult.SetResult(true);
                timer.Stop();

                Dispatcher.Invoke(() => MainWindow.DoMessageDialog("@LOCALE_DialogJoinRoom_Success_Description_Content", "@LOCALE_DialogJoinRoom_Title"));
            };
            timer.Start();

            string arg = $"-c {roomCode} -k {roomPassword} -l {N2NGOServerConnection.ServerIPEndPoint.Address}:{N2NGOServerConnection.ServerSupernodePort}";

            if (!EdgeN2NExecutor.IsCompleted)
            {
                Dispatcher.Invoke(() => MainWindow.DoMessageDialog("您当前仍有其他正在进入房间的任务，请查看日志", "进入房间终止"));

                timer.Stop();
                return false;
            }

            _ = EdgeN2NExecutor.ExecuteAsync(EdgePath, arg);

            return await analysisTaskResult.Task;
        }


        public static bool Peek()
        {
            lock (N2NGOServerConnection)
            {
                if (!N2NGOServerConnection.IsConnected())
                    return false;

                return N2NGOServerConnection.Peek();
            }
        }
        public static bool ConnectAndPeek(bool successEcho = false)
        {
            lock (N2NGOServerConnection)
            {
                if (N2NGOServerConnection.Connect())
                {
                    if (successEcho)
                        Dispatcher.Invoke(() => MainWindow.DoMessageDialog("@LOCALE_DialogConnectionSuccessful_Content", "@LOCALE_DialogConnectionSuccessful_Title"));
                    return Peek();
                }
                else
                    return false;
            }
        }
        public static void ResetConnection(bool echo = false, string? serverIp = null, int? serverPort = null, int? supernodeServerPort = null)
        {
            LeaveRoom(false).Wait();

            N2NGOServerConnection.Close();
            N2NGOServerConnection = new(new(IPAddress.Parse(Config.Get("IpGlobalServer", "43.143.37.61")), int.Parse(Config.Get("PortGlobalServer", "7476"))), int.Parse(Config.Get("PortSupernodeServer", "7478")));
            if (serverIp != null)
                N2NGOServerConnection.ServerIPEndPoint.Address = IPAddress.Parse(serverIp);
            if (serverPort != null)
                N2NGOServerConnection.ServerIPEndPoint.Port = serverPort.Value;
            if (supernodeServerPort != null)
                N2NGOServerConnection.ServerSupernodePort = supernodeServerPort.Value;
            ConnectAndPeek(echo);
        }

        /// <summary>
        /// Check update for N2N GO client asynchronous
        /// Coding Example: <br/>
        /// <code>Task.Run(() => Globals.CheckN2NGOClientUpdate());</code>
        /// </summary>
        public static void CheckN2NGOClientUpdate()
        {
            lock (N2NGOServerConnection)
                if (Peek())
                {
                    try
                    {
                        N2NGOCore.Package? _pkg_get = null;
                        lock (N2NGOServerConnection)
                        {
                            N2NGOServerConnection.Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader._ver_check)); // send _ver_check MemberID

                            _pkg_get = N2NGOServerConnection.Receive();
                            if (_pkg_get == null || (N2NGOCore.Protocol.BaseHeader)_pkg_get.Value.Header != N2NGOCore.Protocol.BaseHeader.msg_string || _pkg_get.Value.external_data == null)
                            {
                                Dispatcher.Invoke(() => MainWindow.DoMessageDialog("@LOCALE_DialogUpdate_Fail_InvalidServer_Content", "@LOCALE_DialogUpdate_Title"));
                                return;
                            }
                        }

                        N2NGOCore.Package pkg_get = _pkg_get.Value;

                        string versionString = N2NGOCore.Package.MsgExternalData.Decode.MsgString(pkg_get.external_data);
                        Version serverVersionGet = Version.Parse(versionString);
                        if (Version < serverVersionGet)
                        {
                            StringBuilder strNewVersionMsg = new();
                            strNewVersionMsg.AppendLine($"{VersionString} -> {serverVersionGet.ToString()}:\nhttps://mail.bestlgf.pro/N2NGO/Download");
                        }
                        else
                            Dispatcher.Invoke(() => MainWindow.DoMessageDialog("@LOCALE_DialogUpdate_UpToDate_Content", "@LOCALE_DialogUpdate_Title"));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => MainWindow.DoMessageDialog($"无法从服务器获取更新：{ex.Message}"));
                        return;
                    }
                }
                else
                {
                    N2NGOServerConnection.ClientExHandler(new Exception("检测更新时发生错误：未连接至N2N GO 服务器"));
                }
        }
        public static void PrintMemSet(string? tag = null)
        {
            Process currentProcess = Process.GetCurrentProcess();

            long memoryUsage = currentProcess.WorkingSet64;
            double memoryUsageInMB = memoryUsage / (1024 * 1024);

            Log.WriteLine($"({tag ?? "App"}) Memory usage: {memoryUsageInMB} MBytes");
        }
        public static void MakeMessageBoxContentClipboard(string content, string caption)
        {
            if (MessageBox.Show(string.Format("{0}\n\n(Copy to clipboard?)", content), caption, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                // WPF Error: OpenClipboard HRESULT:0x800401D0 (CLIPBRD_E_CANT_OPEN))
                // Clipboard.SetText(content); 

                Clipboard.SetDataObject(content);
            }
        }

        public static void TerminateEdge()
        {
            Process[] process = Process.GetProcessesByName("edge");
            if (process.Length > 0)
            {
                foreach (Process p in process)
                {
                    p.Kill();
                }
            }
        }

        public static string GenerateRoomCode()
        {
            return new Random((int)DateTime.Now.Ticks).Next(0, 1000000).ToString("x");
        }
    }
}

public class SolidColorBrushColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SolidColorBrush solidColorBrush)
            return new Color();

        return solidColorBrush.Color;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}

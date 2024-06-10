using MineMP;
using N2Nmc.UtilsClass;
using N2Nmc_Protocol.Objects;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace N2Nmc_Server.N2NmcServer
{
    internal class ServerInConsole
    {
        private static readonly string NameExpansion = "in Console";

        private bool IsRunning = false;

        public EasyConfig ServerConfig { get; private set; }
        public Base.N2NmcServer Server { get; private set; }


        private void InitConsole()
        {
            Console.Title = Base.N2NmcServer.MainName + ' ' + NameExpansion;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.None, "Welcome to N2Nmc Server by LGF!");
        }

        private bool InitServer()
        {
            return Server.Init();
        }

        private void Run()
        {
            Server.Start();

            IsRunning = true;

            Server.ProcessV4TaskAsync();
            if (ServerConfig.Get("EnableIPv6", "1") == "1")
                Server.ProcessV6TaskAsync();

            while (true)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

            loop:
                if (!IsRunning)
                    break;

                stopwatch.Stop();

                Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "\n({1}) [{0}]>", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), stopwatch.Elapsed.ToString());

                string userInputLine = string.Empty;
                userInputLine = Server.ConsoleBuffer.ReadLine();

                stopwatch = Stopwatch.StartNew();
                string userInputLineFormatted = userInputLine.Trim();
                string[] cmd_Args = userInputLine.Split(' ');
                string cmd = cmd_Args[0];
                string cmdLower = cmd.ToLower();

                if (string.IsNullOrWhiteSpace(userInputLineFormatted))
                    goto loop;

                if (cmdLower == "clrscr")
                {
                    Server.ConsoleBuffer.MakeControl(ConsoleBuffer.ControlSymbols.ClearScreen);

                    goto loop;
                }

                if (cmdLower == "stop")
                {
                    Server.Stop();
                    IsRunning = false;

                    goto loop;
                }

                if (cmdLower == "list")
                {
                    DateTime now = DateTime.Now;
                    if (cmd_Args.Length <= 1)
                    {
                        for (int j = 0; j < Server.Rooms.Count; j++)
                        {
                            var room = Server.Rooms[j];
                            string? c = room.RoomCode, n = room.RoomName;
                            if (c == null || n == null)
                                throw new NullReferenceException();

                            Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "\n{0}[{1}] ToLastAccess: {2}\n",
                                n, c, now - room.CB.lastReqTime);
                        }

                        goto loop;
                    }
                    for (int i = 1; i < cmd_Args.Length; i++)
                        for (int j = 0; j < Server.Rooms.Count; j++)
                        {
                            var room = Server.Rooms[j];
                            if (room.RoomCode == cmd_Args[i])
                            {
                                string? c = room.RoomCode, n = room.RoomName;
                                if (c == null || n == null)
                                    throw new NullReferenceException();

                                StringBuilder stringBuilder = new StringBuilder();
                                stringBuilder.AppendFormat(
                                    "\n" +
                                    "[{0}] Name: {1}\n" +
                                    "Invisible: {2}\n" +
                                    "NeedPassword: {3}\n" +
                                    "Password: {4}\n" +
                                    "MajorColor: {7} MinorColor: {8}\n" +
                                    "ToLastAccess: {5}\n" +
                                    "AdminKey: {6}\n",

                                    c,
                                    n,
                                    room.IsRoomInvisible.ToString(),
                                    room.IsRoomPasswordNeeded.ToString(),
                                    room.RoomPassword, now - room.CB.lastReqTime,
                                    room.CB.AdminKey,
                                    room.colorMain.data.ToString("x"), room.colorMinor.data.ToString("x")
                                    );

                                Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, stringBuilder.ToString());

                                break;
                            }
                        }

                    goto loop;
                }

                if (cmdLower == "list_tcp")
                {
                    if (Server.tcpClients == null)
                        throw new NullReferenceException(nameof(Server.tcpClients));

                    try
                    {
                        foreach (var client in Server.tcpClients)
                        {
                            var c = client.client.Client;
                            if (c == null)
                                throw new NullReferenceException(nameof(c));
                            var ce = c.RemoteEndPoint;
                            if (ce == null)
                                throw new NullReferenceException(nameof(ce));

                            Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "\n{0}:{1} ({2}h)", ((IPEndPoint)ce).Address, ((IPEndPoint)ce).Port, (DateTime.Now - client.objTime).Hours);
                        }

                        goto loop;
                    }
                    catch (Exception ex) { Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Error, "Exception while listing tcp connections: {0}", ex.Message); goto loop; }

                }

                if (cmdLower == "close")
                {
                    if (cmd_Args.Length >= 2)
                    {
                        for (int i = 1; i < cmd_Args.Length; i++)
                            for (int j = 0; j < Server.Rooms.Count; j++)
                            {
                                if (Server.Rooms[j].RoomCode == cmd_Args[i])
                                {
                                    Server.Rooms.RemoveAt(j);
                                    break;
                                }
                            }
                    }
                    else
                        Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Error, "Need room code!\n");

                    goto loop;
                }

                if (cmdLower == "close_all")
                {
                    Server.Rooms.Clear();

                    goto loop;
                }

                if (cmdLower == "create")
                {
                    if (cmd_Args.Length >= 3)
                    {
                        Room r = new Room { RoomCode = cmd_Args[1], RoomName = cmd_Args[2] };
                        if (cmd_Args.Length >= 4)
                            r.IsRoomInvisible = cmd_Args[3] == "0" ? false : true;
                        if (cmd_Args.Length >= 5)
                            r.IsRoomPasswordNeeded = cmd_Args[4] == "0" ? false : true;
                        if (cmd_Args.Length >= 6)
                            r.RoomPassword = cmd_Args[5];
                        if (cmd_Args.Length >= 7)
                        {
                            UInt32 color = 0;
                            if (UInt32.TryParse(cmd_Args[6], System.Globalization.NumberStyles.HexNumber, null, out color))
                                r.colorMain = new RoomColor(color);
                        }
                        if (cmd_Args.Length >= 8)
                        {
                            UInt32 color = 0;
                            if (UInt32.TryParse(cmd_Args[7], System.Globalization.NumberStyles.HexNumber, null, out color))
                                r.colorMinor = new RoomColor(color);
                        }

                        Server.Rooms.Add(r);
                    }
                    else
                        Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Error, "Need room code && room name at least!\n format: create [code] [name] [IRI] [IRP] [PWD] [MajorColor] [MinorColor] \n example: Create 10cfd RoomABC 0 0 null ffffff a0efc0");

                    goto loop;
                }

                if (cmdLower == "save_rooms")
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i =0;i<Server.Rooms.Count;i++)
                    {
                        sb.AppendFormat("{0}|{1}|{2}|{3}|{4}|{5}|{6}", Server.Rooms[i].RoomCode, Server.Rooms[i].RoomName, Server.Rooms[i].IsRoomInvisible?'1':'0', Server.Rooms[i].IsRoomPasswordNeeded?'1':'0', Server.Rooms[i].RoomPassword, Server.Rooms[i].colorMain.data, Server.Rooms[i].colorMinor.data);
                        sb.AppendLine();
                    }
                    File.WriteAllText("Rooms", sb.ToString());

                    goto loop;
                }

                if (cmdLower == "load_rooms")
                {
                    string[] lines = File.ReadAllLines("Rooms");
                    foreach (string s in lines)
                    {
                        string[] infos = s.Split('|');
                        if (infos.Length ==7)
                            Server.Rooms.Add(new Room { RoomCode = infos[0], RoomName = infos[1],IsRoomInvisible= infos[2]=="0"?false:true, IsRoomPasswordNeeded = infos[3] == "0" ? false : true, RoomPassword = infos[4], colorMain = new RoomColor(UInt32.Parse(infos[5])), colorMinor = new RoomColor(UInt32.Parse(infos[6])) });
                    }

                    goto loop;
                }

                Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "Command '{0}' undefined\n", cmd);
                goto loop;
            }

            ServerConfig.SaveConfigDataToFile();
            Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Info, "Config Saved.\n");
            Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Info, "ServerStopped.\n");
        }


        public ServerInConsole(MineMP.ConsoleBuffer consoleBuffer, string configFilePath)
        {
            if (consoleBuffer == null)
                throw new ArgumentNullException(nameof(consoleBuffer));

            ServerConfig = new EasyConfig(configFilePath);
            IPAddress ipAddrV4 = IPAddress.Parse(ServerConfig.Get("IPv4", "0.0.0.0"));
            int portV4 = int.Parse(ServerConfig.Get("PortV4", "7476"));

            if (ServerConfig.Get("EnableIPv6", "1") == "1")
            {
                IPAddress ipAddrV6 = IPAddress.Parse(ServerConfig.Get("IPv6", "::"));
                int portV6 = int.Parse(ServerConfig.Get("PortV6", "7476"));

                Server = new Base.N2NmcServer(consoleBuffer, ipAddrV4, portV4, ipAddrV6, portV6);

                Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "Server Config Loaded: Listen {0}:{1}\n", ipAddrV4.ToString(), portV4);
                Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "Server Config Loaded: Listen [{0}]:{1}\n", ipAddrV6.ToString(), portV6);
            }
            else
            {
                Server = new Base.N2NmcServer(consoleBuffer, ipAddrV4, portV4);

                Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "Server Config Loaded: Listen {0}:{1}\n", ipAddrV4.ToString(), portV4);
            }
            
        }

        public bool Init()
        {
            InitConsole();

            if (!InitServer())
            {
                Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Error, "Cannot Init Server.\n");

                return false;
            }

            return true;
        }

        public void RunSync()
        {
            Run();
        }

        public async void RunAsync()
        {
            await Task.Run(() => Run());
        }
    }
}

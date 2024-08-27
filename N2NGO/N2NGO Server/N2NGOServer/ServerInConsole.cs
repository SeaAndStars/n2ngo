using MineMP;
using N2NGO.UtilsClass;
using N2NGO_Core.Objects;
using N2NGO_Core.Models;
using System.Diagnostics;
using System.Net;
using System.Text;
using static N2NGO_Server.N2NGOServer.Base.N2NGOServer;
using N2NGO_Core.Models.Server;
using System.Text.Json;

namespace N2NGO_Server.N2NGOServer
{
    internal class ServerInConsole
    {
        private static readonly string NameExpansion = "in Console";

        private bool _isRunning = false;
        private bool _isTerminating = false;

        public Dictionary<string, string> IdeaCommands { get; private set; } = new()
        {
            {"exit","stop" }
        };

        public EasyConfig ServerConfig { get; private set; }
        public Base.N2NGOServer Server { get; private set; }


        private void InitConsole()
        {
            Console.Title = Base.N2NGOServer.MainName + ' ' + NameExpansion;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.None, "Welcome to N2N GO Server by LGF!");
        }

        private bool InitServer()
        {
            return Server.Init();
        }

        private void Run()
        {
            Server.Start();

            _isRunning = true;

            Server.ProcessV4TaskAsync();
            if (ServerConfig.Get("EnableIPv6", "1") != "0")
                Server.ProcessV6TaskAsync();

            while (true)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

            loop:
                try
                {
                    if (!_isRunning)
                        break;
                    if (_isTerminating)
                    {
                        List<ClientControlBlock> aliveClients = new();
                        if (Server.ConnectionTcpClients.Count > 0)
                        {
                            foreach (var clientControlBlock in Server.ConnectionTcpClients)
                            {
                                if (clientControlBlock.ClientHandlerThread.IsAlive)
                                    aliveClients.Add(clientControlBlock);
                            }
                        }

                        if (aliveClients.Count == 0)
                        {
                            _isRunning = false;
                            goto loop;
                        }

                        Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Info, $"Waiting for all client connection threads to be completed...({aliveClients.Count} left)");
                        Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Info, "Or force terminate? [y/N] (Y)Yes (N)Retry in 1 second");

                        var nextCommand = Server.ConsoleBuffer.ReadLine();
                        switch (nextCommand.ToLower())
                        {
                            default: break;

                            case "y":
                                {
                                    foreach (var client in aliveClients)
                                        (client.ClientConnectionData["_ThreadCancellationTokenSource"] as CancellationTokenSource ?? throw new("client.ClientConnectionData[\"_ThreadCancellationTokenSource\"]")).Cancel(false);

                                    break;
                                }

                            case "n":
                                {
                                    break;
                                }
                        }

                        Thread.Sleep(1000);
                        goto loop;
                    }


                    stopwatch.Stop();

                    Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "\n({1}) [{0}]>", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), stopwatch.Elapsed.ToString());

                    string userInputLine = Server.ConsoleBuffer.ReadLine();

                    stopwatch = Stopwatch.StartNew();
                    string userInputLineFormatted = userInputLine.Trim();
                    List<string> cmd_Args = new(userInputLine.Split(' '));
                    string cmd = cmd_Args[0];
                    string cmdLower = cmd.ToLower();

                    if (string.IsNullOrWhiteSpace(userInputLineFormatted))
                        goto loop;

                    // Commands
                    {
                        if (cmdLower == "clrscr")
                        {
                            Server.ConsoleBuffer.MakeControl(ConsoleBuffer.ControlSymbols.ClearScreen);

                            goto loop;
                        }

                        if (cmdLower == "stop")
                        {
                            Server.Stop();
                            _isTerminating = true;

                            goto loop;
                        }

                        if (cmdLower == "list")
                        {
                            DateTime now = DateTime.Now;
                            if (cmd_Args.Count <= 1)
                            {
                                for (int j = 0; j < Server.Rooms.Count; j++)
                                {
                                    var room = Server.Rooms[j];
                                    var code = room.RoomCode ?? throw new($"RoomCode of '{room.RoomCode}' is null");
                                    var name = room.RoomName ?? throw new($"RoomName of '{room.RoomCode}' is null");

                                    Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "\n{0}[{1}] ({2}/{3}) ToLastAccess: {4} | ActivatedLifetime: {5}\n",
                                        name, code, room.Members.Count, room.RuledMembers.Count, now - room.CB.LastActivatedTime, room.CB.ActivatedLifetime);
                                }

                                goto loop;
                            }
                            for (int i = 1; i < cmd_Args.Count; i++)
                                for (int j = 0; j < Server.Rooms.Count; j++)
                                {
                                    var room = Server.Rooms[j];
                                    if (room.RoomCode == cmd_Args[i])
                                    {
                                        var code = room.RoomCode ?? throw new($"RoomCode of '{room.RoomCode}' is null");
                                        var name = room.RoomName ?? throw new($"RoomName of '{room.RoomCode}' is null");
                                        var iri = room.IsRoomInvisible ?? throw new($"IsRoomInvisible of '{room.RoomCode}' is null");
                                        var ipn = room.IsRoomPasswordNeeded ?? throw new($"IsRoomPasswordNeeded of '{room.RoomCode}' is null");
                                        var pwd = room.RoomPassword ?? throw new($"RoomPassword of '{room.RoomCode}' is null");
                                        var mainColor = room.MainColor ?? throw new($"MainColor of '{room.RoomCode}' is null");
                                        var minorColor = room.MinorColor ?? throw new($"MinorColor of '{room.RoomCode}' is null");

                                        StringBuilder stringBuilder = new();
                                        stringBuilder.Append(
                                            "\n" +
                                            $"[{code}] ({room.Members.Count}/{room.RuledMembers.Count})  Name: {name}\n" +
                                            $"Invisible: {iri.ToString()}\n" +
                                            $"NeedPassword: {ipn.ToString()}\n" +
                                            $"Password: {pwd}\n" +
                                            $"MainColor: {mainColor.data.ToString("x")} MinorColor: {minorColor.data.ToString("x")}\n" +
                                            $"ToLastAccess: {now - room.CB.LastActivatedTime}\n" +
                                            $"ActivatedLifetime: {room.CB.ActivatedLifetime}\n" +
                                            $"AdminKeys: \n{()=> { string final = ""; foreach (var key in room.CB.AdminKey) final += $"  {key}"; }}\n"
                                            );

                                        Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, stringBuilder.ToString());

                                        break;
                                    }
                                }

                            goto loop;
                        }

                        if (cmdLower == "list_tcp")
                        {
                            if (Server.ConnectionTcpClients == null)
                                throw new NullReferenceException(nameof(Server.ConnectionTcpClients));

                            try
                            {
                                long index = 0;
                                foreach (var client in Server.ConnectionTcpClients)
                                {
                                    var c = client.ConnectionClient.Client;
                                    if (c == null)
                                        throw new NullReferenceException(nameof(c));
                                    var ce = c.RemoteEndPoint;
                                    if (ce == null)
                                        throw new NullReferenceException(nameof(ce));

                                    Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "\n[{4}] {0}:{1} ({2})\nUserKey: {3}\n", ((IPEndPoint)ce).Address, ((IPEndPoint)ce).Port, (DateTime.Now - client.AccessTime).ToString("hh\\:mm\\:ss"), client.ClientConnectionData["UserSessionKey"] as string ?? "null", index);

                                    index++;
                                }

                                goto loop;
                            }
                            catch (Exception ex) { Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Error, "Exception while listing tcp connections: {0}", ex.Message); goto loop; }

                        }

                        if (cmdLower == "close")
                        {
                            if (cmd_Args.Count >= 2)
                            {
                                for (int i = 1; i < cmd_Args.Count; i++)
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
                            if (cmd_Args.Count >= 3)
                            {
                                Room room = new() { RoomCode = cmd_Args[1], RoomName = cmd_Args[2], IsRoomInvisible = false, IsRoomPasswordNeeded = false, MainColor = new(), MinorColor = new() };
                                if (cmd_Args.Count >= 4)
                                    room.IsRoomInvisible = cmd_Args[3] != "0";
                                if (cmd_Args.Count >= 5)
                                    room.IsRoomPasswordNeeded = cmd_Args[4] != "0";
                                if (cmd_Args.Count >= 6)
                                    room.RoomPassword = cmd_Args[5];
                                if (cmd_Args.Count >= 7)
                                {
                                    if (UInt32.TryParse(cmd_Args[6], System.Globalization.NumberStyles.HexNumber, null, out var color))
                                        room.MainColor = new RoomColor(color);
                                }
                                if (cmd_Args.Count >= 8)
                                {
                                    if (UInt32.TryParse(cmd_Args[7], System.Globalization.NumberStyles.HexNumber, null, out var color))
                                        room.MinorColor = new RoomColor(color);
                                }
                                room.InitHandler();
                                Server.Rooms.Add(room);
                            }
                            else
                                Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Error, "Need room code && room name at least!\n format: create [code] [name] [IRI] [IRP] [PWD] [MainColor] [MinorColor] \n example: Create 10cfd RoomABC 0 0 null ffffff a0efc0");

                            goto loop;
                        }

                        if (cmdLower == "save_rooms")
                        {
                            lock (Server.Rooms)
                                File.WriteAllText("Rooms.json", JsonSerializer.Serialize<List<Room>>(Server.Rooms));

                            goto loop;
                        }

                        if (cmdLower == "load_rooms")
                        {
                            var rooms = JsonSerializer.Deserialize<List<Room>>(File.ReadAllText("Rooms.json"));
                            if (rooms is null)
                                throw new("Cannot cast data to List<Room> from file 'Rooms.json'");
                            lock (Server.Rooms)
                                foreach (var room in rooms)
                                {
                                    room.InitHandler();
                                    Server.Rooms.Add(room);
                                }

                            goto loop;
                        }

                        if (cmdLower == "disconnect")
                        {
                            if (cmd_Args.Count >= 2)
                            {
                                var args = cmd_Args;

                                bool ask = true;
                                if (args.Count > 2)
                                {
                                    foreach (var arg in args)
                                    {
                                        if (arg == "-y" || arg == "--no-ask")
                                        {
                                            ask = false;
                                            args.Remove(arg);
                                            break;
                                        }
                                    }
                                }

                                List<ClientControlBlock> targetClients = new();
                                for (int i = 1; i < args.Count; i++)
                                {
                                    long index = 0;
                                    foreach (var client in Server.ConnectionTcpClients)
                                    {
                                        if (index == int.Parse(args[i]))
                                        {
                                            targetClients.Add(client);
                                            break;
                                        }

                                        index++;
                                    }
                                }

                                foreach (var client in targetClients)
                                {
                                    if (ask)
                                    {
                                        var c = client.ConnectionClient.Client;
                                        if (c == null)
                                            throw new NullReferenceException(nameof(c));
                                        var ce = c.RemoteEndPoint;
                                        if (ce == null)
                                            throw new NullReferenceException(nameof(ce));
                                        Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "\nClose client? [y/N] (Y)Yes (N)No\n{0}:{1} ({2})\nUserKey: {3}\n", ((IPEndPoint)ce).Address, ((IPEndPoint)ce).Port, (DateTime.Now - client.AccessTime).ToString("hh\\:mm\\:ss"), client.ClientConnectionData["UserSessionKey"] as string ?? "null");
                                   
                                        var nextCommand = Server.ConsoleBuffer.ReadLine(emptyBuffer: true);
                                        switch (nextCommand.ToLower())
                                        {
                                            default: break;

                                            case "y":
                                                {
                                                    (client.ClientConnectionData["_ThreadCancellationTokenSource"] as CancellationTokenSource ?? throw new("client.ClientConnectionData[\"_ThreadCancellationTokenSource\"]")).Cancel(false);
                                                    break;
                                                }

                                            case "n":
                                                {
                                                    break;
                                                }
                                        }
                                    }
                                    else
                                    {
                                        (client.ClientConnectionData["_ThreadCancellationTokenSource"] as CancellationTokenSource ?? throw new("client.ClientConnectionData[\"_ThreadCancellationTokenSource\"]")).Cancel(false);
                                    }
                                }
                            }
                            else
                            {
                                Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Warn, "Close all client connections? [y/N] (Y)Yes (N)No\n");
                                var nextCommand = Server.ConsoleBuffer.ReadLine(emptyBuffer: true);
                                switch (nextCommand.ToLower())
                                {
                                    default: break;

                                    case "y":
                                        {
                                            List<ClientControlBlock> aliveClients = new();
                                            if (Server.ConnectionTcpClients.Count > 0)
                                            {
                                                foreach (var clientControlBlock in Server.ConnectionTcpClients)
                                                {
                                                    if (clientControlBlock.ClientHandlerThread.IsAlive)
                                                        aliveClients.Add(clientControlBlock);
                                                }
                                            }

                                            foreach (var client in aliveClients)
                                                (client.ClientConnectionData["_ThreadCancellationTokenSource"] as CancellationTokenSource ?? throw new("client.ClientConnectionData[\"_ThreadCancellationTokenSource\"]")).Cancel(false);

                                            break;
                                        }

                                    case "n":
                                        {
                                            break;
                                        }
                                }
                            }

                            goto loop;
                        }
                    }

                    void UndefinedCommandHandler()
                    {
                        // ServerStatus
                        Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "Command '{0}' undefined\n", cmd);

                        // Idea
                        if (IdeaCommands.ContainsKey(cmdLower))
                            Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "Do you mean '{0}'?\n", IdeaCommands[cmdLower]);
                    }
                    UndefinedCommandHandler();
                }
                catch (Exception e)
                {
                    Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Error, $"Unhandled Exception: {e}\n");
                    goto loop;
                }
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

            if (ServerConfig.Get("EnableIPv6", "1") != "0")
            {
                IPAddress ipAddrV6 = IPAddress.Parse(ServerConfig.Get("IPv6", "::"));
                int portV6 = int.Parse(ServerConfig.Get("PortV6", "7476"));

                Server = new Base.N2NGOServer(consoleBuffer, ipAddrV4, portV4, ipAddrV6, portV6);

                Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "Server Config Loaded: Listen {0}:{1}\n", ipAddrV4.ToString(), portV4);
                Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "Server Config Loaded: Listen [{0}]:{1}\n", ipAddrV6.ToString(), portV6);
            }
            else
            {
                Server = new Base.N2NGOServer(consoleBuffer, ipAddrV4, portV4);

                Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "Server Config Loaded: Listen {0}:{1}\n", ipAddrV4.ToString(), portV4);
            }

        }

        /// <summary>
        /// Initialize Server & Console components
        /// </summary>
        /// <returns>true if successful</returns>
        public bool Initialize()
        {
            InitConsole();

            if (!InitServer())
            {
                Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Error, "Cannot Init Server.\n");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Start running server on current thread
        /// </summary>
        public void RunSync()
        {
            Run();
        }

        /// <summary>
        /// Start running server asynchronously
        /// </summary>
        public async void RunAsync()
        {
            await Task.Run(() => Run());
        }
    }
}

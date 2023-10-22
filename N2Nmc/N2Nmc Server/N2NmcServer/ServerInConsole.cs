using MineMP;
using N2Nmc_Protocol.Objects;
using System.Net;
using System.Text;

namespace N2Nmc_Server.N2NmcServer
{
    internal class ServerInConsole
    {
        private static readonly string NameExpansion = "in Console";

        private bool IsRunning = false;

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

            Server.ProcessAsync();

            Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "Server Listening on port: {0}\n", Server.port);

            while (true)
            {
            loop:
                if (!IsRunning)
                    break;

                Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Info, "\n>");

                string userInputLine = string.Empty;
                userInputLine = Server.ConsoleBuffer.ReadLine();

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
                            string? c= Server.Rooms[j].RoomCode;
                            if (c==null)
                                throw new NullReferenceException();

                            Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "\n[{0}] ToLastAccess: {1}\n",
                                c, now - Server.Rooms[j].CB.lastReqTime);
                        }

                        goto loop;
                    }
                    for (int i = 1; i < cmd_Args.Length; i++)
                        for (int j = 0; j < Server.Rooms.Count; j++)
                        {
                            if (Server.Rooms[j].RoomCode == cmd_Args[i])
                            {
                                string? c = Server.Rooms[j].RoomCode, n = Server.Rooms[j].RoomName;
                                if (c == null || n == null)
                                    throw new NullReferenceException();

                                Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "\n[{0}] Name: {1}\nIRI: {2}\nIRP: {3}\nPassword: {4}\n ToLastAccess: {5}\n",
                                    c, n, Server.Rooms[j].IsRoomInvisible.ToString(), Server.Rooms[j].IsRoomPasswordNeeded.ToString(), Server.Rooms[j].RoomPassword, now - Server.Rooms[j].CB.lastReqTime);
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
                        if (cmd_Args.Length < 2)
                        {
                            for (int j = 0; j < Server.tcpClients.Count; j++)
                            {
                                var c = Server.tcpClients[j].client.Client;
                                if (c == null)
                                    throw new NullReferenceException(nameof(c));
                                var ce = c.RemoteEndPoint;
                                if (ce == null)
                                    throw new NullReferenceException(nameof(ce));

                                Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "\n[{0}] {1}:{2}", ((IPEndPoint)ce).Address, ((IPEndPoint)ce).Port);
                            }

                            goto loop;
                        }
                        for (int i = 1; i < cmd_Args.Length; i++)
                        {
                            var c = Server.tcpClients[int.Parse(cmd_Args[i])].client.Client;
                            if (c == null)
                                throw new NullReferenceException(nameof(c));
                            var ce = c.RemoteEndPoint;
                            if (ce == null)
                                throw new NullReferenceException(nameof(ce));

                            Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "\n[{0}] {1}:{2}", ((IPEndPoint)ce).Address, ((IPEndPoint)ce).Port);
                        }
                    }
                    catch (Exception ex) { Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Error, "Exception while listing tcp connections: {0}", ex.Message);goto loop; }

                    goto loop;
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

                        Server.Rooms.Add(r);
                    }
                    else
                        Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Error, "Need room code && room name at least!\n format: create [code] [name] [IRI] [IRP] [PWD] \n example: Create 10cfd RoomABC 0 0 null");

                    goto loop;
                }

                if (cmdLower == "save_rooms")
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i =0;i<Server.Rooms.Count;i++)
                    {
                        sb.AppendFormat("{0}|{1}|{2}|{3}|{4}", Server.Rooms[i].RoomCode, Server.Rooms[i].RoomName, Server.Rooms[i].IsRoomInvisible?'1':'0', Server.Rooms[i].IsRoomPasswordNeeded?'1':'0', Server.Rooms[i].RoomPassword);
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
                        if (infos.Length ==5)
                            Server.Rooms.Add(new Room { RoomCode = infos[0], RoomName = infos[1],IsRoomInvisible= infos[2]=="0"?false:true, IsRoomPasswordNeeded = infos[3] == "0" ? false : true, RoomPassword = infos[4] });
                    }

                    goto loop;
                }

                Server.ConsoleBuffer.AppendFormatBuffer(ConsoleBuffer.BufferContentType.Info, "Command '{0}' undefined\n", cmd);
                goto loop;
            }

            Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Info, "ServerStopped.");
        }


        [Obsolete]
        public ServerInConsole(MineMP.ConsoleBuffer consoleBuffer)
        {
            if (consoleBuffer == null)
                throw new ArgumentNullException(nameof(consoleBuffer));

            Server = new Base.N2NmcServer(consoleBuffer);
        }

        public bool Init()
        {
            InitConsole();

            if (!InitServer())
            {
                Server.ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Error, "Cannot Init Server.");

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

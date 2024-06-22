using System.Net;
using System.Net.Sockets;
using static N2Nmc_Protocol.Protocol;
using static N2Nmc_Protocol.Package;
using N2Nmc_Protocol;
using N2Nmc_Protocol.Objects;
using System.Linq;
using System.Drawing;
using MineMP;

namespace N2Nmc_Server.N2NmcServer.Base
{
    internal class N2NmcServer
    {
        public static readonly string MainName = "N2N GO Server";
        public int port = N2Nmc_Protocol.UserDef.ExternServerOptions.N2Nmc_Server_Port; // TODO: port

        public List<Room> Rooms = new List<Room>();

        public MineMP.ConsoleBuffer ConsoleBuffer { get; private set; }

        private System.Timers.Timer CCB_GC = new System.Timers.Timer { Interval = 3000, AutoReset = true };

        public enum Status
        {
            Stopped = 0,
            Initialization,
            Initialized,
            Starting,
            Running,
            Stopping
        }

        public Status status { get; private set; } = Status.Stopped;

        public struct ClientControlBlock
        {
            public DateTime objTime { get; private set; }

            public TcpClient client { get; private set; }
            public Thread clientHandlerThread { get; private set; }

            public ClientControlBlock(TcpClient client, Action<TcpClient> handlerFunc, bool startHandler = true)
            {
                objTime = DateTime.Now;

                this.client = client;
                clientHandlerThread = new(() => handlerFunc(client));

                if (startHandler)
                    clientHandlerThread.Start();
            }

            public void Start()
            {
                clientHandlerThread.Start();
            }
        }

        public TcpListener? serverv4 { get; private set; }
        public TcpListener? serverv6 { get; private set; }
        public List<ClientControlBlock>? tcpClients { get; private set; }
        public List<Task> listenerTasks { get; private set; } = new List<Task>();


        public N2NmcServer(MineMP.ConsoleBuffer consoleBuffer, IPAddress ip, int port)
        {
            ConsoleBuffer = consoleBuffer;

            serverv4 = new TcpListener(ip, port);
            tcpClients = new();

            CCB_GC.Elapsed += CCB_GC_Elapsed;
            CCB_GC.Enabled = true;
        }
        public N2NmcServer(MineMP.ConsoleBuffer consoleBuffer, IPAddress ip, int port, IPAddress ipv6, int portv6) : this(consoleBuffer, ip, port)
        {
            serverv6 = new TcpListener(ipv6, portv6);
        }

        public N2NmcServer(MineMP.ConsoleBuffer consoleBuffer, TcpListener tcpListener)
        {
            ConsoleBuffer = consoleBuffer;

            serverv4 = tcpListener;
            tcpClients = new();

            CCB_GC.Elapsed += CCB_GC_Elapsed;
            CCB_GC.Enabled = true;
        }
        public N2NmcServer(MineMP.ConsoleBuffer consoleBuffer, TcpListener tcpListener, TcpListener tcpListenerv6) : this(consoleBuffer, tcpListener)
        {
            serverv6 = tcpListenerv6;
        }

        private void CCB_GC_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (tcpClients == null)
                throw new NullReferenceException(nameof(tcpClients));

            lock (tcpClients)
                for (int i = 0; i < tcpClients.Count; i++)
                    if (!tcpClients[i].clientHandlerThread.IsAlive)
                        tcpClients.Remove(tcpClients[i]);
        }

        public bool Init()
        {
            if (status != Status.Stopped)
            {
                ConsoleBuffer.AppendBuffer(MineMP.ConsoleBuffer.BufferContentType.Warn, "Init server cancelled due to status.\n");
                return false;
            }

            status = Status.Initialization;

            serverv4?.Stop();
            serverv6?.Stop();

            status = Status.Initialized;

            return true;
        }

        public void Start()
        {
            if (status != Status.Initialized)
            {
                ConsoleBuffer.AppendBuffer(MineMP.ConsoleBuffer.BufferContentType.Warn, "Start server cancelled due to status.\n");
                return;
            }

            status = Status.Starting;

            serverv4?.Start();
            serverv6?.Start();

            CCB_GC.Start();

            status = Status.Running;
        }

        private byte NetworkStreamBlockReadingByte(NetworkStream stream)
        {
            byte[] bytes = new byte[1];
            stream.Read(bytes, 0, 1);

            return bytes[0];
        }

        private void TcpClientHandler(TcpClient client)
        {
            IPEndPoint? iPEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            if (iPEndPoint == null)
                return;

            NetworkStream stream = client.GetStream();

            string userKey = Room.RandomKeyString();
            Room? currentRoom = null;

            while (true)
            {
                try
                {
                    if (!client.Connected) break;

                    Package client_package = ResolvePackage(new byte[] { NetworkStreamBlockReadingByte(stream) });


                    switch ((BaseHeader)client_package.Header)
                    {
                        case BaseHeader.undefined:
                            {
                                Package package = MakePackage(BaseHeader.InvalidClient);
                                stream.Write(BuildPackage(package));
                                stream.Flush();
                                client.Close();
                                break;
                            }

                        case BaseHeader.peek:
                            {
                                Package package = MakePackage(BaseHeader.peek_ok);
                                stream.Write(BuildPackage(package));
                                stream.Flush();
                                break;
                            }

                        // TODO: Obsolete Protocol BaseHeader._ver_check
                        case BaseHeader._ver_check:
                            {
                                Package package = MakePackage(BaseHeader.msg_string, MsgExternalData.Encode.MsgString(SharedData.N2NmcClientLatestVersion.ToString()));
                                stream.Write(BuildPackage(package));
                                stream.Flush();
                                break;
                            }
                        case BaseHeader._user_key_get:
                            {
                                IO_Tool iO_Tool = new();

                                if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_string_long, MsgExternalData.Encode.MsgStringLong(userKey))))
                                    goto RemoveClient;

                                break;
                            }
                        case BaseHeader._pull_online_total:
                            {
                                IO_Tool iO_Tool = new();

                                var total = tcpClients != null ? tcpClients.Count : 0;

                                if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, MsgExternalData.Encode.MsgString(total.ToString()))))
                                    goto RemoveClient;

                                break;
                            }
                        case BaseHeader._rooms_pull_rooms_pages:
                            {
                                IO_Tool iO_Tool = new();

                                var total = Rooms.Where(_ => !_.IsRoomInvisible && _.RoomCode != null).ToList().Count;
                                var pages = (total < _PullRoomsRoomsTake) ? 1 : ((total % _PullRoomsRoomsTake > 0) ? (total / _PullRoomsRoomsTake + 1) : (total / _PullRoomsRoomsTake));

                                if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulong, MsgExternalData.Encode.MsgULong((UInt32)pages))))
                                    goto RemoveClient;

                                break;
                            }
                        case BaseHeader._rooms_pull_rooms:
                            {
                                IO_Tool iO_Tool = new();

                                UInt32 page_index = 0;

                                Package? pkg_app = iO_Tool.Receive(client);
                                if (pkg_app != null)
                                    if (pkg_app.Value.Header == (byte)BaseHeader.msg_ulong && pkg_app.Value.external_data != null)
                                        page_index = Package.MsgExternalData.Decode.MsgULong(pkg_app.Value.external_data);
                                    else goto RemoveClient;
                                else goto RemoveClient;

                                var _rooms = Rooms.Where(_ => !_.IsRoomInvisible && _.RoomCode != null).ToList();   // Do not take invisible room
                                var rooms = _rooms.Skip((int)(_PullRoomsRoomsTake * page_index)).Take((int)_PullRoomsRoomsTake).ToList();
                                int roomsCount = rooms.Count;

                                goto SendRoomsCount;

                            // SendRoomsCount
                            SendRoomsCount:
                                {
                                    if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulong, MsgExternalData.Encode.MsgULong((UInt32)roomsCount))))
                                        goto RemoveClient;

                                    goto SendRooms;
                                }
                            // SendRooms
                            SendRooms:
                                {
                                    for (int i = 0; i < roomsCount; i++)
                                    {
                                        var _code = rooms[i].RoomCode;
                                        var _name = rooms[i].RoomName;
                                        if (rooms[i].IsRoomInvisible || _code == null)
                                            throw new Exception("Invalid Rooms");

                                        string Code = _code;
                                        string Name = _name == null ? string.Empty : _name;
                                        bool PN = Rooms[i].IsRoomPasswordNeeded;

                                        if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, MsgExternalData.Encode.MsgString(Code))))
                                            goto RemoveClient;
                                        if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, MsgExternalData.Encode.MsgString(Name))))
                                            goto RemoveClient;
                                        if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_byte, MsgExternalData.Encode.MsgByte((byte)(PN ? 1 : 0)))))
                                            goto RemoveClient;
                                        if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulong, MsgExternalData.Encode.MsgULong(Rooms[i].colorMain.data))))
                                            goto RemoveClient;
                                        if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulong, MsgExternalData.Encode.MsgULong(Rooms[i].colorMinor.data))))
                                            goto RemoveClient;
                                    }
                                }

                                break;
                            }
                        case BaseHeader._rooms_create:
                            {
                                IO_Tool iO_Tool = new();

                                string code, name;
                                bool IRI, IRP;
                                string passwd;
                                UInt32 MainColor;
                                UInt32 MinorColor;

                                Package? pkg_app = iO_Tool.Receive(client);
                                if (pkg_app != null)
                                    if (pkg_app.Value.Header == (byte)BaseHeader.msg_string && pkg_app.Value.external_data != null)
                                        code = Package.MsgExternalData.Decode.MsgString(pkg_app.Value.external_data);
                                    else goto RemoveClient;
                                else goto RemoveClient;
                                pkg_app = iO_Tool.Receive(client);
                                if (pkg_app != null)
                                    if (pkg_app.Value.Header == (byte)BaseHeader.msg_string && pkg_app.Value.external_data != null)
                                        name = Package.MsgExternalData.Decode.MsgString(pkg_app.Value.external_data);
                                    else goto RemoveClient;
                                else goto RemoveClient;

                                pkg_app = iO_Tool.Receive(client);
                                if (pkg_app != null)
                                    if (pkg_app.Value.Header == (byte)BaseHeader.msg_byte && pkg_app.Value.external_data != null)
                                        IRI = Package.MsgExternalData.Decode.MsgByte(pkg_app.Value.external_data) == 1 ? true : false;
                                    else goto RemoveClient;
                                else goto RemoveClient;
                                pkg_app = iO_Tool.Receive(client);
                                if (pkg_app != null)
                                    if (pkg_app.Value.Header == (byte)BaseHeader.msg_byte && pkg_app.Value.external_data != null)
                                        IRP = Package.MsgExternalData.Decode.MsgByte(pkg_app.Value.external_data) == 1 ? true : false;
                                    else goto RemoveClient;
                                else goto RemoveClient;

                                pkg_app = iO_Tool.Receive(client);
                                if (pkg_app != null)
                                    if (pkg_app.Value.Header == (byte)BaseHeader.msg_string && pkg_app.Value.external_data != null)
                                        passwd = Package.MsgExternalData.Decode.MsgString(pkg_app.Value.external_data);
                                    else goto RemoveClient;
                                else goto RemoveClient;

                                pkg_app = iO_Tool.Receive(client);
                                if (pkg_app != null)
                                    if (pkg_app.Value.Header == (byte)BaseHeader.msg_ulong && pkg_app.Value.external_data != null)
                                        MainColor = Package.MsgExternalData.Decode.MsgULong(pkg_app.Value.external_data);
                                    else goto RemoveClient;
                                else goto RemoveClient;
                                pkg_app = iO_Tool.Receive(client);
                                if (pkg_app != null)
                                    if (pkg_app.Value.Header == (byte)BaseHeader.msg_ulong && pkg_app.Value.external_data != null)
                                        MinorColor = Package.MsgExternalData.Decode.MsgULong(pkg_app.Value.external_data);
                                    else goto RemoveClient;
                                else goto RemoveClient;

                                lock (Rooms)
                                    for (int i = 0; i < Rooms.Count; i++)
                                    {
                                        if (Rooms[i].RoomCode == MsgExternalData.Decode.MsgString(pkg_app.Value.external_data))
                                        {
                                            iO_Tool.Send(client, MakePackage(BaseHeader.InvalidClient));
                                            goto RemoveClient;
                                        }
                                    }

                                var newRoom = new Room { RoomCode = code, RoomName = name, IsRoomInvisible = IRI, IsRoomPasswordNeeded = IRP, RoomPassword = passwd, colorMain = new RoomColor(MainColor), colorMinor = new RoomColor(MinorColor) };
                                newRoom.CB.AdminKey = userKey;
                                lock (Rooms)
                                    Rooms.Add(newRoom);

                                iO_Tool.Send(client, MakePackage(BaseHeader.msg_string_long, Package.MsgExternalData.Encode.MsgStringLong(newRoom.CB.AdminKey)));
                                iO_Tool.Send(client, MakePackage(BaseHeader.msg_ok));
                                break;
                            }
                        case BaseHeader._rooms_is_code_exists:
                            {
                                IO_Tool iO_Tool = new();
                                Package? pkg_app = iO_Tool.Receive(client, (byte)BaseHeader.msg_string);
                                if (pkg_app != null)
                                    if (pkg_app.Value.external_data != null)
                                    {
                                        bool ex = false;
                                        lock (Rooms)
                                            for (int i = 0; i < Rooms.Count; i++)
                                            {
                                                if (Rooms[i].RoomCode == MsgExternalData.Decode.MsgString(pkg_app.Value.external_data)) ex = true;
                                            }
                                        iO_Tool.Send(client, MakePackage(BaseHeader.msg_byte, MsgExternalData.Encode.MsgByte((byte)(ex ? 1 : 0))));
                                        break;
                                    }

                                goto RemoveClient;
                            }
                        case BaseHeader._rooms_get_name:
                            {
                                IO_Tool iO_Tool = new();
                                Package? pkg_app = iO_Tool.Receive(client);
                                if (pkg_app != null)
                                    if (pkg_app.Value.Header == (byte)BaseHeader.msg_string && pkg_app.Value.external_data != null)
                                    {
                                        string code = Package.MsgExternalData.Decode.MsgString(pkg_app.Value.external_data);

                                        lock (Rooms)
                                            for (int i = 0; i < Rooms.Count; i++)
                                            {
                                                if (Rooms[i].RoomCode == code)
                                                {
                                                    var name = Rooms[i].RoomName;
                                                    if (name != null)
                                                        if (iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, MsgExternalData.Encode.MsgString(name))))
                                                            break;
                                                        else
                                                            goto RemoveClient;
                                                }
                                            }

                                        break;
                                    }

                                goto RemoveClient;
                            }

                        case BaseHeader._room_client_join:
                            {
                                IO_Tool iO_Tool = new();

                                if (currentRoom != null)
                                {
                                    iO_Tool.Send(client, MakePackage(BaseHeader._room_client_join_fail_0));
                                    break;
                                }

                                string RoomCode, RoomPassword;
                                string NickName;

                                Package? pkg_app = iO_Tool.Receive(client);
                                if (pkg_app != null)
                                    if (pkg_app.Value.Header == (byte)BaseHeader.msg_string && pkg_app.Value.external_data != null)
                                        RoomCode = Package.MsgExternalData.Decode.MsgString(pkg_app.Value.external_data);
                                    else goto RemoveClient;
                                else goto RemoveClient;
                                pkg_app = iO_Tool.Receive(client);
                                if (pkg_app != null)
                                    if (pkg_app.Value.Header == (byte)BaseHeader.msg_string && pkg_app.Value.external_data != null)
                                        RoomPassword = Package.MsgExternalData.Decode.MsgString(pkg_app.Value.external_data);
                                    else goto RemoveClient;
                                else goto RemoveClient;
                                pkg_app = iO_Tool.Receive(client);
                                if (pkg_app != null)
                                    if (pkg_app.Value.Header == (byte)BaseHeader.msg_string && pkg_app.Value.external_data != null)
                                        NickName = Package.MsgExternalData.Decode.MsgString(pkg_app.Value.external_data);
                                    else goto RemoveClient;
                                else goto RemoveClient;

                                lock (Rooms)
                                    for (int i = 0; i < Rooms.Count; i++)
                                    {
                                        var room = Rooms[i];
                                        if (room.RoomCode == MsgExternalData.Decode.MsgString(pkg_app.Value.external_data))
                                        {
                                            if (room.IsRoomPasswordNeeded)
                                            {
                                                if (RoomPassword != room.RoomPassword)
                                                {
                                                    iO_Tool.Send(client, MakePackage(BaseHeader._room_client_join_fail_2));
                                                    goto room_client_join_fail;
                                                }
                                            }

                                            var rCreateMember = room.CreateMember(NickName, userKey, "Unknown");
                                            var rJoin = room.MemberJoin(rCreateMember.Item1);
                                            if (rJoin != 0)
                                            {
                                                iO_Tool.Send(client, MakePackage(BaseHeader._room_client_join_fail_00));
                                                iO_Tool.Send(client, MakePackage(BaseHeader.msg_long, Package.MsgExternalData.Encode.MsgLong(rJoin)));
                                                goto room_client_join_fail;
                                            }

                                            iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(userKey)));

                                            goto room_client_join_ok;
                                        }
                                    }

                                iO_Tool.Send(client, MakePackage(BaseHeader._room_client_join_fail_1));
                                break;

                            room_client_join_fail:
                                break;

                            room_client_join_ok:
                                break;
                            }

                        case BaseHeader.extended_package:
                            {
                                Package package = MakePackage(BaseHeader.NotImplemented);
                                stream.Write(BuildPackage(package));
                                stream.Flush();
                                break;
                            }

                        default:
                            {
                                Package package = MakePackage(BaseHeader.NotImplemented);
                                stream.Write(BuildPackage(package));
                                stream.Flush();
                                break;
                            }
                    }
                }
                catch (IOException ex)
                {
                    if (ex.InnerException != null)
                    {
                        var socketException = ex.InnerException as SocketException;
                        if (socketException != null)
                        {
                            if (socketException.SocketErrorCode == SocketError.ConnectionReset)
                                goto RemoveClientNoEcho;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ConsoleBuffer.AppendFormatBuffer(MineMP.ConsoleBuffer.BufferContentType.Info, "Connection: {0}:{1} remove due to exception: {2}" + Environment.NewLine, iPEndPoint.Address.ToString(), iPEndPoint.Port.ToString(), ex.Message);
                    goto RemoveClient;
                }
                continue;

            RemoveClient:
                ConsoleBuffer.AppendFormatBuffer(MineMP.ConsoleBuffer.BufferContentType.Info, "Connection: {0}:{1} remove jmp" + Environment.NewLine, iPEndPoint.Address.ToString(), iPEndPoint.Port.ToString());
            RemoveClientNoEcho:
                client.Close();
                client.Dispose();
                return;
            }
        }

        public void ProcessV4()
        {
            if (tcpClients == null)
                throw new ArgumentNullException(nameof(tcpClients));

            if (serverv4 == null)
                throw new ArgumentNullException(nameof(serverv4));

            ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Info, "Start Listening via IPv4\n");

            while (status == Status.Running)
            {
                TcpClient tcpClient = serverv4.AcceptTcpClient();

                lock (tcpClients)
                    tcpClients.Add(new ClientControlBlock(tcpClient, TcpClientHandler));
            }
        }
        public void ProcessV6()
        {
            if (tcpClients == null)
                throw new ArgumentNullException(nameof(tcpClients));

            if (serverv6 == null)
                throw new ArgumentNullException(nameof(serverv6));

            ConsoleBuffer.AppendBuffer(ConsoleBuffer.BufferContentType.Info, "Start Listening via IPv6\n");

            while (status == Status.Running)
            {
                TcpClient tcpClient = serverv6.AcceptTcpClient();

                lock (tcpClients)
                    tcpClients.Add(new ClientControlBlock(tcpClient, TcpClientHandler));
            }
        }

        public async void ProcessV4TaskAsync()
        {
            var t = Task.Run(() => ProcessV4());
            listenerTasks.Add(t);
            await t;
        }

        public async void ProcessV6TaskAsync()
        {
            var t = Task.Run(() => ProcessV6());
            listenerTasks.Add(t);
            await t;
        }

        public void Stop()
        {
            if (status != Status.Running)
            {
                ConsoleBuffer.AppendBuffer(MineMP.ConsoleBuffer.BufferContentType.Warn, "Stop server cancelled due to status.\n");
                return;
            }

            status = Status.Stopping;

            // Stop Listeners
            serverv4?.Stop();
            serverv6?.Stop();

            // Stop GC
            CCB_GC.Enabled = false;
            CCB_GC.Stop();

            status = Status.Stopped;
        }
    }
}

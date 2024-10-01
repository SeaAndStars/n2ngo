using System.Net;
using System.Net.Sockets;
using static N2NGOCore.Protocol;
using static N2NGOCore.Package;
using N2NGOCore;
using N2NGOCore.Objects;
using N2NGOCore.Models;
using N2NGOCore.Models.Server;
using static N2NGOCore.Models.Room;

namespace N2NGOServer.Server.Base;

internal class N2NGOServer
{
    public static readonly string MainName = "N2N GO Server";

    public List<Room> Rooms { get; set; } = new();
    public static List<Room> FilterRooms(List<Room> rooms, string key)
    {
        List<Room> resultRooms = new();

        key = key.Trim().ToLower(); // Format key
        string[] keys = key.Split(' ');

        for (int i = 0; i < rooms.Count; i++)
        {
            bool pairs = false;
            var room = rooms[i];

            foreach (string _key in keys)
            {
                if (room.RoomName != null && room.RoomName.Contains(_key))
                    pairs = true;
                if (room.RoomCode != null && room.RoomCode.Contains(_key))
                    pairs = true;
            }
            if (!pairs)
                continue;

            resultRooms.Add(room);
        }

        return resultRooms;
    }

    public MineMP.ConsoleBuffer ConsoleBuffer { get; private set; }

    private readonly System.Timers.Timer CCB_GC = new() { Interval = 5000, AutoReset = true };  // Garbage Collection for Client Control Blocks
    private readonly System.Timers.Timer RCB_GC = new() { Interval = 10000, AutoReset = true }; // Garbage Collection for Room Control Blocks

    public enum ServerStatus
    {
        Stopped = 0,
        Initialization,
        Initialized,
        Starting,
        Running,
        Stopping
    }

    public ServerStatus Status { get; private set; } = ServerStatus.Stopped;

    public TcpListener? ServerV4 { get; private set; }
    public TcpListener? ServerV6 { get; private set; }
    public List<ClientControlBlock> ConnectionTcpClients { get; private set; }
    public List<Task> ListenerTasks { get; private set; } = new List<Task>();


    public N2NGOServer(MineMP.ConsoleBuffer consoleBuffer, IPAddress ip, int port)
    {
        ConsoleBuffer = consoleBuffer;

        ServerV4 = new TcpListener(ip, port);
        ConnectionTcpClients = new();

        CCB_GC.Elapsed += CCB_GC_Elapsed;
        CCB_GC.Enabled = true;
        RCB_GC.Elapsed += RCB_GC_Elapsed;
        RCB_GC.Enabled = true;
    }
    public N2NGOServer(MineMP.ConsoleBuffer consoleBuffer, IPAddress ip, int port, IPAddress ipv6, int portv6) : this(consoleBuffer, ip, port)
    {
        ServerV6 = new TcpListener(ipv6, portv6);
    }

    public N2NGOServer(MineMP.ConsoleBuffer consoleBuffer, TcpListener tcpListener)
    {
        ConsoleBuffer = consoleBuffer;

        ServerV4 = tcpListener;
        ConnectionTcpClients = new();

        CCB_GC.Elapsed += CCB_GC_Elapsed;
        CCB_GC.Enabled = true;
        RCB_GC.Elapsed += RCB_GC_Elapsed;
        RCB_GC.Enabled = true;
    }
    public N2NGOServer(MineMP.ConsoleBuffer consoleBuffer, TcpListener tcpListener, TcpListener tcpListenerv6) : this(consoleBuffer, tcpListener)
    {
        ServerV6 = tcpListenerv6;
    }

    private void CCB_GC_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        lock (ConnectionTcpClients)
            for (int i = 0; i < ConnectionTcpClients.Count; i++)
                if (!ConnectionTcpClients[i].ClientHandlerThread.IsAlive || !ConnectionTcpClients[i].IsClientAlive)
                {
                    ConnectionTcpClients.Remove(ConnectionTcpClients[i]);
                }
    }

    private void RCB_GC_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        lock (Rooms)
            for (int i = 0; i < Rooms.Count; i++)
                if (!Rooms[i].CB.IsAlive)
                    Rooms.Remove(Rooms[i]);
    }

    public bool Init()
    {
        if (Status != ServerStatus.Stopped)
        {
            ConsoleBuffer.AppendBuffer(MineMP.ConsoleBuffer.BufferContentType.Warn, "Init server cancelled due to status.\n");
            return false;
        }

        Status = ServerStatus.Initialization;

        ServerV4?.Stop();
        ServerV6?.Stop();

        Status = ServerStatus.Initialized;

        return true;
    }

    public void Start()
    {
        if (Status != ServerStatus.Initialized)
        {
            ConsoleBuffer.AppendBuffer(MineMP.ConsoleBuffer.BufferContentType.Warn, "Start server cancelled due to status.\n");
            return;
        }

        Status = ServerStatus.Starting;

        ServerV4?.Start();
        ServerV6?.Start();

        CCB_GC.Start();
        RCB_GC.Start();

        Status = ServerStatus.Running;
    }

    public void Stop()
    {
        if (Status != ServerStatus.Running)
        {
            ConsoleBuffer.AppendBuffer(MineMP.ConsoleBuffer.BufferContentType.Warn, "Stop server cancelled due to status.\n");
            return;
        }

        Status = ServerStatus.Stopping;

        // Stop Listeners
        ServerV4?.Stop();
        ServerV6?.Stop();

        // Stop GC
        CCB_GC.Enabled = false;
        CCB_GC.Stop();
        RCB_GC.Enabled = false;
        RCB_GC.Stop();

        Status = ServerStatus.Stopped;
    }

    private void TcpClientHandler(object? clientControlBlockObject)
    {
        if (clientControlBlockObject is not ClientControlBlock clientControlBlock)
            return;
        if (clientControlBlock.ConnectionClient is not TcpClient client)
            return;
        if (client.Client.RemoteEndPoint is not IPEndPoint iPEndPoint)
            return;

        clientControlBlock.ClientConnectionData["_ThreadCancellationTokenSource"] = new CancellationTokenSource();
        clientControlBlock.ClientConnectionData["_ThreadCancellationToken"] = (clientControlBlock.ClientConnectionData["_ThreadCancellationTokenSource"] as CancellationTokenSource ?? throw new("clientControlBlock.ClientConnectionData[\"_ThreadCancellationTokenSource\"]")).Token;
        clientControlBlock.ClientConnectionData["UserSessionKey"] = Room.RandomKeyString();

        NetworkStream stream = client.GetStream();
        string? currentRoom = null;

        if (clientControlBlock.ClientConnectionData["_ThreadCancellationToken"] is not CancellationToken cancellationToken)
            throw new("clientControlBlock.ClientConnectionData[\"_ThreadCancellationToken\"]");

        while (true)
        {
            try
            {
                if (!client.Connected) break;

                cancellationToken.ThrowIfCancellationRequested();
                Package client_package = ResolvePackage(new byte[] { NetworkStreamBlockReadingByte(stream) });


                switch ((BaseHeader)client_package.Header)
                {
                    case BaseHeader.undefined:
                        {
                            // Unknown Client
                            Package package = MakePackage(BaseHeader.InvalidClient);
                            stream.Write(BuildPackage(package));
                            goto RemoveClientNoEcho;
                        }

                    case BaseHeader.peek:
                        {
                            IO_Tool iO_Tool = new();

                            if (!iO_Tool.Send(client, MakePackage(BaseHeader.peek_ok)))
                                goto RemoveClientNoEcho;

                            break;
                        }

                    // TODO: Obsolete Protocol BaseHeader._ver_check
                    case BaseHeader._ver_check:
                        {
                            IO_Tool iO_Tool = new();

                            if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, MsgExternalData.Encode.MsgString(Globals.N2NGOClientLatestVersion.ToString()))))
                                goto RemoveClientNoEcho;

                            break;
                        }
                    case BaseHeader._user_key_get:
                        {
                            IO_Tool iO_Tool = new();

                            if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_string_long, MsgExternalData.Encode.MsgStringLong(clientControlBlock.ClientConnectionData["UserSessionKey"] as string ?? throw new("Cannot get userkey from data.")))))
                                goto RemoveClientNoEcho;

                            break;
                        }
                    case BaseHeader._pull_online_total:
                        {
                            IO_Tool iO_Tool = new();

                            var total = ConnectionTcpClients.Count;

                            if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_long, MsgExternalData.Encode.MsgLong(total))))
                                goto RemoveClient;

                            break;
                        }
                    case BaseHeader._rooms_pull_rooms_pages:
                        {
                            IO_Tool iO_Tool = new();

                            var total = Rooms.Where(_ => !(_.IsRoomInvisible ?? throw new($"IsRoomInvisible of '{_.RoomCode}' is null")) && _.RoomCode != null).ToList().Count;
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

                            var roomsUnfiltered = Rooms.Where(_ => !(_.IsRoomInvisible ?? throw new($"IsRoomInvisible of '{_.RoomCode}' is null")) && _.RoomCode != null).ToList();   // Do not take invisible room
                            var rooms = roomsUnfiltered.Skip((int)(_PullRoomsRoomsTake * page_index)).Take((int)_PullRoomsRoomsTake).ToList();
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
                                    var code = rooms[i].RoomCode ?? throw new($"RoomCode of '{rooms[i].RoomCode}' is null");
                                    var name = rooms[i].RoomName ?? throw new($"RoomName of '{rooms[i].RoomCode}' is null");
                                    var ipn = rooms[i].IsRoomPasswordNeeded ?? throw new($"IsRoomPasswordNeeded of '{rooms[i].RoomCode}' is null");
                                    var mainColor = rooms[i].MainColor ?? throw new($"MainColor of '{rooms[i].RoomCode}' is null");
                                    var minorColor = rooms[i].MinorColor ?? throw new($"MinorColor of '{rooms[i].RoomCode}' is null");

                                    if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, MsgExternalData.Encode.MsgString(code))))
                                        goto RemoveClient;
                                    if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, MsgExternalData.Encode.MsgString(name))))
                                        goto RemoveClient;
                                    if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_byte, MsgExternalData.Encode.MsgByte((byte)(ipn ? 1 : 0)))))
                                        goto RemoveClient;
                                    if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulong, MsgExternalData.Encode.MsgULong(mainColor.data))))
                                        goto RemoveClient;
                                    if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulong, MsgExternalData.Encode.MsgULong(minorColor.data))))
                                        goto RemoveClient;
                                    if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulong, MsgExternalData.Encode.MsgULong((uint)rooms[i].Members.Count))))
                                        goto RemoveClient;
                                }
                            }

                            break;
                        }
                    case BaseHeader._rooms_pull_rooms_searched:
                        {
                            IO_Tool iO_Tool = new();

                            string search_keywords = string.Empty;

                            Package? pkg_app = iO_Tool.Receive(client);
                            if (pkg_app != null)
                                if (pkg_app.Value.Header == (byte)BaseHeader.msg_string_long && pkg_app.Value.external_data != null)
                                    search_keywords = Package.MsgExternalData.Decode.MsgStringLong(pkg_app.Value.external_data);
                                else goto RemoveClient;
                            else goto RemoveClient;

                            var roomsUnfiltered = Rooms.Where(_ => !(_.IsRoomInvisible ?? throw new($"IsRoomInvisible of '{_.RoomCode}' is null")) && _.RoomCode != null).ToList();   // Do not take invisible room
                            var rooms = FilterRooms(roomsUnfiltered, search_keywords);
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
                                    var code = rooms[i].RoomCode ?? throw new($"RoomCode of '{rooms[i].RoomCode}' is null");
                                    var name = rooms[i].RoomName ?? throw new($"RoomName of '{rooms[i].RoomCode}' is null");
                                    var ipn = rooms[i].IsRoomPasswordNeeded ?? throw new($"IsRoomPasswordNeeded of '{rooms[i].RoomCode}' is null");
                                    var mainColor = rooms[i].MainColor ?? throw new($"MainColor of '{rooms[i].RoomCode}' is null");
                                    var minorColor = rooms[i].MinorColor ?? throw new($"MinorColor of '{rooms[i].RoomCode}' is null");

                                    if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, MsgExternalData.Encode.MsgString(code))))
                                        goto RemoveClient;
                                    if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, MsgExternalData.Encode.MsgString(name))))
                                        goto RemoveClient;
                                    if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_byte, MsgExternalData.Encode.MsgByte((byte)(ipn ? 1 : 0)))))
                                        goto RemoveClient;
                                    if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulong, MsgExternalData.Encode.MsgULong(mainColor.data))))
                                        goto RemoveClient;
                                    if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulong, MsgExternalData.Encode.MsgULong(minorColor.data))))
                                        goto RemoveClient;
                                    if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulong, MsgExternalData.Encode.MsgULong((uint)rooms[i].Members.Count))))
                                        goto RemoveClient;
                                }
                            }

                            break;
                        }
                    case BaseHeader._rooms_create:
                        {
                            IO_Tool iO_Tool = new();

                            string code, name;
                            bool iri, irp;
                            string passwd;
                            UInt32 mainColor;
                            UInt32 minorColor;
                            UInt64 initialLifetime = (ulong)TimeSpan.FromMinutes(10).TotalMilliseconds;

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
                                    iri = Package.MsgExternalData.Decode.MsgByte(pkg_app.Value.external_data) == 1;
                                else goto RemoveClient;
                            else goto RemoveClient;
                            pkg_app = iO_Tool.Receive(client);
                            if (pkg_app != null)
                                if (pkg_app.Value.Header == (byte)BaseHeader.msg_byte && pkg_app.Value.external_data != null)
                                    irp = Package.MsgExternalData.Decode.MsgByte(pkg_app.Value.external_data) == 1;
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
                                    mainColor = Package.MsgExternalData.Decode.MsgULong(pkg_app.Value.external_data);
                                else goto RemoveClient;
                            else goto RemoveClient;
                            pkg_app = iO_Tool.Receive(client);
                            if (pkg_app != null)
                                if (pkg_app.Value.Header == (byte)BaseHeader.msg_ulong && pkg_app.Value.external_data != null)
                                    minorColor = Package.MsgExternalData.Decode.MsgULong(pkg_app.Value.external_data);
                                else goto RemoveClient;
                            else goto RemoveClient;

                            pkg_app = iO_Tool.Receive(client);
                            if (pkg_app != null)
                                if (pkg_app.Value.Header == (byte)BaseHeader.msg_ulonglong && pkg_app.Value.external_data != null)
                                    initialLifetime = Package.MsgExternalData.Decode.MsgULongLong(pkg_app.Value.external_data);
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

                            var newRoom = new Room { RoomCode = code, RoomName = name, IsRoomInvisible = iri, IsRoomPasswordNeeded = irp, RoomPassword = passwd, MainColor = new RoomColor(mainColor), MinorColor = new RoomColor(minorColor) };
                            newRoom.CB.ActivatedLifetime = TimeSpan.FromMilliseconds(initialLifetime);
                            newRoom.CB.AdminKey.Add(clientControlBlock.ClientConnectionData["UserSessionKey"] as string ?? throw new("Cannot get userkey from data."));
                            newRoom.InitHandler();
                            lock (Rooms)
                                Rooms.Add(newRoom);

                            iO_Tool.Send(client, MakePackage(BaseHeader.msg_string_long, Package.MsgExternalData.Encode.MsgStringLong(newRoom.CB.AdminKey[0])));    // Reponse base admin key
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
                                    var targetRoomCode = MsgExternalData.Decode.MsgString(pkg_app.Value.external_data);

                                    bool ex = false;
                                    lock (Rooms)
                                        for (int i = 0; i < Rooms.Count; i++)
                                        {
                                            if (Rooms[i].RoomCode == targetRoomCode)
                                            {
                                                ex = true; break;
                                            }
                                        }
                                    iO_Tool.Send(client, MakePackage(BaseHeader.msg_byte, MsgExternalData.Encode.MsgByte((byte)(ex ? 1 : 0))));
                                    break;
                                }

                            goto RemoveClient;
                        }
                    case BaseHeader._rooms_get_room:
                        {
                            IO_Tool iO_Tool = new();
                            Package? pkg_app = iO_Tool.Receive(client);
                            if (pkg_app != null)
                                if (pkg_app.Value.Header == (byte)BaseHeader.msg_string && pkg_app.Value.external_data != null)
                                {
                                    string roomCode = Package.MsgExternalData.Decode.MsgString(pkg_app.Value.external_data);

                                    lock (Rooms)
                                    {
                                        var rooms = Rooms;
                                        for (int i = 0; i < rooms.Count; i++)
                                        {
                                            if (rooms[i].RoomCode == roomCode)
                                            {
                                                var code = rooms[i].RoomCode ?? throw new($"RoomCode of '{rooms[i].RoomCode}' is null");
                                                var name = rooms[i].RoomName ?? throw new($"RoomName of '{rooms[i].RoomCode}' is null");
                                                var ipn = rooms[i].IsRoomPasswordNeeded ?? throw new($"IsRoomPasswordNeeded of '{rooms[i].RoomCode}' is null");
                                                var mainColor = rooms[i].MainColor ?? throw new($"MainColor of '{rooms[i].RoomCode}' is null");
                                                var minorColor = rooms[i].MinorColor ?? throw new($"MinorColor of '{rooms[i].RoomCode}' is null");

                                                if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, MsgExternalData.Encode.MsgString(code))))
                                                    goto RemoveClient;
                                                if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, MsgExternalData.Encode.MsgString(name))))
                                                    goto RemoveClient;
                                                if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_byte, MsgExternalData.Encode.MsgByte((byte)(ipn ? 1 : 0)))))
                                                    goto RemoveClient;
                                                if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulong, MsgExternalData.Encode.MsgULong(mainColor.data))))
                                                    goto RemoveClient;
                                                if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulong, MsgExternalData.Encode.MsgULong(minorColor.data))))
                                                    goto RemoveClient;
                                                if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulong, MsgExternalData.Encode.MsgULong((uint)rooms[i].Members.Count))))
                                                    goto RemoveClient;
                                                if (!iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulonglong, MsgExternalData.Encode.MsgULongLong((ulong)rooms[i].CB.ActivatedLifetime.TotalMilliseconds))))
                                                    goto RemoveClient;
                                            }
                                        }
                                    }

                                    break;
                                }

                            goto RemoveClient;
                        }

                    case BaseHeader._room_client_join:
                        {
                            IO_Tool iO_Tool = new();

                            if (currentRoom is not null)
                            {
                                iO_Tool.Send(client, MakePackage(BaseHeader._room_client_join_fail_0));
                                break;
                            }

                            string roomCode, roomPassword;

                            Package? pkg_app = iO_Tool.Receive(client);
                            if (pkg_app != null)
                                if (pkg_app.Value.Header == (byte)BaseHeader.msg_string && pkg_app.Value.external_data != null)
                                    roomCode = Package.MsgExternalData.Decode.MsgString(pkg_app.Value.external_data);
                                else goto RemoveClient;
                            else goto RemoveClient;
                            pkg_app = iO_Tool.Receive(client);
                            if (pkg_app != null)
                                if (pkg_app.Value.Header == (byte)BaseHeader.msg_string && pkg_app.Value.external_data != null)
                                    roomPassword = Package.MsgExternalData.Decode.MsgString(pkg_app.Value.external_data);
                                else goto RemoveClient;
                            else goto RemoveClient;

                            lock (Rooms)
                            {
                                var rooms = Rooms;
                                for (int i = 0; i < Rooms.Count; i++)
                                {
                                    var code = rooms[i].RoomCode ?? throw new($"RoomCode of '{rooms[i].RoomCode}' is null");
                                    var pwd = rooms[i].RoomPassword ?? throw new($"Password of '{rooms[i].RoomCode}' is null");
                                    var ipn = rooms[i].IsRoomPasswordNeeded ?? throw new($"IsRoomPasswordNeeded of '{rooms[i].RoomCode}' is null");
                                    if (code == roomCode)
                                    {
                                        if (ipn)
                                        {
                                            if (roomPassword != pwd)
                                            {
                                                iO_Tool.Send(client, MakePackage(BaseHeader._room_client_join_fail_2));
                                                goto room_client_join_fail;
                                            }
                                        }

                                        var rCreateMember = rooms[i].CreateMember(clientControlBlock.ClientConnectionData["UserSessionKey"] as string ?? throw new("Cannot get userkey from data."), "Unknown", "Unknown");
                                        rCreateMember.Item1.ClientControlBlock = clientControlBlock;
                                        var rJoin = rooms[i].MemberJoin(rCreateMember.Item1);
                                        if (rJoin != 0)
                                        {
                                            iO_Tool.Send(client, MakePackage(BaseHeader._room_client_join_fail_00));
                                            iO_Tool.Send(client, MakePackage(BaseHeader.msg_long, Package.MsgExternalData.Encode.MsgLong(rJoin)));
                                            goto room_client_join_fail;
                                        }

                                        iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(rCreateMember.Item2.ID)));

                                        currentRoom = code;
                                        goto room_client_join_ok;
                                    }
                                }
                            }

                            iO_Tool.Send(client, MakePackage(BaseHeader._room_client_join_fail_1));
                            break;

                        room_client_join_fail:
                            break;

                        room_client_join_ok:
                            break;
                        }
                    case BaseHeader._room_client_leave:
                        {
                            IO_Tool iO_Tool = new();
                            iO_Tool.Send(client, MakePackage(BaseHeader.msg_ok));
                            for (int i = 0; i < Rooms.Count; i++)
                            {
                                var code = Rooms[i].RoomCode ?? throw new($"RoomCode of '{Rooms[i].RoomCode}' is null");
                                if (code == currentRoom)
                                {
                                    var currentMember = Rooms[i].GetMember(clientControlBlock.ClientConnectionData["UserSessionKey"] as string ?? throw new("Cannot get userkey from data."));
                                    if (currentMember is not null)
                                        Rooms[i].MemberLeave(currentMember);

                                    break;
                                }
                            }
                            currentRoom = null;
                            break;
                        }
                    case BaseHeader._room_client_push:
                        {
                            IO_Tool iO_Tool = new();

                            string userNickname, userIpAddress;

                            Package? pkg_app = iO_Tool.Receive(client);
                            if (pkg_app != null)
                                if (pkg_app.Value.Header == (byte)BaseHeader.msg_string && pkg_app.Value.external_data != null)
                                    userNickname = Package.MsgExternalData.Decode.MsgString(pkg_app.Value.external_data);
                                else goto RemoveClient;
                            else goto RemoveClient;
                            pkg_app = iO_Tool.Receive(client);
                            if (pkg_app != null)
                                if (pkg_app.Value.Header == (byte)BaseHeader.msg_string && pkg_app.Value.external_data != null)
                                    userIpAddress = Package.MsgExternalData.Decode.MsgString(pkg_app.Value.external_data);
                                else goto RemoveClient;
                            else goto RemoveClient;

                            for (int i = 0; i < Rooms.Count; i++)
                            {
                                var code = Rooms[i].RoomCode ?? throw new($"RoomCode of '{Rooms[i].RoomCode}' is null");
                                if (code == currentRoom)
                                {
                                    var currentMember = Rooms[i].GetMember(clientControlBlock.ClientConnectionData["UserSessionKey"] as string ?? throw new("Cannot get userkey from data."));
                                    if (currentMember is null)
                                    {
                                        iO_Tool.Send(client, MakePackage(BaseHeader._room_client_fail_1));
                                        goto finished;
                                    }

                                    currentMember.Nickname = userNickname;
                                    currentMember.IpAddress = userIpAddress;
                                    break;
                                }
                            }

                            iO_Tool.Send(client, MakePackage(BaseHeader.msg_ok));
                        finished:
                            break;
                        }
                    case BaseHeader._room_client_pull:
                        {
                            IO_Tool iO_Tool = new();

                            Room? room = null;
                            for (int i = 0; i < Rooms.Count; i++)
                            {
                                var code = Rooms[i].RoomCode ?? throw new($"RoomCode of '{Rooms[i].RoomCode}' is null");
                                if (code == currentRoom)
                                {
                                    if (Rooms[i].GetMember(clientControlBlock.ClientConnectionData["UserSessionKey"] as string ?? throw new("Cannot get userkey from data.")) is null)
                                        continue;

                                    room = Rooms[i];
                                    break;
                                }
                            }
                            if (room is null)
                            {
                                iO_Tool.Send(client, MakePackage(BaseHeader._room_client_fail_1));
                                break;
                            }

                            var members = room.Members.AsReadOnly();
                            iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulong, Package.MsgExternalData.Encode.MsgULong((uint)members.Count)));
                            foreach(var member in members)
                            {
                                iO_Tool.Send(client, MakePackage(BaseHeader.msg_byte, Package.MsgExternalData.Encode.MsgByte((byte)(room.IsMemberAdmin(member) ? 1 : 0))));
                                iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(member.ID)));
                                iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(member.Nickname)));
                                iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(member.IpAddress)));
                            }

                            var ruledMembers = room.RuledMembers.AsReadOnly();
                            iO_Tool.Send(client, MakePackage(BaseHeader.msg_ulong, Package.MsgExternalData.Encode.MsgULong((uint)ruledMembers.Count)));
                            foreach (var ruledMember in ruledMembers)
                            {
                                iO_Tool.Send(client, MakePackage(BaseHeader.msg_byte, Package.MsgExternalData.Encode.MsgByte((byte)ruledMember.Behaviour)));
                                iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(ruledMember.ID)));
                                iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(ruledMember.Nickname)));
                                iO_Tool.Send(client, MakePackage(BaseHeader.msg_string, Package.MsgExternalData.Encode.MsgString(ruledMember.IpAddress)));
                            }

                            break;
                        }

                    case BaseHeader._room_client_admin_close_room:
                        {
                            IO_Tool iO_Tool = new();

                            var userKey = clientControlBlock.ClientConnectionData["UserSessionKey"] as string ?? throw new("Cannot get userkey from data.");

                            Room? room = null;
                            for (int i = 0; i < Rooms.Count; i++)
                            {
                                var code = Rooms[i].RoomCode ?? throw new($"RoomCode of '{Rooms[i].RoomCode}' is null");
                                if (code == currentRoom)
                                {
                                    if (Rooms[i].GetMember(userKey) is null)
                                        continue;

                                    room = Rooms[i];
                                    break;
                                }
                            }
                            if (room is null)
                            {
                                iO_Tool.Send(client, MakePackage(BaseHeader._room_client_fail_1));
                                break;
                            }

                            if (!room.IsMemberAdmin(new() { UserKey = userKey }))
                            {
                                iO_Tool.Send(client, MakePackage(BaseHeader._room_client_admin_fail_0));
                                break;
                            }

                            room.CB.ActivatedLifetime = TimeSpan.Zero;

                            iO_Tool.Send(client, MakePackage(BaseHeader.msg_ok));
                            break;
                        }

                    case BaseHeader._room_client_admin_activate_room:
                        {
                            IO_Tool iO_Tool = new();

                            var userKey = clientControlBlock.ClientConnectionData["UserSessionKey"] as string ?? throw new("Cannot get userkey from data.");

                            Room? room = null;
                            for (int i = 0; i < Rooms.Count; i++)
                            {
                                var code = Rooms[i].RoomCode ?? throw new($"RoomCode of '{Rooms[i].RoomCode}' is null");
                                if (code == currentRoom)
                                {
                                    if (Rooms[i].GetMember(userKey) is null)
                                        continue;

                                    room = Rooms[i];
                                    break;
                                }
                            }
                            if (room is null)
                            {
                                iO_Tool.Send(client, MakePackage(BaseHeader._room_client_fail_1));
                                break;
                            }

                            if (!room.IsMemberAdmin(new() { UserKey = userKey }))
                            {
                                iO_Tool.Send(client, MakePackage(BaseHeader._room_client_admin_fail_0));
                                break;
                            }

                            if (room.CB.ActivatedLifetime.TotalHours >= 48)
                            {
                                iO_Tool.Send(client, MakePackage(BaseHeader._room_client_admin_activate_room_fail_0));
                                break;
                            }

                            room.Activate(TimeSpan.FromHours(1));

                            iO_Tool.Send(client, MakePackage(BaseHeader.msg_ok));
                            break;
                        }

                    case BaseHeader.extended_package:
                        {
                            Package package = MakePackage(BaseHeader.NotImplemented);
                            stream.Write(BuildPackage(package));
                            break;
                        }

                    default:
                        {
                            Package package = MakePackage(BaseHeader.NotImplemented);
                            stream.Write(BuildPackage(package));
                            break;
                        }
                }
            }
            catch (OperationCanceledException ex)
            {
                ConsoleBuffer.AppendFormatBuffer(MineMP.ConsoleBuffer.BufferContentType.Info, "Connection Thread({3}) Interrupted: {0}:{1} ({2})" + Environment.NewLine, iPEndPoint.Address.ToString(), iPEndPoint.Port.ToString(), ex.Message, Environment.CurrentManagedThreadId);
                goto RemoveClientNoEcho;
            }
            catch (IOException ex)
            {
                if (ex.InnerException != null)
                {
                    if (ex.InnerException is SocketException socketException)
                    {
                        if (socketException.SocketErrorCode == SocketError.ConnectionReset)
                            goto RemoveClientNoEcho;
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleBuffer.AppendFormatBuffer(MineMP.ConsoleBuffer.BufferContentType.Info, "Connection: {0}:{1} remove due to exception: {2}" + Environment.NewLine, iPEndPoint.Address.ToString(), iPEndPoint.Port.ToString(), ex.Message);
                goto RemoveClientNoEcho;
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
        if (ConnectionTcpClients == null)
            throw new ArgumentNullException(nameof(ConnectionTcpClients));

        if (ServerV4 == null)
            throw new ArgumentNullException(nameof(ServerV4));

        ConsoleBuffer.AppendBuffer(MineMP.ConsoleBuffer.BufferContentType.Info, "Start Listening via IPv4\n");

        while (Status == ServerStatus.Running)
        {
            try
            {
                TcpClient tcpClient = ServerV4.AcceptTcpClient();

                lock (ConnectionTcpClients)
                {
                    var client = new ClientControlBlock(tcpClient, TcpClientHandler, true);
                    ConnectionTcpClients.Add(client);
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.Interrupted)
                    return;
            }
        }
    }
    public void ProcessV6()
    {
        if (ConnectionTcpClients == null)
            throw new ArgumentNullException(nameof(ConnectionTcpClients));

        if (ServerV6 == null)
            throw new ArgumentNullException(nameof(ServerV6));

        ConsoleBuffer.AppendBuffer(MineMP.ConsoleBuffer.BufferContentType.Info, "Start Listening via IPv6\n");

        while (Status == ServerStatus.Running)
        {
            try
            {
                TcpClient tcpClient = ServerV6.AcceptTcpClient();

                lock (ConnectionTcpClients)
                {
                    var client = new ClientControlBlock(tcpClient, TcpClientHandler, true);
                    ConnectionTcpClients.Add(client);
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.Interrupted)
                    return;
            }
        }
    }

    public async void ProcessV4TaskAsync()
    {
        var t = Task.Run(() => ProcessV4());
        ListenerTasks.Add(t);
        await t;
    }

    public async void ProcessV6TaskAsync()
    {
        var t = Task.Run(() => ProcessV6());
        ListenerTasks.Add(t);
        await t;
    }


    private static byte NetworkStreamBlockReadingByte(NetworkStream stream)
    {
        byte[] bytes = new byte[1];
        stream.Read(bytes, 0, 1);

        return bytes[0];
    }
}

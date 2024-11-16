using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace N2NGO.Utils;

internal class N2NGOServerConnection
{
    public IPEndPoint ServerIPEndPoint { get; set; }
    public int ServerSupernodePort { get; set; }

    public N2NGOServerConnection(IPEndPoint serverIPEndPoint, int serverSupernodePort)
    {
        ServerIPEndPoint = serverIPEndPoint;
        ServerSupernodePort = serverSupernodePort;
    }

    private TcpClient _client = new();

    public TcpClient Client => _client;

    public virtual void ClientExHandler(Exception? ex = null)
    {
        Globals.CurrentApp.Dispatcher.InvokeAsync(() => Globals.CurrentApp.MainWindow.DoMessageYesNoDialog($"我们与N2N GO 服务器通出现错误（可能已经断开），信息：{ex?.Message}\n要重置连接吗？", "N2N GO 连接异常",
            new()
            {
                (_) =>
                    {
                        if (_ is not Views.SubPages.Dialogs.DialogMessage dialogMessage || dialogMessage.MessageContent is not Views.SubPages.Dialogs.MessageDialogs.DialogYesNo dialogYesNo)
                            throw new Exception("Cannot get DialogYesNo");

                        if ( dialogYesNo.YesNo == Views.SubPages.Dialogs.MessageDialogs.DialogYesNo.YesNoE.Yes)
                            Task.Run(() => Globals.CurrentApp.ResetConnection(true));
                    }
            }
        ));
    }

    #region Base Operations
    /// <summary>
    /// <see cref="Package.IO_Tool.Send">Send</see> a <see cref="Package">packageMember</see> through the <see cref="_client">Connection Client</see>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    /// </summary>
    public bool Connect(bool exHandle = true)
    {
        lock (this)
        {
            Close();
            _client = new();

            try
            {
                _client.Connect(ServerIPEndPoint);
                if (_client.Connected) return true;
            }
            catch (Exception ex)
            {
                if (exHandle)
                    ClientExHandler(ex);
                return false;
            }
            return false;
        }
    }

    /// <summary>
    /// <see cref="Package.IO_Tool.Send">Send</see> a <see cref="Package">packageMember</see> through the <see cref="_client">Connection Client</see>
    /// </summary>
    /// <param name="package">Package to be sent</param>
    /// <param name="timeOut">Sets <seealso cref="TcpClient.ReceiveTimeout"/></param>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    /// <returns>true if successful, otherwise false.</returns>
    public bool Send(N2NGOCore.Package package, int? timeOut = null, bool exHandle = true)
    {
        //var id = $"[{new Random().Next().ToString("x").Substring(0, 5)}] Send '{(N2NGOCore.Protocol.BaseHeader)package.Header}':";

        //CurrentApp.Dispatcher.InvokeAsync(() => Console.WriteLine($"{id} LOCK"));

        lock (this)
        {
            //CurrentApp.Dispatcher.InvokeAsync(() => Console.WriteLine($"{id} BEGIN"));

            N2NGOCore.Package.IO_Tool iO_Tool = new()
            {
                WriteTimeOut = 2000
            };

            if (!iO_Tool.Send(_client, package, true))
            {
                if (iO_Tool.latestEx != null)
                {
                    if (exHandle)
                        ClientExHandler(iO_Tool.latestEx);
                }
                else
                {
                    if (exHandle)
                        ClientExHandler(new("Unknown"));
                }

                //CurrentApp.Dispatcher.InvokeAsync(() => Console.WriteLine($"{id} END (false)"));
                return false;
            }

            //CurrentApp.Dispatcher.InvokeAsync(() => Console.WriteLine($"{id} END (true)"));
            return true;
        }
    }

    /// <summary>
    /// <see cref="Package.IO_Tool.Receive">Receive</see> a <see cref="Package">packageMember</see> through the <see cref="_client">Connection Client</see>
    /// </summary>
    /// <param name="timeOut">Sets <seealso cref="TcpClient.ReceiveTimeout"/></param>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    /// <returns>If failed, a null packageMember will be returned.</returns>
    public N2NGOCore.Package? Receive(int? timeOut = null, bool exHandle = true)
    {
        //var id = $"[{new Random().Next().ToString("x").Substring(0, 5)}] Receive:";

        //CurrentApp.Dispatcher.InvokeAsync(() => Console.WriteLine($"{id} LOCK"));

        lock (this)
        {
            //CurrentApp.Dispatcher.InvokeAsync(() => Console.WriteLine($"{id} BEGIN"));

            N2NGOCore.Package? package = null;

            N2NGOCore.Package.IO_Tool iO_Tool = new()
            {
                ReadTimeOut = 2000
            };

            package = iO_Tool.Receive(_client, null, true);

            if (package == null)
                if (iO_Tool.latestEx != null)
                {
                    if (exHandle)
                        ClientExHandler(iO_Tool.latestEx);
                }
                else
                {
                    if (exHandle)
                        ClientExHandler(new("Unknown"));
                }

            //CurrentApp.Dispatcher.InvokeAsync(() => Console.WriteLine($"{id} END"));
            return package;
        }
    }

    /// <summary>
    ///  <see cref="NetworkStream.Flush">Flushes</see> data from the <see cref="TcpClient.GetStream">stream</see> of <see cref="_client">Connection Client</see>. This method is reserved for future use.
    /// </summary>
    public void Flush()
    {
        lock (this) _client.GetStream().Flush();
    }

    /// <summary>
    ///  Peek connection to server.
    /// </summary>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    /// <returns>true if successful, otherwise false.</returns>
    public bool Peek(bool exHandle = true)
    {
        lock (this)
        {
            if (!_client.Connected)
                return false;

            try
            {
                N2NGOCore.Package pkg = N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader.peek);
                if (!Send(pkg, null, exHandle))
                {
                    if (exHandle)
                        ClientExHandler(new("Cannot send peek package"));
                    return false;
                }

                var rpackage = Receive(6000, exHandle);
                if (rpackage == null)
                {
                    if (exHandle)
                        ClientExHandler(new("Cannot receive response package for peek"));
                    return false;
                }

                return (N2NGOCore.Protocol.BaseHeader)rpackage.Value.Header == N2NGOCore.Protocol.BaseHeader.peek_ok;
            }
            catch (Exception ex)
            {
                if (exHandle)
                    ClientExHandler(ex);
                return false;
            }
        }
    }

    /// <summary>
    ///  Peek connection to server.
    /// </summary>
    /// <param name="peekTimeSpan">Time elapsed while testing the connection to the server.</param>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    /// <returns>true if successful, otherwise false.</returns>
    public bool Peek(out TimeSpan peekTimeSpan, bool exHandle = true)
    {
        lock (this)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            if (!_client.Connected)
            {
                stopwatch.Stop();
                peekTimeSpan = stopwatch.Elapsed;
                return false;
            }

            try
            {
                N2NGOCore.Package pkg = N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader.peek);
                if (!Send(pkg, null, exHandle))
                {
                    stopwatch.Stop();
                    peekTimeSpan = stopwatch.Elapsed;

                    if (exHandle)
                        ClientExHandler(new("Cannot send peek package"));
                    return false;
                }

                var rpackage = Receive(6000, exHandle);
                stopwatch.Stop();
                peekTimeSpan = stopwatch.Elapsed;
                if (rpackage == null)
                {
                    if (exHandle)
                        ClientExHandler(new("Cannot receive response package for peek"));
                    return false;
                }

                return (N2NGOCore.Protocol.BaseHeader)rpackage.Value.Header == N2NGOCore.Protocol.BaseHeader.peek_ok;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                peekTimeSpan = stopwatch.Elapsed;
                if (exHandle)
                    ClientExHandler(ex);
                return false;
            }
        }
    }

    /// <summary>
    ///  Check that the connection[<see cref="TcpClient.Connected"/>] to the server is valid.
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
        Fail_Others,
        Fail_NotConnected,
        Fail_SendFailure,
        Fail_NullPackage,
        Fail_NullPackageExternalData,
        Fail_InvalidPackageHeader,
        Fail_InvalidPackage,
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

        public readonly bool IsSuccessfulStatusCode => Status == ProtocolOperationReturnStatus.Success;
    }
    #region Protocol Operations
    /// <summary>
    /// Get User Key<br/>
    /// <see cref="N2NGOCore.Protocol.BaseHeader._user_key_get"></see>
    /// </summary>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    public ProtocolOperationReturnType<string> GetUserKey(bool exHandle = true)
    {
        if (!IsConnected())
            return new(ProtocolOperationReturnStatus.Fail_NotConnected);

        if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader._user_key_get), null, exHandle))
        {
            if (exHandle)
                ClientExHandler(new("GetUserKey send package fail"));
            return new(ProtocolOperationReturnStatus.Fail_SendFailure);
        }

        var rpackage = Receive(6000, exHandle);
        if (rpackage == null)
        {
            if (exHandle)
                ClientExHandler(new("GetUserKey receive package null"));
            return new(ProtocolOperationReturnStatus.Fail_NullPackage);
        }

        var package = rpackage.Value;
        if ((N2NGOCore.Protocol.BaseHeader)package.Header != N2NGOCore.Protocol.BaseHeader.msg_string_long)
            return new(ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
        if (package.external_data == null)
            return new(ProtocolOperationReturnStatus.Fail_NullPackageExternalData);

        return new(N2NGOCore.Package.MsgExternalData.Decode.MsgStringLong(package.external_data));
    }

    /// <summary>
    /// Pull number of onlines<br/>
    /// <see cref="N2NGOCore.Protocol.BaseHeader._pull_online_total"></see>
    /// </summary>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    public ProtocolOperationReturnType<int> PullTotalOnlines(bool exHandle = true)
    {
        if (!IsConnected())
            return new(-1, ProtocolOperationReturnStatus.Fail_NotConnected);

        N2NGOCore.Package? p = null;
        lock (this)
        {
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader._pull_online_total), null, exHandle))
            {
                if (exHandle)
                    ClientExHandler(new("PullTotalOnlines send package fail"));
                return new(-1, ProtocolOperationReturnStatus.Fail_SendFailure);
            }

            p = Receive(null, exHandle);
        }


        if (p == null)
        {
            if (exHandle)
                ClientExHandler(new("PullTotalOnlines receive package null"));
            return new(-1, ProtocolOperationReturnStatus.Fail_NullPackage);
        }

        var v = p.Value;
        if ((N2NGOCore.Protocol.BaseHeader)v.Header != N2NGOCore.Protocol.BaseHeader.msg_long)
            return new(-1, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);

        if (v.external_data == null)
            return new(-1, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);

        var totalMembers = N2NGOCore.Package.MsgExternalData.Decode.MsgLong(v.external_data);
        return new(totalMembers);
    }

    /// <summary>
    /// Check if the room exists.<br/>
    /// <see cref="N2NGOCore.Protocol.BaseHeader._rooms_is_code_exists"></see>
    /// </summary>
    /// <param name="roomCode">Room code to check</param>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    public ProtocolOperationReturnType<bool> CheckRoomExists(string roomCode, bool exHandle = true)
    {
        N2NGOCore.Package? _pkg_get = null;
        lock (this)
        {
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader._rooms_is_code_exists, N2NGOCore.Package.MsgExternalData.Encode.MsgString(roomCode)), null, exHandle))
            {
                if (exHandle)
                    ClientExHandler(new("CheckRoomExists send package fail"));
                return new(false, ProtocolOperationReturnStatus.Fail_NullPackage);
            }

            _pkg_get = Receive(null, exHandle);
            if (_pkg_get == null)
            {
                if (exHandle)
                    ClientExHandler(new("CheckRoomExists receive package null"));
                return new(false, ProtocolOperationReturnStatus.Fail_NullPackage);
            }
        }

        N2NGOCore.Package pkg_get = _pkg_get.Value;
        if ((N2NGOCore.Protocol.BaseHeader)pkg_get.Header != N2NGOCore.Protocol.BaseHeader.msg_byte)
            return new(false, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);

        if (pkg_get.external_data == null)
            return new(false, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);

        return new(N2NGOCore.Package.MsgExternalData.Decode.MsgByte(pkg_get.external_data) != 0);
    }

    /// <summary>
    /// Get room info by code.<br/>
    /// <see cref="N2NGOCore.Protocol.BaseHeader._rooms_get_room"></see>
    /// </summary>
    /// <param name="roomCode">Room code</param>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    public ProtocolOperationReturnType<N2NGOCore.Models.Room> GetRoomByCode(string roomCode, bool exHandle = true)
    {
        N2NGOCore.Models.Room room = new();

        var roomExists = CheckRoomExists(roomCode, exHandle);
        if (!roomExists.IsSuccessfulStatusCode || !roomExists.Value)
        {
            if (exHandle)
                ClientExHandler(new($"GetRoomByCode Fail: Cannot get room by code due to room not exists.(CheckRoomExists Status Code: {roomExists.Status})"));
            return new(null, ProtocolOperationReturnStatus.Fail_Others);
        }

        lock (this)
        {
            Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader._rooms_get_room), null, exHandle);
            Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader.msg_string, N2NGOCore.Package.MsgExternalData.Encode.MsgString(roomCode)), null, exHandle);

            N2NGOCore.Package[] roomPackage = new N2NGOCore.Package[7];
            for (int i = 0; i < roomPackage.Length; i++)
            {
                var pkg_get = Receive(null, exHandle);
                if (pkg_get == null || pkg_get.Value.external_data == null)
                    goto invalid;

                roomPackage[i] = pkg_get.Value;
            }

            {
                byte[]? edata = roomPackage[0].external_data;
                if ((N2NGOCore.Protocol.BaseHeader)roomPackage[0].Header != N2NGOCore.Protocol.BaseHeader.msg_string || edata == null)
                    goto invalid;
                room.RoomCode = N2NGOCore.Package.MsgExternalData.Decode.MsgString(edata);
            }
            {
                byte[]? edata = roomPackage[1].external_data;
                if ((N2NGOCore.Protocol.BaseHeader)roomPackage[1].Header != N2NGOCore.Protocol.BaseHeader.msg_string || edata == null)
                    goto invalid;
                room.RoomName = N2NGOCore.Package.MsgExternalData.Decode.MsgString(edata);
            }
            {
                byte[]? edata = roomPackage[2].external_data;
                if ((N2NGOCore.Protocol.BaseHeader)roomPackage[2].Header != N2NGOCore.Protocol.BaseHeader.msg_byte || edata == null)
                    goto invalid;
                room.IsRoomPasswordNeeded = N2NGOCore.Package.MsgExternalData.Decode.MsgByte(edata) == 1;
            }
            {
                byte[]? edata = roomPackage[3].external_data;
                if ((N2NGOCore.Protocol.BaseHeader)roomPackage[3].Header != N2NGOCore.Protocol.BaseHeader.msg_ulong || edata == null)
                    goto invalid;
                room.MainColor = new N2NGOCore.Objects.RoomColor(N2NGOCore.Package.MsgExternalData.Decode.MsgULong(edata));
            }
            {
                byte[]? edata = roomPackage[4].external_data;
                if ((N2NGOCore.Protocol.BaseHeader)roomPackage[4].Header != N2NGOCore.Protocol.BaseHeader.msg_ulong || edata == null)
                    goto invalid;
                room.MinorColor = new N2NGOCore.Objects.RoomColor(N2NGOCore.Package.MsgExternalData.Decode.MsgULong(edata));
            }
            {
                byte[]? edata = roomPackage[5].external_data;
                if ((N2NGOCore.Protocol.BaseHeader)roomPackage[5].Header != N2NGOCore.Protocol.BaseHeader.msg_ulong || edata == null)
                    goto invalid;
                room.Members.AddRange(new N2NGOCore.Models.Room.Member[N2NGOCore.Package.MsgExternalData.Decode.MsgULong(edata)]);
            }
            {
                byte[]? edata = roomPackage[6].external_data;
                if ((N2NGOCore.Protocol.BaseHeader)roomPackage[6].Header != N2NGOCore.Protocol.BaseHeader.msg_ulonglong || edata == null)
                    goto invalid;
                try
                {
                    room.CB.ActivatedLifetime = TimeSpan.FromMilliseconds(N2NGOCore.Package.MsgExternalData.Decode.MsgULongLong(edata));
                }
                catch (OverflowException) { room.CB.ActivatedLifetime = TimeSpan.FromSeconds(-1); }
            }
        }

        return new(room);
    invalid:
        if (exHandle)
            ClientExHandler(new("GetRoomByCode invalid"));
        return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackage);
    }

    /// <summary>
    /// Create Room.<br/>
    /// <see cref="N2NGOCore.Protocol.BaseHeader._rooms_create"></see>
    /// </summary>
    /// <param name="roomName">Room name</param>
    /// <param name="isRoomInvisible">If true, the room will not be displayed in the lobby and vice versa</param>
    /// <param name="isRoomPasswordNeeded">If true, the room will validate the roomPassword and vice versa</param>
    /// <param name="roomPassword">Password for authentication to join the room</param>
    /// <param name="mainColor">Main room color theme</param>
    /// <param name="minorColor">Minor room color theme</param>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    /// <returns>Room code and Admin key in string array</returns>
    public ProtocolOperationReturnType<string[]> CreateRoom(string roomName, bool isRoomInvisible, bool isRoomPasswordNeeded, string roomPassword, uint mainColor, uint minorColor, ulong initialLifetime, bool exHandle = true)
    {
        string RoomCode = Globals.CurrentApp.GenerateRoomCode();
        while (true)
        {
            ProtocolOperationReturnType<bool> roomExistsResult = CheckRoomExists(RoomCode, exHandle);
            if (roomExistsResult.Status != ProtocolOperationReturnStatus.Success)
                return new(null, roomExistsResult.Status);
            if (!roomExistsResult.Value)
                break;

            RoomCode = Globals.CurrentApp.GenerateRoomCode();
        }

        lock (this)
        {
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader._rooms_create), null, exHandle))
                goto CreateRoom_Fail_Send;
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader.msg_string, N2NGOCore.Package.MsgExternalData.Encode.MsgString(RoomCode)), null, exHandle))
                goto CreateRoom_Fail_Send;
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader.msg_string, N2NGOCore.Package.MsgExternalData.Encode.MsgString(roomName)), null, exHandle))
                goto CreateRoom_Fail_Send;
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader.msg_byte, N2NGOCore.Package.MsgExternalData.Encode.MsgByte((byte)(isRoomInvisible ? 1 : 0))), null, exHandle))
                goto CreateRoom_Fail_Send;
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader.msg_byte, N2NGOCore.Package.MsgExternalData.Encode.MsgByte((byte)(isRoomPasswordNeeded ? 1 : 0))), null, exHandle))
                goto CreateRoom_Fail_Send;
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader.msg_string, N2NGOCore.Package.MsgExternalData.Encode.MsgString(roomPassword)), null, exHandle))
                goto CreateRoom_Fail_Send;
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader.msg_ulong, N2NGOCore.Package.MsgExternalData.Encode.MsgULong(mainColor)), null, exHandle))
                goto CreateRoom_Fail_Send;
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader.msg_ulong, N2NGOCore.Package.MsgExternalData.Encode.MsgULong(minorColor)), null, exHandle))
                goto CreateRoom_Fail_Send;
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader.msg_ulonglong, N2NGOCore.Package.MsgExternalData.Encode.MsgULongLong(initialLifetime)), null, exHandle))
                goto CreateRoom_Fail_Send;

            var packageReceive = Receive(6000, exHandle);
            if (packageReceive is null)
            {
                if (exHandle) ClientExHandler(new("Cannot create room: Null Package 0"));
                return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
            }
            var package = packageReceive.Value;
            if ((N2NGOCore.Protocol.BaseHeader)package.Header != N2NGOCore.Protocol.BaseHeader.msg_string_long)
            {
                if (exHandle) ClientExHandler(new("Cannot create room: Invalid Package Header 0"));
                return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
            }
            if (package.external_data is null)
            {
                if (exHandle) ClientExHandler(new("Cannot create room: Null Package External Data 0"));
                return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
            }

            packageReceive = Receive(6000, exHandle);
            if (packageReceive is null)
            {
                if (exHandle) ClientExHandler(new("Cannot create room: Null Package 1"));
                return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
            }
            if ((N2NGOCore.Protocol.BaseHeader)packageReceive.Value.Header != N2NGOCore.Protocol.BaseHeader.msg_ok)
            {
                if (exHandle) ClientExHandler(new("Cannot create room: Invalid Package Header 1"));
                return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
            }

            return new(new string[] { RoomCode, N2NGOCore.Package.MsgExternalData.Decode.MsgStringLong(package.external_data) });
        }

    CreateRoom_Fail_Send:
        if (exHandle) ClientExHandler(new("Cannot create room: Send Failure"));
        return new(null, ProtocolOperationReturnStatus.Fail_SendFailure);
    }

    /// <summary>
    /// JoinRoomAsync room<br/>
    /// <see cref="N2NGOCore.Protocol.BaseHeader._room_client_join"/>
    /// </summary>
    /// <param name="roomCode"></param>
    /// <param name="roomPassword"></param>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    /// <returns>User Id in Room</returns>
    public ProtocolOperationReturnType<string> JoinRoom(string roomCode, string roomPassword, bool exHandle = true)
    {
        lock (this)
        {
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader._room_client_join), null, exHandle))
                goto JoinRoom_Fail_Send;
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader.msg_string, N2NGOCore.Package.MsgExternalData.Encode.MsgString(roomCode)), null, exHandle))
                goto JoinRoom_Fail_Send;
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader.msg_string, N2NGOCore.Package.MsgExternalData.Encode.MsgString(roomPassword)), null, exHandle))
                goto JoinRoom_Fail_Send;

            var rpackage = Receive(6000, exHandle);
            if (rpackage is null)
            {
                if (exHandle) ClientExHandler(new("Cannot join room: Null Package 0"));
                return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
            }
            var rpackagev = rpackage.Value;
            if ((N2NGOCore.Protocol.BaseHeader)rpackagev.Header == N2NGOCore.Protocol.BaseHeader._room_client_join_fail_0)
            {
                if (exHandle) ClientExHandler(new("Cannot join room: You can only be in one room at a time."));
                return new(null, ProtocolOperationReturnStatus.Fail_Others);
            }
            if ((N2NGOCore.Protocol.BaseHeader)rpackagev.Header == N2NGOCore.Protocol.BaseHeader._room_client_join_fail_1)
            {
                if (exHandle) ClientExHandler(new("Cannot join room: The specified room does not exist."));
                return new(null, ProtocolOperationReturnStatus.Fail_Others);
            }
            if ((N2NGOCore.Protocol.BaseHeader)rpackagev.Header == N2NGOCore.Protocol.BaseHeader._room_client_join_fail_2)
            {
                if (exHandle) ClientExHandler(new("Cannot join room: verification failure."));
                return new(null, ProtocolOperationReturnStatus.Fail_Others);
            }
            if ((N2NGOCore.Protocol.BaseHeader)rpackagev.Header == N2NGOCore.Protocol.BaseHeader._room_client_join_fail_00)
            {
                var rrpackage = Receive(6000, exHandle);
                if (rrpackage is null)
                {
                    if (exHandle) ClientExHandler(new("Cannot join room: Null Package 1"));
                    return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                }
                var rrpackagev = rrpackage.Value;

                if ((N2NGOCore.Protocol.BaseHeader)rrpackagev.Header != N2NGOCore.Protocol.BaseHeader.msg_long)
                {
                    if (exHandle) ClientExHandler(new("Cannot join room: Invalid Package Header 1"));
                    return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                }
                if (rrpackagev.external_data is null)
                {
                    if (exHandle) ClientExHandler(new("Cannot join room: Null Package External Data 1"));
                    return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                }
                var rrr = N2NGOCore.Package.MsgExternalData.Decode.MsgLong(rrpackagev.external_data);
                if (exHandle) ClientExHandler(new($"Cannot join room: Others: {rrr}"));   /// <see cref="N2NGOCore.Models.Room.MemberJoin"/>
            }
            if ((N2NGOCore.Protocol.BaseHeader)rpackagev.Header != N2NGOCore.Protocol.BaseHeader.msg_string)
            {
                if (exHandle) ClientExHandler(new("Cannot join room: Invalid Package Header 0"));
                return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
            }
            if (rpackagev.external_data is null)
            {
                if (exHandle) ClientExHandler(new("Cannot join room: Null Package External Data 0"));
                return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
            }

            // Success
            return new(N2NGOCore.Package.MsgExternalData.Decode.MsgString(rpackagev.external_data));
        }

    JoinRoom_Fail_Send:
        if (exHandle) ClientExHandler(new("Cannot join room: Send Failure"));
        return new(null, ProtocolOperationReturnStatus.Fail_SendFailure);
    }

    /// <summary>
    /// Leave current room<br/>
    /// <see cref="N2NGOCore.Protocol.BaseHeader._room_client_leave"/>
    /// </summary>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    public void LeaveRoom(bool exHandle = true)
    {
        lock (this)
        {
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader._room_client_leave), null, exHandle))
            {
                if (exHandle) ClientExHandler(new("Cannot leave room: Send Failure"));
                return;
            }
            var rpackage = Receive(6000, exHandle);
            if (rpackage is null)
            {
                if (exHandle) ClientExHandler(new("Cannot leave room: Null Package"));
                return;
            }
            if ((N2NGOCore.Protocol.BaseHeader)rpackage.Value.Header != N2NGOCore.Protocol.BaseHeader.msg_ok)
            {
                if (exHandle) ClientExHandler(new("Cannot leave room: Invalid Package Header"));
                return;
            }

            return;
        }
    }

    /// <summary>
    /// Push current member of room<br/>
    /// <see cref="N2NGOCore.Protocol.BaseHeader._room_client_push"/>
    /// </summary>
    /// <param name="userNickname"></param>
    /// <param name="userIpAddress"></param>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    public void MemberPush(string userNickname, string userIpAddress, bool exHandle = true)
    {
        lock (this)
        {
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader._room_client_push), null, exHandle))
                goto Fail_SendFailure;
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader.msg_string, N2NGOCore.Package.MsgExternalData.Encode.MsgString(userNickname)), null, exHandle))
                goto Fail_SendFailure;
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader.msg_string, N2NGOCore.Package.MsgExternalData.Encode.MsgString(userIpAddress)), null, exHandle))
                goto Fail_SendFailure;

            var rpackage = Receive(6000, exHandle);
            if (rpackage is null)
            {
                if (exHandle) ClientExHandler(new("Cannot push the Member to room: Null Package"));
                return;
            }
            if ((N2NGOCore.Protocol.BaseHeader)rpackage.Value.Header == N2NGOCore.Protocol.BaseHeader._room_client_fail_1)
            {
                if (exHandle) ClientExHandler(new("Cannot push the Member to room: Member not exists in room"));
                return;
            }

            if ((N2NGOCore.Protocol.BaseHeader)rpackage.Value.Header == N2NGOCore.Protocol.BaseHeader.msg_ok)
            {
                return;
            }
        }

        if (exHandle) ClientExHandler(new("Cannot push the Member to room: Others Failure"));
        return;

    Fail_SendFailure:
        if (exHandle) ClientExHandler(new("Cannot push the Member to room: Send Failure"));
        return;
    }

    /// <summary>
    /// Pull members of current room<br/>
    /// <see cref="N2NGOCore.Protocol.BaseHeader._room_client_pull"/>
    /// </summary>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    public ProtocolOperationReturnType<Tuple<IReadOnlyList<N2NGOCore.Models.Room.Member>, IReadOnlyList<N2NGOCore.Models.Room.RuledMember>>> MemberPull(bool exHandle = true)
    {
        lock (this)
        {
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader._room_client_pull), null, exHandle))
            {
                if (exHandle) ClientExHandler(new("Cannot pull member of room: Send Failure"));
                return new(null, ProtocolOperationReturnStatus.Fail_SendFailure);
            }

            var resultMembers = new List<N2NGOCore.Models.Room.Member>();
            {
                var packageMembersCount = Receive(6000, exHandle);
                if (packageMembersCount is null)
                {
                    if (exHandle) ClientExHandler(new("Cannot pull member of room: Null Package0"));
                    return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                }
                if ((N2NGOCore.Protocol.BaseHeader)packageMembersCount.Value.Header != N2NGOCore.Protocol.BaseHeader.msg_ulong)
                {
                    switch ((N2NGOCore.Protocol.BaseHeader)packageMembersCount.Value.Header)
                    {
                        default:
                            {
                                if (exHandle) ClientExHandler(new("Cannot pull member of room: Invalid Package Header0"));
                                break;
                            }

                        case N2NGOCore.Protocol.BaseHeader._room_client_fail_1:
                            {
                                if (exHandle) ClientExHandler(new("Cannot pull member of room: Member not exists in room"));
                                break;
                            }
                    }
                    return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                }
                if (packageMembersCount.Value.external_data is null)
                {
                    if (exHandle) ClientExHandler(new("Cannot pull member of room: Fail_Null Package External Data0"));
                    return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                }

                var membersCount = N2NGOCore.Package.MsgExternalData.Decode.MsgULong(packageMembersCount.Value.external_data);

                for (ulong i = 0; i < membersCount; i++)
                {
                    var packageMemberIsAdmin = Receive(6000, exHandle);
                    if (packageMemberIsAdmin is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Null Package1"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                    }
                    if ((N2NGOCore.Protocol.BaseHeader)packageMemberIsAdmin.Value.Header != N2NGOCore.Protocol.BaseHeader.msg_byte)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Invalid Package Header1"));
                        return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                    }
                    if (packageMemberIsAdmin.Value.external_data is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Fail_Null Package External Data1"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                    }

                    var memberIsAdmin = N2NGOCore.Package.MsgExternalData.Decode.MsgByte(packageMemberIsAdmin.Value.external_data) != 0;

                    var packageMemberID = Receive(6000, exHandle);
                    if (packageMemberID is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Null Package2"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                    }
                    if ((N2NGOCore.Protocol.BaseHeader)packageMemberID.Value.Header != N2NGOCore.Protocol.BaseHeader.msg_string)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Invalid Package Header2"));
                        return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                    }
                    if (packageMemberID.Value.external_data is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Fail_Null Package External Data2"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                    }

                    var memberID = N2NGOCore.Package.MsgExternalData.Decode.MsgString(packageMemberID.Value.external_data);

                    var packageMemberUserNickname = Receive(6000, exHandle);
                    if (packageMemberUserNickname is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Null Package3"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                    }
                    if ((N2NGOCore.Protocol.BaseHeader)packageMemberUserNickname.Value.Header != N2NGOCore.Protocol.BaseHeader.msg_string)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Invalid Package Header3"));
                        return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                    }
                    if (packageMemberUserNickname.Value.external_data is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Fail_Null Package External Data3"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                    }

                    var memberUserNickname = N2NGOCore.Package.MsgExternalData.Decode.MsgString(packageMemberUserNickname.Value.external_data);

                    var packageMemberUserIpAddress = Receive(6000, exHandle);
                    if (packageMemberUserIpAddress is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Null Package4"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                    }
                    if ((N2NGOCore.Protocol.BaseHeader)packageMemberUserIpAddress.Value.Header != N2NGOCore.Protocol.BaseHeader.msg_string)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Invalid Package Header4"));
                        return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                    }
                    if (packageMemberUserIpAddress.Value.external_data is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Fail_Null Package External Data4"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                    }

                    var memberUserIpAddress = N2NGOCore.Package.MsgExternalData.Decode.MsgString(packageMemberUserIpAddress.Value.external_data);

                    resultMembers.Add(new() { IsAdmin = memberIsAdmin, ID = memberID, Nickname = memberUserNickname, IpAddress = memberUserIpAddress });
                }
            }

            var resultRuledMembers = new List<N2NGOCore.Models.Room.RuledMember>();
            {
                var packageMembersCount = Receive(6000, exHandle);
                if (packageMembersCount is null)
                {
                    if (exHandle) ClientExHandler(new("Cannot pull member of room: Null Package0"));
                    return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                }
                if ((N2NGOCore.Protocol.BaseHeader)packageMembersCount.Value.Header != N2NGOCore.Protocol.BaseHeader.msg_ulong)
                {
                    switch ((N2NGOCore.Protocol.BaseHeader)packageMembersCount.Value.Header)
                    {
                        default:
                            {
                                if (exHandle) ClientExHandler(new("Cannot pull member of room: Invalid Package Header0"));
                                break;
                            }

                        case N2NGOCore.Protocol.BaseHeader._room_client_fail_1:
                            {
                                if (exHandle) ClientExHandler(new("Cannot pull member of room: Member not exists in room"));
                                break;
                            }
                    }
                    return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                }
                if (packageMembersCount.Value.external_data is null)
                {
                    if (exHandle) ClientExHandler(new("Cannot pull member of room: Fail_Null Package External Data0"));
                    return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                }

                var membersCount = N2NGOCore.Package.MsgExternalData.Decode.MsgULong(packageMembersCount.Value.external_data);

                for (ulong i = 0; i < membersCount; i++)
                {
                    var packageMemberBehaviour = Receive(6000, exHandle);
                    if (packageMemberBehaviour is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Null Package2_1"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                    }
                    if ((N2NGOCore.Protocol.BaseHeader)packageMemberBehaviour.Value.Header != N2NGOCore.Protocol.BaseHeader.msg_byte)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Invalid Package Header2_1"));
                        return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                    }
                    if (packageMemberBehaviour.Value.external_data is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Fail_Null Package External Data2_1"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                    }

                    var memberBehaviour = N2NGOCore.Package.MsgExternalData.Decode.MsgByte(packageMemberBehaviour.Value.external_data);

                    var packageMemberID = Receive(6000, exHandle);
                    if (packageMemberID is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Null Package2_2"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                    }
                    if ((N2NGOCore.Protocol.BaseHeader)packageMemberID.Value.Header != N2NGOCore.Protocol.BaseHeader.msg_string)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Invalid Package Header2_2"));
                        return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                    }
                    if (packageMemberID.Value.external_data is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Fail_Null Package External Data2_2"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                    }

                    var memberID = N2NGOCore.Package.MsgExternalData.Decode.MsgString(packageMemberID.Value.external_data);

                    var packageMemberUserNickname = Receive(6000, exHandle);
                    if (packageMemberUserNickname is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Null Package2_3"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                    }
                    if ((N2NGOCore.Protocol.BaseHeader)packageMemberUserNickname.Value.Header != N2NGOCore.Protocol.BaseHeader.msg_string)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Invalid Package Header2_3"));
                        return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                    }
                    if (packageMemberUserNickname.Value.external_data is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Fail_Null Package External Data2_3"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                    }

                    var memberUserNickname = N2NGOCore.Package.MsgExternalData.Decode.MsgString(packageMemberUserNickname.Value.external_data);

                    var packageMemberUserIpAddress = Receive(6000, exHandle);
                    if (packageMemberUserIpAddress is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Null Package2_4"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                    }
                    if ((N2NGOCore.Protocol.BaseHeader)packageMemberUserIpAddress.Value.Header != N2NGOCore.Protocol.BaseHeader.msg_string)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Invalid Package Header2_4"));
                        return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                    }
                    if (packageMemberUserIpAddress.Value.external_data is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Fail_Null Package External Data2_4"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                    }

                    var memberUserIpAddress = N2NGOCore.Package.MsgExternalData.Decode.MsgString(packageMemberUserIpAddress.Value.external_data);

                    resultRuledMembers.Add(new() { Behaviour = (N2NGOCore.Models.Room.RuledMember.MemberBehaviour)memberBehaviour, ID = memberID, Nickname = memberUserNickname, IpAddress = memberUserIpAddress });
                }
            }

            return new ProtocolOperationReturnType<Tuple<IReadOnlyList<N2NGOCore.Models.Room.Member>, IReadOnlyList<N2NGOCore.Models.Room.RuledMember>>>(new Tuple<IReadOnlyList<N2NGOCore.Models.Room.Member>, IReadOnlyList<N2NGOCore.Models.Room.RuledMember>>(resultMembers.AsReadOnly(), resultRuledMembers.AsReadOnly()));
        }
    }

    /// <summary>
    /// (Admin) Close current room.<br/>
    /// <see cref="N2NGOCore.Protocol.BaseHeader._room_client_admin_close_room"/>
    /// </summary>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    public void AdminCloseRoom(bool exHandle = true)
    {
        lock (this)
        {
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader._room_client_admin_close_room), null, exHandle))
                goto Fail_SendFailure;

            var rpackage = Receive(6000, exHandle);
            if (rpackage is null)
            {
                if (exHandle) ClientExHandler(new("Cannot close room: Null Package"));
                return;
            }
            if ((N2NGOCore.Protocol.BaseHeader)rpackage.Value.Header == N2NGOCore.Protocol.BaseHeader._room_client_fail_1)
            {
                if (exHandle) ClientExHandler(new("Cannot close room: Member not exists in room"));
                return;
            }
            if ((N2NGOCore.Protocol.BaseHeader)rpackage.Value.Header == N2NGOCore.Protocol.BaseHeader._room_client_admin_fail_0)
            {
                if (exHandle) ClientExHandler(new("Cannot close room: Permission Denied"));
                return;
            }

            if ((N2NGOCore.Protocol.BaseHeader)rpackage.Value.Header == N2NGOCore.Protocol.BaseHeader.msg_ok)
            {
                return;
            }
        }

        if (exHandle) ClientExHandler(new("Cannot close room: Others Failure"));
        return;

    Fail_SendFailure:
        if (exHandle) ClientExHandler(new("Cannot close room: Send Failure"));
        return;
    }

    /// <summary>
    /// (Admin) Activate current room.<br/>
    /// <see cref="N2NGOCore.Protocol.BaseHeader._room_client_admin_activate_room"/>
    /// </summary>
    /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
    public void AdminActivateRoom(bool exHandle = true)
    {
        lock (this)
        {
            if (!Send(N2NGOCore.Package.MakePackage(N2NGOCore.Protocol.BaseHeader._room_client_admin_activate_room), null, exHandle))
                goto Fail_SendFailure;

            var rpackage = Receive(6000, exHandle);
            if (rpackage is null)
            {
                if (exHandle) ClientExHandler(new("Cannot activate room: Null Package"));
                return;
            }
            if ((N2NGOCore.Protocol.BaseHeader)rpackage.Value.Header == N2NGOCore.Protocol.BaseHeader._room_client_fail_1)
            {
                if (exHandle) ClientExHandler(new("Cannot activate room: Member not exists in room"));
                return;
            }
            if ((N2NGOCore.Protocol.BaseHeader)rpackage.Value.Header == N2NGOCore.Protocol.BaseHeader._room_client_admin_fail_0)
            {
                if (exHandle) ClientExHandler(new("Cannot activate room: Permission Denied"));
                return;
            }
            if ((N2NGOCore.Protocol.BaseHeader)rpackage.Value.Header == N2NGOCore.Protocol.BaseHeader._room_client_admin_activate_room_fail_0)
            {
                if (exHandle) ClientExHandler(new("Cannot activate room: Activated Lifetime reached limit"));
                return;
            }

            if ((N2NGOCore.Protocol.BaseHeader)rpackage.Value.Header == N2NGOCore.Protocol.BaseHeader.msg_ok)
            {
                return;
            }
        }

        if (exHandle) ClientExHandler(new("Cannot activate room: Others Failure"));
        return;

    Fail_SendFailure:
        if (exHandle) ClientExHandler(new("Cannot activate room: Send Failure"));
        return;
    }

    #endregion

}

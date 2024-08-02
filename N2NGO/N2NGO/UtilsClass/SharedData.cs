using N2NGO.Views;
using N2NGO.Views.SubPages;
using N2NGO.Views.SubPages.Dialogs;
using N2NGO.Views.SubPages.Dialogs.MessageDialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace N2NGO.UtilsClass
{
    public static class SharedData
    {
        public readonly static Version Version = new(4, 0, 0);
        public readonly static string VersionTag = "Dev";

        public readonly static string DefaultRoomPassword = "null";
        public static string VersionString { get { return $"{Version}-{VersionTag}"; } }

        public static string BinRefDir
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Path.Combine("Data", "BinRef", "Windows") : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? Path.Combine("Data", "BinRef", "Linux") : throw new Exception("OS Platform not support");

        public static string EdgeExecFile
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "edge.exe" : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "edge" : throw new Exception("OS Platform not support");
        public static string EdgePath =>
            Path.Combine(BinRefDir, "n2n", EdgeExecFile);

        public class N2NGOServerConnection
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
                CurrentApp.Dispatcher.InvokeAsync(() => CurrentApp.MainView.DoMessageYesNoDialog($"我们与N2N GO 服务器通出现错误（可能已经断开），信息：{ex?.Message}\n要重置连接吗？", "N2N GO 连接异常",
                    new()
                    {
                        (_) =>
                            {
                                if (_ is not DialogMessage dialogMessage || dialogMessage.MessageContent is not DialogYesNo dialogYesNo)
                                    throw new Exception("Cannot get DialogYesNo");

                                if ( dialogYesNo.YesNo == DialogYesNo.YesNoE.Yes)
                                    Task.Run(() => CurrentApp.ResetConnection(true));
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
            public bool Send(N2NGO_Core.Package package, int? timeOut = null, bool exHandle = true)
            {
                lock (this)
                {
                    N2NGO_Core.Package.IO_Tool iO_Tool = new();
                    if (timeOut != null)
                    {
                        iO_Tool.WriteTimeOut = timeOut.Value;
                    }

                    if (!iO_Tool.Send(_client, package, timeOut != null))
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

                        return false;
                    }

                    return true;
                }
            }

            /// <summary>
            /// <see cref="Package.IO_Tool.Receive">Receive</see> a <see cref="Package">packageMember</see> through the <see cref="_client">Connection Client</see>
            /// </summary>
            /// <param name="timeOut">Sets <seealso cref="TcpClient.ReceiveTimeout"/></param>
            /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
            /// <returns>If failed, a null packageMember will be returned.</returns>
            public N2NGO_Core.Package? Receive(int? timeOut = null, bool exHandle = true)
            {
                lock (this)
                {
                    N2NGO_Core.Package? package = null;

                    N2NGO_Core.Package.IO_Tool iO_Tool = new();
                    if (timeOut != null)
                    {
                        iO_Tool.ReadTimeOut = timeOut.Value;
                    }

                    package = iO_Tool.Receive(_client, null, timeOut != null);

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
                        N2NGO_Core.Package pkg = N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader.peek);
                        if (!Send(pkg, null, !exHandle))
                        {
                            if (exHandle)
                                ClientExHandler(new("Cannot send peek package"));
                            return false;
                        }

                        byte[] bytes = new byte[1];
                        _client.GetStream().Read(bytes, 0, 1);
                        N2NGO_Core.Package pkg_get = N2NGO_Core.Package.ResolvePackage(bytes);
                        return (N2NGO_Core.Protocol.BaseHeader)pkg_get.Header == N2NGO_Core.Protocol.BaseHeader.peek_ok;
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
                        N2NGO_Core.Package pkg = N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader.peek);
                        if (!Send(pkg, null, !exHandle))
                        {
                            stopwatch.Stop();
                            peekTimeSpan = stopwatch.Elapsed;

                            if (exHandle)
                                ClientExHandler(new("Cannot send peek package"));
                            return false;
                        }

                        byte[] bytes = new byte[1];
                        _client.GetStream().Read(bytes, 0, 1);
                        N2NGO_Core.Package pkg_get = N2NGO_Core.Package.ResolvePackage(bytes);
                        stopwatch.Stop();
                        peekTimeSpan = stopwatch.Elapsed;
                        return (N2NGO_Core.Protocol.BaseHeader)pkg_get.Header == N2NGO_Core.Protocol.BaseHeader.peek_ok;
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
            /// <see cref="N2NGO_Core.Protocol.BaseHeader._user_key_get"></see>
            /// </summary>
            /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
            public ProtocolOperationReturnType<string> GetUserKey(bool exHandle = true)
            {
                if (!IsConnected())
                    return new(ProtocolOperationReturnStatus.Fail_NotConnected);

                if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader._user_key_get), null, !exHandle))
                {
                    if (exHandle)
                        ClientExHandler(new("GetUserKey send package fail"));
                    return new(ProtocolOperationReturnStatus.Fail_SendFailure);
                }

                var rpackage = Receive(6000, !exHandle);
                if (rpackage == null)
                {
                    if (exHandle)
                        ClientExHandler(new("GetUserKey receive package null"));
                    return new(ProtocolOperationReturnStatus.Fail_NullPackage);
                }

                var package = rpackage.Value;
                if ((N2NGO_Core.Protocol.BaseHeader)package.Header != N2NGO_Core.Protocol.BaseHeader.msg_string_long)
                    return new(ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                if (package.external_data == null)
                    return new(ProtocolOperationReturnStatus.Fail_NullPackageExternalData);

                return new(N2NGO_Core.Package.MsgExternalData.Decode.MsgStringLong(package.external_data));
            }

            /// <summary>
            /// Pull number of onlines<br/>
            /// <see cref="N2NGO_Core.Protocol.BaseHeader._pull_online_total"></see>
            /// </summary>
            /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
            public ProtocolOperationReturnType<int> PullTotalOnlines(bool exHandle = true)
            {
                if (!IsConnected())
                    return new(-1, ProtocolOperationReturnStatus.Fail_NotConnected);

                N2NGO_Core.Package? p = null;
                lock (this)
                {
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader._pull_online_total), null, !exHandle))
                    {
                        if (exHandle)
                            ClientExHandler(new("PullTotalOnlines send package fail"));
                        return new(-1, ProtocolOperationReturnStatus.Fail_SendFailure);
                    }

                    p = Receive(null, !exHandle);
                }

                if (p == null)
                {
                    if (exHandle)
                        ClientExHandler(new("PullTotalOnlines receive package null"));
                    return new(-1, ProtocolOperationReturnStatus.Fail_NullPackage);
                }

                var v = p.Value;
                if ((N2NGO_Core.Protocol.BaseHeader)v.Header != N2NGO_Core.Protocol.BaseHeader.msg_long)
                    return new(-1, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);

                if (v.external_data == null)
                    return new(-1, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);

                var totalMembers = N2NGO_Core.Package.MsgExternalData.Decode.MsgLong(v.external_data);
                return new(totalMembers);
            }

            /// <summary>
            /// Check if the room exists.<br/>
            /// <see cref="N2NGO_Core.Protocol.BaseHeader._rooms_is_code_exists"></see>
            /// </summary>
            /// <param name="roomCode">Room code to check</param>
            /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
            public ProtocolOperationReturnType<bool> CheckRoomExists(string roomCode, bool exHandle = true)
            {
                N2NGO_Core.Package? _pkg_get = null;
                lock (this)
                {
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader._rooms_is_code_exists, N2NGO_Core.Package.MsgExternalData.Encode.MsgString(roomCode)), null, !exHandle))
                    {
                        if (exHandle)
                            ClientExHandler(new("CheckRoomExists send package fail"));
                        return new(false, ProtocolOperationReturnStatus.Fail_NullPackage);
                    }

                    _pkg_get = Receive(null, !exHandle);
                    if (_pkg_get == null)
                    {
                        if (exHandle)
                            ClientExHandler(new("CheckRoomExists receive package null"));
                        return new(false, ProtocolOperationReturnStatus.Fail_NullPackage);
                    }
                }

                N2NGO_Core.Package pkg_get = _pkg_get.Value;
                if ((N2NGO_Core.Protocol.BaseHeader)pkg_get.Header != N2NGO_Core.Protocol.BaseHeader.msg_byte)
                    return new(false, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);

                if (pkg_get.external_data == null)
                    return new(false, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);

                return new(N2NGO_Core.Package.MsgExternalData.Decode.MsgByte(pkg_get.external_data) != 0);
            }

            /// <summary>
            /// Get room info by code.<br/>
            /// <see cref="N2NGO_Core.Protocol.BaseHeader._rooms_get_room"></see>
            /// </summary>
            /// <param name="roomCode">Room code</param>
            /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
            public ProtocolOperationReturnType<N2NGO_Core.Models.Room> GetRoomByCode(string roomCode, bool exHandle = true)
            {
                N2NGO_Core.Models.Room room = new();

                var roomExists = CheckRoomExists(roomCode, !exHandle);
                if ((!roomExists.IsSuccessfulStatusCode) || !roomExists.Value)
                {
                    if (exHandle)
                        ClientExHandler(new($"GetRoomByCode Fail: Cannot get room by code due to room not exists.(CheckRoomExists Status Code: {roomExists.Status.ToString()})"));
                    return new(null, ProtocolOperationReturnStatus.Fail_Others);
                }

                lock (this)
                {
                    Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader._rooms_get_room), null, !exHandle);
                    Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader.msg_string, N2NGO_Core.Package.MsgExternalData.Encode.MsgString(roomCode)), null, !exHandle);

                    N2NGO_Core.Package[] roomPackage = new N2NGO_Core.Package[6];
                    for (int i = 0; i < roomPackage.Length; i++)
                    {
                        var pkg_get = Receive(null, !exHandle);
                        if (pkg_get == null || pkg_get.Value.external_data == null)
                            goto invalid;

                        roomPackage[i] = pkg_get.Value;
                    }

                    {
                        byte[]? edata = roomPackage[0].external_data;
                        if ((N2NGO_Core.Protocol.BaseHeader)roomPackage[0].Header != N2NGO_Core.Protocol.BaseHeader.msg_string || edata == null)
                            goto invalid;
                        room.RoomCode = N2NGO_Core.Package.MsgExternalData.Decode.MsgString(edata);
                    }
                    {
                        byte[]? edata = roomPackage[1].external_data;
                        if ((N2NGO_Core.Protocol.BaseHeader)roomPackage[1].Header != N2NGO_Core.Protocol.BaseHeader.msg_string || edata == null)
                            goto invalid;
                        room.RoomName = N2NGO_Core.Package.MsgExternalData.Decode.MsgString(edata);
                    }
                    {
                        byte[]? edata = roomPackage[2].external_data;
                        if ((N2NGO_Core.Protocol.BaseHeader)roomPackage[2].Header != N2NGO_Core.Protocol.BaseHeader.msg_byte || edata == null)
                            goto invalid;
                        room.IsRoomPasswordNeeded = N2NGO_Core.Package.MsgExternalData.Decode.MsgByte(edata) == 1;
                    }
                    {
                        byte[]? edata = roomPackage[3].external_data;
                        if ((N2NGO_Core.Protocol.BaseHeader)roomPackage[3].Header != N2NGO_Core.Protocol.BaseHeader.msg_ulong || edata == null)
                            goto invalid;
                        room.MainColor = new N2NGO_Core.Objects.RoomColor(N2NGO_Core.Package.MsgExternalData.Decode.MsgULong(edata));
                    }
                    {
                        byte[]? edata = roomPackage[4].external_data;
                        if ((N2NGO_Core.Protocol.BaseHeader)roomPackage[4].Header != N2NGO_Core.Protocol.BaseHeader.msg_ulong || edata == null)
                            goto invalid;
                        room.MinorColor = new N2NGO_Core.Objects.RoomColor(N2NGO_Core.Package.MsgExternalData.Decode.MsgULong(edata));
                    }
                    {
                        byte[]? edata = roomPackage[5].external_data;
                        if ((N2NGO_Core.Protocol.BaseHeader)roomPackage[5].Header != N2NGO_Core.Protocol.BaseHeader.msg_ulong || edata == null)
                            goto invalid;
                        room.Members.AddRange(new N2NGO_Core.Models.Room.Member[N2NGO_Core.Package.MsgExternalData.Decode.MsgULong(edata)]);
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
            /// <see cref="N2NGO_Core.Protocol.BaseHeader._rooms_create"></see>
            /// </summary>
            /// <param name="roomName">Room name</param>
            /// <param name="isRoomInvisible">If true, the room will not be displayed in the lobby and vice versa</param>
            /// <param name="isRoomPasswordNeeded">If true, the room will validate the roomPassword and vice versa</param>
            /// <param name="roomPassword">Password for authentication to join the room</param>
            /// <param name="mainColor">Main room color theme</param>
            /// <param name="minorColor">Minor room color theme</param>
            /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
            /// <returns>Room code and Admin key in string array</returns>
            public ProtocolOperationReturnType<string[]> CreateRoom(string roomName, bool isRoomInvisible, bool isRoomPasswordNeeded, string roomPassword, UInt32 mainColor, UInt32 minorColor, bool exHandle = true)
            {
                string RoomCode = GenerateRoomCode();
                while (true)
                {
                    ProtocolOperationReturnType<bool> roomExistsResult = CheckRoomExists(RoomCode, exHandle);
                    if (roomExistsResult.Status != ProtocolOperationReturnStatus.Success)
                        return new(null, roomExistsResult.Status);
                    if (!roomExistsResult.Value)
                        break;

                    RoomCode = GenerateRoomCode();
                }

                lock (this)
                {
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader._rooms_create), null, exHandle))
                        goto CreateRoom_Fail_Send;
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader.msg_string, N2NGO_Core.Package.MsgExternalData.Encode.MsgString(RoomCode)), null, exHandle))
                        goto CreateRoom_Fail_Send;
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader.msg_string, N2NGO_Core.Package.MsgExternalData.Encode.MsgString(roomName)), null, exHandle))
                        goto CreateRoom_Fail_Send;
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader.msg_byte, N2NGO_Core.Package.MsgExternalData.Encode.MsgByte((byte)(isRoomInvisible ? 1 : 0))), null, exHandle))
                        goto CreateRoom_Fail_Send;
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader.msg_byte, N2NGO_Core.Package.MsgExternalData.Encode.MsgByte((byte)(isRoomPasswordNeeded ? 1 : 0))), null, exHandle))
                        goto CreateRoom_Fail_Send;
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader.msg_string, N2NGO_Core.Package.MsgExternalData.Encode.MsgString(roomPassword)), null, exHandle))
                        goto CreateRoom_Fail_Send;
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader.msg_ulong, N2NGO_Core.Package.MsgExternalData.Encode.MsgULong(mainColor)), null, exHandle))
                        goto CreateRoom_Fail_Send;
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader.msg_ulong, N2NGO_Core.Package.MsgExternalData.Encode.MsgULong(minorColor)), null, exHandle))
                        goto CreateRoom_Fail_Send;

                    var packageReceive = Receive(6000, !exHandle);
                    if (packageReceive is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot create room: Null Package 0"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                    }
                    var package = packageReceive.Value;
                    if ((N2NGO_Core.Protocol.BaseHeader)package.Header != N2NGO_Core.Protocol.BaseHeader.msg_string_long)
                    {
                        if (exHandle) ClientExHandler(new("Cannot create room: Invalid Package Header 0"));
                        return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                    }
                    if (package.external_data is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot create room: Null Package External Data 0"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                    }

                    packageReceive = Receive(6000, !exHandle);
                    if (packageReceive is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot create room: Null Package 1"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                    }
                    if ((N2NGO_Core.Protocol.BaseHeader)packageReceive.Value.Header != N2NGO_Core.Protocol.BaseHeader.msg_ok)
                    {
                        if (exHandle) ClientExHandler(new("Cannot create room: Invalid Package Header 1"));
                        return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                    }

                    return new(new string[] { RoomCode, N2NGO_Core.Package.MsgExternalData.Decode.MsgStringLong(package.external_data) });
                }

            CreateRoom_Fail_Send:
                if (exHandle) ClientExHandler(new("Cannot create room: Send Failure"));
                return new(null, ProtocolOperationReturnStatus.Fail_SendFailure);
            }

            /// <summary>
            /// Join room<br/>
            /// <see cref="N2NGO_Core.Protocol.BaseHeader._room_client_join"/>
            /// </summary>
            /// <param name="roomCode"></param>
            /// <param name="roomPassword"></param>
            /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
            /// <returns>User Id in Room</returns>
            public ProtocolOperationReturnType<string> JoinRoom(string roomCode, string roomPassword, bool exHandle = true)
            {
                lock (this)
                {
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader._room_client_join), null, !exHandle))
                        goto JoinRoom_Fail_Send;
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader.msg_string, N2NGO_Core.Package.MsgExternalData.Encode.MsgString(roomCode)), null, !exHandle))
                        goto JoinRoom_Fail_Send;
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader.msg_string, N2NGO_Core.Package.MsgExternalData.Encode.MsgString(roomPassword)), null, !exHandle))
                        goto JoinRoom_Fail_Send;

                    var rpackage = Receive(6000, !exHandle);
                    if (rpackage is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot join room: Null Package 0"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                    }
                    var rpackagev = rpackage.Value;
                    if ((N2NGO_Core.Protocol.BaseHeader)rpackagev.Header == N2NGO_Core.Protocol.BaseHeader._room_client_join_fail_0)
                    {
                        if (exHandle) ClientExHandler(new("Cannot join room: You can only be in one room at a time."));
                        return new(null, ProtocolOperationReturnStatus.Fail_Others);
                    }
                    if ((N2NGO_Core.Protocol.BaseHeader)rpackagev.Header == N2NGO_Core.Protocol.BaseHeader._room_client_join_fail_1)
                    {
                        if (exHandle) ClientExHandler(new("Cannot join room: The specified room does not exist."));
                        return new(null, ProtocolOperationReturnStatus.Fail_Others);
                    }
                    if ((N2NGO_Core.Protocol.BaseHeader)rpackagev.Header == N2NGO_Core.Protocol.BaseHeader._room_client_join_fail_2)
                    {
                        if (exHandle) ClientExHandler(new("Cannot join room: verification failure."));
                        return new(null, ProtocolOperationReturnStatus.Fail_Others);
                    }
                    if ((N2NGO_Core.Protocol.BaseHeader)rpackagev.Header == N2NGO_Core.Protocol.BaseHeader._room_client_join_fail_00)
                    {
                        var rrpackage = Receive(6000, !exHandle);
                        if (rrpackage is null)
                        {
                            if (exHandle) ClientExHandler(new("Cannot join room: Null Package 1"));
                            return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                        }
                        var rrpackagev = rrpackage.Value;

                        if ((N2NGO_Core.Protocol.BaseHeader)rrpackagev.Header != N2NGO_Core.Protocol.BaseHeader.msg_long)
                        {
                            if (exHandle) ClientExHandler(new("Cannot join room: Invalid Package Header 1"));
                            return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                        }
                        if (rrpackagev.external_data is null)
                        {
                            if (exHandle) ClientExHandler(new("Cannot join room: Null Package External Data 1"));
                            return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                        }
                        var rrr = N2NGO_Core.Package.MsgExternalData.Decode.MsgLong(rrpackagev.external_data);
                        if (exHandle) ClientExHandler(new($"Cannot join room: Others: {rrr}"));   /// <see cref="N2NGO_Core.Models.Room.MemberJoin"/>
                    }
                    if ((N2NGO_Core.Protocol.BaseHeader)rpackagev.Header != N2NGO_Core.Protocol.BaseHeader.msg_string)
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
                    return new(N2NGO_Core.Package.MsgExternalData.Decode.MsgString(rpackagev.external_data));
                }

            JoinRoom_Fail_Send:
                if (exHandle) ClientExHandler(new("Cannot join room: Send Failure"));
                return new(null, ProtocolOperationReturnStatus.Fail_SendFailure);
            }

            /// <summary>
            /// Leave current room<br/>
            /// <see cref="N2NGO_Core.Protocol.BaseHeader._room_client_leave"/>
            /// </summary>
            /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
            public void LeaveRoom(bool exHandle = true)
            {
                lock (this)
                {
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader._room_client_leave), null, !exHandle))
                    {
                        if (exHandle) ClientExHandler(new("Cannot leave room: Send Failure"));
                        return;
                    }
                    var rpackage = Receive(6000, !exHandle);
                    if (rpackage is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot leave room: Null Package"));
                        return;
                    }
                    if ((N2NGO_Core.Protocol.BaseHeader)rpackage.Value.Header != N2NGO_Core.Protocol.BaseHeader.msg_ok)
                    {
                        if (exHandle) ClientExHandler(new("Cannot leave room: Invalid Package Header"));
                        return;
                    }

                    return;
                }
            }

            /// <summary>
            /// Push current member of room<br/>
            /// <see cref="N2NGO_Core.Protocol.BaseHeader._room_client_push"/>
            /// </summary>
            /// <param name="userNickname"></param>
            /// <param name="userIpAddress"></param>
            /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
            public void MemberPush(string userNickname, string userIpAddress, bool exHandle = true)
            {
                lock (this)
                {
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader._room_client_push), null, !exHandle))
                        goto Fail_SendFailure;
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader.msg_string, N2NGO_Core.Package.MsgExternalData.Encode.MsgString(userNickname)), null, !exHandle))
                        goto Fail_SendFailure;
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader.msg_string, N2NGO_Core.Package.MsgExternalData.Encode.MsgString(userIpAddress)), null, !exHandle))
                        goto Fail_SendFailure;

                    var rpackage = Receive(6000, !exHandle);
                    if (rpackage is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot push the Member to room: Null Package"));
                        return;
                    }
                    if ((N2NGO_Core.Protocol.BaseHeader)rpackage.Value.Header == N2NGO_Core.Protocol.BaseHeader._room_client_fail_1)
                    {
                        if (exHandle) ClientExHandler(new("Cannot push the Member to room: Member not exists in room"));
                        return;
                    }

                    if ((N2NGO_Core.Protocol.BaseHeader)rpackage.Value.Header == N2NGO_Core.Protocol.BaseHeader.msg_ok)
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
            /// <see cref="N2NGO_Core.Protocol.BaseHeader._room_client_pull"/>
            /// </summary>
            /// <param name="exHandle">If true and an exception occurs, <see cref="ClientExHandler"/> will be called.</param>
            public ProtocolOperationReturnType<IReadOnlyList<N2NGO_Core.Models.Room.Member>> MemberPull(bool exHandle = true)
            {
                lock (this)
                {
                    if (!Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader._room_client_pull), null, !exHandle))
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Send Failure"));
                        return new(null, ProtocolOperationReturnStatus.Fail_SendFailure);
                    }

                    var packageMembersCount = Receive(6000, !exHandle);
                    if (packageMembersCount is null)
                    {
                        if (exHandle) ClientExHandler(new("Cannot pull member of room: Null Package0"));
                        return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                    }
                    if ((N2NGO_Core.Protocol.BaseHeader)packageMembersCount.Value.Header != N2NGO_Core.Protocol.BaseHeader.msg_ulong)
                    {
                        switch ((N2NGO_Core.Protocol.BaseHeader)packageMembersCount.Value.Header)
                        {
                            default:
                                {
                                    if (exHandle) ClientExHandler(new("Cannot pull member of room: Invalid Package Header0"));
                                    break;
                                }

                            case N2NGO_Core.Protocol.BaseHeader._room_client_fail_1:
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

                    var membersCount = N2NGO_Core.Package.MsgExternalData.Decode.MsgULong(packageMembersCount.Value.external_data);
                    var result = new List<N2NGO_Core.Models.Room.Member>();

                    for (ulong i = 0; i < membersCount; i++)
                    {
                        var packageMemberUserNickname = Receive(6000, !exHandle);
                        if (packageMemberUserNickname is null)
                        {
                            if (exHandle) ClientExHandler(new("Cannot pull member of room: Null Package1"));
                            return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                        }
                        if ((N2NGO_Core.Protocol.BaseHeader)packageMemberUserNickname.Value.Header != N2NGO_Core.Protocol.BaseHeader.msg_string)
                        {
                            if (exHandle) ClientExHandler(new("Cannot pull member of room: Invalid Package Header1"));
                            return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                        }
                        if (packageMemberUserNickname.Value.external_data is null)
                        {
                            if (exHandle) ClientExHandler(new("Cannot pull member of room: Fail_Null Package External Data1"));
                            return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                        }

                        var memberUserNickname = N2NGO_Core.Package.MsgExternalData.Decode.MsgString(packageMemberUserNickname.Value.external_data);

                        var packageMemberUserIpAddress = Receive(6000, !exHandle);
                        if (packageMemberUserIpAddress is null)
                        {
                            if (exHandle) ClientExHandler(new("Cannot pull member of room: Null Package2"));
                            return new(null, ProtocolOperationReturnStatus.Fail_NullPackage);
                        }
                        if ((N2NGO_Core.Protocol.BaseHeader)packageMemberUserIpAddress.Value.Header != N2NGO_Core.Protocol.BaseHeader.msg_string)
                        {
                            if (exHandle) ClientExHandler(new("Cannot pull member of room: Invalid Package Header2"));
                            return new(null, ProtocolOperationReturnStatus.Fail_InvalidPackageHeader);
                        }
                        if (packageMemberUserIpAddress.Value.external_data is null)
                        {
                            if (exHandle) ClientExHandler(new("Cannot pull member of room: Fail_Null Package External Data2"));
                            return new(null, ProtocolOperationReturnStatus.Fail_NullPackageExternalData);
                        }

                        var memberUserIpAddress = N2NGO_Core.Package.MsgExternalData.Decode.MsgString(packageMemberUserIpAddress.Value.external_data);

                        result.Add(new() { Nickname = memberUserNickname, IpAddress = memberUserIpAddress });
                    }

                    return new(result.AsReadOnly());
                }
            }

            #endregion
        }

        public class RoomConnection
        {
            public bool IsConnected { get; set; } = false;
            public string CurrentRoomCode { get; set; } = string.Empty;
        }

        public class ExecLog
        {
            public string LogOut { get; private set; } = string.Empty;
            public Task? ProcessExecuteTask { get; private set; }

            /// <summary>
            /// Begin command execution asynchronously
            /// </summary>
            /// <param name="command">Command</param>
            /// <returns>If there is an existing <see cref="ProcessExecuteTask"/> that has not been completed, null is returned.</returns>
            public async Task<int?> ExecuteAsync(string command, bool newLog)
            {
                if (ProcessExecuteTask is not null && !ProcessExecuteTask.IsCompleted)
                    return null;

                if (newLog)
                    LogOut = string.Empty;

                AddLog("Executing Command: " + command + Environment.NewLine);
                Process ProcessExecuteCommand = new()
                {
                    StartInfo = new() { FileName = "cmd.exe", Arguments = $"/c {command}", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true },
                };

                ProcessExecuteCommand.OutputDataReceived += OutputDataReceived;
                ProcessExecuteCommand.ErrorDataReceived += OutputDataReceived;

                ProcessExecuteCommand.Start();

                ProcessExecuteCommand.BeginOutputReadLine();
                ProcessExecuteCommand.BeginErrorReadLine();

                var task = ProcessExecuteCommand.WaitForExitAsync();
                await task;

                return ProcessExecuteCommand.ExitCode;
            }

            protected virtual void AddLog(string data)
            {
                LogOut += data + Environment.NewLine;

                CurrentApp.Dispatcher.Invoke(() =>
                {
                    // Output to LogPage
                    LogPage? logPage = CurrentApp.MainView.PageLog;
                    if (logPage != null)
                        logPage.LogBox.Text += data + Environment.NewLine;
                });
            }

            private void OutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                if (e.Data is not null)
                {
                    AddLog(e.Data);
                }
            }
        }

        public static class N2NEdgeLogHelper
        {
            public enum EdgeDeviceAllocating
            {
                IP, Mask, MAC
            }
            public static Dictionary<EdgeDeviceAllocating, string?> GetEdgeDeviceAllocation(string log)
            {
                Dictionary<EdgeDeviceAllocating, string?> result = new()
                {
                    { EdgeDeviceAllocating.IP, null },
                    { EdgeDeviceAllocating.Mask, null },
                    { EdgeDeviceAllocating.MAC, null }
                };

                foreach (var line in log.Split(Environment.NewLine).Reverse())
                {
                    if (line.Contains("created local tap device IP:"))
                    {
                        string ip = line[(line.IndexOf("IP: ") + "IP: ".Length)..];
                        result[EdgeDeviceAllocating.IP] = ip.Remove(ip.IndexOf(", Mask:"));

                        string mask = line[(line.IndexOf("Mask: ") + "Mask: ".Length)..];
                        result[EdgeDeviceAllocating.Mask] = mask.Remove(mask.IndexOf(", MAC:"));

                        result[EdgeDeviceAllocating.MAC] = line[(line.IndexOf("MAC: ") + "MAC: ".Length)..];
                    }
                }

                return result;
            }

            public static bool IsEdgeConnectedToSupernode(string log) => log.Contains("[OK] edge <<< ================ >>> supernode");
        }

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
            public static MainView MainView { get => (MainView)App.Current.MainWindow; }
            public static EasyConfig Config { get; } = new(Path.Combine(N2NGO_Core.UserDef.N2NGO_N2NGO_AppData_Path, "UserData/config.ini"));

            // 7476 & 7478 for release
            // 7477 & 7479 for alpha
            public static N2NGOServerConnection N2NGOServerConnection { get; set; } = new(new(IPAddress.Parse(Config.Get("IpGlobalServer", "43.143.37.61")), int.Parse(Config.Get("PortGlobalServer", "7477"))), int.Parse(CurrentApp.Config.Get("PortSupernodeServer", "7479")));
            public static RoomConnection RoomConnection { get; set; } = new();

            public static ExecLog N2NExecLog { get; } = new();

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

            public static bool EnterRoom(string roomCode, string roomPassword)
            {
                var joinRoomResult = N2NGOServerConnection.JoinRoom(roomCode, roomPassword);
                if (!joinRoomResult.IsSuccessfulStatusCode)
                    return false;

                Dispatcher.Invoke(() => MainView.PageRoom.BeginRefresh());
                RoomConnection.CurrentRoomCode = roomCode;
                return RoomConnection.IsConnected = true;
            }
            public static void LeaveRoom()
            {
                TerminateEdge();
                Dispatcher.Invoke(() => MainView.PageRoom.EndRefresh());
                RoomConnection.IsConnected = false;
                RoomConnection.CurrentRoomCode = string.Empty;
                N2NGOServerConnection.LeaveRoom();
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
                            Dispatcher.Invoke(() => MainView.DoMessageDialog("@LOCALE_DialogConnectionSuccessful_Content", "@LOCALE_DialogConnectionSuccessful_Title"));
                        return Peek();
                    }
                    else
                        return false;
                }
            }
            public static void ResetConnection(bool echo = false, string? serverIp = null, int? serverPort = null, int? supernodeServerPort = null)
            {
                LeaveRoom();

                N2NGOServerConnection.Close();
                N2NGOServerConnection = new(new(IPAddress.Parse(Config.Get("IpGlobalServer", "43.143.37.61")), int.Parse(Config.Get("PortGlobalServer", "7477"))), int.Parse(CurrentApp.Config.Get("PortSupernodeServer", "7479")));
                if (serverIp != null)
                    N2NGOServerConnection.ServerIPEndPoint.Address = IPAddress.Parse(serverIp);
                if (serverPort != null)
                    N2NGOServerConnection.ServerIPEndPoint.Port = serverPort.Value;
                if (supernodeServerPort != null)
                    N2NGOServerConnection.ServerSupernodePort = supernodeServerPort.Value;
                ConnectAndPeek(echo);
            }

            public static async void DownloadN2NGOUpdateAsync()
            {
                try
                {
                    using HttpClient client = new();
                    using HttpResponseMessage response = await
                        client.GetAsync($"http://{(N2NGOServerConnection.Client.Client.RemoteEndPoint as IPEndPoint) ?? throw new NullReferenceException(nameof(N2NGOServerConnection.Client.Client.RemoteEndPoint))}:{N2NGO_Core.UserDef.ExtendServerOptions.N2NGO_File_Server_Port}/{N2NGO_Core.UserDef.N2NGOUpdateInstallerFileName}");
                    try
                    {
                        response.EnsureSuccessStatusCode();

                        using FileStream fileStream = File.Create(N2NGO_Core.UserDef.N2NGOUpdateInstallerFilePath);
                        await response.Content.CopyToAsync(fileStream);

                        MainView.DispatcherDoMessageDialog($"N2N GO 更新包已下载({GetFileSizeReadableString(fileStream.Length)})，将在N2N GO 关闭后升级。", "更新N2N GO");
                        Config.Set("NeedUpdate", "1");
                    }
                    catch (HttpRequestException ex)
                    {
                        MainView.DispatcherDoMessageDialog($"从服务器下载更新包时失败：{ex.Message}", "更新N2N GO");
                    }
                }
                catch (Exception ex)
                {
                    MainView.DispatcherDoMessageDialog($"从服务器下载更新包时失败：{ex.Message}", "更新N2N GO");
                }
            }
            /// <summary>
            /// Check update for N2N GO client asynchronous
            /// Coding Example: <br/>
            /// <code>Task.Run(() => SharedData.CheckN2NGOClientUpdate());</code>
            /// </summary>
            public static void CheckN2NGOClientUpdate()
            {
                lock (N2NGOServerConnection)
                    if (Peek())
                    {
                        try
                        {
                            N2NGO_Core.Package? _pkg_get = null;
                            lock (N2NGOServerConnection)
                            {
                                N2NGOServerConnection.Send(N2NGO_Core.Package.MakePackage(N2NGO_Core.Protocol.BaseHeader._ver_check)); // send _ver_check ID

                                _pkg_get = N2NGOServerConnection.Receive();
                                if (_pkg_get == null || (N2NGO_Core.Protocol.BaseHeader)_pkg_get.Value.Header != N2NGO_Core.Protocol.BaseHeader.msg_string || _pkg_get.Value.external_data == null)
                                {
                                    Dispatcher.Invoke(() => MainView.DispatcherDoMessageDialog("@LOCALE_DialogUpdate_Fail_InvalidServer_Content", "@LOCALE_DialogUpdate_Title"));
                                    return;
                                }
                            }

                            N2NGO_Core.Package pkg_get = _pkg_get.Value;

                            string versionString = N2NGO_Core.Package.MsgExternalData.Decode.MsgString(pkg_get.external_data);
                            Version serverVersionGet = Version.Parse(versionString);
                            if (SharedData.Version < serverVersionGet)
                            {
                                StringBuilder strNewVersionMsg = new();
                                strNewVersionMsg.AppendLine($"{SharedData.VersionString} -> {serverVersionGet.ToString()} ?");

                                Dispatcher.Invoke(() => MainView.DispatcherDoMessageYesNoDialog(strNewVersionMsg.ToString(), "@LOCALE_DialogUpdate_NewVersion_Title",
                                         new() {
                                        (_dialogMessage) =>
                                        {
                                            if (_dialogMessage is not DialogMessage dialogMessage)
                                                throw new NullReferenceException(nameof(dialogMessage));
                                            if ( dialogMessage.MessageContent is not DialogYesNo dialogYesNo || dialogYesNo.YesNo != DialogYesNo.YesNoE.Yes)
                                                return;

                                            DownloadN2NGOUpdateAsync();
                                        }
                                         }
                                     )
                                );
                            }
                            else
                                Dispatcher.Invoke(() => MainView.DispatcherDoMessageDialog("@LOCALE_DialogUpdate_UpToDate_Content", "@LOCALE_DialogUpdate_Title"));
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() => MainView.DispatcherDoMessageDialog($"无法从服务器获取更新：{ex.Message}"));
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

                Console.WriteLine($"({tag ?? "App"}) Memory usage: {memoryUsageInMB} MBytes");
            }
        }

        public static class UIAnimation
        {
            public static void Refresh()
            {
                MouseDownColorAnimation = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_500"], Duration = TimeSpan.FromMilliseconds(100) };
                MouseUpColorAnimation = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_300"], Duration = TimeSpan.FromMilliseconds(300) };
                MouseEnterColorAnimation = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_300"], Duration = TimeSpan.FromMilliseconds(120) };
                MouseLeaveColorAnimation = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_500"], Duration = TimeSpan.FromMilliseconds(170) };
            }


            public static ColorAnimation MouseDownColorAnimation { get; private set; } = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_500"], Duration = TimeSpan.FromMilliseconds(100) };
            public static ColorAnimation MouseUpColorAnimation { get; private set; } = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_300"], Duration = TimeSpan.FromMilliseconds(300) };
            public static ColorAnimation MouseEnterColorAnimation { get; private set; } = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_200"], Duration = TimeSpan.FromMilliseconds(120) };
            public static ColorAnimation MouseLeaveColorAnimation { get; private set; } = new() { To = (Color)((ResourceDictionary)App.Current.Resources["CurrentColorPalette"])["Palette_300"], Duration = TimeSpan.FromMilliseconds(170) };

            public static DoubleAnimation SmallerScaleAnimation { get; private set; } = new() { To = 0.97, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            public static DoubleAnimation SmallSmallerScaleAnimation { get; private set; } = new() { To = 0.92, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            public static DoubleAnimation NormalScaleAnimation { get; private set; } = new() { To = 1, Duration = TimeSpan.FromSeconds(0.25), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            public static DoubleAnimation BiggerScaleAnimation { get; private set; } = new() { To = 1.03, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };


            public static void Button_MouseUp(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, MouseUpColorAnimation);

                TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                ScaleTransform st = (ScaleTransform)TG.Children[0];

                st.BeginAnimation(ScaleTransform.ScaleXProperty, NormalScaleAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, NormalScaleAnimation);

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

                ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, MouseDownColorAnimation);

                TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                ScaleTransform st = (ScaleTransform)TG.Children[0];

                if (e != null)
                {
                    var p = e.MouseDevice.GetPosition((Control)sender);

                    st.CenterX = p.X;
                    st.CenterY = p.Y;
                }

                st.BeginAnimation(ScaleTransform.ScaleXProperty, SmallSmallerScaleAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, SmallSmallerScaleAnimation);
            }
            public static void Button_MouseLeave(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, MouseLeaveColorAnimation);
            }
            public static void Button_MouseEnter(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, MouseEnterColorAnimation);
            }
            public static void Button_MouseMove(object sender, MouseEventArgs? e)
            {
                if (e != null)
                {
                    var p = e.MouseDevice.GetPosition((Control)sender);

                    TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                    ScaleTransform st = (ScaleTransform)TG.Children[0];
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

                    button.MouseEnter -= Button_MouseEnter;
                    button.MouseLeave -= Button_MouseLeave;
                    button.PreviewMouseDown -= Button_MouseDown;
                    button.PreviewMouseUp -= Button_MouseUp;
                    button.PreviewMouseMove -= Button_MouseMove;

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

                st.BeginAnimation(ScaleTransform.ScaleXProperty, NormalScaleAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, NormalScaleAnimation);
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

                st.BeginAnimation(ScaleTransform.ScaleXProperty, SmallerScaleAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, SmallerScaleAnimation);
            }
            public static void Card_MouseLeave(object sender, MouseEventArgs? e)
            {
                if ((bool)((Control)sender).Resources["UIA_Locked"])
                    return;

                TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                ScaleTransform st = (ScaleTransform)TG.Children[0];

                st.BeginAnimation(ScaleTransform.ScaleXProperty, NormalScaleAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, NormalScaleAnimation);

                //var anim = MouseLeaveColorAnimation;
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

                st.BeginAnimation(ScaleTransform.ScaleXProperty, BiggerScaleAnimation);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, BiggerScaleAnimation);
                //((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, MouseEnterColorAnimation);
            }
            public static void Card_MouseMove(object sender, MouseEventArgs? e)
            {
                if (e != null)
                {
                    var p = e.MouseDevice.GetPosition((Control)sender);

                    TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
                    ScaleTransform st = (ScaleTransform)TG.Children[0];
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


        public static string GenerateRoomCode()
        {
            return new Random((int)DateTime.Now.Ticks).Next(0, 1000000).ToString("x");

            ////string baseNumber = "0123456789";
            //string generatedNumber = GenerateNumber(input);
            //string mixedNumber = MixNumber(generatedNumber);
            //string hexNumber = ToHex(mixedNumber);
            //return hexNumber;
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

        public static void MakeMessageBoxContentClipboard(string content, string caption)
        {
            if (System.Windows.MessageBox.Show(string.Format("{0}\n\n(Copy to clipboard?)", content), caption, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                // WPF Error: OpenClipboard HRESULT:0x800401D0 (CLIPBRD_E_CANT_OPEN))
                // Clipboard.SetText(content); 

                Clipboard.SetDataObject(content);
            }
        }

        public static long? GetDirectorySize(string dirPath)
        {
            if (!Directory.Exists(dirPath))
                return null;

            long size = 0;

            DirectoryInfo directoryRoot = new(dirPath);
            DirectoryInfo[] directories = directoryRoot.GetDirectories();

            foreach (FileInfo file in directoryRoot.GetFiles())
                size += file.Length;

            foreach (DirectoryInfo directory in directories)
                size += GetDirectorySize(directory.FullName) ?? throw new($"Directory {directory.FullName} not exists");

            return size;
        }

        public static string GetFileSizeReadableString(long size)
        {
            const int scale = 1024;

            if (size < scale)
                return $"{size}B";
            if (size < Math.Pow(scale, 2))
                return $"{size / scale:f2}KB";
            if (size < Math.Pow(scale, 3))
                return $"{size / Math.Pow(scale, 2):f2}MB";
            if (size < Math.Pow(scale, 4))
                return $"{size / Math.Pow(scale, 3):f2}GB";

            return $"{size / Math.Pow(scale, 4):f2}TB";
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
}

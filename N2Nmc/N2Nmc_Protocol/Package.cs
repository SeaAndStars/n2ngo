using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace N2Nmc_Protocol
{
    public struct Package
    {
        public byte Header = 0; // enum Protocol.Header
        public byte[]? external_data = null;


        public Package() { }



        public static class MsgExternalData
        {
            public static class Encode
            {
                public static byte[] MsgChar(char data) { return new byte[1] { (byte)data }; }
                public static byte[] MsgByte(byte data) { return new byte[1] { data }; }
                public static byte[] MsgShort(Int16 data) { return new byte[2] { (byte)(data >> 8), (byte)data }; }
                public static byte[] MsgUShort(UInt16 data) { return new byte[2] { (byte)(data >> 8), (byte)data }; }
                public static byte[] MsgLong(Int32 data) { return new byte[4] { (byte)((data >> 24) & 0xFF), (byte)((data >> 16) & 0xFF), (byte)((data >> 8) & 0xFF), (byte)(data & 0xFF) }; }
                public static byte[] MsgULong(UInt32 data) { return new byte[4] { (byte)((data >> 24) & 0xFF), (byte)((data >> 16) & 0xFF), (byte)((data >> 8) & 0xFF), (byte)(data  & 0xFF) }; }
                public static byte[] MsgLongLong(Int64 data) { throw new NotImplementedException(); }
                public static byte[] MsgULongLong(UInt64 data) { throw new NotImplementedException(); }
                public static byte[] MsgFloat(float data) { throw new NotImplementedException(); }
                public static byte[] MsgDouble(double data) { throw new NotImplementedException(); }
                public static byte[] MsgString(string data) { var str_bytes = Protocol.StringEncoding.GetBytes(data); var bytes_list = new List<byte>(); bytes_list.AddRange(MsgUShort((UInt16)str_bytes.LongLength)); bytes_list.AddRange(str_bytes); return bytes_list.ToArray(); }
                public static byte[] MsgStringLong(string data) { var str_bytes = Protocol.StringEncoding.GetBytes(data); var bytes_list = new List<byte>(); bytes_list.AddRange(MsgULong((UInt32)str_bytes.LongLength)); bytes_list.AddRange(str_bytes); return bytes_list.ToArray(); }
                public static byte[] MsgStringLongLong(string data) { throw new NotImplementedException(); }
                public static byte[] MsgBytes(byte[] data) { var bytes = new List<byte>(); bytes.AddRange(MsgUShort((UInt16)data.LongLength)); bytes.AddRange(data); return bytes.ToArray(); }
                public static byte[] MsgBytesLong(byte[] data) { var bytes = new List<byte>(); bytes.AddRange(MsgULong((UInt32)data.LongLength)); bytes.AddRange(data); return bytes.ToArray(); }
            }
            public static class Decode
            {
                public static char MsgChar(byte[] externalData) { return (char)externalData[0]; }
                public static byte MsgByte(byte[] externalData) { return externalData[0]; }
                public static Int16 MsgShort(byte[] externalData) { return (Int16)(externalData[0] << 8 | externalData[1]); }
                public static UInt16 MsgUShort(byte[] externalData) { return (UInt16)(externalData[0] << 8 | externalData[1]); }
                public static Int32 MsgLong(byte[] externalData) { return (Int32)((externalData[0] << 24) | (externalData[1] << 16) | (externalData[2] << 8) | externalData[3]); }
                public static UInt32 MsgULong(byte[] externalData) { return (UInt32)((externalData[0] << 24) | (externalData[1] << 16) | (externalData[2] << 8) | externalData[3]); }
                public static Int64 MsgLongLong(byte[] externalData) { throw new NotImplementedException(); }
                public static UInt64 MsgULongLong(byte[] externalData) { throw new NotImplementedException(); }
                public static float MsgFloat(byte[] externalData) { throw new NotImplementedException(); }
                public static double MsgDouble(byte[] externalData) { throw new NotImplementedException(); }
                public static string MsgString(byte[] externalData) { return Protocol.StringEncoding.GetString(externalData.Skip(2).Take(MsgUShort(externalData)).ToArray()); }
                public static string MsgStringLong(byte[] externalData) { return Protocol.StringEncoding.GetString(externalData.Skip(4).Take(MsgLong(externalData)).ToArray()); }
                public static string MsgStringLongLong(byte[] externalData) { throw new NotImplementedException(); }
                public static byte[] MsgBytes(byte[] externalData) { return externalData.Skip(2).Take(MsgUShort(externalData)).ToArray(); }
                public static byte[] MsgBytesLong(byte[] externalData) { return externalData.Skip(2).Take((int)MsgULong(externalData)).ToArray(); }
            }
        }

        public static Package MakePackage(Protocol.BaseHeader header = Protocol.BaseHeader.undefined, byte[]? package_data = null)
        {
            Package package = new Package();

            package.Header = (byte)header;
            package.external_data = package_data != null ? package_data : new byte[0] { };

            return package;
        }


        public static byte[] BuildPackage(Package package)
        {
            List<byte> bytes_processing = new List<byte>();

            if (package.external_data == null)
                throw new NullReferenceException(nameof(package.external_data));

            bytes_processing.Add(package.Header);
            bytes_processing.AddRange(package.external_data);

            return bytes_processing.ToArray();
        }

        public static Package ResolvePackage(byte[] package_data)
        {
            Package package = new Package();

            package.Header = package_data[0];
            if (package_data.Length > 1)
            {
                package.external_data = new byte[package_data.Length - 1];
                Array.Copy(package_data, 1, package.external_data, 0, package_data.Length - 1);
            }
            else
                package.external_data = new byte[0] { };

            return package;
        }

        public class IO_Tool
        {
            public Exception? latestEx = null;

            public int ReadTimeOut = 15000, WriteTimeOut = 15000;
            public IO_Tool()
            {
                
            }

            public bool Send(TcpClient Client, Package package, bool timeOut = false)
            {
                latestEx = null;
                if (timeOut)
                    Client.SendTimeout = WriteTimeOut;

                try
                {
                    var stream = Client.GetStream();
                    stream.Flush();
                    stream.Write(Package.BuildPackage(package));
                    stream.Flush();
                }
                catch (Exception ex)
                {
                    latestEx = ex;
                    return false;
                }

                return true;
            }

            public Package? Receive(TcpClient Client, byte? defH = null, bool timeOut = false)
            {
                latestEx = null;
                if (timeOut)
                    Client.ReceiveTimeout = ReadTimeOut;

                Package? package = null;

                try
                {
                    var stream = Client.GetStream();
                    stream.Flush();

                    if (!stream.CanRead||!Client.Connected)
                        return package;

                    int h;
                    if (defH == null)
                    {
                        h = stream.ReadByte();
                        if (h == -1)
                            return package;
                    }
                    else
                        h = defH.Value;

                    int data_size = 0;

                    Protocol.BaseHeader header = (Protocol.BaseHeader)h;
                    if (header == Protocol.BaseHeader.msg_char || header == Protocol.BaseHeader.msg_byte)
                    {
                        data_size = 1;
                        goto resolved;
                    }
                    if (header == Protocol.BaseHeader.msg_short || header == Protocol.BaseHeader.msg_ushort)
                    {
                        data_size = 2;
                        goto resolved;
                    }
                    if (header == Protocol.BaseHeader.msg_long || header == Protocol.BaseHeader.msg_ulong)
                    {
                        data_size = 4;
                        goto resolved;
                    }
                    if (header == Protocol.BaseHeader.msg_longlong || header == Protocol.BaseHeader.msg_ulonglong)
                    {
                        data_size = 8;
                        goto resolved;
                    }
                    if (header == Protocol.BaseHeader.msg_float)
                    {
                        data_size = 4;
                        goto resolved;
                    }
                    if (header == Protocol.BaseHeader.msg_double)
                    {
                        data_size = 8;
                        goto resolved;
                    }
                    if (header == Protocol.BaseHeader.msg_string)
                    {
                        data_size = 2;
                        goto str_resolved;
                    }
                    if (header == Protocol.BaseHeader.msg_string_long)
                    {
                        data_size = 4;
                        goto str_resolved;
                    }
                    if (header == Protocol.BaseHeader.msg_string_long_long)
                    {
                        data_size = 8;
                        goto str_resolved;
                    }


                resolved:
                    {
                        byte[] bytes = new byte[data_size];
                        if (data_size > 0)
                        {
                            stream.Read(bytes, 0, bytes.Length);
                        }

                        package = Package.MakePackage(header, bytes);
                        return package;
                    }
                str_resolved:
                    {
                        List<byte> externalBytes = new List<byte>();
                        byte[] bytes = new byte[data_size];

                        stream.Read(bytes, 0, bytes.Length);
                        externalBytes.AddRange(bytes);

                        switch (data_size)
                        {
                            case 2:
                                {
                                    bytes = new byte[Package.MsgExternalData.Decode.MsgUShort(bytes)];
                                    if (bytes.Length > 0)
                                    {
                                        stream.Read(bytes, 0, bytes.Length);
                                    }
                                    externalBytes.AddRange(bytes);
                                    break;
                                }
                            case 4:
                                {
                                    bytes = new byte[Package.MsgExternalData.Decode.MsgULong(bytes)];
                                    if (bytes.Length > 0)
                                    {
                                        stream.Read(bytes, 0, bytes.Length);
                                    }
                                    externalBytes.AddRange(bytes);
                                    break;
                                }
                            case 8:
                                {
                                    bytes = new byte[Package.MsgExternalData.Decode.MsgULongLong(bytes)];
                                    if (bytes.Length > 0)
                                    {
                                        stream.Read(bytes, 0, bytes.Length);
                                    }
                                    externalBytes.AddRange(bytes);
                                    break;
                                }
                        }

                        package = Package.MakePackage(header, externalBytes.ToArray());
                        return package;
                    }
                }
                catch (Exception ex)
                {
                    latestEx = ex;
                    return package;
                }
            }
        }
    }
}

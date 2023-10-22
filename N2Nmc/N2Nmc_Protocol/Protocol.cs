using System.Text;

namespace N2Nmc_Protocol
{
    public class Protocol
    {
        /* BaseHeader as Package's first byte
         * 
         */

        public readonly static Encoding StringEncoding = Encoding.Unicode;

        // 8-bit hex
        public enum BaseHeader
        {
            undefined = 0,               // Invalid client sus, send InvalidClient && disconnect

            InvalidClient       = 0xfd,  // Client Invalid
            NotImplemented      = 0xfe,  // NotImplemented Function
            extended_package    = 0xff,  // Read two more bytes as Big-Endian and resolve with 'ExtendedHeader'

            peek    = 0x01,              // Send peek_ok (Ping)
            peek_ok = 0x02,

            msg_ok = 0x07,
            msg_char = 0x11, msg_byte,   // Read One   byte  in extended data (Big Endian)
            msg_short, msg_ushort,       // Read Two   bytes in extended data (Big Endian)
            msg_long, msg_ulong,         // Read Four  bytes in extended data (Big Endian)
            msg_longlong, msg_ulonglong, // Read Eight bytes in extended data (Big Endian)
            msg_float, msg_double,       // Read Four & Eight bytes in extended data (Big Endian)
            msg_string,                  // Read 'msg_ushort'   (Big Endian) in extended data as 'String Length', and go on read string with length
            msg_string_long,             // Read 'msg_ulong'    (Big Endian) in extended data as 'String Length', and go on read string with length
            msg_string_long_long,        // Read 'msg_ulonglong'(Big Endian) in extended data as 'String Length', and go on read string with length
            msg_bytes,                   // Read 'msg_ushort'   (Big Endian) in extended data as 'Bytes Length', and go on read bytes with length
            msg_bytes_long = 0x1F,       // Read 'msg_ulong'    (Big Endian) in extended data as 'Bytes Length', and go on read bytes with length

            /* Obsolete Protocols */
            _ver_check = 0x20,           // Client requests the latest version as class 'Version', server returns package: { msg_string, Version.ToString() }
            _room_pull_rooms = 0x40,     // Client requests to pull rooms, server sends package{ Rooms-Count(msg_ulong), $${ RoomCode(msg_string), RoomName(msg_string), IsRoomPasswordNeeded(msg_byte) } }
            _room_create,                // Client requests to create room, appends with package{ RoomCode(msg_string), RoomName(msg_string), IsRoomInvisible(msg_byte), IsRoomPasswordNeeded(msg_byte), RoomPassword(msg_string) }
            _room_close,                 // Client requests to close room, appends with RoomCode(msg_string)
            _room_is_code_exists,        // Client sends the room code using 'msg_string', server returns bool as byte using 'msg_byte'
            _room_get_name               // Client sends the room code using 'msg_string', server returns room name using 'msg_string'

            /* Obsolete Protocols */
        }

        // 16-bit hex (Big Endian)
        public enum ExtendedHeader
        {
            err_null = 0x0000,      // Invalid client, disconnect
        }
    }
}
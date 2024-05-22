using System.Text;

namespace N2Nmc_Protocol
{
    public class Protocol
    {

        public readonly static uint _PullRoomsRoomsTake = 50; // Obsoleted Data Define

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
            msg_refuse,
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
            _ver_check = 0x20,                  // Client requests the latest version as class 'Version', server returns package: { Version(msg_string) }
            _pull_online_total,                 // Client requests the number of server's total members, server returns package: { msg_string, ((Int64)MembersCount).ToString() }
            _user_key_get,                      // Client requests userkey, server returns userkey as 'msg_string'

            _rooms_pull_rooms_pages = 0x40,     // Client requests the rooms pages, server returns pages as number using 'msg_ulong'
            _rooms_pull_rooms,                  // Client requests to pull rooms and sends page index using 'msg_ulong', server returns package{ Rooms-Count(msg_ulong), $${ RoomCode(msg_string), RoomName(msg_string), IsRoomPasswordNeeded(msg_byte) } }
            _rooms_create,                      // Client requests to create room, appends with package{ RoomCode(msg_string), RoomName(msg_string), IsRoomInvisible(msg_byte), IsRoomPasswordNeeded(msg_byte), RoomPassword(msg_string), MainColor(msg_ulong), MinorColor(msg_ulong) }, server returns package{CB.AdminKey(msg_string_long), msg_ok}.
            _rooms_is_code_exists,              // Client sends the room code using 'msg_string', server returns bool as byte using 'msg_byte'
            _rooms_get_name,                    // Client sends the room code using 'msg_string', server returns room name using 'msg_string'

            _room_client_join_fail_0,           // Fail: Client can only be in one room at a time
            _room_client_join_fail_1,           // Fail: Room not exists
            _room_client_join_fail_2,           // Fail: Room password verification fail
            _room_client_join_fail_00,          // Fail: append package { code(msg_long) }
            _room_client_join,                  // Client sends package{ Room Code(msg_string), Room Password(msg_string), Nick Name(msg_string) }, server returns package { UserId(msg_string) } or '_room_client_join_fail_0 | _room_client_join_fail_1 | _room_client_join_fail_2 | _room_client_join_fail_00'
            /* Room Commands that needs user verification */
            _room_client_fail_0,                // Fail: user verification not valid
            _room_client_leave,                 // Client sends package{ UserId(msg_string), UserKey(msg_string) }, server returns 'msg_ok' or '_room_client_fail_0'
            _room_client_pullmembers,           // Client sends package{ n2nNodeUserIp(msg_string) }
            _room_client_check_lifetime,        // 
            /* Room Commands for Admins */
            _room_client_admin_fail_0,          // Fail: Permission Denied (Not Admin)
            _room_client_admin_reset_room,      //
            _room_client_admin_user_update,     // Client sends package{ UserId(msg_string), IsAdmin(msg_byte) }, server returns 'msg_ok' or '_room_client_fail_0 | _room_client_admin_fail_0'
            _room_client_admin_kick,            //
            _room_client_admin_update_lifetime, //

            /* Obsolete Protocols */
        }

        // 16-bit hex (Big Endian)
        public enum ExtendedHeader
        {
            err_null = 0x0000,      // Invalid client, disconnect
        }
    }
}
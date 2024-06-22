using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace N2Nmc_Protocol.Objects
{
    public struct RoomColor
    {
        public UInt32 data { get; private set; } = 0x00000000;    // 0xRRGGBBAA

        public byte R
        {
            get => (byte)(data >> 24);
            set { data = (uint)((data & ~0xFF000000) | (uint)((value & 0xFF) << 24)); }
        }

        public byte G
        {
            get => (byte)(data >> 16);
            set { data = (uint)((data & ~0x00FF0000) | (uint)((value & 0xFF) << 16)); }
        }

        public byte B
        {
            get => (byte)(data >> 8);
            set { data = (uint)((data & ~0x0000FF00) | (uint)((value & 0xFF) << 8)); }
        }

        public byte A
        {
            get => (byte)data;
            set { data = (uint)((data & ~0x000000FF) | (uint)(value & 0xFF)); }
        }

        public RoomColor(byte r = 0xFF, byte g = 0xFF, byte b = 0xFF, byte a = 0xFF)
        {
            data = ((UInt32)r << 24) | ((UInt32)g << 16) | ((UInt32)b << 8) | (UInt32)a;
        }

        public RoomColor(UInt32 data) => this.data = data;
    }

    public class Room
    {
        public static string RandomKeyString()
        {
            return new Random((int)DateTime.UtcNow.Ticks).NextInt64(Int64.MinValue, Int64.MaxValue).ToString("x");  // Get a hex code
        }

        public enum RoomAccessMode
        {
            BlockList,  // Default, prevents members with 'MemberBehaviour.PreventJoin' from joining
            AllowList,  // Accepts only members with 'MemberBehaviour.AllowJoin'
            None        // Disables 'MemberBehaviour' checks for member joining
        }

        public struct ControlBlock
        {
            public bool IsAlive;
            public TimeSpan LiveTime;
            public DateTime lastReqTime;
            public string AdminKey;

            public Thread thRoomHandler;
        }

        public class Member
        {
            public string ID = "none"; // Logical ID relative to room
            public string NickName = "Unknown";
            public string IpAddress = "Unknown";
            public string UserKey = "none";
        }
        public class RuledMember : Member
        {
            public enum MemberBehaviour
            {
                None,
                PreventJoin,
                AllowJoin   // Use for AllowList access mode (when Normal access mode, works as 'None')
            }
            public MemberBehaviour Behaviour = MemberBehaviour.None;
        }

        public string? RoomCode;
        public string? RoomName;
        public bool IsRoomInvisible = false;
        public bool IsRoomPasswordNeeded = false;
        public string RoomPassword = "null";
        public RoomColor colorMain = new RoomColor();
        public RoomColor colorMinor = new RoomColor();

        public ControlBlock CB = new ControlBlock();
        public RoomAccessMode AccessMode = RoomAccessMode.BlockList;
        public List<Member> Members = new List<Member>();
        public List<RuledMember> RuledMembers = new List<RuledMember>();

        public Room()
        {
            CB.IsAlive = true;
            CB.LiveTime = new TimeSpan(0, 15, 0);
            CB.lastReqTime = DateTime.Now;
            CB.AdminKey = RandomKeyString();

            CB.thRoomHandler = new(() =>
            {
                while (CB.IsAlive)
                {
                    Thread.Sleep(CB.LiveTime);
                    CB.LiveTime = TimeSpan.Zero;
                    Thread.Sleep(1000 * 20);    // Wait for 20s

                    if (CB.LiveTime.TotalSeconds == 0)
                    {
                        CB.IsAlive = false; // Room is destroyable
                        break;      // User abandons the renewal
                    }
                    else
                        continue;
                }
            });
            CB.thRoomHandler.Start();
        }

        public Member? GetMember(string userKey)
        {
            foreach (Member member in Members)
            {
                if (member.UserKey == userKey)
                    return member;
            }

            return null;
        }
        public RuledMember? GetRuledMember(string userKey)
        {
            foreach (RuledMember ruledMember in RuledMembers)
            {
                if (ruledMember.UserKey == userKey)
                    return ruledMember;
            }

            return null;
        }

        public bool IsMemberAdmin(Member member) => member.UserKey == CB.AdminKey;


        public Tuple<Member, RuledMember> CreateMember(string userKey, string nickName, string iP)
        {
            Member member = new() { ID = RandomKeyString(), NickName = nickName, IpAddress = iP, UserKey = userKey };
            RuledMember ruledMember = new RuledMember { Behaviour = RuledMember.MemberBehaviour.None, ID = member.ID, IpAddress = member.IpAddress, NickName = member.NickName, UserKey = member.UserKey };

            Member? memberInRoom = GetMember(userKey);
            if (memberInRoom != null)
            {
                memberInRoom.ID = member.ID;
                memberInRoom.NickName = member.NickName;
                memberInRoom.IpAddress = member.IpAddress;
            }

            RuledMember? ruledMemberInRoom = GetRuledMember(userKey);
            if (ruledMemberInRoom == null)
            {
                RuledMembers.Add(ruledMember);
            }
            else
            {
                ruledMemberInRoom.ID = ruledMember.ID;
                ruledMemberInRoom.NickName = ruledMember.NickName;
                ruledMemberInRoom.IpAddress = ruledMember.IpAddress;

                // follow last recorded policy
                ruledMember.Behaviour = ruledMemberInRoom.Behaviour;
            }
            
            return new Tuple<Member, RuledMember>(member, ruledMember);
        }

        /*
         * Must call 'CreateMember' before
         *   returns 0 if member joined
         *           -1 for member already exists
         *           -2 for member is prevented from joining due to room blocklist access mode
         *           -3 for member is prevented from joining due to room allowlist access mode
        */
        public int MemberJoin(Member member)
        {
            Member? memberInRoom = GetMember(member.UserKey);
            RuledMember? ruledMemberInRoom = GetRuledMember(member.UserKey);

            if (memberInRoom != null)
                return -1;

            if (ruledMemberInRoom == null)
                throw new Exception("N2Nmc_Protocol.Object.Room.MemberJoin cannot get 'RuledMember' object, did you forget to call 'CreateMember'?");

            if (AccessMode != RoomAccessMode.None)
            {
                if (ruledMemberInRoom.Behaviour == RuledMember.MemberBehaviour.PreventJoin)
                    return -2;

                if (ruledMemberInRoom.Behaviour != RuledMember.MemberBehaviour.AllowJoin && AccessMode == RoomAccessMode.AllowList)
                    return -3;
            }

            Members.Add(member);
            return 0;
        }

        public void MemberLeave(Member member)
        {
            if (GetMember(member.UserKey) != null)
            { Members.Remove(member); }
        }
    }
}

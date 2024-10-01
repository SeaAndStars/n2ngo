using N2NGOCore.Objects;
using System.Text.Json.Serialization;

namespace N2NGOCore.Models
{
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
            [JsonInclude] public bool IsAlive;
            [JsonInclude] public TimeSpan ActivatedLifetime;
            [JsonInclude] public DateTime LastActivatedTime;
            [JsonInclude] public List<string> AdminKey;

            [JsonIgnore]
            public Thread thRoomHandler;
        }

        public class Member
        {
            [JsonInclude] public string ID = "none"; // Logical ID relative to room
            [JsonInclude] public string Nickname = "Unknown";
            [JsonInclude] public string IpAddress = "Unknown";
            [JsonInclude] public string UserKey = "none";

            // Client Properties (null for server)
            [JsonInclude] public bool? IsAdmin = null;

            // Server Properties (null for client)
            [JsonInclude] public Server.ClientControlBlock? ClientControlBlock = null;
        }
        public class RuledMember : Member
        {
            public enum MemberBehaviour : byte
            {
                None,
                PreventJoin,
                AllowJoin   // Use for AllowList access mode (when Normal access mode, works as 'None')
            }
            public MemberBehaviour Behaviour = MemberBehaviour.None;
        }

        [JsonInclude] public string? RoomCode;
        [JsonInclude] public string? RoomName;
        [JsonInclude] public bool? IsRoomInvisible = false;
        [JsonInclude] public bool? IsRoomPasswordNeeded = false;
        [JsonInclude] public string? RoomPassword = "null";
        [JsonInclude] public RoomColor? MainColor = new RoomColor();
        [JsonInclude] public RoomColor? MinorColor = new RoomColor();

        [JsonInclude] public ControlBlock CB = new ControlBlock() { IsAlive = true, ActivatedLifetime = new TimeSpan(0, 10, 0), LastActivatedTime = DateTime.Now, AdminKey = new() { RandomKeyString() } };
        [JsonInclude] public RoomAccessMode AccessMode = RoomAccessMode.BlockList;
        [JsonInclude] public List<Member> Members = new List<Member>();
        [JsonInclude] public List<RuledMember> RuledMembers = new List<RuledMember>();

        public Room()
        {
        }

        public void InitHandler()
        {
            CB.thRoomHandler = new(() =>
            {
                while (CB.IsAlive)
                {
                    const int milliSecondsDelay = 5000;
                    Thread.Sleep(milliSecondsDelay);

                    CB.ActivatedLifetime -= TimeSpan.FromMilliseconds(milliSecondsDelay);

                    if (CB.ActivatedLifetime.TotalSeconds <= 0)
                    {
                        /*
                         * User abandons the renewal(activation)
                         * Room is destroyable for RGC_Handler
                        */
                        CB.IsAlive = false;
                        break;
                    }

                    for (int i = 0; i < Members.Count; i++)
                    {
                        var ccb = Members[i].ClientControlBlock;

                        if (ccb is null)
                        {
                            Members.Remove(Members[i]);
                            continue;
                            //throw new("Client Control Block null for Members");
                        }

                        if (!ccb.ClientHandlerThread.IsAlive || !ccb.IsClientAlive)
                        {
                            Members.Remove(Members[i]);
                        }
                    }
                }
            });
            CB.thRoomHandler.Start();
        }

        public Member? GetMember(string userKey)
        {
            lock (this)
            foreach (Member member in Members)
            {
                if (member.UserKey == userKey)
                    return member;
            }

            return null;
        }

        public RuledMember? GetRuledMember(string userKey)
        {
            lock(this)
            foreach (RuledMember ruledMember in RuledMembers)
            {
                if (ruledMember.UserKey == userKey)
                    return ruledMember;
            }

            return null;
        }

        public bool IsMemberAdmin(Member member) => CB.AdminKey.Contains(member.UserKey);

        public void Activate(TimeSpan time)
        {
            CB.ActivatedLifetime += time;
            CB.LastActivatedTime = DateTime.Now;
        }

        public string GenerateMemberID(int length)
        {
            while (true)
            {
                var id = RandomKeyString().Substring(0, length);

                if (!RuledMembers.Any(m => m.ID == id))
                    return id;
            }
        }

        public Tuple<Member, RuledMember> CreateMember(string userKey, string nickName, string iP, Server.ClientControlBlock? clientControlBlock = null)
        {
            Member member = new() { ID = GenerateMemberID(5), Nickname = nickName, IpAddress = iP, UserKey = userKey, ClientControlBlock = clientControlBlock };
            RuledMember ruledMember = new RuledMember { Behaviour = RuledMember.MemberBehaviour.None, ID = member.ID, IpAddress = member.IpAddress, Nickname = member.Nickname, UserKey = member.UserKey, ClientControlBlock = member.ClientControlBlock };

            Member? memberInRoom = GetMember(userKey);
            if (memberInRoom != null)
            {
                memberInRoom.ID = member.ID;
                memberInRoom.Nickname = member.Nickname;
                memberInRoom.IpAddress = member.IpAddress;
                memberInRoom.ClientControlBlock = member.ClientControlBlock;
            }

            RuledMember? ruledMemberInRoom = GetRuledMember(userKey);
            if (ruledMemberInRoom == null)
            {
                RuledMembers.Add(ruledMember);
            }

            ruledMemberInRoom = GetRuledMember(userKey);
            if (ruledMemberInRoom == null)
            {
                throw new("Cannot create ruled member");
            }
            ruledMemberInRoom.ID = ruledMember.ID;
            ruledMemberInRoom.Nickname = ruledMember.Nickname;
            ruledMemberInRoom.IpAddress = ruledMember.IpAddress;
            ruledMemberInRoom.ClientControlBlock = ruledMember.ClientControlBlock;

            // follow last recorded policy
            ruledMember.Behaviour = ruledMemberInRoom.Behaviour;

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
                throw new Exception("N2NGO_Core.Object.Room.MemberJoin cannot get 'RuledMember' object, did you forget to call 'CreateMember'?");

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
                Members.Remove(member);
        }
    }
}

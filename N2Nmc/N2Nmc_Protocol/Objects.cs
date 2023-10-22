using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace N2Nmc_Protocol.Objects
{
    public class Room
    {
        public string? RoomCode;
        public string? RoomName;
        public bool IsRoomInvisible = false;
        public bool IsRoomPasswordNeeded = false;
        public string RoomPassword = "null";
        public ControlBlock CB = new ControlBlock();

        public Room()
        {
            CB.lastReqTime = DateTime.Now;
        }

        public struct ControlBlock
        {
            public DateTime lastReqTime;
        }
    }

}

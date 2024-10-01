using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace N2NGOCore.Objects
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
}

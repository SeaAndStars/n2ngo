using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace N2Nmc.UtilsClass
{
    internal class EdgeInvoker
    {
        [DllImport("Data/BinRef/Windows/n2n/edge.exe", CallingConvention = CallingConvention.Cdecl)]
        public static extern int edge_main(int argc, string[] argv);


        private Mutex EdgeInvokeLock = new Mutex();
        public List<string> args { get; private set; } = new List<string>();


        public EdgeInvoker()
        {
            NewArgs();
        }


        public void NewArgs()
        {
            args = new List<string>();
        }
        public void PushArg(string arg)
        {
            args.Add(arg);
        }

        public void Call()
        {
            edge_main();
        }
    }
}

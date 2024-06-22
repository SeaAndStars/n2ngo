using HandyControl.Expression.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace N2Nmc.UtilsClass
{
    internal class EdgeInvoker
    {
        [DllImport("Data/BinRef/Windows/n2n/edge.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int edge_main(int argc, IntPtr argv);


        private Task? taskInvoke=null;
        private Mutex EdgeInvokeLock = new();
        public List<string> args { get; private set; } = new();


        public EdgeInvoker()
        {
            NewArgs();
        }

        ~EdgeInvoker()
        {

        }


        public void NewArgs()
        {
            lock (this)
                args = new()
                {
                    "edge" // edge.exe arg0
                };
        }

        public void PushArg(string arg)
        {
            lock (this)
                args.Add(arg);
        }
        public void PushArg(string[] arg)
        {
            lock (this)
                args.AddRange(arg);
        }
        public void PushArgs(string arg)
        {
            lock (this)
                args.AddRange(arg.Split(' '));
        }

        private CancellationTokenSource cts = new();
        public void CancelCall()
        {
            cts.Cancel();
        }
        private Task<int>? CallTask(CancellationToken ct)
        {
            Task<int>? r = null;

            try
            {
                r = Task<int>.FromResult(edge_main(args.Count, new IntPtr()));
            }
            finally
            {

            }

            return r;
        }
        public void Call()
        {
            CancelCall();
            taskInvoke = CallTask(cts.Token);
        }
    }
}

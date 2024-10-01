using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace N2NGOCore.Models.Server
{
    public class ClientControlBlock
    {
        public DateTime AccessTime { get; private set; }

        public TcpClient ConnectionClient { get; private set; }
        public Thread ClientHandlerThread { get; private set; }

        public Dictionary<string, object?> ClientConnectionData = new();

        public bool IsClientAlive
        {
            get
            {
                if (ConnectionClient == null || ConnectionClient.Client == null || ConnectionClient.Client.Connected == false || ConnectionClient.Client.RemoteEndPoint == null)
                    return false;

                bool blockingState = ConnectionClient.Client.Blocking;
                try
                {
                    ConnectionClient.Client.Blocking = false;
                    ConnectionClient.Client.Send(new byte[1] { (byte)Protocol.BaseHeader.Keeplive }, 1, 0);
                    return true;
                }
                catch (SocketException e)
                {
                    // 0035 == WSAEWOULDBLOCK
                    if (e.NativeErrorCode.Equals(10035))
                        return true;
                    else
                        return false;
                }
                finally
                {
                    ConnectionClient.Client.Blocking = blockingState;
                }
            }
        }

        public ClientControlBlock(TcpClient client, Action<object?> handlerFunc, bool startHandler = false)
        {
            AccessTime = DateTime.Now;

            this.ConnectionClient = client;
            ClientHandlerThread = new(new ParameterizedThreadStart(handlerFunc));

            if (startHandler)
                ClientHandlerThread.Start(this);
        }

        ~ClientControlBlock()
        {
            try
            {
                ClientConnectionData.Clear();
                ConnectionClient.Close();
                ConnectionClient.Dispose();
            }
            catch {}
        }

        public void Start()
        {
            ClientHandlerThread.Start(this);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace SLAMBotClasses
{
    public class TCPSlamServer : TCPSlamBase
    {
        #region Members

        public event EventHandler<ServerStatusArgs> OnConnectionStatusChanged;
        public enum ServerStatus { Listening, Connected, Disconnected };
        private Thread listenThread;
        private TcpListener listener;
        private ServerStatus _Status;

        #endregion

        #region Properties

        public ServerStatus Status
        {
            get { return _Status; }
            private set
            {
                _Status = value;
                if (OnConnectionStatusChanged != null)
                    OnConnectionStatusChanged(this, new ServerStatusArgs(_Status));
            }
        }

        #endregion

        #region Helper Classes

        public class ServerStatusArgs : EventArgs
        {
            public ServerStatus Status;
            public ServerStatusArgs(ServerStatus Status)
            {
                this.Status = Status;
            }
        }

        #endregion

        #region Constructor

        public TCPSlamServer()
        {
            Status = ServerStatus.Disconnected;
            OnInternalConnectionClosed += new EventHandler(TCPSlamServer_OnInternalConnectionClosed);
        }

        #endregion

        #region Public Methods

        public void StartServer()
        {
            //check that the ip and port has been set
            if (_IPAddress != null && _Port != 0)
            {
                listener = new TcpListener(_IPAddress, _Port);
                listener.Start();
                Console.WriteLine("Starting server, " + _IPAddress.ToString() + ":" + _Port.ToString());
                listenThread = new Thread(ListenForClient);
                listenThread.Start();
            }
            else
            {
                Exception ex = new Exception("(TCPSlamServer:StartServer) IP and Port has not been set");
                throw ex;
            }
        }

        public void StartServer(IPAddress IPAddress, int Port)
        {
            _IPAddress = IPAddress;
            _Port = Port;
            StartServer();
        }

        public override void CloseConnection()
        {
            base.CloseConnection();
            Status = ServerStatus.Disconnected;              
            listener.Stop();
            base.CloseConnection();
            if (listenThread.IsAlive)
                listenThread.Abort();                      
        }

        #endregion

        #region Private Methods

        private void ListenForClient()
        {
            tcpObject = new TcpClient();
            Status = ServerStatus.Listening;
            Console.WriteLine("Waiting for incomming connection...");
            tcpObject = listener.AcceptTcpClient();
            Console.WriteLine("Client Connected");
            listener.Stop();
            tcpStream = tcpObject.GetStream();
            Status = ServerStatus.Connected;
            comThread = new Thread(ReceiveData);
            comThread.Priority = ThreadPriority.Highest;
            comThread.Start();
        }

        #endregion

        #region Events

        void TCPSlamServer_OnInternalConnectionClosed(object sender, EventArgs e)
        {
            Status = ServerStatus.Disconnected;
        }

        #endregion
    }
}

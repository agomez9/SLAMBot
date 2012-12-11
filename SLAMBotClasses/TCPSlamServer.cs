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

        /// <summary>
        /// Gets the current status of the connection
        /// </summary>
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
            OnInternalConnectionOpened += new EventHandler(TCPSlamServer_OnInternalConnectionOpened);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the server, make sure you have the IP and port set if using this method.
        /// </summary>
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

        /// <summary>
        /// Starts the server
        /// </summary>
        /// <param name="IPAddress">IP of the server, 127.0.0.1 is good for testing on local machine</param>
        /// <param name="Port">Port of the server</param>
        public void StartServer(IPAddress IPAddress, int Port)
        {
            _IPAddress = IPAddress;
            _Port = Port;
            StartServer();
        }

        /// <summary>
        /// Closes the connection
        /// </summary>
        public override void CloseConnection()
        {
            base.CloseConnection();             
            listener.Stop();
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

        void TCPSlamServer_OnInternalConnectionOpened(object sender, EventArgs e)
        {
            Status = ServerStatus.Connected;
        }

        #endregion
    }
}

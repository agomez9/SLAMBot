﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace SLAMBotClasses
{
    public class TCPSlamClient : TCPSlamBase
    {
        #region Members

        public event EventHandler<ClientStatusArgs> OnConnectionStatusChanged;
        public enum ClientStatus { Connecting, Connected, Disconnected };
        private Thread connectThread;
        private ClientStatus _Status;
       
        #endregion

        #region Properties

        /// <summary>
        /// Gets the current status of the connection.
        /// </summary>
        public ClientStatus Status
        {
            get { return _Status; }
            private set
            {
                _Status = value;
                if (OnConnectionStatusChanged != null)
                    OnConnectionStatusChanged(this, new ClientStatusArgs(_Status));
            }
        }

        #endregion

        #region Helper Classes

        public class ClientStatusArgs : EventArgs
        {
            public ClientStatus Status;
            public ClientStatusArgs(ClientStatus Status)
            {
                this.Status = Status;
            }
        }

        #endregion

        #region Constructor

        public TCPSlamClient()
        {
            Status = ClientStatus.Disconnected;
            OnInternalConnectionClosed += new EventHandler(TCPSlamClient_OnInternalConnectionClosed);
            OnInternalConnectionOpened += new EventHandler(TCPSlamClient_OnInternalConnectionOpened);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Connects to the server, make sure you have set the IP and Port if using this method
        /// </summary>
        public void Connect()
        {
            //check that the ip and port has been set
            if (_IPAddress != null && _Port != 0)
            {
                connectThread = new Thread(StartConnect);
                connectThread.Start();
            }
            else
            {
                Exception ex = new Exception("(TCPSlamClient:Connect) IP and Port has not been set");
                throw ex;
            }
        }

        /// <summary>
        /// Connects to the server
        /// </summary>
        /// <param name="IPAddress">IP of the server</param>
        /// <param name="Port">Port of the server</param>
        public void Connect(IPAddress IPAddress, int Port)
        {
            _IPAddress = IPAddress;
            _Port = Port;
            Connect();
        }

        /// <summary>
        /// Disconnects from the server
        /// </summary>
        public override void CloseConnection()
        {
            base.CloseConnection();
            if (connectThread.IsAlive)
                connectThread.Abort();            
        }

        #endregion

        #region Private Methods

        private void StartConnect()
        {
            Status = ClientStatus.Connecting;
            try
            {
                tcpObject = new TcpClient();
                tcpObject.Connect(_IPAddress, _Port);
            }
            catch
            {
                CloseConnection();
            }

            if (tcpObject.Connected)
            {
                tcpObject.NoDelay = true;
                tcpStream = tcpObject.GetStream();
                comThread = new Thread(ReceiveData);
                comThread.Priority = ThreadPriority.Highest;
                comThread.Start();
            }
            else
            {
                CloseConnection();
            }
        }

        #endregion

        #region Events

        void TCPSlamClient_OnInternalConnectionClosed(object sender, EventArgs e)
        {
            Status = ClientStatus.Disconnected;
        }

        void TCPSlamClient_OnInternalConnectionOpened(object sender, EventArgs e)
        {
            Status = ClientStatus.Connected;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace SLAMBotClasses
{
    public abstract class TCPSlamBase
    {
        #region Members

        public enum MessageType { Ping, CameraOneFrame, LeftMotor, RightMotor, Lights, ArduinoConnection, ArduinoStatus };        
        public event EventHandler<MessageArgs> OnDataReceived;
        protected Thread comThread;
        protected TcpClient tcpObject;
        protected NetworkStream tcpStream;
        protected IPAddress _IPAddress;
        protected int _Port;
        protected event EventHandler OnInternalConnectionClosed;
        protected event EventHandler OnInternalConnectionOpened;
        private Queue<AsyncSendMsg> sendQueue;
        private Mutex muSend;
        private DateTime LastMbpsUpCal;
        private DateTime LastMbpsDownCal;
        private const int _bufferSize = 50000;        
        private int bytesSent;
        private int bytesReceived;
        private double _MbpsUp;
        private double _MbpsDown;
        private int _SendQueueSize;
        private Thread sendThread;

        #endregion

        #region Properties

        public int SendQueueSize
        {
            get
            {
                return _SendQueueSize;
            }
        }

        public int BufferSize
        {
            get
            {
                return _bufferSize;
            }
        }

        public IPAddress IPAddress
        {
            get { return _IPAddress; }
            set { _IPAddress = value; }
        }

        public int Port
        {
            get { return _Port; }
            set { _Port = value; }
        }

        public double MbpsUp
        {
            get
            {
                return _MbpsUp;
            }
        }

        public double MbpsDown
        {
            get
            {
                return _MbpsDown;
            }
        }        

        #endregion

        #region Helper Classes       

        public class MessageArgs : EventArgs
        {
            public byte[] Message;
            public MessageType MessageType;
            public MessageArgs(byte[] Message, MessageType MessageType)
            {
                this.Message = Message;
                this.MessageType = MessageType;
            }
        }

        private class AsyncSendMsg
        {
            public MessageType MessageType;
            public byte[] Message;
            public bool Priority;
            public AsyncSendMsg(MessageType MessageType, byte[] Message, bool Priority)
            {
                this.MessageType = MessageType;
                this.Message = Message;
                this.Priority = Priority;
            }
        }

        #endregion

        #region Constructor

        public TCPSlamBase()
        {                      
            sendQueue = new Queue<AsyncSendMsg>();            
        }

        #endregion

        #region Public Methods

        public void SendData(MessageType MessageType, byte[] Message)
        {            
            if (tcpObject.Connected)
            {
                muSend.WaitOne();                               
                sendQueue.Enqueue(new AsyncSendMsg(MessageType, Message, false));
                _SendQueueSize += Message.Length;
                muSend.ReleaseMutex();
            }
        }       

        #endregion

        #region Protected Methods

        public virtual void CloseConnection()
        {
            if (OnInternalConnectionClosed != null)
                OnInternalConnectionClosed(this, EventArgs.Empty);            

            if (tcpStream != null)
                tcpStream.Close(0);
            if (tcpObject != null)
                tcpObject.Close();
            if (comThread != null)
                comThread.Join();
            if (sendThread != null)
                sendThread.Join();          
        }

        protected void ReceiveData()
        {
            muSend = new Mutex();
            sendQueue.Clear();
            LastMbpsDownCal = DateTime.Now;
            _MbpsDown = bytesReceived = 0;
            sendThread = new Thread(AsyncSend);
            sendThread.Priority = ThreadPriority.Highest;
            sendThread.Start();
            tcpObject.ReceiveBufferSize = _bufferSize;
            tcpObject.SendBufferSize = _bufferSize;            
            int chunkSize = tcpObject.ReceiveBufferSize;
            int msgSize = 0;
            int currentMsgSize = 0;
            byte[] msgSizeByteAray = new byte[4];
            byte[] msgType = new byte[1];
            byte[] fullMessage;
            if (OnInternalConnectionOpened != null)
                OnInternalConnectionOpened(this, EventArgs.Empty);

            while (tcpObject.Connected)
            {
                try
                {
                    if (tcpObject.Available >= 4)
                    {
                        //The first 4 bytes of the message will always contain the length of the message, not including
                        //the first 5 bytes. This is how you know when to stop reading.
                        tcpStream.Read(msgSizeByteAray, 0, 4);
                        //convert msgSizeByteAray to an int
                        msgSize = BitConverter.ToInt32(msgSizeByteAray, 0);
                        //the next byte contains the type of message
                        tcpStream.Read(msgType, 0, 1);
                        //fullMessage will contain the entire message but it has to be built message by message.                    
                        fullMessage = new byte[msgSize];
                        currentMsgSize = 0;
                        //keep reading until we get all the data
                        while (currentMsgSize < msgSize)
                        {
                            //when you send something over TCP it will some times get split up
                            //this is why you only read in chuncks
                            if (msgSize - currentMsgSize < chunkSize)
                                currentMsgSize += tcpStream.Read(fullMessage, currentMsgSize, msgSize - currentMsgSize);
                            else
                                currentMsgSize += tcpStream.Read(fullMessage, currentMsgSize, chunkSize);
                        }

                        //thow the data received event
                        if (fullMessage.Length > 0)
                        {
                            bytesReceived += (fullMessage.Length + 5);
                            if (OnDataReceived != null)
                                OnDataReceived(this, new MessageArgs(fullMessage, (MessageType)msgType[0]));
                        }
                    }
                    else
                    {
                        //let other threads catch up if this one is not working
                        Thread.Sleep(1);
                    }

                    //Calculate Mbps down every 1 second and average it
                    if ((DateTime.Now - LastMbpsDownCal).TotalSeconds >= 1)
                    {
                        _MbpsDown = (_MbpsDown + ((double)(bytesReceived / 1024d / 1024d) / (DateTime.Now - LastMbpsDownCal).TotalSeconds)) / 2;
                        bytesReceived = 0;
                        LastMbpsDownCal = DateTime.Now;
                        SendData(MessageType.Ping, new byte[0]);
                    }
                }
                catch
                {
                    CloseConnection();
                }
            }            
        }

        #endregion

        #region Private Methods        

        private void AsyncSend()
        {
            LastMbpsUpCal = DateTime.Now;
            _MbpsUp = bytesSent = 0;

            while (tcpObject.Connected)
            {
                try
                {
                    if (sendQueue.Count > 0)
                    {
                        muSend.WaitOne();
                        AsyncSendMsg asm = sendQueue.Dequeue();
                        byte[] lengthArray = BitConverter.GetBytes(asm.Message.Length);
                        tcpStream.Write(lengthArray, 0, 4);
                        tcpStream.WriteByte((byte)asm.MessageType);
                        tcpStream.Write(asm.Message, 0, asm.Message.Length);
                        bytesSent += (asm.Message.Length + 5);                        
                        _SendQueueSize -= asm.Message.Length;
                        muSend.ReleaseMutex();
                    }
                    else
                    {
                        //let other threads catch up if this one is not working
                        Thread.Sleep(1);
                    }

                    //Calculate Mbps up every 1 second and average it
                    if ((DateTime.Now - LastMbpsUpCal).TotalSeconds >= 1)
                    {
                        _MbpsUp = (_MbpsUp + ((double)(bytesSent / 1024d / 1024d) / (DateTime.Now - LastMbpsUpCal).TotalSeconds)) / 2;
                        bytesSent = 0;
                        LastMbpsUpCal = DateTime.Now;
                    }
                }
                catch
                {
                    CloseConnection();
                }
            }
        }

        #endregion
    }
}

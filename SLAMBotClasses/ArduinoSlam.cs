using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace SLAMBotClasses
{
    public class ArduinoSlam
    {
        #region Private Members

        private SerialPort sp;
        private bool gotHandShakeBack;
        private enum ArduinoCommands { Handshake, TestLight};
        public enum ArduinoStatus { Connected, NotConnected, Connecting };
        private ArduinoStatus _Status;
        Thread cThread;

        #endregion

        #region Public Members

        public class StatusArgs : EventArgs
        {
            private ArduinoStatus _Status;
            public ArduinoStatus Status
            {
                get { return _Status; }
                set { _Status = value; }
            }

            public StatusArgs(ArduinoStatus Status)
            {
                _Status = Status;
            }
        }
        public event EventHandler<StatusArgs> OnStatusChanged;

        #endregion

        #region Class Properties

        public ArduinoStatus Status
        {
            get { return _Status; }
            private set 
            { 
                _Status = value;
                if (OnStatusChanged != null)
                    OnStatusChanged(this, new StatusArgs(_Status));
            }
        }

        #endregion

        #region Constructor

        public ArduinoSlam()
        {
            sp = new SerialPort();
            sp.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
            sp.BaudRate = 9600;
            sp.DataBits = 8;
            sp.Parity = Parity.None;
            sp.StopBits = StopBits.One;
            Status = ArduinoStatus.NotConnected;
        }

        #endregion

        #region Public Methods

        public void Connect()
        {
            cThread = new Thread(ThreadConnect);
            cThread.Start();
        }

        public void CloseConnection()
        {
            if (cThread.IsAlive)
                cThread.Abort();

            if (sp.IsOpen)
                sp.Close();

            Status = ArduinoStatus.NotConnected;
        }

        public void TurnTestLightOff()
        {
            SendData(ArduinoCommands.TestLight, 0);
        }

        public void TurnTestLightOn()
        {
            SendData(ArduinoCommands.TestLight, 1);
        }

        #endregion

        #region Private Methods

        private void ThreadConnect()
        {
            Status = ArduinoStatus.Connecting;
            if (sp.IsOpen)
                sp.Close();

            string[] ports = SerialPort.GetPortNames();

            if (ports.Length > 0)
            {
                foreach (string comPort in ports)
                {
                    Console.Write("Connecting to : " + comPort + "... ");
                    sp.PortName = comPort;
                    sp.Open();
                    if (sp.IsOpen)
                    {
                        gotHandShakeBack = false;
                        Console.WriteLine("Connected");
                        Console.Write("Sending handshake... ");
                        SendData(ArduinoCommands.Handshake, 0);
                        Console.WriteLine("Sent");
                        Console.Write("Waiting for reply... ");
                        DateTime sendTime = DateTime.Now;
                        while ((DateTime.Now - sendTime).Seconds < 1 && !gotHandShakeBack) ;
                        if (gotHandShakeBack)
                        {
                            Console.WriteLine("Got Reply");
                            Status = ArduinoStatus.Connected;
                            return;
                        }
                        else
                        {
                            Console.WriteLine("No Reply");
                        }
                    }
                }
                Console.WriteLine("Can't find arduino, check that the arduino is plugged in.");
                Status = ArduinoStatus.NotConnected;
                if (sp.IsOpen)
                    sp.Close();
            }
            else
            {
                Console.WriteLine("No COM-Ports found, check that the arduino is plugged in.");
                Status = ArduinoStatus.NotConnected;
                if (sp.IsOpen)
                    sp.Close();
            }
        }

        private void SendData(ArduinoCommands command, byte value)
        {
            sp.Write(new byte[] { (byte)command, value }, 0, 2);
        }

        #endregion

        #region Events

        void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte byteCommand = Convert.ToByte(sp.ReadByte());
            ArduinoCommands command = (ArduinoCommands)byteCommand;
            if (command == ArduinoCommands.Handshake)
            {
                if (sp.ReadByte() == 1)
                    gotHandShakeBack = true;
            }
        }

        #endregion
    }
}

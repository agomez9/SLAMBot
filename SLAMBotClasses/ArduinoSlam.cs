using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace SLAMBotClasses
{
    /// <summary>
    /// This class allow you to communicate with the Arduino micro-controller (the blue microchip)
    /// this will let you control all the electronics on the robot.
    /// </summary>
    public class ArduinoSlam
    {
        #region Private Members

        private SerialPort sp;
        private bool gotHandShakeBack;
        private enum ArduinoCommands { Handshake, TestLight, LeftMotor, RightMotor, XForce, YForce, ZForce, Temp, SendInfo };
        public enum ArduinoStatus { Connected, NotConnected, Connecting };
        private ArduinoStatus _Status;
        Thread cThread;
        Thread smoothMotorThread;
        Mutex muMotor;
        private double leftMotorValue;
        private double rightMotorValue;
        private double realLeftMotorValue;
        private double realRightMotorValue;
        private double xForce, yForce, zForce, temperature;

        #endregion

        #region Public Members

        /// <summary>
        /// Contains the status of the Arduino that gets sent to you
        /// when the OnStatusChanged event gets fired.
        /// </summary>
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

        /// <summary>
        /// Contains the sensor information that gets sent to you when
        /// the OnSensorInfoReady event is fired.
        /// </summary>
        public class SensorInfoArgs : EventArgs
        {
            public double XForce, YForce, ZForce, Temperature;

            public SensorInfoArgs (double XForce, double YForce, double ZForce, double Temperature)
            {
                this.XForce = XForce;
                this.YForce = YForce;
                this.ZForce = ZForce;
                this.Temperature = Temperature;
            }
        }

        /// <summary>
        /// Fires when the status of the Arduino micro-controller has changed.
        /// Lets you know if it is connecting, connected or disconnected.
        /// </summary>
        public event EventHandler<StatusArgs> OnStatusChanged;

        /// <summary>
        /// Fires when the Arduino has sent you information. This is for
        /// reading the accelerometer or temperature sensor.
        /// </summary>
        public event EventHandler<SensorInfoArgs> OnSensorInfoReady;

        #endregion

        #region Class Properties

        /// <summary>
        /// Gets the status of the arduino. Connected, connecting or disconnected.
        /// </summary>
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

        /// <summary>
        /// Inits the ArduinoSlam class
        /// </summary>
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

        /// <summary>
        /// This will connect your computer to the arduino micro-controller which
        /// will allow the two to communicate. This will scan all the COM ports on
        /// your computer and automatically find the Arduino. This method is asynchronous
        /// so you can call it and wait for the OnStatusChanged event to tell you it's connected.
        /// </summary>
        public void Connect()
        {
            cThread = new Thread(ThreadConnect);
            cThread.Start();
        }

        /// <summary>
        /// If you are connected to the Arduino this will close the connection. You should
        /// do this before exiting the program but it's not a big deal if you don't.
        /// </summary>
        public void CloseConnection()
        {
            if (cThread.IsAlive)
                cThread.Abort();

            if (sp.IsOpen)
                sp.Close();

            Status = ArduinoStatus.NotConnected;
        }

        /// <summary>
        /// There is a very small LED on the arduino near pin 13, this turns it off.
        /// </summary>
        public void TurnTestLightOff()
        {
            SendData(ArduinoCommands.TestLight, 0);
        }

        /// <summary>
        /// There is a very small LED on the arduino near pin 13, this turns it on.
        /// </summary>
        public void TurnTestLightOn()
        {
            SendData(ArduinoCommands.TestLight, 1);
        }

        /// <summary>
        /// This will tell the arduino that you want to have it start or stop sending you
        /// information about the accelerometer and temperature sensor. This information
        /// will be sent to you through the OnSensorInfoReady event and will fire about
        /// every 200 milliseconds.
        /// </summary>
        /// <param name="OffOrOn"></param>
        public void SendGForceAndTemp(bool OffOrOn)
        {
            SendData(ArduinoCommands.SendInfo, OffOrOn ? (byte)1 : (byte)0);
        }

        /// <summary>
        /// Sets the speed of the left side of the robot
        /// </summary>
        /// <param name="value">Can be between -1 and 1. -1 is full speed backwards 1 is full speed forward.</param>
        public void SetLeftMotor(double value)
        {
            muMotor.WaitOne();
            leftMotorValue = value;
            muMotor.ReleaseMutex();
        }

        /// <summary>
        /// Sets the speed of the right side of the robot
        /// </summary>
        /// <param name="value">Can be between -1 and 1. -1 is full speed backwards 1 is full speed forward.</param>
        public void SetRightMotor(double value)
        {
            muMotor.WaitOne();
            rightMotorValue = value;
            muMotor.ReleaseMutex();
        }

        #endregion

        #region Private Methods

        private void ThreadSmoothMotors()
        {
            muMotor = new Mutex();

            while (Status == ArduinoStatus.Connected)
            {
                muMotor.WaitOne();
                if (realLeftMotorValue != leftMotorValue)
                {
                    //if the real motor value is too high
                    if (leftMotorValue - realLeftMotorValue < 0)
                    {
                        //if your off my only 0.1 just set the realMotor to the motor
                        if (leftMotorValue - realLeftMotorValue > -0.1)
                            realLeftMotorValue = leftMotorValue;
                        else
                            realLeftMotorValue -= 0.1;
                    }
                    //if the real motor value is too low
                    else if (leftMotorValue - realLeftMotorValue > 0)
                    {
                        //if your off my only 0.1 just set the realMotor to the motor
                        if (leftMotorValue - realLeftMotorValue < 0.1)
                            realLeftMotorValue = leftMotorValue;
                        else
                            realLeftMotorValue += 0.1;
                    }

                    double leftMotorSpeed = (64 + (64 * realLeftMotorValue));
                    if (leftMotorSpeed < 1)
                        leftMotorSpeed = 1;
                    else if (leftMotorSpeed > 127)
                        leftMotorSpeed = 127;
                    SendData(ArduinoCommands.LeftMotor, (byte)leftMotorSpeed);
                }

                if (realRightMotorValue != rightMotorValue)
                {
                    //if the real motor value is too high
                    if (rightMotorValue - realRightMotorValue < 0)
                    {
                        //if your off my only 0.1 just set the realMotor to the motor
                        if (rightMotorValue - realRightMotorValue > -0.1)
                            realRightMotorValue = rightMotorValue;
                        else
                            realRightMotorValue -= 0.1;
                    }
                    //if the real motor value is too low
                    else if (rightMotorValue - realRightMotorValue > 0)
                    {
                        //if your off my only 0.1 just set the realMotor to the motor
                        if (rightMotorValue - realRightMotorValue < 0.1)
                            realRightMotorValue = rightMotorValue;
                        else
                            realRightMotorValue += 0.1;
                    }

                    double rightMotorSpeed = (192 + (64 * realRightMotorValue));
                    if (rightMotorSpeed < 128)
                        rightMotorSpeed = 128;
                    else if (rightMotorSpeed > 255)
                        rightMotorSpeed = 255;
                    SendData(ArduinoCommands.RightMotor, (byte)rightMotorSpeed);
                }

                muMotor.ReleaseMutex();

                Thread.Sleep(100);
            }
        }

        private void ThreadConnect()
        {
            leftMotorValue = rightMotorValue = realLeftMotorValue = realRightMotorValue = 0;

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
                            smoothMotorThread = new Thread(ThreadSmoothMotors);
                            smoothMotorThread.Start();
                            SendGForceAndTemp(true);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("No Reply");
                            sp.Close();
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
            else if (command == ArduinoCommands.XForce)
            {
                double x = sp.ReadByte();
                x -= 128;
                double ratio = 1201d / 255d;
                x *= ratio;
                x /= 100d;                
                xForce = x;
            }
            else if (command == ArduinoCommands.YForce)
            {
                double y = sp.ReadByte();
                y -= 128;
                double ratio = 1201d / 255d;
                y *= ratio;
                y /= 100d;               
                yForce = y;
            }
            else if (command == ArduinoCommands.ZForce)
            {
                double z = sp.ReadByte();
                z -= 128;
                double ratio = 1201d / 255d;
                z *= ratio;
                z /= 100d; 
                zForce = z;
            }
            else if (command == ArduinoCommands.Temp)
            {
                double temp = sp.ReadByte() - 50;
                double tempOut = (temp * (9d / 5d)) + 32;
                temperature = tempOut;
                if (OnSensorInfoReady != null)
                    OnSensorInfoReady(this, new SensorInfoArgs(xForce, yForce, zForce, temperature));
            }
        }

        #endregion
    }
}


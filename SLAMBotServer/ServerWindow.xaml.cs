using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using SLAMBotClasses;
using System.Windows.Threading;

namespace SLAMBotServer
{
    /// <summary>
    /// Interaction logic for ServerWindow.xaml
    /// </summary>
    public partial class ServerWindow : Window
    {
        #region Members

        TCPSlamServer tcpServer;
        ArduinoSlam arduino;
        bool userDisconnect = false;

        #endregion

        #region Constructor

        public ServerWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Private Methods

        private void SendArduinoStatus()
        {
            if (tcpServer != null && tcpServer.Status == TCPSlamServer.ServerStatus.Connected)
            {
                tcpServer.SendData(TCPSlamBase.MessageType.ArduinoStatus, new byte[] { (byte)arduino.Status });
            }
        }

        #endregion

        #region Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            arduino = new ArduinoSlam();
            arduino.OnStatusChanged += new EventHandler<ArduinoSlam.StatusArgs>(arduino_OnStatusChanged);
            arduino.Connect();

            tcpServer = new TCPSlamServer();
            tcpServer.Port = 9988;
            tcpServer.OnConnectionStatusChanged += new EventHandler<TCPSlamServer.ServerStatusArgs>(tcpServer_OnConnectionStatusChanged);
            tcpServer.OnDataReceived += new EventHandler<TCPSlamBase.MessageArgs>(tcpServer_OnDataReceived);
            txtIP.Text = Common.GetIP();
            try
            {
                tcpServer.IPAddress = IPAddress.Parse(txtIP.Text);
                tcpServer.StartServer();
            }
            catch
            {
                Console.WriteLine("Invalid IP: " + txtIP.Text);
            }
        }

        void tcpServer_OnDataReceived(object sender, TCPSlamBase.MessageArgs e)
        {
            if (e.MessageType == TCPSlamBase.MessageType.ArduinoConnection)
            {
                if (BitConverter.ToBoolean(e.Message, 0))
                    arduino.Connect();
                else
                    arduino.CloseConnection();
            }
        }

        void arduino_OnStatusChanged(object sender, ArduinoSlam.StatusArgs e)
        {
            SendArduinoStatus();
            if (e.Status == ArduinoSlam.ArduinoStatus.Connected)
            {
                lblArduinoStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblArduinoStatus.Content = "Connected"; }));
                btnArduinoConnect.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnArduinoConnect.Content = "Disconnect"; }));
            }
            else if (e.Status == ArduinoSlam.ArduinoStatus.Connecting)
            {
                lblArduinoStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblArduinoStatus.Content = "Connecting"; }));
                btnArduinoConnect.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnArduinoConnect.Content = "Disconnect"; }));
            }
            else if (e.Status == ArduinoSlam.ArduinoStatus.NotConnected)
            {
                lblArduinoStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblArduinoStatus.Content = "Disconnected"; }));
                btnArduinoConnect.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnArduinoConnect.Content = "Connect"; }));
            }
        }

        void tcpServer_OnConnectionStatusChanged(object sender, TCPSlamServer.ServerStatusArgs e)
        {            
            if (e.Status == TCPSlamServer.ServerStatus.Connected)
            {
                txtIP.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { txtIP.IsEnabled = false; }));
                btnListen.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnListen.Content = "Disconnect"; }));
                lblStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblStatus.Content = "Status: Connected"; }));
                groupBandwidth.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { groupBandwidth.IsEnabled = true; }));
                SendArduinoStatus();
            }
            else if (e.Status == TCPSlamServer.ServerStatus.Disconnected)
            {
                txtIP.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { txtIP.IsEnabled = true; }));
                btnListen.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnListen.Content = "Listen"; }));
                lblStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblStatus.Content = "Status: Disconnected"; }));
                groupBandwidth.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { groupBandwidth.IsEnabled = false; }));
                if (!userDisconnect)
                    tcpServer.StartServer();
                else
                    userDisconnect = false;
            }
            else if (e.Status == TCPSlamServer.ServerStatus.Listening)
            {
                txtIP.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { txtIP.IsEnabled = false; }));
                btnListen.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnListen.Content = "Stop"; }));
                lblStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblStatus.Content = "Status: Listening"; }));
                groupBandwidth.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { groupBandwidth.IsEnabled = false; }));
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (tcpServer.Status == TCPSlamServer.ServerStatus.Disconnected)
            {
                try
                {
                    tcpServer.IPAddress = IPAddress.Parse(txtIP.Text);
                    tcpServer.StartServer();
                }
                catch
                {
                    Console.WriteLine("Invalid IP: " + txtIP.Text);
                }
            }
            else
            {
                userDisconnect = true;
                tcpServer.CloseConnection();                
            }
        }

        private void btnArduinoConnect_Click(object sender, RoutedEventArgs e)
        {
            if (arduino.Status == ArduinoSlam.ArduinoStatus.NotConnected)
            {
                arduino.Connect();
            }
            else
            {
                arduino.CloseConnection();
            }
        }

        #endregion
    }
}

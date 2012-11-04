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
using System.Media;
using System.IO;

namespace SLAMBotClient
{
    /// <summary>
    /// Interaction logic for ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window
    {
        #region Members

        CameraWindow cameraWindow;
        TCPSlamClient tcpClient;
        ControllerSlam controller;
        ArduinoSlam.ArduinoStatus ArduinoStatus = ArduinoSlam.ArduinoStatus.NotConnected;

        #endregion

        #region Constructor

        public ClientWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Private Methods


        #endregion

        #region Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            controller = new ControllerSlam();
            controller.OnButtonsChanged += new EventHandler<ControllerSlam.ButtonArgs>(controller_OnButtonsChanged);
            tcpClient = new TCPSlamClient();
            tcpClient.Port = 9988;
            tcpClient.OnConnectionStatusChanged += new EventHandler<TCPSlamClient.ClientStatusArgs>(tcpClient_OnConnectionStatusChanged);
            tcpClient.OnDataReceived += new EventHandler<TCPSlamBase.MessageArgs>(tcpClient_OnDataReceived);
            txtIP.Text = Common.GetIP();
        }

        void controller_OnButtonsChanged(object sender, ControllerSlam.ButtonArgs e)
        {
            if (tcpClient != null && tcpClient.Status == TCPSlamClient.ClientStatus.Connected)
            {
                if (e.CameraMove != 0)
                {
                    if (cameraWindow != null)
                    {
                        cameraWindow.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { cameraWindow.SetCameraAngle(e.CameraMove); }));
                    }
                }

                if (e.LeftStickChanged)
                    tcpClient.SendData(TCPSlamBase.MessageType.LeftMotor, BitConverter.GetBytes(e.LeftStick));

                if (e.RightStickChanged)
                    tcpClient.SendData(TCPSlamBase.MessageType.RightMotor, BitConverter.GetBytes(e.RightStick));
            }
        }

        void cameraWindow_Closed(object sender, EventArgs e)
        {
            if (tcpClient.Status == TCPSlamClient.ClientStatus.Connected)
                tcpClient.SendData(TCPSlamBase.MessageType.StopVideo, new byte[1]);
        }

        void tcpClient_OnDataReceived(object sender, TCPSlamBase.MessageArgs e)
        {
            if (e.MessageType == TCPSlamBase.MessageType.ArduinoStatus)
            {
                ArduinoStatus = (ArduinoSlam.ArduinoStatus)e.Message[0];
                if (ArduinoStatus == ArduinoSlam.ArduinoStatus.Connected)
                {
                    lblArduinoStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblArduinoStatus.Content = "Connected"; }));
                    btnArduinoConnect.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnArduinoConnect.Content = "Disconnect"; }));
                }
                else if (ArduinoStatus == ArduinoSlam.ArduinoStatus.Connecting)
                {
                    lblArduinoStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblArduinoStatus.Content = "Connecting..."; }));
                    btnArduinoConnect.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnArduinoConnect.Content = "Disconnect"; }));
                }
                else if (ArduinoStatus == ArduinoSlam.ArduinoStatus.NotConnected)
                {
                    lblArduinoStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblArduinoStatus.Content = "Disconnected"; }));
                    btnArduinoConnect.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnArduinoConnect.Content = "Connect"; }));
                }
            }
            else if (e.MessageType == TCPSlamBase.MessageType.KinectList)
            {
                byte count = e.Message[0];
                cmbKinect.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
                {
                    cmbKinect.Items.Clear();
                    for (byte i = 1; i <= count; i++)
                        cmbKinect.Items.Add(new ComboBoxItem() { Content = "Kinect " + i });
                    if (count > 0)
                        cmbKinect.SelectedIndex = 0;
                }));
            }
            else if (e.MessageType == TCPSlamBase.MessageType.KinectFrame)
            {
                cameraWindow.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate() { cameraWindow.LoadFrame(e.Message); }));
            }
            else if (e.MessageType == TCPSlamBase.MessageType.Audio)
            {
                SoundPlayer sp = new SoundPlayer(new MemoryStream(e.Message));
                sp.Play();
            }
            else if (e.MessageType == TCPSlamBase.MessageType.XForce)
            {
                if (cameraWindow != null)
                    cameraWindow.XForce = BitConverter.ToDouble(e.Message, 0);
            }
            else if (e.MessageType == TCPSlamBase.MessageType.YForce)
            {
                if (cameraWindow != null)
                    cameraWindow.YForce = BitConverter.ToDouble(e.Message, 0);
            }
            else if (e.MessageType == TCPSlamBase.MessageType.ZForce)
            {
                if (cameraWindow != null)
                    cameraWindow.ZForce = BitConverter.ToDouble(e.Message, 0);
            }
            else if (e.MessageType == TCPSlamBase.MessageType.Temperature)
            {
                if (cameraWindow != null)
                    cameraWindow.Temperature = BitConverter.ToDouble(e.Message, 0);
            }
        }

        void tcpClient_OnConnectionStatusChanged(object sender, TCPSlamClient.ClientStatusArgs e)
        {
            if (e.Status == TCPSlamClient.ClientStatus.Connected)
            {
                txtIP.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { txtIP.IsEnabled = false; }));
                btnConnect.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnConnect.Content = "Disconnect"; }));
                lblStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblStatus.Content = "Status: Connected"; }));
                groupCommunication.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { groupCommunication.IsEnabled = true; }));
                groupArduino.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { groupArduino.IsEnabled = true; }));
            }
            else if (e.Status == TCPSlamClient.ClientStatus.Connecting)
            {
                txtIP.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { txtIP.IsEnabled = false; }));
                btnConnect.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnConnect.Content = "Disconnect"; }));
                lblStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblStatus.Content = "Status: Connecting..."; }));
                groupCommunication.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { groupCommunication.IsEnabled = false; }));
                groupArduino.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { groupArduino.IsEnabled = false; }));
            }
            else if (e.Status == TCPSlamClient.ClientStatus.Disconnected)
            {
                txtIP.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { txtIP.IsEnabled = true; }));
                btnConnect.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnConnect.Content = "Connect"; }));
                lblStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblStatus.Content = "Status: Disconnected"; }));
                groupCommunication.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { groupCommunication.IsEnabled = false; }));
                btnArduinoConnect.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnArduinoConnect.Content = "Connect"; }));
                lblArduinoStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblArduinoStatus.Content = "Disconnected"; }));
                groupArduino.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { groupArduino.IsEnabled = false; }));
                ArduinoStatus = ArduinoSlam.ArduinoStatus.NotConnected;
                cmbKinect.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { cmbKinect.Items.Clear(); }));
            }
        }

        private void btnArduinoConnect_Click(object sender, RoutedEventArgs e)
        {
            if (tcpClient.Status == TCPSlamClient.ClientStatus.Connected)
            {
                if (ArduinoStatus == ArduinoSlam.ArduinoStatus.NotConnected)
                    tcpClient.SendData(TCPSlamBase.MessageType.ArduinoConnection, BitConverter.GetBytes(true));
                else
                    tcpClient.SendData(TCPSlamBase.MessageType.ArduinoConnection, BitConverter.GetBytes(false));
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (tcpClient.Status == TCPSlamClient.ClientStatus.Disconnected)
            {
                try
                {
                    tcpClient.IPAddress = IPAddress.Parse(txtIP.Text);
                    tcpClient.Connect();
                }
                catch
                {
                    Console.WriteLine("Invalid IP: " + txtIP.Text);
                }
            }
            else
            {
                tcpClient.CloseConnection();
            }
        }

        private void cmbKinect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbKinect.SelectedItem != null && tcpClient.Status == TCPSlamClient.ClientStatus.Connected)
            {
                btnStartCamera.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnStartCamera.IsEnabled = true; }));
                checkVoice.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { checkVoice.IsEnabled = true; }));
            }
            else
            {
                btnStartCamera.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnStartCamera.IsEnabled = false; }));
                checkVoice.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { checkVoice.IsEnabled = false; }));
            }
        }

        private void btnStartCamera_Click(object sender, RoutedEventArgs e)
        {
            cameraWindow = new CameraWindow();
            cameraWindow.Closed += new EventHandler(cameraWindow_Closed);
            cameraWindow.AngleUpdated += new EventHandler(cameraWindow_AngleUpdated);
            cameraWindow.Show();
            tcpClient.SendData(TCPSlamBase.MessageType.SendVideo, new byte[] { (byte)cmbKinect.SelectedIndex });
        }

        void cameraWindow_AngleUpdated(object sender, EventArgs e)
        {
            if (tcpClient.Status == TCPSlamClient.ClientStatus.Connected)
                tcpClient.SendData(TCPSlamBase.MessageType.CameraMove, BitConverter.GetBytes(cameraWindow.CameraAngle));
        }

        #endregion
    }
}


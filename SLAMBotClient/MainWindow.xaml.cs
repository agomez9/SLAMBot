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
using System.Windows.Navigation;
using System.Windows.Shapes;
using SLAMBotClasses;
using System.Net;
using System.Windows.Threading;

namespace SLAMBotClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TCPSlamClient slamClient;
        ControllerSlam controller;
        ExampleKinect exampleK;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (slamClient.Status == TCPSlamClient.ClientStatus.Disconnected)
                slamClient.Connect(IPAddress.Parse(txtIP.Text), 9988);
            else if (slamClient.Status == TCPSlamClient.ClientStatus.Connected)
                slamClient.CloseConnection();
            else if (slamClient.Status == TCPSlamClient.ClientStatus.Connecting)
                slamClient.CloseConnection();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtIP.Text = Common.GetIP();
            slamClient = new TCPSlamClient();
            slamClient.OnConnectionStatusChanged += new EventHandler<TCPSlamClient.ClientStatusArgs>(slamClient_OnConnectionStatusChanged);
            slamClient.OnDataReceived += new EventHandler<TCPSlamBase.MessageArgs>(slamClient_OnDataReceived);
            exampleK = new ExampleKinect();
            exampleK.Show();
            controller = new ControllerSlam();
            controller.OnButtonsChanged += new EventHandler<ControllerSlam.ButtonArgs>(controller_OnButtonsChanged);
        }

        void controller_OnButtonsChanged(object sender, ControllerSlam.ButtonArgs e)
        {
            if (slamClient.Status == TCPSlamClient.ClientStatus.Connected)
            {
                if (e.LeftStickChanged)
                    slamClient.SendData(TCPSlamBase.MessageType.LeftMotor, BitConverter.GetBytes(e.LeftStick));
                if (e.RightStickChanged)
                    slamClient.SendData(TCPSlamBase.MessageType.RightMotor, BitConverter.GetBytes(e.RightStick));
                if (e.LightsChanged)
                    slamClient.SendData(TCPSlamBase.MessageType.Lights, BitConverter.GetBytes((byte)e.Lights));
            }
        }

        void slamClient_OnDataReceived(object sender, TCPSlamBase.MessageArgs e)
        {
            if (e.MessageType == TCPSlamBase.MessageType.CameraOneFrame)
            {
                exampleK.LoadImage(e.Message);
            }
        }

        void slamClient_OnConnectionStatusChanged(object sender, TCPSlamClient.ClientStatusArgs e)
        {
            if (e.Status == TCPSlamClient.ClientStatus.Connected)
                lblStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblStatus.Content = "Status: Connected"; btnConnect.Content = "Disconnect"; }));
            else if (e.Status == TCPSlamClient.ClientStatus.Disconnected)
                lblStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblStatus.Content = "Status: Disconnected"; btnConnect.Content = "Connect"; }));
            else if (e.Status == TCPSlamClient.ClientStatus.Connecting)
                lblStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblStatus.Content = "Status: Connecting"; btnConnect.Content = "Stop"; }));
        }
    }
}

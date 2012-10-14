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
using System.Threading;


namespace SLAMBotServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ExampleKinect exampleK = new ExampleKinect();
        KinectSlam slamKinect;
        TCPSlamServer slamServer;
        private Thread testThread;
        string IP;

        public MainWindow()
        {
            InitializeComponent();            
        }

        void slamServer_OnConnectionStatusChanged(object sender, TCPSlamServer.ServerStatusArgs e)
        {
            if (e.Status == TCPSlamServer.ServerStatus.Connected)
                lblStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblStatus.Content = "Status: Connected"; btnListen.Content = "Disconnect"; }));  
            else if (e.Status == TCPSlamServer.ServerStatus.Disconnected)
                lblStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblStatus.Content = "Status: Disconnected"; btnListen.Content = "Listen"; }));  
            else if (e.Status == TCPSlamServer.ServerStatus.Listening)
                lblStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblStatus.Content = "Status: Listening"; btnListen.Content = "Stop"; }));                
        }

        private void btnListen_Click(object sender, RoutedEventArgs e)
        {
            if (slamServer.Status == TCPSlamServer.ServerStatus.Disconnected)
                slamServer.StartServer(IPAddress.Parse(IP), 9988);
            else if (slamServer.Status == TCPSlamServer.ServerStatus.Connected)
                slamServer.CloseConnection();
            else if (slamServer.Status == TCPSlamServer.ServerStatus.Listening)
                slamServer.CloseConnection();
        }       

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            slamServer = new TCPSlamServer();
            slamServer.OnConnectionStatusChanged += new EventHandler<TCPSlamServer.ServerStatusArgs>(slamServer_OnConnectionStatusChanged);
            slamServer.OnDataReceived += new EventHandler<TCPSlamBase.MessageArgs>(slamServer_OnDataReceived);
            //slamKinect = new KinectSlam();
            //slamKinect.StartSensor(slamKinect.GetKinectList()[0]);
            
            IP = Common.GetIP();
            lblIP.Content = "IP: " + IP;
            //exampleK.Show();
        }

        void slamServer_OnDataReceived(object sender, TCPSlamBase.MessageArgs e)
        {
            if (e.MessageType == TCPSlamBase.MessageType.LeftMotor)
                Console.WriteLine("Left Motor: " + BitConverter.ToDouble(e.Message, 0));
            else if (e.MessageType == TCPSlamBase.MessageType.RightMotor)
                Console.WriteLine("Right Motor: " + BitConverter.ToDouble(e.Message, 0));
            else if (e.MessageType == TCPSlamBase.MessageType.Lights)
                Console.WriteLine("Light: " + e.Message[0]);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //exampleK.Show();
            testThread = new Thread(testRun);
            testThread.Start();                       
        }

        private void testRun()
        {
            long lastFrame = -1;
            DateTime lastFrameSent = DateTime.Now;
            double sendInterval = 1 / 15;
            while(true)
            {
                int newFrame = slamKinect.GetCurrentFrameNumber();
                double numerator = slamServer.SendQueueSize > 20000 ? 20000 : slamServer.SendQueueSize;
                slamKinect.FrameQuality = (int)(20d - ((numerator / 20000d) * 20d));
                if (lastFrame != newFrame &&  slamServer.SendQueueSize < 20000 && (DateTime.Now - lastFrameSent).TotalSeconds >= sendInterval)
                {
                    //exampleK.LoadImage(slamKinect.GetCurrentFrame(KinectSlam.KinectIdentifer.KinectOne));
                    lastFrameSent = DateTime.Now;
                    lastFrame = newFrame;
                    slamServer.SendData(TCPSlamBase.MessageType.CameraOneFrame, slamKinect.GetCurrentFrame());
                }
                else
                {
                    if (slamServer.SendQueueSize > 20000)
                        Console.WriteLine("bad");
                    Thread.Sleep(1);
                }
            }
        }
    }
}

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

namespace SLAMBotClient
{
    /// <summary>
    /// Interaction logic for ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window
    {
        #region Members

        TCPSlamClient tcpClient;

        #endregion

        #region Constructor

        public ClientWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tcpClient = new TCPSlamClient();
            tcpClient.Port = 9988;
            tcpClient.OnConnectionStatusChanged += new EventHandler<TCPSlamClient.ClientStatusArgs>(tcpClient_OnConnectionStatusChanged);
            txtIP.Text = Common.GetIP();
        }

        void tcpClient_OnConnectionStatusChanged(object sender, TCPSlamClient.ClientStatusArgs e)
        {
            if (e.Status == TCPSlamClient.ClientStatus.Connected)
            {
                txtIP.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { txtIP.IsEnabled = false; }));
                btnConnect.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnConnect.Content = "Disconnect"; }));
                lblStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblStatus.Content = "Status: Connected"; }));
            }
            else if (e.Status == TCPSlamClient.ClientStatus.Connecting)
            {
                txtIP.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { txtIP.IsEnabled = false; }));
                btnConnect.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnConnect.Content = "Disconnect"; }));
                lblStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblStatus.Content = "Status: Connecting..."; }));
            }
            else if (e.Status == TCPSlamClient.ClientStatus.Disconnected)
            {
                txtIP.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { txtIP.IsEnabled = true; }));
                btnConnect.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { btnConnect.Content = "Connect"; }));
                lblStatus.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { lblStatus.Content = "Status: Disconnected"; }));
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

        #endregion
    }
}

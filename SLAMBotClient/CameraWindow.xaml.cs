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
using System.IO;
using System.Windows.Threading;
using SLAMBotClient.GUIControls;

namespace SLAMBotClient
{
    /// <summary>
    /// Interaction logic for CameraWindow.xaml
    /// </summary>
    public partial class CameraWindow : Window
    {
        #region Members

        public event EventHandler AngleUpdated;
        CameraAngle cameraAngleControl;

        #endregion

        #region Properties

        public int CameraAngle
        {
            get
            {
                return cameraAngleControl.Angle;
            }
        }

        #endregion

        #region Constructor

        public CameraWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Public Methods

        public void LoadFrame(byte[] frame)
        {
            var bitmapImage = new BitmapImage();
            Stream stream = new MemoryStream(frame);
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            image1.Source = bitmapImage;
        }

        public void SetCameraAngle(float direction)
        {
            cameraAngleControl.SetAngle(direction);
        }        

        #endregion

        #region Events

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                WindowStyle = System.Windows.WindowStyle.None;
                WindowState = System.Windows.WindowState.Maximized;
            }
            else if (e.Key == Key.Escape)
            {
                WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                WindowState = System.Windows.WindowState.Normal;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            image1.Height = Height;
            image1.Width = Width;

            cameraAngleControl = new CameraAngle();
            cameraAngleControl.AngleUpdated += new EventHandler(cameraAngleControl_AngleUpdated);
            canvasCameraAngle.Children.Add(cameraAngleControl);
            canvasCameraAngle.Margin = new Thickness(Width - 120, 10, 0, 0);
        }

        void cameraAngleControl_AngleUpdated(object sender, EventArgs e)
        {
            if (AngleUpdated != null)
                AngleUpdated(this, EventArgs.Empty);
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            image1.Height = Height;
            image1.Width = Width;
            canvasCameraAngle.Margin = new Thickness(Width - 120, 10, 0, 0);
        }

        #endregion        
    }
}

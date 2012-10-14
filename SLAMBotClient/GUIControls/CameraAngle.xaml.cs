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
using System.Threading;
using System.Windows.Threading;

namespace SLAMBotClient.GUIControls
{
    /// <summary>
    /// Interaction logic for CameraAngle.xaml
    /// </summary>
    public partial class CameraAngle : UserControl
    {
        #region Members

        public event EventHandler AngleUpdated;
        private DateTime lastChange;
        private float _CurrentAngle;
        Thread DoneMovingThread;
        bool ReadyToSend;

        #endregion

        #region Properties

        public int Angle
        {
            get
            {
                return (int)_CurrentAngle;
            }
        }

        #endregion

        #region Constructor

        public CameraAngle()
        {
            InitializeComponent();
            lastChange = DateTime.Now;
            _CurrentAngle = 0;
            ReadyToSend = false;
            Visibility = System.Windows.Visibility.Hidden;
            DoneMovingThread = new Thread(DoneMoving);
            DoneMovingThread.Start();
        }

        #endregion

        #region Public Methods

        public void SetAngle(float direction)
        {
            if ((DateTime.Now - lastChange).TotalMilliseconds > 20)
            {
                _CurrentAngle += direction;
                if (_CurrentAngle > 27)
                    _CurrentAngle = 27;
                else if (_CurrentAngle < -27)
                    _CurrentAngle = -27;
                lastChange = DateTime.Now;
                UpdateGUI();
                ReadyToSend = true;
            }
        }

        #endregion

        #region Private Methods

        private void DoneMoving()
        {
            while (true)
            {
                if (ReadyToSend == true)
                {
                    if ((DateTime.Now - lastChange).TotalSeconds > 1)
                    {
                        ReadyToSend = false;
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { Visibility = System.Windows.Visibility.Hidden; }));
                        if (AngleUpdated != null)
                            AngleUpdated(this, EventArgs.Empty);
                    }
                }
                Thread.Sleep(1);
            }
        }

        private void UpdateGUI()
        {
            if (Visibility == System.Windows.Visibility.Hidden)
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { Visibility = System.Windows.Visibility.Visible; }));
            float totalPosition = ((27 - _CurrentAngle) / 54) * 200;
            canvasSelection.Margin = new Thickness(canvasSelection.Margin.Left, totalPosition, canvasSelection.Margin.Right, canvasSelection.Margin.Bottom);
            lblAngle.Content = "Camera Angle: " + (int)_CurrentAngle;
        }

        #endregion     
    }
}

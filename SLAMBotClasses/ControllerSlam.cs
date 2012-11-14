using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework.Input;

namespace SLAMBotClasses
{
    /// <summary>
    /// This is for the XBox remote control. Use this to easily drive the robot.
    /// </summary>
    public class ControllerSlam
    {
        #region Members
        /// <summary>
        /// This event gets fired when a button has been pressed on the controller.
        /// </summary>
        public event EventHandler<ButtonArgs> OnButtonsChanged;
        private Thread processControlsThread;
        private double _LeftStick;
        private double _RightStick;
        private double LastLeftStick;
        private double LastRightStick;
        private DateTime lastStickSend;
        private float _CameraMove;              

        #endregion

        #region Properties

        /// <summary>
        /// Gets the position of the left thumb stick, value is between -1 and 1
        /// </summary>
        public double LeftStick
        {
            get
            {
                return _LeftStick;
            }
        }

        /// <summary>
        /// Gets the position of the right thumb stick, value is between -1 and 1
        /// </summary>
        public double RightStick
        {
            get
            {
                return _RightStick;
            }
        }

        /// <summary>
        /// Gets the position of the right - left trigger which is use to move the Kinect camers.
        /// </summary>
        public float CameraMove
        {
            get
            {
                return _CameraMove;
            }
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Arguments returned from the OnButtonsChanged() event.
        /// </summary>
        public class ButtonArgs : EventArgs
        {
            //Position of the left stick.
            public double LeftStick;
            //If the left stick has changed.
            public bool LeftStickChanged;
            //Position of the right stick.
            public double RightStick;
            //If the right stick has changed.
            public bool RightStickChanged;
            //How much to move the camers.
            public float CameraMove;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Starts up the ControllerSlam class.
        /// </summary>
        public ControllerSlam()
        {
            ResetControls();
            processControlsThread = new Thread(ProcessControls);
            processControlsThread.Start();            
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets everything to it's defaults. You shouldn't really need to call this.
        /// </summary>
        public void ResetControls()
        {
            _LeftStick = LastLeftStick = 0;
            _RightStick = LastRightStick = 0;
            _CameraMove = 0;
            lastStickSend = DateTime.Now;
        }

        #endregion

        #region Private Methods

        private void ProcessControls()
        {
            while (true)
            {
                if (GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One).IsConnected)
                {
                    _LeftStick = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One).ThumbSticks.Left.Y;
                    _RightStick = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One).ThumbSticks.Right.Y;                                        
                    _CameraMove = -GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One).Triggers.Left + GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One).Triggers.Right;

                    TestControls();
                }
                Thread.Sleep(1);
            }
        }

        private void TestControls()
        {
            bool SendLeftStick = false;
            bool SendRightStick = false;            

            if ((DateTime.Now - lastStickSend).TotalMilliseconds >= 50)
            {
                if (_LeftStick != LastLeftStick)
                {
                    LastLeftStick = _LeftStick;
                    SendLeftStick = true;
                    lastStickSend = DateTime.Now;
                }
                if (_RightStick != LastRightStick)
                {
                    LastRightStick = _RightStick;
                    SendRightStick = true;
                    lastStickSend = DateTime.Now;
                }
            }

            if (OnButtonsChanged != null)
            {
                ButtonArgs args = new ButtonArgs();
                args.LeftStick = _LeftStick;
                args.LeftStickChanged = SendLeftStick;
                args.RightStick = _RightStick;
                args.RightStickChanged = SendRightStick;
                args.CameraMove = _CameraMove;
                OnButtonsChanged(this, args);
            }
        }

        #endregion
    }
}

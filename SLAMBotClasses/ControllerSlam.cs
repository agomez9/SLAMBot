using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework.Input;

namespace SLAMBotClasses
{
    public class ControllerSlam
    {
        #region Members

        public event EventHandler<ButtonArgs> OnButtonsChanged;
        public enum LightMode { AllOff, AllOn, Blink, Circle };
        private Thread processControlsThread;
        private double _LeftStick;
        private double _RightStick;
        private double LastLeftStick;
        private double LastRightStick;
        private DateTime lastStickSend;
        private LightMode _Lights;
        private LightMode LastLights;
        private float _CameraMove;              

        #endregion

        #region Properties

        public double LeftStick
        {
            get
            {
                return _LeftStick;
            }
        }

        public double RightStick
        {
            get
            {
                return _RightStick;
            }
        }

        public LightMode Lights
        {
            get
            {
                return _Lights;
            }
        }

        public float CameraMove
        {
            get
            {
                return _CameraMove;
            }
        }

        #endregion

        #region Helper Classes

        public class ButtonArgs : EventArgs
        {
            public double LeftStick;
            public bool LeftStickChanged;
            public double RightStick;
            public bool RightStickChanged;
            public LightMode Lights;
            public bool LightsChanged;
            public float CameraMove;
        }

        #endregion

        #region Constructor

        public ControllerSlam()
        {
            ResetControls();
            processControlsThread = new Thread(ProcessControls);
            processControlsThread.Start();            
        }

        #endregion

        #region Public Methods

        public void ResetControls()
        {
            _LeftStick = LastLeftStick = 0;
            _RightStick = LastRightStick = 0;
            _Lights = LastLights = LightMode.AllOff;
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
                    if (GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One).DPad.Down == ButtonState.Pressed)
                        _Lights = LightMode.AllOff;
                    else if (GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One).DPad.Up == ButtonState.Pressed)
                        _Lights = LightMode.AllOn;
                    else if (GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One).DPad.Left == ButtonState.Pressed)
                        _Lights = LightMode.Blink;
                    else if (GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One).DPad.Right == ButtonState.Pressed)
                        _Lights = LightMode.Circle;
                    
                    _CameraMove = -GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One).Triggers.Left + GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One).Triggers.Right;

                    TestControls();
                }
                Thread.Sleep(1);
            }
        }

        private void TestControls()
        {
            bool SendLights = false;
            bool SendLeftStick = false;
            bool SendRightStick = false;            

            if (_Lights != LastLights)
            {
                LastLights = _Lights;
                SendLights = true;
            }

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
                args.Lights = _Lights;
                args.LightsChanged = SendLights;
                args.CameraMove = _CameraMove;
                OnButtonsChanged(this, args);
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SLAMBotClasses;
using System.Net;
using System.Threading;
using Microsoft.Kinect;
using System.Windows.Documents;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SLAMBotClasses
{   
    public class KinectSlam
    {
        #region Members

        private KinectSensor kinectSensor;
        private byte[] CurrentFrame;
        private Thread processFramesThread;
        private Thread processAudioThread;
        private Thread processCameraMoveThread;
        private ColorImageFrame ImageOneRaw;
        private int CurrentFrameNumber;
        private long _FrameQuality = 15;
        private Stream audioStream;
        private bool processFrames;
        private bool processAudio;
        private bool processCameraMove;
        private int cameraAngle;
        private int lastCameraAngle;

        #endregion

        #region Properties        

        public long FrameQuality
        {
            get
            {
                return _FrameQuality;
            }
            set
            {
                _FrameQuality = value > 20 ? 20 : value;
                _FrameQuality = _FrameQuality < 1 ? 1 : _FrameQuality;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Use the /// on each method and property, or atleast the public ones
        /// </summary>
        public KinectSlam()
        {            
            
        }

        #endregion

        #region Public Methods
    
        public List<KinectSensor> GetKinectList()
        {
            List<KinectSensor> sensors = new List<KinectSensor>();
            foreach (KinectSensor kinect in KinectSensor.KinectSensors)            
                if (kinect.Status == KinectStatus.Connected)                
                    sensors.Add(kinect);
            return sensors; 
        }

        public byte[] GetCurrentFrame()
        {
            return CurrentFrame;
        }

        public int GetCurrentFrameNumber()
        {
            return CurrentFrameNumber;
        }

        public void StartSensor(KinectSensor sensor)
        {
            StopSensor();

            cameraAngle = 0;

            processFrames = true;
            processAudio = true;
            processCameraMove = true;

            kinectSensor = sensor;

            CurrentFrameNumber = 0;
            kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            kinectSensor.Start();
            kinectSensor.ColorFrameReady += new System.EventHandler<ColorImageFrameReadyEventArgs>(kinectSensor_ColorFrameReady);
            audioStream = kinectSensor.AudioSource.Start();            

            processFramesThread = new Thread(ProcessFrames);
            processFramesThread.Start();

            //processAudioThread = new Thread(ProcessAudio);
            //processAudioThread.Start();

            lastCameraAngle = 0;
            processCameraMoveThread = new Thread(ProcessCameraMove);
            processCameraMoveThread.Start();
        }

        public void StopSensor()
        {
            if (kinectSensor != null)
            {
                if (processFrames)
                {
                    processFrames = false;
                    processFramesThread.Join();
                    //processAudio = false;
                    //processAudioThread.Join();
                    processCameraMove = false;
                    processCameraMoveThread.Join();
                    kinectSensor.ColorFrameReady -= kinectSensor_ColorFrameReady;
                    kinectSensor.Stop();
                    kinectSensor = null;
                }
            }
        }

        public void MoveCamera(int angle)
        {
            cameraAngle = angle;
        }

        #endregion

        #region Private Methods

        private void ProcessCameraMove()
        {
            while (processCameraMove)
            {
                if (kinectSensor != null)
                {
                    if (cameraAngle != lastCameraAngle)
                    {
                        try
                        {
                            kinectSensor.ElevationAngle = cameraAngle;
                            lastCameraAngle = cameraAngle;
                        }
                        catch
                        {

                        }
                    }
                }
                Thread.Sleep(1);
            }
        }

        private void ProcessAudio()
        {
            while(processAudio)
            {
                byte[] audioArray = new byte[1000];              
                int size = audioStream.Read(audioArray, 0, 1000);
                if (size < 1000)
                    Array.Resize(ref audioArray, size);

                Thread.Sleep(1);
            }
        }

        private void ProcessFrames()
        {
            MemoryStream ms = new MemoryStream();
            //This just sets up jpg compression stuff
            ImageCodecInfo jgpEncoder = GetEncoder(System.Drawing.Imaging.ImageFormat.Jpeg);
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
            EncoderParameters myEncoderParameters = new EncoderParameters(1);
            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder,(long)_FrameQuality);
            myEncoderParameters.Param[0] = myEncoderParameter;

            while (processFrames)
            {
                if (ImageOneRaw != null)
                {
                    myEncoderParameter = new EncoderParameter(myEncoder, _FrameQuality);
                    myEncoderParameters.Param[0] = myEncoderParameter;

                    ImageToBitmap(ImageOneRaw).Save(ms, jgpEncoder, myEncoderParameters);
                    CurrentFrame = ms.ToArray();                                        
                    CurrentFrameNumber = ImageOneRaw.FrameNumber;
                    ImageOneRaw.Dispose();
                    ImageOneRaw = null;
                    ms = new MemoryStream();
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        private ImageCodecInfo GetEncoder(System.Drawing.Imaging.ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
                if (codec.FormatID == format.Guid)
                    return codec;
            return null;
        }

        private Bitmap ImageToBitmap(ColorImageFrame Image)
        {
            byte[] pixeldata = new byte[Image.PixelDataLength];
            Image.CopyPixelDataTo(pixeldata);
            Bitmap bmap = new Bitmap(Image.Width, Image.Height, PixelFormat.Format32bppRgb);
            BitmapData bmapdata = bmap.LockBits(
                new Rectangle(0, 0, Image.Width, Image.Height),
                ImageLockMode.WriteOnly,
                bmap.PixelFormat);
            IntPtr ptr = bmapdata.Scan0;
            Marshal.Copy(pixeldata, 0, ptr, Image.PixelDataLength);
            bmap.UnlockBits(bmapdata);
            return bmap;
        }

        #endregion

        #region Events

        void kinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            //test if you are clear to write to it
            //if it's not null that means the last image is still being processed
            if (ImageOneRaw == null)
            {
                    
                ImageOneRaw = e.OpenColorImageFrame();
            }            
        }

        #endregion       
    }
}

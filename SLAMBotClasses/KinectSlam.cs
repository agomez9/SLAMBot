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

        public event EventHandler<AudioStreamArgs> OnAudioReady;
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

        //audio stuff
        private const int RiffHeaderSize = 20;
        private const string RiffHeaderTag = "RIFF";
        private const int WaveformatExSize = 18; // native sizeof(WAVEFORMATEX)
        private const int DataHeaderSize = 8;
        private const string DataHeaderTag = "data";
        private const int FullHeaderSize = RiffHeaderSize + WaveformatExSize + DataHeaderSize;

        #endregion

        #region Structs

        private struct WAVEFORMATEX
        {
            public ushort FormatTag;
            public ushort Channels;
            public uint SamplesPerSec;
            public uint AvgBytesPerSec;
            public ushort BlockAlign;
            public ushort BitsPerSample;
            public ushort Size;
        }

        #endregion

        #region HelperClasses

        public class AudioStreamArgs : EventArgs
        {
            public byte[] audio;
            public AudioStreamArgs(byte[] audio)
            {
                this.audio = audio;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Get is sets the Quality of the JPEG returned from GetCurrentFrame() don't remember what the
        /// range is, just mess around with it until it crashes.
        /// </summary>
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
        /// Starts the KinectSlam class
        /// </summary>
        public KinectSlam()
        {            
            
        }

        #endregion

        #region Public Methods
    
        /// <summary>
        /// Gets a list of KinectSensors connected to the computer.
        /// </summary>
        /// <returns>A List of the connected Kinects.</returns>
        public List<KinectSensor> GetKinectList()
        {
            List<KinectSensor> sensors = new List<KinectSensor>();
            //foreach (KinectSensor kinect in KinectSensor.KinectSensors)            
            //    if (kinect.Status == KinectStatus.Connected)                
            //        sensors.Add(kinect);
            return sensors; 
        }

        /// <summary>
        /// Gets a JPEG of the most current frame.
        /// </summary>
        /// <returns>A JPEG that is in a byte array, good for sending over a network.</returns>
        public byte[] GetCurrentFrame()
        {
            return CurrentFrame;
        }

        /// <summary>
        /// Gets the current frame number. Useful to know if you have already seen this frame.
        /// </summary>
        /// <returns>Current frame number</returns>
        public int GetCurrentFrameNumber()
        {
            return CurrentFrameNumber;
        }

        /// <summary>
        /// Starts a kinect, need to call this before using a Kinect. Most likely you will only have one Kinect hooked up.
        /// </summary>
        /// <param name="sensor">Pass in which Kinect you want to start. Use GetKinectList() to decide which Kinect to use.</param>
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

            processAudioThread = new Thread(ProcessAudio);
            //processAudioThread.Start();

            lastCameraAngle = 0;
            processCameraMoveThread = new Thread(ProcessCameraMove);
            processCameraMoveThread.Start();
        }

        /// <summary>
        /// Stops the Kinect that is currently running.
        /// </summary>
        public void StopSensor()
        {
            if (kinectSensor != null)
            {
                if (processFrames)
                {
                    processFrames = false;
                    processFramesThread.Join();
                    processAudio = false;
                    //processAudioThread.Join();
                    processCameraMove = false;
                    processCameraMoveThread.Join();
                    kinectSensor.ColorFrameReady -= kinectSensor_ColorFrameReady;
                    kinectSensor.Stop();
                    kinectSensor = null;
                }
            }
        }

        /// <summary>
        /// Change the camera angle.
        /// </summary>
        /// <param name="angle">I think it can take between -28 and 28</param>
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

        private static void WriteHeaderString(Stream stream, string s)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(s);            
            stream.Write(bytes, 0, bytes.Length);
        }

        private static void UpdateDataLength(Stream stream, int dataLength)
        {
            using (var bw = new BinaryWriter(stream))
            {
                // Write file size - 8 to riff header
                bw.Seek(RiffHeaderTag.Length, SeekOrigin.Begin);
                bw.Write(dataLength + FullHeaderSize - 8);

                // Write data size to data header
                bw.Seek(FullHeaderSize - 4, SeekOrigin.Begin);
                bw.Write(dataLength);
            }
        }

        /// <summary>
        /// A bare bones WAV file header writer
        /// </summary>        
        private static void WriteWavHeader(Stream stream)
        {
            // Data length to be fixed up later
            int dataLength = 0;

            // We need to use a memory stream because the BinaryWriter will close the underlying stream when it is closed
            MemoryStream memStream = null;
            BinaryWriter bw = null;

            // FXCop note: This try/finally block may look strange, but it is
            // the recommended way to correctly dispose a stream that is used
            // by a writer to avoid the stream from being double disposed.
            // For more information see FXCop rule: CA2202
            try
            {
                memStream = new MemoryStream(64);

                WAVEFORMATEX format = new WAVEFORMATEX
                {
                    FormatTag = 1,
                    Channels = 1,
                    SamplesPerSec = 16000,
                    AvgBytesPerSec = 32000,
                    BlockAlign = 2,
                    BitsPerSample = 16,
                    Size = 0
                };

                bw = new BinaryWriter(memStream);

                // RIFF header
                WriteHeaderString(memStream, RiffHeaderTag);
                bw.Write(dataLength + FullHeaderSize - 8); // File size - 8
                WriteHeaderString(memStream, "WAVE");
                WriteHeaderString(memStream, "fmt ");
                bw.Write(WaveformatExSize);

                // WAVEFORMATEX
                bw.Write(format.FormatTag);
                bw.Write(format.Channels);
                bw.Write(format.SamplesPerSec);
                bw.Write(format.AvgBytesPerSec);
                bw.Write(format.BlockAlign);
                bw.Write(format.BitsPerSample);
                bw.Write(format.Size);

                // data header
                WriteHeaderString(memStream, DataHeaderTag);
                bw.Write(dataLength);
                memStream.WriteTo(stream);
            }
            finally
            {
                if (bw != null)
                {
                    memStream = null;
                    bw.Dispose();
                }

                if (memStream != null)
                {
                    memStream.Dispose();
                }
            }
        }

        private void ProcessAudio()
        {            
            while(processAudio)
            {
                MemoryStream ms = new MemoryStream();
                WriteWavHeader(ms);
                byte[] buffer = new byte[4096];
                int count = 0;
                int recordingLength = 0;

                while ((count = audioStream.Read(buffer, 0, buffer.Length)) > 0 && recordingLength < 40960)
                {
                    ms.Write(buffer, 0, count);
                    recordingLength += count;
                }

                UpdateDataLength(ms, recordingLength);

                if (OnAudioReady != null)
                    OnAudioReady(this, new AudioStreamArgs(ms.ToArray()));
                
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

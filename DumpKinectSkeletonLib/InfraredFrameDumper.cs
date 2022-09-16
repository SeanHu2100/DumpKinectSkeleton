using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

using Accord.Video.FFMPEG;
using System.Drawing;

namespace DumpKinectSkeletonLib
{
    public class InfraredFrameDumper
    {
        /// <summary>
        /// Array storing latest infrared frame pixels.
        /// </summary>
        private byte[] _infraredFrameBytes;

        /// <summary>
        /// Maximum value (as a float) that can be returned by the InfraredFrame
        /// </summary>
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;

        /// <summary>
        /// The value by which the infrared source data will be scaled
        /// </summary>
        private const float InfraredSourceScale = 0.75f;

        /// <summary>
        /// Smallest value to display when the infrared data is normalized
        /// </summary>
        private const float InfraredOutputValueMinimum = 0.01f;

        /// <summary>
        /// Largest value to display when the infrared data is normalized
        /// </summary>
        private const float InfraredOutputValueMaximum = 1.0f;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap infraredBitmap = null;

        /// <summary>
        /// Infrared frames output file stream.
        /// </summary>
        //private Stream _infraredOutputStream;

        int width = 512;
        int height = 424;

        // create instance of video writer
        VideoFileWriter writer_infrared = new VideoFileWriter();

        public InfraredFrameDumper( KinectSource kinectSource, string infraredDataOutputFile)
        {
            // open file for output
            try
            {
                //_infraredOutputStream = new BufferedStream( new FileStream(infraredDataOutputFile, FileMode.Create ) );

                // create new video file
                writer_infrared.Open(infraredDataOutputFile, width, height, 30, VideoCodec.MPEG4);
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error opening output file: " + e.Message );
                Close();
                throw;
            }
            kinectSource.InfraredFrameEvent += HandleInfraredFrame;
        }

        /// <summary>
        /// Close subjacent output streams
        /// </summary>
        public void Close()
        {
            writer_infrared?.Close();
            writer_infrared = null;
        }

        /// <summary>
        /// Handle a InfraredFrame. Dumps the frame in raw kinect YUY2 format.
        /// </summary>
        /// <param name="frame"></param>
        public void HandleInfraredFrame(InfraredFrame infraredFrame)
        {
            var format = PixelFormats.Gray32Float;
            int width = infraredFrame.FrameDescription.Width;
            int height = infraredFrame.FrameDescription.Height;
            ushort[] infraredData = new ushort[width * height];
            uint length = infraredFrame.FrameDescription.BytesPerPixel;

            DateTime serverDate = DateTime.Now;
            string currentDateString = string.Format("{0}", serverDate.ToString("yyyyMMddHHmmssffffff"));

            // throw an error is dumper has been closed or output stream could not be opened or written to.
            if (writer_infrared == null )
            {
                throw new InvalidOperationException("InfraredFrameDumper is closed.");
            }
            // lazy infrared frame buffer initialization
            if (infraredBitmap == null )
            {
                infraredBitmap =
                    //new byte[ frame.FrameDescription.LengthInPixels * frame.FrameDescription.BytesPerPixel ];
                    //new byte[width * height * ((format.BitsPerPixel + 7) / 8)];
                    new WriteableBitmap(width, height, 96.0, 96.0, format, null);
            }

            //frame.CopyFrameDataToArray(infraredData);
            // the fastest way to process the infrared frame data is to directly access 
            // the underlying buffer
            using (Microsoft.Kinect.KinectBuffer infraredBuffer = infraredFrame.LockImageBuffer())
            {
                // verify data and write the new infrared frame data to the display bitmap
                if (((width * height) == (infraredBuffer.Size / length)) &&
                    (width == infraredBitmap.PixelWidth) && (height == infraredBitmap.PixelHeight))
                {
                    ProcessInfraredFrameData(infraredBuffer.UnderlyingBuffer, infraredBuffer.Size, length);
                }
            }
            //int colorIndex = 0;
            //for (int infraredIndex = 0; infraredIndex < infraredData.Length; ++infraredIndex)
            //{
            //    ushort ir = infraredData[infraredIndex];
            //    byte intensity = (byte)(ir >> 8);
            //    _infraredFrameBytes[colorIndex++] = intensity; // Blue
            //    _infraredFrameBytes[colorIndex++] = intensity; // Green   
            //    _infraredFrameBytes[colorIndex++] = intensity; // Red
            //    ++colorIndex;
            //}

            try
            {
                //_infraredOutputStream.Write( _infraredFrameBytes, 0, _infraredFrameBytes.Length );
                //int stride = width * format.BitsPerPixel / 8;
                //var infraredBitmap = BitmapSource.Create(width, height, 96.0, 96.0, format, null, _infraredFrameBytes, stride);
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(infraredBitmap));

                //long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                serverDate = DateTime.Now;
                currentDateString = string.Format("{0}", serverDate.ToString("yyyyMMddHHmmssffffff"));

                // write the .png file to disk
                FileStream fs = new FileStream("./output_infrared/" + currentDateString + ".png", FileMode.Create);
                encoder.Save(fs);
                fs.Close();

                // video 
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(infraredBitmap));

                MemoryStream outStream = new MemoryStream();
                enc.Save(outStream);
                Bitmap bmp = new Bitmap(outStream);

                writer_infrared.WriteVideoFrame(bmp);
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error writing to output file(s): " + e.Message );
                Close();
                throw;
            }
        }
        /// <summary>
        /// Directly accesses the underlying image buffer of the InfraredFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the infraredFrameData pointer.
        /// </summary>
        /// <param name="infraredFrameData">Pointer to the InfraredFrame image data</param>
        /// <param name="infraredFrameDataSize">Size of the InfraredFrame image data</param>
        private unsafe void ProcessInfraredFrameData(IntPtr infraredFrameData, uint infraredFrameDataSize, uint length)
        {
            // infrared frame data is a 16 bit value
            ushort* frameData = (ushort*)infraredFrameData;

            // lock the target bitmap
            infraredBitmap.Lock();

            // get the pointer to the bitmap's back buffer
            float* backBuffer = (float*)infraredBitmap.BackBuffer;

            // process the infrared data
            for (int i = 0; i < (int)(infraredFrameDataSize / length); ++i)
            {
                // since we are displaying the image as a normalized grey scale image, we need to convert from
                // the ushort data (as provided by the InfraredFrame) to a value from [InfraredOutputValueMinimum, InfraredOutputValueMaximum]
                backBuffer[i] = Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
            }

            // mark the entire bitmap as needing to be drawn
            infraredBitmap.AddDirtyRect(new Int32Rect(0, 0, infraredBitmap.PixelWidth, infraredBitmap.PixelHeight));

            // unlock the bitmap
            infraredBitmap.Unlock();
        }
    }
}

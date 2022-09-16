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
    public class DepthFrameDumper
    {
        /// <summary>
        /// Array storing latest color frame pixels.
        /// </summary>
        private byte[] _depthFrameBytes;

        /// <summary>
        /// Map depth range to byte range
        /// </summary>
        private const int MapDepthToByte = 8000 / 256;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap depthBitmap = null;

        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private byte[] depthPixels = null;

        /// <summary>
        /// Color frames output file stream.
        /// </summary>
        //private Stream _depthOutputStream;

        int width = 512;
        int height = 424;

        // create instance of video writer
        VideoFileWriter writer_depth = new VideoFileWriter();

        public DepthFrameDumper( KinectSource kinectSource, string depthDataOutputFile )
        {
            // open file for output
            try
            {
                //_depthOutputStream = new BufferedStream( new FileStream(depthDataOutputFile, FileMode.Create ) );

                // create new video file
                writer_depth.Open(depthDataOutputFile, width, height, 30, VideoCodec.MPEG4);
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error opening output file: " + e.Message );
                Close();
                throw;
            }
            kinectSource.DepthFrameEvent += HandleDepthFrame;
        }

        /// <summary>
        /// Close subjacent output streams
        /// </summary>
        public void Close()
        {
            writer_depth?.Close();
            writer_depth = null;
        }

        /// <summary>
        /// Handle a ColorFrame. Dumps the frame in raw kinect YUY2 format.
        /// </summary>
        /// <param name="frame"></param>
        public void HandleDepthFrame( DepthFrame depthFrame)
        {
            var format = PixelFormats.Gray8;
            int width = depthFrame.FrameDescription.Width;
            int height = depthFrame.FrameDescription.Height;
            ushort minDepth = depthFrame.DepthMinReliableDistance;
            //ushort maxDepth = frame.DepthMaxReliableDistance;
            ushort maxDepth = ushort.MaxValue;
            ushort[] depthData = new ushort[width * height];
            uint length = depthFrame.FrameDescription.BytesPerPixel;

            DateTime serverDate = DateTime.Now;
            string currentDateString = string.Format("{0}", serverDate.ToString("yyyyMMddHHmmssffffff"));

            // throw an error is dumper has been closed or output stream could not be opened or written to.
            if (writer_depth == null )
            {
                throw new InvalidOperationException( "ColorFrameDumper is closed." );
            }
            // lazy color frame buffer initialization
            if (depthBitmap == null )
            {
                depthBitmap =
                    //new byte[ frame.FrameDescription.LengthInPixels * frame.FrameDescription.BytesPerPixel ];
                    //new byte[width * height * ((format.BitsPerPixel + 7) / 8)];
                    new WriteableBitmap(width, height, 96.0, 96.0, format, null);
            }
            // lazy color frame buffer initialization
            if (depthPixels == null)
            {
                depthPixels =
                    new byte[width * height];
            }

            bool depthFrameProcessed = false;

            //frame.CopyFrameDataToArray(depthData);
            // the fastest way to process the body index data is to directly access 
            // the underlying buffer
            using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
            {
                // verify data and write the color data to the display bitmap
                if (((width * height) == (depthBuffer.Size / length)) &&
                    (width == depthBitmap.PixelWidth) && (height == depthBitmap.PixelHeight))
                {
                    // Note: In order to see the full range of depth (including the less reliable far field depth)
                    // we are setting maxDepth to the extreme potential depth threshold
                    //ushort maxDepth = ushort.MaxValue;

                    // If you wish to filter by reliable depth distance, uncomment the following line:
                    //// maxDepth = depthFrame.DepthMaxReliableDistance

                    ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, length, depthFrame.DepthMinReliableDistance, maxDepth);
                    depthFrameProcessed = true;
                }
            }

            if (depthFrameProcessed)
            {
                RenderDepthPixels();
            }
            //// convert depth to a visual representation
            //for (int depthIndex = 0; depthIndex < depthData.Length; ++depthIndex)
            //{
            //    // Get the depth for this pixel
            //    ushort depth = depthData[depthIndex];

            //    // To convert to a byte, we're mapping the depth value to the byte range.
            //    // Values outside the reliable depth range are mapped to 0 (black).
            //    _depthFrameBytes[depthIndex] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            //}

            try
            {
                //_depthOutputStream.Write( _depthFrameBytes, 0, _depthFrameBytes.Length );
                //int stride = width * format.BitsPerPixel / 8;
                //var depthBitmap = BitmapSource.Create(width, height, 96.0, 96.0, format, null, _depthFrameBytes, stride);
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(depthBitmap));

                //long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                serverDate = DateTime.Now;
                currentDateString = string.Format("{0}", serverDate.ToString("yyyyMMddHHmmssffffff"));

                // write the .png file to disk
                FileStream fs = new FileStream("./output_depth/" + currentDateString + ".png", FileMode.Create);
                encoder.Save(fs);
                fs.Close();

                // video 
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(depthBitmap));

                MemoryStream outStream = new MemoryStream();
                enc.Save(outStream);
                Bitmap bmp = new Bitmap(outStream);

                writer_depth.WriteVideoFrame(bmp);

            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error writing to output file(s): " + e.Message );
                Close();
                throw;
            }
        }
        /// <summary>
        /// Directly accesses the underlying image buffer of the DepthFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the depthFrameData pointer.
        /// </summary>
        /// <param name="depthFrameData">Pointer to the DepthFrame image data</param>
        /// <param name="depthFrameDataSize">Size of the DepthFrame image data</param>
        /// <param name="minDepth">The minimum reliable depth value for the frame</param>
        /// <param name="maxDepth">The maximum reliable depth value for the frame</param>
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, uint length, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / length); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderDepthPixels()
        {
            depthBitmap.WritePixels(
                new Int32Rect(0, 0, depthBitmap.PixelWidth, depthBitmap.PixelHeight),
                depthPixels,
                depthBitmap.PixelWidth,
                0);
        }
    }
}

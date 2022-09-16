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
    public class BodyIndexFrameDumper
    {
        /// <summary>
        /// Array storing latest bodyIndex frame pixels.
        /// </summary>
        private byte[] _bodyIndexFrameBytes;

        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private const int BytesPerPixel = 4;

        /// <summary>
        /// Collection of colors to be used to display the BodyIndexFrame data.
        /// </summary>
        private static readonly uint[] BodyColor =
        {
            0x0000FF00,
            0x00FF0000,
            0xFFFF4000,
            0x40FFFF00,
            0xFF40FF00,
            0xFF808000,
        };

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap bodyIndexBitmap = null;

        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private uint[] bodyIndexPixels = null;

        /// <summary>
        /// BodyIndex frames output file stream.
        /// </summary>
        //private Stream _bodyIndexOutputStream;

        int width = 512;
        int height = 424;

        // create instance of video writer
        VideoFileWriter writer_bodyIndex = new VideoFileWriter();

        public BodyIndexFrameDumper( KinectSource kinectSource, string bodyIndexDataOutputFile)
        {
            // open file for output
            try
            {
                //_bodyIndexOutputStream = new BufferedStream( new FileStream(bodyIndexDataOutputFile, FileMode.Create ) );

                // create new video file
                writer_bodyIndex.Open(bodyIndexDataOutputFile, width, height, 30, VideoCodec.MPEG4);

            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error opening output file: " + e.Message );
                Close();
                throw;
            }
            kinectSource.BodyIndexFrameEvent += HandleBodyIndexFrame;
        }

        /// <summary>
        /// Close subjacent output streams
        /// </summary>
        public void Close()
        {
            writer_bodyIndex?.Close();
            writer_bodyIndex = null;
        }

        /// <summary>
        /// Handle a BodyIndexFrame. Dumps the frame in raw kinect YUY2 format.
        /// </summary>
        /// <param name="frame"></param>
        public void HandleBodyIndexFrame(BodyIndexFrame bodyIndexFrame)
        {
            var format = PixelFormats.Bgr32;
            int width = bodyIndexFrame.FrameDescription.Width;
            int height = bodyIndexFrame.FrameDescription.Height;
            ushort[] bodyIndexData = new ushort[width * height];
            uint length = bodyIndexFrame.FrameDescription.BytesPerPixel;

            DateTime serverDate = DateTime.Now;
            string currentDateString = string.Format("{0}", serverDate.ToString("yyyyMMddHHmmssffffff"));

            // throw an error is dumper has been closed or output stream could not be opened or written to.
            if (writer_bodyIndex == null )
            {
                throw new InvalidOperationException("BodyIndexFrameDumper is closed.");
            }
            // lazy bodyIndex frame buffer initialization
            if (bodyIndexBitmap == null )
            {
                bodyIndexBitmap =
                    //new byte[ frame.FrameDescription.LengthInPixels * frame.FrameDescription.BytesPerPixel ];
                    //new byte[width * height * ((format.BitsPerPixel + 7) / 8)];
                    new WriteableBitmap(width, height, 96.0, 96.0, format, null);
            }
            // lazy color frame buffer initialization
            if (bodyIndexPixels == null)
            {
                bodyIndexPixels =
                    new uint[width * height];
            }

            //frame.CopyFrameDataToArray(bodyIndexData);

            bool bodyIndexFrameProcessed = false;
            // the fastest way to process the body index data is to directly access 
            // the underlying buffer
            using (Microsoft.Kinect.KinectBuffer bodyIndexBuffer = bodyIndexFrame.LockImageBuffer())
            {
                // verify data and write the color data to the display bitmap
                if (((width * height) == bodyIndexBuffer.Size) &&
                    (width == bodyIndexBitmap.PixelWidth) && (height == bodyIndexBitmap.PixelHeight))
                {
                    ProcessBodyIndexFrameData(bodyIndexBuffer.UnderlyingBuffer, bodyIndexBuffer.Size);
                    bodyIndexFrameProcessed = true;
                }
            }

            if (bodyIndexFrameProcessed)
            {
                RenderBodyIndexPixels();
            }

            try
            {
                //_bodyIndexOutputStream.Write( _bodyIndexFrameBytes, 0, _bodyIndexFrameBytes.Length );
                //int stride = width * format.BitsPerPixel / 8;
                //var bodyIndexBitmap = BitmapSource.Create(width, height, 96.0, 96.0, format, null, _bodyIndexFrameBytes, stride);
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bodyIndexBitmap));

                //long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                serverDate = DateTime.Now;
                currentDateString = string.Format("{0}", serverDate.ToString("yyyyMMddHHmmssffffff"));

                // write the .png file to disk
                FileStream fs = new FileStream("./output_bodyIndex/" + currentDateString + ".png", FileMode.Create);
                encoder.Save(fs);
                fs.Close();

                // create a bitmap to save into the video file
                Bitmap image = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                // video 
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bodyIndexBitmap));

                MemoryStream outStream = new MemoryStream();
                enc.Save(outStream);
                Bitmap bmp = new Bitmap(outStream);

                writer_bodyIndex.WriteVideoFrame(bmp);

                //// write 1000 video frames
                //for (int i = 0; i < 1000; i++)
                //{
                //    image.SetPixel(i % width, i % height, System.Drawing.Color.Red);
                //    writer.WriteVideoFrame(image);
                //}
                //writer.Close();
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error writing to output file(s): " + e.Message );
                Close();
                throw;
            }
        }
        /// <summary>
        /// Directly accesses the underlying image buffer of the BodyIndexFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the bodyIndexFrameData pointer.
        /// </summary>
        /// <param name="bodyIndexFrameData">Pointer to the BodyIndexFrame image data</param>
        /// <param name="bodyIndexFrameDataSize">Size of the BodyIndexFrame image data</param>
        private unsafe void ProcessBodyIndexFrameData(IntPtr bodyIndexFrameData, uint bodyIndexFrameDataSize)
        {
            byte* frameData = (byte*)bodyIndexFrameData;

            // convert body index to a visual representation
            for (int i = 0; i < (int)bodyIndexFrameDataSize; ++i)
            {
                // the BodyColor array has been sized to match
                // BodyFrameSource.BodyCount
                if (frameData[i] < BodyColor.Length)
                {
                    // this pixel is part of a player,
                    // display the appropriate color
                    bodyIndexPixels[i] = BodyColor[frameData[i]];
                }
                else
                {
                    // this pixel is not part of a player
                    // display black
                    bodyIndexPixels[i] = 0x00000000;
                }
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderBodyIndexPixels()
        {
            bodyIndexBitmap.WritePixels(
                new Int32Rect(0, 0, bodyIndexBitmap.PixelWidth, bodyIndexBitmap.PixelHeight),
                bodyIndexPixels,
                bodyIndexBitmap.PixelWidth * (int)BytesPerPixel,
                0);
        }
    }
}

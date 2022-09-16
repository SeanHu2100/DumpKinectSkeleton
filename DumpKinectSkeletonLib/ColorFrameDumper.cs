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
    public class ColorFrameDumper
    {
        /// <summary>
        /// Array storing latest color frame pixels.
        /// </summary>
        public byte[] _colorFrameBytes;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap colorBitmap = null;

        /// <summary>
        /// Color frames output file stream.
        /// </summary>
        //private Stream _colorOutputStream;

        int width = 1920;
        int height = 1080;

        // create instance of video writer
        VideoFileWriter writer_color = new VideoFileWriter();

        public ColorFrameDumper( KinectSource kinectSource, string colorDataOutputFile )
        {
            // open file for output
            try
            {
                //_colorOutputStream = new BufferedStream(new FileStream(colorDataOutputFile, FileMode.Create));

                // create new video file
                writer_color.Open(colorDataOutputFile, width, height, 30, VideoCodec.MPEG4);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error opening output file: " + e.Message);
                Close();
                throw;
            }
            kinectSource.ColorFrameEvent += HandleColorFrame;

            //if (writer.WriteVideo)
            //{
            //    // Write a video frame
            //    writer.WriteVideoFrame(GetBitmap(bitmap));
            //}

        }

        /// <summary>
        /// Close subjacent output streams
        /// </summary>
        public void Close()
        {
            writer_color?.Close();
            writer_color = null;
        }

        /// <summary>
        /// Handle a ColorFrame. Dumps the frame in raw kinect YUY2 format.
        /// </summary>
        /// <param name="frame"></param>
        public void HandleColorFrame( ColorFrame colorFrame)
        {
            var format = PixelFormats.Bgr32;
            int width = colorFrame.FrameDescription.Width;
            int height = colorFrame.FrameDescription.Height;

            DateTime serverDate = DateTime.Now;
            string currentDateString = string.Format("{0}", serverDate.ToString("yyyyMMddHHmmssffffff"));

            // throw an error is dumper has been closed or output stream could not be opened or written to.
            if (writer_color == null )
            {
                throw new InvalidOperationException( "ColorFrameDumper is closed." );
            }
            // lazy color frame buffer initialization
            if (colorBitmap == null )
            {
                colorBitmap =
                    //new byte[ frame.FrameDescription.LengthInPixels * frame.FrameDescription.BytesPerPixel ];
                    //new byte[width * height * ((format.BitsPerPixel + 7) / 8)];
                new WriteableBitmap(width, height, 96.0, 96.0, format, null);
            }

            //if ( frame.RawColorImageFormat != ColorImageFormat.Bgra)
            //{
            //    frame.CopyConvertedFrameDataToArray( _colorFrameBytes, ColorImageFormat.Bgra);
            //}
            //else
            //{
            //    frame.CopyRawFrameDataToArray( _colorFrameBytes );
            //}
            using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
            {
                colorBitmap.Lock();

                // verify data and write the new color frame data to the display bitmap
                if ((width == colorBitmap.PixelWidth) && (height == colorBitmap.PixelHeight))
                {
                    colorFrame.CopyConvertedFrameDataToIntPtr(
                        colorBitmap.BackBuffer,
                        (uint)(width * height * 4),
                        ColorImageFormat.Bgra);

                    colorBitmap.AddDirtyRect(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight));
                }

                colorBitmap.Unlock();
            }
            try
            {
                //_colorOutputStream.Write( _colorFrameBytes, 0, _colorFrameBytes.Length );
                //int stride = width * format.BitsPerPixel / 8;
                //var colorBitmap = BitmapSource.Create(width, height, 96.0, 96.0, format, null, _colorFrameBytes, stride);
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(colorBitmap));

                //long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                serverDate = DateTime.Now;
                currentDateString = string.Format("{0}", serverDate.ToString("yyyyMMddHHmmssffffff"));
                
                // write the .png file to disk
                FileStream fs = new FileStream("./output_color/" + currentDateString + ".png", FileMode.Create);
                encoder.Save(fs);
                fs.Close();

                // video 
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(colorBitmap));

                MemoryStream outStream = new MemoryStream();
                enc.Save(outStream);
                Bitmap bmp = new Bitmap(outStream);

                writer_color.WriteVideoFrame(bmp);
            }
            catch ( Exception e )
            {
                Console.Error.WriteLine( "Error writing to output file(s): " + e.Message );
                Close();
                throw;
            }
        }        
    }
}

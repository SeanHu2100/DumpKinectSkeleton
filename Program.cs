using System;
using System.Threading;
using System.Windows.Forms;
using CommandLine;
using CommandLine.Text;
using DumpKinectSkeletonLib;
using Microsoft.Kinect;
using Timer = System.Threading.Timer;

namespace DumpKinectSkeleton
{
    internal class Program
    {
        private const string BodyDataOutputFileSuffix = "_body.csv";
        private const string ColorDataOutputFileSuffix = "_color.avi";
        private const string DepthDataOutputFileSuffix = "_depth.avi";
        private const string InfraredDataOutputFileSuffix = "_infrared.avi";
        private const string BodyIndexDataOutputFileSuffix = "_bodyIndex.avi";

        //private static KinectSource _kinectSource;
        private static KinectSource _kinectSource_body;
        private static KinectSource _kinectSource_color;
        private static KinectSource _kinectSource_depth;
        private static KinectSource _kinectSource_infrared;
        private static KinectSource _kinectSource_bodyIndex;

        /// <summary>
        /// Dump body to file.
        /// </summary>
        private static BodyFrameDumper _bodyFrameDumper;

        /// <summary>
        /// Dump body to file.
        /// </summary>
        private static ColorFrameDumper _colorFrameDumper; 

        /// <summary>
        /// Dump body to file.
        /// </summary>
        private static DepthFrameDumper _depthFrameDumper;

        /// <summary>
        /// Dump body to file.
        /// </summary>
        private static InfraredFrameDumper _infraredFrameDumper;

        /// <summary>
        /// Dump body to file.
        /// </summary>
        private static BodyIndexFrameDumper _bodyIndexFrameDumper;

        /// <summary>
        /// Status display timer
        /// </summary>
        private static Timer _timer;

        [Option( 'v', "video", HelpText = "Dump color video stream data as a mp4 video format." )]
        public bool DumpVideo { get; set; }

        [Option( 's', "synchronize", HelpText = "Synchronize streams." )]
        public static bool Synchronize { get; set; }

        [Option( "prefix", DefaultValue = "output", MetaValue = "PREFIX", HelpText = "Output files prefix." )]
        public static string BaseOutputFile { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild( this, current => HelpText.DefaultParsingErrorsHandler( this, current ) );
        }

        public void Run()
        {
            //try
            //{
            //    _kinectSource = new KinectSource();
            //    _kinectSource.FrameSync = Synchronize;
            //    _kinectSource.FrameProcessExceptionEvent += e =>
            //    {
            //        Console.Error.WriteLine( "Error: " + e.Message );
            //        Terminate();
            //    };                
            //}
            //catch ( Exception e )
            //{
            //    Console.Error.WriteLine( "Error initializing Kinect: " + e.Message );
            //    Cleanup();
            //    return;
            //}

            //// initialize dumpers
            //try
            //{
            //    _bodyFrameDumper = new BodyFrameDumper(_kinectSource, BaseOutputFile + BodyDataOutputFileSuffix);
            //    if (DumpVideo)
            //    {
            //        _colorFrameDumper = new ColorFrameDumper(_kinectSource, BaseOutputFile + ColorDataOutputFileSuffix);
            //        _depthFrameDumper = new DepthFrameDumper(_kinectSource, BaseOutputFile + DepthDataOutputFileSuffix);
            //        _infraredFrameDumper = new InfraredFrameDumper(_kinectSource, BaseOutputFile + InfraredDataOutputFileSuffix);
            //        _bodyIndexFrameDumper = new BodyIndexFrameDumper(_kinectSource, BaseOutputFile + BodyIndexDataOutputFileSuffix);
            //    }
            //}
            //catch ( Exception e )
            //{
            //    Console.Error.WriteLine( "Error preparing dumpers: " + e.Message );
            //    Cleanup();
            //    return;
            //}

            //Console.WriteLine( "Starting capture" );
            //Console.WriteLine( $"Ouput skeleton data in file {BaseOutputFile + BodyDataOutputFileSuffix}" );
            //if (DumpVideo)
            //{
            //    Console.WriteLine( $"Color Video stream @{_kinectSource.ColorFrameDescription.Width}x{_kinectSource.ColorFrameDescription.Height} outputed in file {BaseOutputFile + ColorDataOutputFileSuffix}" );
            //    Console.WriteLine($"Depth Video stream @{_kinectSource.DepthFrameDescription.Width}x{_kinectSource.DepthFrameDescription.Height} outputed in file {BaseOutputFile + DepthDataOutputFileSuffix}");
            //    Console.WriteLine($"Infrared Video stream @{_kinectSource.InfraredFrameDescription.Width}x{_kinectSource.InfraredFrameDescription.Height} outputed in file {BaseOutputFile + InfraredDataOutputFileSuffix}");
            //    Console.WriteLine($"BodyIndex Video stream @{_kinectSource.BodyIndexFrameDescription.Width}x{_kinectSource.BodyIndexFrameDescription.Height} outputed in file {BaseOutputFile + BodyIndexDataOutputFileSuffix}");
            //}
            //Console.WriteLine( "Press X, Q or Control + C to stop capture" );
            //Console.WriteLine();

            //Console.WriteLine( "Capture rate(s):" );
            //// write status in console every seconds
            //_timer = new Timer( o =>
            //{
            //    Console.Write($"{_bodyFrameDumper.BodyCount} Skeleton(s) @ {_kinectSource.BodySourceFps:F1} Fps");
            //    if (DumpVideo)
            //    {
            //        Console.Write($" - Color @ {_kinectSource.ColorSourceFps:F1} Fps");
            //        Console.Write($" - Depth @ {_kinectSource.DepthSourceFps:F1} Fps");
            //        Console.Write($" - Infrared @ {_kinectSource.InfraredSourceFps:F1} Fps");
            //        Console.Write($" - BodyIndex @ {_kinectSource.BodyIndexSourceFps:F1} Fps");
            //    }
            //    Console.Write( "\r" );
            //}, null, 1000, 1000 );

            //// start capture
            //_kinectSource.Start();
            
            //// wait for X, Q or Ctrl+C events to exit
            //Console.CancelKeyPress += (sender, args) => Cleanup();
            //while ( true )
            //{
            //    // Start a console read operation. Do not display the input.
            //    var cki = Console.ReadKey( true );

            //    // Exit if the user pressed the 'X', 'Q' or ControlC key. 
            //    if ( cki.Key == ConsoleKey.X || cki.Key == ConsoleKey.Q )
            //    {
            //        break;
            //    }
            //}
            //Cleanup();
        }

        private static void Cleanup()
        {
            Console.WriteLine( Environment.NewLine + $"Stopping capture" );
            Close();
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        private static void Close()
        {
            _timer?.Change( Timeout.Infinite, Timeout.Infinite );

            //if ( _kinectSource != null )
            //{
            //    _kinectSource.Close();
            //    _kinectSource = null;
            //}

            if (_kinectSource_body != null)
            {
                _kinectSource_body.Close();
                _kinectSource_body = null;
            }

            if (_kinectSource_color != null)
            {
                _kinectSource_color.Close();
                _kinectSource_color = null;
            }

            if (_kinectSource_depth != null)
            {
                _kinectSource_depth.Close();
                _kinectSource_depth = null;
            }

            if (_kinectSource_infrared != null)
            {
                _kinectSource_infrared.Close();
                _kinectSource_infrared = null;
            }

            if (_kinectSource_bodyIndex != null)
            {
                _kinectSource_bodyIndex.Close();
                _kinectSource_bodyIndex = null;
            }

            if ( _bodyFrameDumper != null )
            {
                _bodyFrameDumper.Close();
                _bodyFrameDumper = null;
            }

            if ( _colorFrameDumper != null )
            {
                _colorFrameDumper.Close();
                _colorFrameDumper = null;
            }
            if (_depthFrameDumper != null)
            {
                _depthFrameDumper.Close();
                _depthFrameDumper = null;
            }
            if (_infraredFrameDumper != null)
            {
                _infraredFrameDumper.Close();
                _infraredFrameDumper = null;
            }
            if (_bodyIndexFrameDumper != null)
            {
                _bodyIndexFrameDumper.Close();
                _bodyIndexFrameDumper = null;
            }
        }        

        private static void Terminate()
        {
            SendKeys.SendWait( "Q" );
        }

        public static void Main()
        {
            //var main = new Program();
                //main.Run();

            BaseOutputFile = "output";

            Synchronize = false;

            //Creating Threads
            Thread t1 = new Thread(_bodyFrame);
            Thread t2 = new Thread(_colorFrame);
            Thread t3 = new Thread(_depthFrame);
            Thread t4 = new Thread(_infraredFrame);
            Thread t5 = new Thread(_bodyIndexFrame);

            //Executing the methods
            t1.Start();
            t2.Start();
            t3.Start();
            t4.Start();
            t5.Start();

            Console.Read();
        }

        static void _bodyFrame()
        {
            Console.WriteLine("_bodyFrame Thread Started.");

            try
            {
                _kinectSource_body = new KinectSource();
                _kinectSource_body.FrameSync = Synchronize;
                _kinectSource_body.FrameProcessExceptionEvent += e =>
                {
                    Console.Error.WriteLine("Error: " + e.Message);
                    Terminate();
                };
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error initializing Kinect: " + e.Message);
                Cleanup();
                return;
            }

            // initialize dumpers
            try
            {
                _bodyFrameDumper = new BodyFrameDumper(_kinectSource_body, BaseOutputFile + BodyDataOutputFileSuffix);

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error preparing dumpers: " + e.Message);
                Cleanup();
                return;
            }

            Console.WriteLine("Starting capture");
            Console.WriteLine($"Ouput skeleton data in file {BaseOutputFile + BodyDataOutputFileSuffix}");

            Console.WriteLine("Press X, Q or Control + C to stop capture");
            Console.WriteLine();

            Console.WriteLine("Capture rate(s):");
            // write status in console every seconds
            _timer = new Timer(o =>
            {
                Console.Write($"{_bodyFrameDumper.BodyCount} Skeleton(s) @ {_kinectSource_body.BodySourceFps:F1} Fps");

                Console.Write("\r");
            }, null, 1000, 1000);

            // start capture
            _kinectSource_body.Start();

            // wait for X, Q or Ctrl+C events to exit
            Console.CancelKeyPress += (sender, args) => Cleanup();
            while (true)
            {
                // Start a console read operation. Do not display the input.
                var cki = Console.ReadKey(true);

                // Exit if the user pressed the 'X', 'Q' or ControlC key. 
                if (cki.Key == ConsoleKey.X || cki.Key == ConsoleKey.Q)
                {
                    break;
                }
            }
            Cleanup();

            //Console.WriteLine("Method1 Ended using " + Thread.CurrentThread.Name);
        }

        static void _colorFrame()
        {
            Console.WriteLine("_colorFrame Thread Started.");

            try
            {
                _kinectSource_color = new KinectSource();
                _kinectSource_color.FrameSync = Synchronize;
                _kinectSource_color.FrameProcessExceptionEvent += e =>
                {
                    Console.Error.WriteLine("Error: " + e.Message);
                    Terminate();
                };
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error initializing Kinect: " + e.Message);
                Cleanup();
                return;
            }

            // initialize dumpers
            try
            {
                _colorFrameDumper = new ColorFrameDumper(_kinectSource_color, BaseOutputFile + ColorDataOutputFileSuffix);

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error preparing dumpers: " + e.Message);
                Cleanup();
                return;
            }

            Console.WriteLine("Starting capture");
            Console.WriteLine($"Color Video stream @{_kinectSource_color.ColorFrameDescription.Width}x{_kinectSource_color.ColorFrameDescription.Height} outputed in file {BaseOutputFile + ColorDataOutputFileSuffix}");

            Console.WriteLine("Press X, Q or Control + C to stop capture");
            Console.WriteLine();

            Console.WriteLine("Capture rate(s):");
            // write status in console every seconds
            _timer = new Timer(o =>
            {
                Console.Write($" - ColorFrame @ {_kinectSource_color.ColorSourceFps:F1} Fps");

                Console.Write("\r");
            }, null, 1000, 1000);

            // start capture
            _kinectSource_color.Start();

            // wait for X, Q or Ctrl+C events to exit
            Console.CancelKeyPress += (sender, args) => Cleanup();
            while (true)
            {
                // Start a console read operation. Do not display the input.
                var cki = Console.ReadKey(true);

                // Exit if the user pressed the 'X', 'Q' or ControlC key. 
                if (cki.Key == ConsoleKey.X || cki.Key == ConsoleKey.Q)
                {
                    break;
                }
            }
            Cleanup();

            //Console.WriteLine("Method1 Ended using " + Thread.CurrentThread.Name);
        }

        static void _depthFrame()
        {
            Console.WriteLine("_depthFrame Thread Started.");

            try
            {
                _kinectSource_depth = new KinectSource();
                _kinectSource_depth.FrameSync = Synchronize;
                _kinectSource_depth.FrameProcessExceptionEvent += e =>
                {
                    Console.Error.WriteLine("Error: " + e.Message);
                    Terminate();
                };
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error initializing Kinect: " + e.Message);
                Cleanup();
                return;
            }

            // initialize dumpers
            try
            {
                _depthFrameDumper = new DepthFrameDumper(_kinectSource_depth, BaseOutputFile + DepthDataOutputFileSuffix);

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error preparing dumpers: " + e.Message);
                Cleanup();
                return;
            }

            Console.WriteLine("Starting capture");
            Console.WriteLine($"Depth Video stream @{_kinectSource_depth.DepthFrameDescription.Width}x{_kinectSource_depth.DepthFrameDescription.Height} outputed in file {BaseOutputFile + DepthDataOutputFileSuffix}");

            Console.WriteLine("Press X, Q or Control + C to stop capture");
            Console.WriteLine();

            Console.WriteLine("Capture rate(s):");
            // write status in console every seconds
            _timer = new Timer(o =>
            {
                Console.Write($" - DepthFrame @ {_kinectSource_depth.DepthSourceFps:F1} Fps");

                Console.Write("\r");
            }, null, 1000, 1000);

            // start capture
            _kinectSource_depth.Start();

            // wait for X, Q or Ctrl+C events to exit
            Console.CancelKeyPress += (sender, args) => Cleanup();
            while (true)
            {
                // Start a console read operation. Do not display the input.
                var cki = Console.ReadKey(true);

                // Exit if the user pressed the 'X', 'Q' or ControlC key. 
                if (cki.Key == ConsoleKey.X || cki.Key == ConsoleKey.Q)
                {
                    break;
                }
            }
            Cleanup();

            //Console.WriteLine("Method1 Ended using " + Thread.CurrentThread.Name);
        }

        static void _infraredFrame()
        {
            Console.WriteLine("_infraredFrame Thread Started.");

            try
            {
                _kinectSource_infrared = new KinectSource();
                _kinectSource_infrared.FrameSync = Synchronize;
                _kinectSource_infrared.FrameProcessExceptionEvent += e =>
                {
                    Console.Error.WriteLine("Error: " + e.Message);
                    Terminate();
                };
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error initializing Kinect: " + e.Message);
                Cleanup();
                return;
            }

            // initialize dumpers
            try
            {
                _infraredFrameDumper = new InfraredFrameDumper(_kinectSource_infrared, BaseOutputFile + InfraredDataOutputFileSuffix);

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error preparing dumpers: " + e.Message);
                Cleanup();
                return;
            }

            Console.WriteLine("Starting capture");
            Console.WriteLine($"Infrared Video stream @{_kinectSource_infrared.InfraredFrameDescription.Width}x{_kinectSource_infrared.InfraredFrameDescription.Height} outputed in file {BaseOutputFile + InfraredDataOutputFileSuffix}");

            Console.WriteLine("Press X, Q or Control + C to stop capture");
            Console.WriteLine();

            Console.WriteLine("Capture rate(s):");
            // write status in console every seconds
            _timer = new Timer(o =>
            {
                Console.Write($" - InfraredFrame @ {_kinectSource_infrared.InfraredSourceFps:F1} Fps");

                Console.Write("\r");
            }, null, 1000, 1000);

            // start capture
            _kinectSource_infrared.Start();

            // wait for X, Q or Ctrl+C events to exit
            Console.CancelKeyPress += (sender, args) => Cleanup();
            while (true)
            {
                // Start a console read operation. Do not display the input.
                var cki = Console.ReadKey(true);

                // Exit if the user pressed the 'X', 'Q' or ControlC key. 
                if (cki.Key == ConsoleKey.X || cki.Key == ConsoleKey.Q)
                {
                    break;
                }
            }
            Cleanup();

            //Console.WriteLine("Method1 Ended using " + Thread.CurrentThread.Name);
        }

        static void _bodyIndexFrame()
        {
            Console.WriteLine("_bodyIndexFrame Thread Started.");

            try
            {
                _kinectSource_bodyIndex = new KinectSource();
                _kinectSource_bodyIndex.FrameSync = Synchronize;
                _kinectSource_bodyIndex.FrameProcessExceptionEvent += e =>
                {
                    Console.Error.WriteLine("Error: " + e.Message);
                    Terminate();
                };
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error initializing Kinect: " + e.Message);
                Cleanup();
                return;
            }

            // initialize dumpers
            try
            {
                _bodyIndexFrameDumper = new BodyIndexFrameDumper(_kinectSource_bodyIndex, BaseOutputFile + BodyIndexDataOutputFileSuffix);

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error preparing dumpers: " + e.Message);
                Cleanup();
                return;
            }

            Console.WriteLine("Starting capture");
            Console.WriteLine($"BodyIndex Video stream @{_kinectSource_bodyIndex.BodyIndexFrameDescription.Width}x{_kinectSource_bodyIndex.BodyIndexFrameDescription.Height} outputed in file {BaseOutputFile + BodyIndexDataOutputFileSuffix}");

            Console.WriteLine("Press X, Q or Control + C to stop capture");
            Console.WriteLine();

            Console.WriteLine("Capture rate(s):");
            // write status in console every seconds
            _timer = new Timer(o =>
            {
                Console.Write($" - BodyIndexFrame @ {_kinectSource_bodyIndex.BodyIndexSourceFps:F1} Fps");

                Console.Write("\r");
            }, null, 1000, 1000);

            // start capture
            _kinectSource_bodyIndex.Start();

            // wait for X, Q or Ctrl+C events to exit
            Console.CancelKeyPress += (sender, args) => Cleanup();
            while (true)
            {
                // Start a console read operation. Do not display the input.
                var cki = Console.ReadKey(true);

                // Exit if the user pressed the 'X', 'Q' or ControlC key. 
                if (cki.Key == ConsoleKey.X || cki.Key == ConsoleKey.Q)
                {
                    break;
                }
            }
            Cleanup();

            //Console.WriteLine("Method1 Ended using " + Thread.CurrentThread.Name);
        }

    }
}

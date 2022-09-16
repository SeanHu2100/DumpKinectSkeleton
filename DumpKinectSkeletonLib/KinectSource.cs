using System;
using Microsoft.Kinect;

namespace DumpKinectSkeletonLib
{
    public class KinectSource
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private readonly KinectSensor _kinectSensor;

        /// <summary>
        /// Reader for multi sources sync'ed frames
        /// </summary>
        private MultiSourceFrameReader _multiFrameReader;

        /// <summary>
        /// Reader for non sync'ed body frames
        /// </summary>
        private BodyFrameReader _bodyFrameReader;

        /// <summary>
        /// Reader for non sync'ed color frames
        /// </summary>
        private ColorFrameReader _colorFrameReader;

        /// <summary>
        /// Reader for non sync'ed depth frames
        /// </summary>
        private DepthFrameReader _depthFrameReader;

        /// <summary>
        /// Reader for non sync'ed infrared frames
        /// </summary>
        private InfraredFrameReader _infraredFrameReader;

        /// <summary>
        /// Reader for non sync'ed bodyIndex frames
        /// </summary>
        private BodyIndexFrameReader _bodyIndexFrameReader;

        public bool FrameSync { get; set; }

        /// <summary>
        /// Compute body stream frame rate.
        /// </summary>
        private readonly FpsWatch _bodySourceFpsWatcher = new FpsWatch( 1 );

        /// <summary>
        /// Get the skeleton stream frame rate.
        /// </summary>
        public double BodySourceFps
        {
            get { return _bodySourceFpsWatcher.Value; }
        }

        /// <summary>
        /// Compute color stream frame rate.
        /// </summary>
        private readonly FpsWatch _colorSourceFpsWatcher = new FpsWatch( 1 );

        /// <summary>
        /// Get the color stream frame rate.
        /// </summary>
        public double ColorSourceFps
        {
            get { return _colorSourceFpsWatcher.Value; }
        }

        /// <summary>
        /// Compute depth stream frame rate.
        /// </summary>
        private readonly FpsWatch _depthSourceFpsWatcher = new FpsWatch(1);

        /// <summary>
        /// Get the depth stream frame rate.
        /// </summary>
        public double DepthSourceFps
        {
            get { return _depthSourceFpsWatcher.Value; }
        }

        /// <summary>
        /// Compute infrared stream frame rate.
        /// </summary>
        private readonly FpsWatch _infraredSourceFpsWatcher = new FpsWatch(1);

        /// <summary>
        /// Get the infrared stream frame rate.
        /// </summary>
        public double InfraredSourceFps
        {
            get { return _infraredSourceFpsWatcher.Value; }
        }

        /// <summary>
        /// Compute bodyIndex stream frame rate.
        /// </summary>
        private readonly FpsWatch _bodyIndexSourceFpsWatcher = new FpsWatch(1);

        /// <summary>
        /// Get the bodyIndex stream frame rate.
        /// </summary>
        public double BodyIndexSourceFps
        {
            get { return _bodyIndexSourceFpsWatcher.Value; }
        }

        public FrameDescription ColorFrameDescription
        {
            get { return _kinectSensor.ColorFrameSource.FrameDescription; }
        }

        public FrameDescription DepthFrameDescription
        {
            get { return _kinectSensor.DepthFrameSource.FrameDescription; }
        }

        public FrameDescription InfraredFrameDescription
        {
            get { return _kinectSensor.InfraredFrameSource.FrameDescription; }
        }

        public FrameDescription BodyIndexFrameDescription
        {
            get { return _kinectSensor.BodyIndexFrameSource.FrameDescription; }
        }

        /// <summary>
        /// First Frame RelativeTime event handler delegate.
        /// </summary>
        public delegate void FirstFrameRelativeTimeEventHandler( TimeSpan firstRelativeTime );

        private bool _firstFrameRelativeTimeEventFired;
        private bool _kinectUsedExternally = false;
        public event FirstFrameRelativeTimeEventHandler FirstFrameRelativeTimeEvent;

        /// <summary>
        /// Frame processing exception event handler delegate.
        /// </summary>
        /// <param name="exception"></param>
        public delegate void FrameProcessExceptionEventHandler( Exception exception );

        /// <summary>
        /// Event fired when an exception occured during frames processing.
        /// </summary>
        public event FrameProcessExceptionEventHandler FrameProcessExceptionEvent;

        /// <summary>
        /// Body frame processor event handler
        /// </summary>
        /// <param name="frame"></param>
        public delegate void BodyFrameEventHandler( BodyFrame frame );

        /// <summary>
        /// Event fired when a body frame is captured.
        /// </summary>
        public event BodyFrameEventHandler BodyFrameEvent;

        /// <summary>
        /// Color frame processor event handler
        /// </summary>
        /// <param name="frame"></param>
        public delegate void ColorFrameEventHandler( ColorFrame frame );

        /// <summary>
        /// Event fired when a color frame is captured.
        /// </summary>
        public event ColorFrameEventHandler ColorFrameEvent;

        /// <summary>
        /// Depth frame processor event handler
        /// </summary>
        /// <param name="frame"></param>
        public delegate void DepthFrameEventHandler(DepthFrame frame);

        /// <summary>
        /// Event fired when a depth frame is captured.
        /// </summary>
        public event DepthFrameEventHandler DepthFrameEvent;

        /// <summary>
        /// Infrared frame processor event handler
        /// </summary>
        /// <param name="frame"></param>
        public delegate void InfraredFrameEventHandler(InfraredFrame frame);

        /// <summary>
        /// Event fired when a infrared frame is captured.
        /// </summary>
        public event InfraredFrameEventHandler InfraredFrameEvent;

        /// <summary>
        /// BodyIndex frame processor event handler
        /// </summary>
        /// <param name="frame"></param>
        public delegate void BodyIndexFrameEventHandler(BodyIndexFrame frame);

        /// <summary>
        /// Event fired when a bodyIndex frame is captured.
        /// </summary>
        public event BodyIndexFrameEventHandler BodyIndexFrameEvent;

        /// <summary>
        /// Create a new kinect source, initialize Kinect sensor with one multi frame reader.
        /// </summary>
        public KinectSource()
        {
            _kinectSensor = KinectSensor.GetDefault();
            CheckSensor();
        }

        public KinectSource(KinectSensor sensor)
        {
            _kinectSensor = sensor;
            _kinectUsedExternally = true;
            CheckSensor();
        }

        private void CheckSensor()
        {
            if (_kinectSensor == null)
            {
                Close();
                throw new ApplicationException("Error getting Kinect Sensor.");
            }
        }
        
        private void OnFirstFrameRelativeTimeEvent( TimeSpan firstRelativeTime )
        {
            if ( FirstFrameRelativeTimeEvent != null )
            {
                FirstFrameRelativeTimeEvent( firstRelativeTime );
                _firstFrameRelativeTimeEventFired = true;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the multi frame reader.
        /// Dispatch each frames to corresponding processors and Dispose them.
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="evt">event arguments</param>
        private void MultiFrameArrived( object sender, MultiSourceFrameArrivedEventArgs evt )
        {
            var frame = evt.FrameReference.AcquireFrame();
            if ( frame == null ) return;

            try
            {
                if ( BodyFrameEvent != null )
                {
                    using ( var bodyFrame = frame.BodyFrameReference.AcquireFrame() )
                    {
                        if ( !_firstFrameRelativeTimeEventFired )
                        {
                            OnFirstFrameRelativeTimeEvent( bodyFrame.RelativeTime );
                        }
                        BodyFrameEvent( bodyFrame );
                    }
                }
                _bodySourceFpsWatcher.Tick();
                if ( ColorFrameEvent != null )
                {
                    using ( var colorFrame = frame.ColorFrameReference.AcquireFrame() )
                    {
                        if ( !_firstFrameRelativeTimeEventFired )
                        {
                            OnFirstFrameRelativeTimeEvent( colorFrame.RelativeTime );
                        }
                        ColorFrameEvent( colorFrame );
                    }
                }
                _colorSourceFpsWatcher.Tick();
                if (DepthFrameEvent != null)
                {
                    using (var depthFrame = frame.DepthFrameReference.AcquireFrame())
                    {
                        if (!_firstFrameRelativeTimeEventFired)
                        {
                            OnFirstFrameRelativeTimeEvent(depthFrame.RelativeTime);
                        }
                        DepthFrameEvent( depthFrame );
                    }
                }
                _depthSourceFpsWatcher.Tick();
                if (InfraredFrameEvent != null)
                {
                    using (var infraredFrame = frame.InfraredFrameReference.AcquireFrame())
                    {
                        if (!_firstFrameRelativeTimeEventFired)
                        {
                            OnFirstFrameRelativeTimeEvent(infraredFrame.RelativeTime);
                        }
                        InfraredFrameEvent(infraredFrame);
                    }
                }
                _infraredSourceFpsWatcher.Tick();
                if (BodyIndexFrameEvent != null)
                {
                    using (var bodyIndexFrame = frame.BodyIndexFrameReference.AcquireFrame())
                    {
                        if (!_firstFrameRelativeTimeEventFired)
                        {
                            OnFirstFrameRelativeTimeEvent(bodyIndexFrame.RelativeTime);
                        }
                        BodyIndexFrameEvent(bodyIndexFrame);
                    }
                }
                _bodyIndexSourceFpsWatcher.Tick();
            }
            catch ( Exception e )
            {
                if ( FrameProcessExceptionEvent != null )
                {
                    FrameProcessExceptionEvent( e );
                }
            }
        }

        /// <summary>
        /// Handle body frame arrived from dedicated Color frames reader. 
        /// Send frame to all registered processors and Dispose it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="evt"></param>
        private void BodyFrameArrived( object sender, BodyFrameArrivedEventArgs evt )
        {
            try
            {
                if ( BodyFrameEvent != null )
                {
                    using ( var bodyFrame = evt.FrameReference.AcquireFrame() )
                    {
                        if ( !_firstFrameRelativeTimeEventFired )
                        {
                            OnFirstFrameRelativeTimeEvent( bodyFrame.RelativeTime );
                        }
                        BodyFrameEvent( bodyFrame );
                    }
                }
                _bodySourceFpsWatcher.Tick();
            }
            catch ( Exception e )
            {
                if ( FrameProcessExceptionEvent != null )
                {
                    FrameProcessExceptionEvent( e );
                }
            }
        }

        /// <summary>
        /// Handle color frame arrived from dedicated Color frames reader. 
        /// Send frame to all registered processors and Dispose it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="evt"></param>
        private void ColorFrameArrived( object sender, ColorFrameArrivedEventArgs evt )
        {
            try
            {
                if ( ColorFrameEvent != null )
                {
                    using ( var colorFrame = evt.FrameReference.AcquireFrame() )
                    {
                        if ( !_firstFrameRelativeTimeEventFired )
                        {
                            OnFirstFrameRelativeTimeEvent( colorFrame.RelativeTime );
                        }
                        ColorFrameEvent( colorFrame );
                    }
                }
                _colorSourceFpsWatcher.Tick();
            }
            catch ( Exception e )
            {
                if ( FrameProcessExceptionEvent != null )
                {
                    FrameProcessExceptionEvent( e );
                }
            }
        }

        /// <summary>
        /// Handle depth frame arrived from dedicated Depth frames reader. 
        /// Send frame to all registered processors and Dispose it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="evt"></param>
        private void DepthFrameArrived(object sender, DepthFrameArrivedEventArgs evt)
        {
            try
            {
                if (DepthFrameEvent != null)
                {
                    using (var depthFrame = evt.FrameReference.AcquireFrame())
                    {
                        if (!_firstFrameRelativeTimeEventFired)
                        {
                            OnFirstFrameRelativeTimeEvent(depthFrame.RelativeTime);
                        }
                        DepthFrameEvent(depthFrame);
                    }
                }
                _depthSourceFpsWatcher.Tick();
            }
            catch (Exception e)
            {
                if (FrameProcessExceptionEvent != null)
                {
                    FrameProcessExceptionEvent(e);
                }
            }
        }

        /// <summary>
        /// Handle infrared frame arrived from dedicated Infrared frames reader. 
        /// Send frame to all registered processors and Dispose it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="evt"></param>
        private void InfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs evt)
        {
            try
            {
                if (InfraredFrameEvent != null)
                {
                    using (var infraredFrame = evt.FrameReference.AcquireFrame())
                    {
                        if (!_firstFrameRelativeTimeEventFired)
                        {
                            OnFirstFrameRelativeTimeEvent(infraredFrame.RelativeTime);
                        }
                        InfraredFrameEvent(infraredFrame);
                    }
                }
                _infraredSourceFpsWatcher.Tick();
            }
            catch (Exception e)
            {
                if (FrameProcessExceptionEvent != null)
                {
                    FrameProcessExceptionEvent(e);
                }
            }
        }

        /// <summary>
        /// Handle bodyIndex frame arrived from dedicated BodyIndex frames reader. 
        /// Send frame to all registered processors and Dispose it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="evt"></param>
        private void BodyIndexFrameArrived(object sender, BodyIndexFrameArrivedEventArgs evt)
        {
            try
            {
                if (BodyIndexFrameEvent != null)
                {
                    using (var bodyIndexFrame = evt.FrameReference.AcquireFrame())
                    {
                        if (!_firstFrameRelativeTimeEventFired)
                        {
                            OnFirstFrameRelativeTimeEvent(bodyIndexFrame.RelativeTime);
                        }
                        BodyIndexFrameEvent(bodyIndexFrame);
                    }
                }
                _bodyIndexSourceFpsWatcher.Tick();
            }
            catch (Exception e)
            {
                if (FrameProcessExceptionEvent != null)
                {
                    FrameProcessExceptionEvent(e);
                }
            }
        }

        /// <summary>
        /// Start capture.
        /// </summary>
        public void Start()
        {
            if (FrameSync)
            {
                // open streams using a synchronized readers, the frame rate is equal to the lowest of each separated stream.
                // select which streams to enable
                var features = FrameSourceTypes.None;
                if ( BodyFrameEvent != null )
                {
                    features |= FrameSourceTypes.Body;
                }
                if ( ColorFrameEvent != null )
                {
                    features |= FrameSourceTypes.Color;
                }
                if (DepthFrameEvent != null)
                {
                    features |= FrameSourceTypes.Depth;
                }
                if (InfraredFrameEvent != null)
                {
                    features |= FrameSourceTypes.Infrared;
                }
                if (BodyIndexFrameEvent != null)
                {
                    features |= FrameSourceTypes.BodyIndex;
                }
                if ( features == FrameSourceTypes.None )
                {
                    throw new ApplicationException( "No event processor registered." );
                }
                // check reader state
                if ( _multiFrameReader != null )
                {
                    throw new InvalidOperationException( "Kinect already started." );
                }
                // open the reader
                _multiFrameReader = _kinectSensor.OpenMultiSourceFrameReader( features );
                if ( _multiFrameReader == null )
                {
                    Close();
                    throw new ApplicationException( "Error opening readers." );
                }

                // register to frames
                _multiFrameReader.MultiSourceFrameArrived += MultiFrameArrived;
            }
            else
            {
                // open streams using separate readers, each one with the highest frame rate possible.
                // open body reader
                if ( BodyFrameEvent != null )
                {
                    if ( _bodyFrameReader != null )
                    {
                        throw new InvalidOperationException( "Kinect already started." );
                    }
                    _bodyFrameReader = _kinectSensor.BodyFrameSource.OpenReader();
                    if ( _bodyFrameReader == null )
                    {
                        Close();
                        throw new ApplicationException( "Error opening readers." );
                    }
                    _bodyFrameReader.FrameArrived += BodyFrameArrived;
                }
                // open color stream reader
                if ( ColorFrameEvent != null )
                {
                    if ( _colorFrameReader != null )
                    {
                        throw new InvalidOperationException( "Kinect already started." );
                    }
                    _colorFrameReader = _kinectSensor.ColorFrameSource.OpenReader();
                    if ( _colorFrameReader == null )
                    {
                        Close();
                        throw new ApplicationException( "Error opening readers." );
                    }
                    _colorFrameReader.FrameArrived += ColorFrameArrived;
                }
                // open depth stream reader
                if (DepthFrameEvent != null)
                {
                    if ( _depthFrameReader != null)
                    {
                        throw new InvalidOperationException("Kinect already started.");
                    }
                    _depthFrameReader = _kinectSensor.DepthFrameSource.OpenReader();
                    if ( _depthFrameReader == null)
                    {
                        Close();
                        throw new ApplicationException("Error opening readers.");
                    }
                    _depthFrameReader.FrameArrived += DepthFrameArrived;
                }
                // open infrared stream reader
                if (InfraredFrameEvent != null)
                {
                    if (_infraredFrameReader != null)
                    {
                        throw new InvalidOperationException("Kinect already started.");
                    }
                    _infraredFrameReader = _kinectSensor.InfraredFrameSource.OpenReader();
                    if (_infraredFrameReader == null)
                    {
                        Close();
                        throw new ApplicationException("Error opening readers.");
                    }
                    _infraredFrameReader.FrameArrived += InfraredFrameArrived;
                }
                // open bodyIndex stream reader
                if (BodyIndexFrameEvent != null)
                {
                    if (_bodyIndexFrameReader != null)
                    {
                        throw new InvalidOperationException("Kinect already started.");
                    }
                    _bodyIndexFrameReader = _kinectSensor.BodyIndexFrameSource.OpenReader();
                    if (_bodyIndexFrameReader == null)
                    {
                        Close();
                        throw new ApplicationException("Error opening readers.");
                    }
                    _bodyIndexFrameReader.FrameArrived += BodyIndexFrameArrived;
                }
            }
            _firstFrameRelativeTimeEventFired = false;
            _kinectSensor.Open();
        }

        /// <summary>
        /// Close opened reader and sensors.
        /// </summary>
        public void Close()
        {
            if ( _multiFrameReader != null )
            {
                _multiFrameReader.MultiSourceFrameArrived -= MultiFrameArrived;
                _multiFrameReader.Dispose();
                _multiFrameReader = null;
            }

            if (_bodyFrameReader != null)
            {
                _bodyFrameReader.FrameArrived -= BodyFrameArrived;
                _bodyFrameReader.Dispose();
                _bodyFrameReader = null;
            }
            if ( _colorFrameReader != null )
            {
                _colorFrameReader.FrameArrived -= ColorFrameArrived;
                _colorFrameReader.Dispose();
                _colorFrameReader = null;
            }

            if (_depthFrameReader != null)
            {
                _depthFrameReader.FrameArrived -= DepthFrameArrived;
                _depthFrameReader.Dispose();
                _depthFrameReader = null;
            }

            if (_infraredFrameReader != null)
            {
                _infraredFrameReader.FrameArrived -= InfraredFrameArrived;
                _infraredFrameReader.Dispose();
                _infraredFrameReader = null;
            }
            if (_bodyIndexFrameReader != null)
            {
                _bodyIndexFrameReader.FrameArrived -= BodyIndexFrameArrived;
                _bodyIndexFrameReader.Dispose();
                _bodyIndexFrameReader = null;
            }

            if (!_kinectUsedExternally)
            {
                _kinectSensor?.Close();
            }
        }
    }
}

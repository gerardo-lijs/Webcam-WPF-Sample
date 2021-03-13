using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace Webcam_WPF_Sample
{
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
#pragma warning disable CS0067 // Event used by Fody
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067 // Event used by Fody

        private CancellationTokenSource? _cts;
        private readonly System.Diagnostics.Stopwatch _fpsStopWatch = new System.Diagnostics.Stopwatch();

        /// <summary>
        /// Frame rate used to display video.
        /// </summary>
        private const int MaxDisplayFrameRate = 30;

        public bool FlipImageY { get; set; }
        public bool FlipImageX { get; set; }
        public bool ApplyFilter { get; set; }
        public int CurrentFPS { get; private set; }

        public List<Helpers.CameraDevicesEnumerator.CameraDevice> CameraDevices { get; }
        public Helpers.CameraDevicesEnumerator.CameraDevice? CameraDeviceSelected { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            CameraDevices = Helpers.CameraDevicesEnumerator.GetAllConnectedCameras().ToList();
            CameraDeviceSelected = CameraDevices.FirstOrDefault();
        }

        private async void WebcamStartButton_Click(object sender, RoutedEventArgs e)
        {
            WebcamStartButton.IsEnabled = false;
            WebcamStopButton.IsEnabled = true;

            try
            {
                _cts = new CancellationTokenSource();
                await Task.Run(() => Start_CameraGrab(_cts.Token), _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancel exception
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }

            WebcamStartButton.IsEnabled = true;
            WebcamStopButton.IsEnabled = false;
        }

        private void WebcamStopButton_Click(object sender, RoutedEventArgs e) => WebcamStop();

        private void WebcamStop()
        {
            try
            {
                // Cancel and dispose
                _cts?.Cancel();

                // Refresh UI buttons
                WebcamStopButton.IsEnabled = false;
                WebcamStartButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private async Task Start_CameraGrab(CancellationToken cancellationToken)
        {
            // Check
            if (CameraDeviceSelected is null) throw new Exception("Camera device not selected.");

            // FPS delay
            var fpsMilliseconds = 1000 / MaxDisplayFrameRate;

            // Init capture
            var videoCapture = new VideoCapture(CameraDeviceSelected.CameraIndex);
            videoCapture.Open(CameraDeviceSelected.CameraIndex);
            if (!videoCapture.IsOpened()) throw new Exception("Could not open camera.");

            var fpsCounter = new List<int>();

            // Grab
            using var frame = new Mat();
            while (!cancellationToken.IsCancellationRequested)
            {
                // Reduce the number of displayed images to a reasonable amount if the camera is acquiring images very fast.
                if (!_fpsStopWatch.IsRunning || _fpsStopWatch.ElapsedMilliseconds > fpsMilliseconds)
                {
                    // Display FPS counter
                    if (_fpsStopWatch.IsRunning)
                    {
                        fpsCounter.Add((int)Math.Ceiling((double)1000 / (int)_fpsStopWatch.ElapsedMilliseconds));
                        if (fpsCounter.Count > MaxDisplayFrameRate / 2)
                        {
                            CurrentFPS = (int)Math.Ceiling(fpsCounter.Average());
                            fpsCounter.Clear();
                        }
                    }

                    _fpsStopWatch.Restart();

                    // Get frame
                    videoCapture.Read(frame);

                    if (!frame.Empty())
                    {
                        // Optional flip
                        Mat workFrame = FlipImageY ? frame.Flip(FlipMode.Y) : frame;
                        workFrame = FlipImageX ? workFrame.Flip(FlipMode.X) : workFrame;

                        if (ApplyFilter)
                        {
                            workFrame = Filter_Canny(workFrame);
                        }

                        // Update frame in UI thread
                        await Dispatcher.InvokeAsync(() =>
                        {
                            WebcamImage.Source = workFrame.ToBitmapSource();
                        });
                    }
                }
                else
                {
                    // Display frame rate speed to get desired display frame rate. We use half the expected time to consider time spent executing other code
                    var fpsDelay = (fpsMilliseconds / 2) - (int)_fpsStopWatch.ElapsedMilliseconds;
                    if (fpsDelay > 0) await Task.Delay(fpsDelay, CancellationToken.None);
                }
            }

            videoCapture.Release();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WebcamStop();
        }

        private static Mat Filter_Canny(Mat image)
        {
            return image.Canny(100, 200);
        }

        private static void ShowException(Exception ex)
        {
            MessageBox.Show(ex.Message, "Webcam WPF Sample Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

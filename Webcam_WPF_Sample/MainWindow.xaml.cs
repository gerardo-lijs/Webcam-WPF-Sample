using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        private System.Diagnostics.Stopwatch _fpsStopWatch = new System.Diagnostics.Stopwatch();

        /// <summary>
        /// For multiples cameras specify index here.
        /// </summary>
        private const int CameraIndex = 0;

        /// <summary>
        /// Frame rate used to display video.
        /// </summary>
        private const int MaxDisplayFrameRate = 30;

        public bool FlipImage { get; set; }
        public bool ApplyFilter { get; set; }
        public int CurrentFPS { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Camera exception", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(ex.ToString(), "Camera exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task Start_CameraGrab(CancellationToken cancellationToken)
        {
            // FPS delay
            var fpsMilliseconds = 1000 / MaxDisplayFrameRate;

            // Init capture
            var videoCapture = new VideoCapture(CameraIndex);
            videoCapture.Open(CameraIndex);
            if (!videoCapture.IsOpened()) throw new ApplicationException("Could not open camera.");

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
                        Mat workFrame = FlipImage ? frame.Flip(FlipMode.Y) : frame;

                        if (ApplyFilter)
                        {
                            workFrame = Filter_Canny(workFrame);
                        }

                        // Update frame in UI thread
                        await Dispatcher.InvokeAsync(() =>
                        {
                            WebcamImage.Source = workFrame.ToWriteableBitmap();
                        });
                    }
                }
                else
                {
                    // Display frame rate speed to get desired display frame rate
                    var fpsDelay = fpsMilliseconds - (int)_fpsStopWatch.ElapsedMilliseconds;

                    // We delay half the expected time to consider time spent executing other code
                    if (fpsDelay > 0) await Task.Delay(fpsDelay /2);
                }
            }

            videoCapture.Release();
        }

        private Mat Filter_Canny(Mat image)
        {
            return image.Canny(100, 200);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WebcamStop();
        }
    }
}

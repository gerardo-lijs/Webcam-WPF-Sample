using System;
using System.Collections.Generic;
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
    public partial class MainWindow : System.Windows.Window
    {
        private CancellationTokenSource? _cts;

        // TODO: This should be implemented as binding with MVVM but I wanted to keep the sample simple
        private bool FlipImage { get; set; }

        /// <summary>
        /// For multiples cameras specify index here.
        /// </summary>
        private const int CameraIndex = 0;

        /// <summary>
        /// Frame rate used to grab video.
        /// Value 0 means disabled = max frame rate possible
        /// </summary>
        private const int DisplayFrameRate = 30;

        public MainWindow()
        {
            InitializeComponent();
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

        private void WebcamStopButton_Click(object sender, RoutedEventArgs e)
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
            var videoCapture = new VideoCapture(CameraIndex);
            videoCapture.Open(CameraIndex);
            if (!videoCapture.IsOpened()) throw new ApplicationException("Could not open camera.");

            using var frame = new Mat();
            while (!cancellationToken.IsCancellationRequested)
            {
                // Get frame
                videoCapture.Read(frame);

                if (!frame.Empty())
                {
                    // Optional flip
                    var workFrame = FlipImage ? frame.Flip(FlipMode.Y) : frame;

                    // Update frame in UI thread
                    WebcamImage.Dispatcher.Invoke(() =>
                    {
                        WebcamImage.Source = workFrame.ToWriteableBitmap();
                    });
                }

                // Display frame rate speed to get 30 fps
                await Task.Delay(33);
            }

            videoCapture.Release();
        }

        // TODO: This should be implemented as binding with MVVM but I wanted to keep the sample simple
        private void FlipImageCheckBox_Checked(object sender, RoutedEventArgs e) => FlipImage = true;
        private void FlipImageCheckBox_Unchecked(object sender, RoutedEventArgs e) => FlipImage = false;
    }
}

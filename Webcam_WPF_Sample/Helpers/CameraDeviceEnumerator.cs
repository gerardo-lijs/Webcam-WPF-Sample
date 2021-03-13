using DirectShowLib;
using System.Collections.Generic;
using System.Linq;

namespace Webcam_WPF_Sample.Helpers
{
    // Based on: https://github.com/FrancescoBonizzi/WebcamControl-WPF-With-OpenCV/blob/master/WebcamWithOpenCV/WebcamWithOpenCV/CameraDevicesEnumerator.cs

    /// <summary>
    /// Enumerates the cameras with the same order as OpenCv does.
    /// With OpenCv you cannot connect to a camera by name, you have to use an index.
    /// The index in OpenCv is based on the connection order to the computer. Neither WMI gives you that information,
    /// so I had to reference DirectShowLib which, luckly, does the same as OpenCv. (As far as I know!)
    /// </summary>
    public static class CameraDevicesEnumerator
    {
        public record CameraDevice(int OpenCvId, string Name, string DeviceId);

        public static IEnumerable<CameraDevice> GetAllConnectedCameras()
        {
            var videoInputDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            var openCvId = 0;
            return videoInputDevices.Select(x => new CameraDevice(openCvId++, x.Name, x.DevicePath));
        }
    }
}

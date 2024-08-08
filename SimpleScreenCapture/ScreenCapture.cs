using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibSimpleScreenCapture.Windows;

namespace LibSimpleScreenCapture
{
    public static class ScreenCapture
    {
        public static bool DoScreenCapture()
        {
            var screenCaptureWindow = new ScreenCaptureWindow();
            screenCaptureWindow.ShowDialog();

            return screenCaptureWindow.DialogResult ?? false;
        }
    }
}

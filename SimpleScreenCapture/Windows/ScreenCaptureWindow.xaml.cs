using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using LibScreenCapture;
using SkiaSharp;

namespace LibSimpleScreenCapture.Windows
{
    /// <summary>
    /// Interaction logic for ScreenCaptureWindow.xaml
    /// </summary>
    [ObservableObject]
    public partial class ScreenCaptureWindow : Window
    {
        private readonly Stream _destination;
        IScreenCapture _screenCapture;

        private Action<System.Windows.Controls.Primitives.Popup> repositionPopupAction = (Action<System.Windows.Controls.Primitives.Popup>)typeof(System.Windows.Controls.Primitives.Popup)
            .GetMethod("Reposition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .CreateDelegate(typeof(Action<System.Windows.Controls.Primitives.Popup>));

        public WriteableBitmap ScreenBitmap { get; }

        public ScreenCaptureWindow(System.IO.Stream destination)
        {
            DataContext = this;

            _screenCapture = new GdiScreenCapture(0);
            _screenCapture.Capture();

            var writeableBitmap = new WriteableBitmap(_screenCapture.ScreenWidth, _screenCapture.ScreenHeight, _screenCapture.DpiX, _screenCapture.DpiY, PixelFormats.Pbgra32, null);
            writeableBitmap.WritePixels(new Int32Rect(0, 0, _screenCapture.ScreenWidth, _screenCapture.ScreenHeight), _screenCapture.DataPointer, _screenCapture.Stride * _screenCapture.ScreenHeight, _screenCapture.Stride);

            ScreenBitmap = writeableBitmap;
            //ScreenBitmap.Freeze();
            _destination = destination;

            InitializeComponent();
        }


        private Point _mouseStartPosition;

        [ObservableProperty]
        private bool _areaSelecting;

        [ObservableProperty]
        private bool _areaSelected;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AreaRight))]
        private double _areaLeft;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AreaBottom))]
        private double _areaTop;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AreaRight))]
        private double _areaWidth;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AreaBottom))]
        private double _areaHeight;

        [ObservableProperty]
        private double _mouseX;

        [ObservableProperty]
        private double _mouseY;

        public double AreaRight => (rootCanvas?.ActualWidth ?? 0) - AreaLeft - AreaWidth;

        public double AreaBottom => (rootCanvas?.ActualHeight ?? 0) - AreaTop - AreaHeight;





        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void AreaMouseDown(object sender, EventArgs e)
        {
            var mousePosition = Mouse.GetPosition(rootCanvas);
            _mouseStartPosition = mousePosition;

            AreaLeft = mousePosition.X;
            AreaTop = mousePosition.Y;
            AreaWidth = Math.Max(mousePosition.X, _mouseStartPosition.X) - AreaLeft;
            AreaHeight = Math.Max(mousePosition.Y, _mouseStartPosition.Y) - AreaTop;
            AreaSelecting = true;
        }

        private void AreaMouseMove(object sender, EventArgs e)
        {
            var mousePosition = Mouse.GetPosition(rootCanvas);

            MouseX = mousePosition.X;
            MouseY = mousePosition.Y;

            if (AreaSelecting)
            {
                AreaLeft = Math.Min(mousePosition.X, _mouseStartPosition.X);
                AreaTop = Math.Min(mousePosition.Y, _mouseStartPosition.Y);
                AreaWidth = Math.Max(mousePosition.X, _mouseStartPosition.X) - AreaLeft;
                AreaHeight = Math.Max(mousePosition.Y, _mouseStartPosition.Y) - AreaTop;
            }

            repositionPopupAction.Invoke(followMousePopup);
        }

        private void AreaMouseUp(object sender, EventArgs e)
        {
            if (AreaSelecting)
            {
                AreaSelected = true;
                AreaSelecting = false;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Escape)
            {
                DialogResult = false;
                Close();
            }
            else if (e.Key is Key.Enter)
            {
                var normalizedAreaX = AreaLeft / rootCanvas.ActualWidth;
                var normalizedAreaY = AreaTop / rootCanvas.ActualHeight;
                var normalizedAreaWidth = AreaWidth / rootCanvas.ActualWidth;
                var normalizedAreaHeight = AreaHeight / rootCanvas.ActualHeight;

                var pixelAreaX = (int)(ScreenBitmap.PixelWidth * normalizedAreaX);
                var pixelAreaY = (int)(ScreenBitmap.PixelHeight * normalizedAreaY);
                var pixelAreaWidth = (int)(ScreenBitmap.PixelWidth * normalizedAreaWidth);
                var pixelAreaHeight = (int)(ScreenBitmap.PixelHeight * normalizedAreaHeight);

                using var screenImage = SKImage.FromPixels(new SKImageInfo(ScreenBitmap.PixelWidth, ScreenBitmap.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul), ScreenBitmap.BackBuffer, ScreenBitmap.BackBufferStride);
                using var captureBitmap = new SKBitmap(pixelAreaWidth, pixelAreaHeight);
                using var captureCanvas = new SKCanvas(captureBitmap);

                captureCanvas.DrawImage(screenImage, new SKPoint(-pixelAreaX, -pixelAreaY));
                captureBitmap.Encode(_destination, SKEncodedImageFormat.Png, 100);

                DialogResult = true;
                Close();
            }
        }

        private void followMousePopup_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            repositionPopupAction.Invoke(followMousePopup);
        }
    }
}

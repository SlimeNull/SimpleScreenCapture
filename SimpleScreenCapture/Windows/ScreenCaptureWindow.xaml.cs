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


        private Point _areaStartPosition;

        [ObservableProperty]
        private bool _areaSelected;

        [ObservableProperty]
        private AreaSelectionMode _selectionMode;

        [ObservableProperty]
        private string _overlayText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AreaRight))]
        [NotifyPropertyChangedFor(nameof(ToolbarPlacementTargetLeft))]
        [NotifyPropertyChangedFor(nameof(ToolbarPlacementTargetWidth))]
        private double _areaLeft;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AreaBottom))]
        [NotifyPropertyChangedFor(nameof(ToolbarPlacementTargetTop))]
        [NotifyPropertyChangedFor(nameof(ToolbarPlacementTargetHeight))]
        private double _areaTop;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AreaRight))]
        [NotifyPropertyChangedFor(nameof(ToolbarPlacementTargetLeft))]
        [NotifyPropertyChangedFor(nameof(ToolbarPlacementTargetWidth))]
        private double _areaWidth;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AreaBottom))]
        [NotifyPropertyChangedFor(nameof(ToolbarPlacementTargetTop))]
        [NotifyPropertyChangedFor(nameof(ToolbarPlacementTargetHeight))]
        private double _areaHeight;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MouseCursor))]
        private double _mouseX;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MouseCursor))]
        private double _mouseY;

        public double AreaRight => (rootCanvas?.ActualWidth ?? 0) - AreaLeft - AreaWidth;

        public double AreaBottom => (rootCanvas?.ActualHeight ?? 0) - AreaTop - AreaHeight;

        public double ToolbarPlacementScreenPadding => 35;

        public double ToolbarPlacementTargetLeft => Math.Max(ToolbarPlacementScreenPadding, AreaLeft);
        public double ToolbarPlacementTargetTop => Math.Max(ToolbarPlacementScreenPadding, AreaTop - 5);
        public double ToolbarPlacementTargetWidth => Math.Max(Math.Min(rootCanvas.ActualWidth - ToolbarPlacementScreenPadding, AreaLeft + AreaWidth) - ToolbarPlacementTargetLeft, 0);
        public double ToolbarPlacementTargetHeight => Math.Max(Math.Min(rootCanvas.ActualHeight - ToolbarPlacementScreenPadding, AreaTop + AreaHeight + 10) - ToolbarPlacementTargetTop, 0);

        public Cursor MouseCursor
        {
            get
            {
                var selectionMode = SelectionMode;
                if (selectionMode == AreaSelectionMode.None)
                {
                    selectionMode = GetMouseSelectionMode();
                }

                var cursorByNode = selectionMode switch
                {
                    AreaSelectionMode.None => Cursors.Cross,
                    AreaSelectionMode.ChangeLeft or AreaSelectionMode.ChangeRight => Cursors.SizeWE,
                    AreaSelectionMode.ChangeTop or AreaSelectionMode.ChangeBottom => Cursors.SizeNS,
                    AreaSelectionMode.ChangePosition => Cursors.SizeAll,
                    _ => null,
                };

                if (cursorByNode is { } cursor)
                {
                    return cursor;
                }

                if (selectionMode == (AreaSelectionMode.ChangeLeft | AreaSelectionMode.ChangeTop) ||
                    selectionMode == (AreaSelectionMode.ChangeRight | AreaSelectionMode.ChangeBottom))
                {
                    return Cursors.SizeNWSE;
                }

                if (selectionMode == (AreaSelectionMode.ChangeRight | AreaSelectionMode.ChangeTop) ||
                    selectionMode == (AreaSelectionMode.ChangeLeft | AreaSelectionMode.ChangeBottom))
                {
                    return Cursors.SizeNESW;
                }

                return Cursors.Arrow;
            }
        }

        private AreaSelectionMode GetMouseSelectionMode()
        {
            var mousePosition = Mouse.GetPosition(rootCanvas);

            if (!AreaSelected)
            {
                return AreaSelectionMode.None;
            }

            var selectionMode = AreaSelectionMode.None;
            if (mousePosition.X <= AreaLeft)
            {
                _areaStartPosition.X = AreaLeft + AreaWidth;
                selectionMode |= AreaSelectionMode.ChangeLeft;
            }
            else if (mousePosition.X >= AreaLeft + AreaWidth)
            {
                _areaStartPosition.X = AreaLeft;
                selectionMode |= AreaSelectionMode.ChangeRight;
            }

            if (mousePosition.Y <= AreaTop)
            {
                _areaStartPosition.Y = AreaTop + AreaHeight;
                selectionMode |= AreaSelectionMode.ChangeTop;
            }
            else if (mousePosition.Y >= AreaTop + AreaHeight)
            {
                _areaStartPosition.Y = AreaTop;
                selectionMode |= AreaSelectionMode.ChangeBottom;
            }

            if (selectionMode == AreaSelectionMode.None)
            {
                selectionMode = AreaSelectionMode.ChangePosition;
            }

            return selectionMode;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void AreaMouseDown(object sender, EventArgs e)
        {
            var mousePosition = Mouse.GetPosition(rootCanvas);

            if (!AreaSelected)
            {
                _areaStartPosition = mousePosition;
                SelectionMode = AreaSelectionMode.ChangeX | AreaSelectionMode.ChangeY;
            }
            else
            {
                var selectionMode = AreaSelectionMode.None;

                if (mousePosition.X <= AreaLeft)
                {
                    _areaStartPosition.X = AreaLeft + AreaWidth;
                    selectionMode |= AreaSelectionMode.ChangeLeft;
                }
                else if (mousePosition.X >= AreaLeft + AreaWidth)
                {
                    _areaStartPosition.X = AreaLeft;
                    selectionMode |= AreaSelectionMode.ChangeRight;
                }

                if (mousePosition.Y <= AreaTop)
                {
                    _areaStartPosition.Y = AreaTop + AreaHeight;
                    selectionMode |= AreaSelectionMode.ChangeTop;
                }
                else if (mousePosition.Y >= AreaTop + AreaHeight)
                {
                    _areaStartPosition.Y = AreaTop;
                    selectionMode |= AreaSelectionMode.ChangeBottom;
                }

                if (selectionMode == AreaSelectionMode.None)
                {
                    selectionMode = AreaSelectionMode.ChangePosition;
                }

                SelectionMode = selectionMode;
            }

            if (SelectionMode.HasFlag(AreaSelectionMode.ChangeX))
            {
                AreaLeft = Math.Min(mousePosition.X, _areaStartPosition.X);
                AreaWidth = Math.Max(mousePosition.X, _areaStartPosition.X) - AreaLeft;
            }

            if (SelectionMode.HasFlag(AreaSelectionMode.ChangeY))
            {
                AreaTop = Math.Min(mousePosition.Y, _areaStartPosition.Y);
                AreaHeight = Math.Max(mousePosition.Y, _areaStartPosition.Y) - AreaTop;
            }

            if (SelectionMode != AreaSelectionMode.None)
            {
                rootCanvas.CaptureMouse();
            }
        }

        private void AreaMouseMove(object sender, EventArgs e)
        {
            var mousePosition = Mouse.GetPosition(rootCanvas);

            MouseX = mousePosition.X;
            MouseY = mousePosition.Y;

            if (SelectionMode.HasFlag(AreaSelectionMode.ChangeX))
            {
                AreaLeft = Math.Min(mousePosition.X, _areaStartPosition.X);
                AreaWidth = Math.Max(mousePosition.X, _areaStartPosition.X) - AreaLeft;
            }

            if (SelectionMode.HasFlag(AreaSelectionMode.ChangeY))
            {
                AreaTop = Math.Min(mousePosition.Y, _areaStartPosition.Y);
                AreaHeight = Math.Max(mousePosition.Y, _areaStartPosition.Y) - AreaTop;
            }
        }

        private void AreaMouseUp(object sender, EventArgs e)
        {
            if (SelectionMode != AreaSelectionMode.None)
            {
                SelectionMode = AreaSelectionMode.None;
                AreaSelected = true;
            }

            if (rootCanvas.IsMouseCaptured)
            {
                rootCanvas.ReleaseMouseCapture();
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

        [Flags]
        public enum AreaSelectionMode
        {
            None           = 0b00000000,
            ChangeX        = 0b00000001,
            ChangeLeft     = 0b00000011,
            ChangeRight    = 0b00000101,
            ChangeY        = 0b00001000,
            ChangeTop      = 0b00011000,
            ChangeBottom   = 0b00101000,
            ChangePosition = 0b01000000,
        }
    }
}

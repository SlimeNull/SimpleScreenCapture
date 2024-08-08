using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibScreenCapture;
using Microsoft.Win32;
using SkiaSharp;

namespace LibSimpleScreenCapture.Windows
{
    /// <summary>
    /// Interaction logic for ScreenCaptureWindow.xaml
    /// </summary>
    [ObservableObject]
    public partial class ScreenCaptureWindow : Window
    {
        private readonly double _dpiScale;
        private readonly IScreenCapture _screenCapture;

        private SaveFileDialog? _saveImageDialog;

        private Action<System.Windows.Controls.Primitives.Popup> repositionPopupAction = (Action<System.Windows.Controls.Primitives.Popup>)typeof(System.Windows.Controls.Primitives.Popup)
            .GetMethod("Reposition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .CreateDelegate(typeof(Action<System.Windows.Controls.Primitives.Popup>));

        public WriteableBitmap ScreenBitmap { get; }

        public ScreenCaptureWindow()
        {

            _screenCapture = new GdiScreenCapture(0);
            _screenCapture.Capture();

            var screenBitmap = new WriteableBitmap(_screenCapture.ScreenWidth, _screenCapture.ScreenHeight, _screenCapture.DpiX, _screenCapture.DpiY, PixelFormats.Pbgra32, null);
            screenBitmap.WritePixels(new Int32Rect(0, 0, _screenCapture.ScreenWidth, _screenCapture.ScreenHeight), _screenCapture.DataPointer, _screenCapture.Stride * _screenCapture.ScreenHeight, _screenCapture.Stride);
            screenBitmap.Freeze();

            var primaryScreen = ScreenInfo.GetScreen(0);

            _dpiScale = primaryScreen.DpiY / 96.0;

            ToolbarPopupPlacementCallback = (popupSize, targetSize, offset) =>
            {
                var point = new Point(targetSize.Width - popupSize.Width, targetSize.Height + 5);

                if (point.Y / _dpiScale + AreaTop + popupSize.Height > rootCanvas.ActualHeight)
                {
                    point.Y = -popupSize.Height - 5;

                    if (point.Y / _dpiScale + AreaTop < 0)
                    {
                        point.Y = targetSize.Height - popupSize.Height - 5;
                    }
                }

                return [new CustomPopupPlacement(point, PopupPrimaryAxis.None)];
            };

            DataContext = this;
            ScreenBitmap = screenBitmap;

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
        [NotifyPropertyChangedFor(nameof(MouseCursor))]
        private double _mouseX;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MouseCursor))]
        private double _mouseY;

        public double AreaRight => (rootCanvas?.ActualWidth ?? 0) - AreaLeft - AreaWidth;

        public double AreaBottom => (rootCanvas?.ActualHeight ?? 0) - AreaTop - AreaHeight;

        public double ToolbarPlacementScreenPadding => 35;

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

                if (selectionMode == (AreaSelectionMode.ChangeX | AreaSelectionMode.ChangeY))
                {
                    return Cursors.Cross;
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

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var presentationSource = (HwndSource)PresentationSource.FromVisual(this);
            presentationSource.CompositionTarget.BackgroundColor = Colors.Black;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //WindowState = WindowState.Maximized;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            WindowState = WindowState.Maximized;
        }

        private void AreaMouseDown(object sender, EventArgs e)
        {
            var mousePosition = Mouse.GetPosition(rootCanvas);

            if (!AreaSelected)
            {
                _areaStartPosition = mousePosition;

                SelectionMode = AreaSelectionMode.ChangeX | AreaSelectionMode.ChangeY;
                AreaSelected = true;
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
                Cancel();
            }
            else if (e.Key is Key.Enter)
            {
                AcceptAndCopyToClipboard();
            }
            else if (e.Key is Key.S && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                AcceptAndSaveToFile();
            }
        }

        public CustomPopupPlacementCallback ToolbarPopupPlacementCallback { get; }

        [RelayCommand]
        public void Cancel()
        {
            DialogResult = false;
            Close();
        }

        [RelayCommand]
        public void AcceptAndSaveToFile()
        {
            var dateTime = DateTime.Now;

            _saveImageDialog = _saveImageDialog ?? new SaveFileDialog()
            {
                Title = "Save Image",
                CheckPathExists = true,
                Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|WEBP Image|*.webp|BMP Image|*.bmp",
            };

            _saveImageDialog.FileName = $"ScreenCapture-{dateTime.Year}-{dateTime.Month}-{dateTime.Day}_{dateTime.Hour}-{dateTime.Minute}-{dateTime.Second}";

            var dialogResult = _saveImageDialog.ShowDialog();
            if (dialogResult is null or false)
            {
                return;
            }

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

            try
            {
                using var fileStream = File.Create(_saveImageDialog.FileName);

                var format = System.IO.Path.GetExtension(_saveImageDialog.FileName).ToUpper() switch
                {
                    ".PNG" => SKEncodedImageFormat.Png,
                    ".BMP" => SKEncodedImageFormat.Bmp,
                    ".WEBP" => SKEncodedImageFormat.Webp,
                    ".JPEG" or ".JPG" => SKEncodedImageFormat.Jpeg,
                    _ => SKEncodedImageFormat.Png,
                };

                captureBitmap.Encode(fileStream, format, 100);
            }
            catch
            {
                MessageBox.Show("Failed to save image", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            DialogResult = true;
            Close();
        }

        [RelayCommand]
        public void AcceptAndCopyToClipboard()
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

            WriteableBitmap toCopy = new WriteableBitmap(captureBitmap.Width, captureBitmap.Height, ScreenBitmap.DpiX, ScreenBitmap.DpiY, PixelFormats.Pbgra32, null);
            toCopy.WritePixels(new Int32Rect(0, 0, toCopy.PixelWidth, toCopy.PixelHeight), captureBitmap.GetPixels(), captureBitmap.ByteCount, captureBitmap.RowBytes);

            Clipboard.SetImage(toCopy);

            DialogResult = true;
            Close();
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

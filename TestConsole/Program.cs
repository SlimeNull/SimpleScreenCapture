using System.Windows;
using System.Windows.Media.Imaging;
using LibSimpleScreenCapture;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        System.IO.MemoryStream ms = new();
        if (ScreenCapture.DoScreenCapture(ms))
        {
            BitmapImage bmpImage = new();
            bmpImage.BeginInit();
            bmpImage.StreamSource = ms;
            bmpImage.EndInit();

            Clipboard.SetImage(bmpImage);
        }

        // See https://aka.ms/new-console-template for more information
        Console.WriteLine("Hello, World!");
    }
}
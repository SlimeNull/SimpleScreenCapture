using System.Windows;
using System.Windows.Media.Imaging;
using LibSimpleScreenCapture;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        System.IO.MemoryStream ms = new();

        while (true)
        {
            Console.WriteLine("Press any key to capture screen");
            Console.ReadKey(true);

            if (ScreenCapture.DoScreenCapture(ms))
            {
                BitmapImage bmpImage = new();
                bmpImage.BeginInit();
                bmpImage.StreamSource = ms;
                bmpImage.EndInit();

                Clipboard.SetImage(bmpImage);
            }

        }

    }
}
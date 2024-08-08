using System.Windows;
using System.Windows.Media.Imaging;
using LibSimpleScreenCapture;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("Press any key to capture screen");
            Console.ReadKey(true);

            if (ScreenCapture.DoScreenCapture())
            {

            }

        }

    }
}
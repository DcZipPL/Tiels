using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
using Tiels.Classes;

namespace Tiels.Pages
{
    /// <summary>
    /// Logika interakcji dla klasy ErrorPage.xaml
    /// </summary>
    public partial class ErrorPage : Page
    {
        public ErrorPage()
        {
            InitializeComponent();
        }

        private void Reconf(object sender, RoutedEventArgs e) => Util.Reconfigurate();

        private void ClearCache(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\Cache"))
            {
                System.IO.DirectoryInfo dir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\Cache");
                foreach (FileInfo file in dir.GetFiles())
                {
                    file.Delete();
                }
            }
            Util.Reconfigurate(true);
        }

        private void Run_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Directory.Exists(@"C:\Users\DcZipPL\AppData\Local\Tiels\temp"))
                Process.Start(@"C:\Users\DcZipPL\AppData\Local\Tiels\temp");
        }

        private void Config_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Directory.Exists(@"C:\Users\DcZipPL\AppData\Local\Tiels"))
                Process.Start(@"C:\Users\DcZipPL\AppData\Local\Tiels");
        }
    }
}

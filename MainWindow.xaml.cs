using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DWinOverlay
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)+"\\Tiles";
        public List<TileWindow> tilesw = new List<TileWindow>();
        private System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();

            ni.Icon = new System.Drawing.Icon(@"C:\Users\DcZipPL\Desktop\Tiles\appicon.ico");
            ni.Visible = false;
            ni.Click +=
                delegate (object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    ni.Visible = false;
                };
        }

        private void TileLoaded(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(path) || !File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\config.json"))
            {
                ConfigureFirstRun();
            }
            else
            {
                Load();
            }
        }

        public async void Load()
        {
            main.Navigate(new Uri("pack://application:,,,/DWinOverlay;component/Pages/LoadingPage.xaml"));
            string[] tiles = Directory.EnumerateDirectories(path).ToArray();
            if (tiles.Length != 0)
            {
                for (int i = 0; i <= tiles.Length - 1; i++)
                {
                    tiles[i] = tiles[i].Replace(path + "\\", "");
                    TileWindow tile = new TileWindow();
                    tile.folderNameTB.Text = tiles[i];
                    tile.name = tiles[i];
                    tile.Show();
                    tilesw.Add(tile);
                }
            }

            await Task.Delay(1300);
            main.Navigate(new Uri("pack://application:,,,/DWinOverlay;component/Pages/MainPage.xaml"));
            loadingMessage.Text = "Tile Loaded Successfully!";
        }

        public async void ConfigureFirstRun()
        {
            main.Navigate(new Uri("pack://application:,,,/DWinOverlay;component/Pages/LoadingPage.xaml"));
            Process.Start("TielsConsole.exe","createlostandfound");
            await Task.Delay(1300);
            main.Navigate(new Uri("pack://application:,,,/DWinOverlay;component/Pages/ConfigurePage.xaml"));
            loadingMessage.Text = "Configuration.";
            WindowPosition pos0 = new WindowPosition();
            WindowPosition pos1 = new WindowPosition();
            WindowPosition[] positions = new WindowPosition[] { pos0, pos1 };
            ConfigClass config = new ConfigClass
            {
                FirstRun = true,
                Blur = true,
                Transparency = 255,
                Color = "#000000",
                Positions = positions
            };
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels"))
                File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\config.json", json);
            Console.WriteLine(json);
            if (!Directory.Exists(path + "\\Example"))
                Directory.CreateDirectory(path + "\\Example");

            File.WriteAllText(path + "\\Example\\ExampleContent.txt","Example Text.");
        }

        private void CloseWindowBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void HideWindowBtn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void TaskbarWindowBtn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            this.Hide();
            ni.Visible = true;
        }
    }
}
using System;
using System.Collections.Generic;
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
        System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();

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

        private async void TileLoaded(object sender, RoutedEventArgs e)
        {
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
                }
                //folderNameTB.Text = tiles[0];
            }

            /*if (File.Exists(path + "\\PositionData.dat"))
            {
                string posString = File.ReadAllText(path + "\\PositionData.dat"); // Input {FOLDERNAME}:{X}:{Y} ex. (Files X=120 Y=60) Files:120:60;
                string[] positions = posString.Replace("\r", "").Replace("\n", "").Replace(" ", "").Split(';');
                foreach (string position in positions)
                {
                    string[] splitedpos = position.Split(':');
                    if (position != "")
                    {
                        string name = splitedpos[0];
                        string x = splitedpos[1];
                        string y = splitedpos[2];
                        Top = int.Parse(y);
                        Left = int.Parse(x);
                    }
                }
            }
            else
            {
                File.WriteAllText(path + "\\PositionData.dat", tiles[0] + ":" + Left + ":" + Top + ";");
            }*/
            await Task.Delay(1300);
            loadingMessage.Text = "Tile Loaded Successfully!";
            loadingGif.Visibility = Visibility.Collapsed;
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
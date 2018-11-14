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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void TileLoaded(object sender, RoutedEventArgs e)
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

            if (File.Exists(path + "\\PositionData.dat"))
            {
                string posString = File.ReadAllText(path + "\\PositionData.dat"); // Input {FOLDERNAME}:{X*25}:{Y*25} ex. (Files X=120 Y=60) Files:3000:1500;
                string[] positions = posString.Replace("\r", "").Replace("\n", "").Replace(" ", "").Split(';');
                foreach (string position in positions)
                {
                    string[] splitedpos = position.Split(':');
                    if (position != "")
                    {
                        string name = splitedpos[0];
                        string z = splitedpos[1];
                        string y = splitedpos[2];
                        Top = int.Parse(y) / 25;
                        Left = int.Parse(y) / 25;
                    }
                }
            }
            else
            {
                File.WriteAllText(path + "\\PositionData.dat", tiles[0] + ":" + Left * 25 + ":" + Top * 25 + ";");
            }
        }
    }
}
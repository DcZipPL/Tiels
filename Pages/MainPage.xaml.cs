using Newtonsoft.Json;
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

namespace DWinOverlay.Pages
{
    /// <summary>
    /// Logika interakcji dla klasy MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        protected string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Tiles";
        public MainPage()
        {
            InitializeComponent();
        }

        private void CloseDialogBtn_Click(object sender, RoutedEventArgs e)
        {
            dialogBox.Visibility = Visibility.Collapsed;
        }

        private void CreateNewTile(object sender, RoutedEventArgs e)
        {
            //TODO: Creating Folder as Tile
            if (!newTileName.Text.Contains("\\"))
                if (!newTileName.Text.Contains("/"))
                    if (!newTileName.Text.Contains("*"))
                        if (!newTileName.Text.Contains(":"))
                            if (!newTileName.Text.Contains("?"))
                                if (!newTileName.Text.Contains("<"))
                                    if (!newTileName.Text.Contains(">"))
                                        if (!newTileName.Text.Contains("|"))
                                        {
                                            Directory.CreateDirectory(path + "\\" + newTileName.Text);
                                        }
        }

        private void CloseDeleteDialogBtn_Click(object sender, RoutedEventArgs e)
        {
            deleteDialogBox.Visibility = Visibility.Collapsed;
        }

        private void Tilelist_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow mw = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            foreach (var tile in mw.tilesw)
            {
                ListBoxItem lbi = new ListBoxItem();
                lbi.Content = tile.name;
                lbi.Tag = tile.path;
                tilelist.Items.Add(lbi);
            }
        }

        private void DeleteTile(object sender, RoutedEventArgs e)
        {
            foreach (ListBoxItem item in tilelist.Items)
            {
                if (item.IsSelected)
                {
                    string itempath = item.Tag + "\\" + item.Content;
                    string[] files = Directory.EnumerateFiles(itempath).ToArray();
                    Directory.Move(itempath, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\temp\\" + item.Content);
                    Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\temp\\");
                    MainWindow mw = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                    foreach (var tile in mw.tilesw)
                    {
                        if (tile.name == (string)item.Content)
                        {
                            tile.Close();
                        }
                    }
                }
            }
        }

        private void Reconf(object sender, RoutedEventArgs e)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels");

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                foreach (FileInfo filei in dir.GetFiles())
                {
                    filei.Delete();
                }
                foreach (DirectoryInfo diri in dir.GetDirectories())
                {
                    diri.Delete(true);
                }
                dir.Delete(true);
            }
            Process.Start(System.Reflection.Assembly.GetEntryAssembly().Location);
            System.Windows.Application.Current.Shutdown();
        }

        private void OpenCreateDialog(object sender, RoutedEventArgs e)
        {
            HideAllDialogs();
            dialogBox.Visibility = Visibility.Visible;
        }

        private void OpenDeleteDialog(object sender, RoutedEventArgs e)
        {
            HideAllDialogs();
            dialogBox.Visibility = Visibility.Visible;
        }

        private void HideAllDialogs()
        {
            dialogBox.Visibility = Visibility.Collapsed;
            deleteDialogBox.Visibility = Visibility.Collapsed;
        }

        private void ColorTile_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\config.json"))
            {
                string json_out = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\config.json");
                ConfigClass config = JsonConvert.DeserializeObject<ConfigClass>(json_out);
                colorTile.SelectedColor = (Color)ColorConverter.ConvertFromString(config.Color);
            }
            else
            {
                Reconf(this, null);
            }
        }

        private void SetNewColor(object sender, RoutedEventArgs e)
        {
            string json_out = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\config.json");
            ConfigClass config = JsonConvert.DeserializeObject<ConfigClass>(json_out);

            if (colorTile.SelectedColor != null)
            {
                System.Windows.Media.Color color = (System.Windows.Media.Color)colorTile.SelectedColor;
                System.Drawing.Color newColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
                config.Color = HexConverter(newColor);
            }

            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels"))
                File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\config.json", json);
        }

        private static String HexConverter(System.Drawing.Color c)
        {
            return "#" + c.A.ToString("X2") + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }
    }
}

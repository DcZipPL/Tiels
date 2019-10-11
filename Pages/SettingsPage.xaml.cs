using DWinOverlay.Classes;
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
using System.Windows.Shapes;

namespace DWinOverlay.Pages
{
    /// <summary>
    /// Logika interakcji dla klasy SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void SetNewColor(object sender, RoutedEventArgs e)
        {
            string json_out = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\config.json");
            ConfigClass config = JsonConvert.DeserializeObject<ConfigClass>(json_out);

            if (colorTile.SelectedColor != null)
            {
                System.Windows.Media.Color color = (System.Windows.Media.Color)colorTile.SelectedColor;
                System.Drawing.Color newColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
                config.Color = Util.HexConverter(newColor);
            }

            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels"))
                File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\config.json", json);
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
                Reconf();
            }
        }

        private void Reconf()
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

        private void BackHome(object sender, RoutedEventArgs e)
        {
            MainWindow mw = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            mw.Visibility = Visibility.Hidden;
            mw.main.Navigate(new Uri("pack://application:,,,/DWinOverlay;component/Pages/MainPage.xaml"));
            mw.Width = 900;
            mw.Height = 500;
            mw.Top = (System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - mw.Height) / 2;
            mw.Left = (System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width - mw.Width) / 2;
            mw.Visibility = Visibility.Visible;
        }
    }
}

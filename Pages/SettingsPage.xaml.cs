using Tiels.Classes;
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

namespace Tiels.Pages
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
            ConfigClass config = Config.GetConfig();

            if (colorTile.SelectedColor != null)
            {
                System.Windows.Media.Color color = (System.Windows.Media.Color)colorTile.SelectedColor;
                System.Drawing.Color newColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
                config.Color = Util.HexConverter(newColor);
            }

            bool result = Config.SetConfig(config);
            if (result == false)
            {
                Util.Reconfigurate();
            }
        }

        private void ColorTile_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\config.json"))
            {
                ConfigClass config = Config.GetConfig();
                colorTile.SelectedColor = (Color)ColorConverter.ConvertFromString(config.Color);
            }
            else
            {
                Util.Reconfigurate();
            }
        }

        private void BackHome(object sender, RoutedEventArgs e)
        {
            SetNewColor(null, null);
            MainWindow mw = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            mw.Visibility = Visibility.Hidden;
            mw.main.Navigate(new Uri("pack://application:,,,/Tiels;component/Pages/MainPage.xaml"));
            mw.Width = 900;
            mw.Height = 500;
            mw.Top = (System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - mw.Height) / 2;
            mw.Left = (System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width - mw.Width) / 2;
            foreach (TileWindow tile in mw.tilesw)
            {
                tile.Close();
            }
            mw.tilesw.Clear();
            mw.Load();
            mw.Visibility = Visibility.Visible;
        }

        private void ColorTile_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            MainWindow mw = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            foreach (TileWindow tile in mw.tilesw)
            {
                tile.MainGrid.Background = new SolidColorBrush((System.Windows.Media.Color)colorTile.SelectedColor);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeCombobox.SelectedIndex != -1)
            {
                ConfigClass config = Config.GetConfig();
                config.Theme = ThemeCombobox.SelectedIndex;
                bool result = Config.SetConfig(config);
                if (result == false)
                {
                    Util.Reconfigurate();
                }
            }
        }

        private void ThemeCombobox_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigClass config = Config.GetConfig();
            ThemeCombobox.SelectedIndex = config.Theme;
        }
    }
}

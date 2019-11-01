using Tiels.Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Tiels.Pages
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

        private void CreateNewTile(object sender, RoutedEventArgs e)
        {
            //"/[<>/\\*:\?\|]/g
            if (!Regex.IsMatch(newTileName.Text, @"\<|\>|\\|\/|\*|\?|\||:"))
            {
                Directory.CreateDirectory(path + "\\" + newTileName.Text);
                File.WriteAllText(path + "\\"+ newTileName.Text + "\\desktop.ini", "[.ShellClassInfo]\r\nIconResource=" + Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\directoryicon.ico,0");
                MainWindow mw = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                foreach (TileWindow tile in mw.tilesw)
                {
                    tile.Close();
                }
                mw.tilesw.Clear();
                tilelist.Items.Clear();
                mw.Load();
            }
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
            ListBoxItem tmp_item = null;
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
                    tmp_item = item;
                }
            }
            if (tmp_item != null)
                tilelist.Items.Remove(tmp_item);
        }

        private void Reconf(object sender, RoutedEventArgs e) => Util.Reconfigurate();

        private void ShowSettings(object sender, RoutedEventArgs e)
        {
            //MainWindow mw = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            //mw.Visibility = Visibility.Hidden;
            //mw.main.Navigate(new Uri("pack://application:,,,/Tiels;component/Pages/SettingsPage.xaml"));
            //mw.Width = 500;
            //mw.Height = 800;
            //mw.Top = (System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - mw.Height) / 2;
            //mw.Left = (System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width - mw.Width) / 2;
            //mw.Visibility = Visibility.Visible;
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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigClass config = Config.GetConfig();
            dmoveinfo.Text = dmoveinfo.Text.Replace("{filepos}", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\temp");
            AutostartCB.IsChecked = File.Exists(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Tiels.lnk"));
            HidewhileloadingCB.IsChecked = config.HideFilesWhileLoading;
        }

        private void CreateShortcut(string tpath)
        {
            IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
            string shortcutAddress = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Tiels.lnk");
            IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "Startup shortcut for Tiels";
            shortcut.TargetPath = tpath;
            shortcut.Save();
        }

        private void ShowGeneral(object sender, RoutedEventArgs e)
        {
            GeneralBtn.IsChecked = true;
            AppearanceBtn.IsChecked = false;
            WindowsBtn.IsChecked = false;
            generalWindow.Visibility = Visibility.Visible;
            appearanceWindow.Visibility = Visibility.Collapsed;
            windowsWindow.Visibility = Visibility.Collapsed;
        }

        private void AppearanceBtn_Click(object sender, RoutedEventArgs e)
        {
            GeneralBtn.IsChecked = false;
            AppearanceBtn.IsChecked = true;
            WindowsBtn.IsChecked = false;
            generalWindow.Visibility = Visibility.Collapsed;
            appearanceWindow.Visibility = Visibility.Visible;
            windowsWindow.Visibility = Visibility.Collapsed;
        }

        private void ShowWindows(object sender, RoutedEventArgs e)
        {
            GeneralBtn.IsChecked = false;
            AppearanceBtn.IsChecked = false;
            WindowsBtn.IsChecked = true;
            generalWindow.Visibility = Visibility.Collapsed;
            appearanceWindow.Visibility = Visibility.Collapsed;
            windowsWindow.Visibility = Visibility.Visible;
        }

        private async void ShowUpdates(object sender, RoutedEventArgs e)
        {
            try
            {
                //Check updates
                var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("tiels-updates-check"));
                var releases = await client.Repository.Release.GetAll("DcZipPL", "Tiels");
                var latest = releases[0];
                if (latest.TagName == App.Version)
                {
                    Console.WriteLine("No updates found.");
                }
                else
                {
                    UpdatesBtn.FontWeight = FontWeights.Bold;
                    UpdatesBtn.Foreground = Brushes.ForestGreen;
                    Process.Start("https://github.com/DcZipPL/Tiels/releases");
                }
                Console.WriteLine(
                    "The latest release is tagged at {0} and is named {1}",
                    latest.TagName,
                    latest.Name);
            }
            catch (Exception ex)
            {
                UpdatesBtn.Foreground = Brushes.DarkRed;
            }
        }

        #region Checkboxes
        private void AutostartCB_Checked(object sender, RoutedEventArgs e)
        {
            CreateShortcut(System.Reflection.Assembly.GetEntryAssembly().Location);
        }

        private void AutostartCB_Unchecked(object sender, RoutedEventArgs e)
        {
            File.Delete(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Tiels.lnk"));
        }

        private void HidewhileloadingCB_Checked(object sender, RoutedEventArgs e)
        {
            ConfigClass config = Config.GetConfig();
            config.HideFilesWhileLoading = true;
            bool result = Config.SetConfig(config);
            if (result == false)
            {
                Util.Reconfigurate();
            }
        }

        private void HidewhileloadingCB_Unchecked(object sender, RoutedEventArgs e)
        {
            ConfigClass config = Config.GetConfig();
            config.HideFilesWhileLoading = false;
            bool result = Config.SetConfig(config);
            if (result == false)
            {
                Util.Reconfigurate();
            }
        }
        #endregion
    }
}

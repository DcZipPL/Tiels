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
using TielsUILib;
using System.Runtime.InteropServices;
using Microsoft.Win32;

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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigClass config = Config.GetConfig();
            dmoveinfo.Text = dmoveinfo.Text.Replace("{filepos}", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\temp");
            AutostartCB.IsChecked = File.Exists(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Tiels.lnk"));
            HideafterstartCB.IsChecked = config.HideAfterStart;
            EffectsCB.IsChecked = config.SpecialEffects;

            TielsUILib.MainControl control = ((TielsUILib.MainControl)((Microsoft.Toolkit.Wpf.UI.XamlHost.WindowsXamlHost)host).Child);
            SettingsPage settings = control.GetSettingsPage();

            settings.autostart_tg.IsOn = File.Exists(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Tiels.lnk"));
            settings.autostart_tg.Toggled += (sender, args) =>
            {
                if (settings.autostart_tg.IsOn)
                {
                    CreateShortcut(System.Reflection.Assembly.GetEntryAssembly().Location);
                }
                else
                {
                    File.Delete(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Tiels.lnk"));
                }
            };

            ManagePage manage = control.GetManagePage();

            MainWindow mw = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            foreach (var tile in mw.tilesw)
            {
                manage.AddTileToList(tile.name, tile.path);
            }

            // Add create tile event on "createTileBtn" button click
            manage.createTileBtn.Click += (sender, e) =>
            {
                string tileName = "tt";
                //"/[<>/\\*:\?\|]/g
                if (!Regex.IsMatch(tileName, @"\<|\>|\\|\/|\*|\?|\||:"))
                {
                    Directory.CreateDirectory(path + "\\" + tileName);
                    string dir = path + "\\" + tileName + "\\desktop.ini";
                    File.WriteAllText(dir,
                        "[.ShellClassInfo]\r\n" +
                        "IconResource=" + Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\directoryicon.ico,0\r\n" +
                        "[ViewState]\r\n" +
                        "Mode =\r\n" +
                        "Vid =\r\n" +
                        "FolderType = Generic"
                        );

                    File.SetAttributes(path + "\\" + tileName + "\\desktop.ini", FileAttributes.Hidden | FileAttributes.Archive | FileAttributes.System | FileAttributes.ReadOnly);
                    //RefreshIconCache();

                    MainWindow mw = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                    foreach (TileWindow tile in mw.tilesw)
                    {
                        tile.Close();
                    }
                    mw.tilesw.Clear();
                    tilelist.Items.Clear();
                    mw.Load();
                }
            };

            foreach (object item in manage.tileList.Items)
            {
                if (item is ListViewItem)
                {
                    // -> button
                    ((ListViewItem)item).Selected += (sender, e) =>
                    {
                        ListViewItem tmp_item = null;
                        foreach (ListViewItem item in tilelist.Items)
                        {
                            if (item.IsSelected)
                            {
                                string itempath = item.Tag + "\\" + item.Content;
                                if (File.Exists(itempath))
                                {
                                    string[] files = Directory.EnumerateFiles(itempath).ToArray();
                                    Directory.Move(itempath, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\temp\\" + item.Content);
                                    Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\temp\\");
                                }
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
                    };
                }
            }
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr SendMessageTimeout(
            int windowHandle,
            int Msg,
            int wParam,
            String lParam,
            SendMessageTimeoutFlags flags,
            int timeout,
            out int result);
        [Flags]
        enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0,
            SMTO_BLOCK = 0x1,
            SMTO_ABORTIFHUNG = 0x2,
            SMTO_NOTIMEOUTIFNOTHUNG = 0x8
        }
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(
    int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

        static void RefreshIconCache()
        {

            // get the the original Shell Icon Size registry string value
            RegistryKey k = Registry.CurrentUser.OpenSubKey("Control Panel").OpenSubKey("Desktop").OpenSubKey("WindowMetrics", true);
            Object OriginalIconSize = k.GetValue("Shell Icon Size");

            // set the Shell Icon Size registry string value
            k.SetValue("Shell Icon Size", (Convert.ToInt32(OriginalIconSize) + 1).ToString());
            k.Flush(); k.Close();

            // broadcast WM_SETTINGCHANGE to all window handles
            int res = 0;
            SendMessageTimeout(0xffff, 0x001A, 0, "", SendMessageTimeoutFlags.SMTO_ABORTIFHUNG, 5000, out res);
            //SendMessageTimeout(HWD_BROADCAST,WM_SETTINGCHANGE,0,"",SMTO_ABORTIFHUNG,5 seconds, return result to res)

            // set the Shell Icon Size registry string value to original value
            k = Registry.CurrentUser.OpenSubKey("Control Panel").OpenSubKey("Desktop").OpenSubKey("WindowMetrics", true);
            k.SetValue("Shell Icon Size", OriginalIconSize);
            k.Flush(); k.Close();

            SendMessageTimeout(0xffff, 0x001A, 0, "", SendMessageTimeoutFlags.SMTO_ABORTIFHUNG, 5000, out res);

        }

        private void host_ChildChanged(object sender, EventArgs e)
        {

        }

        private void CreateNewTile(object sender, RoutedEventArgs e)
        {

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

        }

        private void Reconf(object sender, RoutedEventArgs e) => ErrorHandler.Error();

        private void SetNewColor(object sender, RoutedEventArgs e)
        {
            ConfigClass config = Config.GetConfig();

            // TODO: colorTile must be replaced
            /*if (colorTile.SelectedColor != null)
            {
                System.Windows.Media.Color color = (System.Windows.Media.Color)colorTile.SelectedColor;
                System.Drawing.Color newColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
                config.Color = Util.HexConverter(newColor);
            }

            bool result = Config.SetConfig(config);
            if (result == false)
            {
                ErrorHandler.Error();
            }*/
        }

        // TODO: colorTile must be replaced
        /*private void ColorTile_Loaded(object sender, RoutedEventArgs e)
        {
            // TODO: colorTile must be replaced
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\config.json"))
            {
                ConfigClass config = Config.GetConfig();
                colorTile.SelectedColor = (Color)ColorConverter.ConvertFromString(config.Color);
            }
            else
            {
                ErrorHandler.Error();
            }
        }*/

        private void BackHome(object sender, RoutedEventArgs e)
        {
            //SetNewColor(null, null);
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

        // TODO: colorTile must be replaced
        /*private void ColorTile_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            MainWindow mw = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            foreach (TileWindow tile in mw.tilesw)
            {
                tile.MainGrid.Background = new SolidColorBrush((System.Windows.Media.Color)colorTile.SelectedColor);
            }
        }*/

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeCombobox.SelectedIndex != -1)
            {
                ConfigClass config = Config.GetConfig();
                config.Theme = ThemeCombobox.SelectedIndex;
                bool result = Config.SetConfig(config);
                if (result == false)
                {
                    ErrorHandler.Error();
                }
                MainWindow mw = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                foreach (TileWindow tile in mw.tilesw)
                {
                    tile.folderNameTB.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
                    tile.hideBtn.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
                    tile.gotodirectoryBtn.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
                    tile.editBtn.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
                    tile.rotateBtn.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
                    foreach (UIElement grid in tile.FilesList.Children)
                    {
                        if (grid is Grid)
                        {
                            foreach (UIElement element in ((Grid)(((Button)((Grid)grid).Children[0]).Content)).Children)
                            {
                                if (element is TextBlock)
                                {
                                    ((TextBlock)element).Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ThemeCombobox_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigClass config = Config.GetConfig();
            ThemeCombobox.SelectedIndex = config.Theme;
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

        /*private void ShowGeneral(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            GeneralBtn.IsChecked = true;
            generalWindow.Visibility = Visibility.Visible;
        }

        private void AppearanceBtn_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            AppearanceBtn.IsChecked = true;
            appearanceWindow.Visibility = Visibility.Visible;
        }

        private void ShowTiles(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            TilesBtn.IsChecked = true;
            tilesWindow.Visibility = Visibility.Visible;
        }

        private void ShowDirectoryPortals(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            DirectoryPortalBtn.IsChecked = true;
        }

        private void ShowFloatingImages(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            FloatingImageBtn.IsChecked = true;
        }

        private void ShowNotes(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            NotesBtn.IsChecked = true;
        }

        private void HideAllPages()
        {
            GeneralBtn.IsChecked = false;
            AppearanceBtn.IsChecked = false;
            TilesBtn.IsChecked = false;
            DirectoryPortalBtn.IsChecked = false;
            FloatingImageBtn.IsChecked = false;
            NotesBtn.IsChecked = false;
            generalWindow.Visibility = Visibility.Collapsed;
            appearanceWindow.Visibility = Visibility.Collapsed;
            tilesWindow.Visibility = Visibility.Collapsed;
        }*/

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
                    ErrorHandler.Info("No updates found.");
                }
                else
                {
                    UpdatesBtn.FontWeight = FontWeights.Bold;
                    UpdatesBtn.Foreground = Brushes.ForestGreen;
                    Process.Start("https://github.com/DcZipPL/Tiels/releases");
                }
                Console.WriteLine(
                    "[Debug] The latest release is tagged at {0} and is named {1}",
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
            
        }

        private void AutostartCB_Unchecked(object sender, RoutedEventArgs e)
        {
            
        }

        private void ExperimentalFeaturesCB_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ExperimentalFeaturesCB_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void EffectsCB_Checked(object sender, RoutedEventArgs e)
        {
            ConfigClass config = Config.GetConfig();
            config.SpecialEffects = true;
            bool result = Config.SetConfig(config);
            if (result == false)
            {
                ErrorHandler.Error();
            }
        }

        private void EffectsCB_Unchecked(object sender, RoutedEventArgs e)
        {
            ConfigClass config = Config.GetConfig();
            config.SpecialEffects = false;
            bool result = Config.SetConfig(config);
            if (result == false)
            {
                ErrorHandler.Error();
            }
        }

        private void HideafterstartCB_Checked(object sender, RoutedEventArgs e)
        {
            ConfigClass config = Config.GetConfig();
            config.HideAfterStart = true;
            bool result = Config.SetConfig(config);
            if (result == false)
            {
                ErrorHandler.Error();
            }
        }

        private void HideafterstartCB_Unchecked(object sender, RoutedEventArgs e)
        {
            ConfigClass config = Config.GetConfig();
            config.HideAfterStart = false;
            bool result = Config.SetConfig(config);
            if (result == false)
            {
                ErrorHandler.Error();
            }
        }
        #endregion
    }
}

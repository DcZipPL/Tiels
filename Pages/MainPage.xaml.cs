using DWinOverlay.Classes;
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

        private void OpenCreateDialog(object sender, RoutedEventArgs e)
        {
            HideAllDialogs();
            dialogBox.Visibility = Visibility.Visible;
        }

        private void OpenDeleteDialog(object sender, RoutedEventArgs e)
        {
            HideAllDialogs();
            deleteDialogBox.Visibility = Visibility.Visible;
        }

        private void HideAllDialogs()
        {
            dialogBox.Visibility = Visibility.Collapsed;
            deleteDialogBox.Visibility = Visibility.Collapsed;
        }

        private void ShowSettings(object sender, RoutedEventArgs e)
        {
            MainWindow mw = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            mw.Visibility = Visibility.Hidden;
            mw.main.Navigate(new Uri("pack://application:,,,/DWinOverlay;component/Pages/SettingsPage.xaml"));
            mw.Width = 500;
            mw.Height = 800;
            mw.Top = (System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - mw.Height) / 2;
            mw.Left = (System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width - mw.Width) / 2;
            mw.Visibility = Visibility.Visible;
        }
    }
}

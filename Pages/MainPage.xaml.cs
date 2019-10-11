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
                                            MainWindow mw = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                                            foreach (TileWindow tile in mw.tilesw)
                                            {
                                                tile.Close();
                                            }
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

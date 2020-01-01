using Tiels.Classes;
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
using System.Windows.Resources;

namespace Tiels
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)+"\\Tiles";
        protected string config_path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels";
        public List<TileWindow> tilesw = new List<TileWindow>();
        public bool isLoading = true;
        private System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();

            //Notify Icon
            ni.Icon = File.Exists(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\Assets\\" + "appicon.ico") ? new System.Drawing.Icon(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)+ "\\Assets\\" + "appicon.ico") : null;
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
            //If exists config and main app directory
            if (!Directory.Exists(path) || !File.Exists(config_path + "\\config.json"))
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
            isLoading = true;
            if (File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tiels\\config.json") == "") Util.Reconfigurate();
            ConfigClass config = Config.GetConfig();
            main.Navigate(new Uri("pack://application:,,,/Tiels;component/Pages/LoadingPage.xaml"));
            await Task.Delay(200);

            string[] tiles = Directory.EnumerateDirectories(path).ToArray();
            //Check config version
            if (App.Version != config.Version)
            {
                //Update version and add default values
                try
                {
                    if (int.Parse(config.Version.Substring(3, 1)) < 5)
                    {
                        Util.Reconfigurate();
                    }
                }
                catch (Exception ex)
                {
                    Util.Reconfigurate();
                }
            }

            if (!config.SpecialEffects) shadoweffect.Effect = null;

            if (tiles.Length != 0)
            {
                config = Config.GetConfig();
                for (int i = 0; i <= tiles.Length - 1; i++)
                {
                    tiles[i] = tiles[i].Replace(path + "\\", "");

                    //Creating tile
                    TileWindow tile = new TileWindow();
                    tile.folderNameTB.Text = tiles[i];
                    tile.name = tiles[i];
                    tilesw.Add(tile);
                    bool windowexist = false;

                    //Search for tile in config
                    foreach (var window in config.Windows)
                    {
                        if (window.Name == tiles[i])
                        {
                            //Tile exists?
                            windowexist = true;

                            //Setting tile rosition
                            tile.Left = window.Position.X;
                            tile.Top = window.Position.Y;

                            //Rotating tile
                            if (!window.EditBar)
                            {
                                tile.rd.Height = new GridLength(1,GridUnitType.Star);
                                tile.trd.Height = new GridLength(28);
                                Grid.SetRow(tile.ActionGrid, 1);
                                Grid.SetRow(tile.ScrollFilesList, 0);
                            }
                            else
                            {
                                tile.rd.Height = new GridLength(28);
                                tile.trd.Height = new GridLength(1, GridUnitType.Star);
                                Grid.SetRow(tile.ActionGrid, 0);
                                Grid.SetRow(tile.ScrollFilesList, 1);
                            }
                            if (window.CollapsedRows > 0)
                            {
                                tile.Height = window.CollapsedRows;
                                tile.ScrollFilesList.Height = window.CollapsedRows;
                            }
                            else
                            {
                                window.CollapsedRows = 0;
                            }
                        }
                    }
                    //If tile not exists create default values
                    if (!windowexist)
                    {
                        JsonWindow jsonwindow = new JsonWindow();
                        jsonwindow.Name = tiles[i];
                        jsonwindow.Position = new WindowPosition { X = 0, Y = 0 };
                        jsonwindow.CollapsedRows = 0;
                        jsonwindow.EditBar = false;
                        jsonwindow.Id = i;
                        config.Windows.Add(jsonwindow);
                    }
                    tile.Show();
                }
                //If Config File not Exists
                bool result = Config.SetConfig(config);
                if (result == false)
                {
                    Util.Reconfigurate();
                }
            }
            isLoading = false;

            //Load MainPage
            main.Navigate(new Uri("pack://application:,,,/Tiels;component/Pages/MainPage.xaml"));
            loadingMessage.Text = "Tile Loaded Successfully!";
        }

        public void ConfigureFirstRun()
        {
            main.Navigate(new Uri("pack://application:,,,/Tiels;component/Pages/LoadingPage.xaml"));

            //Starting Tiels Console that generate Main App Directory and temp
            //Process.Start("TielsConsole.exe","createlostandfound");
            Console.WriteLine("creating lostandfound directory...");
            try
            {
                if (!Directory.Exists(config_path))
                    Directory.CreateDirectory(config_path);
                if (!Directory.Exists(config_path + "\\" + "temp"))
                    Directory.CreateDirectory(config_path + "\\" + "temp");
                Console.WriteLine("succesfully created lostandfound directory!");
            }
            catch (Exception ex)
            {
                File.AppendAllText(config_path + "\\Error.log", "\r\n[Error: " + DateTime.Now + "] " + ex.ToString());
                try
                {
                    ProcessStartInfo info = new ProcessStartInfo
                    {
                        Arguments = "createlostandfounda",
                        Verb = "runas"
                    };
                    Console.WriteLine(ex.ToString());
                }
                catch (Exception iex)
                {
                    File.AppendAllText(config_path + "\\Error.log", "\r\n[Error: " + DateTime.Now + "] " + iex.ToString());
                    Application.Current.Shutdown();
                }
            }
            main.Navigate(new Uri("pack://application:,,,/Tiels;component/Pages/ConfigurePage.xaml"));
            loadingMessage.Text = "Configuration.";

            //Creating config and creating example tile
            WindowPosition pos0 = new WindowPosition();
            WindowPosition pos1 = new WindowPosition();
            WindowPosition[] positions = new WindowPosition[] { pos0, pos1 };
            JsonWindow jwindow = new JsonWindow();
            List<JsonWindow> jwindows = new List<JsonWindow>();
            jwindows.Add(jwindow);
            ConfigClass config = new ConfigClass
            {
                Version = App.Version,
                FirstRun = true,
                Blur = true,
                Theme = 1,
                Color = "#19000000",
                HideAfterStart = true,
                SpecialEffects = true,
                Windows = jwindows
            };

            //Creating directory and config file
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            if (Directory.Exists(config_path))
                File.WriteAllText(config_path+"\\config.json", json);
            Console.WriteLine(json);
            if (!Directory.Exists(path + "\\Example"))
                Directory.CreateDirectory(path + "\\Example");

            string icoPath = "pack://application:,,,/Tiels;component/Assets/TielsDirectory.ico";
            StreamResourceInfo icoInfo = System.Windows.Application.GetResourceStream(new Uri(icoPath));
            byte[] bytes = Util.ReadFully(icoInfo.Stream);
            File.WriteAllBytes(config_path + "\\directoryicon.ico", bytes);

            //Creating example text file in tile
            File.WriteAllText(path + "\\Example\\ExampleContent.txt","Example Text.");
            File.WriteAllText(path + "\\Example\\desktop.ini", "[.ShellClassInfo]\r\nIconResource="+ config_path + "\\directoryicon.ico,0");
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
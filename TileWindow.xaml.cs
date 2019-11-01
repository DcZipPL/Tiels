using Shell32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Newtonsoft.Json;
using Tiels.Classes;
using System.Text.RegularExpressions;
using System.Windows.Shapes;

namespace Tiels
{
    /// <summary>
    /// Logika interakcji dla klasy TileWindow.xaml
    /// </summary>
    public partial class TileWindow : Window
    {
        public string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Tiles";
        public string config_path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\" + "Tiels";
        public string name = null;
        private string newname = null;

        public int rows = 0;
        private int tries = 0;
        private int lastHeight = 0;
        private static int id = 0;

        private bool isLoading = false;
        private bool isMoving = false;
        public bool isHidded = false;
        public bool isEditMode = false;

        private Point MousePos;

        List<SoftFileData> filedata = new List<SoftFileData>();
        //System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        //System.Windows.Threading.DispatcherTimer updateTimer = new System.Windows.Threading.DispatcherTimer();

        #region DLL IMPORTS

        #region Window styles
        [Flags]
        public enum ExtendedWindowStyles
        {
            // ...
            WS_EX_TOOLWINDOW = 0x00000080,
            // ...
        }

        public enum GetWindowLongFields
        {
            // ...
            GWL_EXSTYLE = (-20),
            // ...
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);
        #endregion

        #endregion

        public TileWindow()
        {
            InitializeComponent();

            //dispatcherTimer.Tick += DispatcherTimer_Tick;
            //dispatcherTimer.Interval = new TimeSpan(0, 1, 0);
            //dispatcherTimer.Start();
            //dispatcherTimer.Tick += UpdateTimer_Tick;
            //dispatcherTimer.Interval = new TimeSpan(10);
            //dispatcherTimer.Start();
        }

        private void MoveActionStart(object sender, MouseButtonEventArgs e)
        {
            isMoving = true;
            MousePos = Util.GetMousePosition();
            MousePos.X = Convert.ToInt32(this.Left) - MousePos.X;
            MousePos.Y = Convert.ToInt32(this.Top) - MousePos.Y;
        }
        private void MoveActionCancel(object sender, MouseEventArgs e) => MoveActionStop(this, null);
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            CheckFileUpdates();
            //SetBottom(this);
            if (isLoading == false)
            {
                ConfigClass config = Config.GetConfig();
                if (config != null)
                {
                    foreach (var window in config.Windows)
                    {
                        if (window.Name == name)
                        {
                            window.CollapsedRows = (int)this.Height;
                        }
                    }
                    bool result = Config.SetConfig(config);
                    if (result == false)
                    {
                        Util.Reconfigurate();
                    }
                }
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            //if (isLoading == false)
                //if (this.Height >= this.Height - 28)
                    //ScrollFilesList.Height = this.Height - 28;
        }

        private void CheckFileUpdates()
        {
            if (File.Exists(path + "\\" + name + "_fileupdate.json") && Directory.Exists(path+"\\"+name))
            {
                try
                {
                    string json_out = File.ReadAllText(path + "\\" + name + "_fileupdate.json");
                    SoftFileData[] data = JsonConvert.DeserializeObject<FileUpdateClass>(json_out).Data;
                    string[] elements = Directory.EnumerateFiles(path + "\\" + name).ToArray();
                    if (elements.Length != data.Length)
                    {
                        ReadElements();
                    }
                    else
                    {
                        for (int i = 0; i < elements.Length; i++)
                        {
                            if (data[i].Name == elements[i])
                            {
                                if ((int)data[i].Size / 100 != (int)new System.IO.FileInfo(elements[i]).Length / 100)
                                {
                                    ReadElements();
                                }
                            }
                            else
                            {
                                ReadElements();
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    File.WriteAllText(config_path + "\\Error.log", ex.ToString());
                    if (tries <= 3)
                    {
                        ReadElements();
                        tries++;
                    }
                    else
                    {
                        ErrorWindow ew = new ErrorWindow();
                        ew.ExceptionReason = ex;
                        ew.Show();
                    }
                }
            }
        }

        public async Task LateUpdate()
        {
            while (true)
            {
                await Task.Delay(500);
                CheckFileUpdates();
                //SetBottom(this);
                if (isLoading == false)
                {
                    ConfigClass config = Config.GetConfig();
                    if (config != null)
                    {
                        foreach (var window in config.Windows)
                        {
                            if (window.Name == name)
                            {
                                window.CollapsedRows = (int)this.Height;
                            }
                        }
                        bool result = Config.SetConfig(config);
                        if (result == false)
                        {
                            Util.Reconfigurate();
                        }
                    }
                }
            }
        }

        public async Task Update()
        {
            while (true)
            {
                await Task.Delay(10);
                if (isLoading == false)
                    if (this.Height >= this.Height - 28)
                        ScrollFilesList.Height = this.Height - 28;
            }
        }

        private async void TileLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _=LateUpdate();
                _=Update();
                ConfigClass config = Config.GetConfig();
                //updateTimer.Interval = new TimeSpan(!config.SpecialEffects ? 400 : 2);

                //Disable Alt-Tab
                WindowInteropHelper wndHelper = new WindowInteropHelper(this);

                int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);

                exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
                SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);

                Util.EnableBlur(this);
                SetBottom(this);

                MainGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(config.Color));

                //Set text color by theme
                folderNameTB.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
                hideBtn.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
                gotodirectoryBtn.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
                editBtn.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
                rotateBtn.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
                editBox.Text = name;

                if (folderNameTB.Text.Length >= 36)
                {
                    folderNameTB.Text = folderNameTB.Text.Remove(folderNameTB.Text.Length - (folderNameTB.Text.Length - 36)) + "...";
                }

                ReadElements();
            }
            catch (Exception ex)
            {
                File.WriteAllText(config_path + "\\Error.log",ex.ToString());
            }
        }

        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        public static void SetBottom(Window window)
        {
            //TODO: Move to bottom but with id
            IntPtr hWnd = new WindowInteropHelper(window).Handle;
            Util.SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, Util.SWP_NOSIZE | Util.SWP_NOMOVE | Util.SWP_NOACTIVATE);
        }

        public bool IsSmallIcon(System.Drawing.Bitmap img)
        {
            int solidPixels = 0;
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    if (i >= 48 && j >= 48)
                    {
                        System.Drawing.Color pixel = img.GetPixel(i, j);
                        if (pixel.A != 0)
                        {
                            solidPixels++;
                        }
                    }
                }
            }
            if (solidPixels <= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ReadElements()
        {
            try
            {
                isLoading = true;
                ConfigClass config = Config.GetConfig();
                if (config.HideFilesWhileLoading == true)
                {
                    FilesList.Visibility = Visibility.Hidden;
                    loadinginfo.Visibility = Visibility.Visible;
                }
                //Clear data
                filedata.Clear();

                FilesList.Children.Clear();
                FilesList.ColumnDefinitions.Clear();
                FilesList.RowDefinitions.Clear();
                rows = 0;

                //Set default values
                RowDefinition mainrow = new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star)
                };
                FilesList.RowDefinitions.Add(mainrow);
                FilesList.Height = 80;
                this.MaxHeight = 108;
                this.Height = 108;

                //Creating file with paths to icons
                if (!File.Exists(path + "\\iconcache.db"))
                    File.WriteAllText(path + "\\iconcache.db", "");
                //Read icon paths
                string[] tmp_iconcache = File.ReadAllLines(path + "\\iconcache.db");
                Dictionary<string, string> iconcache = new Dictionary<string, string>();

                string[] elements = Directory.EnumerateFiles(path + "\\" + name).ToArray();
                string[] directories = Directory.EnumerateDirectories(path + "\\" + name).ToArray();

                foreach (var cache in tmp_iconcache)
                {
                    iconcache.Add(cache.Split('*')[0], cache.Split('*')[1]);
                }

                //Pseudo-random icon id
                int ri = new Random().Next(0, 1000000);

                int j = 3;
                int n = 0;
                int m = 0;
                for (int i = 0; i < elements.Length; i++)
                {
                    SoftFileData sfd = new SoftFileData();
                    sfd.Name = elements[i];
                    sfd.Size = (int)new System.IO.FileInfo(elements[i]).Length;
                    filedata.Add(sfd);

                    string num = "";

                    if (!iconcache.ContainsKey(elements[i]))
                    {
                        IntPtr hIcon = Util.GetJumboIcon(Util.GetIconIndex(elements[i]));

                        // from native to managed
                        using (System.Drawing.Icon ico = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(hIcon).Clone())
                        {
                            // save to file (or show in a picture box)
                            if (!IsSmallIcon(ico.ToBitmap()))
                            {
                                ico.ToBitmap().Save(path + "\\TMP_ICON_" + ri + i, System.Drawing.Imaging.ImageFormat.Png);
                            }
                            else
                            {
                                System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(0, 0, 48, 48);
                                System.Drawing.Image img = ico.ToBitmap();
                                Util.CropImage(img, cropRect).Save(path + "\\TMP_ICON_" + ri + i, System.Drawing.Imaging.ImageFormat.Png);
                            }
                            File.AppendAllText(path + "\\iconcache.db", elements[i] + "*" + "\\TMP_ICON_" + ri + i + "\r\n");
                            num = "\\TMP_ICON_" + ri + i;
                        }

                        Util.Shell32.DestroyIcon(hIcon); // don't forget to cleanup
                    }
                    else
                    {
                        num = iconcache[elements[i]];
                    }

                    Image image = new Image
                    {
                        Width = 44,
                        Height = 44,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 1, 0, 19),
                        Source = Util.BitmapFromUri(new Uri(elements[i].Contains(".png") || elements[i].Contains(".jpg")
                        ? elements[i] : path + num // ICON
                        ))
                    };

                    string filetext = elements[i].Replace(path + "\\" + name + "\\", "").Replace(".lnk", "").Replace(".url", "");
                    TextBlock filename = new TextBlock
                    {
                        VerticalAlignment = VerticalAlignment.Bottom,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White,
                        Text = filetext.Length > 14 ? filetext.Remove(filetext.Length - (filetext.Length - 14)) + "..." : filetext
                    };

                    Grid button_content = new Grid();
                    button_content.Children.Add(image);
                    button_content.Children.Add(filename);

                    Button button = new Button
                    {
                        Content = button_content,
                        Background = Brushes.Transparent,
                        BorderBrush = Brushes.Transparent,
                        Name = "elementButton_" + i,
                        Tag = elements[i],
                        ToolTip = elements[i].Replace(path + "\\" + name + "\\", "").Replace(".lnk", " - Shortcut").Replace(".url", " - Internet Shortcut")
                    };
                    button.Click += ElementClicked;

                    Grid grid = new Grid
                    {
                        Width = 110,
                        Height = 68,
                        VerticalAlignment = VerticalAlignment.Top,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(0, 4, 10, 4),
                    };
                    Grid.SetColumn(grid, m);
                    grid.Children.Add(button);
                    m++;

                    ColumnDefinition column = new ColumnDefinition
                    {
                        Width = new GridLength(120, GridUnitType.Pixel)
                    };

                    Grid.SetRow(grid, n);
                    if (i == j)
                    {
                        m = 0;
                        j += 4;
                        n++;
                        rows++;
                    }
                    if (i == j - 4)
                    {
                        //this.MaxHeight += 80;
                        //this.Height += 80;
                        //ScrollFilesList.Height += 80;

                        FilesList.Height += 80;

                        RowDefinition row = new RowDefinition
                        {
                            Height = new GridLength(1, GridUnitType.Star)
                        };
                        FilesList.RowDefinitions.Add(row);
                    }

                    FilesList.ColumnDefinitions.Add(column);
                    FilesList.Children.Add(grid);
                }
                if (directories.Length != 0)
                    for (int i = 0; i < directories.Length; i++)
                    {
                        TextBlock image = new TextBlock
                        {
                            FontSize = 44,
                            FontFamily = new FontFamily("Segoe MDL2 Assets"),
                            Width = 44,
                            Height = 44,
                            Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 1, 0, 19),
                            Text = ""
                        };

                        string filetext = directories[i].Replace(path + "\\" + name + "\\", "");
                        TextBlock filename = new TextBlock
                        {
                            VerticalAlignment = VerticalAlignment.Bottom,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White,
                            Text = filetext.Length <= 14 ? filetext : filetext.Remove(filetext.Length - (filetext.Length - 14)) + "..."
                        };

                        Grid button_content = new Grid();
                        button_content.Children.Add(image);
                        button_content.Children.Add(filename);

                        Button button = new Button
                        {
                            Content = button_content,
                            Background = Brushes.Transparent,
                            BorderBrush = Brushes.Transparent,
                            Name = "elementButton_" + i,
                            Tag = directories[i],
                            ToolTip = directories[i].Replace(path + "\\" + name + "\\", "") + " - Directory"
                        };
                        button.Click += ElementClicked;

                        Grid grid = new Grid
                        {
                            Width = 110,
                            Height = 68,
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Margin = new Thickness(0, 4, 10, 4),
                        };
                        Grid.SetColumn(grid, m);
                        grid.Children.Add(button);
                        m++;

                        ColumnDefinition column = new ColumnDefinition
                        {
                            Width = new GridLength(120, GridUnitType.Pixel)
                        };

                        Grid.SetRow(grid, n);
                        if (i == j)
                        {
                            m = 0;
                            j += 4;
                            n++;
                            rows++;
                        }
                        if (i == j - 4)
                        {
                            //this.MaxHeight += 80;
                            //this.Height += 80;
                            //ScrollFilesList.Height += 80;

                            FilesList.Height += 80;

                            RowDefinition row = new RowDefinition
                            {
                                Height = new GridLength(1, GridUnitType.Star)
                            };
                            FilesList.RowDefinitions.Add(row);
                        }

                        FilesList.ColumnDefinitions.Add(column);
                        FilesList.Children.Add(grid);
                    }
                if (File.Exists(path + "\\" + name + "_fileupdate.json"))
                    File.Delete(path + "\\" + name + "_fileupdate.json");

                FileUpdateClass fileupdate = new FileUpdateClass
                {
                    Data = filedata.ToArray()
                };
                string json = JsonConvert.SerializeObject(fileupdate, Formatting.Indented);
                Console.WriteLine(json);
                File.WriteAllText(path + "\\" + name + "_fileupdate.json", json);
                this.MaxHeight += rows * 80;
                this.Height += rows * 80;
                this.ScrollFilesList.Height += rows * 80;
                foreach (var window in config.Windows)
                {
                    if (window.Name == name)
                    {
                        id = window.Id;
                        if (window.CollapsedRows > 0)
                        {
                            //this.MaxHeight = window.CollapsedRows;
                            this.Height = window.CollapsedRows;
                            this.ScrollFilesList.Height = window.CollapsedRows;
                        }
                    }
                }
                if (this.Height >= System.Windows.SystemParameters.PrimaryScreenHeight - 20)
                {
                    this.Height = System.Windows.SystemParameters.PrimaryScreenHeight / 2;
                    this.ScrollFilesList.Height = System.Windows.SystemParameters.PrimaryScreenHeight / 2;
                }
            }
            catch (FileNotFoundException fnfex)
            {
                File.WriteAllText(config_path + "\\Error.log", fnfex.ToString());
                tries++;
                ResetIconCache();
                if (tries == 1)
                    ReadElements();
                else
                    throw fnfex;

            }
            catch (Exception ex)
            {
                File.WriteAllText(config_path + "\\Error.log", ex.ToString());
                throw ex;
            }
            loadinginfo.Visibility = Visibility.Collapsed;
            FilesList.Visibility = Visibility.Visible;
            this.Show();
            this.Visibility = Visibility.Visible;
            isLoading = false;
            tries = 0;
        }

        private void ResetIconCache()
        {
            foreach (var file in Directory.EnumerateFiles(path))
            {
                if (file.Contains("TMP_ICON"))
                {
                    File.Delete(file);
                }
            }
            File.Delete(path + "\\iconcache.db");
        }

        private void ElementClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start((string)(((Button)sender).Tag));
            }
            catch (Win32Exception ex)
            {
                File.WriteAllText(config_path + "\\Error.log", ex.ToString());
                ErrorWindow ew = new ErrorWindow();
                ew.ExceptionReason = ex;
                ew.ExceptionString = ex.ToString()+$"\r\nWhile opening: {(string)(((Button)sender).Tag)} program caused an error.\r\nIf this issue appears too often, please add issue to github.";//$"File {(string)(((Button)sender).Tag)} can't be opened by default application(s).\r\nIf this issue appears too often, please add issue to github.";
                ew.Show();
            }
            catch (Exception ex)
            {
                File.WriteAllText(config_path + "\\Error.log", ex.ToString());
                ErrorWindow ew = new ErrorWindow();
                ew.ExceptionReason = ex;
                ew.Show();
            }
        }

        private void MoveAction(object sender, MouseEventArgs e)
        {
            if (isMoving)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    Left = (Util.GetMousePosition().X + MousePos.X);
                    Top = (Util.GetMousePosition().Y + MousePos.Y);
                }
                else
                {
                    if (Util.IsEvenX((int)(Util.GetMousePosition().X + MousePos.X),4))
                        Left = (int)(Util.GetMousePosition().X + MousePos.X);
                    if (Util.IsEvenX((int)(Util.GetMousePosition().Y + MousePos.Y),4))
                        Top = (int)(Util.GetMousePosition().Y + MousePos.Y);
                }
            }
        }

        private void MoveActionStop(object sender, MouseButtonEventArgs e)
        {
            isMoving = false;
            //Saving window position
            ConfigClass config = Config.GetConfig();
            foreach (var window in config.Windows)
            {
                if (window.Name == name)
                {
                    window.Position = new WindowPosition { X = this.Left, Y = this.Top };
                }
            }
            bool result = Config.SetConfig(config);
            if (result == false)
            {
                Util.Reconfigurate();
            }
        }

        public static void Save(BitmapSource image, string filePath)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }

        private void HideTileButton(object sender, RoutedEventArgs e)
        {
            if (isHidded)
            {
                this.Height = lastHeight;
                hideBtn.Content = "";
                isHidded = false;
                chromewindow.ResizeBorderThickness = new Thickness(6);
            }
            else
            {
                lastHeight = (int)this.Height;
                this.Height = 28;
                hideBtn.Content = "";
                isHidded = true;
                chromewindow.ResizeBorderThickness = new Thickness(0);
            }
        }

        private void OpenDirectoryBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(path+"\\"+name);
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isEditMode)
            {
                MoveRectangle.Visibility = Visibility.Collapsed;
                folderNameTB.Visibility = Visibility.Collapsed;
                editBox.Visibility = Visibility.Visible;
                isEditMode = true;
            }
            else
            {
                MoveRectangle.Visibility = Visibility.Visible;
                folderNameTB.Visibility = Visibility.Visible;
                editBox.Visibility = Visibility.Collapsed;
                if (!Regex.IsMatch(editBox.Text, @"\<|\>|\\|\/|\*|\?|\||:") && name != editBox.Text)
                {
                    folderNameTB.Text = editBox.Text;
                    ChangeTileName();
                }
                else
                {
                    editBox.Text = name;
                    folderNameTB.Text = name;
                }
                isEditMode = false;
            }
        }

        private void ChangeTileName()
        {
            newname = editBox.Text;
            Directory.Move(path + "\\" + name, path + "\\" + newname);
            name = newname;
        }

        private void RotateBtn_Click(object sender, RoutedEventArgs e)
        {
            ConfigClass config = Config.GetConfig();
            foreach (var window in config.Windows)
            {
                if (window.Name == name)
                {
                    if (!window.EditBar)
                    {
                        window.EditBar = true;
                        rd.Height = new GridLength(28);
                        trd.Height = GridLength.Auto;
                        Grid.SetRow(ActionGrid, 0);
                        Grid.SetRow(ScrollFilesList, 1);
                    }
                    else
                    {
                        window.EditBar = false;
                        rd.Height = GridLength.Auto;
                        trd.Height = new GridLength(28);
                        Grid.SetRow(ActionGrid, 1);
                        Grid.SetRow(ScrollFilesList, 0);
                    }
                }
            }
            bool result = Config.SetConfig(config);
            if (result == false)
            {
                Util.Reconfigurate();
            }
        }

        private void RemoverowBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ScrollFilesList.Height > 80)
            {
                ScrollFilesList.Height -= 80;
                this.Height -= 80;
                ConfigClass config = Config.GetConfig();
                foreach (var window in config.Windows)
                {
                    if (window.Name == name)
                    {
                        window.CollapsedRows++;
                    }
                }
                bool result = Config.SetConfig(config);
                if (result == false)
                {
                    Util.Reconfigurate();
                }
            }
        }

        private void AddrowBtn_Click(object sender, RoutedEventArgs e)
        {
            ConfigClass config = Config.GetConfig();
            foreach (var window in config.Windows)
            {
                if (window.Name == name)
                {
                    if (window.CollapsedRows > 0)
                    {
                        window.CollapsedRows--;
                        ScrollFilesList.Height += 80;
                        this.Height += 80;
                    }
                }
            }
            bool result = Config.SetConfig(config);
            if (result == false)
            {
                Util.Reconfigurate();
            }
        }

        private void ActionGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            hideBtn.Visibility = Visibility.Visible;
            gotodirectoryBtn.Visibility = Visibility.Visible;
            editBtn.Visibility = Visibility.Visible;
            rotateBtn.Visibility = Visibility.Visible;
            moveableinfo.Visibility = Visibility.Visible;
        }

        private void ActionGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            hideBtn.Visibility = Visibility.Collapsed;
            gotodirectoryBtn.Visibility = Visibility.Collapsed;
            editBtn.Visibility = Visibility.Collapsed;
            rotateBtn.Visibility = Visibility.Collapsed;
            moveableinfo.Visibility = Visibility.Collapsed;
        }
    }
}
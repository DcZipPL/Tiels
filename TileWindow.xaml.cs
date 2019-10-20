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
using DWinOverlay.Classes;
using System.Text.RegularExpressions;

namespace DWinOverlay
{
    /// <summary>
    /// Logika interakcji dla klasy TileWindow.xaml
    /// </summary>
    public partial class TileWindow : Window
    {
        public string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Tiles";
        public string name = null;
        private string newname = null;
        private bool isMoving = false;
        private Point MousePos;

        int tries = 0;
        private int lastHeight = 0;
        public bool isHidded = false;
        public bool isEditMode = false;

        List<SoftFileData> filedata = new List<SoftFileData>();

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

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private void MoveActionStart(object sender, MouseButtonEventArgs e)
        {
            isMoving = true;
            MousePos = Util.GetMousePosition();
            MousePos.X = Convert.ToInt32(this.Left) - MousePos.X;
            MousePos.Y = Convert.ToInt32(this.Top) - MousePos.Y;
        }
        private void MoveActionCancel(object sender, MouseEventArgs e) => MoveActionStop(this, null);
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            CheckFileUpdates();
            //SetBottom(this);
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

        private void TileLoaded(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);

            Util.EnableBlur(this);
            SetBottom(this);

            ConfigClass config = Config.GetConfig();
            MainGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(config.Color));

            folderNameTB.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
            hideBtn.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
            gotodirectoryBtn.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
            editBtn.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
            rotateBtn.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
            editBox.Text = name;
            ReadElements();
        }

        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        public static void SetBottom(Window window)
        {
            /*IntPtr hWnd = new WindowInteropHelper(window).Handle;
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);*/
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
            filedata.Clear();

            FilesList.Children.Clear();
            FilesList.ColumnDefinitions.Clear();
            FilesList.RowDefinitions.Clear();
            RowDefinition mainrow = new RowDefinition
            {
                Height = new GridLength(1, GridUnitType.Star)
            };
            FilesList.RowDefinitions.Add(mainrow);
            FilesList.Height = 80;
            this.Height = 108;
            if (!File.Exists(path + "\\iconcache.db"))
                File.WriteAllText(path + "\\iconcache.db", "");
            string[] tmp_iconcache = File.ReadAllLines(path + "\\iconcache.db");
            Dictionary<string,string> iconcache = new Dictionary<string,string>();
            string[] elements = Directory.EnumerateFiles(path + "\\" + name).ToArray(); // Elements link
            string[] directories = Directory.EnumerateDirectories(path + "\\" + name).ToArray();

            ConfigClass config = Config.GetConfig();

            foreach (var cache in tmp_iconcache)
            {
                iconcache.Add(cache.Split('*')[0], cache.Split('*')[1]);
            }

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
                            Util.CropImage(img,cropRect).Save(path + "\\TMP_ICON_" + ri + i, System.Drawing.Imaging.ImageFormat.Png);
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
                    Text = filetext.Length > 14 ? filetext.Remove(filetext.Length - (filetext.Length - 14)) +"..." : filetext
                };

                Grid button_content = new Grid();
                button_content.Children.Add(image);
                button_content.Children.Add(filename);

                Button button = new Button
                {
                    Content = button_content,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    Name = "elementButton_"+i,
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
                }
                if (i == j - 4)
                {
                    this.Height += 80;
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
                        Text = filetext.Length <= 14 ? filetext : filetext.Remove(filetext.Length - (filetext.Length - 14)) +"..."
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
                        ToolTip = directories[i].Replace(path + "\\" + name + "\\", "")+" - Directory"
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
                    }
                    if (i == j - 4)
                    {
                        this.Height += 80;
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
        }

        private void ElementClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start((string)(((Button)sender).Tag));
            }
            catch (Win32Exception ex)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.ExceptionReason = ex;
                ew.ExceptionString = ex.ToString()+$"\r\nWhile opening: {(string)(((Button)sender).Tag)} program caused an error.\r\nIf this issue appears too often, please add issue to github.";//$"File {(string)(((Button)sender).Tag)} can't be opened by default application(s).\r\nIf this issue appears too often, please add issue to github.";
                ew.Show();
            }
            catch (Exception ex)
            {
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
                    if (Util.IsEvenFour((int)(Util.GetMousePosition().X + MousePos.X)))
                        Left = (int)(Util.GetMousePosition().X + MousePos.X);
                    if (Util.IsEvenFour((int)(Util.GetMousePosition().Y + MousePos.Y)))
                        Top = (int)(Util.GetMousePosition().Y + MousePos.Y);
                }
            }
        }

        private void MoveActionStop(object sender, MouseButtonEventArgs e)
        {
            isMoving = false;
            //Saving Window Position
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
            }
            else
            {
                lastHeight = (int)this.Height;
                this.Height = 28;
                hideBtn.Content = "";
                isHidded = true;
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

        private void ResizeActionStart(object sender, MouseButtonEventArgs e)
        {

        }

        private void ResizeAction(object sender, MouseEventArgs e)
        {

        }

        private void ResizeActionStop(object sender, MouseButtonEventArgs e)
        {

        }

        private void RotateBtn_Click(object sender, RoutedEventArgs e)
        {
            ConfigClass config = Config.GetConfig();
            foreach (var window in config.Windows)
            {
                if (window.Name == name)
                {
                    if (window.EditBar)
                    {
                        window.EditBar = false;
                        rd.Height = new GridLength(28);
                        trd.Height = GridLength.Auto;
                        Grid.SetRow(ActionGrid, 0);
                        Grid.SetRow(FilesList, 1);
                    }
                    else
                    {
                        window.EditBar = true;
                        rd.Height = GridLength.Auto;
                        trd.Height = new GridLength(28);
                        Grid.SetRow(ActionGrid, 1);
                        Grid.SetRow(FilesList, 0);
                    }
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
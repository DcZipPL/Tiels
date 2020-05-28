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
using Microsoft.Win32;
using Peter;
using System.Security.Permissions;

namespace Tiels
{
    /// <summary>
    /// Logika interakcji dla klasy TileWindow.xaml
    /// </summary>
    public partial class TileWindow : Window
    {
        public string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Tiles";
        public string name = null;
        private string newname = null;

        public int rows = 0;
        public int collumns = 4;
        public int lastHeight = 0;
        public int fixedLastHeight = 0;
        private int tries = 0;
        private static int id = 0;

        private bool isMoving = false;
        public bool isLoading = false;
        public bool isHidded = false;
        public bool isEditMode = false;

        public bool isFullyInitalized = false;

        public string lastFile = "";
        public FileInfo fi;

        private Point MousePos;

        protected Dictionary<string, ChangedType> fileupdates = new Dictionary<string, ChangedType>();

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
        }

        private void AddHandler()
        {
            //AddHandler(Mouse.PreviewMouseDownOutsideCapturedElementEvent, new MouseButtonEventHandler(HandleClickOutsideOfControl), true);
            AddHandler(Mouse.PreviewMouseUpOutsideCapturedElementEvent, new MouseButtonEventHandler(HandleClickOutsideOfControl), true);
            AddHandler(Mouse.PreviewMouseMoveEvent, new MouseEventHandler(HandleMouseOutsideOfControl), true);
        }

        private void HandleClickOutsideOfControl(object sender, MouseButtonEventArgs e)
        {
            //do stuff (eg close drop down)
            MoveActionStop(this, null);
            ReleaseMouseCapture();
        }

        private void HandleMouseOutsideOfControl(object sender, MouseEventArgs e)
        {
            //do stuff (eg close drop down)
            //MoveActionStop(this, null);
            //ReleaseMouseCapture();
            if (isMoving)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    Left = (Util.GetMousePosition().X + MousePos.X);
                    Top = (Util.GetMousePosition().Y + MousePos.Y);
                }
                else
                {
                    if (Util.IsEvenX((int)(Util.GetMousePosition().X + MousePos.X), 4))
                        Left = (int)(Util.GetMousePosition().X + MousePos.X);
                    if (Util.IsEvenX((int)(Util.GetMousePosition().Y + MousePos.Y), 4))
                        Top = (int)(Util.GetMousePosition().Y + MousePos.Y);
                }
            }
        }

        private void MoveActionStart(object sender, MouseButtonEventArgs e)
        {
            isMoving = true;
            Mouse.Capture(this, CaptureMode.Element);
            Mouse.Capture(this,CaptureMode.SubTree);
            AddHandler();
            MousePos = Util.GetMousePosition();
            MousePos.X = Convert.ToInt32(this.Left) - MousePos.X;
            MousePos.Y = Convert.ToInt32(this.Top) - MousePos.Y;
        }

        private void MoveActionCancel(object sender, MouseEventArgs e) { }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void WatchFileUpdates()
        {
            // Create a new FileSystemWatcher and set its properties.
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = path+"\\"+ name;

            // Watch for changes in LastAccess and LastWrite times, and
            // the renaming of files or directories.
            watcher.NotifyFilter = NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.Size;

            // Only watch text files.
            //watcher.Filter = "*.*";

            // Add event handlers.
            watcher.Changed += OnEntryChanged;
            watcher.Created += OnEntryChanged;
            watcher.Deleted += OnEntryDeleted;
            watcher.Renamed += OnEntryRenamed;

            // Begin watching.
            watcher.EnableRaisingEvents = true;

        }

        // Define the event handlers.
        private void OnEntryChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed or created.
            if (lastFile == "" || lastFile != e.FullPath || (fi.Exists && new FileInfo(e.FullPath).Exists && (new FileInfo(e.FullPath).Length != fi.Length || new FileInfo(e.FullPath).CreationTime != fi.CreationTime || new FileInfo(e.FullPath).LastAccessTime != fi.LastAccessTime)))
            {
                lastFile = e.FullPath;
                fi = new FileInfo(e.FullPath);
                if (e.ChangeType == WatcherChangeTypes.Created)
                {
                    ErrorHandler.Info($"File: {e.FullPath} {e.ChangeType} -> Created");
                    FileUpdates.CreateEntry(e.FullPath + "\t" + FileUpdates.NextID());
                }
                else
                {
                    ErrorHandler.Info($"File: {e.FullPath} {e.ChangeType} -> Changed");
                    FileUpdates.ChangeEntry(e.FullPath + "\t" + FileUpdates.NextID());
                }
            }
        }

        private void OnEntryRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            ErrorHandler.Info($"File: {e.OldFullPath} renamed to {e.FullPath}");
            FileUpdates.DeleteEntry(e.OldFullPath + "\t" + FileUpdates.NextID());
            FileUpdates.CreateEntry(e.FullPath + "\t" + FileUpdates.NextID());
        }

        private void OnEntryDeleted(object source, FileSystemEventArgs e)
        {
            ErrorHandler.Info($"File: {e.FullPath} {e.ChangeType} -> Deleted");
            //fileupdates.Add(e.FullPath, ChangedType.DELETED);
            FileUpdates.DeleteEntry(e.FullPath + "\t" + FileUpdates.NextID());
        }

        protected enum ChangedType
        {
            CREATED,
            CHANGED,
            DELETED
        }

        private void CheckFileUpdates()
        {
            /*if (File.Exists(path + "\\" + name + "_fileupdate.json") && Directory.Exists(path+"\\"+name))
            {
                try
                {
                    //ReadElements();
                }
                catch(Exception ex)
                {
                    File.AppendAllText(App.config_path + "\\Error.log", "\r\n[Error: "+DateTime.Now+"] "+ex.ToString());
                    if (tries <= 3)
                    {
                        ReadElements();
                        tries++;
                    }
                    else
                    {
                        ErrorWindow ew = new ErrorWindow();
                        ew.ExceptionReason = ex;
                        ew.ShowDialog();
                    }
                }
            }*/
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
                    if (Height != fixedLastHeight)
                    {
                        fixedLastHeight = (Int32)this.Height;
                        ConfigClass config = Config.GetConfig();
                        if (config != null)
                        {
                            foreach (var window in config.Windows)
                            {
                                if (window.Name == name)
                                {
                                    if (!isHidded)
                                        window.Height = (int)this.Height;
                                }
                            }
                            if (isFullyInitalized)
                            {
                                bool result = Config.SetConfig(config);
                                if (result == false)
                                {
                                    ErrorHandler.Error();
                                }
                            }
                            else
                                isFullyInitalized = true;
                        }
                    }
                }
                CheckWindowWidth();
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

        private void CheckWindowWidth()
        {
            bool isChanged = false;
            ConfigClass config = Config.GetConfig();
            if (isLoading == false)
            {
                foreach (var window in config.Windows)
                {
                    if (window.Name == name)
                    {
                        if (window.Width != (int)this.Width)
                        {
                            window.Width = (int)this.Width;
                            isChanged = true;
                        }
                    }
                }
                if (isChanged)
                {
                    bool result = Config.SetConfig(config);
                    if (result == false)
                    {
                        ErrorHandler.Error();
                    }
                }
            }

            int floor = (int)Math.Floor(this.Width / 120);
            if (floor != collumns)
            {
                collumns = floor;
                int floor1 = (int)Math.Floor(this.Height / 80);
                ReadElements(floor,floor1);
            }
        }

        private void TileLoaded(object sender, RoutedEventArgs e)
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

                int floor = (int)Math.Floor(this.Width / 120);
                collumns = floor;
                int floor1 = (int)Math.Floor(this.Height / 80);
                _ = ReadElements(floor, floor1);
                WatchFileUpdates();
            }
            catch (Exception ex)
            {
                ErrorHandler.Log(ex);
            }
        }

        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        public static void SetBottom(Window window)
        {
            //TODO: Move to bottom but with id
            IntPtr hWnd = new WindowInteropHelper(window).Handle;
            Util.SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, Util.SWP_NOSIZE | Util.SWP_NOMOVE | Util.SWP_NOACTIVATE);
        }

        public async Task ReadElements(int collumCount = 4, int rowCount = 1)
        {
            try
            {
                isLoading = true;
                ConfigClass config = Config.GetConfig();
                double tmp_height;
                double tmp_max_height;

                JsonWindow window = null;
                foreach (var in_window in config.Windows)
                {
                    if (in_window.Name == name)
                    {
                        window = in_window;
                    }
                }
                if (window.Hidden)
                {
                    isHidded = true;
                    this.Height = 28;
                }

                FilesList.Visibility = Visibility.Hidden;
                loadinginfo.Visibility = Visibility.Visible;
                await Task.Delay(100);

                FilesList.Children.Clear();
                FilesList.ColumnDefinitions.Clear();
                FilesList.RowDefinitions.Clear();
                rows = rowCount;

                //Set default values
                RowDefinition mainrow = new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star)
                };
                FilesList.RowDefinitions.Add(mainrow);
                FilesList.Height = 80;
                tmp_max_height = rowCount * 80 + 28;
                tmp_height = rowCount * 80 + 28;

                //Creating file and directory with paths to icons
                if (!Directory.Exists(App.config_path + "\\Cache"))
                    Directory.CreateDirectory(App.config_path + "\\Cache");
                if (!File.Exists(App.config_path + "\\Cache\\iconcache.db"))
                    File.WriteAllText(App.config_path + "\\Cache\\iconcache.db", "");
                //Read icon paths
                string[] tmp_iconcache = File.ReadAllLines(App.config_path + "\\Cache\\iconcache.db");
                Dictionary<string, string> iconcache = new Dictionary<string, string>();

                string[] elements = Directory.EnumerateFiles(path + "\\" + name).ToArray();
                string[] directories = Directory.EnumerateDirectories(path + "\\" + name).ToArray();

                foreach (var cache in tmp_iconcache)
                {
                    iconcache.Add(cache.Split('*')[0], cache.Split('*')[1]);
                }

                int skipframe = 0;

                //Pseudo-random icon id
                int ri = new Random().Next(0, 1000000);

                int j = 0;
                int n = 0;
                int m = 0;
                for (int i = 0; i < elements.Length; i++)
                {
                    //await Task.Delay(5);

                    string num = "";

                    if (!iconcache.ContainsKey(elements[i]))
                    {
                        num = FileUpdates.GenerateIcons(elements[i], App.config_path,ri,i);
                    }
                    else
                    {
                        num = iconcache[elements[i]];
                    }

                    //Util.ResizeImage(Util.BitmapFromSource((BitmapSource)bitmap), (int)bitmap.Width <= 128 ? (int)bitmap.Width : ((int)bitmap.Width/10), (int)bitmap.Height <= 128 ? (int)bitmap.Height : (int)bitmap.Height/10);
                    Image image = new Image
                    {
                        Width = 44,
                        Height = 44,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 1, 0, 19),
                        Source = Util.BitmapFromUri(new Uri(App.config_path + num))
                        //Source = elements[i].ToLower().Contains(".png") || elements[i].ToLower().Contains(".jpg") || elements[i].ToLower().Contains(".jpeg") ? Util.ImageSourceFromBitmap(bitmap2) : Util.BitmapFromUri(new Uri(path + num)) // ICON
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
                    button.MouseRightButtonUp += (sender, e) =>
                    {
                        ShellContextMenu menu = new ShellContextMenu();
                        FileInfo[] arrFI = new FileInfo[1];
                        arrFI[0] = new FileInfo((string)((Button)sender).Tag);

                        menu.ShowContextMenu(arrFI, System.Windows.Forms.Control.MousePosition);
                    };

                    SortGrid(collumCount, i, button, ref j, ref n, ref m);
                }
                #region Old (Needs Refactor)
                //TODO: REFACTOR THIS
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

                        SortGrid(collumCount, i, button, ref j, ref n, ref m);
                    }
                #endregion
                tmp_max_height = System.Windows.SystemParameters.PrimaryScreenHeight / 1.025;
                tmp_height = rows * 80 + 28;
                this.ScrollFilesList.Height += rows * 80;

                id = window.Id;
                if (window.Height > 0)
                {
                    //tmp_max_height = window.CollapsedRows;
                    tmp_height = window.Height;
                    this.ScrollFilesList.Height = window.Height;
                }
                if (window.Hidden)
                {
                    tmp_height = 28;
                    this.lastHeight = window.Height;
                    this.hideBtn.Content = "";
                    this.chromewindow.ResizeBorderThickness = new Thickness(0);
                }

                if (tmp_height >= System.Windows.SystemParameters.PrimaryScreenHeight - 20)
                {
                    tmp_height = System.Windows.SystemParameters.PrimaryScreenHeight / 2;
                    this.ScrollFilesList.Height = System.Windows.SystemParameters.PrimaryScreenHeight / 2;
                }

                this.MaxHeight = tmp_max_height;
                this.Height = tmp_height;
            }
            catch (FileNotFoundException fnfex)
            {
                File.AppendAllText(App.config_path + "\\Error.log", "\r\n[Error: " + DateTime.Now + "] " + fnfex.ToString());
                tries++;
                ResetIconCache();
                if (tries == 1)
                    ReadElements();
                else
                    throw fnfex;

            }
            /*catch (Exception ex)
            {
                ErrorHandler.Log(ex);
                throw ex;
            }*/

            loadinginfo.Visibility = Visibility.Collapsed;
            FilesList.Visibility = Visibility.Visible;
            this.Show();
            this.Visibility = Visibility.Visible;
            isLoading = false;
            tries = 0;
            GC.Collect();
            //filedata.Clear();
        }

        private void SortGrid(int columnCount, int i, Button button, ref int j, ref int n, ref int m)
        {
            //j = columnCount - 1;
            //n, rows = Current Row
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

            if (i == j + columnCount - 1)
            {
                m = 0;
                j += columnCount;
                n++;
                rows++;

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

        private void Mi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start((string)((MenuItem)sender).Tag);
            }
            catch (Win32Exception ex)
            {
                ErrorHandler.Log(ex);
                ErrorWindow ew = new ErrorWindow();
                ew.ExceptionReason = ex;
                ew.ExceptionString = ex.ToString() + $"\r\n{(string)(((MenuItem)sender).Tag)} can't be opened by default application(s)";//$"File {(string)(((Button)sender).Tag)} can't be opened by default application(s).\r\nIf this issue appears too often, please add issue to github.";
                ew.ShowDialog();
            }
            catch (Exception ex)
            {
                ErrorHandler.Log(ex);
                ErrorWindow ew = new ErrorWindow();
                ew.ExceptionReason = ex;
                ew.ShowDialog();
            }
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
                ErrorHandler.Log(ex);
                ErrorWindow ew = new ErrorWindow();
                ew.ExceptionReason = ex;
                ew.ExceptionString = ex.ToString()+$"\r\n{(string)(((Button)sender).Tag)} can't be opened by default application(s)";
                ew.ShowDialog();
            }
            catch (Exception ex)
            {
                ErrorHandler.Log(ex);
                ErrorWindow ew = new ErrorWindow();
                ew.ExceptionReason = ex;
                ew.ShowDialog();
            }
        }

        private void MoveAction(object sender, MouseEventArgs e)
        {

        }

        private void MoveActionStop(object sender, MouseButtonEventArgs e)
        {
            isMoving = false;

            ReleaseMouseCapture();
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
                ErrorHandler.Error();
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

                if (!isLoading)
                {
                    ConfigClass config = Config.GetConfig();
                    foreach (var window in config.Windows)
                    {
                        if (window.Name == name)
                        {
                            window.Hidden = false;
                        }
                    }
                    bool result = Config.SetConfig(config);
                    if (result == false)
                    {
                        ErrorHandler.Error();
                    }
                }
            }
            else
            {
                lastHeight = (int)this.Height;
                this.Height = 28;
                hideBtn.Content = "";
                isHidded = true;
                chromewindow.ResizeBorderThickness = new Thickness(0);

                if (!isLoading)
                {
                    ConfigClass config = Config.GetConfig();
                    foreach (var window in config.Windows)
                    {
                        if (window.Name == name)
                        {
                            window.Hidden = true;
                        }
                    }
                    bool result = Config.SetConfig(config);
                    if (result == false)
                    {
                        ErrorHandler.Error();
                    }
                }
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
                ErrorHandler.Error();
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
                        window.Height++;
                    }
                }
                bool result = Config.SetConfig(config);
                if (result == false)
                {
                    ErrorHandler.Error();
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
                    if (window.Height > 0)
                    {
                        window.Height--;
                        ScrollFilesList.Height += 80;
                        this.Height += 80;
                    }
                }
            }
            bool result = Config.SetConfig(config);
            if (result == false)
            {
                ErrorHandler.Error();
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

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(path + "\\" + name);
        }

        private void MenuItemOpenCMD_Click(object sender, RoutedEventArgs e)
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                WorkingDirectory = path + "\\" + name,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal,
                FileName = "cmd.exe",
                RedirectStandardInput = false,
                UseShellExecute = false
            };

            Process.Start(startInfo);
        }

        private void MenuItemOpenDebug_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow();
            about.Show();
            //ReadElements(4);
        }

        private void WindowMouseUp(object sender, MouseButtonEventArgs e)
        {
            MoveActionStop(this, null);
            ReleaseMouseCapture();
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            foreach (string fileToCopy_path in ((string[])e.Data.GetData(DataFormats.FileDrop)))
            {
                //ErrorHandler.Info(path + "\\" + name + " : " + fileToCopy_path);
                File.Copy(fileToCopy_path, path + "\\" + name + "\\" + System.IO.Path.GetFileName(fileToCopy_path));
            }
            //Application.Current.Windows.OfType<MainWindow>().FirstOrDefault().fu.UpdateEntries();
            ReadElements(collumns,rows);
        }
    }
}
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

namespace DWinOverlay
{
    /// <summary>
    /// Logika interakcji dla klasy TileWindow.xaml
    /// </summary>
    public partial class TileWindow : Window
    {
        protected string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Tiles";
        public string name = null;
        private bool isMoving = false;
        //private bool isResizing = false;

        #region DLL IMPORTS

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
        public static Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        //BlurHelper
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            // ...
            WCA_ACCENT_POLICY = 19
            // ...
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_INVALID_STATE = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        internal void EnableBlur()
        {
            var windowHelper = new WindowInteropHelper(this);

            var accent = new AccentPolicy();
            var accentStructSize = Marshal.SizeOf(accent);
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        #endregion

        public TileWindow()
        {
            InitializeComponent();
        }

        public static string GetShortcutTargetFile(string shortcutFilename)
        {
            Console.WriteLine(File.ReadAllText(shortcutFilename));
            string pathOnly = System.IO.Path.GetDirectoryName(shortcutFilename);
            string filenameOnly = System.IO.Path.GetFileName(shortcutFilename);

            Shell shell = new Shell();
            Folder folder = shell.NameSpace(pathOnly);
            FolderItem folderItem = folder.ParseName(filenameOnly);
            if (folderItem != null)
            {
                Shell32.ShellLinkObject link = (Shell32.ShellLinkObject)folderItem.GetLink;
                return link.Path;
            }

            return string.Empty;
        }

        private void TileLoaded(object sender, RoutedEventArgs e)
        {
            EnableBlur();
            string[] tiles = Directory.EnumerateDirectories(path).ToArray();

            if (File.Exists(path + "\\PositionData.dat"))
            {
                string posString = File.ReadAllText(path + "\\PositionData.dat"); // Input {FOLDERNAME}:{X*25}:{Y*25} ex. (Files X=120 Y=60) Files:3000:1500;
                string[] positions = posString.Replace("\r", "").Replace("\n", "").Replace(" ", "").Split(';');
                foreach (string position in positions)
                {
                    string[] splitedpos = position.Split(':');
                    if (position != "")
                    {
                        //string name = splitedpos[0];
                        string z = splitedpos[1];
                        string y = splitedpos[2];
                        Top = int.Parse(y) / 25;
                        Left = int.Parse(y) / 25;
                    }
                }
            }
            else
            {
                File.WriteAllText(path + "\\PositionData.dat", folderNameTB.Text + ":" + Left * 25 + ":" + Top * 25 + ";");
            }
            ReadElements();
        }

        private void ReadElements()
        {
            string[] elements = Directory.EnumerateFiles(path + "\\" + name).ToArray();

            int ri = new Random().Next(0, 1000000);

            for (int i = 0; i < elements.Length; i++)
            {
                System.Drawing.Icon ico = null;

                if (elements[i].Contains(".lnk")||elements[i].Contains(".url"))
                {
                    ico = ExtractIcon.GetIcon(GetShortcutTargetFile(elements[i]), false);
                    ico.ToBitmap().Save(path + "\\TMP_ICON_" + ri + i);
                }

                Image image = new Image
                {
                    Width = 44,
                    Height = 44,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 1, 0, 19),
                    Source = new BitmapImage(new Uri(elements[i].Contains(".lnk")|| elements[i].Contains(".url")
                    ? path + "\\TMP_ICON_" + ri + i : elements[i].Contains(".png")
                    ? elements[i] : path + "//unknown.png"
                    ))
                };

                TextBlock filename = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Brushes.White,
                    Text = elements[i].Replace(path + "\\" + name + "\\", "").Replace(".lnk", "").Replace(".png", "")
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
                    Tag = elements[i]
                };
                button.Click += ElementClicked;

                Grid grid = new Grid
                {
                    Width = 110,
                    Height = 68,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 10, 10, 10),
                };
                Grid.SetColumn(grid, i);
                grid.Children.Add(button);

                ColumnDefinition column = new ColumnDefinition
                {
                    Width = new GridLength(110, GridUnitType.Pixel)
                };

                FilesList.ColumnDefinitions.Add(column);
                FilesList.Children.Add(grid);
            }
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
                ew.ExceptionString = $"File {(string)(((Button)sender).Tag)} can't be opened by default application(s).\r\nIf you will to open in File Explorer at Default you can change in Settings Menu.\r\nIf this issue appears too often, please add issue to github.";
                ew.Show();
            }
            catch (Exception ex)
            {
                ErrorWindow ew = new ErrorWindow();
                ew.ExceptionReason = ex;
                ew.Show();
            }
        }

        private void MoveActionStart(object sender, MouseButtonEventArgs e) => isMoving = true;

        private void MoveAction(object sender, MouseEventArgs e)
        {
            if (isMoving)
            {
                Top = GetMousePosition().Y - Height + 10;
                Left = GetMousePosition().X - 400;
            }
        }

        private void MoveActionStop(object sender, MouseButtonEventArgs e)
        {
            isMoving = false;
            File.WriteAllText(path + "\\PositionData.dat", folderNameTB.Text + ":" + Left * 25 + ":" + Top * 25 + ";");
        }

        private void MoveActionCancel(object sender, MouseEventArgs e) => MoveActionStop(this, null);

        /*private void ResizeStart(object sender, MouseButtonEventArgs e) { isResizing = true; isMoving = false; }

        private void ResizeStop(object sender, MouseButtonEventArgs e)
        {
            isResizing = false;
        }

        private void ResizeAction(object sender, MouseEventArgs e)
        {
            double boxesSizeWidth = Width;
            double boxesSizeHeight = Height;
            double width = e.GetPosition(this).X;
            double height = e.GetPosition(this).Y;
            if (isResizing)
            {
                height += 100;
                if (height > 0)
                {
                    boxesSizeHeight = height;
                }
                Width = boxesSizeWidth;
                Height = boxesSizeHeight;
                MainGrid.Height = height - 12;
            }
        }

        private void ResizeCancel(object sender, MouseEventArgs e) => ResizeStop(this, null);*/
    }
}

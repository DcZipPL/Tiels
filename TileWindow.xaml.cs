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

namespace DWinOverlay
{
    /// <summary>
    /// Logika interakcji dla klasy TileWindow.xaml
    /// </summary>
    public partial class TileWindow : Window
    {
        public string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Tiles";
        public string name = null;
        private bool isMoving = false;

        int tries = 0;
        private int lastHeight = 0;
        public bool isHidded = false;

        List<SoftFileData> filedata = new List<SoftFileData>();

        #region DLL IMPORTS

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X,
           int Y, int cx, int cy, uint uFlags);

        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

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

        [Flags]
        enum SHGFI : uint
        {
            /// <summary>get icon</summary>
            Icon = 0x000000100,
            /// <summary>get display name</summary>
            DisplayName = 0x000000200,
            /// <summary>get type name</summary>
            TypeName = 0x000000400,
            /// <summary>get attributes</summary>
            Attributes = 0x000000800,
            /// <summary>get icon location</summary>
            IconLocation = 0x000001000,
            /// <summary>return exe type</summary>
            ExeType = 0x000002000,
            /// <summary>get system icon index</summary>
            SysIconIndex = 0x000004000,
            /// <summary>put a link overlay on icon</summary>
            LinkOverlay = 0x000008000,
            /// <summary>show icon in selected state</summary>
            Selected = 0x000010000,
            /// <summary>get only specified attributes</summary>
            Attr_Specified = 0x000020000,
            /// <summary>get large icon</summary>
            LargeIcon = 0x000000000,
            /// <summary>get small icon</summary>
            SmallIcon = 0x000000001,
            /// <summary>get open icon</summary>
            OpenIcon = 0x000000002,
            /// <summary>get shell size icon</summary>
            ShellIconSize = 0x000000004,
            /// <summary>pszPath is a pidl</summary>
            PIDL = 0x000000008,
            /// <summary>use passed dwFileAttribute</summary>
            UseFileAttributes = 0x000000010,
            /// <summary>apply the appropriate overlays</summary>
            AddOverlays = 0x000000020,
            /// <summary>Get the index of the overlay in the upper 8 bits of the iIcon</summary>
            OverlayIndex = 0x000000040,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public const int NAMESIZE = 80;
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left, top, right, bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            int x;
            int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGELISTDRAWPARAMS
        {
            public int cbSize;
            public IntPtr himl;
            public int i;
            public IntPtr hdcDst;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int xBitmap;    // x offest from the upperleft of bitmap
            public int yBitmap;    // y offset from the upperleft of bitmap
            public int rgbBk;
            public int rgbFg;
            public int fStyle;
            public int dwRop;
            public int fState;
            public int Frame;
            public int crEffect;
        }

        #region IMAGEINFO IImageList
        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGEINFO
        {
            public IntPtr hbmImage;
            public IntPtr hbmMask;
            public int Unused1;
            public int Unused2;
            public RECT rcImage;
        }
        [ComImportAttribute()]
        [GuidAttribute("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IImageList
        {
            [PreserveSig]
            int Add(
            IntPtr hbmImage,
            IntPtr hbmMask,
            ref int pi);

            [PreserveSig]
            int ReplaceIcon(
            int i,
            IntPtr hicon,
            ref int pi);

            [PreserveSig]
            int SetOverlayImage(
            int iImage,
            int iOverlay);

            [PreserveSig]
            int Replace(
            int i,
            IntPtr hbmImage,
            IntPtr hbmMask);

            [PreserveSig]
            int AddMasked(
            IntPtr hbmImage,
            int crMask,
            ref int pi);

            [PreserveSig]
            int Draw(
            ref IMAGELISTDRAWPARAMS pimldp);

            [PreserveSig]
            int Remove(
        int i);

            [PreserveSig]
            int GetIcon(
            int i,
            int flags,
            ref IntPtr picon);

            [PreserveSig]
            int GetImageInfo(
            int i,
            ref IMAGEINFO pImageInfo);

            [PreserveSig]
            int Copy(
            int iDst,
            IImageList punkSrc,
            int iSrc,
            int uFlags);

            [PreserveSig]
            int Merge(
            int i1,
            IImageList punk2,
            int i2,
            int dx,
            int dy,
            ref Guid riid,
            ref IntPtr ppv);

            [PreserveSig]
            int Clone(
            ref Guid riid,
            ref IntPtr ppv);

            [PreserveSig]
            int GetImageRect(
            int i,
            ref RECT prc);

            [PreserveSig]
            int GetIconSize(
            ref int cx,
            ref int cy);

            [PreserveSig]
            int SetIconSize(
            int cx,
            int cy);

            [PreserveSig]
            int GetImageCount(
        ref int pi);

            [PreserveSig]
            int SetImageCount(
            int uNewCount);

            [PreserveSig]
            int SetBkColor(
            int clrBk,
            ref int pclr);

            [PreserveSig]
            int GetBkColor(
            ref int pclr);

            [PreserveSig]
            int BeginDrag(
            int iTrack,
            int dxHotspot,
            int dyHotspot);

            [PreserveSig]
            int EndDrag();

            [PreserveSig]
            int DragEnter(
            IntPtr hwndLock,
            int x,
            int y);

            [PreserveSig]
            int DragLeave(
            IntPtr hwndLock);

            [PreserveSig]
            int DragMove(
            int x,
            int y);

            [PreserveSig]
            int SetDragCursorImage(
            ref IImageList punk,
            int iDrag,
            int dxHotspot,
            int dyHotspot);

            [PreserveSig]
            int DragShowNolock(
            int fShow);

            [PreserveSig]
            int GetDragImage(
            ref POINT ppt,
            ref POINT pptHotspot,
            ref Guid riid,
            ref IntPtr ppv);

            [PreserveSig]
            int GetItemFlags(
            int i,
            ref int dwFlags);

            [PreserveSig]
            int GetOverlayImage(
            int iOverlay,
            ref int piIndex);
        };
        #endregion

        const string IID_IImageList = "46EB5926-582E-4017-9FDF-E8998DAA0950";
        const string IID_IImageList2 = "192B9D83-50FC-457B-90A0-2B82A8B5DAE1";

        public static class Shell32
        {

            public const int SHIL_LARGE = 0x0;
            public const int SHIL_SMALL = 0x1;
            public const int SHIL_EXTRALARGE = 0x2;
            public const int SHIL_SYSSMALL = 0x3;
            public const int SHIL_JUMBO = 0x4;
            public const int SHIL_LAST = 0x4;

            public const int ILD_TRANSPARENT = 0x00000001;
            public const int ILD_IMAGE = 0x00000020;

            [DllImport("shell32.dll", EntryPoint = "#727")]
            public extern static int SHGetImageList(int iImageList, ref Guid riid, ref IImageList ppv);

            [DllImport("user32.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
            public static unsafe extern int DestroyIcon(IntPtr hIcon);

            [DllImport("shell32.dll")]
            public static extern uint SHGetIDListFromObject([MarshalAs(UnmanagedType.IUnknown)] object iUnknown, out IntPtr ppidl);

            [DllImport("Shell32.dll")]
            public static extern IntPtr SHGetFileInfo(
                string pszPath,
                uint dwFileAttributes,
                ref SHFILEINFO psfi,
                uint cbFileInfo,
                uint uFlags
            );
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

        private void MoveActionStart(object sender, MouseButtonEventArgs e) => isMoving = true;
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
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data.Length != elements.Length)
                        {
                            ReadElements();
                        }
                        else if (data[i].Name == elements[i])
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
                catch(Exception ex)
                {
                    if (tries <= 3)
                    {
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

            EnableBlur();
            SetBottom(this);
            string[] tiles = Directory.EnumerateDirectories(path).ToArray();

            if (File.Exists(path + "\\PositionData.dat"))
            {
                string posString = File.ReadAllText(path + "\\PositionData.dat"); // Input {FOLDERNAME}:{X}:{Y} ex. (Files X=120 Y=60) Files:120:60;
                string[] positions = posString.Replace("\r", "").Replace("\n", "").Replace(" ", "").Split(';');
                foreach (string position in positions)
                {
                    string[] splitedpos = position.Split(':');
                    if (position != "")
                    {
                        //string name = splitedpos[0];
                        string x = splitedpos[1];
                        string y = splitedpos[2];
                        Top = int.Parse(y);
                        Left = int.Parse(x);
                    }
                }
            }
            else
            {
                File.WriteAllText(path + "\\PositionData.dat", folderNameTB.Text + ":" + Left + ":" + Top + ";");
            }
            ReadElements();
        }

        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        public static void SetBottom(Window window)
        {
            /*IntPtr hWnd = new WindowInteropHelper(window).Handle;
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);*/
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
                    IntPtr hIcon = GetJumboIcon(GetIconIndex(elements[i]));

                    // from native to managed
                    using (System.Drawing.Icon ico = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(hIcon).Clone())
                    {
                        // save to file (or show in a picture box)
                        ico.ToBitmap().Save(path + "\\TMP_ICON_" + ri + i, System.Drawing.Imaging.ImageFormat.Png);
                        File.AppendAllText(path + "\\iconcache.db", elements[i] + "*" + "\\TMP_ICON_" + ri + i + "\r\n");
                        num = "\\TMP_ICON_" + ri + i;
                    }

                    Shell32.DestroyIcon(hIcon); // don't forget to cleanup
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
                    Source = new BitmapImage(new Uri(elements[i].Contains(".png") || elements[i].Contains(".jpg")
                    ? elements[i] : path + num // ICON
                    ))
                };

                TextBlock filename = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Brushes.White,
                    Text = elements[i].Replace(path + "\\" + name + "\\", "").Replace(".lnk", "") // File Name
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
                Top = GetMousePosition().Y - Height + 10;
                Left = GetMousePosition().X - 400;
            }
        }

        private void MoveActionStop(object sender, MouseButtonEventArgs e)
        {
            isMoving = false;
            File.WriteAllText(path + "\\PositionData.dat", folderNameTB.Text + ":" + Left + ":" + Top + ";");
        }

        int GetIconIndex(string pszFile)
        {
            SHFILEINFO sfi = new SHFILEINFO();
            Shell32.SHGetFileInfo(pszFile
                , 0
                , ref sfi
                , (uint)System.Runtime.InteropServices.Marshal.SizeOf(sfi)
                , (uint)(SHGFI.SysIconIndex | SHGFI.LargeIcon | SHGFI.UseFileAttributes));
            return sfi.iIcon;
        }

        // 256*256
        IntPtr GetJumboIcon(int iImage)
        {
            IImageList spiml = null;
            Guid guil = new Guid(IID_IImageList);//or IID_IImageList

            Shell32.SHGetImageList(Shell32.SHIL_JUMBO, ref guil, ref spiml);
            IntPtr hIcon = IntPtr.Zero;
            spiml.GetIcon(iImage, Shell32.ILD_TRANSPARENT | Shell32.ILD_IMAGE, ref hIcon); //

            return hIcon;
        }

        IntPtr GetXLIcon(int iImage)
        {
            IImageList spiml = null;
            Guid guil = new Guid(IID_IImageList);//or IID_IImageList

            Shell32.SHGetImageList(Shell32.SHIL_EXTRALARGE, ref guil, ref spiml);
            IntPtr hIcon = IntPtr.Zero;
            spiml.GetIcon(iImage, Shell32.ILD_TRANSPARENT | Shell32.ILD_IMAGE, ref hIcon); //

            return hIcon;
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
                isHidded = false;
            }
            else
            {
                lastHeight = (int)this.Height;
                this.Height = 28;
                isHidded = true;
            }
        }

        private void OpenDirectoryBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(path+"\\"+name);
        }
    }
}
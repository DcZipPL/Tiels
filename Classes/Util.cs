using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DWinOverlay.Classes
{
    class Util
    {
        public static String HexConverter(System.Drawing.Color c)
        {
            return "#" + c.A.ToString("X2") + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        public static bool IsEvenFour(int value)
        {
            return value % 4 == 0;
        }

        public static ImageSource BitmapFromUri(Uri source)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = source;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }

        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public static void Reconfigurate()
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

        public static System.Drawing.Image CropImage(System.Drawing.Image img, System.Drawing.Rectangle cropArea)
        {
            System.Drawing.Bitmap bmpImage = new System.Drawing.Bitmap(img);
            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
        }

        public static void EnableBlur(Window window)
        {
            var windowHelper = new WindowInteropHelper(window);

            var accent = new Util.AccentPolicy();
            var accentStructSize = Marshal.SizeOf(accent);
            accent.AccentState = Util.AccentState.ACCENT_ENABLE_BLURBEHIND;

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new Util.WindowCompositionAttributeData();
            data.Attribute = Util.WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            Util.SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        public static int GetIconIndex(string pszFile)
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
        public static IntPtr GetJumboIcon(int iImage)
        {
            IImageList spiml = null;
            Guid guil = new Guid(IID_IImageList);//or IID_IImageList

            Shell32.SHGetImageList(Shell32.SHIL_JUMBO, ref guil, ref spiml);
            IntPtr hIcon = IntPtr.Zero;
            spiml.GetIcon(iImage, Shell32.ILD_TRANSPARENT | Shell32.ILD_IMAGE, ref hIcon);

            return hIcon;
        }

        public static IntPtr GetXLIcon(int iImage)
        {
            IImageList spiml = null;
            Guid guil = new Guid(IID_IImageList);//or IID_IImageList

            Shell32.SHGetImageList(Shell32.SHIL_EXTRALARGE, ref guil, ref spiml);
            IntPtr hIcon = IntPtr.Zero;
            spiml.GetIcon(iImage, Shell32.ILD_TRANSPARENT | Shell32.ILD_IMAGE, ref hIcon);

            return hIcon;
        }

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X,
   int Y, int cx, int cy, uint uFlags);

        public const UInt32 SWP_NOSIZE = 0x0001;
        public const UInt32 SWP_NOMOVE = 0x0002;
        public const UInt32 SWP_NOACTIVATE = 0x0010;

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
        public enum SHGFI : uint
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

        public const string IID_IImageList = "46EB5926-582E-4017-9FDF-E8998DAA0950";
        public const string IID_IImageList2 = "192B9D83-50FC-457B-90A0-2B82A8B5DAE1";

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
    }
}

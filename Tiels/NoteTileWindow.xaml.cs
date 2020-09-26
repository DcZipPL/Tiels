using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Tiels.Classes;

namespace Tiels
{
    /// <summary>
    /// Logika interakcji dla klasy NoteTileWindow.xaml
    /// </summary>
    public partial class NoteTileWindow : Window
    {
        public string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Tiles";
        public string config_path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\" + "Tiels";

        private string newname = null;
        public string name = null;

        private bool isMoving = false;
        private bool isLoading = false;
        private bool isHidded = false;
        public bool isEditMode = false;
        public int lastHeight = 0;
        private Point MousePos;

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

        public NoteTileWindow()
        {
            InitializeComponent();
        }

        public async Task LateUpdate()
        {
            while (true)
            {
                await Task.Delay(500);
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
                                window.Height = (int)this.Height;
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

        private void TileLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = LateUpdate();
                _ = Update();
                ConfigClass config = Config.GetConfig();
                //updateTimer.Interval = new TimeSpan(!config.SpecialEffects ? 400 : 2);

                //Disable Alt-Tab
                WindowInteropHelper wndHelper = new WindowInteropHelper(this);

                int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);

                exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
                SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);

                Util.EnableBlur(this);
                //SetBottom(this);

                MainGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(config.Color));

                //Set text color by theme
                folderNameTB.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
                hideBtn.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
                editBtn.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
                rotateBtn.Foreground = config.Theme == 0 ? Brushes.Black : Brushes.White;
                editBox.Text = name;

                if (folderNameTB.Text.Length >= 36)
                {
                    folderNameTB.Text = folderNameTB.Text.Remove(folderNameTB.Text.Length - (folderNameTB.Text.Length - 36)) + "...";
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(config_path + "\\Error.log", "\r\n[Error: " + DateTime.Now + "] " + ex.ToString());
            }
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
            Mouse.Capture(this, CaptureMode.SubTree);
            AddHandler();
            MousePos = Util.GetMousePosition();
            MousePos.X = Convert.ToInt32(this.Left) - MousePos.X;
            MousePos.Y = Convert.ToInt32(this.Top) - MousePos.Y;
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

        //TODO: Fix move window bug
        private void MoveActionCancel(object sender, MouseEventArgs e) { }// => MoveActionStop(this, null);

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
                Util.Reconfigurate();
            }
        }

        private void ActionGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            hideBtn.Visibility = Visibility.Visible;
            editBtn.Visibility = Visibility.Visible;
            rotateBtn.Visibility = Visibility.Visible;
            moveableinfo.Visibility = Visibility.Visible;
        }

        private void ActionGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            hideBtn.Visibility = Visibility.Collapsed;
            editBtn.Visibility = Visibility.Collapsed;
            rotateBtn.Visibility = Visibility.Collapsed;
            moveableinfo.Visibility = Visibility.Collapsed;
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

        private void ChangeTileName()
        {
            newname = editBox.Text;
            Directory.Move(path + "\\" + name, path + "\\" + newname);
            name = newname;
        }
    }
}

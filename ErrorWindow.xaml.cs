using Tiels.Classes;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Tiels
{
    /// <summary>
    /// Logika interakcji dla klasy ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {
        public Exception ExceptionReason = new NullReferenceException("Varable ExceptionReason is Null");
        public string ExceptionString = "";

        public ErrorWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Util.EnableBlur(this);
            ErrorMessage.Text = ExceptionReason.Message;
            ErrorText.Text = ExceptionReason.ToString();

            if (ExceptionString!="")
            {
                ErrorText.Text = ExceptionString;
            }
        }

        private void CloseWindowBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

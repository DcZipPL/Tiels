using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace TielsUILib
{
    public sealed partial class MainControl : UserControl
    {
        public string XamlIslandMessage { get; set; }

        public string InfoText { get; set; }

        public MainControl()
        {
            this.InitializeComponent();
        }

        private void mainNav_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            string tag = (string)((Microsoft.UI.Xaml.Controls.NavigationViewItem)args.SelectedItem).Tag;
            Type page = null;
            if (tag == "Manage")
                page = new ManagePage().GetType();
            else if (tag == "Appearance")
                page = new AppearancePage().GetType();
            else
                page = new AppearancePage().GetType();
            contentFrame.Navigate(page);
        }
    }
}

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
        private ManagePage managePage = new ManagePage();
        private AppearancePage appearancePage = new AppearancePage();
        private SettingsPage settingsPage = new SettingsPage();

        public string XamlIslandMessage { get; set; }

        public string InfoText { get; set; }

        public ManagePage GetManagePage()
        {
            return managePage;
        }

        public AppearancePage GetAppearancePage()
        {
            return appearancePage;
        }

        public SettingsPage GetSettingsPage()
        {
            return settingsPage;
        }

        public Page getCurrentPage()
        {
            if (mainNav.SelectedItem == null) return null;
            string tag = (string)((Microsoft.UI.Xaml.Controls.NavigationViewItem)mainNav.SelectedItem).Tag;
            if (tag == "Manage")
                return managePage;
            else if (tag == "Appearance")
                return appearancePage;
            else
                return settingsPage;
        }

        public MainControl()
        {
            this.InitializeComponent();
        }

        private void mainNav_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            string tag = (string)((Microsoft.UI.Xaml.Controls.NavigationViewItem)args.SelectedItem).Tag;
            Page page;
            if (tag == "Manage")
                page = managePage;
            else if (tag == "Appearance")
                page = appearancePage;
            else
                page = settingsPage;
            contentFrame.Content = page; // TODO: use .Navigate()
        }
    }
}

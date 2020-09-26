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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace TielsUILib
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ManagePage : Page
    {
        public ManagePage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            int length = 10;
            for (int i = length - 1; i >= 0; i--)
            {
                ListViewItem item = new ListViewItem();
                item.Name = "item" + i;

                Grid grid = new Grid();
                TextBlock text = new TextBlock { Text = "Element " + i, Margin = new Thickness(0, 10, 10, 10) };
                FontIcon icon = new FontIcon { FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"), Glyph = "\uE107" };
                AppBarButton rm_button = new AppBarButton { Icon = icon, Height = 40, Width = 40, HorizontalAlignment = HorizontalAlignment.Right };
                Grid.SetColumn(rm_button, 1);
                grid.Children.Add(rm_button);
                grid.Children.Add(text);
                grid.Width = tileList.Width;
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40, GridUnitType.Pixel) });
                item.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                item.Content = grid;
                tileList.Items.Add(item);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TielsUILib.TilesIO.CreateNewTile();
        }
    }
}
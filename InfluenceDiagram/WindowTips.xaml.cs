using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace InfluenceDiagram
{
    /// <summary>
    /// Interaction logic for WindowTips.xaml
    /// </summary>
    public partial class WindowTips : Window
    {
        public WindowTips()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application curApp = Application.Current;
            Window mainWindow = curApp.MainWindow;
            this.Left = mainWindow.Left + (mainWindow.Width - this.ActualWidth) / 2;
            this.Top = mainWindow.Top + (mainWindow.Height - this.ActualHeight) / 2;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox.IsChecked.HasValue && checkBox.IsChecked.Value)
            {
                Properties.Settings.Default["ShowTipsOnStart"] = false;
                Properties.Settings.Default.Save();
            }
            this.Close();
        }
    }
}

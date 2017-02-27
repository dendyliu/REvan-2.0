using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InfluenceDiagram.ComponentControl
{
    public class LabelTextBox: TextBox
    {
        public LabelTextBox(): base()
        {
            this.Visibility = Visibility.Collapsed;

            this.KeyDown += LabelTextBox_KeyDown;
            this.TextChanged += LabelTextBox_TextChanged;
        }

        void LabelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Text == null || Text.Length == 0)
            {
                this.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.Visibility = Visibility.Visible;
            }
        }

        void LabelTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                /*DependencyObject scope = FocusManager.GetFocusScope(this);
                FocusManager.SetFocusedElement(scope, Application.Current.MainWindow);
                Keyboard.Focus(Application.Current.MainWindow);*/
                RoutedUICommand command = Command.UnselectComponent;
                command.Execute(sender, Application.Current.MainWindow);
            }
        }
    }
}

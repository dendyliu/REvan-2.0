using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using InfluenceDiagram.Utility;

namespace InfluenceDiagram.ComponentControl
{
    /** reference from
     *  http://stackoverflow.com/questions/17381753/wpf-datagrid-creating-a-new-custom-column
     **/
    class SpreadsheetColumn: DataGridBoundColumn
    {
        /*public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set
            {
                SetValue(IsEditableProperty, value);
            }
        }
        public static readonly DependencyProperty IsEditableProperty =
          DependencyProperty.Register("IsEditable",
                                       typeof(bool),
                                       typeof(SpreadsheetColumn)
                                       );*/

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var el = new SpreadsheetComponentCell();
            //el.textBox.IsHitTestVisible = false;
            el.IsEditing = false;
            
            var bb = this.Binding as Binding;
            var b = new Binding { Path = bb.Path, Source = dataItem, Mode = BindingMode.TwoWay };
            el.SetBinding(SpreadsheetComponentCell.DataProperty, b);
            
            b = new Binding("IsReadOnly") { Source = this, Mode = BindingMode.OneWay };
            el.SetBinding(SpreadsheetComponentCell.IsColumnReadOnlyProperty, b);

            return el;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            var el = new SpreadsheetComponentCell();
            //el.textBox.IsHitTestVisible = true;
            el.IsEditing = true;

            var bb = this.Binding as Binding;
            var b = new Binding { Path = bb.Path, Source = dataItem, Mode = BindingMode.TwoWay };
            el.SetBinding(SpreadsheetComponentCell.DataProperty, b);
            return el;
        }

        protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            var el = editingElement as SpreadsheetComponentCell;
            //el.textBox.IsHitTestVisible = true;
            el.IsEditing = true;

            el.textBox.Focus();
            return false;
        }

        protected override void CancelCellEdit(FrameworkElement editingElement, object uneditedValue)
        {
            var el = editingElement as SpreadsheetComponentCell;
            //el.textBox.IsHitTestVisible = false;
            el.IsEditing = false;
        }

        protected override bool CommitCellEdit(FrameworkElement editingElement)
        {
            var el = editingElement as SpreadsheetComponentCell;
            BindingExpression binding = editingElement.GetBindingExpression(SpreadsheetComponentCell.DataProperty);
            if (binding != null) binding.UpdateSource();

            //el.textBox.IsHitTestVisible = false;
            el.IsEditing = false;
            return true;// base.CommitCellEdit(editingElement);
        }
    }
}

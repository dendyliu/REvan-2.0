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
using System.Windows.Navigation;
using System.Windows.Shapes;
using InfluenceDiagram.Data;
using InfluenceDiagram.Utility;

namespace InfluenceDiagram.ComponentControl
{
    /// <summary>
    /// Interaction logic for LoopComponentControl.xaml
    /// </summary>
    public partial class LoopComponentControl : UserControl, IComponentControl
    {
        public LoopComponentData data {get; private set;}
        public RootComponentData rootData { get { return data; } }
        private IComponentValueStore valueStore;

        public IConnectable GetConnector(string variableId)
        {
            if (variableId == data.id)
            {
                return textExpression;
            }
            else if (variableId == data.conditionData.id){
                return textCondition;
            }
            else if (variableId == data.expressionData.id){
                return textExpression;
            }
            else
            {
                var obj = data.parametersData.SingleOrDefault(o => o.id == variableId);
                int index = data.parametersData.IndexOf(obj);
                //int index = data.parametersData.FindIndex(o => o.id == variableId);
                if (index >= 0)
                {
                    return UIHelper.FindVisualChild<LoopComponentTextParam>(containerParams.ItemContainerGenerator.ContainerFromIndex(index));
                }
                else {
                    var obj2 = data.iterationsData.SingleOrDefault(o=>o.id == variableId);
                    index = data.iterationsData.IndexOf(obj2);
                    if (index >= 0){
                        return UIHelper.FindVisualChild<LoopComponentTextExpression>(containerIterators.ItemContainerGenerator.ContainerFromIndex(index));
                    }
                }
            }
            return null;
        }

        public LoopComponentControl(LoopComponentData data, IComponentValueStore valueStore)
        {
            this.data = data;
            this.valueStore = valueStore;
            InitializeComponent();
            this.Loaded += LoopComponentControl_Loaded;
            TextBoxMasking.SetMask(textName, DataHelper.FunctionNameRegex);
            textExpression.valueStore = valueStore;
            textExpression.data = data.expressionData;
            textCondition.valueStore = valueStore;
            textCondition.data = data.conditionData;
            textName.Text = data.Function;
            containerParams.ItemsSource = data.parametersData;
            containerIterators.ItemsSource = data.iterationsData;

            textExpression.placeholder = "result";
            textCondition.placeholder = "while";

            textName.LostFocus += textName_LostFocus;
        }

        void LoopComponentControl_Loaded(object sender, RoutedEventArgs e)
        {
            DependencyObject parent = this.Parent as DependencyObject;
            data.BindPositionToCanvas(parent);
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            textName.Text = data.Function;                  
            //containerParams.Items.Refresh();
            textExpression.UpdateDisplay();
            textCondition.UpdateDisplay();

            for(int i = 0; i < containerIterators.Items.Count; i++)
            {
                LoopComponentTextIteration textIteration = GetTextIteration(i);
                if (textIteration != null)
                {
                    textIteration.UpdatePlaceholder();
                }
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            DependencyObject textBox = sender as DependencyObject;
            if (e.Key == Key.Return)
            {
                /*DependencyObject scope = FocusManager.GetFocusScope(this);
                FocusManager.SetFocusedElement(scope, Application.Current.MainWindow);
                Keyboard.Focus(Application.Current.MainWindow);*/
                RoutedUICommand command = Command.UnselectComponent;
                command.Execute(sender, Application.Current.MainWindow);
            }
        }

        private void textName_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string newName = textBox.Text;
            if (newName != this.data.Function)
            {
                if (newName.Length > 0)
                {
                    try
                    {
                        Command.LoopChangeName.Execute(textName.Text, this);
                    }
                    catch (DataException exc)
                    {
                        textBox.Text = this.data.Function;
                        MessageBox.Show(exc.Message);
                    }
                }
                else
                {
                    UpdateDisplay();
                }
            }
        }

        private void TextParam_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textParam = sender as TextBox;
            int index = data.parametersData.IndexOf(textParam.Tag as LoopParameterData);

            RoutedUICommand command = Command.ActivateComponent;
            command.Execute(sender, Application.Current.MainWindow);
            // calling UpdateDisplay will recreate the parameter TextBoxes!
            //UpdateDisplay();
        }

        private LoopComponentTextIteration GetTextIteration(int index)
        {
            return UIHelper.FindVisualChild<LoopComponentTextIteration>(containerIterators.ItemContainerGenerator.ContainerFromIndex(index));
        }

        private void TextParam_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textParam = sender as TextBox;
            int index = data.parametersData.IndexOf(textParam.Tag as LoopParameterData);
            try
            {
                data.RenameParameter(index, textParam.Text);

                RoutedUICommand command = Command.DeactivateComponent;
                command.Execute(sender, Application.Current.MainWindow);
                UpdateDisplay();
            }
            catch (DataException exc)
            {
                textParam.Text = data.parametersData[index].varname;
                MessageBox.Show(exc.Message);
            }
        }

        private void textName_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            BorderDecorator.DecorateBorderHexagon(borderName, textName);
        }
    }

    public class LoopComponentTextIteration: LoopComponentTextExpression
    {
        public LoopComponentTextIteration()
            : base()
        {
            this.BorderThickness = new Thickness(2);
            this.Loaded += LoopComponentTextIteration_Loaded;
        }

        void LoopComponentTextIteration_Loaded(object sender, RoutedEventArgs e)
        {
            this.data = this.Tag as LoopExpressionData;
            this.valueStore = this.data.valueStore;
            UpdatePlaceholder();
        }

        public void UpdatePlaceholder()
        {
            if (this.data == null) return;

            LoopComponentData componentData = (UIHelper.FindVisualParent<LoopComponentControl>(this)).data;
            int index = componentData.iterationsData.IndexOf(this.data);
            this.placeholder = "next " + componentData.parametersData[index].varname;
            this.UpdateDisplay();
        }
    }

    public class LoopComponentTextExpression: ExpressionTextBox, IComponentVariableReceiver, IComponentVariableSource
    {
        public string placeholder;

        public LoopExpressionData data;

        public LoopComponentTextExpression(): base()
        {
            this.Document.TextAlignment = TextAlignment.Center;
            this.GotFocus += LoopComponentTextExpression_GotFocus;
            this.LostFocus += LoopComponentTextExpression_LostFocus;
        }

        public void UpdateDisplay()
        {
            bool isPlaceholder = false;
            if (this.IsFocused)
            {
                this.SetExpression(data.expression);
            }
            else
            {
                if (data.expression.Length == 0)
                {
                    this.SetPlainText(placeholder);
                    isPlaceholder = true;
                }
                else
                {
                    this.SetPlainText(data.GetValueAsString());
                }
            }
            this.FontWeight = isPlaceholder ? FontWeights.Normal : FontWeights.Bold;
            this.FontStyle = isPlaceholder ? FontStyles.Italic : FontStyles.Normal;
        }

        private void UpdateExpressionData()
        {
            string newExpression = this.GetExpression();
            this.data.expression = newExpression;
        }

        private void LoopComponentTextExpression_GotFocus(object sender, RoutedEventArgs e)
        {
            RoutedUICommand command = Command.ActivateComponent;
            command.Execute(sender, Application.Current.MainWindow);
            UpdateDisplay();
        }

        private void LoopComponentTextExpression_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateExpressionData();
            RoutedUICommand command = Command.DeactivateComponent;
            command.Execute(sender, Application.Current.MainWindow);
            UpdateDisplay();
        }

        public bool ReceiveComponentVariable(IComponentVariableSource component)
        {
            if (!(component.GetData() is LoopParameterData))
            {
                string parentId = valueStore.GetRootComponentId(data.id);
                string otherParentId = valueStore.GetRootComponentId(component.GetData().id);
                if (parentId == otherParentId) return false;
            }
            return this.ReceiveComponentVariable(component, data);
        }

        // loop expression can only receive its own parameter
        override protected bool CheckParameterData(AbstractParameterData receivedData)
        {
            return receivedData.parentId == data.parentId;
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (!this.IsFocused)
            {
                RoutedUICommand command = Command.ClickComponent;
                command.Execute(this, Application.Current.MainWindow);
                e.Handled = true;
            }
        }

        public AbstractComponentData GetData()
        {
            // return the LoopComponentData, not LoopExpressionData
            return (UIHelper.FindVisualParent<LoopComponentControl>(this)).data;
        }
    }

    public class LoopComponentTextParam: TextBox, IComponentVariableSource
    {
        public ControlConnector connector { get; private set; }

        public LoopComponentTextParam()
            : base()
        {
            connector = new ControlConnector();
            connector.HorizontalAlignment = HorizontalAlignment.Center;
            connector.VerticalAlignment = VerticalAlignment.Center;
            this.AddVisualChild(connector);
            connector.InitConnectable(this);

            this.Unloaded += LoopComponentTextParam_Unloaded;
        }

        void LoopComponentTextParam_Unloaded(object sender, RoutedEventArgs e)
        {
            connector.Cleanup();
        }

        public AbstractComponentData GetData()
        {
            return (this.Tag as LoopParameterData);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (!this.IsFocused)
            {
                RoutedUICommand command = Command.ClickComponent;
                command.Execute(this, Application.Current.MainWindow);
                e.Handled = true;
            }
        }
    }
}

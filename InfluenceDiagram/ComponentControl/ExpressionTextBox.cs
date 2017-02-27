using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using InfluenceDiagram.Utility;
using InfluenceDiagram.Data;
using System.Text.RegularExpressions;
using System.Windows;
using DiagramDesigner;

namespace InfluenceDiagram.ComponentControl
{
    public class ExpressionTextBox : RichTextBox, IConnectable
    {
        public virtual IComponentValueStore valueStore { get; set; }

        // an expression cannot include macro parameters from 2 different functions
        private List<string> referredMacroParams;

        public ControlConnector connector { get; private set; }

        private int? prevCaretOffset;  // caret offset before lost focus

        public ExpressionTextBox(): base()
        {
            connector = new ControlConnector();
            connector.HorizontalAlignment = HorizontalAlignment.Center;
            connector.VerticalAlignment = VerticalAlignment.Center;
            this.AddVisualChild(connector);
            connector.InitConnectable(this);

            //this.MinWidth = 50;
            //this.MinHeight = 25;
            this.IsEnabled = true;
            this.AcceptsReturn = false;
            this.AcceptsTab = false;
            this.BorderThickness = new Thickness(0);
            this.HorizontalAlignment = HorizontalAlignment.Center;
            this.VerticalAlignment = VerticalAlignment.Center;
            this.Padding = new Thickness(0);
            this.Margin = new Thickness(0);
            this.FontSize = 14;
            this.FontWeight = FontWeights.Bold;
            this.KeyDown += textBox_KeyDown;
            this.GotFocus += textBox_GotFocus;
            this.LostFocus += textBox_LostFocus;
            this.LayoutUpdated += textBox_LayoutUpdated;

            referredMacroParams = new List<string>();

            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, Copy_Executed));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, Cut_Executed));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, Paste_Executed));

            this.Unloaded += ExpressionTextBox_Unloaded;
        }
               

        void ExpressionTextBox_Unloaded(object sender, RoutedEventArgs e)
        {
            connector.Cleanup();
        }

        public string GetExpression(TextPointer start = null, TextPointer end = null)
        {
            return this.Document.GetTextWithVariable(start, end);
        }

        public void SetPlainText(string value)
        {
            this.Document.Blocks.Clear();
            Paragraph paragraph = new Paragraph(new Run(value));
            this.Document.Blocks.Add(paragraph);
        }

        // parse the expression and make variables into InlineUIContainers
        public void SetExpression(string expression)
        {
            Paragraph paragraph = new Paragraph();
            MatchCollection matches = new Regex(@"\[([^\]]*)\]").Matches(expression);
            int pos = 0;
            foreach (Match match in matches)
            {
                if (match.Index > pos)
                {
                    paragraph.Inlines.Add(new Run(expression.Substring(pos, match.Index - pos)));
                }
                string variable = expression.Substring(match.Index + 1, match.Length - 2);
                InlineUIContainer container = CreateInlineContainer(variable, null);
                paragraph.Inlines.Add(container);
                pos = match.Index + match.Length;
            }
            if (expression.Length > pos)
            {
                paragraph.Inlines.Add(new Run(expression.Substring(pos)));
            }
            this.Document.Blocks.Clear();
            this.Document.Blocks.Add(paragraph);
        }

        public InlineUIContainer CreateInlineContainer(string variable, TextPointer caretPosition)
        {
            TextBox t = new TextBox();
            t.Text = valueStore.GetComponentLabelOrValueAsString(variable);
            t.Tag = variable;
            t.Background = Brushes.Red;
            InlineUIContainer container = new InlineUIContainer(t, caretPosition);
            container.Tag = variable;
            return container;
        }

        protected string referredMacroId()
        {
            if (referredMacroParams.Count == 0) return null;
            else return valueStore.GetRootComponentId(referredMacroParams[0]);
        }

        virtual protected bool CheckParameterData(AbstractParameterData data)
        {
            string macroId = referredMacroId();
            return (macroId == null) || (macroId == data.parentId);
        }

        public void InsertVariable(AbstractComponentData receivedData)
        {
            string variable = receivedData.id;
            bool shouldReceive = true;
            if (receivedData is AbstractParameterData)
            {
                shouldReceive = CheckParameterData(receivedData as AbstractParameterData);
            }
            if (shouldReceive)
            {
                InlineUIContainer container = CreateInlineContainer(variable, this.CaretPosition);
                this.CaretPosition = container.ContentEnd;
                if (receivedData is AbstractParameterData)
                {
                    referredMacroParams.Add(variable);
                }
                DependenceCommandArgs args = new DependenceCommandArgs(){
                    source = this,
                    targetVariableId = variable
                };
                Command.AddDependence.Execute(args, this);

                container.Unloaded += macroParameterContainer_Unloaded;
            }
            else
            {
                Console.Out.WriteLine("Cannot receive macro parameter " + variable + ", expression already contains parameter from macro " + referredMacroId());
            }
        }

        void macroParameterContainer_Unloaded(object sender, RoutedEventArgs e)
        {
            InlineUIContainer container = sender as InlineUIContainer;

            string variableId = (container.Tag as string);
            referredMacroParams.Remove(variableId);

            DependenceCommandArgs args = new DependenceCommandArgs()
            {
                source = this,
                targetVariableId = variableId
            };
            Command.RemoveDependence.Execute(args, this);
        }

        public void InsertFunction(string function)
        {
            InsertTextInRunAndMoveCaret(function + "(");
        }

        public bool ReceiveComponentVariable(IComponentVariableSource component, AbstractComponentData selfData)
        {
            AbstractComponentData receivedData = component.GetData();
            return this.ReceiveComponentData(receivedData, selfData);
        }

        virtual public void GetFocusBack()
        {
            this.Focus();
            Keyboard.Focus(this);
            if (this.prevCaretOffset.HasValue)
            {
                this.CaretPosition = this.CaretPosition.DocumentStart.GetPositionAtOffset(prevCaretOffset.Value);
            }
        }

        public bool ReceiveComponentData(AbstractComponentData receivedData, AbstractComponentData selfData)
        {
            if (this.IsReadOnly) return false;
            if (receivedData == null) return false;

            if (!this.IsFocused)
            {
                // if textbox has lost focus, e.g. because clicking on spreadsheet cell
                this.GetFocusBack();
            }
            if (receivedData is AbstractMacroData)
            {
                // macro can have cyclic dependency with other components too
                if (valueStore.CanCreateEdge(selfData, receivedData))
                {
                    this.InsertFunction((receivedData as AbstractMacroData).Function);
                }
                else
                {
                    Console.Out.WriteLine("Macro cyclic dependency from " + selfData.id + " to " + receivedData.id);
                }
            }
            else
            {
                if (valueStore.CanCreateEdge(selfData, receivedData))
                {
                    this.InsertVariable(receivedData);
                }
                else
                {
                    Console.Out.WriteLine("Cyclic dependency from " + selfData.id + " to " + receivedData.id);
                }
            }
            return true;
        }

        virtual protected void OnPressReturn(object sender, KeyEventArgs e)
        {
            /*DependencyObject scope = FocusManager.GetFocusScope(this);
            FocusManager.SetFocusedElement(scope, Application.Current.MainWindow);
            Keyboard.Focus(Application.Current.MainWindow);*/
            RoutedUICommand command = Command.UnselectComponent;
            command.Execute(sender, Application.Current.MainWindow);
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                OnPressReturn(sender, e);
            }
        }

        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            this.CaretPosition = this.CaretPosition.DocumentEnd;
        }

        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            this.prevCaretOffset = -this.CaretPosition.GetOffsetToPosition(this.CaretPosition.DocumentStart);
        }

        void UpdateWidth()
        {
            this.Width = this.Document.CalculateWidth() + 20;
        }
        private void textBox_LayoutUpdated(object sender, EventArgs e)
        {
            UpdateWidth();
        }

        #region Cut, Copy, Paste
        
        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!this.Selection.IsEmpty)
            {
                string text = GetExpression(this.Selection.Start, this.Selection.End);
                Clipboard.SetText(text);
            }
            e.Handled = true;
        }

        private void Cut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!this.Selection.IsEmpty)
            {
                string text = GetExpression(this.Selection.Start, this.Selection.End);
                Clipboard.SetText(text);
                this.Document.DeleteRunsAndInlinecontainers(this.Selection.Start, this.Selection.End);
            }
            e.Handled = true;
        }

        private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string expression = Clipboard.GetText();
            if (expression != null)
            {
                // clear text in selection first, if any
                if (!this.Selection.IsEmpty)
                {
                    this.Document.DeleteRunsAndInlinecontainers(this.Selection.Start, this.Selection.End);
                }
                MatchCollection matches = new Regex(DataHelper.VariableRegex).Matches(expression);
                int pos = 0;
                foreach (Match match in matches)
                {
                    if (match.Index > pos)
                    {
                        InsertTextInRunAndMoveCaret(expression.Substring(pos, match.Index - pos));
                    }
                    string variable = expression.Substring(match.Index + 1, match.Length - 2);
                    InlineUIContainer container = CreateInlineContainer(variable, null);
                    this.Document.InsertInlineAt(container, this.CaretPosition);
                    this.CaretPosition = container.ContentEnd;
                    pos = match.Index + match.Length;
                }
                if (expression.Length > pos)
                {
                    InsertTextInRunAndMoveCaret(expression.Substring(pos));
                }
            }
            e.Handled = true;
        }

        private void InsertTextInRunAndMoveCaret(string text)
        {
            TextPointer newPosition = this.Document.InsertTextAt(text, this.CaretPosition);
            this.CaretPosition = newPosition;
        }

        #endregion

    }

}

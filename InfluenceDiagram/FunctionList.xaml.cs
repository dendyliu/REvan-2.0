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
using InfluenceDiagram.Data;
using NCalc.Domain;

namespace InfluenceDiagram
{
    /// <summary>
    /// Interaction logic for FunctionList.xaml
    /// </summary>
    public partial class FunctionList : Window
    {
        private WorksheetData worksheetData;
        private Dictionary<string, object> functionsDict;
        private List<string> functionsArr;

        public string selectedFunction;

        public FunctionList(WorksheetData worksheetData)
        {
            this.worksheetData = worksheetData;
            functionsDict = new Dictionary<string, object>();
            foreach (KeyValuePair<string,AbstractFunction> pair in BuiltinFunctions.AllFunctions){
                functionsDict[pair.Key] = pair.Value;
            }
            // internal macro
            foreach (KeyValuePair<string,AbstractMacroData> pair in worksheetData.mapMacroData){
                functionsDict[pair.Key] = pair.Value;
            }
            // external macro
            foreach (WorksheetData externalWorksheet in worksheetData.listExternalWorksheet)
            {
                foreach (KeyValuePair<string, AbstractMacroData> pair in externalWorksheet.mapMacroData)
                {
                    functionsDict[pair.Key] = pair.Value;
                }
            }

            functionsArr = functionsDict.Keys.ToArray().OrderBy(item => item).ToList();

            InitializeComponent();
            listBox.ItemsSource = functionsArr;
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string functionName = listBox.SelectedItem as string;
            if (functionName == null)
            {
                textDescription.Text = "";
            }
            else
            {
                object function = functionsDict[functionName];
                if (function is AbstractFunction)
                {
                    textDescription.Text = (function as AbstractFunction).Description;
                }
                else if (function is AbstractMacroData)
                {
                    AbstractMacroData macro = function as AbstractMacroData;
                    string info = "[custom macro]\n";
                    info += macro.Function + "(";
                    List<string> parameters = new List<string>();
                    if (macro is MacroComponentData)
                    {
                        foreach (MacroParameterData p in (macro as MacroComponentData).parametersData)
                        {
                            parameters.Add(p.varname);
                        }
                    }
                    else if (macro is LoopComponentData)
                    {
                        foreach (LoopParameterData p in (macro as LoopComponentData).parametersData)
                        {
                            parameters.Add(p.varname);
                        }
                    }
                    info += String.Join(",", parameters);
                    info += ")\n";

                    string description = (function as AbstractMacroData).Description;
                    if (description == null || description.Length == 0)
                    {
                        textDescription.Text = info;
                    }
                    else
                    {
                        textDescription.Text = info + description;
                    }
                }
            }
        }

        private void textSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (textSearch.Text.Length == 0)
                {
                    listBox.ItemsSource = functionsArr;
                }
                else
                {
                    List<string> filtered = functionsArr.Where(item => item.ToLower().Contains(textSearch.Text.ToLower())).ToList();
                    listBox.ItemsSource = filtered;
                }
            }
        }

        private void listBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string functionName = listBox.SelectedItem as string;
            if (functionName != null)
            {
                selectedFunction = functionName;
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}

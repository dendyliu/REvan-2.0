using InfluenceDiagram.ComponentControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace InfluenceDiagram
{
    public class Command
    {
        public static readonly RoutedUICommand ResetZoom = new RoutedUICommand();

        public static readonly RoutedUICommand Add = new RoutedUICommand();
        public static readonly RoutedUICommand Rename = new RoutedUICommand();

        public static readonly RoutedUICommand DropExpression = new RoutedUICommand();
        public static readonly RoutedUICommand DropSpreadsheet = new RoutedUICommand();
        public static readonly RoutedUICommand DropMacro = new RoutedUICommand();
        public static readonly RoutedUICommand DropShape = new RoutedUICommand();
        public static readonly RoutedUICommand DropIf = new RoutedUICommand();
        public static readonly RoutedUICommand DropLoop = new RoutedUICommand();

        public static readonly RoutedUICommand SpreadsheetAddColumn = new RoutedUICommand();
        public static readonly RoutedUICommand SpreadsheetAddRow = new RoutedUICommand();
        public static readonly RoutedUICommand SpreadsheetDeleteColumn = new RoutedUICommand();
        public static readonly RoutedUICommand SpreadsheetDeleteRow = new RoutedUICommand();
        public static readonly RoutedUICommand SpreadsheetColumnExpression = new RoutedUICommand();
        public static readonly RoutedUICommand SpreadsheetRowExpression = new RoutedUICommand();

        public static readonly RoutedUICommand MacroAddParam = new RoutedUICommand();
        public static readonly RoutedUICommand MacroDeleteParam = new RoutedUICommand();
        public static readonly RoutedUICommand MacroChangeName = new RoutedUICommand();

        public static readonly RoutedUICommand LoopAddParam = new RoutedUICommand();
        public static readonly RoutedUICommand LoopDeleteParam = new RoutedUICommand();
        public static readonly RoutedUICommand LoopChangeName = new RoutedUICommand();

        public static readonly RoutedUICommand ShapeAddConnection = new RoutedUICommand();

        // dispatched when a component becomes active (editable)
        public static readonly RoutedUICommand ActivateComponent = new RoutedUICommand();
        // dispatched when a component becomes inactive (not editable, e.g. clicking on other component/canvas)
        public static readonly RoutedUICommand DeactivateComponent = new RoutedUICommand();
        // dispatched when unselecting a component, making no other component selected (e.g. pressing Enter)
        public static readonly RoutedUICommand UnselectComponent = new RoutedUICommand();
        // dispatched when clicking a component
        // need to determine whether to select this component or link it to another active component, if any
        public static readonly RoutedUICommand ClickComponent = new RoutedUICommand();

        public static readonly RoutedUICommand IncludeExternal = new RoutedUICommand();
        public static readonly RoutedUICommand FunctionList = new RoutedUICommand();
        public static readonly RoutedUICommand OpenDatatools = new RoutedUICommand();
        public static readonly RoutedUICommand OpenGoalseek = new RoutedUICommand();
        public static readonly RoutedUICommand OpenDatatable = new RoutedUICommand();
        public static readonly RoutedUICommand CreateDatatable = new RoutedUICommand();
        public static readonly RoutedUICommand DeleteDatatable = new RoutedUICommand();
        public static readonly RoutedUICommand ExportExcel = new RoutedUICommand();

        public static readonly RoutedUICommand AddDependence = new RoutedUICommand();
        public static readonly RoutedUICommand RemoveDependence = new RoutedUICommand();
    }

    public class DependenceCommandArgs
    {
        public IConnectable source { get; set; }
        public string targetVariableId { get; set; }
    }
}

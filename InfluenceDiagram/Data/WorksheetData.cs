using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickGraph;
using QuickGraph.Algorithms;
using NCalc;
using NCalc.Domain;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Threading;
using System.ComponentModel;
using InfluenceDiagram.Utility;
using System.Reflection;
using System.Globalization;

namespace InfluenceDiagram.Data
{
    
    [DataContract]
    public class WorksheetData: IComponentValueStore
    {
        [DataMember]
        private string version = @"1.0";
        [DataMember]
        public List<RootComponentData> listRootComponentData { get; private set; }
        private Dictionary<string, AbstractComponentData> mapComponentData;
        private Dictionary<string, object> mapComponentValue;
        private BidirectionalGraph<AbstractComponentData, Edge<AbstractComponentData>> graphDependency;
        public Dictionary<string, AbstractMacroData> mapMacroData {get; private set;} // mapping the function name to Macro object
        [DataMember]
        private VariableNameGenerator variableNameGenerator;
        [DataMember]
        public ObservableCollection<string> listExternalWorksheetPaths { get; private set; }
        public List<WorksheetData> listExternalWorksheet { get; private set; }
        public string currentFilePath;

        static private EvaluateOptions DefaultEvaluateOptions = EvaluateOptions.IgnoreCase;

        private bool ShouldDispatchEvents { get; set; }
        public bool ShouldDispatchPropertyChanged { get; set; }

        public event ComponentDataChangedEventHandler DataChanged;
        public event ComponentPropertyChangedEventHandler PropertyChanged;
        public event ComponentDependenceAddedEventHandler DependenceAdded;
        public event ComponentDependenceRemovedEventHandler DependenceRemoved;


        // for now, don't use Expression Cache because it somehow just slows down calculation
        bool EnableExpressionCache
        {
            get { return Expression.CacheEnabled; }
            set
            {
                Expression.CacheEnabled = false;// value;
            }
        }

        public WorksheetData()
        {
            Construct();
        }
        void Construct()
        {
            ShouldDispatchEvents = true;
            ShouldDispatchPropertyChanged = true;
            EnableExpressionCache = false;
            listRootComponentData = new List<RootComponentData>();
            mapComponentData = new Dictionary<string, AbstractComponentData>();
            mapComponentValue = new Dictionary<string, object>();
            graphDependency = new BidirectionalGraph<AbstractComponentData, Edge<AbstractComponentData>>();
            mapMacroData = new Dictionary<string, AbstractMacroData>();
            variableNameGenerator = new VariableNameGenerator("v");
            listExternalWorksheetPaths = new ObservableCollection<string>();
            listExternalWorksheet = new List<WorksheetData>();

            graphDependency.EdgeAdded += graphDependency_EdgeAdded;
            graphDependency.EdgeRemoved += graphDependency_EdgeRemoved;
        }

        void graphDependency_EdgeAdded(Edge<AbstractComponentData> e)
        {
            if (DependenceAdded != null && ShouldDispatchEvents)
            {
                ComponentDependenceEventArgs args = new ComponentDependenceEventArgs()
                {
                    sourceVariableId = e.Source.id,
                    targetVariableId = e.Target.id,
                };
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render,
                 new Action(delegate()
                 {
                     DependenceAdded(this, args);
                 }));
            }
        }

        void graphDependency_EdgeRemoved(Edge<AbstractComponentData> e)
        {
            if (DependenceRemoved != null && ShouldDispatchEvents)
            {
                ComponentDependenceEventArgs args = new ComponentDependenceEventArgs()
                {
                    sourceVariableId = e.Source.id,
                    targetVariableId = e.Target.id,
                };
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render,
                 new Action(delegate()
                 {
                     DependenceRemoved(this, args);
                 }));
            }
        }

        private void DispatchDataChanged(string id)
        {
            if (DataChanged != null && ShouldDispatchEvents)
            {
                ComponentDataChangedEventArgs args = new ComponentDataChangedEventArgs() { 
                    variableId = id
                };
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render,
                 new Action(delegate()
                 {
                     DataChanged(this, args);
                 }));
            }
        }

        private void DispatchPropertyChanged(string id, string propertyName, object oldValue, object newValue)
        {
            if (PropertyChanged != null && ShouldDispatchEvents && ShouldDispatchPropertyChanged)
            {
                ComponentPropertyChangedEventArgs args = new ComponentPropertyChangedEventArgs()
                {
                    VariableId = id,
                    OldProperties = new Dictionary<string,object>{{propertyName, oldValue}},
                    NewProperties = new Dictionary<string,object>{{propertyName, newValue}}
                };
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render,
                 new Action(delegate()
                 {
                     PropertyChanged(this, args);
                 }));
            }
        }

        #region IComponentValueStore
        
        // get the base component id from composite variable id
        // e.g. variable v1_1_1, the component id is v1
        public string GetRootComponentId(string variableId)
        {
            int index = variableId.IndexOf('_');
            if (index >= 0)
            {
                return variableId.Substring(0, index);
            }
            else
            {
                return variableId;
            }
        }

        public AbstractComponentData GetComponentData(string id)
        {
            return mapComponentData[id];
        }

        public object GetComponentValue(string id)
        {
            if (IsSpreadsheetRangeId(id))
            {
                return "RANGE";
            }
            else if (mapComponentValue.ContainsKey(id))
            {
                object value = mapComponentValue[id];
                if (value is Expression)
                {
                    return "#ERR";
                }
                else if (value is List<List<object>>)
                {
                    // raw range doesn't have any real value
                    return "#ERR";
                }
                else
                {
                    return mapComponentValue[id];
                }
            }
            else
            {
                return "#ERR";
            }
        }

        public string GetComponentValueAsString(string id)
        {
            object value = this.GetComponentValue(id);
            if (value == null)
            {
                return "";
            }
            else if (value is Double)
            {
                // display double rounded to 3 decimals
                return Math.Round((double)value, 3).ToString(CultureInfo.InvariantCulture);
            }
            else if (value is RawExpression)
            {
                return (value as RawExpression).Expression;
            }
            else if (value is RawIdentifierExpression)
            {
                return (value as RawIdentifierExpression).Identifier;
            }
            else
            {
                return value.ToString();
            }
        }

        private bool IsSpreadsheetRangeId(string id)
        {
            return DataHelper.IsSpreadsheetRangeId(id);
        }
        
        public string GetComponentLabelOrValueAsString(string id)
        {
            if (mapComponentData.ContainsKey(id))
            {
                AbstractComponentData data = mapComponentData[id];
                string label = null;
                if (data is RootComponentData)
                {
                    label = (data as RootComponentData).label;
                }
                else if (data is SpreadsheetCellData)
                {
                    SpreadsheetCellData cellData = (data as SpreadsheetCellData);
                    string rootId = GetRootComponentId(data.id);
                    SpreadsheetComponentData spreadsheet = mapComponentData[rootId] as SpreadsheetComponentData;
                    PointInt? position = spreadsheet.GetPositionFromCellId(cellData.id);
                    label = SpreadsheetComponentData.GetDefaultCellLabel(position);
                }
                else if (data is SpreadsheetColumnData)
                {
                    SpreadsheetColumnData columnData = (data as SpreadsheetColumnData);
                    label = columnData.label;
                    if (label == null || label.Length == 0)
                    {
                        string rootId = GetRootComponentId(data.id);
                        SpreadsheetComponentData spreadsheet = mapComponentData[rootId] as SpreadsheetComponentData;
                        int colIndex = spreadsheet.GetColumnIndexFromColumnId(columnData.GetColumnId());
                        label = SpreadsheetComponentData.GetDefaultColumnName(colIndex);
                    }
                }
                else if (data is SpreadsheetRowData)
                {
                    SpreadsheetRowData rowData = (data as SpreadsheetRowData);
                    label = rowData.label;
                    if (label == null || label.Length == 0)
                    {
                        string rootId = GetRootComponentId(data.id);
                        SpreadsheetComponentData spreadsheet = mapComponentData[rootId] as SpreadsheetComponentData;
                        int rowIndex = spreadsheet.GetRowIndexFromRowId(rowData.GetRowId());
                        label = SpreadsheetComponentData.GetDefaultRowName(rowIndex);
                    }
                }
                else if (data is SpreadsheetRangeData)
                {
                    SpreadsheetRangeData rangeData = data as SpreadsheetRangeData;
                    label = rangeData.autoLabel;
                }
                if (label != null && label.Length > 0)
                {
                    return label;
                }
            }
            else if (IsSpreadsheetRangeId(id))
            {
                SpreadsheetRangeData rangeData = CreateSpreadsheetRangeDataFromRangeId(id);
                return rangeData.autoLabel;
            }
            return this.GetComponentValueAsString(id);
        }

        /*public void UpdateComponent(AbstractComponentData component)
        {
            if (component is ExpressionComponentData)
            {
                UpdateExpressionComponent(component as ExpressionComponentData);
            }
            else if (component is MacroComponentData)
            {
                UpdateMacroExpression(component as MacroComponentData);
            }
        }*/

        public void ResolveValue(string id)
        {
            ResolveComponentValue(mapComponentData[id]);
        }
    
        public void ChangeMacroName(AbstractMacroData macro, string newName)
        {
            if (BuiltinFunctions.Has(newName))
            {
                throw new DataException("\""+ newName + "\" is a builtin function. Please choose another name");
            }
            if (mapMacroData.ContainsKey(newName.ToLower()))
            {
                throw new DataException("Another macro with name \"" + newName + "\" already exists!");
            } 
            else 
            {
                string oldName = macro.Function;
                macro.Function = newName;
                mapMacroData.Remove(oldName.ToLower());
                mapMacroData.Add(newName.ToLower(), macro);

                // update expressions that use this function
                AbstractComponentData vertex = macro;
                List<IExpressionData> dependentExpressions = new List<IExpressionData>();
                foreach (Edge<AbstractComponentData> edge in graphDependency.InEdges(vertex))
                {
                    AbstractComponentData data = edge.Source;
                    if (data is IExpressionData)
                    {
                        dependentExpressions.Add(data as IExpressionData);
                    }
                }
                ShouldDispatchPropertyChanged = false;  // don't store changed dependent expressions to Undo stack
                foreach (IExpressionData expressionData in dependentExpressions)
                {
                    // regex: find substring that ends with ) but not preceded by alphanumeric (because in that case that's a different function name)
                    expressionData.expression = Regex.Replace(expressionData.expression, @"(?<![a-zA-Z0-9])" + oldName + @"\(", newName + "(", RegexOptions.IgnoreCase);
                }
                ShouldDispatchPropertyChanged = true;

                //ResolveComponentValue(macro.expressionData);
                DispatchDataChanged(macro.id);
            }
        }

        public void PasteRootComponent(RootComponentData component, System.Windows.Point? pastePosition)
        {
            int index = listRootComponentData.FindIndex(obj => obj.id == component.id);
            if (index >= 0)
            {
                // if id already exists, change it
                component.id = variableNameGenerator.NewVariableName();
            }
            if (component is AbstractMacroData)
            {
                (component as AbstractMacroData).Function = GenerateNewMacroName();
            }
            if (pastePosition.HasValue)
            {
                component.PositionX = pastePosition.Value.X;
                component.PositionY = pastePosition.Value.Y;
            }
            else
            {
                // add some space so it doesn't overlapped
                Random rand = new Random();
                component.PositionX += rand.Next(150);
                component.PositionY += rand.Next(150);
            }
            component.valueStore = this;
            listRootComponentData.Add(component);
            PrepareRootComponent(component, false);
        }

        public void PrepareRootComponent(RootComponentData component, bool resolveValue = true)
        {
            if (component is ExpressionComponentData)
            {
                PrepareExpressionComponent(component as ExpressionComponentData, resolveValue);
            }
            else if (component is SpreadsheetComponentData)
            {
                PrepareSpreadsheetComponent(component as SpreadsheetComponentData, resolveValue);
            }
            else if (component is MacroComponentData)
            {
                PrepareMacroComponent(component as MacroComponentData, resolveValue);
            }
            else if (component is ShapeComponentData)
            {
                PrepareShapeComponent(component as ShapeComponentData);
            }
            else if (component is IfComponentData)
            {
                PrepareIfComponent(component as IfComponentData, resolveValue);
            }
            else if (component is LoopComponentData)
            {
                PrepareLoopComponent(component as LoopComponentData, resolveValue);
            }
        }

        #endregion

        void PrepareComponentData(AbstractComponentData component)
        {
            mapComponentData.Add(component.id, component);
            component.PropertyChanged += component_PropertyChanged;
        }

        void component_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e is PropertyChangedExtendedEventArgs)
            {
                AbstractComponentData component = sender as AbstractComponentData;
                PropertyChangedExtendedEventArgs ext = e as PropertyChangedExtendedEventArgs;
                DispatchPropertyChanged(component.id, ext.PropertyName, ext.OldValue, ext.NewValue);
            }
        }

        // change component property without dispatching OnPropertyChanged
        // called when doing Undo/Redo
        public void ForceChangeComponentProperties(string componentId, Dictionary<string, object> oldProperties, Dictionary<string, object> newProperties)
        {
            AbstractComponentData component = mapComponentData[componentId];
            ShouldDispatchPropertyChanged = false;
            foreach (KeyValuePair<string,object> pair in newProperties){
                if (pair.Key == "Position")
                {
                    // Position is a custom property
                    if (component is RootComponentData)
                    {
                        RootComponentData rootData = component as RootComponentData;
                        System.Windows.Point position = (System.Windows.Point)pair.Value;
                        rootData.PositionX = position.X;
                        rootData.PositionY = position.Y;
                    }
                    else
                    {
                        throw new Exception(String.Format("Property {0} not found on component {1}", pair.Key, componentId));
                    }
                }
                else if (pair.Key == "expression" && (component is SpreadsheetRowData || component is SpreadsheetColumnData))
                {
                    // spreadsheet row/column expression
                    string rootId = GetRootComponentId(componentId);
                    SpreadsheetComponentData spreadsheet = mapComponentData[rootId] as SpreadsheetComponentData;
                    string[] parts = componentId.Split('_');
                    if (component is SpreadsheetColumnData)
                    {
                        string columnId = parts[1].Substring(1);
                        int col = spreadsheet.GetColumnIndexFromColumnId(columnId);
                        spreadsheet.SetColumnExpression(col, pair.Value as string);
                    }
                    else if (component is SpreadsheetRowData)
                    {
                        string rowId = parts[1].Substring(1);
                        int row = spreadsheet.GetRowIndexFromRowId(rowId);
                        spreadsheet.SetRowExpression(row, pair.Value as string);
                    }
                }
                else if (pair.Key == "Function" && component is AbstractMacroData)
                {
                    // rename macro
                    this.ChangeMacroName(component as AbstractMacroData, pair.Value as string);
                }
                else if (pair.Key == "varname" && component is MacroParameterData)
                {
                    MacroParameterData param = component as MacroParameterData;
                    param.varname = pair.Value as string;
                    this.RenameMacroParameter(param);
                }
                else if (pair.Key == "varname" && component is LoopParameterData)
                {
                    LoopParameterData param = component as LoopParameterData;
                    param.varname = pair.Value as string;
                    this.RenameLoopParameter(param);
                }
                else
                {
                    PropertyInfo prop = component.GetType().GetProperty(pair.Key, BindingFlags.Public | BindingFlags.Instance);
                    if (null != prop && prop.CanWrite)
                    {
                        prop.SetValue(component, pair.Value, null);
                    }
                    else
                    {
                        throw new Exception(String.Format("Property {0} not found on component {1}", pair.Key, componentId));
                    }
                }
            }
            ShouldDispatchPropertyChanged = true;
            DispatchDataChanged(componentId);
        }

        #region expression component

        public ExpressionComponentData AddExpressionComponent(string expression = "")
        {
            string id = variableNameGenerator.NewVariableName();
            ExpressionComponentData component = new ExpressionComponentData(this, id, expression);
            listRootComponentData.Add(component);
            PrepareExpressionComponent(component);
            return component;
        }
        void PrepareExpressionComponent(ExpressionComponentData component, bool resolveValue = true)
        {
            PrepareComponentData(component);
            graphDependency.AddVertex(component);
            if (resolveValue)
            {
                ResolveComponentValue(component);
            }
        }


        /**
         * if expression is null, don't change the component expression. Just evaluate the value
         **/
        /*public void UpdateExpressionComponent(ExpressionComponentData component, string expression = null)
        {
            if (expression != null)
            {
                component.expression = expression;
            }
            ResolveComponentValue(component);
        }
        public void UpdateExpressionComponent(string id, string expression = null)
        {
            if (mapComponentData.ContainsKey(id))
            {
                ExpressionComponentData component = mapComponentData[id] as ExpressionComponentData;
                if (component != null)
                {
                    UpdateExpressionComponent(component, expression);
                }
                else
                {
                    // TODO: throw exception
                }
            }
            else
            {
                // TODO: throw exception
            }
        }*/

        #endregion

        #region shape component

        public ShapeComponentData AddShapeComponent()
        {
            string id = variableNameGenerator.NewVariableName();
            ShapeComponentData component = new ShapeComponentData(this, id);
            listRootComponentData.Add(component);
            PrepareShapeComponent(component);
            return component;
        }
        void PrepareShapeComponent(ShapeComponentData component)
        {
            PrepareComponentData(component);
            graphDependency.AddVertex(component);
        }

        #endregion

        #region spreadsheet component

        public SpreadsheetComponentData AddSpreadsheetComponent()
        {
            string id = variableNameGenerator.NewVariableName();
            SpreadsheetComponentData component = new SpreadsheetComponentData(this, id);
            listRootComponentData.Add(component);
            PrepareSpreadsheetComponent(component);
            return component;
        }
        void PrepareSpreadsheetComponent(SpreadsheetComponentData component, bool resolveValue = true)
        {
            //  The spreadsheet doesn't have any expression, no need to include in graph
            PrepareComponentData(component);
            graphDependency.AddVertex(component);
            // add the SpreadsheetColumnData
            foreach (SpreadsheetColumnData columnData in component.columnDatas)
            {
                PrepareComponentData(columnData);
                graphDependency.AddVertex(columnData);
            }
            foreach (SpreadsheetRowData rowData in component.rowDatas)
            {
                PrepareComponentData(rowData);
                graphDependency.AddVertex(rowData);
            }
            foreach (List<SpreadsheetCellData> row in component.cells)
            {
                foreach (SpreadsheetCellData cell in row)
                {
                    PrepareComponentData(cell);
                    graphDependency.AddVertex(cell);
                    if (resolveValue)
                    {
                        ResolveComponentValue(cell);
                    }
                }
            }
        }

        void RemoveSpreadsheetComponent(SpreadsheetComponentData component)
        {
            foreach (List<SpreadsheetCellData> row in component.cells)
            {
                foreach (SpreadsheetCellData cell in row)
                {
                    RemoveComponent(cell);
                }
            }
            foreach (SpreadsheetColumnData columnData in component.columnDatas)
            {
                RemoveComponentFromGraph(columnData);
            }
            foreach (SpreadsheetRowData rowData in component.rowDatas)
            {
                RemoveComponentFromGraph(rowData);
            }
            RemoveComponentFromGraph(component);
        }

        public void AddSpreadsheetColumn(SpreadsheetComponentData component, int? col, List<SpreadsheetCellData> restoreCells = null, SpreadsheetColumnData restoreColumn = null)
        {
            int colIndex = col.HasValue ? col.Value : component.columnDatas.Count;

            // check if there's any Range dependence that needs to be updated
            // range dependence needs to be updated if colIndex is one of the column in range except the first column
            HashSet<AbstractComponentData> needsRangeUpdate = new HashSet<AbstractComponentData>();
            if (colIndex > 0 && colIndex < component.columnDatas.Count)
            {
                for (int row = 0; row < component.rowDatas.Count; ++row)
                {
                    SpreadsheetCellData cell = component.cells[row][colIndex];
                    foreach (var edge in graphDependency.InEdges(cell))
                    {
                        if (!needsRangeUpdate.Contains(edge.Source) && edge.Source is SpreadsheetRangeData)
                        {
                            SpreadsheetRangeData rangeData = edge.Source as SpreadsheetRangeData;
                            PointInt? position = rangeData.GetPositionOfCell(cell);
                            if (position.HasValue)
                            {
                                if (position.Value.X > 0)
                                {
                                    needsRangeUpdate.Add(edge.Source);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            List<SpreadsheetCellData> cells = component.AddColumn(col, restoreCells, restoreColumn);

            // add the SpreadsheetColumnData
            SpreadsheetColumnData columnData = component.columnDatas[colIndex];
            PrepareComponentData(columnData);
            graphDependency.AddVertex(columnData);

            foreach (SpreadsheetCellData cell in cells)
            {
                PrepareComponentData(cell);
                graphDependency.AddVertex(cell);
                ResolveComponentValue(cell);                
            }

            // update range dependence
            foreach (AbstractComponentData data in needsRangeUpdate)
            {
                // need to update the out edges because there may be new cells in the range
                ResolveComponentValue(data, true);
            }
        }

        public void AddSpreadsheetRow(SpreadsheetComponentData component, int? row, List<SpreadsheetCellData> restoreCells = null, SpreadsheetRowData restoreRow = null)
        {
            int rowIndex = row.HasValue ? row.Value : component.rowDatas.Count;

            // check if there's any Range dependence that needs to be updated
            // range dependence needs to be updated if rowIndex is one of the row in range except the first row
            HashSet<AbstractComponentData> needsRangeUpdate = new HashSet<AbstractComponentData>();
            if (rowIndex > 0 && rowIndex < component.rowDatas.Count)
            {
                for (int col = 0; col < component.columnDatas.Count; ++col)
                {
                    SpreadsheetCellData cell = component.cells[rowIndex][col];
                    foreach (var edge in graphDependency.InEdges(cell))
                    {
                        if (!needsRangeUpdate.Contains(edge.Source) && edge.Source is SpreadsheetRangeData)
                        {
                            SpreadsheetRangeData rangeData = edge.Source as SpreadsheetRangeData;
                            PointInt? position = rangeData.GetPositionOfCell(cell);
                            if (position.HasValue)
                            {
                                if (position.Value.Y > 0)
                                {
                                    needsRangeUpdate.Add(edge.Source);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            List<SpreadsheetCellData> cells = component.AddRow(row, restoreCells, restoreRow);

            // add the SpreadsheetRowData
            SpreadsheetRowData rowData = component.rowDatas[rowIndex];
            PrepareComponentData(rowData);
            graphDependency.AddVertex(rowData);

            foreach (SpreadsheetCellData cell in cells)
            {
                PrepareComponentData(cell);
                graphDependency.AddVertex(cell);
                ResolveComponentValue(cell);
            }

            // update range dependence
            foreach (AbstractComponentData data in needsRangeUpdate)
            {
                // need to update the out edges because there may be new cells in the range
                ResolveComponentValue(data, true);
            }
        }

        public void DeleteSpreadsheetColumn(SpreadsheetComponentData component, int? col, out List<SpreadsheetCellData> deletedCells, out SpreadsheetColumnData deletedColumn)
        {
            int colIndex = col.HasValue ? col.Value : component.rowDatas.Count - 1;

            // check if there's any Range dependence that needs to be updated
            // range dependence needs to be updated if col is one of the column in range
            HashSet<AbstractComponentData> needsRangeUpdate = new HashSet<AbstractComponentData>();
            for (int row = 0; row < component.rowDatas.Count; ++row)
            {
                SpreadsheetCellData cell = component.cells[row][colIndex];
                foreach (var edge in graphDependency.InEdges(cell))
                {
                    if (!needsRangeUpdate.Contains(edge.Source) && edge.Source is SpreadsheetRangeData)
                    {
                        SpreadsheetRangeData rangeData = edge.Source as SpreadsheetRangeData;
                        PointInt? position = rangeData.GetPositionOfCell(cell);
                        if (position.HasValue)
                        {
                            foreach (var edge2 in graphDependency.InEdges(rangeData))
                            {
                                if (edge2.Source is IExpressionData)
                                {
                                    IExpressionData expressionData = edge2.Source as IExpressionData;
                                    MatchCollection collection = DataHelper.MatchesRangeVariable(expressionData.expression, component.id);
                                    foreach (Match match in collection)
                                    {
                                        if (rangeData.GetDimension().X == 1)
                                        {
                                            // range is reduced to zero dimension
                                            expressionData.SetExpressionRaw(
                                                expressionData.expression.Replace(match.Value, "[]")
                                            );
                                        }
                                        else
                                        {
                                            if (position.Value.X == 0)
                                            {
                                                // first column is removed, the range id must be changed
                                                PointInt startPos = rangeData.spreadsheet.GetPositionFromCellId(rangeData.startId).Value;
                                                rangeData.startId = rangeData.spreadsheet.cells[startPos.Y][startPos.X + 1].id;
                                                expressionData.SetExpressionRaw(
                                                    expressionData.expression.Replace(match.Value, "[" + rangeData.id + "]")
                                                );
                                            }
                                            if (position.Value.X == rangeData.GetDimension().X - 1)
                                            {
                                                // last column is removed, the range id must be changed
                                                PointInt endPos = rangeData.spreadsheet.GetPositionFromCellId(rangeData.endId).Value;
                                                rangeData.endId = rangeData.spreadsheet.cells[endPos.Y][endPos.X - 1].id;
                                                expressionData.SetExpressionRaw(
                                                    expressionData.expression.Replace(match.Value, "[" + rangeData.id + "]")
                                                );
                                            }
                                        }
                                    }
                                }
                            }
                            needsRangeUpdate.Add(edge.Source);
                        }
                    }
                }
            }

            SpreadsheetColumnData columnData = component.columnDatas[colIndex];
            List<SpreadsheetCellData> cells = component.DeleteColumn(colIndex);
            foreach (SpreadsheetCellData cell in cells)
            {
                RemoveComponent(cell.id);
            }
            RemoveComponent(columnData);

            // update range dependence
            foreach (AbstractComponentData data in needsRangeUpdate)
            {
                // need to update the out edges because there may be new cells in the range
                ResolveComponentValue(data, true);
            }

            deletedCells = cells;
            deletedColumn = columnData;
        }

        public void DeleteSpreadsheetRow(SpreadsheetComponentData component, int? row, out List<SpreadsheetCellData> deletedCells, out SpreadsheetRowData deletedRow)
        {
            int rowIndex = row.HasValue ? row.Value : component.rowDatas.Count - 1;

            // check if there's any Range dependence that needs to be updated
            // range dependence needs to be updated if col is one of the column in range
            HashSet<AbstractComponentData> needsRangeUpdate = new HashSet<AbstractComponentData>();
            for (int col = 0; col < component.columnDatas.Count; ++col)
            {
                SpreadsheetCellData cell = component.cells[rowIndex][col];
                foreach (var edge in graphDependency.InEdges(cell))
                {
                    if (!needsRangeUpdate.Contains(edge.Source) && edge.Source is SpreadsheetRangeData)
                    {
                        SpreadsheetRangeData rangeData = edge.Source as SpreadsheetRangeData;
                        PointInt? position = rangeData.GetPositionOfCell(cell);
                        if (position.HasValue)
                        {
                            foreach (var edge2 in graphDependency.InEdges(rangeData))
                            {
                                if (edge2.Source is IExpressionData)
                                {
                                    IExpressionData expressionData = edge2.Source as IExpressionData;
                                    MatchCollection collection = DataHelper.MatchesRangeVariable(expressionData.expression, component.id);
                                    foreach (Match match in collection)
                                    {
                                        if (rangeData.GetDimension().Y == 1)
                                        {
                                            // range is reduced to zero dimension
                                            expressionData.SetExpressionRaw(
                                                expressionData.expression.Replace(match.Value, "[]")
                                            );
                                        }
                                        else
                                        {
                                            if (position.Value.Y == 0)
                                            {
                                                // first row is removed, the range id must be changed
                                                PointInt startPos = rangeData.spreadsheet.GetPositionFromCellId(rangeData.startId).Value;
                                                rangeData.startId = rangeData.spreadsheet.cells[startPos.Y+1][startPos.X].id;
                                                expressionData.SetExpressionRaw(
                                                    expressionData.expression.Replace(match.Value, "[" + rangeData.id + "]")
                                                );
                                            }
                                            if (position.Value.Y == rangeData.GetDimension().Y - 1)
                                            {
                                                // last column is removed, the range id must be changed
                                                PointInt endPos = rangeData.spreadsheet.GetPositionFromCellId(rangeData.endId).Value;
                                                rangeData.endId = rangeData.spreadsheet.cells[endPos.Y-1][endPos.X].id;
                                                expressionData.SetExpressionRaw(
                                                    expressionData.expression.Replace(match.Value, "[" + rangeData.id + "]")
                                                );
                                            }
                                        }
                                    }
                                }
                            }
                            needsRangeUpdate.Add(edge.Source);
                        }
                    }
                }
            }

            SpreadsheetRowData rowData = component.rowDatas[rowIndex];
            List<SpreadsheetCellData> cells = component.DeleteRow(rowIndex);
            foreach (SpreadsheetCellData cell in cells)
            {
                RemoveComponent(cell.id);
            }
            RemoveComponent(rowData);

            // update range dependence
            foreach (AbstractComponentData data in needsRangeUpdate)
            {
                // need to update the out edges because there may be new cells in the range
                ResolveComponentValue(data, true);
            }

            deletedCells = cells;
            deletedRow = rowData;
        }

        public SpreadsheetRangeData CreateSpreadsheetRangeDataFromRangeId(string var)
        {
            if (!IsSpreadsheetRangeId(var))
            {
                return null;
            }
            string[] parts = var.Split(':');
            SpreadsheetComponentData spreadsheet = mapComponentData[GetRootComponentId(var)] as SpreadsheetComponentData;
            SpreadsheetRangeData rangeData = new SpreadsheetRangeData(this, spreadsheet, parts[0], parts[1]);
            return rangeData;
        }

        #endregion

        #region if component

        public IfComponentData AddIfComponent()
        {
            string id = variableNameGenerator.NewVariableName();
            IfComponentData component = new IfComponentData(this, id);
            listRootComponentData.Add(component);
            PrepareIfComponent(component);
            return component;
        }

        void PrepareIfComponent(IfComponentData component, bool resolveValue = true)
        {
            PrepareComponentData(component);
            graphDependency.AddVertex(component);
            PrepareComponentData(component.conditionData);
            graphDependency.AddVertex(component.conditionData);
            PrepareComponentData(component.trueData);
            graphDependency.AddVertex(component.trueData);
            PrepareComponentData(component.falseData);
            graphDependency.AddVertex(component.falseData);

            // the edges to its 3 expresion (condition, true, and false expression)
            Edge<AbstractComponentData> edge = new Edge<AbstractComponentData>(component, component.conditionData);
            graphDependency.AddEdge(edge);
            edge = new Edge<AbstractComponentData>(component, component.trueData);
            graphDependency.AddEdge(edge);
            edge = new Edge<AbstractComponentData>(component, component.falseData);
            graphDependency.AddEdge(edge);

            if (resolveValue)
            {
                ResolveComponentValue(component);
            }
        }

        #endregion

        #region loop component

        public LoopComponentData AddLoopComponent()
        {
            string function = null;
            // create default function name f1, f2, f3, etc
            int i = 1;
            do
            {
                function = "f" + i;
                ++i;
            } while (mapMacroData.ContainsKey(function.ToLower()));

            string id = variableNameGenerator.NewVariableName();
            LoopComponentData component = new LoopComponentData(this, id) { Function = function };
            listRootComponentData.Add(component);
            PrepareLoopComponent(component);
            return component;
        }
        void PrepareLoopComponent(LoopComponentData component, bool resolveValue = true)
        {
            // macro name is case insensitive
            mapMacroData.Add(component.Function.ToLower(), component);
            PrepareComponentData(component);
            graphDependency.AddVertex(component);

            LoopExpressionData expData = component.expressionData;
            PrepareComponentData(expData);
            graphDependency.AddVertex(expData);
            LoopExpressionData conditionData = component.conditionData;
            PrepareComponentData(conditionData);
            graphDependency.AddVertex(conditionData);
            
            // the edges to condition and expression
            Edge<AbstractComponentData> edge = new Edge<AbstractComponentData>(component, component.conditionData);
            graphDependency.AddEdge(edge);
            edge = new Edge<AbstractComponentData>(component, component.expressionData);
            graphDependency.AddEdge(edge);

            // edges to iterations will be added inside AddLoopParameter
            for (int i = 0; i < component.parametersData.Count; ++i)
            {
                AddLoopParameter(component.parametersData[i], component.iterationsData[i], resolveValue);
            }

            if (resolveValue)
            {
                ResolveComponentValue(component);
            }
        }

        public void AddLoopParameter(LoopParameterData paramData, LoopExpressionData iterationData)
        {
            AddLoopParameter(paramData, iterationData, true);
        }
        public void AddLoopParameter(LoopParameterData paramData, LoopExpressionData iterationData, bool resolveValue)
        {
            LoopComponentData component = mapComponentData[paramData.parentId] as LoopComponentData;

            PrepareComponentData(paramData);
            graphDependency.AddVertex(paramData);
            PrepareComponentData(iterationData);
            graphDependency.AddVertex(iterationData);

            // the edge from component to iterationData
            Edge<AbstractComponentData> edge = new Edge<AbstractComponentData>(component, iterationData);
            graphDependency.AddEdge(edge);

            if (resolveValue)
            {
                ResolveComponentValue(paramData);
                ResolveComponentValue(iterationData);
                if (mapComponentData.ContainsKey(paramData.parentId))
                {
                    // need to update components that use the macro expression. all the values will be error because the parameter number change
                    ResolveComponentValue(mapComponentData[paramData.parentId]);
                }
            }
        }

        public void DeleteLoopParameter(LoopParameterData paramData, LoopExpressionData iterationData)
        {
            RemoveComponent(paramData.id);
            RemoveComponent(iterationData.id);
            DispatchDataChanged(paramData.id);
            DispatchDataChanged(iterationData.id);
            // need to update components that use the macro expression. all the values will be error because the parameter number change
            ResolveComponentValue(mapComponentData[paramData.parentId]);
        }

        public void RenameLoopParameter(LoopParameterData paramData)
        {
            ResolveComponentValue(paramData);
        }

        #endregion

        #region macro component

        // create default function name f1, f2, f3, etc
        string GenerateNewMacroName()
        {
            string function = null;
            // create default function name f1, f2, f3, etc
            int i = 1;
            do
            {
                function = "f" + i;
                ++i;
            } while (mapMacroData.ContainsKey(function.ToLower()));
            return function;
        }

        public MacroComponentData AddMacroComponent(string function = null, List<string> parameters = null, string expression = "")
        {
            if (function == null)
            {
                function = GenerateNewMacroName();
            }
            string id = variableNameGenerator.NewVariableName();
            MacroComponentData macro = new MacroComponentData(this, id, parameters, expression) { Function = function };
            listRootComponentData.Add(macro);
            PrepareMacroComponent(macro);
            return macro;
        }
        void PrepareMacroComponent(MacroComponentData macro, bool resolveValue = true)
        {
            // macro name is case insensitive
            mapMacroData.Add(macro.Function.ToLower(), macro);
            PrepareComponentData(macro);
            graphDependency.AddVertex(macro);
            foreach (MacroParameterData param in macro.parametersData)
            {
                AddMacroParameter(param, resolveValue);
            }
            MacroExpressionData expData = macro.expressionData;
            PrepareComponentData(expData);
            graphDependency.AddVertex(expData);

            // the edges to and expression
            Edge<AbstractComponentData> edge = new Edge<AbstractComponentData>(macro, macro.expressionData);
            graphDependency.AddEdge(edge);

            if (resolveValue)
            {
                ResolveComponentValue(expData);
            }
        }

        public void AddMacroParameter(MacroParameterData paramData)
        {
            AddMacroParameter(paramData, true);
        }
        public void AddMacroParameter(MacroParameterData paramData, bool resolveValue)
        {
            PrepareComponentData(paramData);
            graphDependency.AddVertex(paramData);
            if (resolveValue)
            {
                ResolveComponentValue(paramData);
                if (mapComponentData.ContainsKey(paramData.parentId))
                {
                    // need to update components that use the macro expression. all the values will be error because the parameter number change
                    ResolveComponentValue(mapComponentData[paramData.parentId]);
                }
            }
        }

        public void DeleteMacroParameter(MacroParameterData paramData)
        {
            RemoveComponent(paramData.id);
            DispatchDataChanged(paramData.id);
            // need to update components that use the macro expression. all the values will be error because the parameter number change
            ResolveComponentValue(mapComponentData[paramData.parentId]);
        }

        public void RenameMacroParameter(MacroParameterData paramData)
        {
            ResolveComponentValue(paramData);
        }

        #endregion

        void EvaluateComponentParameter(string name, ParameterArgs args, HashSet<string> varList = null, bool evaluateConstants = true)
        {
            if (IsSpreadsheetRangeId(name))
            {
                SpreadsheetRangeData rangeData = null;
                if (!mapComponentData.ContainsKey(name))
                {
                    rangeData = CreateSpreadsheetRangeDataFromRangeId(name);
                    mapComponentData[rangeData.id] = rangeData;
                    graphDependency.AddVertex(rangeData);
                    // create dependencies
                    ResolveSpreadsheetRangeData(rangeData, true, false);
                }
                else
                {
                    rangeData = mapComponentData[name] as SpreadsheetRangeData;
                }
                // someshow the dependency graph should be directed to the spreadsheet, not the cells because there's too many arrows
                if (varList != null)
                {
                    varList.Add(rangeData.id);
                }
                varList.Add(rangeData.id);
                List<List<SpreadsheetCellData>> cells = rangeData.GetCellsTable();
                List<List<object>> values = new List<List<object>>();
                foreach (List<SpreadsheetCellData> cellRow in cells)
                {
                    List<object> valueRow = new List<object>();
                    foreach (SpreadsheetCellData cell in cellRow)
                    {
                        if (!mapComponentValue.ContainsKey(cell.id))
                        {
                            ResolveComponentValue(mapComponentData[cell.id]);
                        }
                        valueRow.Add(mapComponentValue[cell.id]);
                    }
                    values.Add(valueRow);
                }
                args.Result = values;
            }
            else if (mapComponentData.ContainsKey(name))
            {
                // if the referred parameter has not been resolved, resolve it first
                if (!mapComponentValue.ContainsKey(name))
                {
                    ResolveComponentValue(mapComponentData[name]);
                }
                args.Result = mapComponentValue[name];
                if (args.Result == null) args.Result = 0;
                if (varList != null)
                {
                    varList.Add(name);
                }
            }
            else
            {
                if (name == "pi")
                {
                    if (evaluateConstants)
                    {
                        args.Result = Math.PI;
                    }
                    else
                    {
                        args.Result = new RawIdentifierExpression("pi");
                    }
                }
                else if (name == "ec")
                {
                    if (evaluateConstants)
                    {
                        args.Result = Math.E;
                    }
                    else
                    {
                        args.Result = new RawIdentifierExpression("ec");
                    }
                }
                else
                {
                    // TODO: throw exception
                }
            }
        }

        object EvaluateExpressionWithMacroParameter(Expression expression, MacroComponentData macro, FunctionArgs functionArgs)
        {
            expression.EvaluateParameter += delegate(string n, ParameterArgs args)
            {
                EvaluateMacroParameter(macro, n, args, functionArgs);
            };
            expression.EvaluateFunction += delegate(string n, FunctionArgs args)
            {
                EvaluateFunction(n, args);
            };
            expression.ValidateFunction += delegate(string n, FunctionArgs args)
            {
                return ValidateFunction(n, args);
            };
            object result = expression.Evaluate();
            return result;
        }

        void EvaluateMacroParameter(MacroComponentData macro, string name, ParameterArgs parameterArgs, FunctionArgs functionArgs)
        {
            if (name.StartsWith(macro.id + "_"))
            {
                int index = int.Parse(name.Substring(macro.id.Length + 1));
                parameterArgs.Result = functionArgs.Parameters[index].Evaluate();
            }
            else
            {
                EvaluateComponentParameter(name, parameterArgs);
            }
            // if the value is RawExpression, it means it contains macro parameter, so evaluate the raw expression instead
            if (parameterArgs.Result is RawExpression)
            {
                IExpressionData data = mapComponentData[name] as IExpressionData;
                Expression exp = new Expression(data.expression, DefaultEvaluateOptions);                
                parameterArgs.Result = EvaluateExpressionWithMacroParameter(exp, macro, functionArgs);
            }
            while (parameterArgs.Result is Expression)
            {
                Expression resultExp = new Expression((parameterArgs.Result as Expression).ParsedExpression, DefaultEvaluateOptions);
                object result = EvaluateExpressionWithMacroParameter(resultExp, macro, functionArgs);
                parameterArgs.Result = result;
            }
        }

        void EvaluateFunction(string name, FunctionArgs args, HashSet<string> macroList = null)
        {
            if (mapMacroData.ContainsKey(name.ToLower()))
            {
                EvaluateMacroWithArguments(mapMacroData[name.ToLower()], args);
                if (macroList != null)
                {
                    macroList.Add(name);
                }
            }
            else
            {
                foreach (WorksheetData worksheet in listExternalWorksheet)
                {
                    if (worksheet.mapMacroData.ContainsKey(name.ToLower()))
                    {
                        worksheet.EvaluateMacroWithArguments(worksheet.mapMacroData[name.ToLower()], args);
                        // don't add to macro list because external macro doesn't need to be added to dependency graph
                        break;
                    }
                }
                // TODO: throw exception or something if function not found
            }
        }

        // return true if function exists and valid, false if function not found
        // should throw error with explanation if function exists but not valid
        bool ValidateFunction(string name, FunctionArgs args, HashSet<string> macroList = null)
        {
            if (mapMacroData.ContainsKey(name.ToLower()))
            {
                // validate the number of function arguments
                ValidateMacroWithArguments(mapMacroData[name.ToLower()], args);
                if (macroList != null)
                {
                    macroList.Add(name);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        void ValidateMacroWithArguments(AbstractMacroData macro, FunctionArgs functionArgs)
        {
            if (macro.parametersCount != functionArgs.Parameters.Length)
            {
                throw new ArgumentException("Macro \""+macro.Function+"\" takes exactly "+macro.parametersCount+" arguments!");
            }
        }

        object EvaluateExpressionWithLoopParameter(Expression expression, LoopComponentData component, List<object> paramValues)
        {
            expression.EvaluateParameter += delegate(string n, ParameterArgs args)
            {
                EvaluateLoopParameter(component, n, args, paramValues);
            };
            expression.EvaluateFunction += delegate(string n, FunctionArgs args)
            {
                EvaluateFunction(n, args);
            };
            expression.ValidateFunction += delegate(string n, FunctionArgs args)
            {
                return ValidateFunction(n, args);
            };
            return expression.Evaluate();
        }

        void EvaluateLoopParameter(LoopComponentData component, string name, ParameterArgs parameterArgs, List<object> paramValues)
        {
            if (name.StartsWith(component.id + "_p"))
            {
                int index = int.Parse(name.Substring(component.id.Length + 2));
                parameterArgs.Result = paramValues[index];
            }
            else
            {
                EvaluateComponentParameter(name, parameterArgs);
            }
            // if the value is RawExpression, it means it contains macro parameter, so evaluate the raw expression instead
            if (parameterArgs.Result is RawExpression)
            {
                IExpressionData data = mapComponentData[name] as IExpressionData;
                Expression exp = new Expression(data.expression, DefaultEvaluateOptions);
                parameterArgs.Result = EvaluateExpressionWithLoopParameter(exp, component, paramValues);
            }
            while (parameterArgs.Result is Expression)
            {
                Expression resultExp = new Expression((parameterArgs.Result as Expression).ParsedExpression, DefaultEvaluateOptions);
                object result = EvaluateExpressionWithLoopParameter(resultExp, component, paramValues);
                parameterArgs.Result = result;
            }
        }

        void EvaluateLoopWithArguments(LoopComponentData component, FunctionArgs functionArgs)
        {
            List<object> paramValues = new List<object>();
            for (int i=0; i<component.parametersCount; ++i){
                LoopParameterData paramData = component.parametersData[i];
                paramValues.Add(functionArgs.Parameters[i].Evaluate());
                while (paramValues[i] is Expression)
                {
                    Expression resultExp = new Expression((paramValues[i] as Expression).ParsedExpression, DefaultEvaluateOptions);
                    paramValues[i] = EvaluateExpressionWithLoopParameter(resultExp, component, paramValues);
                }
            }
            bool keepLoop = false;
            Expression exp;
            int timeLimit = 1000;   // 1 second
            Stopwatch stopwatch = Stopwatch.StartNew();
            do
            {
                exp = new Expression(component.conditionData.expression, DefaultEvaluateOptions);                
                object conditionObj = EvaluateExpressionWithLoopParameter(exp, component, paramValues);
                // convert condition into boolean
                exp = new Expression("if([x],true,false)", DefaultEvaluateOptions);
                exp.Parameters["x"] = conditionObj;
                keepLoop = (bool)exp.Evaluate();
                if (keepLoop)
                {
                    // iterate parameters
                    List<object> newParamValues = new List<object>();
                    for (int i = 0; i < component.parametersCount; ++i)
                    {
                        LoopExpressionData iterationData = component.iterationsData[i];
                        exp = new Expression(iterationData.expression, DefaultEvaluateOptions);
                        object newValue = EvaluateExpressionWithLoopParameter(exp, component, paramValues);
                        newParamValues.Add(newValue);
                    }
                    paramValues = newParamValues;                    
                }
            } while (keepLoop && stopwatch.ElapsedMilliseconds <= timeLimit);
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > timeLimit)
            {
                throw new Exception("Loop takes more than one second, aborted");
            }
            LoopExpressionData expressionData = component.expressionData;
            exp = new Expression(expressionData.expression, DefaultEvaluateOptions);
            object result = EvaluateExpressionWithLoopParameter(exp, component, paramValues);
            functionArgs.Result = result;
        }

        void EvaluateMacroWithArguments(AbstractMacroData macroData, FunctionArgs functionArgs)
        {
            if (macroData is MacroComponentData)
            {
                MacroComponentData macro = macroData as MacroComponentData;
                ValidateMacroWithArguments(macro, functionArgs);

                // TODO: the parsed expression should be cached for optimization
                Expression exp = new Expression(macro.expression, DefaultEvaluateOptions);                
                object result = EvaluateExpressionWithMacroParameter(exp, macro, functionArgs);
                functionArgs.Result = result;
            }
            else if (macroData is LoopComponentData)
            {
                EvaluateLoopWithArguments(macroData as LoopComponentData, functionArgs);
            }
        }

        void ResolveSpreadsheetHeaderExpressionData(AbstractComponentData component, bool updateOutEdges = true)
        {
            if (!(component is IExpressionData))
            {
                throw new Exception("component does not implement IExpressionData");
            }
            Expression exp = null;
            object result = null;
            HashSet<string> varList = new HashSet<string>();
            HashSet<string> macroList = new HashSet<string>();
            try
            {
                // only need to know the dependencies. the result is not needed
                exp = new Expression((component as IExpressionData).expression, DefaultEvaluateOptions);
                exp.EvaluateParameter += delegate(string name, ParameterArgs args)
                {
                    // when referring other spreadsheet header, prevent exception (because the header doesn't have real value
                    if (name != component.id && mapComponentData.ContainsKey(name) && mapComponentData[name].GetType().Equals(component.GetType()))
                    {
                        // do nothing, just prevent exception from value not found
                        args.Result = 0;
                    }
                    else
                    {
                        EvaluateComponentParameter(name, args, varList);
                    }
                };
                exp.EvaluateFunction += delegate(string name, FunctionArgs args)
                {
                    EvaluateFunction(name, args, macroList);
                };
                exp.ValidateFunction += delegate(string n, FunctionArgs args)
                {
                    return ValidateFunction(n, args);
                };
                result = exp.Evaluate();
                //mapComponentValue[component.id] = result;
            }
            catch (Exception e)
            {
            }
            if (updateOutEdges)
            {
                // update dependency from component
                graphDependency.RemoveOutEdgeIf(component, e => true);
                foreach (string var in varList)
                {
                    graphDependency.AddEdge(new Edge<AbstractComponentData>(component, mapComponentData[var]));
                }
                foreach (string function in macroList)
                {
                    string macroComponentId = mapMacroData[function.ToLower()].id;
                    graphDependency.AddEdge(new Edge<AbstractComponentData>(component, mapComponentData[macroComponentId]));
                }
            }
            DispatchDataChanged(component.id);
            // update components that depend on this component, this will be recursive
            var inEdges = graphDependency.InEdges(component).ToList();
            for (int i = 0; i < inEdges.Count; ++i)
            {
                Edge<AbstractComponentData> edge = inEdges[i];
                // for the parents, no need to update out edges because the dependency doesn't change
                ResolveComponentValue(edge.Source as AbstractComponentData, false);
            }
        }

        void ResolveSpreadsheetRangeData(SpreadsheetRangeData rangeData, bool updateOutEdges, bool resolveDependants)
        {
            if (updateOutEdges)
            {
                // update dependency from component
                graphDependency.RemoveOutEdgeIf(rangeData, e => true);
                List<List<SpreadsheetCellData>> cells = rangeData.GetCellsTable();
                foreach (List<SpreadsheetCellData> cellRow in cells)
                {
                    foreach (SpreadsheetCellData cell in cellRow)
                    {
                        Edge<AbstractComponentData> edge = new Edge<AbstractComponentData>(rangeData, cell);
                        graphDependency.AddEdge(edge);
                    }
                }
            }
            DispatchDataChanged(rangeData.id);
            // update components that depend on this component, this will be recursive
            if (resolveDependants)
            {
                var inEdges = graphDependency.InEdges(rangeData).ToList();
                for (int i = 0; i < inEdges.Count; ++i)
                {
                    Edge<AbstractComponentData> edge = inEdges[i];
                    // for the parents, no need to update out edges because the dependency doesn't change
                    ResolveComponentValue(edge.Source as AbstractComponentData, false);
                }
            }
        }

        void ResolveComponentValue(AbstractComponentData component, bool updateOutEdges = true)
        {
            if (component is ExpressionComponentData)
            {
                ResolveExpressionComponentValue(component as ExpressionComponentData, updateOutEdges);
            }
            else if (component is SpreadsheetCellData)
            {
                ResolveSpreadsheetCellValue(component as SpreadsheetCellData, updateOutEdges);
            }
            else if (component is SpreadsheetRowData || component is SpreadsheetColumnData)
            {
                ResolveSpreadsheetHeaderExpressionData(component as AbstractComponentData, updateOutEdges);
            }
            else if (component is SpreadsheetRangeData)
            {
                ResolveSpreadsheetRangeData(component as SpreadsheetRangeData, updateOutEdges, true);
            }
            else if (component is AbstractParameterData)
            {
                ResolveAbstractParameterDataValue(component as AbstractParameterData);
            }
            else if (component is MacroExpressionData || component is LoopExpressionData)
            {
                ResolveMacroExpressionDataValue(component as AbstractExpressionData, updateOutEdges);
            }
            else if (component is IfComponentData)
            {
                ResolveIfComponentValue(component as IfComponentData, updateOutEdges);
            }
            else if (component is IfExpressionData)
            {
                ResolveIfExpressionValue(component as IfExpressionData, updateOutEdges);                
            }
            else if (component is MacroComponentData)
            {
                ResolveMacroComponent(component as MacroComponentData);
            }
            else if (component is LoopComponentData)
            {
                ResolveLoopComponent(component as LoopComponentData);
            }
        }

        void RemoveComponent(string id)
        {
            RemoveComponent(mapComponentData[id]);
        }
        public void RemoveComponent(AbstractComponentData component)
        {
            if (component is SpreadsheetComponentData)
            {
                RemoveSpreadsheetComponent(component as SpreadsheetComponentData);
            }
            else if (component is MacroComponentData)
            {
                MacroComponentData macro = component as MacroComponentData;
                RemoveComponentFromGraph(macro.expressionData); // the MacroExpressionData
                for (int i = 0; i < macro.parametersData.Count; ++i)
                {
                    RemoveComponentFromGraph(macro.parametersData[i]); // the MacroParameterData
                }
                RemoveComponentFromGraph(macro);
                mapMacroData.Remove(macro.Function.ToLower());    
            }
            else if (component is LoopComponentData)
            {
                LoopComponentData loop = component as LoopComponentData;
                RemoveComponentFromGraph(loop.expressionData);
                RemoveComponentFromGraph(loop.conditionData);
                for (int i = 0; i < loop.parametersData.Count; ++i)
                {
                    RemoveComponentFromGraph(loop.parametersData[i]);
                    RemoveComponentFromGraph(loop.iterationsData[i]);
                }
                RemoveComponentFromGraph(loop);
                mapMacroData.Remove(loop.Function.ToLower());
            }
            else if (component is IfComponentData)
            {
                IfComponentData ifComponent = component as IfComponentData;
                RemoveComponentFromGraph(ifComponent);
                RemoveComponentFromGraph(ifComponent.conditionData);
                RemoveComponentFromGraph(ifComponent.trueData);
                RemoveComponentFromGraph(ifComponent.falseData);
            }
            else
            {
                RemoveComponentFromGraph(component);
            }

            if (component is RootComponentData)
            {
                listRootComponentData.Remove(component as RootComponentData);
            }
        }

        void RemoveComponentFromGraph(AbstractComponentData component)
        {
            mapComponentData.Remove(component.id);
            mapComponentValue.Remove(component.id);
            // update components that depend on this component
            // all the new values will be UNDEFINED since the referred variable is not defined anymore
            var edges = graphDependency.InEdges(component).ToList();
            foreach (Edge<AbstractComponentData> edge in edges)
            {
                // for the parents, no need to update out edges because the dependency doesn't change
                ResolveComponentValue(edge.Source as AbstractComponentData, false);
            }
            graphDependency.ClearEdges(component);
            graphDependency.RemoveVertex(component);
        }

        void ResolveAbstractParameterDataValue(AbstractParameterData component)
        {
            // for parameter component, the displayed value is the variable name
            HashSet<string> varList = new HashSet<string>();
            HashSet<string> macroList = new HashSet<string>();
            mapComponentValue[component.id] = new RawIdentifierExpression(component.varname);
            // there's no out edges from MacroParameterData, but there's in edges
            DispatchDataChanged(component.id);
            // update components that depend on this component, this will be recursive
            foreach (Edge<AbstractComponentData> edge in graphDependency.InEdges(component))
            {
                // for the parents, no need to update out edges because the dependency doesn't change
                ResolveComponentValue(edge.Source as AbstractComponentData, false);
            }
        }

        void ResolveMacroComponent(MacroComponentData component)
        {
            // the macro component only acts as container, so it doesn't have value
            // however, it needs to propagate changes from the expression to dependent vertices
            DispatchDataChanged(component.id);
            // update components that depend on this component, this will be recursive
            foreach (Edge<AbstractComponentData> edge in graphDependency.InEdges(component))
            {
                // for the parents, no need to update out edges because the dependency doesn't change
                ResolveComponentValue(edge.Source as AbstractComponentData, false);
            }
        }

        void ResolveLoopComponent(LoopComponentData component)
        {
            // the loop component only acts as container, so it doesn't have value
            // however, it needs to propagate changes from the subcomponents to dependent vertices
            DispatchDataChanged(component.id);
            /*foreach (LoopExpressionData iterationData in component.iterationsData)
            {
                ResolveComponentValue(iterationData);
            }
            ResolveComponentValue(component.conditionData);
            ResolveComponentValue(component.expressionData);*/
            // update components that depend on this component, this will be recursive
            foreach (Edge<AbstractComponentData> edge in graphDependency.InEdges(component))
            {
                // for the parents, no need to update out edges because the dependency doesn't change
                ResolveComponentValue(edge.Source as AbstractComponentData, false);
            }
        }

        void ResolveMacroExpressionDataValue(AbstractExpressionData component, bool updateOutEdges = true)
        {
            Expression exp = null;
            object result = null;
            HashSet<string> varList = new HashSet<string>();
            HashSet<string> macroList = new HashSet<string>();
            try
            {
                // prefers raw result rather than real value
                exp = new Expression(component.expression, DefaultEvaluateOptions | EvaluateOptions.RawResult);
                exp.EvaluateParameter += delegate(string name, ParameterArgs args)
                {
                    // don't define the constants so that "e" and "pi" is displayed as it is
                    EvaluateComponentParameter(name, args, varList, false);
                };
                exp.EvaluateFunction += delegate(string name, FunctionArgs args)
                {
                    EvaluateFunction(name, args, macroList);
                };
                exp.ValidateFunction += delegate(string n, FunctionArgs args)
                {
                    return ValidateFunction(n, args, macroList);
                };
                result = exp.Evaluate();
                mapComponentValue[component.id] = result;
            }
            catch (Exception e)
            {
            }
            if (result == null)
            {
                // if the expression cannot be evaluated, just store the expression
                mapComponentValue[component.id] = exp;
            }
            if (updateOutEdges)
            {
                // update dependency from component
                graphDependency.RemoveOutEdgeIf(component, e => true);
                foreach (string var in varList)
                {
                    graphDependency.AddEdge(new Edge<AbstractComponentData>(component, mapComponentData[var]));
                }
                foreach (string function in macroList)
                {
                    string macroComponentId = mapMacroData[function.ToLower()].id;
                    graphDependency.AddEdge(new Edge<AbstractComponentData>(component, mapComponentData[macroComponentId]));
                }
            }
            DispatchDataChanged(component.id);
            // update components that depend on this component, this will be recursive
            foreach (Edge<AbstractComponentData> edge in graphDependency.InEdges(component))
            {
                // for the parents, no need to update out edges because the dependency doesn't change
                ResolveComponentValue(edge.Source as AbstractComponentData, false);
            }

            ResolveBrokenComponentValues();
        }

        public void AddShapeConnection(string sourceId, string targetId)
        {
            ShapeComponentData source = mapComponentData[sourceId] as ShapeComponentData;
            ShapeComponentData target = mapComponentData[targetId] as ShapeComponentData;
            Edge<AbstractComponentData> edge;
            if (graphDependency.TryGetEdge(source, target, out edge))
            {
                // already exist
            }
            else
            {
                edge = new Edge<AbstractComponentData>(source, target);
                graphDependency.AddEdge(edge);
            }
        }

        public void RemoveShapeConnection(string sourceId, string targetId)
        {
            ShapeComponentData source = mapComponentData[sourceId] as ShapeComponentData;
            ShapeComponentData target = mapComponentData[targetId] as ShapeComponentData;
            Edge<AbstractComponentData> edge;
            if (graphDependency.TryGetEdge(source, target, out edge)){
                graphDependency.RemoveEdge(edge);
            }
        }

        /**
         * check whether it's possible to create edge, by checking for cyclic dependency
         **/
        public bool CanCreateEdge(AbstractComponentData source, AbstractComponentData target)
        {
            if (target is SpreadsheetRangeData)
            {
                SpreadsheetRangeData rangeData = target as SpreadsheetRangeData;
                List<List<SpreadsheetCellData>> cells = rangeData.GetCellsTable();
                // check whether there's cyclic dependency to the cells one by one
                foreach (List<SpreadsheetCellData> cellRow in cells)
                {
                    foreach (SpreadsheetCellData cell in cellRow)
                    {
                        if (!CanCreateEdge(source, cell))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            else
            {

                Edge<AbstractComponentData> edge;
                if (graphDependency.TryGetEdge(source, target, out edge))
                {
                    // if the edge already exists, no problem
                    return true;
                }
                else
                {
                    // spreadsheet column data can only be linked by another column data
                    if (target is SpreadsheetColumnData && !(source is SpreadsheetColumnData))
                    {
                        return false;
                    }
                    if (target is SpreadsheetRowData && !(source is SpreadsheetRowData))
                    {
                        return false;
                    }
                    if (source is SpreadsheetColumnData)
                    {
                        // column data cannot link to:
                        // - the corresponding column cells
                        // - column data from other spreadsheets
                        if (target is SpreadsheetCellData)
                        {
                            if ((target as SpreadsheetCellData).GetColumnId() == (source as SpreadsheetColumnData).GetColumnId())
                            {
                                return false;
                            }
                        }
                        else if (target is SpreadsheetColumnData)
                        {
                            if (this.GetRootComponentId(target.id) != this.GetRootComponentId(source.id))
                            {
                                return false;
                            }
                        }
                    }
                    if (source is SpreadsheetRowData)
                    {
                        // row data cannot link to:
                        // - the corresponding row cells
                        // - row data from other spreadsheets
                        if (target is SpreadsheetCellData)
                        {
                            if ((target as SpreadsheetCellData).GetRowId() == (source as SpreadsheetRowData).GetRowId())
                            {
                                return false;
                            }
                        }
                        else if (target is SpreadsheetRowData)
                        {
                            if (this.GetRootComponentId(target.id) != this.GetRootComponentId(source.id))
                            {
                                return false;
                            }
                        }
                    }

                    edge = new Edge<AbstractComponentData>(source, target);
                    graphDependency.AddEdge(edge);
                    IDictionary<AbstractComponentData, int> components;
                    int componentCount = graphDependency.StronglyConnectedComponents(out components);
                    graphDependency.RemoveEdge(edge);
                    // if the added edge creates a strongly connected components with more than 1 member, it means cyclic dependency
                    // in other words, the component number of the source and target will be the same
                    if (components[source] != components[target])
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        void ResolveAbstractExpressionDataValue(AbstractComponentData component, bool updateOutEdges = true)
        {
            if (!(component is IExpressionData))
            {
                throw new Exception("component does not implement IExpressionData");
            }
            Expression exp = null;
            object result = null;
            HashSet<string> varList = new HashSet<string>();
            HashSet<string> macroList = new HashSet<string>();
            try
            {
                exp = new Expression((component as IExpressionData).expression, DefaultEvaluateOptions);
                exp.EvaluateParameter += delegate(string name, ParameterArgs args)
                {
                    EvaluateComponentParameter(name, args, varList);
                };
                exp.EvaluateFunction += delegate(string name, FunctionArgs args)
                {
                    EvaluateFunction(name, args, macroList);
                };
                exp.ValidateFunction += delegate(string n, FunctionArgs args)
                {
                    return ValidateFunction(n, args);
                };
                result = exp.Evaluate();
                mapComponentValue[component.id] = result;
            }
            catch (Exception e)
            {
            }
            if (result == null)
            {
                // if the expression cannot be evaluated, just store the expression
                mapComponentValue[component.id] = exp;
            }
            if (updateOutEdges)
            {
                // update dependency from component
                graphDependency.RemoveOutEdgeIf(component, e => true);
                foreach (string var in varList)
                {
                    graphDependency.AddEdge(new Edge<AbstractComponentData>(component, mapComponentData[var]));
                }
                foreach (string function in macroList)
                {
                    string macroComponentId = mapMacroData[function.ToLower()].id;
                    graphDependency.AddEdge(new Edge<AbstractComponentData>(component, mapComponentData[macroComponentId]));
                }
            }
            DispatchDataChanged(component.id);
            // update components that depend on this component, this will be recursive
            var inEdges = graphDependency.InEdges(component).ToList();
            for (int i = 0; i < inEdges.Count; ++i)
            {
                Edge<AbstractComponentData> edge = inEdges[i];
                // for the parents, no need to update out edges because the dependency doesn't change
                ResolveComponentValue(edge.Source as AbstractComponentData, false);
            }
        }

        void ResolveExpressionComponentValue(ExpressionComponentData component, bool updateOutEdges = true)
        {
            ResolveAbstractExpressionDataValue(component, updateOutEdges);
        }
        
        void ResolveIfComponentValue(IfComponentData component, bool updateOutEdges)
        {
            // the out edges don't need to be renewed
            ResolveAbstractExpressionDataValue(component, false);
        }
        void ResolveIfExpressionValue(IfExpressionData component, bool updateOutEdges)
        {
            ResolveAbstractExpressionDataValue(component, updateOutEdges);
        }

        void ResolveSpreadsheetCellValue(SpreadsheetCellData component, bool updateOutEdges = true)
        {
            if (component.dataTableData != null && !component.dataTableData.IsHeader)
            {
                ResolveSpreadsheetCellAsDataTableContent(component, updateOutEdges);
            }
            else
            {
                // dataTable header is treated as normal expression
                ResolveAbstractExpressionDataValue(component, updateOutEdges);
            }
        }

        /**
         * precondition: cell.dataTableData is not null
         **/
        void ResolveSpreadsheetCellAsDataTableContent(SpreadsheetCellData cell, bool updateOutEdges = true)
        {
            DataTableData dataTableData = cell.dataTableData;
            SpreadsheetRangeData rangeData = CreateSpreadsheetRangeDataFromRangeId(dataTableData.rangeId);
            AbstractComponentData rowInputData = null;
            if (dataTableData.rowInputId != null){
                if (mapComponentData.ContainsKey(dataTableData.rowInputId))
                {
                    rowInputData = mapComponentData[dataTableData.rowInputId];
                }
            }
            AbstractComponentData colInputData = null;
            if (dataTableData.colInputId != null)
            {
                if (mapComponentData.ContainsKey(dataTableData.colInputId))
                {
                    colInputData = mapComponentData[dataTableData.colInputId];
                }
            }

            if (rangeData == null || (rowInputData == null && colInputData == null))
            {
                // invalid
                mapComponentValue[cell.id] = null;
            }
            else
            {
                SpreadsheetCellData referenceCell = null;
                if (rowInputData != null && colInputData == null)
                {
                    // row input only, reference cell is first column second row
                    referenceCell = rangeData.GetCellAtPosition(new PointInt(0, 1));
                }
                else if (colInputData != null && rowInputData == null)
                {
                    // col input only, reference cell is first row second column
                    referenceCell = rangeData.GetCellAtPosition(new PointInt(1, 0));
                }
                else
                {
                    // both row & col input, reference cell is top left
                    referenceCell = rangeData.GetCellAtPosition(new PointInt(0, 0));
                }

                PointInt position = rangeData.GetPositionOfCell(cell).Value;
                SpreadsheetCellData rowSubstituteData = null;
                SpreadsheetCellData colSubstituteData = null;
                Dictionary<string, object> mapSubstituteValue = new Dictionary<string, object>();
                if (rowInputData != null)
                {
                    rowSubstituteData = rangeData.GetCellAtPosition(new PointInt(position.X, 0));
                    if (mapComponentValue.ContainsKey(rowSubstituteData.id))
                    {
                        mapSubstituteValue[rowInputData.id] = mapComponentValue[rowSubstituteData.id];
                    }
                    else
                    {
                        mapSubstituteValue[rowInputData.id] = null;
                    }
                }
                if (colInputData != null)
                {
                    colSubstituteData = rangeData.GetCellAtPosition(new PointInt(0, position.Y));
                    if (mapComponentValue.ContainsKey(colSubstituteData.id))
                    {
                        mapSubstituteValue[colInputData.id] = mapComponentValue[colSubstituteData.id];
                    }
                    else
                    {
                        mapSubstituteValue[colInputData.id] = null;
                    }
                }

                // method 1: directly substitute the input expression, check the change, then reset it
                // temporarily remove dependence to prevent stack overflow
                EnableExpressionCache = false;
                ShouldDispatchEvents = false;
                var cellOutEdges = graphDependency.OutEdges(cell).ToList();
                graphDependency.RemoveOutEdgeIf(cell, e => true);
                var referenceCellInEdges = graphDependency.InEdges(referenceCell).ToList();
                graphDependency.RemoveInEdgeIf(referenceCell, e => true);

                string rowOriExpression = null;
                string colOriExpression = null;
                if (rowInputData != null)
                {
                    rowOriExpression = (rowInputData as IExpressionData).expression;
                    (rowInputData as IExpressionData).expression = rowSubstituteData.expression;
                }
                if (colInputData != null)
                {
                    colOriExpression = (colInputData as IExpressionData).expression;
                    (colInputData as IExpressionData).expression = colSubstituteData.expression;
                }
                if (mapComponentValue.ContainsKey(referenceCell.id))
                {
                    mapComponentValue[cell.id] = mapComponentValue[referenceCell.id];
                }
                else
                {
                    mapComponentValue[cell.id] = new Expression(referenceCell.expression, DefaultEvaluateOptions);
                }
                if (rowInputData != null)
                {
                    (rowInputData as IExpressionData).expression = rowOriExpression;
                }
                if (colInputData != null)
                {
                    (colInputData as IExpressionData).expression = colOriExpression;
                }
                // restore edges
                foreach (var edge in cellOutEdges)
                {
                    graphDependency.AddEdge(edge);
                }
                foreach (var edge in referenceCellInEdges)
                {
                    graphDependency.AddEdge(edge);
                }
                ShouldDispatchEvents = true;
                EnableExpressionCache = true;
                

                // method 2: using substituted value map
                // algorithm is wrong because currently it can't substitude more than 1 link long
                /*Expression exp = null;
                object result = null;
                try
                {
                    exp = new Expression(referenceCell.expression, DefaultEvaluateOptions);
                    exp.EvaluateParameter += delegate(string name, ParameterArgs args)
                    {
                        if (mapSubstituteValue.ContainsKey(name))
                        {
                            args.Result = mapSubstituteValue[name];
                        }
                        else
                        {
                            EvaluateComponentParameter(name, args);
                        }
                    };
                    exp.EvaluateFunction += delegate(string name, FunctionArgs args)
                    {
                        EvaluateFunction(name, args);
                    };
                    exp.ValidateFunction += delegate(string n, FunctionArgs args)
                    {
                        return ValidateFunction(n, args);
                    };
                    result = exp.Evaluate();
                    mapComponentValue[cell.id] = result;
                }
                catch (Exception e)
                {
                }
                if (result == null)
                {
                    // if the expression cannot be evaluated, just store the expression
                    mapComponentValue[cell.id] = exp;
                }*/


                if (updateOutEdges)
                {
                    // update dependency from cell to referenceCell, row/col substitute
                    // no need to link to row/col input because if the referenceCell depends on row/col input, it will be propagated
                    //graphDependency.RemoveOutEdgeIf(cell, e => true);
                    graphDependency.AddEdge(new Edge<AbstractComponentData>(cell, referenceCell));
                    if (rowSubstituteData != null)
                    {
                        graphDependency.AddEdge(new Edge<AbstractComponentData>(cell, rowSubstituteData));
                    }
                    if (colSubstituteData != null)
                    {
                        graphDependency.AddEdge(new Edge<AbstractComponentData>(cell, colSubstituteData));
                    }
                    /*if (rowInputData != null)
                    {
                        graphDependency.AddEdge(new Edge<AbstractComponentData>(cell, rowInputData));
                    }
                    if (colInputData != null)
                    {
                        graphDependency.AddEdge(new Edge<AbstractComponentData>(cell, colInputData));
                    }*/
                }
                DispatchDataChanged(cell.id);
                // update components that depend on this component, this will be recursive
                foreach (Edge<AbstractComponentData> edge in graphDependency.InEdges(cell))
                {
                    // for the parents, no need to update out edges because the dependency doesn't change
                    ResolveComponentValue(edge.Source as AbstractComponentData, false);
                }
            }
        }

        [OnDeserializing()]
        public void OnDeserializing(StreamingContext context)
        {
            Construct();
        }
        public void AfterDeserialized(bool asExternal)
        {
            EnableExpressionCache = false;
            // only listRootComponentData has contents, the other data needs to be filled
            foreach (RootComponentData component in listRootComponentData){
                component.valueStore = this;                
                PrepareRootComponent(component, false);
            }
            if (!asExternal)
            {
                LoadAllExternalWorksheets();
            }
            ResolveNullComponentValues();
            EnableExpressionCache = true;
        }

        public void TriggerEdgeForComponent(string id)
        {
            AbstractComponentData component = mapComponentData[id];
            foreach (var edge in graphDependency.InEdges(component))
            {
                graphDependency_EdgeAdded(edge);
            }
            foreach (var edge in graphDependency.OutEdges(component))
            {
                graphDependency_EdgeAdded(edge);
            }
        }

        public void TriggerAllEdges()
        {
            // dispatch edgeAdded so all connections are visualized
            foreach (Edge<AbstractComponentData> edge in graphDependency.Edges)
            {
                graphDependency_EdgeAdded(edge);
            }
        }

        private void RepairBrokenShapeConnection()
        {
            // add shape connections
            foreach (RootComponentData component in listRootComponentData)
            {
                if (component is ShapeComponentData)
                {
                    ShapeComponentData shape = component as ShapeComponentData;
                    foreach (string targetId in shape.connections)
                    {
                        AddShapeConnection(shape.id, targetId);
                    }
                }
            }
        }

        private void ResolveNullComponentValues()
        {
            var keys = mapComponentData.Keys.ToList();
            foreach (string key in keys)
            {
                if (!mapComponentValue.ContainsKey(key))
                {
                    ResolveComponentValue(mapComponentData[key]);
                }
            }
            RepairBrokenShapeConnection();
        }

        // resolve component that has null or #ERR value
        public void ResolveBrokenComponentValues()
        {
            var keys = mapComponentData.Keys.ToList();
            foreach (string key in keys)
            {
                if (!mapComponentValue.ContainsKey(key) || mapComponentValue[key] is Expression)
                {
                    // LoopComponentData can have Expression as value, no need to resolve
                    if (!(mapComponentData[key] is LoopComponentData))
                    {
                        ResolveComponentValue(mapComponentData[key]);
                    }
                }
            }
            RepairBrokenShapeConnection();
        }

        #region external worksheets

        public void AddExternalWorksheet(string path)
        {
            Uri externalFile = new Uri(path);
            Uri currentFile = new Uri(currentFilePath);
            if (currentFile.Equals(externalFile))
            {
                throw new Exception("Cannot include current worksheet! Please choose other file");
            }
            else
            {
                Uri relativeUri = currentFile.MakeRelativeUri(externalFile);
                string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

                if (listExternalWorksheetPaths.Contains(relativePath))
                {
                    throw new Exception("This worksheet is already included!");
                }
                else
                {
                    WorksheetSerializer serializer = new WorksheetSerializer();
                    WorksheetData worksheet = serializer.LoadWorksheet(path, true);
                    listExternalWorksheet.Add(worksheet);
                    listExternalWorksheetPaths.Add(relativePath);
                }

            }
        }

        public void RemoveExternalWorksheet(int index)
        {
            listExternalWorksheet.RemoveAt(index);
            listExternalWorksheetPaths.RemoveAt(index);
        }

        void LoadAllExternalWorksheets()
        {
            WorksheetSerializer serializer = new WorksheetSerializer();
            List<string> failedWorksheets = new List<string>();
            foreach (string relativePath in listExternalWorksheetPaths)
            {
                string currentFolderPath = new FileInfo(currentFilePath).Directory.FullName;
                string externalPath = Path.Combine(currentFolderPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
                try
                {
                    WorksheetData worksheet = serializer.LoadWorksheet(externalPath, true);
                    listExternalWorksheet.Add(worksheet);
                }
                catch (Exception e)
                {
                    failedWorksheets.Add(relativePath);
                }
            }
            if (failedWorksheets.Count > 0)
            {
                StringBuilder str = new StringBuilder();
                str.Append("Cannot load the following external worksheets. Please make sure the files are in correct places and try again.\n\n");
                foreach (string path in failedWorksheets){
                    str.Append(path+"\n");
                }
                System.Windows.MessageBox.Show(str.ToString());
            }
        }

        #endregion

        #region goal seeking
        
        /**
         * precondition: targetData contains linked variable, variableData contains double value
         **/
        public void Goalseek(IExpressionData targetData, double targetValue, IExpressionData variableData, NotifyProgressFunction notifyFunc)
        {
            // disable cache for faster calculation
            EnableExpressionCache = false;
            ShouldDispatchEvents = false;
            string originalExpression = variableData.expression;
            double initialValue = 0.0;  // default value if expression is empty
            if (variableData.expression.Length > 0)
            {
                initialValue = Convert.ToDouble(variableData.expression);
            }
            SimulatedAnnealingParams param = new SimulatedAnnealingParams();
            param.stepSize = 1.0;
            param.initialTemp = 1000.0;
            param.maxIteration = 1000;
            param.freezeIteration = 25;
            param.boltzmanConstant = 0.01;

            Console.WriteLine("Initial value {0} target value {1}", initialValue, targetValue);

            Random rand = new Random();
            // energy function must return 0 if the energy = targetValue, and nonnegative otherwise
            EnergyFunction energyFunction = delegate(double x)
            {
                Console.Write("EnergyFunction input {0} ", x);
                if (Double.IsNaN(x)) return Double.NaN;

                // change variableData and check the propagated change to targetData value
                variableData.expression = x.ToString(CultureInfo.InvariantCulture);
                double value;
                try
                {
                    object compValue = GetComponentValue((targetData as AbstractComponentData).id);
                    Console.WriteLine("comp val {0} ", compValue);
                    value = Convert.ToDouble(compValue);
                    return Math.Abs(value - targetValue);
                }
                catch (Exception e)
                {
                    return Double.NaN;
                }
            };

            double bestState = SimulatedAnnealing.FindMinimumState(initialValue, energyFunction, param, notifyFunc);
            // reset the original expression to store it on Undo stack
            variableData.SetExpressionRaw(originalExpression);
            ShouldDispatchEvents = true;
            variableData.expression = bestState.ToString(CultureInfo.InvariantCulture);

            EnableExpressionCache = true;
        }

        #endregion

        #region data table

        public DataTableData SetDatatable(DataTableData dataTableData)
        {
            SpreadsheetRangeData rangeData = this.CreateSpreadsheetRangeDataFromRangeId(dataTableData.rangeId);
            AbstractComponentData rowData = dataTableData.rowInputId != null ? mapComponentData[dataTableData.rowInputId] : null;
            AbstractComponentData colData = dataTableData.colInputId != null ? mapComponentData[dataTableData.colInputId] : null;
            return SetDatatable(rangeData, rowData, colData);
        }

        /**
         *  precondition: rangeData, rowData, and columnData is valid
         *  rangeData dimension is 2x2 minimum
         *  either or both the rowData and columnData is set, 1 could be null
         **/
        public DataTableData SetDatatable(SpreadsheetRangeData rangeData, AbstractComponentData rowData, AbstractComponentData columnData)
        {
            string rowDataId = rowData != null ? rowData.id : null;
            string columnDataId = columnData != null ? columnData.id : null;                
            List<List<SpreadsheetCellData>> cells = rangeData.GetCellsTable();
            for (int row = 0; row < cells.Count; ++row)
            {
                List<SpreadsheetCellData> cellRow = cells[row];
                for (int col = 0; col < cellRow.Count; ++col)
                {
                    bool isHeader = (row == 0 || col == 0);
                    DataTableData dataTableData = new DataTableData(rangeData.id, rowDataId, columnDataId, isHeader);
                    cellRow[col].dataTableData = dataTableData;
                }
            }
            // return a dummy DataTableData for storing in Undo stack
            DataTableData ret = new DataTableData(rangeData.id, rowDataId, columnDataId, false);
            return ret;
        }

        public void DeleteDataTable(DataTableData data)
        {
            SpreadsheetRangeData rangeData = CreateSpreadsheetRangeDataFromRangeId(data.rangeId);
            List<List<SpreadsheetCellData>> cells = rangeData.GetCellsTable();
            for (int row = 0; row < cells.Count; ++row)
            {
                List<SpreadsheetCellData> cellRow = cells[row];
                for (int col = 0; col < cellRow.Count; ++col)
                {
                    cellRow[col].dataTableData = null;
                }
            }
        }

        #endregion

        public void PrintAllComponents()
        {
            foreach (AbstractComponentData component in mapComponentData.Values)
            {
                object value;
                mapComponentValue.TryGetValue(component.id, out value);
                if (value is Expression)
                {
                    Console.Out.WriteLine(component.id + " = " + (value as Expression).ParsedExpression);
                }
                else
                {
                    Console.Out.WriteLine(component.id + " = " + value.ToString());
                }
            }
        }

        public void PrintAllMacros()
        {
            foreach (KeyValuePair<string, AbstractMacroData> pair in mapMacroData)
            {
                AbstractMacroData macroData = pair.Value;
                if (macroData is MacroComponentData)
                {
                    MacroComponentData macro = macroData as MacroComponentData;
                    Console.Out.WriteLine(macro.id + " : " + pair.Key + " = " + macro.expression);
                }
                else if (macroData is LoopComponentData)
                {
                    LoopComponentData component = macroData as LoopComponentData;
                    Console.Out.WriteLine(component.id + " : " + pair.Key + " = " + component.expressionData.expression);
                }
            }
        }

        public void PrintAllDependencies()
        {
            foreach (Edge<AbstractComponentData> edge in graphDependency.Edges)
            {
                Console.Out.WriteLine(edge.Source.id + " -> " + edge.Target.id);
            }
        }
    }
}

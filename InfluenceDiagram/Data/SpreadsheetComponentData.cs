using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.ComponentModel;

namespace InfluenceDiagram.Data
{
    public struct PointInt { 
        public int X; 
        public int Y;
        public PointInt(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }

    [DataContract]
    [Serializable]
    [KnownType(typeof(SpreadsheetCellData))]
    [KnownType(typeof(SpreadsheetColumnData))]
    public class SpreadsheetComponentData: RootComponentData
    {
        public override string typeLabel
        {
            get { return "Spreadsheet"; }
        }

        override public IComponentValueStore valueStore
        {
            get { return _valueStore; }
            set
            {
                _valueStore = value;
                if (cells != null)
                {
                    foreach (List<SpreadsheetCellData> row in cells)
                    {
                        foreach (SpreadsheetCellData cell in row)
                        {
                            cell.valueStore = value;
                        }
                    }
                }
            }
        }

        public override string id
        {
            get
            {
                return base.id;
            }
            set
            {
                string oldId = base.id;
                base.id = value;
                if (cells != null)
                {
                    for (int row = 0; row < cells.Count; ++row)
                    {
                        for (int col = 0; col < columnDatas.Count; ++col)
                        {
                            cells[row][col].id = this.GetCellId(row, col);
                            cells[row][col].ReplaceExpressionVariableId(oldId, value);
                        }
                    }
                }
                if (columnDatas != null)
                {
                    for (int i = 0; i < columnDatas.Count; ++i)
                    {
                        columnDatas[i].id = GetColumnId(columnDatas[i].partId);
                        columnDatas[i].ReplaceExpressionVariableId(oldId, value);
                    }
                }
                if (rowDatas != null)
                {
                    for (int i = 0; i < rowDatas.Count; ++i)
                    {
                        rowDatas[i].id = GetRowId(rowDatas[i].partId);
                        rowDatas[i].ReplaceExpressionVariableId(oldId, value);
                    }
                }
            }
        }

        // cells are listed row first then column
        [DataMember]
        public ObservableCollection<List<SpreadsheetCellData>> cells { get; private set; }
        // column/row order does not correspond directly to id since user can delete row/column in the middle of spreadsheet
        // and in the future maybe insert in the middle too
        [DataMember]
        VariableNameGenerator rowIdGenerator;
        [DataMember]
        VariableNameGenerator columnIdGenerator;
        //[DataMember]
        //public List<string> rowIds { get; private set; }
        [DataMember]
        public List<SpreadsheetColumnData> columnDatas { get; private set; }
        [DataMember]
        public List<SpreadsheetRowData> rowDatas { get; private set; }

        /**
         *  constructor always create spreadsheet with 1 column and 1 row
         **/
        public SpreadsheetComponentData(IComponentValueStore valueStore, string id)
            : base(valueStore)
        {
            this.id = id;
            rowIdGenerator = new VariableNameGenerator("");
            columnIdGenerator = new VariableNameGenerator("");
            //rowIds = new List<string>();
            columnDatas = new List<SpreadsheetColumnData>();
            rowDatas = new List<SpreadsheetRowData>();
            cells = new ObservableCollection<List<SpreadsheetCellData>>();
            AddRow();
            AddColumn();
        }

        /**
         * column 0 = A, 1 = B, ..., 25 = Z, 26 = AA, ..., 701 = ZZ, 702 = AAA, ...
         **/
        public static string GetDefaultColumnName(int col)
        {
            string name = "";
            col += 1;
            while (col > 0)
            {
                int rem = (col - 1) % 26 + 1; // remainder from 1 (A) to 26 (Z)
                name = Convert.ToChar(Convert.ToInt16('A') + rem - 1) + name;
                col = (col - rem) / 26;
            }
            return name;
        }

        public static string GetDefaultRowName(int row)
        {
            return (row + 1).ToString();
        }

        public static string GetDefaultCellLabel(PointInt? p)
        {
            if (p.HasValue)
            {
                return GetDefaultColumnName(p.Value.X) + GetDefaultRowName(p.Value.Y);
            }
            else
            {
                return "";
            }
        }

        public int GetColumnIndexFromColumnId(string colId)
        {
            return columnDatas.FindIndex(colData => colData.GetColumnId() == colId);
        }

        public int GetRowIndexFromRowId(string rowId)
        {
            return rowDatas.FindIndex(rowData => rowData.GetRowId() == rowId);
        }

        public SpreadsheetColumnData GetColumnDataFromColumnId(string colId)
        {
            return columnDatas[GetColumnIndexFromColumnId(colId)];
        }

        public SpreadsheetRowData GetRowDataFromRowId(string rowId)
        {
            return rowDatas[GetRowIndexFromRowId(rowId)];
        }

        public PointInt? GetPositionFromCellId(string cellId)
        {
            string[] parts = cellId.Split('_');
            string rowId = parts[1];
            string colId = parts[2];
            int colIndex = GetColumnIndexFromColumnId(colId);
            int rowIndex = GetRowIndexFromRowId(rowId);
            if (colIndex < 0 || rowIndex < 0) return null;
            return new PointInt(colIndex, rowIndex);
        }

        string GetCellId(int row, int column)
        {
            return this.id + "_" + rowDatas[row].partId + "_" + columnDatas[column].partId;
        }

        string GetColumnId(string partId)
        {
            return this.id + "_c" + partId;
        }

        string GetRowId(string partId)
        {
            return this.id + "_r" + partId;
        }

        public List<SpreadsheetCellData> AddRow(int? row = null, List<SpreadsheetCellData> restoreCells = null, SpreadsheetRowData restoreRow = null)
        {
            int rowIndex = row.HasValue ? row.Value : rowDatas.Count;
            if (rowIndex > 0 && rowIndex < rowDatas.Count)
            {
                // check for dataTable. Cannot insert between dataTable, so only the first row of dataTable can be inserted
                foreach (SpreadsheetCellData cell in cells[rowIndex])
                {
                    if (cell.dataTableData != null)
                    {
                        SpreadsheetRangeData rangeData = valueStore.CreateSpreadsheetRangeDataFromRangeId(cell.dataTableData.rangeId);
                        PointInt? position = rangeData.GetPositionOfCell(cell);
                        if (position.Value.Y > 0)
                        {
                            throw new Exception("Cannot change part of a data table");
                        }
                    }
                }
            }

            if (restoreRow == null)
            {
                string newRowId = rowIdGenerator.NewVariableName();
                //rowIds.Insert(rowIndex, newRowId);
                rowDatas.Insert(rowIndex, new SpreadsheetRowData(valueStore) { id = GetRowId(newRowId) });
            }
            else
            {
                rowDatas.Insert(rowIndex, restoreRow);
            }

            if (restoreCells == null)
            {
                List<SpreadsheetCellData> newRow = new List<SpreadsheetCellData>();
                for (int col = 0; col < columnDatas.Count; ++col)
                {
                    string expression = ResolveColumnExpressionForRow(columnDatas[col].expression, rowIndex);
                    newRow.Add(new SpreadsheetCellData(this.valueStore, GetCellId(rowIndex, col), expression));
                }
                cells.Insert(rowIndex, newRow);
                return newRow;
            }
            else
            {
                cells.Insert(rowIndex, restoreCells);
                return restoreCells;
            }
        }

        public List<SpreadsheetCellData> AddColumn(int? col = null, List<SpreadsheetCellData> restoreCells = null, SpreadsheetColumnData restoreColumn = null)
        {
            int colIndex = col.HasValue ? col.Value : columnDatas.Count;
            if (colIndex > 0 && colIndex < columnDatas.Count)
            {
                // check for dataTable. Cannot insert between dataTable, so only the first column of dataTable can be inserted
                for (int row = 0; row < rowDatas.Count; ++row)
                {
                    SpreadsheetCellData cell = cells[row][colIndex];
                    if (cell.dataTableData != null)
                    {
                        SpreadsheetRangeData rangeData = valueStore.CreateSpreadsheetRangeDataFromRangeId(cell.dataTableData.rangeId);
                        PointInt? position = rangeData.GetPositionOfCell(cell);
                        if (position.Value.X > 0)
                        {
                            throw new Exception("Cannot change part of a data table");
                        }
                    }
                }
            }

            if (restoreColumn == null)
            {
                string newColumnId = columnIdGenerator.NewVariableName();
                columnDatas.Insert(colIndex, new SpreadsheetColumnData(valueStore) { id = GetColumnId(newColumnId) });
            }
            else
            {
                columnDatas.Insert(colIndex, restoreColumn);
            }

            if (restoreCells == null)
            {
                List<SpreadsheetCellData> newColumn = new List<SpreadsheetCellData>();
                for (int row = 0; row < rowDatas.Count; ++row)
                {
                    string expression = ResolveRowExpressionForColumn(rowDatas[row].expression, colIndex);
                    SpreadsheetCellData cellData = new SpreadsheetCellData(this.valueStore, GetCellId(row, colIndex), expression);
                    cells[row].Insert(colIndex, cellData);
                    newColumn.Add(cellData);
                }
                return newColumn;
            }
            else
            {
                for (int row = 0; row < rowDatas.Count; ++row)
                {
                    cells[row].Insert(colIndex, restoreCells[row]);
                }
                return restoreCells;
            }
        }

        public List<SpreadsheetCellData> DeleteRow(int row)
        {
            List<SpreadsheetCellData> removedRow = cells[row];
            // check for data table
            foreach (SpreadsheetCellData cell in removedRow)
            {
                if (cell.dataTableData != null)
                {
                    throw new Exception("Cannot change part of a data table");
                }
            }
            cells.RemoveAt(row);
            rowDatas.RemoveAt(row);
            return removedRow;
        }

        public List<SpreadsheetCellData> DeleteColumn(int col)
        {
            List<SpreadsheetCellData> removedColumn = new List<SpreadsheetCellData>();
            for (int row = 0; row < rowDatas.Count; ++row)
            {
                SpreadsheetCellData cellData = cells[row][col];
                if (cellData.dataTableData != null)
                {
                    throw new Exception("Cannot change part of a data table");
                }
                removedColumn.Add(cellData);
                cells[row].RemoveAt(col);
            }
            columnDatas.RemoveAt(col);
            return removedColumn;
        }

        public void RenameColumn(int col, string label)
        {
            if (label != null && label.Length == 0) label = null;
            columnDatas[col].label = label;
        }

        public void RenameRow(int row, string label)
        {
            if (label != null && label.Length == 0) label = null;
            rowDatas[row].label = label;
        }

        public void SetColumnExpression(int col, string expression)
        {
            if (columnDatas[col].expression != expression)
            {
                columnDatas[col].expression = expression;
                valueStore.ShouldDispatchPropertyChanged = false;   // don't store changed cells to Undo stack
                for (int row = 0; row < rowDatas.Count; ++row)
                {
                    SpreadsheetCellData cellData = cells[row][col];
                    cellData.expression = ResolveColumnExpressionForRow(expression, row);
                }
                valueStore.ShouldDispatchPropertyChanged = true;
                valueStore.ResolveValue(columnDatas[col].id);
            }
        }

        public void SetRowExpression(int row, string expression)
        {
            if (rowDatas[row].expression != expression)
            {
                rowDatas[row].expression = expression;
                valueStore.ShouldDispatchPropertyChanged = false;   // don't store changed cells to Undo stack
                for (int col = 0; col < columnDatas.Count; ++col)
                {
                    SpreadsheetCellData cellData = cells[row][col];
                    cellData.expression = ResolveRowExpressionForColumn(expression, col);
                }
                valueStore.ShouldDispatchPropertyChanged = true;
                valueStore.ResolveValue(rowDatas[row].id);
            }
        }

        public string ResolveColumnExpressionForRow(string expression, int row)
        {
            string rowId = rowDatas[row].partId;
            return Regex.Replace(expression, @"\["+this.id+@"_c(\d+)\]", "["+this.id+"_"+rowId+"_$1]");
        }

        public string ResolveRowExpressionForColumn(string expression, int col)
        {
            string colId = columnDatas[col].partId;
            return Regex.Replace(expression, @"\[" + this.id + @"_r(\d+)\]", "[" + this.id + "_$1_" + colId + "]");
        }

        public bool HasCustomLabeledColumnHeader()
        {
            foreach (SpreadsheetColumnData columnData in columnDatas)
            {
                if (columnData.HasCustomLabel()) return true;
            }
            return false;
        }

        public bool HasCustomLabeledRowHeader()
        {
            foreach (SpreadsheetRowData rowData in rowDatas)
            {
                if (rowData.HasCustomLabel()) return true;
            }
            return false;
        }
    }

    [DataContract]
    [Serializable]
    public class SpreadsheetColumnData : AbstractComponentData, IExpressionData
    {
        private String _label;
        [DataMember]
        public String label
        {
            get
            {
                return _label;
            }
            set
            {
                if (_label != value)
                {
                    object oldValue = _label;
                    _label = value;
                    NotifyPropertyChanged("label", oldValue, value);
                }
            }
        }
        private string _expression;
        [DataMember]
        public string expression
        {
            get
            {
                return _expression;
            }
            set
            {
                if (_expression != value)
                {
                    object oldValue = _expression;
                    _expression = value;
                    NotifyPropertyChanged("expression", oldValue, value);
                }
            }
        }

        // e.g. id = v1_c2 then partId=2
        public string partId
        {
            get
            {
                string[] parts = this.id.Split('_');
                return parts[1].Substring(1);
            }
        }

        public void SetExpressionRaw(string expression)
        {
            this.expression = expression;
        }

        public SpreadsheetColumnData(IComponentValueStore valueStore)
            : base(valueStore)
        {
            expression = "";
        }

        public string GetColumnId()
        {
            return this.partId;
        }

        public bool HasExpression()
        {
            return _expression != null && _expression.Length > 0;
        }

        public bool HasCustomLabel()
        {
            return _label != null && _label.Length > 0;
        }

        public void ReplaceExpressionVariableId(string oldId, string newId)
        {
            expression = DataHelper.ReplaceExpressionVariableId(expression, oldId, newId);
        }
    }

    [DataContract]
    [Serializable]
    public class SpreadsheetRowData : AbstractComponentData, IExpressionData
    {
        private String _label;
        [DataMember]
        public String label
        {
            get
            {
                return _label;
            }
            set
            {
                if (_label != value)
                {
                    object oldValue = _label;
                    _label = value;
                    NotifyPropertyChanged("label", oldValue, value);
                }
            }
        }
        private string _expression;
        [DataMember]
        public string expression
        {
            get
            {
                return _expression;
            }
            set
            {
                if (_expression != value)
                {
                    object oldValue = _expression;
                    _expression = value;
                    NotifyPropertyChanged("expression", oldValue, value);
                }
            }
        }

        // e.g. id = v1_r2 then partId=2
        public string partId
        {
            get
            {
                string[] parts = this.id.Split('_');
                return parts[1].Substring(1);
            }
        }

        public void SetExpressionRaw(string expression)
        {
            this.expression = expression;
        }

        public SpreadsheetRowData(IComponentValueStore valueStore)
            : base(valueStore)
        {
            expression = "";
        }

        public string GetRowId()
        {
            return this.partId;
        }

        public bool HasExpression()
        {
            return _expression != null && _expression.Length > 0;
        }
        public bool HasCustomLabel()
        {
            return _label != null && _label.Length > 0;
        }

        public void ReplaceExpressionVariableId(string oldId, string newId)
        {
            expression = DataHelper.ReplaceExpressionVariableId(expression, oldId, newId);
        }
    }

    [DataContract]
    [Serializable]
    public class DataTableData
    {
        [DataMember]
        public string rangeId;
        [DataMember]
        public string rowInputId;
        [DataMember]
        public string colInputId;
        [DataMember]
        public bool IsHeader;

        public DataTableData(string rangeId, string rowInputId, string colInputId, bool IsHeader)
        {
            this.rangeId = rangeId;
            this.rowInputId = rowInputId;
            this.colInputId = colInputId;
            this.IsHeader = IsHeader;
        }
    }

    [DataContract]
    [Serializable]
    public class SpreadsheetCellData : AbstractExpressionData
    {
        [DataMember]
        public override string expression
        {
            get { return _expression; }
            set
            {
                if (_expression != value)
                {
                    object oldValue = _expression;
                    _expression = value;
                    NotifyPropertyChanged("expression", oldValue, value);
                }
                if (_expression != null && _expression.Length > 0)
                {
                    if (_dataTableData != null && !_dataTableData.IsHeader)
                    {
                        // dataTable content cannot have expression. Only dataTable header can have expression
                        _dataTableData = null;
                    }
                }
                if (valueStore != null)
                {
                    valueStore.ResolveValue(this.id);
                }
            }
        }

        public override string autoLabel
        {
            get
            {
                return valueStore.GetComponentLabelOrValueAsString(this.id);
            }
            set { }
        }

        public string typeLabel
        {
            get { return "Spreadsheet Cell"; }
        }

        private DataTableData _dataTableData;
        [DataMember]
        public DataTableData dataTableData
        {
            get { return _dataTableData; }
            set {
                _dataTableData = value;
                if (_dataTableData != null && !_dataTableData.IsHeader)
                {
                    // dataTable content cannot have expression. Only dataTable header can have expression
                    _expression = "";
                }
                if (valueStore != null)
                {
                    valueStore.ResolveValue(this.id);
                }
            }
        }

        #region component styles

        [NonSerialized]
        private Color _backgroundColor;
        [DataMember]
        [Category("Style")]
        [DisplayName("Background Color")]
        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                if (_backgroundColor != value)
                {
                    object oldValue = _backgroundColor;
                    _backgroundColor = value;
                    NotifyPropertyChanged("BackgroundColor", oldValue, value);
                }
            }
        }
        private String BackgroundColor_Surrogate;

        [NonSerialized]
        private Color _fontColor;
        [DataMember]
        [Category("Style")]
        [DisplayName("Font Color")]
        public Color FontColor
        {
            get { return _fontColor; }
            set
            {
                if (_fontColor != value)
                {
                    object oldValue = _fontColor;
                    _fontColor = value;
                    NotifyPropertyChanged("FontColor", oldValue, value);
                }
            }
        }
        private String FontColor_Surrogate;

        #endregion

        public SpreadsheetCellData(IComponentValueStore valueStore, string id, string expression = "")
            : base(valueStore, id)
        {
            _expression = expression;
        }

        protected override void InitDefaultStyle()
        {
            base.InitDefaultStyle();
            this.BackgroundColor = GetDefaultColor("Background");
            this.FontColor = GetDefaultColor("Font");
        }

        [OnDeserializing()]
        public void OnDeserializing(StreamingContext context)
        {
            InitDefaultStyle();
        }

        [OnSerializing()]
        public void OnSerializing(StreamingContext context)
        {
            BackgroundColor_Surrogate = BackgroundColor.ToString();
            FontColor_Surrogate = FontColor.ToString();
        }

        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            if (BackgroundColor_Surrogate != null)
            {
                BackgroundColor = (Color)ColorConverter.ConvertFromString(BackgroundColor_Surrogate);
            }
            if (FontColor_Surrogate != null)
            {
                FontColor = (Color)ColorConverter.ConvertFromString(FontColor_Surrogate);
            }
        }

        public string GetColumnId()
        {
            return id.Split('_')[2];
        }

        public string GetRowId()
        {
            return id.Split('_')[1];
        }
    }

    public class SpreadsheetRangeData : AbstractComponentData
    {
        public SpreadsheetComponentData spreadsheet { get; private set; }
        public string startId, endId;

        public override string id
        {
            get
            {
                return startId + ":" + endId;
            }
            set
            {                
            }
        }

        public override string autoLabel
        {
            get
            {
                PointInt? startPosition = this.spreadsheet.GetPositionFromCellId(this.startId);
                PointInt? endPosition = this.spreadsheet.GetPositionFromCellId(this.endId);
                string startLabel = SpreadsheetComponentData.GetDefaultCellLabel(startPosition);
                string endLabel = SpreadsheetComponentData.GetDefaultCellLabel(endPosition);
                string label = startLabel + ":" + endLabel;
                return label;
            }
            set
            {
            }
        }

        public SpreadsheetRangeData(IComponentValueStore valueStore, SpreadsheetComponentData spreadsheet, string startId, string endId)
            : base(valueStore)
        {
            this.spreadsheet = spreadsheet;
            this.startId = startId;
            this.endId = endId;
        }

        public PointInt GetDimension()
        {
            PointInt? startPosition = spreadsheet.GetPositionFromCellId(startId);
            PointInt? endPosition = spreadsheet.GetPositionFromCellId(endId);
            if (startPosition.HasValue && endPosition.HasValue)
            {
                return new PointInt(endPosition.Value.X - startPosition.Value.X + 1, endPosition.Value.Y - startPosition.Value.Y + 1);
            }
            else
            {
                return new PointInt(0, 0);
            }
        }

        // return position of cell if it is part of the range, otherwise return null
        public PointInt? GetPositionOfCell(SpreadsheetCellData cell)
        {
            if (cell.parentId == this.spreadsheet.id)
            {
                PointInt? startPosition = spreadsheet.GetPositionFromCellId(startId);
                PointInt? endPosition = spreadsheet.GetPositionFromCellId(endId);
                PointInt? cellPosition = spreadsheet.GetPositionFromCellId(cell.id);
                if (startPosition.HasValue && endPosition.HasValue && cellPosition.HasValue)
                {
                    if (cellPosition.Value.X >= startPosition.Value.X && cellPosition.Value.Y >= startPosition.Value.Y &&
                        cellPosition.Value.X <= endPosition.Value.X && cellPosition.Value.Y <= endPosition.Value.Y)
                    {
                        return new PointInt(cellPosition.Value.X - startPosition.Value.X, cellPosition.Value.Y - startPosition.Value.Y);
                    }
                }
            }
            return null;
        }

        public List<List<SpreadsheetCellData>> GetCellsTable()
        {
            PointInt? startPosition = spreadsheet.GetPositionFromCellId(startId);
            PointInt? endPosition = spreadsheet.GetPositionFromCellId(endId);
            List<List<SpreadsheetCellData>> cells = new List<List<SpreadsheetCellData>>();
            if (startPosition.HasValue && endPosition.HasValue)
            {
                for (int row = startPosition.Value.Y; row <= endPosition.Value.Y; ++row)
                {
                    List<SpreadsheetCellData> cellRow = new List<SpreadsheetCellData>();
                    for (int col = startPosition.Value.X; col <= endPosition.Value.X; ++col)
                    {
                        cellRow.Add(spreadsheet.cells[row][col]);
                    }
                    cells.Add(cellRow);
                }
            }
            return cells;
        }

        public SpreadsheetCellData GetCellAtPosition(PointInt position)
        {
            PointInt dimension = GetDimension();
            if (position.X >= 0 && position.Y >= 0 && position.X < dimension.X && position.Y < dimension.Y)
            {
                PointInt? startPosition = spreadsheet.GetPositionFromCellId(startId);
                if (startPosition.HasValue)
                {
                    return spreadsheet.cells[startPosition.Value.Y + position.Y][startPosition.Value.X + position.X];
                }
            }
            return null;
        }

        /*public List<SpreadsheetCellData> EnumerateCellsByRowFirst()
        {
            PointInt startPosition = spreadsheet.GetPositionFromCellId(startId);
            PointInt endPosition = spreadsheet.GetPositionFromCellId(endId);
            List<SpreadsheetCellData> cells = new List<SpreadsheetCellData>();
            for (int row = startPosition.Y; row <= endPosition.Y; ++row)
            {
                for (int col = startPosition.X; col <= endPosition.X; ++col)
                {
                    cells.Add(spreadsheet.cells[row][col]);
                }
            }
            return cells;
        }*/
    }
}

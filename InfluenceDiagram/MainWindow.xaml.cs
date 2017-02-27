using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using InfluenceDiagram.ComponentControl;
using InfluenceDiagram.Data;
using DiagramDesigner;
using InfluenceDiagram.Utility;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;
using System.Collections;

namespace InfluenceDiagram
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window

    {
        //selected command tool
        public static ICommand command;
  

        private string openFilePath;
        private WorksheetData worksheetData;
        private Dictionary<string, object> mapComponentControl;
        // component/sub-component that's currently editable
        private object prevActiveComponent;
        private object _currentActiveComponent;
        private object currentActiveComponent
        {
            get { return _currentActiveComponent; }
            set
            {
                prevActiveComponent = _currentActiveComponent;
                _currentActiveComponent = value;
            }
        }
        Point? pastePosition;
        private WindowGoalseek windowGoalseek;
        private WindowDatatable windowDatatable;
        private UndoManager undoManager;
        
        public bool IsPanelsEnabled
        {
            set
            {
                topPanel.IsEnabled = leftPanel.IsEnabled = rightPanel.IsEnabled = value;
            }
        }

        public MainWindow(string openFilePath = null)
        {
            this.openFilePath = openFilePath;
            undoManager = new UndoManager();
            InitializeComponent();
            worksheetData = new WorksheetData();
            SetupWorksheetData();
            mapComponentControl = new Dictionary<string, object>();
            outlineTreeRoot.ItemsSource = designerCanvas.listDesignerItem;
            pastePosition = null;
        }

        void SetupWorksheetData()
        {
            worksheetData.DataChanged += worksheetData_DataChanged;
            worksheetData.PropertyChanged += worksheetData_PropertyChanged;
            worksheetData.DependenceAdded += worksheetData_DependenceAdded;
            worksheetData.DependenceRemoved += worksheetData_DependenceRemoved;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        void UpdateTitle()
        {
            if (worksheetData != null && worksheetData.currentFilePath != null)
            {
                this.Title = "Influence Diagram - " + worksheetData.currentFilePath;
            }
            else
            {
                this.Title = "Influence Diagram";
            }
        }

        #region ScrollViewer

        Point capturePoint { get; set; }

        private void ScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source == designerCanvas)
            {
                // if Ctrl is pressed, don't scroll
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) return;
                else
                {
                    // when scrolling, need to clear the canvas selection first
                    designerCanvas.ClearSelection();
                    Command.ClickComponent.Execute(scrollViewer, Application.Current.MainWindow);
                    capturePoint = e.MouseDevice.GetPosition(scrollViewer);
                    scrollViewer.CaptureMouse();

                    pastePosition = e.MouseDevice.GetPosition(scrollViewer);
                }
            }
        }

        private void ScrollViewer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            pastePosition = e.MouseDevice.GetPosition(scrollViewer);
        }

        private void ScrollViewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            scrollViewer.ReleaseMouseCapture();
        }

        private void ScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (scrollViewer.IsMouseCaptured)
            {
                Point currentPoint = e.MouseDevice.GetPosition(scrollViewer);
                Vector delta = Point.Subtract(currentPoint, capturePoint);
                Point newPoint = Point.Subtract(new Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset), delta);
                scrollViewer.ScrollToHorizontalOffset(newPoint.X);
                scrollViewer.ScrollToVerticalOffset(newPoint.Y);
                capturePoint = currentPoint;
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                double d = e.Delta/120.0;
                DoChangeZoom(d);
                e.Handled = true;
            }
        }

        // v should be in +-1 increment (will be converted to +-0.1)
        // + for zoom in, - for zoom out
        void DoChangeZoom(double v)
        {
            double scale = canvasScaleTransform.ScaleX;
            scale += 0.1 * v;
            SetZoom(scale);
        }

        void SetZoom(double scale)
        {
            scale = Math.Min(5.0, Math.Max(0.1, scale));    // scale range 0.1 to 5.0
            canvasScaleTransform.ScaleX = canvasScaleTransform.ScaleY = scale;
        }

        void IncreaseZoom_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DoChangeZoom(1);
        }

        void DecreaseZoom_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DoChangeZoom(-1);
        }

        void ResetZoom_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetZoom(1.0);
        }

        private void scrollViewer_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = scrollViewer;
            e.Handled = true;
        }

        private void scrollViewer_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            double scale = canvasScaleTransform.ScaleX;
            scale *= (e.DeltaManipulation.Scale.X);
            SetZoom(scale);
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.DeltaManipulation.Translation.X);
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.DeltaManipulation.Translation.Y);
            e.Handled = true;
        }

        #endregion

        #region general commands

        void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (designerCanvas.SelectionService.CurrentSelection.Count > 0)
            {
                List<object> components = new List<object>();   // should contain RootComponentData or ControlConnection
                foreach (ISelectable el in designerCanvas.SelectionService.CurrentSelection)
                {
                    if (el is DesignerItem)
                    {
                        DesignerItem item = el as DesignerItem;
                        if (item.Content is IComponentControl)
                        {
                            IComponentControl control = item.Content as IComponentControl;
                            components.Add(control.rootData);
                        }              
                    }
                    else if (el is ControlConnection)
                    {
                        ControlConnection connection = el as ControlConnection;
                        ShapeComponentControl source = UIHelper.FindVisualParent<ShapeComponentControl>(connection.Source);
                        ShapeComponentControl target = UIHelper.FindVisualParent<ShapeComponentControl>(connection.Sink);
                        ShapeConnectionData connectionData = new ShapeConnectionData
                        {
                            sourceId = source.data.id,
                            targetId = target.data.id
                        };
                        components.Add(connectionData);
                    }
                }
                undoManager.PushCommand(new UndoableCommand
                {
                    type = UndoableCommandType.DeleteComponent,
                    appliedObject = components
                });
                DoDeleteComponents(components);
                designerCanvas.DeleteCurrentSelection();
            }
        }

        bool HasSelectedControl()
        {
            if (designerCanvas != null)
            {
                return designerCanvas.SelectionService.CurrentSelection.Count > 0 && this.currentActiveComponent == null;
            }
            else
            {
                return false;
            }
        }

        void Cut_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasSelectedControl();
        }

        void Copy_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasSelectedControl();
        }

        void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasSelectedControl();
        }
        
        private static bool IsSerializable(object obj)
        {
            System.IO.MemoryStream mem = new System.IO.MemoryStream();
            BinaryFormatter bin = new BinaryFormatter();
            try
            {
                bin.Serialize(mem, obj);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Your object cannot be serialized." +
                                 " The reason is: " + ex.ToString());
                return false;
            }
        }

        void Cut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Copy_Executed(sender, e);
            Delete_Executed(sender, e);
        }

        void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (designerCanvas.SelectionService.CurrentSelection.Count > 0)
            {
                // TODO: enable copy multiple objects
                //foreach (ISelectable el in designerCanvas.SelectionService.CurrentSelection)
                //{
                ISelectable el = designerCanvas.SelectionService.CurrentSelection[0];
                    if (el is DesignerItem)
                    {
                        IComponentControl content = (el as DesignerItem).Content as IComponentControl;
                        RootComponentData data = content.rootData;
                        
                        // TODO: just for testing. comment when release
                        //IsSerializable(data);

                        DataFormat dataFormat = DataFormats.GetDataFormat(typeof(RootComponentData).FullName);
                        IDataObject dataObj = new DataObject();
                        dataObj.SetData(dataFormat.Name, data, false);
                        Clipboard.SetDataObject(dataObj, false);
                    }
                //}
            }
        }

        private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DataFormat dataFormat = DataFormats.GetDataFormat(typeof(RootComponentData).FullName);
            IDataObject dataObj = Clipboard.GetDataObject();
            e.CanExecute = dataObj.GetDataPresent(dataFormat.Name);
        }

        void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataFormat dataFormat = DataFormats.GetDataFormat(typeof(RootComponentData).FullName);
            IDataObject dataObj = Clipboard.GetDataObject();
            if (dataObj.GetDataPresent(dataFormat.Name))
            {
                RootComponentData componentData = dataObj.GetData(dataFormat.Name) as RootComponentData;
                worksheetData.PasteRootComponent(componentData, pastePosition);
                CreateComponentControl(componentData, true);
                // fix broken links & dependency arrow
                worksheetData.ResolveBrokenComponentValues();
            }
        }

        #region Undo/Redo

        private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = undoManager.CanUndo();
        }

        private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = undoManager.CanRedo();
        }

        void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            UndoableCommand command = undoManager.Undo();
            if (command != null)
            {
                command = UndoableCommand.GetReversedForUndo(command);
                ApplyUndoableCommand(command);
            }
        }

        void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            UndoableCommand command = undoManager.Redo();
            if (command != null)
            {
                ApplyUndoableCommand(command);
            }
        }

        void ApplyUndoableCommand(UndoableCommand command)
        {
            if (command.type == UndoableCommandType.InsertComponent)
            {
                DoInsertComponents(command.appliedObject);
                worksheetData.ResolveBrokenComponentValues();
            }
            else if (command.type == UndoableCommandType.DeleteComponent)
            {
                DoDeleteComponents(command.appliedObject);
            }
            else if (command.type == UndoableCommandType.ChangeComponentProperty)
            {
                UndoableCommand_Property commandP = command as UndoableCommand_Property;
                DoChangeComponentProperties(commandP.appliedObject as string, commandP.oldValue as Dictionary<string,object>, commandP.newValue as Dictionary<string,object>);
            }
            else if (command.type == UndoableCommandType.RoutedCommand)
            {
                UndoableCommand_Routed commandR = command as UndoableCommand_Routed;
                DoApplyRoutedCommand(commandR.appliedObject, commandR.command, commandR.parameter, commandR.data);
            }
        }

        // source is usually the component id (string)
        private void DoApplyRoutedCommand(object appliedObject, ICommand command, object parameter, object data)
        {
            if (command == Command.SpreadsheetAddRow)
            {
                List<SpreadsheetCellData> restoreCells = null;
                SpreadsheetRowData restoreRow = null;
                if (data is List<object>)
                {
                    List<object> list = data as List<object>;
                    restoreCells = list[0] as List<SpreadsheetCellData>;
                    restoreRow = list[1] as SpreadsheetRowData;
                }
                AddSpreadsheetRow(appliedObject as string, parameter as int?, restoreCells, restoreRow);
                worksheetData.ResolveBrokenComponentValues();
            }
            else if (command == Command.SpreadsheetAddColumn)
            {
                List<SpreadsheetCellData> restoreCells = null;
                SpreadsheetColumnData restoreColumn = null;
                if (data is List<object>)
                {
                    List<object> list = data as List<object>;
                    restoreCells = list[0] as List<SpreadsheetCellData>;
                    restoreColumn = list[1] as SpreadsheetColumnData;
                }
                AddSpreadsheetColumn(appliedObject as string, parameter as int?, restoreCells, restoreColumn);
                worksheetData.ResolveBrokenComponentValues();
            }
            else if (command == Command.SpreadsheetDeleteRow)
            {
                List<SpreadsheetCellData> deletedCells;
                SpreadsheetRowData deletedRow;
                DeleteSpreadsheetRow(appliedObject as string, parameter as int?, out deletedCells, out deletedRow);
            }
            else if (command == Command.SpreadsheetDeleteColumn)
            {
                List<SpreadsheetCellData> deletedCells;
                SpreadsheetColumnData deletedColumn;
                DeleteSpreadsheetColumn(appliedObject as string, parameter as int?, out deletedCells, out deletedColumn);
            }
            else if (command == Command.MacroAddParam)
            {
                AddMacroParameter(appliedObject as string);
            }
            else if (command == Command.MacroDeleteParam)
            {
                DeleteMacroParameter(appliedObject as string);
            }
            else if (command == Command.LoopAddParam)
            {
                AddLoopParameter(appliedObject as string);
            }
            else if (command == Command.LoopDeleteParam)
            {
                DeleteLoopParameter(appliedObject as string);
            }
            else if (command == Command.CreateDatatable)
            {
                worksheetData.SetDatatable(appliedObject as DataTableData);
            }
            else if (command == Command.DeleteDatatable)
            {
                worksheetData.DeleteDataTable(appliedObject as DataTableData);
            }
        }

        private void DoChangeComponentProperties(string componentId, Dictionary<string,object> oldProperties, Dictionary<string,object> newProperties)
        {
            worksheetData.ForceChangeComponentProperties(componentId, oldProperties, newProperties);
        }

        // Undoable command
        // components can be a RootComponentData (in case of redoing an insert command) or List<object> (in case of undoing a delete command)
        // object can be a RootComponentData or a ShapeConnectionData
        void DoInsertComponents(object components)
        {
            if (!(components is List<object>))
            {
                // if it's 1 object, wrap in a list
                List<object> list = new List<object>();
                list.Add(components);
                components = list;
            }
            foreach (object obj in (components as List<object>))
            {
                if (obj is RootComponentData)
                {
                    RootComponentData data = obj as RootComponentData;
                    Point? position = new Point(data.PositionX, data.PositionY);
                    worksheetData.PasteRootComponent(data, position);
                    CreateComponentControl(data, false);
                    // fix broken links & dependency arrow
                    worksheetData.ResolveBrokenComponentValues();
                }
                else if (obj is ShapeConnectionData)
                {
                    ShapeConnectionData connectionData = obj as ShapeConnectionData;
                    ShapeComponentControl source = mapComponentControl[connectionData.sourceId] as ShapeComponentControl;
                    ShapeComponentControl target = mapComponentControl[connectionData.targetId] as ShapeComponentControl;
                    source.data.AddConnection(target.data.id);
                }
            }
        }

        // Undoable command
        // components can be a RootComponentData (in case of undoing an insert command) or List<object> (in case of redoing a delete command)
        // object can be a RootComponentData or a ShapeConnectionData
        void DoDeleteComponents(object components)
        {
            if (!(components is List<object>))
            {
                // if it's 1 object, wrap in a list
                List<object> list = new List<object>();
                list.Add(components);
                components = list;
            }
            foreach (object obj in (components as List<object>))
            {
                if (obj is RootComponentData)
                {
                    RootComponentData data = obj as RootComponentData;
                    worksheetData.RemoveComponent(data);
                    IComponentControl control = mapComponentControl[data.id] as IComponentControl;
                    mapComponentControl.Remove(data.id);
                    designerCanvas.DeleteChild(UIHelper.FindVisualParent<DesignerItem>(control as UIElement));
                }
                else if (obj is ShapeConnectionData)
                {
                    ShapeConnectionData connectionData = obj as ShapeConnectionData;
                    ShapeComponentControl source = mapComponentControl[connectionData.sourceId] as ShapeComponentControl;
                    ShapeComponentControl target = mapComponentControl[connectionData.targetId] as ShapeComponentControl;
                    ControlConnection connection = GetControlConnection(connectionData.sourceId, connectionData.targetId);
                    source.data.RemoveConnection(target.data.id);
                    designerCanvas.DeleteChild(connection);
                }
            }
        }

        #endregion

        void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // dispatch a new blank window
            Process p = new Process();
            p.StartInfo.FileName =
                System.Reflection.Assembly.GetExecutingAssembly().Location;
            p.Start();
        }

        void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DoSave();
        }

        bool DoSave()
        {
            if (worksheetData.currentFilePath != null)
            {
                WorksheetSerializer serializer = new WorksheetSerializer();
                serializer.SaveWorksheet(worksheetData, worksheetData.currentFilePath);
                undoManager.SetCurrentAsSaveState();
                return true;
            }
            else
            {
                return DoSaveAs();
            }
        }

        void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DoSaveAs();
        }

        bool DoSaveAs()
        {
            System.Windows.Forms.SaveFileDialog saveDialog = new System.Windows.Forms.SaveFileDialog();
            saveDialog.InitialDirectory = Convert.ToString(Environment.SpecialFolder.MyDocuments);
            saveDialog.Filter = "REvan Influence Diagram file|*.rvn|Image File|*.png";

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string extension = System.IO.Path.GetExtension(saveDialog.FileName);
                if (extension.ToLower() == ".rvn")
                {
                    WorksheetSerializer serializer = new WorksheetSerializer();
                    serializer.SaveWorksheet(worksheetData, saveDialog.FileName);
                    undoManager.SetCurrentAsSaveState();
                    UpdateTitle();
                }
                else if (extension.ToLower() == ".png")
                {
                    UIElement element = scrollViewer.Content as UIElement;
                    Size size = element.RenderSize;
                    var target = new RenderTargetBitmap((int)(size.Width), (int)(size.Height), 96, 96, PixelFormats.Pbgra32);
                    element.Measure(size);
                    element.Arrange(new Rect(size));
                    target.Render(element);

                    var encoder = new PngBitmapEncoder();
                    var outputFrame = BitmapFrame.Create(target);
                    encoder.Frames.Add(outputFrame);

                    using (var file = File.OpenWrite(saveDialog.FileName))
                    {
                        encoder.Save(file);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openDialog = new System.Windows.Forms.OpenFileDialog();
            openDialog.InitialDirectory = Convert.ToString(Environment.SpecialFolder.MyDocuments);
            openDialog.Filter = "REvan Influence Diagram file|*.rvn";

            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (CheckSaveAndNeedCancel()) return;

                DoOpen(openDialog.FileName);
            }
        }

        void DoOpen(string filePath)
        {
            this.IsPanelsEnabled = false;
            WindowBusy windowBusy = new WindowBusy();
            windowBusy.Show();
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate(object s, DoWorkEventArgs a)
            {
                WorksheetSerializer serializer = new WorksheetSerializer();
                try
                {
                    worksheetData = serializer.LoadWorksheet(filePath);
                    SetupWorksheetData();
                }
                catch (Exception exc)
                {
                    // wait until components are rendered
                    Dispatcher.BeginInvoke(DispatcherPriority.Render,
                     new Action(delegate()
                     {
                         MessageBox.Show(this, exc.Message);
                     }));
                }
            };
            worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs a)
            {
                ClearCanvas();
                foreach (RootComponentData data in worksheetData.listRootComponentData)
                {
                    CreateComponentControl(data, false);
                }
                UpdateTitle();
                // wait until components are rendered
                Dispatcher.BeginInvoke(DispatcherPriority.Render,
                 new Action(delegate()
                 {
                     worksheetData.TriggerAllEdges();
                     windowBusy.Close();
                     this.IsPanelsEnabled = true;
                 }));
            };
            worker.RunWorkerAsync();
        }

        void Help_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, @"Resources\REvan Influence Diagram User Manual.pdf");
            System.Diagnostics.Process.Start(path);
        }

        void IncludeExternal_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // because the included worksheet will be saved as relative paths, user needs to save this worksheet first
            if (worksheetData.currentFilePath == null)
            {
                MessageBox.Show("To include external macros, please save first!");
            }
            else
            {
                IncludeExternal includeWindow = new IncludeExternal(worksheetData);
                includeWindow.ShowDialog();
            }
        }

        void FunctionList_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FunctionList functionWindow = new FunctionList(worksheetData);
            if (functionWindow.ShowDialog() == true)
            {
                string selectedFunction = functionWindow.selectedFunction;
                if (selectedFunction != null)
                {
                    if (Keyboard.FocusedElement is ExpressionTextBox)
                    {
                        (Keyboard.FocusedElement as ExpressionTextBox).InsertFunction(selectedFunction);
                    }
                }
            }
        }

        bool popupDatatoolsWasOpen = false;
        private void buttonDatatools_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            popupDatatoolsWasOpen = popupDatatools.IsOpen;
        }
        void OpenDatatools_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            popupDatatools.IsOpen = !popupDatatoolsWasOpen;
        }

        void OpenGoalseek_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            popupDatatools.IsOpen = false;
            if (windowGoalseek == null)
            {
                windowGoalseek = new WindowGoalseek(worksheetData);
                windowGoalseek.Owner = this;
                windowGoalseek.Closed += windowGoalseek_Closed;
                windowGoalseek.Show();
            }
            else
            {
                windowGoalseek.Focus();
            }
        }

        void windowGoalseek_Closed(object sender, EventArgs e)
        {
            windowGoalseek = null;
            this.IsPanelsEnabled = true;
            this.scrollViewer.IsEnabled = true;
            this.Activate();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (windowGoalseek != null && !scrollViewer.IsEnabled)
            {
                // if canvas is also disabled, don't accept focus
                windowGoalseek.Activate();
            }
        }

        private void Window_Loaded(object sender, EventArgs e)
        {
            if (openFilePath != null)
            {
                DoOpen(openFilePath);
            }
            else
            {
                // don't show tips when opening file with double click
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    CheckShowTips();
                };
                timer.Start();
            }
        }

        void CheckShowTips()
        {
            if ((bool)Properties.Settings.Default["ShowTipsOnStart"] == true)
            {
                WindowTips windowTips = new WindowTips();
                windowTips.ShowDialog();
            }
        }

        // return true if the current action (closing window or opening a new document) needs to be cancelled
        bool CheckSaveAndNeedCancel()
        {
            if (undoManager.CheckNeedSave())
            {
                MessageBoxResult result = MessageBox.Show("Your current progress will be lost. Save changes before closing?", "REvan", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    bool saved = DoSave();
                    if (!saved)
                    {
                        // cancel Close if not saved yet
                        return true;
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return true;
                }
                // Do nothing if "No" is selected
            }
            return false;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (CheckSaveAndNeedCancel())
            {
                e.Cancel = true;
            }
        }

        /**
         * If there is a selected spreadsheet range, return it. Otherwise return null
         **/
        SpreadsheetRangeData CheckSelectedSpreadsheetRange()
        {
            if (designerCanvas.SelectionService.CurrentSelection.Count == 1)
            {
                ISelectable el = designerCanvas.SelectionService.CurrentSelection[0];
                if (el is DesignerItem)
                {
                    object content = (el as DesignerItem).Content;
                    if (content is SpreadsheetComponentControl)
                    {
                        SpreadsheetComponentControl spreadsheet = content as SpreadsheetComponentControl;
                        AbstractComponentData data = spreadsheet.GetData();
                        if (data != null && data is SpreadsheetRangeData)
                        {
                            SpreadsheetRangeData rangeData = data as SpreadsheetRangeData;
                            return rangeData;
                        }
                    }
                }
            }
            return null;
        }

        void OpenDatatable_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            popupDatatools.IsOpen = false;
            // to use data table, user needs to select minimum 2 rows & 2 columns on a spreadsheet
            bool shouldOpen = false;
            SpreadsheetRangeData rangeData = CheckSelectedSpreadsheetRange();
            if (rangeData != null)
            {
                PointInt dimension = rangeData.GetDimension();
                if (dimension.X >= 2 && dimension.Y >= 2)
                {
                    shouldOpen = true;
                }
            }

            if (shouldOpen){
                if (windowDatatable == null)
                {
                    windowDatatable = new WindowDatatable(worksheetData, rangeData);
                    windowDatatable.Owner = this;
                    windowDatatable.Closed += windowDatatable_Closed;
                    windowDatatable.Show();
                }
                else
                {
                    windowDatatable.Focus();
                }
            }
            else
            {
                MessageBox.Show("To use Data Table, please select a spreadsheet range with minimum 2 rows and 2 columns");
            }
        }

        void DeleteDatatable_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataTableData data = e.Parameter as DataTableData;
            worksheetData.DeleteDataTable(data);
            undoManager.PushCommand(new UndoableCommand_Routed
            {
                type = UndoableCommandType.RoutedCommand,
                command = e.Command,
                appliedObject = data
            });
        }

        void windowDatatable_Closed(object sender, EventArgs e)
        {
            windowDatatable = null;
            this.IsPanelsEnabled = true;
            this.scrollViewer.IsEnabled = true;
            this.Activate();
        }

        public void CreateDatatable(SpreadsheetRangeData rangeData, AbstractComponentData rowData, AbstractComponentData columnData)
        {
            DataTableData dataTableData = worksheetData.SetDatatable(rangeData, rowData as AbstractComponentData, columnData as AbstractComponentData);
            undoManager.PushCommand(new UndoableCommand_Routed
            {
                type = UndoableCommandType.RoutedCommand,
                command = Command.CreateDatatable,
                appliedObject = dataTableData
            });
        }

        void ClearCanvas()
        {
            designerCanvas.DeleteAll();
            mapComponentControl.Clear();
        }

        void CreateComponentControl(RootComponentData data, bool shouldPushUndoCommand)
        {
            IComponentControl control = null;
            if (data is ExpressionComponentData) 
            {
                control = new ExpressionComponentControl(data as ExpressionComponentData, worksheetData);
            }
            else if (data is SpreadsheetComponentData)
            {
                control = new SpreadsheetComponentControl(data as SpreadsheetComponentData, worksheetData);
            }
            else if (data is MacroComponentData)
            {
                control = new MacroComponentControl(data as MacroComponentData, worksheetData);
            }
            else if (data is ShapeComponentData)
            {
                control = new ShapeComponentControl(data as ShapeComponentData, worksheetData);
            }
            else if (data is IfComponentData)
            {
                control = new IfComponentControl(data as IfComponentData, worksheetData);
            }
            else if (data is LoopComponentData)
            {
                control = new LoopComponentControl(data as LoopComponentData, worksheetData);
            }
            designerCanvas.AddItem(control);
            mapComponentControl.Add(data.id, control);

            DesignerItem designerItem = (control as FrameworkElement).Parent as DesignerItem;
            designerItem.ComponentDragCompleted += designerItem_ComponentDragCompleted;

            if (shouldPushUndoCommand)
            {
                undoManager.PushCommand(new UndoableCommand
                {
                    type = UndoableCommandType.InsertComponent,
                    appliedObject = data
                });
            }
        }

        void designerItem_ComponentDragCompleted(object sender, ComponentDragCompletedEventArgs e)
        {
            IComponentControl control = e.component as IComponentControl;
            undoManager.PushCommand(new UndoableCommand_Property
            {
                type = UndoableCommandType.ChangeComponentProperty,
                appliedObject = control.rootData.id,
                oldValue = new Dictionary<string, object> { { "Position", e.startPosition } },
                newValue = new Dictionary<string, object> { { "Position", e.endPosition } },
            });
        }
                
        private void DropExpression_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Point? position = e.Parameter as Point?;
            ExpressionComponentData data = worksheetData.AddExpressionComponent();
            data.PositionX = position.Value.X;
            data.PositionY = position.Value.Y;
            CreateComponentControl(data, true);
        }

        private void DropSpreadsheet_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Point? position = e.Parameter as Point?;
            SpreadsheetComponentData data = worksheetData.AddSpreadsheetComponent();
            data.PositionX = position.Value.X;
            data.PositionY = position.Value.Y;
            CreateComponentControl(data, true);
        }

        private void DropMacro_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Point? position = e.Parameter as Point?;
            MacroComponentData data = worksheetData.AddMacroComponent();
            data.PositionX = position.Value.X;
            data.PositionY = position.Value.Y;
            CreateComponentControl(data, true);
        }

        private void DropShape_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Point? position = e.Parameter as Point?;
            ShapeComponentData data = worksheetData.AddShapeComponent();
            data.PositionX = position.Value.X;
            data.PositionY = position.Value.Y;
            CreateComponentControl(data, true);
        }

        private void DropIf_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Point? position = e.Parameter as Point?;
            IfComponentData data = worksheetData.AddIfComponent();
            data.PositionX = position.Value.X;
            data.PositionY = position.Value.Y;
            CreateComponentControl(data, true);
        }

        private void DropLoop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Point? position = e.Parameter as Point?;
            LoopComponentData data = worksheetData.AddLoopComponent();
            data.PositionX = position.Value.X;
            data.PositionY = position.Value.Y;
            CreateComponentControl(data, true);
        }

        private void ActivateComponent_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is FrameworkElement)
            {
                FrameworkElement el = e.Parameter as FrameworkElement;
                if (el.Parent is ISelectable)
                {
                    designerCanvas.SelectionService.SelectItem(el.Parent as ISelectable);
                }
            }
            this.currentActiveComponent = e.Parameter;
        }

        private void DeactivateComponent_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is FrameworkElement)
            {
                FrameworkElement el = e.Parameter as FrameworkElement;
                if (el.Parent is ISelectable)
                {
                    designerCanvas.SelectionService.RemoveFromSelection(el.Parent as ISelectable);
                }
            }
            if (this.currentActiveComponent == e.Parameter)
            {
                this.currentActiveComponent = null;
                // prevActiveComponent is set automatically to previous active component (in setter)
                propertyGrid.SelectedObject = null;
                TreeViewDeselectAll(LogicalTreeHelper.GetChildren(outlineTreeView), false);
            }
        }

        private void TreeViewDeselectAll(IEnumerable myTreeViewItems, bool value)
        {
            if (myTreeViewItems != null)
            {
                foreach (var currentItem in myTreeViewItems)
                {
                    if (currentItem is TreeViewItem)
                    {
                        TreeViewItem item = (TreeViewItem)currentItem;
                        item.IsSelected = value;
                        if (item.HasItems)
                        {
                            TreeViewDeselectAll(LogicalTreeHelper.GetChildren(item), value);
                        }
                    }
                }
            }
        }

        private void UnselectComponent()
        {
            // clear selection
            this.currentActiveComponent = null;
            this.prevActiveComponent = null;    // also clear previous component
            propertyGrid.SelectedObject = null;
            TreeViewDeselectAll(LogicalTreeHelper.GetChildren(outlineTreeView), false);
        }

        private void UnselectComponent_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            UnselectComponent();
            // move focus
            DependencyObject scope = FocusManager.GetFocusScope(sender as DependencyObject);
            FocusManager.SetFocusedElement(scope, Application.Current.MainWindow);
            Keyboard.Focus(Application.Current.MainWindow);
        }

        // true if a variable is received when clicking component
        static public bool? ClickComponentReceiveVariable;

        private void ClickComponent_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            bool shouldSelectObject = false;
            object newComponent = e.Parameter;
            if (newComponent == scrollViewer)
            {
                UnselectComponent();
            }
            else if (newComponent is DesignerItem)
            {
                DesignerItem designerItem = newComponent as DesignerItem;
                if (designerItem.Content is IComponentControl)
                {
                    IComponentControl control = (newComponent as DesignerItem).Content as IComponentControl;
                    propertyGrid.SelectedObject = control.rootData;
                    propertyGrid.SelectedObjectName = control.rootData.autoLabel;
                    propertyGrid.SelectedObjectTypeName = control.rootData.typeLabel;
                }
            }
            else
            {
                ClickComponentReceiveVariable = false;
                object newObj = e.Parameter;
                object oldObj = this.currentActiveComponent;

                if (windowGoalseek != null)
                {
                    // redirect receiver to window goalseek
                    oldObj = windowGoalseek;
                }
                else if (windowDatatable != null)
                {
                    oldObj = windowDatatable;
                }

                if (newComponent is SpreadsheetComponentControl)
                {
                    // when clicking spreadsheet, the focus is forced to change to spreadsheet, so the currentActiveComponent has already changed to null
                    // thus check also the previous active component
                    if (oldObj == null)
                    {
                        oldObj = this.prevActiveComponent;
                    }
                }
                if (oldObj == null)
                {
                    shouldSelectObject = true;
                }
                else
                {
                    // if there's other selected item, this item may need to be linked to that one's expression, if possible
                    if (oldObj != newObj)
                    {
                        if (newObj is IComponentVariableSource && oldObj is IComponentVariableReceiver)
                        {
                            if (oldObj is SpreadsheetCellText)
                            {
                                // need to save the old cell information before focusing to the clicked component
                                AbstractComponentData data = (newObj as IComponentVariableSource).GetData();
                                SpreadsheetCellText cellText = oldObj as SpreadsheetCellText;
                                if (!cellText.IsFocused)
                                {
                                    SpreadsheetComponentControl control = mapComponentControl[cellText.data.parentId] as SpreadsheetComponentControl;
                                    PointInt position = control.data.GetPositionFromCellId(cellText.data.id).Value;
                                    DataGridCell gridCell = control.grid.GetCell(position.Y, position.X);
                                    DataGridCellInfo cellInfo = new DataGridCellInfo(gridCell);
                                    control.grid.CurrentCell = cellInfo;
                                    control.grid.SelectedCells.Clear();
                                    control.grid.SelectedCells.Add(control.grid.CurrentCell);
                                    control.grid.BeginEdit();
                                    SpreadsheetComponentCell cell = gridCell.Content as SpreadsheetComponentCell;
                                    cell.textBox.Focus();
                                    oldObj = cell.textBox;
                                }
                                bool success = (oldObj as SpreadsheetCellText).ReceiveComponentData(data);
                                shouldSelectObject = !success;
                            }
                            else
                            {
                                bool success = (oldObj as IComponentVariableReceiver).ReceiveComponentVariable(newObj as IComponentVariableSource);
                                shouldSelectObject = !success;
                            }
                        }
                        else if (newObj is ShapeComponentText && oldObj is ShapeComponentText)
                        {
                            (oldObj as ShapeComponentText).CreateConnection(newObj as ShapeComponentText);
                        }
                        else
                        {
                            shouldSelectObject = true;
                        }
                    }
                    else
                    {
                        shouldSelectObject = true;
                    }
                }
                ClickComponentReceiveVariable = !shouldSelectObject;
                if (shouldSelectObject)
                {
                    /*if (newComponent is DesignerItem)
                    {
                        DesignerItem item = newComponent as DesignerItem;
                        Console.Out.WriteLine("Select");
                        if (item.Content is ExpressionComponentControl)
                        {
                            (item.Content as ExpressionComponentControl).textBox.Focus();
                        }
                        //designerCanvas.SelectionService.SelectItem(newComponent as DesignerItem);
                    }*/
                    if (newComponent is ExpressionComponentText)
                    {
                        (newComponent as ExpressionComponentText).Focus();
                    }
                    else if (newComponent is SpreadsheetCellText)
                    {
                        (newComponent as SpreadsheetCellText).Focus();
                    }
                    else if (newComponent is SpreadsheetHeaderExpressionText)
                    {
                        (newComponent as SpreadsheetHeaderExpressionText).Focus();
                    }
                    else if (newComponent is SpreadsheetHeaderText)
                    {
                        (newComponent as SpreadsheetHeaderText).Focus();
                    }
                    else if (newComponent is MacroComponentTextExpression)
                    {
                        (newComponent as MacroComponentTextExpression).Focus();
                    }
                    else if (newComponent is MacroComponentTextParam)
                    {
                        (newComponent as MacroComponentTextParam).Focus();
                    }
                    else if (newComponent is ShapeComponentText)
                    {
                        (newComponent as ShapeComponentText).Focus();
                    }
                    else if (newComponent is IfComponentText)
                    {
                        (newComponent as IfComponentText).Focus();
                    }
                    else if (newComponent is LoopComponentTextExpression)
                    {
                        (newComponent as LoopComponentTextExpression).Focus();
                    }
                    else if (newComponent is LoopComponentTextParam)
                    {
                        (newComponent as LoopComponentTextParam).Focus();
                    }
                    else if (newComponent is SpreadsheetComponentControl)
                    {
                        // if only a cell is selected, display the cell properties on property grid
                        SpreadsheetComponentControl spreadsheet = newComponent as SpreadsheetComponentControl;
                        if (spreadsheet.grid.SelectedCells.Count == 1)
                        {
                            DataGridCellInfo cellInfo = spreadsheet.grid.SelectedCells[0];
                            SpreadsheetComponentCell cell = cellInfo.Column.GetCellContent(cellInfo.Item) as SpreadsheetComponentCell;                            
                            propertyGrid.SelectedObject = cell.data;
                            propertyGrid.SelectedObjectName = cell.data.autoLabel;
                            propertyGrid.SelectedObjectTypeName = cell.data.typeLabel;
                        }
                    }
                }
            }
        }

        #endregion

        void worksheetData_DataChanged(object sender, ComponentDataChangedEventArgs e)
        {
            string componentId = worksheetData.GetRootComponentId(e.variableId);
            if (mapComponentControl.ContainsKey(componentId))
            {
                object obj = mapComponentControl[componentId];
                if (obj is ExpressionComponentControl)
                {
                    (obj as ExpressionComponentControl).UpdateDisplay();
                }
                else if (obj is SpreadsheetComponentControl)
                {
                    (obj as SpreadsheetComponentControl).UpdateDisplay(e.variableId);
                }
                else if (obj is MacroComponentControl)
                {
                    (obj as MacroComponentControl).UpdateDisplay();
                }
                else if (obj is ShapeComponentControl)
                {
                    (obj as ShapeComponentControl).UpdateDisplay();
                }
                else if (obj is IfComponentControl)
                {
                    (obj as IfComponentControl).UpdateDisplay();
                }
                else if (obj is LoopComponentControl)
                {
                    (obj as LoopComponentControl).UpdateDisplay();
                }
            }
        }

        void worksheetData_PropertyChanged(object sender, ComponentPropertyChangedEventArgs e)
        {
            undoManager.PushCommand(new UndoableCommand_Property
            {
                type = UndoableCommandType.ChangeComponentProperty,
                appliedObject = e.VariableId,
                oldValue = e.OldProperties,
                newValue = e.NewProperties
            });
        }

        IConnectable GetConnectable(string variableId)
        {
            string componentId = worksheetData.GetRootComponentId(variableId);
            if (mapComponentControl.ContainsKey(componentId))
            {
                object obj = mapComponentControl[componentId];
                if (obj is IComponentControl)
                {
                    return (obj as IComponentControl).GetConnector(variableId);
                }
            }
            return null;
        }

        void worksheetData_DependenceAdded(object sender, ComponentDependenceEventArgs e)
        {
            IConnectable source = GetConnectable(e.sourceVariableId);
            IConnectable target = GetConnectable(e.targetVariableId);
            AddDependence(source, target);
        }

        void worksheetData_DependenceRemoved(object sender, ComponentDependenceEventArgs e)
        {
            IConnectable source = GetConnectable(e.sourceVariableId);
            IConnectable target = GetConnectable(e.targetVariableId);
            RemoveDependence(source, target);
        }

        private void AddDependence_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DependenceCommandArgs args = e.Parameter as DependenceCommandArgs;
            IConnectable target = GetConnectable(args.targetVariableId);
            AddDependence(args.source, target);
        }

        private void RemoveDependence_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DependenceCommandArgs args = e.Parameter as DependenceCommandArgs;
            IConnectable target = GetConnectable(args.targetVariableId);
            RemoveDependence(args.source, target);
        }

        ControlConnection GetControlConnection(string sourceId, string targetId)
        {
            return this.GetConnectable(sourceId).connector.Connections.Find(o => o.Sink == this.GetConnectable(targetId).connector) as ControlConnection;
        }

        bool IsSpreadsheetConnectable(IConnectable connectable)
        {
            return connectable is SpreadsheetCellText || connectable is SpreadsheetHeaderExpressionText;
        }

        string GetSpreadsheetIdOfConnectable(IConnectable connectable)
        {
            if (connectable is SpreadsheetCellText)
            {
                SpreadsheetCellText text = connectable as SpreadsheetCellText;
                return text.data.parentId;
            }
            else if (connectable is SpreadsheetHeaderExpressionText)
            {
                SpreadsheetHeaderExpressionText text = connectable as SpreadsheetHeaderExpressionText;
                return text.data.parentId;
            }
            return null;
        }

        bool IsSpreadsheetConnectableHasHeaderExpression(IConnectable connectable)
        {
            if (connectable is SpreadsheetCellText)
            {
                SpreadsheetCellText text = connectable as SpreadsheetCellText;
                SpreadsheetComponentData spreadsheet = (mapComponentControl[text.data.parentId] as SpreadsheetComponentControl).data;
                return (spreadsheet.GetColumnDataFromColumnId(text.data.GetColumnId()).HasExpression()
                    || spreadsheet.GetRowDataFromRowId(text.data.GetRowId()).HasExpression());
            }
            return false;
        }

        private void AddDependence(IConnectable source, IConnectable target)
        {
            if (source != null && target != null)
            {
                // dependence to macro or loop doesn't need to be visualized
                if (target is MacroComponentTextExpression || target is LoopComponentTextExpression) return;
                // dependence between subcomponents of 1 spreadsheet is hidden anyway, so no need to visualize
                if (IsSpreadsheetConnectable(source) && IsSpreadsheetConnectable(target))
                {
                    string sourceParentId = GetSpreadsheetIdOfConnectable(source);
                    string targetParentId = GetSpreadsheetIdOfConnectable(target);
                    if (sourceParentId == targetParentId)
                    {
                        return;
                    }
                }
                // dependence arrow from cells with row/column expression is delegated to the row/column header
                if (IsSpreadsheetConnectableHasHeaderExpression(source)) return;

                // validity ok, add arrow
                ControlConnection connection = source.connector.Connections.Find(o => o.Sink == target.connector) as ControlConnection;
                if (connection != null)
                {
                    connection.IncrementEdgeCount();
                }
                else
                {
                    connection = new ControlConnection(source.connector, target.connector);
                    if (source.connector.CanSelectConnection && target.connector.CanSelectConnection)
                    {
                        connection.IsSelectable = true;
                    
                    }
                    if (source.IsFocused || target.IsFocused)
                    {
                        connection.IsOpaque = true;
                    }
                    designerCanvas.Children.Insert(0, connection);
                }
            }
        }

        private void RemoveDependence(IConnectable source, IConnectable target)
        {
            if (target is MacroComponentTextExpression) return;
            if (source != null && target != null)
            {
                ControlConnection connection = source.connector.Connections.Find(o => o.Sink == target.connector) as ControlConnection;
                if (connection != null)
                {
                    connection.DecrementEdgeCount();
                }
            }
        }

        #region spreadsheet commands

        private void SpreadsheetAddColumn_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int? col = e.Parameter as int?;
            SpreadsheetComponentControl control = e.Source as SpreadsheetComponentControl;
            try
            {
                AddSpreadsheetColumn(control.data.id, col);
                undoManager.PushCommand(new UndoableCommand_Routed { 
                    type = UndoableCommandType.RoutedCommand,
                    appliedObject = control.data.id,
                    command = e.Command,
                    parameter = e.Parameter
                });
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        void AddSpreadsheetColumn(string componentId, int? col, List<SpreadsheetCellData> restoreCells = null, SpreadsheetColumnData restoreColumn = null)
        {
            SpreadsheetComponentControl control = mapComponentControl[componentId] as SpreadsheetComponentControl;
            worksheetData.AddSpreadsheetColumn(control.data, col, restoreCells, restoreColumn);
            control.AddColumn(col);
        }

        private void SpreadsheetAddRow_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int? row = e.Parameter as int?;
            SpreadsheetComponentControl control = e.Source as SpreadsheetComponentControl;
            try
            {
                AddSpreadsheetRow(control.data.id, row);
                undoManager.PushCommand(new UndoableCommand_Routed
                {
                    type = UndoableCommandType.RoutedCommand,
                    appliedObject = control.data.id,
                    command = e.Command,
                    parameter = e.Parameter
                });
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        void AddSpreadsheetRow(string componentId, int? row, List<SpreadsheetCellData> restoreCells = null, SpreadsheetRowData restoreRow = null)
        {
            SpreadsheetComponentControl control = mapComponentControl[componentId] as SpreadsheetComponentControl;
            worksheetData.AddSpreadsheetRow(control.data, row, restoreCells, restoreRow);
            control.AddRow(row);
        }        

        private void SpreadsheetDeleteColumn_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int? col = e.Parameter as int?;
            SpreadsheetComponentControl control = e.Source as SpreadsheetComponentControl;
            try
            {
                List<SpreadsheetCellData> deletedCells;
                SpreadsheetColumnData deletedColumn;
                DeleteSpreadsheetColumn(control.data.id, col, out deletedCells, out deletedColumn);
                List<object> data = new List<object>{ deletedCells, deletedColumn };
                undoManager.PushCommand(new UndoableCommand_Routed
                {
                    type = UndoableCommandType.RoutedCommand,
                    appliedObject = control.data.id,
                    command = e.Command,
                    parameter = e.Parameter,
                    data = data
                });
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        void DeleteSpreadsheetColumn(string componentId, int? col, out List<SpreadsheetCellData> deletedCells, out SpreadsheetColumnData deletedColumn)
        {
            SpreadsheetComponentControl control = mapComponentControl[componentId] as SpreadsheetComponentControl;
            worksheetData.DeleteSpreadsheetColumn(control.data, col, out deletedCells, out deletedColumn);
            control.DeleteColumn(col);
        }        

        private void SpreadsheetDeleteRow_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int? row = e.Parameter as int?;
            SpreadsheetComponentControl control = e.Source as SpreadsheetComponentControl;
            try
            {
                List<SpreadsheetCellData> deletedCells;
                SpreadsheetRowData deletedRow;
                DeleteSpreadsheetRow(control.data.id, row, out deletedCells, out deletedRow);
                List<object> data = new List<object> { deletedCells, deletedRow };
                undoManager.PushCommand(new UndoableCommand_Routed
                {
                    type = UndoableCommandType.RoutedCommand,
                    appliedObject = control.data.id,
                    command = e.Command,
                    parameter = e.Parameter,
                    data = data
                });
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        void DeleteSpreadsheetRow(string componentId, int? row, out List<SpreadsheetCellData> deletedCells, out SpreadsheetRowData deletedRow)
        {
            SpreadsheetComponentControl control = mapComponentControl[componentId] as SpreadsheetComponentControl;
            worksheetData.DeleteSpreadsheetRow(control.data, row, out deletedCells, out deletedRow);
            control.DeleteRow(row);
        }

        void ExportExcel_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SpreadsheetComponentControl control = null;
            DesignerItem designerItem = designerCanvas.SelectionService.CurrentSelection[0] as DesignerItem;
            control = designerItem.Content as SpreadsheetComponentControl;

            System.Windows.Forms.SaveFileDialog saveDialog = new System.Windows.Forms.SaveFileDialog();
            saveDialog.InitialDirectory = Convert.ToString(Environment.SpecialFolder.MyDocuments);
            saveDialog.Filter = "Excel Workbook|*.xlsx|Excel 97-2003 Workbook|*.xls";

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string extension = System.IO.Path.GetExtension(saveDialog.FileName);
                if (extension.ToLower() == ".xlsx")
                {
                    try
                    {
                        ExcelExporter.ExportToXLSX(control.data, saveDialog.FileName);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(exc.Message);
                    }
                }
                else if (extension.ToLower() == ".xls")
                {
                    try
                    {
                        ExcelExporter.ExportToXLS(control.data, saveDialog.FileName);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(exc.Message);
                    }
                }
            }
        }

        #endregion

        #region macro commands

        private void MacroChangeName_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string newName = (string)e.Parameter;
            MacroComponentControl control = e.Source as MacroComponentControl;
            worksheetData.ChangeMacroName(control.data, newName);
        }

        private void MacroAddParam_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MacroComponentControl control = e.Source as MacroComponentControl;
            AddMacroParameter(control.data.id);
            undoManager.PushCommand(new UndoableCommand_Routed
            {
                type = UndoableCommandType.RoutedCommand,
                appliedObject = control.data.id,
                command = e.Command
            });
        }

        void AddMacroParameter(string componentId)
        {
            MacroComponentControl control = mapComponentControl[componentId] as MacroComponentControl;
            control.data.AddParameter();
        }

        private void MacroDeleteParam_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MacroComponentControl control = e.Source as MacroComponentControl;
            DeleteMacroParameter(control.data.id);
            undoManager.PushCommand(new UndoableCommand_Routed
            {
                type = UndoableCommandType.RoutedCommand,
                appliedObject = control.data.id,
                command = e.Command
            });
        }

        void DeleteMacroParameter(string componentId)
        {
            MacroComponentControl control = mapComponentControl[componentId] as MacroComponentControl;
            control.data.DeleteParameter();
        }

        #endregion

        #region loop commands

        private void LoopChangeName_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string newName = (string)e.Parameter;
            LoopComponentControl control = e.Source as LoopComponentControl;
            worksheetData.ChangeMacroName(control.data, newName);
        }

        private void LoopAddParam_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            LoopComponentControl control = e.Source as LoopComponentControl;
            AddLoopParameter(control.data.id);
            undoManager.PushCommand(new UndoableCommand_Routed
            {
                type = UndoableCommandType.RoutedCommand,
                appliedObject = control.data.id,
                command = e.Command
            });
        }

        void AddLoopParameter(string componentId)
        {
            LoopComponentControl control = mapComponentControl[componentId] as LoopComponentControl;
            control.data.AddParameter();
        }

        private void LoopDeleteParam_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            LoopComponentControl control = e.Source as LoopComponentControl;
            DeleteLoopParameter(control.data.id);
            undoManager.PushCommand(new UndoableCommand_Routed
            {
                type = UndoableCommandType.RoutedCommand,
                appliedObject = control.data.id,
                command = e.Command
            });
        }

        void DeleteLoopParameter(string componentId)
        {
            LoopComponentControl control = mapComponentControl[componentId] as LoopComponentControl;
            control.data.DeleteParameter();
        }

        #endregion

        #region shape commands

        private void ShapeAddConnection_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShapeComponentControl source = e.Source as ShapeComponentControl;
            ShapeComponentData targetData = e.Parameter as ShapeComponentData;
            source.data.AddConnection(targetData.id);
            ShapeConnectionData connectionData = new ShapeConnectionData
            {
                sourceId = source.data.id,
                targetId = targetData.id
            };
            undoManager.PushCommand(new UndoableCommand
            {
                type = UndoableCommandType.InsertComponent,
                appliedObject = connectionData
            });
        }

        #endregion

        #region Outline panel

        private void OutlineItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label label = sender as Label;
            DesignerItem designerItem = label.DataContext as DesignerItem;
            designerItem.ReceiveClick();         
        }

        #endregion

        // hide the overflow grid
        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }
            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = new Thickness();
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            WindowAbout window = new WindowAbout();
            window.ShowDialog();
        }
    }
}

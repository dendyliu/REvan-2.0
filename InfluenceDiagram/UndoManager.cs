using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace InfluenceDiagram
{
    enum UndoableCommandType
    {
        InsertComponent,
        DeleteComponent,
        ChangeComponentProperty,
        RoutedCommand
    }

    class UndoableCommand: ICloneable
    {
        public UndoableCommandType type;
        public object appliedObject;

        // return reversed command for Undo. Redo is not reversed
        static public UndoableCommand GetReversedForUndo(UndoableCommand command)
        {
            UndoableCommand reversed = null;
            if (command.type == UndoableCommandType.InsertComponent)
            {
                reversed = command.Clone() as UndoableCommand;
                reversed.type = UndoableCommandType.DeleteComponent;
            }
            else if (command.type == UndoableCommandType.DeleteComponent)
            {
                reversed = command.Clone() as UndoableCommand;
                reversed.type = UndoableCommandType.InsertComponent;
            }
            else if (command.type == UndoableCommandType.ChangeComponentProperty)
            {
                reversed = UndoableCommand_Property.GetReversed(command as UndoableCommand_Property);
            }
            else if (command.type == UndoableCommandType.RoutedCommand)
            {
                reversed = UndoableCommand_Routed.GetReversed(command as UndoableCommand_Routed);
            }
            else
            {
                throw new Exception(String.Format("Command type {0} cannot be reversed", command.type));
            }
            return reversed;
        }

        virtual public object Clone()
        {
            return new UndoableCommand
            {
                type = this.type,
                appliedObject = this.appliedObject,
            };
        }
    }

    class UndoableCommand_Property: UndoableCommand
    {
        public object oldValue;
        public object newValue;

        static public UndoableCommand_Property GetReversed(UndoableCommand_Property command)
        {
            UndoableCommand_Property reversed = command.Clone() as UndoableCommand_Property;
            reversed.newValue = command.oldValue;
            reversed.oldValue = command.newValue;
            return reversed;
        }

        public override object Clone()
        {
            return new UndoableCommand_Property
            {
                type = this.type,
                appliedObject = this.appliedObject,
                oldValue = this.oldValue,
                newValue = this.newValue
            };            
        }
    }

    class UndoableCommand_Routed: UndoableCommand
    {
        public ICommand command;
        public object parameter;
        public object data;

        public UndoableCommand_Routed(): base()
        {
        }

        static public UndoableCommand_Routed GetReversed(UndoableCommand_Routed command)
        {
            UndoableCommand_Routed reversed = command.Clone() as UndoableCommand_Routed;
            if (command.command == Command.SpreadsheetAddColumn)
            {
                reversed.command = Command.SpreadsheetDeleteColumn;
            }
            else if (command.command == Command.SpreadsheetDeleteColumn)
            {
                reversed.command = Command.SpreadsheetAddColumn;
            }
            else if (command.command == Command.SpreadsheetAddRow)
            {
                reversed.command = Command.SpreadsheetDeleteRow;
            }
            else if (command.command == Command.SpreadsheetDeleteRow)
            {
                reversed.command = Command.SpreadsheetAddRow;
            }
            else if (command.command == Command.MacroAddParam)
            {
                reversed.command = Command.MacroDeleteParam;
            }
            else if (command.command == Command.MacroDeleteParam)
            {
                reversed.command = Command.MacroAddParam;
            }
            else if (command.command == Command.LoopAddParam)
            {
                reversed.command = Command.LoopDeleteParam;
            }
            else if (command.command == Command.LoopDeleteParam)
            {
                reversed.command = Command.LoopAddParam;
            }
            else if (command.command == Command.CreateDatatable)
            {
                reversed.command = Command.DeleteDatatable;
            }
            else if (command.command == Command.DeleteDatatable)
            {
                reversed.command = Command.CreateDatatable;
            }
            else
            {
                throw new Exception(String.Format("Routed Command type {0} cannot be reversed", command.command));
            }
            return reversed;
        }

        public override object Clone()
        {
            return new UndoableCommand_Routed
            {
                type = this.type,
                appliedObject = this.appliedObject,
                command = this.command,
                parameter = this.parameter,
                data = this.data
            };
        }
    }

    class UndoManager
    {
        const int MAX_STATES = 100;

        LinkedList<UndoableCommand> undoList;
        LinkedList<UndoableCommand> redoList;

        UndoableCommand lastSaveState;

        public UndoManager()
        {
            undoList = new LinkedList<UndoableCommand>();
            redoList = new LinkedList<UndoableCommand>();
        }

        public void PushCommand(UndoableCommand command)
        {
            undoList.AddLast(command);
            if (undoList.Count > MAX_STATES)
            {
                // if over capacity, remove old command
                if (lastSaveState != null && undoList.First() == lastSaveState)
                {
                    lastSaveState = null;
                }
                undoList.RemoveFirst();
            }
            // when pushing a command, the redo list always becomes empty
            redoList.Clear();
            CommandManager.InvalidateRequerySuggested();
        }

        public bool CanUndo()
        {
            return undoList.Count > 0;
        }

        public bool CanRedo()
        {
            return redoList.Count > 0;
        }

        public UndoableCommand Undo()
        {
            if (undoList.Count == 0) return null;

            UndoableCommand command = undoList.Last();
            undoList.RemoveLast();
            // when Undoing, push the command to Redo list
            redoList.AddLast(command);
            CommandManager.InvalidateRequerySuggested();
            return command;
        }

        public UndoableCommand Redo()
        {
            if (redoList.Count == 0) return null;

            UndoableCommand command = redoList.Last();
            redoList.RemoveLast();
            // when Redoing, push the command back to Undo list
            undoList.AddLast(command);
            CommandManager.InvalidateRequerySuggested();
            return command;
        }

        public void SetCurrentAsSaveState()
        {
            if (undoList.Count > 0)
            {
                lastSaveState = undoList.Last();
            }
        }

        public bool CheckNeedSave()
        {
            if (undoList.Count == 0) return false;
            else
            {
                return undoList.Last() != lastSaveState;
            }
        }
    }
}

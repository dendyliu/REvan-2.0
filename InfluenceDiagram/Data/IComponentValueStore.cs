using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfluenceDiagram.Data
{
    public interface IComponentValueStore
    {
        AbstractComponentData GetComponentData(string id);
        object GetComponentValue(string id);
        string GetComponentValueAsString(string id);
        string GetComponentLabelOrValueAsString(string id);
        string GetRootComponentId(string id);
        bool CanCreateEdge(AbstractComponentData source, AbstractComponentData target);
        void AddShapeConnection(string sourceId, string targetId);
        void RemoveShapeConnection(string sourceId, string targetId);
        void ResolveValue(string id);
        void TriggerEdgeForComponent(string id);

        void AddMacroParameter(MacroParameterData data);
        void DeleteMacroParameter(MacroParameterData data);
        void RenameMacroParameter(MacroParameterData data);

        void AddLoopParameter(LoopParameterData paramData, LoopExpressionData iterationdata);
        void DeleteLoopParameter(LoopParameterData paramData, LoopExpressionData iterationdata);
        void RenameLoopParameter(LoopParameterData paramData);

        SpreadsheetRangeData CreateSpreadsheetRangeDataFromRangeId(string var);

        bool ShouldDispatchPropertyChanged { set; }
    }
}

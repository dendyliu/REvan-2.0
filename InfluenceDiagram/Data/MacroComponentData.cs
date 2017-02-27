using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;

namespace InfluenceDiagram.Data
{
    [DataContract]
    [Serializable]
    [KnownType(typeof(MacroExpressionData))]
    [KnownType(typeof(MacroParameterData))]
    public class MacroComponentData: AbstractMacroData
    {
        public override string typeLabel
        {
            get { return "Macro"; }
        }

        override public IComponentValueStore valueStore {
            get { return _valueStore; } 
            set {
                _valueStore = value;
                if (expressionData != null)
                {
                    expressionData.valueStore = value;
                }
                if (parametersData != null)
                {
                    foreach (MacroParameterData paramData in parametersData)
                    {
                        paramData.valueStore = value;
                    }
                }
            }
        }

        //public List<string> parameters { get; private set; }
        [DataMember]
        public MacroExpressionData expressionData { get; private set; }
        [DataMember]
        public ObservableCollection<MacroParameterData> parametersData { get; private set; }
        override public string id
        {
            get { return base.id; }
            set
            {
                string oldId = base.id;
                base.id = value;
                if (parametersData != null)
                {
                    for (int i = 0; i < parametersData.Count; ++i)
                    {
                        parametersData[i].id = GetParameterId(i);
                    }
                }
                if (expressionData != null)
                {
                    expressionData.id = GetExpressionId();
                    expressionData.ReplaceExpressionVariableId(oldId, value);
                }
            }
        }
        public string expression
        {
            get { return expressionData.expression; }
            set { expressionData.expression = value; }
        }

        public override int parametersCount
        {
            get
            {
                return parametersData.Count;
            }
        }
        
        public MacroComponentData(IComponentValueStore valueStore, string id, List<string> p = null, string expression = "")
            : base(valueStore, id)
        {
            if (p != null && p.Count > 0){
                foreach (string paramName in p){
                    AddParameter(paramName, false);
                }
            }
            else
            {
                parametersData = new ObservableCollection<MacroParameterData>();
                // minimum number of parameter is 1
                AddParameter(null, false);
            }
            expressionData = new MacroExpressionData(valueStore, GetExpressionId(), expression);
        }

        public void AddParameter(string paramName = null, bool callValueStore = true)
        {
            if (paramName == null)
            {
                int i = parametersData.Count + 1;
                paramName = "p" + i;
            }
            // the parameter id is in the form <id>_0, <id>_1, so on
            MacroParameterData data = new MacroParameterData(valueStore) { id = GetParameterId(parametersData.Count), varname = paramName };
            parametersData.Add(data);
            if (callValueStore)
            {
                valueStore.AddMacroParameter(data);
            }
        }

        string GetExpressionId()
        {
            return this.id + "_e";
        }
        string GetParameterId(int i)
        {
            return this.id + "_" + i;
        }

        public void DeleteParameter()
        {
            // minimum number of parameter is 1
            if (parametersData.Count > 1)
            {
                MacroParameterData data = parametersData.Last();
                parametersData.RemoveAt(parametersData.Count - 1);
                valueStore.DeleteMacroParameter(data);
            }
        }

        public void RenameParameter(int index, string name)
        {
            parametersData[index].varname = name;
            valueStore.RenameMacroParameter(parametersData[index]);
        }
    }

    [DataContract]
    [Serializable]
    public class MacroParameterData: AbstractParameterData
    {        
        public MacroParameterData(IComponentValueStore valueStore)
            : base(valueStore)
        {

        }
    }

    [DataContract]
    [Serializable]
    public class MacroExpressionData: AbstractExpressionData
    {        
        public MacroExpressionData(IComponentValueStore valueStore, string id, string expression)
            : base(valueStore, id)
        {
            _expression = expression;
        }
    }
}

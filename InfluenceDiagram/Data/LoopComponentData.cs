using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace InfluenceDiagram.Data
{
    [DataContract]
    [Serializable]
    [KnownType(typeof(LoopExpressionData))]
    [KnownType(typeof(LoopParameterData))]
    public class LoopComponentData: AbstractMacroData
    {
        public override string typeLabel
        {
            get { return "Loop"; }
        }

        override public IComponentValueStore valueStore
        {
            get { return _valueStore; }
            set
            {
                _valueStore = value;
                if (expressionData != null)
                {
                    expressionData.valueStore = value;
                }
                if (conditionData != null)
                {
                    conditionData.valueStore = value;
                }
                if (parametersData != null)
                {
                    foreach (LoopParameterData paramData in parametersData)
                    {
                        paramData.valueStore = value;
                    }
                }
                if (iterationsData != null)
                {
                    foreach (LoopExpressionData data in iterationsData)
                    {
                        data.valueStore = value;
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
                if (parametersData != null)
                {
                    for (int i = 0; i < parametersData.Count; ++i)
                    {
                        parametersData[i].id = GetParameterId(i);
                    }
                }
                if (iterationsData != null)
                {
                    for (int i = 0; i < parametersData.Count; ++i)
                    {
                        iterationsData[i].id = GetIterationId(i);
                        iterationsData[i].ReplaceExpressionVariableId(oldId, value);
                    }
                }
                if (conditionData != null)
                {
                    conditionData.id = GetConditionId();
                    conditionData.ReplaceExpressionVariableId(oldId, value);
                }
                if (expressionData != null)
                {
                    expressionData.id = GetExpressionId();
                    expressionData.ReplaceExpressionVariableId(oldId, value);
                }
            }
        }

        [DataMember]
        public ObservableCollection<LoopParameterData> parametersData { get; private set; }
        [DataMember]
        public ObservableCollection<LoopExpressionData> iterationsData { get; private set; }
        [DataMember]
        public LoopExpressionData conditionData { get; private set; }
        [DataMember]
        public LoopExpressionData expressionData { get; private set; }

        public override int parametersCount
        {
            get
            {
                return parametersData.Count;
            }
        }

        public LoopComponentData(IComponentValueStore valueStore, string id)
            : base(valueStore, id)
        {
            parametersData = new ObservableCollection<LoopParameterData>();
            iterationsData = new ObservableCollection<LoopExpressionData>();
            // minimum number of parameter is 1
            AddParameter(null, false);
            // expression id is <id>_e, condition id is <id>_c
            expressionData = new LoopExpressionData(valueStore, GetExpressionId());
            conditionData = new LoopExpressionData(valueStore, GetConditionId());
        }

        public void AddParameter(string paramName = null, bool callValueStore = true)
        {
            if (paramName == null)
            {
                int i = parametersData.Count + 1;
                paramName = "p" + i;
            }
            // the parameter id is in the form <id>_p0, <id>_p1, so on
            // the iteration id is in the form <id>_i0, <id>_i1, so on
            LoopParameterData parameterData = new LoopParameterData(valueStore) { id = GetParameterId(parametersData.Count), varname = paramName };
            parametersData.Add(parameterData);
            LoopExpressionData iterationData = new LoopExpressionData(valueStore, GetIterationId(parametersData.Count));
            iterationsData.Add(iterationData);
            if (callValueStore)
            {
                valueStore.AddLoopParameter(parameterData, iterationData);
            }
        }

        string GetExpressionId()
        {
            return this.id + "_e";
        }
        string GetConditionId()
        {
            return this.id + "_c";
        }
        string GetParameterId(int i)
        {
            return this.id + "_p" + i;
        }
        string GetIterationId(int i)
        {
            return this.id + "_i" + i;
        }

        public void DeleteParameter()
        {
            // minimum number of parameter is 1
            if (parametersData.Count > 1)
            {
                LoopParameterData parameterData = parametersData.Last();
                LoopExpressionData iterationData = iterationsData.Last();
                parametersData.RemoveAt(parametersData.Count - 1);
                iterationsData.RemoveAt(iterationsData.Count - 1);
                valueStore.DeleteLoopParameter(parameterData, iterationData);
            }
        }

        public void RenameParameter(int index, string name)
        {
            parametersData[index].varname = name;
            valueStore.RenameLoopParameter(parametersData[index]);
        }
    }

    [DataContract]
    [Serializable]
    public class LoopParameterData: AbstractParameterData
    {
        public LoopParameterData(IComponentValueStore valueStore)
            : base(valueStore)
        {

        }
    }

    [DataContract]
    [Serializable]
    public class LoopExpressionData: AbstractExpressionData
    {
        public LoopExpressionData(IComponentValueStore valueStore, string id)
            : base(valueStore, id)
        {
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace InfluenceDiagram.Data
{
    [DataContract]
    [Serializable]
    [KnownType(typeof(IfExpressionData))]
    public class IfComponentData: RootComponentData, IExpressionData
    {
        public override string typeLabel
        {
            get { return "Conditional"; }
        }

        override public IComponentValueStore valueStore
        {
            get { return _valueStore; }
            set
            {
                _valueStore = value;
                if (conditionData != null)
                {
                    conditionData.valueStore = value;
                }
                if (trueData != null)
                {
                    trueData.valueStore = value;
                }
                if (falseData != null)
                {
                    falseData.valueStore = value;
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
                if (conditionData != null)
                {
                    conditionData.id = GetConditionId();
                    conditionData.ReplaceExpressionVariableId(oldId, value);
                }
                if (trueData != null)
                {
                    trueData.id = GetTrueId();
                    trueData.ReplaceExpressionVariableId(oldId, value);
                }
                if (falseData != null)
                {
                    falseData.id = GetFalseId();
                    falseData.ReplaceExpressionVariableId(oldId, value);
                }
            }
        }

        [DataMember]
        public IfExpressionData conditionData { get; private set; }
        [DataMember]
        public IfExpressionData trueData { get; private set; }
        [DataMember]
        public IfExpressionData falseData { get; private set; }

        public string expression
        {
            get {
                return String.Format("if([{0}],[{1}],[{2}])", conditionData.id, trueData.id, falseData.id);
            }
            set
            {
            }
        }

        public void SetExpressionRaw(string expression)
        {
        }

        public IfComponentData(IComponentValueStore valueStore, string id)
            : base(valueStore)
        {
            this.id = id;
            conditionData = new IfExpressionData(valueStore, GetConditionId());
            trueData = new IfExpressionData(valueStore, GetTrueId());
            falseData = new IfExpressionData(valueStore, GetFalseId());
        }

        string GetConditionId()
        {
            return this.id + "_0";
        }
        string GetTrueId()
        {
            return this.id + "_1";
        }
        string GetFalseId()
        {
            return this.id + "_2";
        }

        public String GetValueAsString()
        {
            return valueStore.GetComponentValueAsString(this.id);
        }
    }
    
    [DataContract]
    [Serializable]
    public class IfExpressionData: AbstractExpressionData
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
                if (valueStore != null)
                {
                    valueStore.ResolveValue(this.id);
                }
            }
        }

        public IfExpressionData(IComponentValueStore valueStore, string id)
            : base(valueStore, id)
        {
            _expression = "";
        }

    }

}

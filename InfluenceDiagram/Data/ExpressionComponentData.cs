using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.ComponentModel;
using InfluenceDiagram.ComponentControl;

namespace InfluenceDiagram.Data
{
    [DataContract]
    [Serializable]
    public class ExpressionComponentData: RootComponentData, IExpressionData
    {
        private ComponentShape _shape;
        [DataMember]
        [Category("Style")]
        [DisplayName("Shape")]
        [Editor(typeof(ComponentShapeEditor), typeof(ComponentShapeEditor))]
        public ComponentShape Shape
        {
            get { return _shape; }
            set
            {
                if (_shape != value)
                {
                    object oldValue = _shape;
                    _shape = value;
                    NotifyPropertyChanged("Shape", oldValue, value);
                }
            }
        }

        public override string typeLabel
        {
            get { return "Variable"; }
        }

        private string _expression;
        [DataMember]
        public string expression
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

        public ExpressionComponentData(IComponentValueStore valueStore, string id, string expression)
            : base(valueStore)
        {
            this.id = id;
            _expression = expression;
        }

        protected override void InitDefaultStyle()
        {
            base.InitDefaultStyle();
            _shape = ComponentShape.RectangleRounded1;
        }

        public String GetValueAsString()
        {
            return valueStore.GetComponentValueAsString(this.id);            
        }

        public void SetExpressionRaw(string expression)
        {
            _expression = expression;
        }
    }
}

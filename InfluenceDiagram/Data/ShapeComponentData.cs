using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel;
using InfluenceDiagram.ComponentControl;

namespace InfluenceDiagram.Data
{
    [DataContract]
    [Serializable]
    public class ShapeComponentData: RootComponentData
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
            get { return "Mindmap"; }
        }

        [DataMember]
        public HashSet<string> connections { get; private set; }

        public ShapeComponentData(IComponentValueStore valueStore, string id)
            : base(valueStore)
        {
            this.id = id;
            connections = new HashSet<string>();
        }

        protected override void InitDefaultStyle()
        {
            base.InitDefaultStyle();
            _shape = ComponentShape.Octagon;
        }

        public void AddConnection(string targetId)
        {
            bool success = connections.Add(targetId);
            if (success)
            {
                valueStore.AddShapeConnection(this.id, targetId);
            }
        }

        public void RemoveConnection(string targetId)
        {
            bool success = connections.Remove(targetId);
            if (success)
            {
                valueStore.RemoveShapeConnection(this.id, targetId);
            }
        }
    }

    // transient class, only used to store information for Undo/Redo a delete connection command.
    public class ShapeConnectionData
    {
        public string sourceId;
        public string targetId;
    }
}

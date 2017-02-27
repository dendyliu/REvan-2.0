using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Runtime.Serialization;
using System.Windows.Data;
using System.Windows.Controls;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Media;
using InfluenceDiagram.Utility;

namespace InfluenceDiagram.Data
{

    [DataContract]
    [Serializable]
    [KnownType(typeof(RootComponentData))]
    abstract public class AbstractComponentData : INotifyPropertyChanged
    {
        protected string _id;
        [DataMember]
        virtual public String id { get { return _id; } set { _id = value; } }   // the complete id
        virtual public string autoLabel { get; set; }
        [NonSerialized]
        protected IComponentValueStore _valueStore;
        virtual public IComponentValueStore valueStore { get { return _valueStore; } set { _valueStore = value; } }

        public AbstractComponentData(IComponentValueStore valueStore)
        {
            this.valueStore = valueStore;
            InitDefaultStyle();
        }

        public string parentId
        {
            get { return id.Split('_')[0]; }
        }

        protected Color GetDefaultColor(string property)
        {
            string className = this.GetType().Name;
            // resource name is in format Component.<data_class>.DefaultBorderColor, etc
            Color? color = Application.Current.TryFindResource(String.Format("Component.{0}.Default{1}Color", className, property)) as Color?;
            if (color.HasValue)
                return color.Value;
            else
            {
                // default color is in format Component.DefaultBorderColor, etc
                return (Color)Application.Current.Resources[String.Format("Component.Default{0}Color", property)];
            }
        }

        virtual protected void InitDefaultStyle()
        {
        }


        #region INotifyPropertyChangedExtended Members

        // we could use DependencyProperties as well to inform others of property changes
        [NonSerialized]
        private PropertyChangedEventHandler _propertyChanged;
        public event PropertyChangedEventHandler PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }

        protected void NotifyPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = _propertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        protected void NotifyPropertyChanged(string name, object oldValue, object newValue)
        {
            PropertyChangedEventHandler handler = _propertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedExtendedEventArgs(name, oldValue, newValue));
            }
        }

        #endregion
    }

    // component data that stands by itself (i.e. those that can be placed on worksheet
    [DataContract]
    [Serializable]
    [KnownType(typeof(ExpressionComponentData))]
    [KnownType(typeof(SpreadsheetComponentData))]
    [KnownType(typeof(MacroComponentData))]
    [KnownType(typeof(ShapeComponentData))]
    [KnownType(typeof(IfComponentData))]
    [KnownType(typeof(LoopComponentData))]
    abstract public class RootComponentData: AbstractComponentData
    {
        private String _label;
        [DataMember]
        [Category("General")]
        [DisplayName("Label")]
        public String label 
        { 
            get {            
                return _label;
            }
            set {
                if (_label != value)
                {
                    object oldValue = _label;
                    _label = value;
                    NotifyPropertyChanged("label", oldValue, value);
                    NotifyPropertyChanged("autoLabel"); // autoLabel depends on label
                }
            } 
        }
        private double _positionX;
        [DataMember]
        [Category("General")]
        [DisplayName("X")]
        public double PositionX
        {
            get
            {
                return _positionX;
            }
            set
            {
                if (_positionX != value)
                {
                    object oldValue = _positionX;
                    _positionX = value;
                    NotifyPropertyChanged("PositionX"); // don't store to Undo stack, for now
                    // TODO: need to store position change when done via Property Panel, but not in unit change
                }
            }
        }
        private double _positionY;
        [DataMember]
        [Category("General")]
        [DisplayName("Y")]
        public double PositionY
        {
            get
            {
                return _positionY;
            }
            set
            {
                if (_positionY != value)
                {
                    object oldValue = _positionY;
                    _positionY = value;
                    NotifyPropertyChanged("PositionY"); // don't store to Undo stack, for now
                    // TODO: need to store position change when done via Property Panel, but not in unit change
                }
            }
        }

        abstract public string typeLabel { get; }

        override public string autoLabel
        {
            get
            {
                if (label != null && label.Length > 0)
                    return label;
                else
                    return typeLabel + " " + id.Substring(1);
            }
        }


        #region component styles

        [NonSerialized]
        private Color _borderColor;
        [DataMember]
        [Category("Style")]
        [DisplayName("Border Color")]
        public Color BorderColor {
            get { return _borderColor; }
            set
            {
                if (_borderColor != value)
                {
                    object oldValue = _borderColor;
                    _borderColor = value;
                    NotifyPropertyChanged("BorderColor", oldValue, value);
                }
            }
        }
        private String BorderColor_Surrogate;

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

        public RootComponentData(IComponentValueStore valueStore)
            : base(valueStore)
        {
        }

        override protected void InitDefaultStyle()
        {
            base.InitDefaultStyle();
            this.BackgroundColor = GetDefaultColor("Background");
            this.BorderColor = GetDefaultColor("Border");
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
            BorderColor_Surrogate = BorderColor.ToString();
            BackgroundColor_Surrogate = BackgroundColor.ToString();
            FontColor_Surrogate = FontColor.ToString();
        }

        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            if (BorderColor_Surrogate != null)
            {
                BorderColor = (Color)ColorConverter.ConvertFromString(BorderColor_Surrogate);
            }
            if (BackgroundColor_Surrogate != null)
            {
                BackgroundColor = (Color)ColorConverter.ConvertFromString(BackgroundColor_Surrogate);
            }
            if (FontColor_Surrogate != null)
            {
                FontColor = (Color)ColorConverter.ConvertFromString(FontColor_Surrogate);
            }
        }

        public void BindPositionToCanvas(DependencyObject obj)
        {
            // even with TwoWay binding, it won't update the target because the class doesn't implement INotifyPropertyChange
            // the TwoWay binding is only useful for updating the target first time, right when the binding is done
            Binding positionXBinding = new Binding("PositionX");
            positionXBinding.Source = this;
            positionXBinding.Mode = BindingMode.TwoWay;
            positionXBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(obj, Canvas.LeftProperty, positionXBinding);
            Binding positionYBinding = new Binding("PositionY");
            positionYBinding.Source = this;
            positionYBinding.Mode = BindingMode.TwoWay;
            positionYBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(obj, Canvas.TopProperty, positionYBinding);
        }
    }

    [DataContract]
    [Serializable]
    abstract public class AbstractMacroData: RootComponentData
    {
        private String _description;
        [DataMember]
        [Category("General")]
        [DisplayName("Description")]
        public String Description
        {
            get { return _description; }
            set
            {
                if (_description != value)
                {
                    object oldValue = _description;
                    _description = value;
                    NotifyPropertyChanged("Description", oldValue, value);
                }
            }
        }

        protected string _function;
        [DataMember]
        public string Function // this is the function name
        {
            get { return _function; }
            set
            {
                if (DataHelper.IsValidFunctionName(value))
                {
                    if (_function != value)
                    {
                        object oldValue = _function;
                        _function = value;
                        NotifyPropertyChanged("Function", oldValue, value);
                    }
                }
                else
                {
                    throw new DataException("Function name must begin with alphabet and contains only alphanumerics");
                }
            }
        }

        public virtual int parametersCount { get { return 0; } }

        public AbstractMacroData(IComponentValueStore valueStore, string id)
            : base(valueStore)
        {
            _id = id;
        }

    }

    public interface IExpressionData
    {
        string expression { get; set; }
        void SetExpressionRaw(string expression);   // set expression without dispatching changes
    }

    [DataContract]
    [Serializable]
    abstract public class AbstractExpressionData : AbstractComponentData, IExpressionData
    {   
        protected string _expression;
        [DataMember]
        public virtual string expression
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

        public void SetExpressionRaw(string expression)
        {
            _expression = expression;
        }

        public AbstractExpressionData(IComponentValueStore valueStore, string id)
            : base(valueStore)
        {
            _id = id;
            _expression = "";
        }

        public String GetValueAsString()
        {
            return valueStore.GetComponentValueAsString(this.id);
        }

        public object GetValue()
        {
            return valueStore.GetComponentValue(this.id);
        }
        
        public void ReplaceExpressionVariableId(string oldId, string newId)
        {
            _expression = DataHelper.ReplaceExpressionVariableId(_expression, oldId, newId);
        }
    }

    [DataContract]
    [Serializable]
    abstract public class AbstractParameterData : AbstractComponentData
    {
        // variable name (x, y, etc)
        protected string _varname;
        [DataMember]
        public virtual string varname
        {
            get { return _varname; }
            set
            {
                if (DataHelper.IsValidParamName(value))
                {
                    if (_varname != value)
                    {
                        object oldValue = _varname;
                        _varname = value;
                        NotifyPropertyChanged("varname", oldValue, value);
                    }
                }
                else
                {
                    throw new DataException("Parameter name must begin with alphabet and contains only alphanumerics");
                }
            }
        }

        public AbstractParameterData(IComponentValueStore valueStore)
            : base(valueStore)
        {

        }
    }
}

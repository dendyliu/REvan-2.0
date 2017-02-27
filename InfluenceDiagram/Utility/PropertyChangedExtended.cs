using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace InfluenceDiagram.Utility
{
    public class PropertyChangedExtendedEventArgs : PropertyChangedEventArgs
    {
        public virtual object OldValue { get; private set; }
        public virtual object NewValue { get; private set; }

        public PropertyChangedExtendedEventArgs(string propertyName, object oldValue, object newValue)
            : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    // Summary: Notifies clients that a property value is changing, but includes extended event infomation
    /* The following NotifyPropertyChanged Interface is employed when you wish to enforce the inclusion of old and
     * new values. (Users must provide PropertyChangedExtendedEventArgs, PropertyChangedEventArgs are disallowed.) */
    public interface INotifyPropertyChangedExtended
    {
        event PropertyChangedExtendedEventHandler PropertyChanged;
    }

    public delegate void PropertyChangedExtendedEventHandler(object sender, PropertyChangedExtendedEventArgs e);
}

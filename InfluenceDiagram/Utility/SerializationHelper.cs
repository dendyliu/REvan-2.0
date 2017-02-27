using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows;

namespace InfluenceDiagram.Utility
{
    public class SerializationHelper
    {
        public static bool IsBinarySerializable(object obj)
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
    }
}

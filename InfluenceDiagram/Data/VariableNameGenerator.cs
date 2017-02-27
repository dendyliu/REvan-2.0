using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfluenceDiagram.Data
{
    [DataContract]
    [Serializable]
    class VariableNameGenerator
    {
        [DataMember]
        private String prefix = "";
        [DataMember]
        private int lastIndex;

        /** the variable indexing starts with v1, v2, v3, etc **/
        public VariableNameGenerator(string prefix)
        {
            this.prefix = prefix;
        }

        public String NewVariableName()
        {
            lastIndex++;
            return prefix + lastIndex;
        }

        /** for setting last index e.g. when loading worksheet data **/
        public void SetLastIndex(int index)
        {
            lastIndex = index;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;

namespace InfluenceDiagram.Data
{
    public class WorksheetSerializer
    {
        public void SaveWorksheet(WorksheetData data, string path)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(WorksheetData));
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = Encoding.UTF8;
            XmlWriter stream = XmlWriter.Create(path, settings); 
            serializer.WriteObject(stream, data);
            stream.Close();
            data.currentFilePath = path;
        }

        public WorksheetData LoadWorksheet(string path, bool asExternal = false)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(WorksheetData));            
            XmlReader stream = XmlReader.Create(path);
            try
            {
                WorksheetData data = serializer.ReadObject(stream) as WorksheetData;
                data.currentFilePath = path;
                data.AfterDeserialized(asExternal);
                return data;
            }
            catch (Exception e)
            {
                throw new Exception("Cannot load worksheet. File not recognized");
            }
            finally
            {
                stream.Close();
            }
        }
    }
}

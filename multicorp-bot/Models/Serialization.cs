using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace multicorp_bot.Models
{
    public static class Serialization
    {
        public static void Serialize(Type t, object objectToSerialize, string filename)
        {
            XmlSerializer x = new XmlSerializer(t);
            using (TextWriter writer = new StreamWriter(filename))
            {
                x.Serialize(writer, objectToSerialize);
            }
        }

        public static object Deserialize(Type t, string filename)
        {
            XmlSerializer x = new XmlSerializer(t);
            using (FileStream fileStream = new FileStream(filename, FileMode.Open))
            {
                return x.Deserialize(fileStream);
            }
        }
    }
}

using HawkEngine.Graphics;
using Silk.NET.Maths;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace HawkEngine.Core
{
    public static class HawkSerializer
    {
        private static readonly List<HawkObject> savedReferences = new();
        private static XmlWriter xmlWriter;
        private static readonly List<(HawkObject, Type)> loadedReferences = new();
        private static XmlReader xmlReader;
        private static event Action referencesLoaded;

        public static void Serialize(string path, object obj)
        {
            if (File.Exists(path)) File.WriteAllBytes(path, Array.Empty<byte>());
            Serialize(File.Open(path, FileMode.OpenOrCreate), obj);
        }
        public static void Serialize(Stream stream, object obj)
        {
            savedReferences.Clear();
            xmlWriter = XmlWriter.Create(stream, new()
            {
                CloseOutput = true,
                Indent = true,
                NewLineOnAttributes = true,
                WriteEndDocumentOnClose = true,
            });

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("Root");

            SerializeByType(obj, obj.ToString(), "Start", obj.GetType());

            for (int i = 0; i < savedReferences.Count; i++)
            {
                SerializeWithReflection(savedReferences[i], savedReferences[i].name, "Reference", true);
            }

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }
        private static void SerializeByType(object obj, string name, string info, Type nullType)
        {
            Type objType = obj?.GetType() ?? nullType;

            xmlWriter.WriteStartElement(info);
            xmlWriter.WriteAttributeString("Name", name);
            xmlWriter.WriteAttributeString("Type", objType.AssemblyQualifiedName);

            if (obj is null)
            {
                xmlWriter.WriteString("null");
            }
            else if (obj is string str)
            {
                xmlWriter.WriteString(str);
            }
            else if (obj is Array array)
            {
                xmlWriter.WriteAttributeString("ElementType", objType.GetElementType().AssemblyQualifiedName);
                xmlWriter.WriteAttributeString("Length", array.Length.ToString());

                int i = 0;
                foreach (object item in array)
                {
                    SerializeByType(item, i.ToString(), "Item", objType.GetElementType());
                    i++;
                }
            }
            else if (obj is IDictionary dictionary)
            {
                xmlWriter.WriteAttributeString("Length", dictionary.Count.ToString());

                Array keys = Array.CreateInstance(objType.GetGenericArguments()[0], dictionary.Count);
                dictionary.Keys.CopyTo(keys, 0);

                Array values = Array.CreateInstance(objType.GetGenericArguments()[1], dictionary.Count);
                dictionary.Values.CopyTo(values, 0);

                SerializeByType(keys, "Keys", "Keys", objType.GetGenericArguments()[0]);
                SerializeByType(values, "Values", "Values", objType.GetGenericArguments()[1]);
            }
            else if (obj is IList list)
            {
                xmlWriter.WriteAttributeString("Length", list.Count.ToString());

                int i = 0;
                foreach (object item in list)
                {
                    SerializeByType(item, i.ToString(), "Item", objType.GenericTypeArguments[0]);
                    i++;
                }
            }
            else if (obj is HawkObject hawkObj)
            {
                if (!savedReferences.Contains(hawkObj)) savedReferences.Add(hawkObj);
                xmlWriter.WriteString(hawkObj.engineID.ToString());
            }
            else if (obj.GetType().GetInterface("IParsable`1") != null)
            {
                xmlWriter.WriteString(obj.ToString());
            }
            else if (obj is bool boolean)
            {
                xmlWriter.WriteString(boolean.ToString());
            }
            else if ((objType.Namespace == "System.Numerics" || objType.Namespace == "Silk.NET.Maths")
                && (objType.Name.Contains("Vector") || objType.Name.Contains("Quaternion")))
            {
                FieldInfo[] fields = new FieldInfo[4]
                {
                    objType.GetField("X"),
                    objType.GetField("Y"),
                    objType.GetField("Z"),
                    objType.GetField("W"),
                };

                for (int i = 0; i < 4; i++)
                {
                    if (fields[i] == null) break;
                    SerializeByType(fields[i].GetValue(obj), fields[i].Name, fields[i].Name, objType.GetGenericArguments()[0]);
                }
            }
            else if (objType.IsEnum)
            {
                xmlWriter.WriteString(obj.ToString());
            }
            else
            {
                SerializeWithReflection(obj, name, info, false);
            }

            xmlWriter.WriteEndElement();
        }
        private static void SerializeWithReflection(object obj, string name, string info, bool writeElement)
        {
            if (obj == null) return;

            Type type = obj.GetType();

            if (writeElement)
            {
                xmlWriter.WriteStartElement(info);
                xmlWriter.WriteAttributeString("Name", name);
                xmlWriter.WriteAttributeString("Type", type.AssemblyQualifiedName);
            }

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo[] dataFields = fields.Where(f => f.GetCustomAttribute(typeof(Utils.DontSerializeAttribute)) == null).ToArray();

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo[] dataProperties = properties.Where(p => p.GetCustomAttribute(typeof(Utils.DontSerializeAttribute)) == null
                && p.GetGetMethod(true) != null && p.GetSetMethod(true) != null).ToArray();

            for (int i = 0; i < dataFields.Length; i++)
            {
                FieldInfo field = dataFields[i];
                object val = field.GetValue(obj);

                SerializeByType(val, field.Name, "Field", field.FieldType);
            }

            for (int i = 0; i < dataProperties.Length; i++)
            {
                PropertyInfo property = dataProperties[i];
                object val = property.GetValue(obj);

                SerializeByType(val, property.Name, "Property", property.PropertyType);
            }

            if (writeElement) xmlWriter.WriteEndElement();
        }

        public static object Deserialize(string path)
        {
            if (!File.Exists(path)) return null;
            return Deserialize(File.Open(path, FileMode.OpenOrCreate));
        }
        public static object Deserialize(Stream stream)
        {
            loadedReferences.Clear();

            foreach (Delegate d in referencesLoaded.GetInvocationList())
            {
                referencesLoaded -= (Action)d;
            }

            xmlReader = XmlReader.Create(stream, new()
            {
                CloseInput = true,
                IgnoreWhitespace = true,
            });

            xmlReader.Read();
            xmlReader.Read();

            object obj = DeserializeByType();

            for (int i = 0; i < loadedReferences.Count; i++)
            {
                xmlReader.ReadToFollowing("Reference");
                DeserializeWithReflection(loadedReferences[i].Item2);
            }

            referencesLoaded?.Invoke();
            xmlReader.Close();

            return obj;
        }
        public static object DeserializeByType()
        {
            xmlReader.Read();
            while (xmlReader.NodeType != XmlNodeType.Element)
            {
                xmlReader.Read();
            }

            string name = xmlReader.GetAttribute("Name");
            Type type = Type.GetType(xmlReader.GetAttribute("Type"));
            string elementType = xmlReader.GetAttribute("ElementType");
            string length = xmlReader.GetAttribute("Length");

            if (type == typeof(string))
            {
                while (xmlReader.NodeType != XmlNodeType.Text)
                {
                    xmlReader.Read();
                }

                return xmlReader.ReadContentAsString();
            }
            else if (Utils.GetRootType(type) == typeof(Array))
            {
                Array array = Array.CreateInstance(Type.GetType(elementType), int.Parse(length));

                for (int i = 0; i < array.Length; i++)
                {
                    array.SetValue(DeserializeByType(), i);
                }

                return array;
            }
            else if (type.GetInterface("IDictionary") != null)
            {
                IDictionary dictionary = (IDictionary)Activator.CreateInstance(type);

                Array keys = (Array)DeserializeByType();
                Array values = (Array)DeserializeByType();

                for (int i = 0; i < keys.Length; i++)
                {
                    dictionary.Add(keys.GetValue(i), values.GetValue(i));
                }

                return dictionary;
            }
            else if (type.GetInterface("IList") != null)
            {
                IList list = (IList)Activator.CreateInstance(type);
                int size = int.Parse(length);

                for (int i = 0; i < size; i++)
                {
                    object item = DeserializeByType();
                    list.Add(item);
                }

                return list;
            }
            else if (Utils.GetRootType(type) == typeof(HawkObject))
            {
                while (xmlReader.NodeType != XmlNodeType.Text)
                {
                    xmlReader.Read();
                }

                string content = xmlReader.ReadContentAsString();
                if (content == "null") return null;

                ulong id = ulong.Parse(content);
                if (!loadedReferences.Any(o => o.Item1.engineID == id))
                {
                    HawkObject obj = HawkObject.CreateContract(id, type);
                    loadedReferences.Add((obj, type));

                    referencesLoaded += () => { HawkObject.ResolveContract(ref obj); };
                    return obj;
                }
                else return loadedReferences.Find(o => o.Item1.engineID == id).Item1;
            }
            else if (type.GetInterface("IParsable`1") != null)
            {
                while (xmlReader.NodeType != XmlNodeType.Text)
                {
                    xmlReader.Read();
                }

                MethodInfo parseMethod = type.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, new Type[1] { typeof(string) });
                return parseMethod.Invoke(null, new object[1] { xmlReader.ReadContentAsString() });
            }
            else if (type == typeof(bool))
            {
                while (xmlReader.NodeType != XmlNodeType.Text)
                {
                    xmlReader.Read();
                }

                return bool.Parse(xmlReader.ReadContentAsString());
            }
            else if ((type.Namespace == "System.Numerics" || type.Namespace == "Silk.NET.Maths")
                && (type.Name.Contains("Vector") || type.Name.Contains("Quaternion")))
            {
                object vec = Activator.CreateInstance(type);

                FieldInfo[] fields = new FieldInfo[4]
                {
                    type.GetField("X"),
                    type.GetField("Y"),
                    type.GetField("Z"),
                    type.GetField("W"),
                };

                for (int i = 0; i < 4; i++)
                {
                    if (fields[i] == null) break;
                    fields[i].SetValue(vec, DeserializeByType());
                }

                return vec;
            }
            else if (type.IsEnum)
            {
                while (xmlReader.NodeType != XmlNodeType.Text)
                {
                    xmlReader.Read();
                }

                return Enum.Parse(type, xmlReader.ReadContentAsString());
            }
            else
            {
                return DeserializeWithReflection(type);
            }
        }
        private static object DeserializeWithReflection(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo[] dataFields = fields.Where(f => f.GetCustomAttribute(typeof(Utils.DontSerializeAttribute)) == null).ToArray();

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo[] dataProperties = properties.Where(p => p.GetCustomAttribute(typeof(Utils.DontSerializeAttribute)) == null
                && p.GetMethod != null && p.SetMethod != null).ToArray();

            object obj = Activator.CreateInstance(type);

            for (int i = 0; i < dataFields.Length; i++)
            {
                FieldInfo field = dataFields[i];
                object val = DeserializeByType();

                field.SetValue(obj, val);
            }

            if (type == typeof(Transform)) { }

            for (int i = 0; i < dataProperties.Length; i++)
            {
                PropertyInfo property = dataProperties[i];
                object val = DeserializeByType();

                property.SetValue(obj, val);
            }

            type.GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic, Array.Empty<Type>())?.Invoke(obj, null);

            return obj;
        }
    }
}

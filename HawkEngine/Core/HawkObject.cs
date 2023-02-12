using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using System.Threading.Tasks;

namespace HawkEngine.Core
{
    public class HawkObject
    {
        public static readonly List<HawkObject> allObjects = new();

        public ulong engineID { get; protected set; }
        public string name;

        protected HawkObject()
        {

        }
        public HawkObject(string name)
        {
            this.name = name;
            engineID = (ulong)Random.Shared.NextInt64();
            allObjects.Add(this);
        }
        public HawkObject(string name, ulong engineID)
        {
            this.name = name;
            this.engineID = engineID;
        }
        protected virtual void Create()
        {
            allObjects.Add(this);
        }
        public static implicit operator ulong(HawkObject obj)
        {
            return obj.engineID;
        }
        public static implicit operator HawkObject(ulong id)
        {
            return GetWithID(id);
        }
        public override string ToString()
        {
            return name;
        }
        public static bool TryGetWithID(ulong id, out HawkObject obj)
        {
            obj = GetWithID(id);
            return obj != null;
        }
        public static HawkObject GetWithID(ulong id)
        {
            return allObjects.FirstOrDefault(o => o.engineID == id);
        }
        public static HawkObject CreateContract(ulong id, Type type)
        {
            object obj = Activator.CreateInstance(type);
            Utils.SetPropertyWithReflection(obj, "engineID", id);
            return (HawkObject)obj;
        }
        public static void ResolveContract(ref HawkObject obj)
        {
            obj = GetWithID(obj.engineID);
        }
    }
}

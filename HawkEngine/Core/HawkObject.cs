using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Core
{
    public class HawkObject
    {
        public static readonly List<HawkObject> allObjects = new();

        public ulong engineID { get; protected set; }
        public string name;

        public HawkObject(string name)
        {
            this.name = name;
            engineID = (ulong)Random.Shared.NextInt64();
            allObjects.Add(this);
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Core
{
    public class HawkObject
    {
        public ulong engineID { get; protected set; }

        protected void GenRandomID()
        {
            engineID = (ulong)Random.Shared.NextInt64();
        }
    }
}

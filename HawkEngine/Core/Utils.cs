using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Core
{
    public static class Utils
    {
        public static T[] UniformArray<T>(T value, int length)
        {
            T[] array = new T[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = value;
            }
            return array;
        }

        [Flags]
        public enum ShaderFeature : ushort
        {
            None = 0x0000000000000000,
            Lighting = 0x0000000000000001,
            Shadows = 0x0000000000000010,
            Transparency = 0x0000000000000100,
        }
    }
}

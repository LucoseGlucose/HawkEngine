#if DEBUG
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Editor
{
    public static class EditorUtils
    {
        public static Vector4D<float> IDToColor(ulong id)
        {
            ulong first16 = (id & 0x000000000000FFFF) >> 0;
            ulong second16 = (id & 0x00000000FFFF0000) >> 16;
            ulong third16 = (id & 0x0000FFFF00000000) >> 32;
            ulong fourth16 = (id & 0xFFFF000000000000) >> 48;

            return new(first16, second16, third16, fourth16);
        }
        public static ulong ColorToID(Vector4D<float> color)
        {
            ulong first16 = (ulong)color.X;
            ulong second16 = (ulong)color.Y;
            ulong third16 = (ulong)color.Z;
            ulong fourth16 = (ulong)color.W;

            return first16 | (second16 << 16) | (third16 << 32) | (fourth16 << 48);
        }
    }
}
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Core
{
    public static class Conversions
    {
        public delegate TTo ConvertFunc<TFrom, TTo>(TFrom from);
        public static TTo[] ConvertArray<TFrom, TTo>(TFrom[] froms, ConvertFunc<TFrom, TTo> func)
        {
            TTo[] tos = new TTo[froms.Length];
            for (int i = 0; i < froms.Length; i++)
            {
                tos[i] = func(froms[i]);
            }
            return tos;
        }
        public unsafe static TTo[] ConvertPointer<TFrom, TTo>(TFrom* froms, uint count, ConvertFunc<TFrom, TTo> func)
            where TFrom : unmanaged where TTo : unmanaged
        {
            TTo[] tos = new TTo[count];
            for (int i = 0; i < count; i++)
            {
                tos[i] = func(froms[i]);
            }
            return tos;
        }
        public static TTo[] ExpandArray<TFrom, TTo>(TFrom[] froms, ConvertFunc<TFrom, TTo[]> func)
        {
            List<TTo> tos = new();
            for (int i = 0; i < froms.Length; i++)
            {
                tos.AddRange(func(froms[i]));
            }
            return tos.ToArray();
        }
    }
}

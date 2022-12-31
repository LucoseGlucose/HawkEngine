using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Maths;

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
        public delegate TReturn IndexedFunc<TReturn, TIn>(TIn tIn);
        public static T[] IndexedArray<T>(IndexedFunc<T, int> func, int length)
        {
            T[] array = new T[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = func(i);
            }
            return array;
        }
        public enum TonemappingMode
        {
            None,
            Aces,
            Filmic,
            Lottes,
            Reinhard,
            Uchimura,
            Uncharted,
        }
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}

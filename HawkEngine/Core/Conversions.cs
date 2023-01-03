using Silk.NET.Maths;
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

        public const float degToRad = MathF.PI / 180f;
        public const float radToDeg = 180f / MathF.PI;

        public static Quaternion<float> ToQuaternion(this Vector3D<float> eulerAngles)
        {
            eulerAngles *= degToRad;
            return Quaternion<float>.CreateFromYawPitchRoll(eulerAngles.Y, eulerAngles.X, eulerAngles.Z);
        }
        public static Vector3D<float> ToEulerAngles(this Quaternion<float> quat)
        {
            float yaw = (float)Math.Atan2(2 * quat.Y * quat.W - 2 * quat.X * quat.Z, 1 - 2 * quat.Y * quat.Y - 2 * quat.Z * quat.Z);
            float pitch = (float)Math.Asin(2 * quat.X * quat.Y + 2 * quat.Z * quat.W);
            float roll = (float)Math.Atan2(2 * quat.X * quat.W - 2 * quat.Y * quat.Z, 1 - 2 * quat.X * quat.X - 2 * quat.Z * quat.Z);

            return new Vector3D<float>(pitch, yaw, roll) * radToDeg;
        }
        public static Quaternion<float> AsQuaternion(Vector3D<float> angles)
        {
            return angles.ToQuaternion();
        }
        public static Vector3D<float> AsEulerAngles(Quaternion<float> q)
        {
            return q.ToEulerAngles();
        }

        public static Matrix4X4<T> Inverse<T>(this Matrix4X4<T> m) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
        {
            if (!Matrix4X4.Invert(m, out Matrix4X4<T> inverse)) return m;
            return inverse;
        }
    }
}

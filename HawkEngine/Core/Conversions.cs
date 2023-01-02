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

        public static Quaternion<float> ToQuaternion(this Vector3D<float> angles)
        {
            Vector3D<float> v = degToRad * angles;

            float cy = Scalar.Cos(v.Z * 0.5f);
            float sy = Scalar.Sin(v.Z * 0.5f);
            float cp = Scalar.Cos(v.Y * 0.5f);
            float sp = Scalar.Sin(v.Y * 0.5f);
            float cr = Scalar.Cos(v.X * 0.5f);
            float sr = Scalar.Sin(v.X * 0.5f);

            return new Quaternion<float>
            {
                W = (cr * cp * cy + sr * sp * sy),
                X = (sr * cp * cy - cr * sp * sy),
                Y = (cr * sp * cy + sr * cp * sy),
                Z = (cr * cp * sy - sr * sp * cy)
            };
        }
        public static Vector3D<float> ToEulerAngles(this Quaternion<float> q)
        {
            Vector3D<float> angles = new();

            double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = (float)Scalar.Atan2(sinr_cosp, cosr_cosp);

            double sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Scalar.Abs(sinp) >= 1)
            {
                angles.Y = (float)Math.CopySign(Math.PI / 2, sinp);
            }
            else
            {
                angles.Y = (float)Scalar.Asin(sinp);
            }

            double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = (float)Scalar.Atan2(siny_cosp, cosy_cosp);

            return radToDeg * angles;
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

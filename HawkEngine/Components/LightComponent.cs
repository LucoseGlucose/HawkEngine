using HawkEngine.Core;
using HawkEngine.Graphics;
using System;
using System.Collections.Generic;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace HawkEngine.Components
{
    public class LightComponent : Component
    {
        public Vector3D<float> color = Vector3D<float>.One;
        public float strength = 1f;
        public virtual int type { get; }
        public virtual Vector3D<float> positionUniform { get; }

        public Vector3D<float> output { get { return strength * color; } }
        public virtual Vector2D<float> falloff { get; set; }
    }
}

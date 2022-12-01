using HawkEngine.Core;
using HawkEngine.Graphics;
using System;
using System.Collections.Generic;
using Silk.NET.Maths;

namespace HawkEngine.Components
{
    public class LightComponent : Component
    {
        public LightType type = LightType.Point;
        public Vector3D<float> color = Vector3D<float>.One;
        public float strength = 1f;
        public Vector3D<float> output { get { return strength * color; } }
        public Vector2D<float> falloff = new(.09f, .032f);
    }
}

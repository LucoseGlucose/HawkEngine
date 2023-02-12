using HawkEngine.Core;
using HawkEngine.Graphics;
using System;
using System.Collections.Generic;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Xml.Serialization;

namespace HawkEngine.Components
{
    public abstract class LightComponent : Component
    {
        public Vector3D<float> color = Vector3D<float>.One;
        public float strength = 1f;
        public abstract int type { get; }

        public bool shadowsEnabled = true;
        public int shadowResolution;
        public float shadowDistance;

        [Utils.DontSerialize] public Graphics.Framebuffer shadowMapBuffer { get; protected set; }
        protected Graphics.Shader shadowFragmentShader;

        public Vector2D<float> shadowNormalBias;
        public int shadowMapSamples;
        public float shadowSoftness;
        public float shadowNoise;

        public Vector3D<float> output { get { return strength * color; } }
        public virtual Vector2D<float> falloff { get; set; }

        public abstract void CreateShadowBuffer();
        public abstract void SetUniforms(string prefix, ShaderProgram shader);
        public abstract void RenderShadowMap(List<MeshComponent> meshes);
    }
}

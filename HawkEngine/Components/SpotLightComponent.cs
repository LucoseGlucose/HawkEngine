using HawkEngine.Core;
using HawkEngine.Graphics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Components
{
    public class SpotLightComponent : LightComponent
    {
        public override Vector2D<float> falloff { get; set; } = new(.09f, .032f);
        public override int type => 3;
        public override bool supportsShadows => true;

        public bool shadowsEnabled = true;
        public int shadowResolution = 1024;
        public float shadowDistance = 75f;
        public Graphics.Framebuffer shadowMapBuffer { get; protected set; }

        public Vector2D<float> shadowNormalBias = new(.000002f, .000008f);
        public int shadowMapSamples = 1;
        public float shadowSoftness = 1f;
        public float shadowNoise = 1000f;

        public float nearClip = .01f;
        public Vector2D<float> angles = new(45f, 60f);

        public Matrix4X4<float> projectionMat
        {
            get { return Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(90f), 1f, nearClip, shadowDistance); }
        }
        public Matrix4X4<float> viewMat { get { return Matrix4X4.CreateLookAt(transform.position, transform.position + transform.forward, transform.up); } }

        public override void Create(SceneObject owner)
        {
            base.Create(owner);

            CreateShadowBuffer();
        }
        public void CreateShadowBuffer()
        {
            FramebufferTexture tex = new((uint)shadowResolution, (uint)shadowResolution, FramebufferAttachment.DepthAttachment,
                InternalFormat.DepthComponent24, PixelFormat.DepthComponent, wrap: GLEnum.ClampToBorder);
            Span<float> col = stackalloc float[4] { 1f, 1f, 1f, 1f };

            tex.Bind(0);
            Rendering.gl.TexParameter(tex.textureType, TextureParameterName.TextureBorderColor, col);
            shadowMapBuffer = new(tex);
        }
        public override void SetUniforms(string prefix, ShaderProgram shader)
        {
            shader.SetIntCache($"{prefix}.uType", type);
            shader.SetVec3Cache($"{prefix}.uColor", output);
            shader.SetVec2Cache($"{prefix}.uFalloff", falloff);
            shader.SetVec3Cache($"{prefix}.uPosition", transform.position);

            shader.SetVec3Cache($"{prefix}.uDirection", transform.forward);
            shader.SetVec2Cache($"{prefix}.uRadius", new(Scalar.Cos(Scalar.DegreesToRadians(angles.X)), Scalar.Cos(Scalar.DegreesToRadians(angles.Y))));

            if (shadowsEnabled)
            {
                shader.SetTexture($"{prefix}.uShadowTexW", shadowMapBuffer[FramebufferAttachment.DepthAttachment]);
                shader.SetMat4Cache($"{prefix}.uShadowMat", viewMat * projectionMat);

                shader.SetVec2Cache($"{prefix}.uShadowNormalBias", shadowNormalBias);
                shader.SetIntCache($"{prefix}.uShadowMapSamples", shadowMapSamples);
                shader.SetFloatCache($"{prefix}.uShadowSoftness", shadowSoftness);
                shader.SetFloatCache($"{prefix}.uShadowNoise", shadowNoise);
            }
            else shader.SetIntCache($"{prefix}.uShadowMapSamples", -1);
        }
    }
}

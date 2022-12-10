using HawkEngine.Core;
using HawkEngine.Graphics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Components
{
    public class DirectionalLightComponent : LightComponent
    {
        public override Vector2D<float> falloff { get => new(0f); }
        public override int type => 0;
        public override Vector3D<float> positionUniform => transform.forward;

        public bool shadowsEnabled = true;
        public Vector2D<int> shadowResolution = new(1024);
        public Graphics.Framebuffer shadowMapBuffer { get; protected set; }

        public float nearClip = .1f;
        public float farClip = 50f;
        public Vector2D<float> size = new(30f);

        public Matrix4X4<float> projectionMat
        {
            get { return Matrix4X4.CreateOrthographic(size.X, size.Y, nearClip, farClip); }
        }
        public Matrix4X4<float> viewMat { get { return Matrix4X4.CreateLookAt(transform.position, new(0f), transform.up); } }

        public override void Create(SceneObject owner)
        {
            base.Create(owner);

            CreateShadowBuffer();
        }
        public void CreateShadowBuffer()
        {
            FramebufferTexture tex = new FramebufferTexture((uint)shadowResolution.X, (uint)shadowResolution.Y, FramebufferAttachment.DepthAttachment,
                InternalFormat.DepthComponent16, PixelFormat.DepthComponent, wrap: GLEnum.ClampToBorder);
            Span<float> col = stackalloc float[4] { 1f, 1f, 1f, 1f };

            tex.Bind(0);
            Rendering.gl.TexParameter(tex.textureType, TextureParameterName.TextureBorderColor, col);
            shadowMapBuffer = new(tex);
        }
    }
}

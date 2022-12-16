using System;
using System.Collections.Generic;
using Silk.NET.Maths;
using HawkEngine.Core;
using HawkEngine.Graphics;
using Silk.NET.OpenGL;

namespace HawkEngine.Components
{
    public class CameraComponent : Component
    {
        public bool matchScreen = true;
        public float fov = 60f;
        public float nearClip = .1f;
        public float farClip = 100f;
        public Vector2D<int> size;

        public uint multisamples
        {
            get
            {
                FramebufferTexture tex = framebuffer.attachments[0];
                if (tex.textureType != TextureTarget.Texture2DMultisample) return 1;
                Rendering.gl.GetTextureLevelParameter(tex.id, 0, GLEnum.TextureSamples, out int samples);
                return Scalar.Max((uint)samples, 1u);
            }
            set
            {
                CreateFramebuffer(Scalar.Max(value, 1u));
            }
        }

        public Graphics.Framebuffer framebuffer { get; protected set; }

        public Matrix4X4<float> projectionMat
        {
            get { return Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(fov), (float)size.X / size.Y, nearClip, farClip); }
        }
        public Matrix4X4<float> viewMat { get { return Matrix4X4.CreateLookAt(transform.position, transform.position + transform.forward, transform.up); } }

        public override void Create(SceneObject owner)
        {
            base.Create(owner);

            size = App.window.FramebufferSize;
            CreateFramebuffer(4);

            App.window.FramebufferResize += (v2) =>
            {
                if (matchScreen)
                {
                    size = v2;
                    CreateFramebuffer(multisamples);
                }
            };
        }
        public void CreateFramebuffer(uint samples = 1)
        {
            framebuffer = new
                (
                    new FramebufferTexture((uint)size.X, (uint)size.Y, 
                        FramebufferAttachment.ColorAttachment0, InternalFormat.Rgba16f, PixelFormat.Rgba, samples),
                    new((uint)size.X, (uint)size.Y, FramebufferAttachment.DepthAttachment,
                        InternalFormat.DepthComponent24, PixelFormat.DepthComponent, samples)
                );
        }
    }
}

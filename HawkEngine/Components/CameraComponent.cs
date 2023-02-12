using System;
using System.Collections.Generic;
using Silk.NET.Maths;
using HawkEngine.Core;
using HawkEngine.Graphics;
using Silk.NET.OpenGL;
using System.Xml.Serialization;

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
                Graphics.Texture tex = framebuffer[FramebufferAttachment.ColorAttachment0];
                if (tex.textureType != TextureTarget.Texture2DMultisample) return 1;
                Rendering.gl.GetTextureLevelParameter(tex.glID, 0, GLEnum.TextureSamples, out int samples);
                return Scalar.Max((uint)samples, 1u);
            }
            set
            {
                CreateFramebuffer(Scalar.Max(value, 1u));
            }
        }

        [Utils.DontSerialize]
        [field: Utils.DontSerialize]
        public Graphics.Framebuffer framebuffer { get; protected set; }

        public Matrix4X4<float> projectionMat
        {
            get { return Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(fov), (float)size.X / size.Y, nearClip, farClip); }
        }
        public Matrix4X4<float> viewMat { get { return Matrix4X4.CreateLookAt(transform.position, transform.position + transform.forward, transform.up); } }

        public override void Create(SceneObject owner)
        {
            base.Create(owner);

#if !DEBUG

            size = App.window.FramebufferSize;
            CreateFramebuffer(4);
#else
            System.Numerics.Vector2 s = Editor.EditorGUI.FindWindow<Editor.EditorViewport>().size;
            size = new((int)s.X, (int)s.Y);
            CreateFramebuffer(4);
#endif

#if DEBUG
            Editor.EditorGUI.FindWindow<Editor.EditorViewport>().sizeChangeEnd += (v2) =>
            {
                if (matchScreen || Rendering.outputCam == this)
                {
                    size = new((int)v2.X, (int)v2.Y);
                    CreateFramebuffer(multisamples);
                }
            };
#else
            App.window.FramebufferResize += (v2) =>
            {
                if (matchScreen || Rendering.outputCam == this)
                {
                    size = v2;
                    CreateFramebuffer(multisamples);
                }
            };
#endif
        }
        public void CreateFramebuffer(uint samples = 1)
        {
            if (size == Vector2D<int>.Zero) return;

            framebuffer = new
            (
                new FramebufferTexture(new Texture2D((uint)size.X, (uint)size.Y, InternalFormat.Rgba16f, PixelFormat.Rgba, samples),
                    FramebufferAttachment.ColorAttachment0),
                new FramebufferTexture(new Texture2D((uint)size.X, (uint)size.Y, InternalFormat.Depth24Stencil8, PixelFormat.DepthStencil,
                    samples, GLEnum.Nearest), FramebufferAttachment.DepthStencilAttachment)
            );
        }
    }
}

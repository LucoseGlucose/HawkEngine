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
            /*get
            {
                float ar = (float)App.window.FramebufferSize.X / App.window.FramebufferSize.Y;
                float fov = Scalar.DegreesToRadians(Rendering.outputCam.fov);
                float Hnear = 2 * Scalar.Tan(fov / 2) * nearClip;
                float Wnear = Hnear * ar;
                float Hfar = 2 * Scalar.Tan(fov / 2) * farClip;
                float Wfar = Hfar * ar;
                Vector3D<float> centerFar = Rendering.outputCam.transform.position + Rendering.outputCam.transform.forward * farClip;

                Vector3D<float> topLeftFar = centerFar + (Rendering.outputCam.transform.up * Hfar / 2) - (Rendering.outputCam.transform.right * Wfar / 2);
                Vector3D<float> topRightFar = centerFar + (Rendering.outputCam.transform.up * Hfar / 2) + (Rendering.outputCam.transform.right * Wfar / 2);
                Vector3D<float> bottomLeftFar = centerFar - (Rendering.outputCam.transform.up * Hfar / 2) - (Rendering.outputCam.transform.right * Wfar / 2);
                Vector3D<float> bottomRightFar = centerFar - (Rendering.outputCam.transform.up * Hfar / 2) + (Rendering.outputCam.transform.right * Wfar / 2);
                Vector3D<float> centerNear = Rendering.outputCam.transform.position + Rendering.outputCam.transform.forward * nearClip;

                Vector3D<float> topLeftNear = centerNear + ((Rendering.outputCam.transform.up * Hnear / 2)
                    - (Rendering.outputCam.transform.right * Wnear / 2));
                Vector3D<float> topRightNear = centerNear + ((Rendering.outputCam.transform.up * Hnear / 2)
                    + (Rendering.outputCam.transform.right * Wnear / 2));
                Vector3D<float> bottomLeftNear = centerNear - ((Rendering.outputCam.transform.up * Hnear / 2)
                    - (Rendering.outputCam.transform.right * Wnear / 2));
                Vector3D<float> bottomRightNear = centerNear - ((Rendering.outputCam.transform.up * Hnear / 2)
                    + (Rendering.outputCam.transform.right * Wnear / 2));

                Vector4D<float>[] frustumToLightView = new Vector4D<float>[8]
                {
                    Vector4D.Transform(new Vector4D<float>(bottomRightNear, 1.0f), viewMat),
                    Vector4D.Transform(new Vector4D<float>(topRightNear, 1.0f), viewMat),
                    Vector4D.Transform(new Vector4D<float>(bottomLeftNear, 1.0f), viewMat),
                    Vector4D.Transform(new Vector4D<float>(topLeftNear, 1.0f), viewMat),
                    Vector4D.Transform(new Vector4D<float>(bottomRightFar, 1.0f), viewMat),
                    Vector4D.Transform(new Vector4D<float>(topRightFar, 1.0f), viewMat),
                    Vector4D.Transform(new Vector4D<float>(bottomLeftFar, 1.0f), viewMat),
                    Vector4D.Transform(new Vector4D<float>(topLeftFar, 1.0f), viewMat),
                };

                Vector3D<float> max = new(float.MaxValue);
                Vector3D<float> min = new(float.MinValue);

                for (uint i = 0; i < frustumToLightView.Length; i++)
                {
                    if (frustumToLightView[i].X < min.X) min.X = frustumToLightView[i].X;
                    if (frustumToLightView[i].Y < min.Y) min.Y = frustumToLightView[i].Y;
                    if (frustumToLightView[i].Z < min.Z) min.Z = frustumToLightView[i].Z;

                    if (frustumToLightView[i].X > max.X) max.X = frustumToLightView[i].X;
                    if (frustumToLightView[i].Y > max.Y) max.Y = frustumToLightView[i].Y;
                    if (frustumToLightView[i].Z > max.Z) max.Z = frustumToLightView[i].Z;
                }

                float l = min.X;
                float r = max.X;
                float b = min.Y;
                float t = max.Y;
                float n = -max.Z;
                float f = -min.Z;

                return Matrix4X4.CreateOrthographicOffCenter(l, r, b, t, n, f);
            }*/
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

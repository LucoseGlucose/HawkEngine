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
        public override int type => 1;

        public Matrix4X4<float> projectionMat
        {
            get
            {
                CameraComponent cam = Rendering.outputCam;

                float ar = (float)App.window.FramebufferSize.X / App.window.FramebufferSize.Y;
                float fov = Scalar.DegreesToRadians(Rendering.outputCam.fov);
                float Hnear = 2f * Scalar.Tan(fov * .5f) * cam.nearClip;
                float Wnear = Hnear * ar;
                float Hfar = 2f * Scalar.Tan(fov * .5f) * shadowDistance;
                float Wfar = Hfar * ar;
                Vector3D<float> centerFar = cam.transform.position + cam.transform.forward * shadowDistance;

                Vector3D<float> tlf = centerFar + (Rendering.outputCam.transform.up * Hfar * .5f) - (Rendering.outputCam.transform.right * Wfar * .5f);
                Vector3D<float> trf = centerFar + (Rendering.outputCam.transform.up * Hfar * .5f) + (Rendering.outputCam.transform.right * Wfar * .5f);
                Vector3D<float> blf = centerFar - (Rendering.outputCam.transform.up * Hfar * .5f) - (Rendering.outputCam.transform.right * Wfar * .5f);
                Vector3D<float> brf = centerFar - (Rendering.outputCam.transform.up * Hfar * .5f) + (Rendering.outputCam.transform.right * Wfar * .5f);
                Vector3D<float> centerNear = cam.transform.position + cam.transform.forward * cam.nearClip;

                Vector3D<float> tln = centerNear + ((cam.transform.up * Hnear / 2) - (cam.transform.right * Wnear / 2));
                Vector3D<float> trn = centerNear + ((cam.transform.up * Hnear / 2) + (cam.transform.right * Wnear / 2));
                Vector3D<float> bln = centerNear - ((cam.transform.up * Hnear / 2) - (cam.transform.right * Wnear / 2));
                Vector3D<float> brn = centerNear - ((cam.transform.up * Hnear / 2) + (cam.transform.right * Wnear / 2));

                Vector4D<float>[] frustumToLightView = new Vector4D<float>[8]
                {
                    new Vector4D<float>(brn, 1.0f) * viewMat,
                    new Vector4D<float>(trn, 1.0f) * viewMat,
                    new Vector4D<float>(bln, 1.0f) * viewMat,
                    new Vector4D<float>(tln, 1.0f) * viewMat,
                    new Vector4D<float>(brf, 1.0f) * viewMat,
                    new Vector4D<float>(trf, 1.0f) * viewMat,
                    new Vector4D<float>(blf, 1.0f) * viewMat,
                    new Vector4D<float>(tlf, 1.0f) * viewMat,
                };

                Vector3D<float> max = new(float.MinValue);
                Vector3D<float> min = new(float.MaxValue);

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
            }
        }
        public Matrix4X4<float> viewMat { get { return Matrix4X4.CreateLookAt(-transform.forward, new(0f), transform.up); } }

        public override void Create(SceneObject owner)
        {
            base.Create(owner);

            shadowDistance = 50f;
            shadowResolution = 2048;
            shadowNormalBias = new(.002f, .008f);
            shadowMapSamples = 2;
            shadowSoftness = .65f;
            shadowNoise = 8000f;

            shadowShader = new("Shaders/Shadows/ShadowVert.glsl", "Shaders/EmptyFrag.glsl");

            CreateShadowBuffer();
        }
        public override void CreateShadowBuffer()
        {
            FramebufferTexture tex = new(new Texture2D((uint)shadowResolution, (uint)shadowResolution,
                InternalFormat.DepthComponent24, PixelFormat.DepthComponent, wrap: GLEnum.ClampToBorder), FramebufferAttachment.DepthAttachment);
            Span<float> col = stackalloc float[4] { 1f, 1f, 1f, 1f };

            tex.texture.Bind(0);
            Rendering.gl.TexParameter(tex.texture.textureType, TextureParameterName.TextureBorderColor, col);
            shadowMapBuffer = new(tex);
        }
        public override void SetUniforms(string prefix, ShaderProgram shader)
        {
            shader.SetIntCache($"{prefix}.uType", type);
            shader.SetVec3Cache($"{prefix}.uColor", output);
            shader.SetVec3Cache($"{prefix}.uDirection", transform.forward);

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
        public override unsafe void RenderShadowMap(List<MeshComponent> meshes)
        {
            shadowMapBuffer.Bind();
            Rendering.gl.DrawBuffer(DrawBufferMode.None);
            Rendering.gl.Viewport(new Vector2D<int>(shadowResolution));
            Rendering.gl.Clear(ClearBufferMask.DepthBufferBit);

            for (int m = 0; m < meshes.Count; m++)
            {
                if (!meshes[m].castShadows) continue;

                shadowShader.SetMat4Cache("uMat", meshes[m].transform.matrix * viewMat * projectionMat);
                meshes[m].mesh.vertexArray.Bind();
                Rendering.gl.DrawElements(PrimitiveType.Triangles, (uint)meshes[m].mesh.meshData.indices.Length, DrawElementsType.UnsignedInt, null);
            }
        }
    }
}

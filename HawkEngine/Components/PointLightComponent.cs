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
    public class PointLightComponent : LightComponent
    {
        public override Vector2D<float> falloff { get; set; } = new(.09f, .032f);
        public override int type => 2;
        public float nearClip = .01f;

        public Matrix4X4<float> projectionMat
        {
            get { return Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(90f), 1f, nearClip, shadowDistance); }
        }
        public Matrix4X4<float>[] viewMats
        {
            get
            {
                return new Matrix4X4<float>[6]
                {
                    Matrix4X4.CreateLookAt(transform.position, transform.position + Vector3D<float>.UnitX, -Vector3D<float>.UnitY),
                    Matrix4X4.CreateLookAt(transform.position, transform.position - Vector3D<float>.UnitX, -Vector3D<float>.UnitY),
                    Matrix4X4.CreateLookAt(transform.position, transform.position + Vector3D<float>.UnitY, Vector3D<float>.UnitZ),
                    Matrix4X4.CreateLookAt(transform.position, transform.position - Vector3D<float>.UnitY, -Vector3D<float>.UnitZ),
                    Matrix4X4.CreateLookAt(transform.position, transform.position + Vector3D<float>.UnitZ, -Vector3D<float>.UnitY),
                    Matrix4X4.CreateLookAt(transform.position, transform.position - Vector3D<float>.UnitZ, -Vector3D<float>.UnitY),
                };
            }
        }

        public override void Create(SceneObject owner)
        {
            base.Create(owner);

            shadowDistance = 75f;
            shadowResolution = 2048;
            shadowNormalBias = new(.02f, .08f);

            shadowShader = new("Shaders/Shadows/ShadowVert.glsl", "Shaders/Skybox/CubemapGeom.glsl", "Shaders/Shadows/PointShadowFrag.glsl");

            CreateShadowBuffer();
        }
        public override void CreateShadowBuffer()
        {
            FramebufferTexture tex = new(new TextureCubemap((uint)shadowResolution, (uint)shadowResolution,
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
            shader.SetVec2Cache($"{prefix}.uFalloff", falloff);
            shader.SetVec3Cache($"{prefix}.uPosition", transform.position);

            if (shadowsEnabled)
            {
                shader.SetTexture($"{prefix}.uShadowCubeW", shadowMapBuffer[FramebufferAttachment.DepthAttachment].texture);

                shader.SetVec2Cache($"{prefix}.uShadowNormalBias", shadowNormalBias);
                shader.SetIntCache($"{prefix}.uShadowMapSamples", shadowMapSamples);
                shader.SetFloatCache($"{prefix}.uShadowSoftness", shadowSoftness);
                shader.SetFloatCache($"{prefix}.uShadowNoise", shadowNoise);
                shader.SetFloatCache($"{prefix}.uFarPlane", shadowDistance);
            }
            else shader.SetIntCache($"{prefix}.uShadowMapSamples", -1);
        }
        public override unsafe void RenderShadowMap(List<MeshComponent> meshes)
        {
            shadowMapBuffer.Bind();
            Rendering.gl.DrawBuffer(DrawBufferMode.None);
            Rendering.gl.Viewport(new Vector2D<int>(shadowResolution));
            Rendering.gl.Clear(ClearBufferMask.DepthBufferBit);

            for (int i = 0; i < 6; i++)
            {
                shadowShader.SetMat4Cache($"uMats[{i}].uMat", viewMats[i] * projectionMat);
            }
            shadowShader.SetVec3Cache("uLightPos", transform.position);
            shadowShader.SetFloatCache("uFarPlane", shadowDistance);

            for (int m = 0; m < meshes.Count; m++)
            {
                if (!meshes[m].castShadows) continue;

                shadowShader.SetMat4Cache("uMat", meshes[m].transform.matrix);
                meshes[m].mesh.vertexArray.Bind();
                Rendering.gl.DrawElements(PrimitiveType.Triangles, (uint)meshes[m].mesh.meshData.indices.Length, DrawElementsType.UnsignedInt, null);
            }
        }
    }
}

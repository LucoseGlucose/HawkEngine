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

        public static readonly List<ShaderProgram> shadowShaders = new();
        protected Graphics.Shader shadowGeometryShader;

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

            shadowFragmentShader = new("Shaders/Scene/PointShadowFrag.glsl", ShaderType.FragmentShader);
            shadowGeometryShader = new("Shaders/Skybox/CubemapGeom.glsl", ShaderType.GeometryShader);

            CreateShadowBuffer();
        }
        public override void CreateShadowBuffer()
        {
            FramebufferTexture tex = new(new TextureCubemap((uint)shadowResolution, (uint)shadowResolution,
                InternalFormat.DepthComponent24, PixelFormat.DepthComponent, wrap: GLEnum.ClampToBorder), FramebufferAttachment.DepthAttachment);
            Span<float> col = stackalloc float[4] { 1f, 1f, 1f, 1f };
            Rendering.gl.TextureParameter(tex.texture.glID, TextureParameterName.TextureBorderColor, col);

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
                shader.SetTexture($"{prefix}.uShadowCubeW", shadowMapBuffer[FramebufferAttachment.DepthAttachment]);

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
            Rendering.gl.Viewport(new Vector2D<int>(shadowResolution));
            Rendering.gl.Clear(ClearBufferMask.DepthBufferBit);

            for (int m = 0; m < meshes.Count; m++)
            {
                if (!meshes[m].castShadows) continue;

                ShaderProgram newShader = shadowShaders.FirstOrDefault(ns => ns[ShaderType.VertexShader] == meshes[m].shader[ShaderType.VertexShader]);
                if (newShader == null)
                {
                    newShader = new(meshes[m].shader[ShaderType.VertexShader], shadowGeometryShader, shadowFragmentShader);
                    shadowShaders.Add(newShader);
                }

                newShader.Bind();
                newShader.SetMat4Cache("uMat", meshes[m].transform.matrix);

                for (int i = 0; i < 6; i++)
                {
                    newShader.SetMat4Cache($"uMats[{i}].uMat", viewMats[i] * projectionMat);
                }
                newShader.SetVec3Cache("uLightPos", transform.position);
                newShader.SetFloatCache("uFarPlane", shadowDistance);

                meshes[m].mesh.vertexArray.Bind();
                Rendering.gl.DrawElements(PrimitiveType.Triangles, (uint)meshes[m].mesh.meshData.indices.Length, DrawElementsType.UnsignedInt, null);
            }
        }
    }
}

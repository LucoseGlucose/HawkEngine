﻿using HawkEngine.Core;
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
        public static readonly List<ShaderProgram> shadowShaders = new();

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

            shadowDistance = 75f;
            shadowResolution = 1024;
            shadowNormalBias = new(.000002f, .000008f);
            shadowMapSamples = 1;
            shadowSoftness = 1f;
            shadowNoise = 1000f;

            shadowFragmentShader = new("Shaders/EmptyFrag.glsl", ShaderType.FragmentShader);
            CreateShadowBuffer();
        }
        public override void CreateShadowBuffer()
        {
            FramebufferTexture tex = new(new Texture2D((uint)shadowResolution, (uint)shadowResolution,
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
                    newShader = new(meshes[m].shader[ShaderType.VertexShader], shadowFragmentShader);
                    shadowShaders.Add(newShader);
                }

                newShader.Bind();
                newShader.SetMat4Cache("uMat", meshes[m].transform.matrix * viewMat * projectionMat);

                meshes[m].mesh.vertexArray.Bind();
                Rendering.gl.DrawElements(PrimitiveType.Triangles, (uint)meshes[m].mesh.meshData.indices.Length, DrawElementsType.UnsignedInt, null);
            }
        }
    }
}

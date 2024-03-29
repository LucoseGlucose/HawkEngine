﻿using HawkEngine.Core;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Graphics
{
    public sealed class Skybox
    {
        public readonly TextureCubemap skybox;
        public readonly TextureCubemap irradiance;
        public readonly TextureCubemap specularReflections;

        public static readonly Matrix4X4<float> projMat = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(90f), 1f, .1f, 10f);
        public static readonly Matrix4X4<float>[] viewMats = new Matrix4X4<float>[6]
        {
                Matrix4X4.CreateLookAt(new(0f), Vector3D<float>.UnitX, -Vector3D<float>.UnitY),
                Matrix4X4.CreateLookAt(new(0f), -Vector3D<float>.UnitX, -Vector3D<float>.UnitY),
                Matrix4X4.CreateLookAt(new(0f), Vector3D<float>.UnitY, Vector3D<float>.UnitZ),
                Matrix4X4.CreateLookAt(new(0f), -Vector3D<float>.UnitY, -Vector3D<float>.UnitZ),
                Matrix4X4.CreateLookAt(new(0f), Vector3D<float>.UnitZ, -Vector3D<float>.UnitY),
                Matrix4X4.CreateLookAt(new(0f), -Vector3D<float>.UnitZ, -Vector3D<float>.UnitY),
        };

        public Skybox(string path, uint resolution, uint irradianceResolution, uint reflectionResolution)
            : this(EquirectToCubemap(new(path, hdr: true), resolution), irradianceResolution, reflectionResolution)
        {

        }
        public Skybox(string[] paths, uint irradianceResolution, uint reflectionResolution)
            : this(new TextureCubemap(paths, mipmap: true), irradianceResolution, reflectionResolution)
        {

        }
        public Skybox(TextureCubemap baseCubemap, uint irradianceResolution, uint reflectionResolution)
        {
            skybox = baseCubemap;
            Rendering.gl.TextureParameterI(skybox.glID, TextureParameterName.TextureMinFilter, (uint)GLEnum.LinearMipmapLinear);

            irradiance = ComputeIrradiance(skybox, irradianceResolution);
            specularReflections = ComputeSpecularReflectionMap(skybox, reflectionResolution);
        }
        public static unsafe TextureCubemap EquirectToCubemap(Texture2D tex, uint resolution)
        {
            TextureCubemap cubemap = new(resolution, resolution, InternalFormat.Rgb16f, PixelFormat.Rgb);
            uint fbId = Rendering.gl.GenFramebuffer();
            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbId);

            ShaderProgram shader = new("Shaders/Skybox/RectToCubemapVert.glsl", "Shaders/Skybox/RectToCubemapFrag.glsl");

            Rendering.gl.TextureParameterI(cubemap.glID, TextureParameterName.TextureMinFilter, (uint)GLEnum.LinearMipmapLinear);
            Rendering.gl.Viewport(new Vector2D<int>((int)resolution));

            shader.Bind();
            shader.SetTexture("uTexture", tex);
            shader.BindTextures();

            for (int i = 0; i < 6; i++)
            {
                shader.SetMat4Cache("uMat", viewMats[i] * projMat);
                Rendering.gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                    TextureTarget.TextureCubeMapPositiveX + i, cubemap.glID, 0);

                Rendering.gl.Clear(ClearBufferMask.ColorBufferBit);
                Rendering.skyboxMesh.vertexArray.Bind();
                Rendering.gl.DrawElements(PrimitiveType.Triangles, (uint)Rendering.skyboxMesh.meshData.indices.Length, DrawElementsType.UnsignedInt, null);
            }

            Rendering.gl.GenerateTextureMipmap(cubemap.glID);

            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Rendering.gl.DeleteFramebuffer(fbId);
            return cubemap;
        }
        public static unsafe TextureCubemap ComputeIrradiance(TextureCubemap tex, uint resolution, float sampleDelta = .025f)
        {
            ShaderProgram shader = new("Shaders/Skybox/RectToCubemapVert.glsl", "Shaders/Skybox/IrradianceConvolutionFrag.glsl");

            shader.SetFloatCache("uSampleDelta", sampleDelta);

            TextureCubemap cubemap = new(resolution, resolution, InternalFormat.Rgb16f, PixelFormat.Rgb);
            uint fbId = Rendering.gl.GenFramebuffer();

            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbId);
            Rendering.gl.Viewport(new Vector2D<int>((int)resolution));

            shader.Bind();
            shader.SetTexture("uTexture", tex);
            shader.BindTextures();

            for (int i = 0; i < 6; i++)
            {
                shader.SetMat4Cache("uMat", viewMats[i] * projMat);
                Rendering.gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                    TextureTarget.TextureCubeMapPositiveX + i, cubemap.glID, 0);

                Rendering.gl.Clear(ClearBufferMask.ColorBufferBit);
                Rendering.skyboxMesh.vertexArray.Bind();
                Rendering.gl.DrawElements(PrimitiveType.Triangles, (uint)Rendering.skyboxMesh.meshData.indices.Length, DrawElementsType.UnsignedInt, null);
            }

            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Rendering.gl.DeleteFramebuffer(fbId);
            return cubemap;
        }
        public static unsafe TextureCubemap ComputeSpecularReflectionMap(TextureCubemap tex, uint resolution, int sampleCount = 512)
        {
            TextureCubemap cubemap = new(resolution, resolution, InternalFormat.Rgb16f, PixelFormat.Rgb, mipmap: true);
            ShaderProgram shader = new("Shaders/Skybox/RectToCubemapVert.glsl", "Shaders/Skybox/SpecularReflectionFilter.glsl");

            Rendering.gl.TextureParameterI(cubemap.glID, TextureParameterName.TextureMinFilter, (uint)GLEnum.LinearMipmapLinear);

            uint fbId = Rendering.gl.GenFramebuffer();
            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbId);

            shader.Bind();
            shader.SetTexture("uTexture", tex);
            shader.BindTextures();

            shader.SetIntCache("uSampleCount", sampleCount);

            for (int mip = 0; mip < 5; mip++)
            {
                int mipSize = (int)Scalar.Round(resolution * Scalar.Pow(.5f, mip));
                Rendering.gl.Viewport(new Vector2D<int>(mipSize));

                float roughness = mip / 5f;
                shader.SetFloatCache("uRoughness", roughness);

                for (int i = 0; i < 6; i++)
                {
                    shader.SetMat4Cache("uMat", viewMats[i] * projMat);
                    Rendering.gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                        TextureTarget.TextureCubeMapPositiveX + i, cubemap.glID, mip);

                    Rendering.gl.Clear(ClearBufferMask.ColorBufferBit);
                    Rendering.skyboxMesh.vertexArray.Bind();
                    Rendering.gl.DrawElements(PrimitiveType.Triangles, (uint)Rendering.skyboxMesh.meshData.indices.Length, DrawElementsType.UnsignedInt, null);
                }
            }

            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Rendering.gl.DeleteFramebuffer(fbId);
            return cubemap;
        }
    }
}

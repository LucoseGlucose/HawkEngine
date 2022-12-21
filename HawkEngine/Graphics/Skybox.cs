using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Graphics
{
    public class Skybox
    {
        public readonly TextureCubemap skybox;
        public readonly TextureCubemap irradiance;
        public readonly TextureCubemap specularReflections;

        public Skybox(string path, uint resolution, uint irradianceResolution, uint reflectionResolution)
        {
            skybox = EquirectToCubemap(new(path, hdr: true), resolution);
            irradiance = ComputeIrradiance(skybox, irradianceResolution);
            specularReflections = ComputeSpecularReflectionMap(skybox, reflectionResolution);
        }
        public static unsafe TextureCubemap RenderToCubemap(Texture tex, ShaderProgram shader, uint resolution)
        {
            TextureCubemap cubemap = new(resolution, resolution, InternalFormat.Rgb16f, PixelFormat.Rgb);
            uint fbId = Rendering.gl.GenFramebuffer();
            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbId);

            cubemap.Bind(0);
            Rendering.gl.TexParameterI(cubemap.textureType, TextureParameterName.TextureMinFilter, (uint)GLEnum.LinearMipmapLinear);
            cubemap.Unbind(0);

            Matrix4X4<float> projMat = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(90f), 1f, .1f, 10f);
            Matrix4X4<float>[] viewMats = new Matrix4X4<float>[6]
            {
                Matrix4X4.CreateLookAt(new(0f), Vector3D<float>.UnitX, -Vector3D<float>.UnitY),
                Matrix4X4.CreateLookAt(new(0f), -Vector3D<float>.UnitX, -Vector3D<float>.UnitY),
                Matrix4X4.CreateLookAt(new(0f), Vector3D<float>.UnitY, Vector3D<float>.UnitZ),
                Matrix4X4.CreateLookAt(new(0f), -Vector3D<float>.UnitY, -Vector3D<float>.UnitZ),
                Matrix4X4.CreateLookAt(new(0f), Vector3D<float>.UnitZ, -Vector3D<float>.UnitY),
                Matrix4X4.CreateLookAt(new(0f), -Vector3D<float>.UnitZ, -Vector3D<float>.UnitY),
            };

            Rendering.gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            Rendering.gl.Viewport(new Vector2D<int>((int)resolution));
            shader.Bind();
            shader.SetTexture("uTexture", tex);
            shader.BindTextures();

            for (int i = 0; i < 6; i++)
            {
                shader.SetMat4Cache("uMat", viewMats[i] * projMat);
                Rendering.gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                    TextureTarget.TextureCubeMapPositiveX + i, cubemap.id, 0);

                Rendering.gl.Clear(ClearBufferMask.ColorBufferBit);
                Rendering.skyboxMesh.vertexArray.Bind();
                Rendering.gl.DrawElements(PrimitiveType.Triangles, (uint)Rendering.skyboxMesh.meshData.indices.Length, DrawElementsType.UnsignedInt, null);
            }

            cubemap.Bind(0);
            Rendering.gl.GenerateMipmap(cubemap.textureType);
            cubemap.Unbind(0);

            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Rendering.gl.DeleteFramebuffer(fbId);
            return cubemap;
        }
        public static unsafe TextureCubemap EquirectToCubemap(Texture2D tex, uint resolution)
        {
            TextureCubemap cubemap = new(resolution, resolution, InternalFormat.Rgb16f, PixelFormat.Rgb);
            uint fbId = Rendering.gl.GenFramebuffer();
            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbId);

            ShaderProgram shader = new ShaderProgram(Shader.Create("Shaders/Skybox/RectToCubemapVert.glsl", ShaderType.VertexShader),
                Shader.Create("Shaders/Skybox/RectToCubemapFrag.glsl", ShaderType.FragmentShader));

            cubemap.Bind(0);
            Rendering.gl.TexParameterI(cubemap.textureType, TextureParameterName.TextureMinFilter, (uint)GLEnum.LinearMipmapLinear);
            cubemap.Unbind(0);

            Matrix4X4<float> projMat = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(90f), 1f, .1f, 10f);
            Matrix4X4<float>[] viewMats = new Matrix4X4<float>[6]
            {
                Matrix4X4.CreateLookAt(new(0f), Vector3D<float>.UnitX, -Vector3D<float>.UnitY),
                Matrix4X4.CreateLookAt(new(0f), -Vector3D<float>.UnitX, -Vector3D<float>.UnitY),
                Matrix4X4.CreateLookAt(new(0f), Vector3D<float>.UnitY, Vector3D<float>.UnitZ),
                Matrix4X4.CreateLookAt(new(0f), -Vector3D<float>.UnitY, -Vector3D<float>.UnitZ),
                Matrix4X4.CreateLookAt(new(0f), Vector3D<float>.UnitZ, -Vector3D<float>.UnitY),
                Matrix4X4.CreateLookAt(new(0f), -Vector3D<float>.UnitZ, -Vector3D<float>.UnitY),
            };

            Rendering.gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            Rendering.gl.Viewport(new Vector2D<int>((int)resolution));
            shader.Bind();
            shader.SetTexture("uTexture", tex);
            shader.BindTextures();

            for (int i = 0; i < 6; i++)
            {
                shader.SetMat4Cache("uMat", viewMats[i] * projMat);
                Rendering.gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                    TextureTarget.TextureCubeMapPositiveX + i, cubemap.id, 0);

                Rendering.gl.Clear(ClearBufferMask.ColorBufferBit);
                Rendering.skyboxMesh.vertexArray.Bind();
                Rendering.gl.DrawElements(PrimitiveType.Triangles, (uint)Rendering.skyboxMesh.meshData.indices.Length, DrawElementsType.UnsignedInt, null);
            }

            cubemap.Bind(0);
            Rendering.gl.GenerateMipmap(cubemap.textureType);
            cubemap.Unbind(0);

            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Rendering.gl.DeleteFramebuffer(fbId);
            return cubemap;
        }
        public static unsafe TextureCubemap ComputeIrradiance(TextureCubemap tex, uint resolution, float sampleDelta = .025f)
        {
            ShaderProgram shader = new(Shader.Create("Shaders/Skybox/RectToCubemapVert.glsl", ShaderType.VertexShader),
                Shader.Create("Shaders/Skybox/IrradianceConvolutionFrag.glsl", ShaderType.FragmentShader));

            shader.SetFloatCache("uSampleDelta", sampleDelta);

            TextureCubemap cubemap = new(resolution, resolution, InternalFormat.Rgb16f, PixelFormat.Rgb);
            uint fbId = Rendering.gl.GenFramebuffer();
            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbId);

            Matrix4X4<float> projMat = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(90f), 1f, .1f, 10f);
            Matrix4X4<float>[] viewMats = new Matrix4X4<float>[6]
            {
                Matrix4X4.CreateLookAt(new(0f), Vector3D<float>.UnitX, -Vector3D<float>.UnitY),
                Matrix4X4.CreateLookAt(new(0f), -Vector3D<float>.UnitX, -Vector3D<float>.UnitY),
                Matrix4X4.CreateLookAt(new(0f), Vector3D<float>.UnitY, Vector3D<float>.UnitZ),
                Matrix4X4.CreateLookAt(new(0f), -Vector3D<float>.UnitY, -Vector3D<float>.UnitZ),
                Matrix4X4.CreateLookAt(new(0f), Vector3D<float>.UnitZ, -Vector3D<float>.UnitY),
                Matrix4X4.CreateLookAt(new(0f), -Vector3D<float>.UnitZ, -Vector3D<float>.UnitY),
            };

            Rendering.gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            Rendering.gl.Viewport(new Vector2D<int>((int)resolution));
            shader.Bind();
            shader.SetTexture("uTexture", tex);
            shader.BindTextures();

            for (int i = 0; i < 6; i++)
            {
                shader.SetMat4Cache("uMat", viewMats[i] * projMat);
                Rendering.gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                    TextureTarget.TextureCubeMapPositiveX + i, cubemap.id, 0);

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
            ShaderProgram shader = new(Shader.Create("Shaders/Skybox/RectToCubemapVert.glsl", ShaderType.VertexShader),
                Shader.Create("Shaders/Skybox/SpecularReflectionFilter.glsl", ShaderType.FragmentShader));

            cubemap.Bind(0);
            Rendering.gl.TexParameterI(cubemap.textureType, TextureParameterName.TextureMinFilter, (uint)GLEnum.LinearMipmapLinear);
            cubemap.Unbind(0);

            uint fbId = Rendering.gl.GenFramebuffer();
            Rendering.gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbId);

            Matrix4X4<float> projMat = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(90f), 1f, .1f, 10f);
            Matrix4X4<float>[] viewMats = new Matrix4X4<float>[6]
            {
                Matrix4X4.CreateLookAt(new(0f), Vector3D<float>.UnitX, -Vector3D<float>.UnitY),
                Matrix4X4.CreateLookAt(new(0f), -Vector3D<float>.UnitX, -Vector3D<float>.UnitY),
                Matrix4X4.CreateLookAt(new(0f), Vector3D<float>.UnitY, Vector3D<float>.UnitZ),
                Matrix4X4.CreateLookAt(new(0f), -Vector3D<float>.UnitY, -Vector3D<float>.UnitZ),
                Matrix4X4.CreateLookAt(new(0f), Vector3D<float>.UnitZ, -Vector3D<float>.UnitY),
                Matrix4X4.CreateLookAt(new(0f), -Vector3D<float>.UnitZ, -Vector3D<float>.UnitY),
            };

            Rendering.gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            shader.Bind();
            shader.SetTexture("uTexture", tex);
            shader.BindTextures();

            shader.SetFloatCache("uResolution", resolution);
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
                        TextureTarget.TextureCubeMapPositiveX + i, cubemap.id, mip);

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

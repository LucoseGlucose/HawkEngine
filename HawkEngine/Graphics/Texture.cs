using System;
using System.Collections.Generic;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using HawkEngine.Core;
using StbImageSharp;
using System.IO;

namespace HawkEngine.Graphics
{
    public class Texture : IDisposable
    {
        public readonly uint id;
        public readonly TextureTarget textureType;

        public Texture(TextureTarget textureType)
        {
            this.textureType = textureType;
            id = Rendering.gl.GenTexture();
        }
        ~Texture()
        {
            Rendering.deletedObjects.Enqueue(() => Rendering.gl.DeleteTexture(id));
        }
        public void Bind(int unit)
        {
            Rendering.gl.ActiveTexture(TextureUnit.Texture0 + unit);
            Rendering.gl.BindTexture(textureType, id);
        }
        public void Unbind(int unit)
        {
            Rendering.gl.ActiveTexture(TextureUnit.Texture0 + unit);
            Rendering.gl.BindTexture(textureType, 0);
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Rendering.gl.DeleteTexture(id);
        }
    }

    public class Texture2D : Texture
    {
        public static readonly Texture2D whiteTex = new(Vector4D<float>.One);
        public static readonly Texture2D blackTex = new(Vector4D<float>.Zero);
        public static readonly Texture2D normalTex = new(new Vector4D<float>(.5f, .5f, 1f, 1f));

        public Texture2D(uint width, uint height, byte[] data, InternalFormat internalFormat = InternalFormat.Rgba8,
            PixelFormat pixelFormat = PixelFormat.Rgba, uint samples = 1, int border = 0, GLEnum filter = GLEnum.Linear, GLEnum wrap = GLEnum.Repeat)
            : base(samples <= 1 ? TextureTarget.Texture2D : TextureTarget.Texture2DMultisample)
        {
            Bind(0);
            if (samples <= 1) Rendering.gl.TexImage2D<byte>(textureType, 0, internalFormat, width, height, border, pixelFormat, PixelType.UnsignedByte, data);
            else
            {
                Rendering.gl.TexImage2DMultisample(textureType, samples, internalFormat, width, height, true);
                Unbind(0);
                return;
            }

            Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureMinFilter, (uint)filter);
            Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureMagFilter, (uint)filter);

            Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureWrapS, (uint)wrap);
            Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureWrapT, (uint)wrap);

            Rendering.gl.GenerateMipmap(textureType);
            Unbind(0);
        }
        public Texture2D(uint width, uint height, InternalFormat internalFormat = InternalFormat.Rgba8,
            PixelFormat pixelFormat = PixelFormat.Rgba, uint samples = 1, int border = 0, GLEnum filter = GLEnum.Linear, GLEnum wrap = GLEnum.Repeat)
            : this(width, height, null, internalFormat, pixelFormat, samples, border, filter, wrap)
        {

        }
        public unsafe Texture2D(string path, bool sRGB = true, int border = 0, GLEnum filter = GLEnum.Linear, GLEnum wrap = GLEnum.Repeat)
            : base(TextureTarget.Texture2D)
        {
            using Stream file = File.OpenRead(Path.GetFullPath("../../../Resources/" + path));

            StbImage.stbi__result_info result;
            int width;
            int height;
            int components;

            void* data = StbImage.stbi__load_main(new StbImage.stbi__context(file), &width, &height, &components, 4, &result, 8);
            StbImage.stbi__vertical_flip(data, width, height, 4);
            ReadOnlySpan<byte> span = new(data, width * height * 32);

            Bind(0);
            Rendering.gl.TexImage2D(textureType, 0, sRGB ? InternalFormat.Srgb8Alpha8 : InternalFormat.Rgba8,
                (uint)width, (uint)height, border, PixelFormat.Rgba, PixelType.UnsignedByte, span);

            Rendering.gl.TexParameter(textureType, TextureParameterName.TextureMinFilter, (int)filter);
            Rendering.gl.TexParameter(textureType, TextureParameterName.TextureMagFilter, (int)filter);

            Rendering.gl.TexParameter(textureType, TextureParameterName.TextureWrapS, (int)wrap);
            Rendering.gl.TexParameter(textureType, TextureParameterName.TextureWrapT, (int)wrap);

            Rendering.gl.GenerateMipmap(textureType);
            Unbind(0);
        }
        public Texture2D(Vector4D<float> color) : this(1, 1, new byte[4] { (byte)(color.X * 255f), (byte)(color.Y * 255f),
            (byte)(color.Z * 255f), (byte)(color.W * 255f) }, InternalFormat.Rgba8, PixelFormat.Rgba, 1, 0, GLEnum.Nearest, GLEnum.Repeat)
        {

        }
    }

    public class TextureCubemap : Texture
    {
        public TextureCubemap(uint width, uint height, byte[][] data, InternalFormat internalFormat = InternalFormat.Rgba8,
            PixelFormat pixelFormat = PixelFormat.Rgba, int border = 0, GLEnum filter = GLEnum.Linear, GLEnum wrap = GLEnum.Repeat)
            : base(TextureTarget.TextureCubeMap)
        {
            Bind(0);
            for (int i = 0; i < 6; i++)
            {
                Rendering.gl.TexImage2D<byte>(textureType, 0, internalFormat, width, height, border, pixelFormat, PixelType.UnsignedByte, data[i]);

                Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureMinFilter, (uint)filter);
                Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureMagFilter, (uint)filter);

                Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureWrapS, (uint)wrap);
                Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureWrapT, (uint)wrap);
                Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureWrapR, (uint)wrap);
            }

            Unbind(0);
        }
        public TextureCubemap(uint width, uint height, InternalFormat internalFormat = InternalFormat.Rgba8,
            PixelFormat pixelFormat = PixelFormat.Rgba, int border = 0, GLEnum filter = GLEnum.Linear, GLEnum wrap = GLEnum.Repeat)
            : this(width, height, new byte[6][], internalFormat, pixelFormat, border, filter, wrap)
        {

        }
        public unsafe TextureCubemap(string[] paths, bool sRGB = true, int border = 0 , GLEnum filter = GLEnum.Linear, GLEnum wrap = GLEnum.Repeat)
            : base(TextureTarget.TextureCubeMap)
        {
            for (int i = 0; i < 6; i++)
            {
                using Stream file = File.OpenRead(Path.GetFullPath("../../../Resources/" + paths[i]));

                StbImage.stbi__result_info result;
                int width;
                int height;
                int components;

                void* data = StbImage.stbi__load_main(new StbImage.stbi__context(file), &width, &height, &components, 4, &result, 8);
                StbImage.stbi__vertical_flip(data, width, height, 4);
                ReadOnlySpan<byte> span = new(data, width * height * 32);

                Bind(0);
                Rendering.gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, sRGB ? InternalFormat.Srgb8Alpha8 : InternalFormat.Rgba8,
                    (uint)width, (uint)height, border, PixelFormat.Rgba, PixelType.UnsignedByte, span);

                Rendering.gl.TexParameter(textureType, TextureParameterName.TextureMinFilter, (int)filter);
                Rendering.gl.TexParameter(textureType, TextureParameterName.TextureMagFilter, (int)filter);

                Rendering.gl.TexParameter(textureType, TextureParameterName.TextureWrapS, (int)wrap);
                Rendering.gl.TexParameter(textureType, TextureParameterName.TextureWrapT, (int)wrap);
                Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureWrapR, (uint)wrap);
            }

            Unbind(0);
        }
        public TextureCubemap(Vector4D<float> color) : this(1, 1, new byte[6][] { new byte[4] { (byte)(color.X * 255f), (byte)(color.Y * 255f),
            (byte)(color.Z * 255f), (byte)(color.W * 255f) }, new byte[4] { (byte)(color.X * 255f), (byte)(color.Y * 255f),
            (byte)(color.Z * 255f), (byte)(color.W * 255f) }, new byte[4] { (byte)(color.X * 255f), (byte)(color.Y * 255f),
            (byte)(color.Z * 255f), (byte)(color.W * 255f) }, new byte[4] { (byte)(color.X * 255f), (byte)(color.Y * 255f),
            (byte)(color.Z * 255f), (byte)(color.W * 255f) }, new byte[4] { (byte)(color.X * 255f), (byte)(color.Y * 255f),
            (byte)(color.Z * 255f), (byte)(color.W * 255f) }, new byte[4] { (byte)(color.X * 255f), (byte)(color.Y * 255f),
            (byte)(color.Z * 255f), (byte)(color.W * 255f) } }, InternalFormat.Rgba8, PixelFormat.Rgba, 0, GLEnum.Nearest, GLEnum.Repeat)
        {

        }
    }

    public class FramebufferTexture : Texture2D
    {
        public readonly FramebufferAttachment attachment;
        public readonly InternalFormat internalFormat;
        public readonly PixelFormat pixelFormat;
        
        public FramebufferTexture(uint width, uint height, FramebufferAttachment attachment, InternalFormat internalFormat,
            PixelFormat pixelFormat, uint samples = 1, int border = 0, GLEnum filter = GLEnum.Linear, GLEnum wrap = GLEnum.Repeat)
            : base(width, height, internalFormat, pixelFormat, samples, border, filter, wrap)
        {
            this.attachment = attachment;
            this.internalFormat = internalFormat;
            this.pixelFormat = pixelFormat;
        }
    }
}

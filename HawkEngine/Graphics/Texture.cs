using System;
using System.Collections.Generic;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using HawkEngine.Core;
using StbImageSharp;
using System.IO;

namespace HawkEngine.Graphics
{
    public class Texture : HawkObject, IDisposable
    {
        public readonly uint id;
        public readonly TextureTarget textureType;
        public readonly InternalFormat internalFormat;
        public readonly PixelFormat pixelFormat;
        public readonly Vector2D<uint> size;

        public Texture(TextureTarget textureType, InternalFormat internalFormat, PixelFormat pixelFormat, Vector2D<uint> size, string name) : base(name)
        {
            this.textureType = textureType;
            this.internalFormat = internalFormat;
            this.pixelFormat = pixelFormat;
            this.size = size;

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
        public static readonly Texture2D grayTex = new(new(.5f));
        public static readonly Texture2D normalTex = new(new Vector4D<float>(.5f, .5f, 1f, 1f));
        public static readonly Texture2D brdfTex = new("Images/ibl_brdf_lut.png", false, wrap: GLEnum.ClampToEdge);

        public Texture2D(uint width, uint height, byte[] data, InternalFormat internalFormat = InternalFormat.Rgba8, PixelFormat pixelFormat = PixelFormat.Rgba,
            uint samples = 1, GLEnum filter = GLEnum.Linear, GLEnum wrap = GLEnum.Repeat, bool mipmap = false, PixelType pixelType = PixelType.UnsignedByte,
            string name = nameof(Texture2D)) : base(samples <= 1 ? TextureTarget.Texture2D : TextureTarget.Texture2DMultisample, internalFormat,
                pixelFormat, new(width, height), name)
        {
            Bind(0);
            if (samples <= 1) Rendering.gl.TexImage2D<byte>(textureType, 0, internalFormat, width, height, 0, pixelFormat, pixelType, data);
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

            if (mipmap) Rendering.gl.GenerateMipmap(textureType);
            Unbind(0);
        }
        public Texture2D(uint width, uint height, InternalFormat internalFormat = InternalFormat.Rgba8, PixelFormat pixelFormat = PixelFormat.Rgba,
            uint samples = 1, GLEnum filter = GLEnum.Linear, GLEnum wrap = GLEnum.Repeat, bool mipmap = false, PixelType pixelType = PixelType.UnsignedByte,
            string name = nameof(Texture2D)) : this(width, height, null, internalFormat, pixelFormat, samples, filter, wrap, mipmap, pixelType, name)
        {

        }
        public unsafe Texture2D(string path, bool sRGB = true, bool hdr = false, GLEnum filter = GLEnum.Linear,
            GLEnum wrap = GLEnum.Repeat, bool flip = true, bool mipmap = false, string name = nameof(Texture2D))
            : base(TextureTarget.Texture2D, sRGB ? InternalFormat.Srgb8Alpha8 : InternalFormat.Rgba8, PixelFormat.Rgba, new(0), name)
        {
            using Stream file = File.OpenRead(Path.GetFullPath("../../../Resources/" + path));
            Bind(0);

            StbImage.stbi__result_info result;
            int components;

            fixed (void* xP = &size.X, yP = &size.Y)
            {
                if (!hdr)
                {
                    void* data = StbImage.stbi__load_main(new StbImage.stbi__context(file), (int*)xP, (int*)yP, &components, 4, &result, 8);
                    if (flip) StbImage.stbi__vertical_flip(data, (int)size.X, (int)size.Y, 4);

                    ReadOnlySpan<byte> span = new(data, (int)(size.X * size.Y * 4u));
                    Rendering.gl.TexImage2D(textureType, 0, sRGB ? InternalFormat.Srgb8Alpha8 : InternalFormat.Rgba8,
                        size.X, size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, span);
                }
                else
                {
                    float* data = StbImage.stbi__loadf_main(new StbImage.stbi__context(file), (int*)xP, (int*)yP, &components, 3);
                    if (flip) StbImage.stbi__vertical_flip(data, (int)size.X, (int)size.Y, 12);

                    ReadOnlySpan<float> span = new(data, (int)(size.X * size.Y * 3u));
                    Rendering.gl.TexImage2D(textureType, 0, InternalFormat.Rgb16f,
                        size.X, size.Y, 0, PixelFormat.Rgb, PixelType.Float, span);
                }
            }

            Rendering.gl.TexParameter(textureType, TextureParameterName.TextureMinFilter, (int)filter);
            Rendering.gl.TexParameter(textureType, TextureParameterName.TextureMagFilter, (int)filter);

            Rendering.gl.TexParameter(textureType, TextureParameterName.TextureWrapS, (int)wrap);
            Rendering.gl.TexParameter(textureType, TextureParameterName.TextureWrapT, (int)wrap);

            if (mipmap) Rendering.gl.GenerateMipmap(textureType);
            Unbind(0);
        }
        public Texture2D(Vector4D<float> color) : this(1, 1, new byte[4] { (byte)(color.X * 255f), (byte)(color.Y * 255f),
            (byte)(color.Z * 255f), (byte)(color.W * 255f) }, InternalFormat.Rgba8, PixelFormat.Rgba, 1, GLEnum.Nearest, GLEnum.Repeat)
        {

        }
    }

    public class TextureCubemap : Texture
    {
        public static readonly TextureCubemap whiteTex = new(Vector4D<float>.One);
        public static readonly TextureCubemap blackTex = new(Vector4D<float>.Zero);
        public static readonly TextureCubemap grayTex = new(new(.5f));

        public TextureCubemap(uint width, uint height, byte[][] data, InternalFormat internalFormat = InternalFormat.Rgba8, PixelFormat pixelFormat
            = PixelFormat.Rgba, GLEnum filter = GLEnum.Linear, GLEnum wrap = GLEnum.Repeat, bool mipmap = false, string name = nameof(TextureCubemap))
            : base(TextureTarget.TextureCubeMap, internalFormat, pixelFormat, new(width, height), name)
        {
            Bind(0);
            for (int i = 0; i < 6; i++)
            {
                Rendering.gl.TexImage2D<byte>(TextureTarget.TextureCubeMapPositiveX + i,
                    0, internalFormat, width, height, 0, pixelFormat, PixelType.UnsignedByte, data[i]);
            }

            Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureMinFilter, (uint)filter);
            Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureMagFilter, (uint)filter);

            Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureWrapS, (uint)wrap);
            Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureWrapT, (uint)wrap);
            Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureWrapR, (uint)wrap);

            if (mipmap) Rendering.gl.GenerateMipmap(textureType);
            Unbind(0);
        }
        public TextureCubemap(uint width, uint height, InternalFormat internalFormat = InternalFormat.Rgba8, PixelFormat pixelFormat = PixelFormat.Rgba,
            GLEnum filter = GLEnum.Linear, GLEnum wrap = GLEnum.Repeat, bool mipmap = false, string name = nameof(TextureCubemap))
            : this(width, height, new byte[6][], internalFormat, pixelFormat, filter, wrap, mipmap, name)
        {

        }
        public unsafe TextureCubemap(string[] paths, bool sRGB = true, GLEnum filter = GLEnum.Linear,
            GLEnum wrap = GLEnum.Repeat, bool flip = true, bool mipmap = false, string name = nameof(TextureCubemap))
            : base(TextureTarget.TextureCubeMap, sRGB ? InternalFormat.Srgb8Alpha8 : InternalFormat.Rgba8, PixelFormat.Rgba, new(0), name)
        {
            for (int i = 0; i < 6; i++)
            {
                using Stream file = File.OpenRead(Path.GetFullPath("../../../Resources/" + paths[i]));

                fixed (void* xP = &size.X, yP = &size.Y)
                {
                    StbImage.stbi__result_info result;
                    int components;

                    void* data = StbImage.stbi__load_main(new StbImage.stbi__context(file), (int*)xP, (int*)yP, &components, 4, &result, 8);
                    if (flip) StbImage.stbi__vertical_flip(data, (int)size.X, (int)size.Y, 4);
                    ReadOnlySpan<byte> span = new(data, (int)(size.X * size.Y * 4u));

                    Bind(0);
                    Rendering.gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, sRGB ? InternalFormat.Srgb8Alpha8 : InternalFormat.Rgba8,
                        size.X, size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, span);
                }

                Rendering.gl.TexParameter(textureType, TextureParameterName.TextureMinFilter, (int)filter);
                Rendering.gl.TexParameter(textureType, TextureParameterName.TextureMagFilter, (int)filter);

                Rendering.gl.TexParameter(textureType, TextureParameterName.TextureWrapS, (int)wrap);
                Rendering.gl.TexParameter(textureType, TextureParameterName.TextureWrapT, (int)wrap);
                Rendering.gl.TexParameterI(textureType, TextureParameterName.TextureWrapR, (uint)wrap);
            }

            if (mipmap) Rendering.gl.GenerateMipmap(textureType);
            Unbind(0);
        }
        public TextureCubemap(Vector4D<float> color) : this(1, 1, new byte[6][] { new byte[4] { (byte)(color.X * 255f), (byte)(color.Y * 255f),
            (byte)(color.Z * 255f), (byte)(color.W * 255f) }, new byte[4] { (byte)(color.X * 255f), (byte)(color.Y * 255f),
            (byte)(color.Z * 255f), (byte)(color.W * 255f) }, new byte[4] { (byte)(color.X * 255f), (byte)(color.Y * 255f),
            (byte)(color.Z * 255f), (byte)(color.W * 255f) }, new byte[4] { (byte)(color.X * 255f), (byte)(color.Y * 255f),
            (byte)(color.Z * 255f), (byte)(color.W * 255f) }, new byte[4] { (byte)(color.X * 255f), (byte)(color.Y * 255f),
            (byte)(color.Z * 255f), (byte)(color.W * 255f) }, new byte[4] { (byte)(color.X * 255f), (byte)(color.Y * 255f),
            (byte)(color.Z * 255f), (byte)(color.W * 255f) } }, InternalFormat.Rgba8, PixelFormat.Rgba, GLEnum.Nearest, GLEnum.Repeat)
        {

        }
    }
}

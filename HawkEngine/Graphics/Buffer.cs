using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HawkEngine.Core;
using Silk.NET.OpenGL;
using System.Xml.Serialization;

namespace HawkEngine.Graphics
{
    public unsafe class Buffer : IDisposable
    {
        [Utils.DontSerialize] public readonly uint glID;
        protected readonly GLEnum bufferType;

        protected Buffer(uint id, GLEnum bufferType)
        {
            glID = id;
            this.bufferType = bufferType;
        }
        ~Buffer()
        {
            Rendering.deletedObjects.Enqueue(() => Rendering.gl.DeleteBuffer(glID));
        }
        public virtual void Bind()
        {
            Rendering.gl.BindBuffer(bufferType, glID);
        }
        public virtual void Unbind()
        {
            Rendering.gl.BindBuffer(bufferType, 0);
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Rendering.gl.DeleteBuffer(glID);
        }
    }

    public class Buffer<T> : Buffer where T : unmanaged
    {
        public T[] data { get; protected set; }

        public Buffer(GLEnum bufferType, T[] data, BufferStorageMask usage) : base(Rendering.gl.GenBuffer(), bufferType)
        {
            Bind();
            Rendering.gl.BufferStorage<T>(bufferType, data, usage);
            Unbind();

            this.data = data;
        }
        public virtual void SetData(T[] data, int offset = 0)
        {
            Bind();
            Rendering.gl.BufferSubData<T>(bufferType, offset, data);
            Unbind();

            data.CopyTo(this.data, offset);
        }
    }

    public sealed class AttribBuffer : Buffer<float>
    {
        public readonly int numComponents;

        public AttribBuffer(float[] data, int numComponents, BufferStorageMask usage) : base(GLEnum.ArrayBuffer, data, usage)
        {
            this.numComponents = numComponents;
        }
    }

    public sealed class IndexBuffer : Buffer<uint>
    {
        public IndexBuffer(uint[] data, BufferStorageMask usage) : base(GLEnum.ElementArrayBuffer, data, usage)
        {

        }
    }
}

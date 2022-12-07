using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace HawkEngine.Graphics
{
    public unsafe class Buffer
    {
        public readonly uint id;
        protected readonly GLEnum bufferType;

        public Buffer(uint id, GLEnum bufferType)
        {
            this.id = id;
            this.bufferType = bufferType;
        }
        ~Buffer()
        {
            Rendering.gl.DeleteBuffer(id);
        }
        public virtual void Bind()
        {
            Rendering.gl.BindBuffer(bufferType, id);
        }
        public virtual void Unbind()
        {
            Rendering.gl.BindBuffer(bufferType, 0);
        }
        public void Delete()
        {
            GC.SuppressFinalize(this);
            Rendering.gl.DeleteBuffer(id);
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

    public class AttribBuffer : Buffer<float>
    {
        public readonly int numComponents;

        public AttribBuffer(float[] data, int numComponents, BufferStorageMask usage) : base(GLEnum.ArrayBuffer, data, usage)
        {
            this.numComponents = numComponents;
        }
    }

    public class IndexBuffer : Buffer<uint>
    {
        public IndexBuffer(uint[] data, BufferStorageMask usage) : base(GLEnum.ElementArrayBuffer, data, usage)
        {

        }
    }
}

using System;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace HawkEngine.Graphics
{
    public unsafe class ReadOnlyBuffer
    {
        public readonly uint id;
        private readonly BufferTargetARB type;

        protected ReadOnlyBuffer(uint id, BufferTargetARB type)
        {
            this.id = id;
            this.type = type;
        }
        public virtual ReadOnlyBuffer Create<T>(BufferTargetARB type, T[] data, BufferUsageARB usage = BufferUsageARB.StaticDraw) where T : unmanaged
        {
            ReadOnlyBuffer buf = new(Rendering.gl.GenBuffer(), type);

            Bind();
            Rendering.gl.BufferData<T>(type, data, usage);
            Unbind();

            return buf;
        }
        public virtual void Bind()
        {
            Rendering.gl.BindBuffer(type, id);
        }
        public virtual void Unbind()
        {
            Rendering.gl.BindBuffer(type, 0);
        }
    }
}

using System;
using System.Collections.Generic;
using Silk.NET.Maths;
using HawkEngine.Core;
using Silk.NET.OpenGL;
using System.Xml.Serialization;

namespace HawkEngine.Graphics
{
    public class VertexArray : IDisposable
    {
        [Utils.DontSerialize] public readonly uint glID;
        public readonly Buffer indexBuffer;
        public readonly AttribBuffer[] attribBuffers;

        public unsafe VertexArray(Buffer indexBuffer, params AttribBuffer[] attribBuffers)
        {
            glID = Rendering.gl.GenVertexArray();

            this.attribBuffers = attribBuffers;
            this.indexBuffer = indexBuffer;

            Bind();

            for (uint i = 0; i < attribBuffers.Length; i++)
            {
                Rendering.gl.EnableVertexAttribArray(i);
                attribBuffers[i].Bind();
                Rendering.gl.VertexAttribPointer(i, attribBuffers[i].numComponents, GLEnum.Float, false, 0, null);
            }
        }
        public unsafe VertexArray(MeshData meshData) : this
            (
                new IndexBuffer(meshData.indices, BufferStorageMask.MapReadBit),

                new AttribBuffer(Conversions.ExpandArray(meshData.verts, v => { return new float[3] { v.X, v.Y, v.Z }; }), 3, BufferStorageMask.MapReadBit),
                new(Conversions.ExpandArray(meshData.normals, v => { return new float[3] { v.X, v.Y, v.Z }; }), 3, BufferStorageMask.MapReadBit),
                new(Conversions.ExpandArray(meshData.uvs, v => { return new float[2] { v.X, v.Y }; }), 2, BufferStorageMask.MapReadBit),
                new(Conversions.ExpandArray(meshData.tangents, v => { return new float[3] { v.X, v.Y, v.Z }; }), 3, BufferStorageMask.MapReadBit),
                new(Conversions.ExpandArray(meshData.bitangents, v => { return new float[3] { v.X, v.Y, v.Z }; }), 3, BufferStorageMask.MapReadBit)
            )
        {

        }
        ~VertexArray()
        {
            Rendering.deletedObjects.Enqueue(() => Rendering.gl.DeleteVertexArray(glID));
        }
        public void Bind()
        {
            Rendering.gl.BindVertexArray(glID);
            indexBuffer.Bind();
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Rendering.gl.DeleteVertexArray(glID);
        }
    }
}

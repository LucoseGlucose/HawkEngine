using System;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace HawkEngine.Graphics
{
    public class Mesh : IDisposable
    {
        public MeshData meshData;
        public readonly VertexArray vertexArray;

        public Mesh(string path)
        {
            meshData = new(path);
            vertexArray = new(meshData);
        }
        public Mesh(MeshData meshData, VertexArray vertexArray)
        {
            this.meshData = meshData;
            this.vertexArray = vertexArray;
        }
        public void Dispose()
        {
            for (int i = 0; i < vertexArray.attribBuffers.Length; i++)
            {
                vertexArray.attribBuffers[i].Dispose();
            }
            vertexArray.indexBuffer.Dispose();
            vertexArray.Dispose();
        }
    }
}

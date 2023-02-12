using System;
using System.Collections.Generic;
using HawkEngine.Core;
using Silk.NET.OpenGL;
using System.Xml.Serialization;

namespace HawkEngine.Graphics
{
    public sealed class Mesh : IDisposable
    {
        public MeshData meshData;
        [Utils.DontSerialize] public readonly VertexArray vertexArray;

        public Mesh()
        {

        }
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
        private void Create()
        {
            //Utils.SetFieldWithReflection(this, "vertexArray", new VertexArray(meshData));
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

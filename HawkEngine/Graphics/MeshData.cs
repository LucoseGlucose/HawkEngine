using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Silk.NET.Assimp;
using HawkEngine.Core;
using System.Numerics;
using Silk.NET.Maths;

namespace HawkEngine.Graphics
{
    public struct MeshData
    {
        public static PostProcessSteps postProcessSteps = PostProcessSteps.CalculateTangentSpace | PostProcessSteps.JoinIdenticalVertices
            | PostProcessSteps.Triangulate | PostProcessSteps.OptimizeMeshes;

        public readonly uint[] indices;
        public readonly Vector3D<float>[] verts;
        public readonly Vector3D<float>[] normals;
        public readonly Vector2D<float>[] uvs;
        public readonly Vector3D<float>[] tangents;
        public readonly Vector3D<float>[] bitangents;

        public MeshData(Vector3D<float>[] verts, Vector3D<float>[] normals, Vector2D<float>[] uvs)
        {
            this.verts = verts;
            this.normals = normals;
            this.uvs = uvs;
        }
        public unsafe MeshData(string path)
        {
            Assimp assimp = Assimp.GetApi();
            Silk.NET.Assimp.Scene* scene = assimp.ImportFile(Path.GetFullPath(path), (uint)postProcessSteps);

            if (scene->MNumMeshes < 1) return;
            Silk.NET.Assimp.Mesh* mesh = scene->MMeshes[0];

            List<uint> indexes = new();
            for (int f = 0; f < mesh->MNumFaces; f++)
            {
                Face face = mesh->MFaces[f];
                for (int i = 0; i < face.MNumIndices; i++)
                {
                    indexes.Add(face.MIndices[i]);
                }
            }
            indices = indexes.ToArray();

            verts = Conversions.ConvertPointer(mesh->MVertices, mesh->MNumVertices, v => { return new Vector3D<float>(v.X, v.Y, v.Z); });
            normals = Conversions.ConvertPointer(mesh->MNormals, mesh->MNumVertices, v => { return new Vector3D<float>(v.X, v.Y, v.Z); });
            uvs = Conversions.ConvertPointer(mesh->MTextureCoords[0], mesh->MNumVertices, v => { return new Vector2D<float>(v.X, v.Y); });
            tangents = Conversions.ConvertPointer(mesh->MTangents, mesh->MNumVertices, v => { return new Vector3D<float>(v.X, v.Y, v.Z); });
            bitangents = Conversions.ConvertPointer(mesh->MBitangents, mesh->MNumVertices, v => { return new Vector3D<float>(v.X, v.Y, v.Z); });

            assimp.FreeScene(scene);
        }
    }
}

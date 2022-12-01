using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.OpenGL;
using System.IO;

namespace HawkEngine.Graphics
{
    public static class Shader
    {
        private static readonly Dictionary<string, uint> compiledShaders = new();

        public static uint Create(string path, ShaderType type)
        {
            if (compiledShaders.ContainsKey(path)) return compiledShaders[path];
            uint id = Rendering.gl.CreateShader(type);

            Rendering.gl.ShaderSource(id, File.ReadAllText(path));
            Rendering.gl.CompileShader(id);

            compiledShaders.Add(path, id);
            return id;
        }
    }
}

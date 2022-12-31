using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.OpenGL;
using System.IO;
using HawkEngine.Core;

namespace HawkEngine.Graphics
{
    public class Shader : HawkObject
    {
        private static readonly Dictionary<string, Shader> compiledShaders = new();

        public readonly string filePath;
        public readonly ShaderType type;
        public readonly uint id;

        private Shader(string filePath, ShaderType type, uint id) : base(filePath)
        {
            this.filePath = filePath;
            this.type = type;
            this.id = id;
        }
        public static Shader Create(string path, ShaderType type)
        {
            if (compiledShaders.TryGetValue(path, out Shader value)) return value;
            uint id = Rendering.gl.CreateShader(type);

            Rendering.gl.ShaderSource(id, File.ReadAllText(Path.GetFullPath("../../../Resources/" + path)));
            Rendering.gl.CompileShader(id);

            Shader shader = new(path, type, id);
            compiledShaders.Add(path, shader);

            string infoLog = Rendering.gl.GetShaderInfoLog(id);
            if (!string.IsNullOrEmpty(infoLog)) Editor.EditorUtils.PrintMessage(Editor.EditorUtils.MessageSeverity.Error, infoLog, shader);

            return shader;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.OpenGL;
using System.IO;
using HawkEngine.Core;
using System.Xml.Serialization;

namespace HawkEngine.Graphics
{
    public sealed class Shader : HawkObject
    {
        private static readonly Dictionary<string, Shader> compiledShaders = new();

        public readonly string filePath;
        public readonly ShaderType type;
        [Utils.DontSerialize] public readonly uint glID;

        public Shader() : base()
        {

        }
        public Shader(string filePath, ShaderType type) : base(filePath)
        {
            this.filePath = filePath;

            if (compiledShaders.TryGetValue(filePath, out Shader value))
            {
                this.type = value.type;
                glID = value.glID;

                SetShaderPointer(ref value, this);
                bool equal = this == value;
                return;
            }

            this.type = type;
            glID = Rendering.gl.CreateShader(type);

            Rendering.gl.ShaderSource(glID, File.ReadAllText(Path.GetFullPath("../../../Resources/" + filePath)));
            Rendering.gl.CompileShader(glID);

            compiledShaders.Add(filePath, this);

#if DEBUG
            string infoLog = Rendering.gl.GetShaderInfoLog(glID);
            if (!string.IsNullOrEmpty(infoLog)) Editor.EditorUtils.PrintMessage(Editor.EditorUtils.MessageSeverity.Error,
                $"Could not compile shader at {filePath}", this, infoLog);
#endif
        }
        private static void SetShaderPointer(ref Shader dest, Shader src)
        {
            dest = src;
        }
        protected override void Create()
        {
            base.Create();

            if (compiledShaders.TryGetValue(filePath, out Shader value))
            {
                Utils.SetFieldWithReflection(this, "type", value.type);
                Utils.SetFieldWithReflection(this, "glID", value.glID);

                SetShaderPointer(ref value, this);
                return;
            }

            Utils.SetFieldWithReflection(this, "glID", Rendering.gl.CreateShader(type));

            Rendering.gl.ShaderSource(glID, File.ReadAllText(Path.GetFullPath("../../../Resources/" + filePath)));
            Rendering.gl.CompileShader(glID);

            compiledShaders.Add(filePath, this);
        }
    }
}

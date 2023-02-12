using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.OpenGL;
using Silk.NET.Maths;
using HawkEngine.Core;
using System.IO;
using System.Xml.Serialization;

namespace HawkEngine.Graphics
{
    public sealed unsafe class ShaderProgram : HawkObject, IDisposable
    {
        [Utils.DontSerialize] public readonly uint glID;

        [Utils.DontSerialize] public readonly (Texture[], List<string>) textures = (null, new());
        [Utils.DontSerialize] public readonly Dictionary<string, int> uniforms = new();

        [Utils.DontSerialize] private readonly Dictionary<string, bool> boolValues = new();
        [Utils.DontSerialize] private readonly Dictionary<string, int> intValues = new();
        [Utils.DontSerialize] private readonly Dictionary<string, float> floatValues = new();
        [Utils.DontSerialize] private readonly Dictionary<string, Vector2D<float>> vec2Values = new();
        [Utils.DontSerialize] private readonly Dictionary<string, Vector3D<float>> vec3Values = new();
        [Utils.DontSerialize] private readonly Dictionary<string, Vector4D<float>> vec4Values = new();
        [Utils.DontSerialize] private readonly Dictionary<string, Matrix3X3<float>> mat3Values = new();
        [Utils.DontSerialize] private readonly Dictionary<string, Matrix4X4<float>> mat4Values = new();

        public Shader[] compiledShaders;

        public ShaderProgram() : base()
        {

        }
    public ShaderProgram(string vertexShader, string fragmentShader)
            : this(new Shader(vertexShader, ShaderType.VertexShader), new(fragmentShader, ShaderType.FragmentShader))
        {

        }
        public ShaderProgram(string vertexShader, string geometryShader, string fragmentShader) : this(new Shader(vertexShader,
            ShaderType.VertexShader), new(geometryShader, ShaderType.GeometryShader), new(fragmentShader, ShaderType.FragmentShader))
        {

        }
        public unsafe ShaderProgram(params Shader[] shaders) : base(GetNameFromShaders(shaders))
        {
            compiledShaders = shaders;
            glID = Rendering.gl.CreateProgram();

            for (int i = 0; i < shaders.Length; i++)
            {
                Rendering.gl.AttachShader(glID, shaders[i].glID);
            }
            Rendering.gl.LinkProgram(glID);

#if DEBUG
            string infoLog = Rendering.gl.GetProgramInfoLog(glID);
            if (!string.IsNullOrEmpty(infoLog)) Editor.EditorUtils.PrintMessage(Editor.EditorUtils.MessageSeverity.Error,
                $"Could not link shader program {name}", this, infoLog);
#endif

            for (int i = 0; i < shaders.Length; i++)
            {
                Rendering.gl.DetachShader(glID, shaders[i].glID);
            }

            Rendering.gl.GetProgram(glID, ProgramPropertyARB.ActiveUniforms, out int uniformCount);
            List<Texture> texs = new();

            for (uint i = 0; i < uniformCount; i++)
            {
                Rendering.gl.GetActiveUniform(glID, i, 64, out _, out _, out UniformType type, out string name);

                uniforms.Add(name, Rendering.gl.GetUniformLocation(glID, name));

                if (type == UniformType.Bool) boolValues.Add(name, GetBool(name));
                if (type == UniformType.Int) intValues.Add(name, GetInt(name));
                if (type == UniformType.Float) floatValues.Add(name, GetFloat(name));
                if (type == UniformType.FloatVec2) vec2Values.Add(name, GetVec2(name));
                if (type == UniformType.FloatVec3) vec3Values.Add(name, GetVec3(name));
                if (type == UniformType.FloatVec4) vec4Values.Add(name, GetVec4(name));
                if (type == UniformType.FloatMat3) mat3Values.Add(name, GetMat3(name));
                if (type == UniformType.FloatMat4) mat4Values.Add(name, GetMat4(name));

                if (type.ToString().StartsWith("Sampler"))
                {
                    SetInt(name, texs.Count);
                    textures.Item2.Add(name);

                    if (type == UniformType.Sampler2D)
                    {
                        if (name.EndsWith('W')) texs.Add(Texture2D.whiteTex);
                        else if (name.EndsWith('B')) texs.Add(Texture2D.blackTex);
                        else if (name.EndsWith('N')) texs.Add(Texture2D.normalTex);
                        else if (name.EndsWith('G')) texs.Add(Texture2D.grayTex);
                        else texs.Add(null);
                    }
                    else if (type == UniformType.SamplerCube)
                    {
                        if (name.EndsWith('W')) texs.Add(TextureCubemap.whiteTex);
                        else if (name.EndsWith('B')) texs.Add(TextureCubemap.blackTex);
                        else if (name.EndsWith('G')) texs.Add(TextureCubemap.grayTex);
                        else texs.Add(null);
                    }
                    else texs.Add(null);
                }
            }

            textures.Item1 = texs.ToArray();
        }
        ~ShaderProgram()
        {
            Rendering.deletedObjects.Enqueue(() => Rendering.gl.DeleteProgram(glID));
        }
        private static string GetNameFromShaders(Shader[] shaders)
        {
            string name = "";
            for (int i = 0; i < shaders.Length; i++)
            {
                name += shaders[i].name + " : ";
            }
            return name;
        }
        protected override void Create()
        {
            base.Create();

            Utils.SetFieldWithReflection(this, "glID", Rendering.gl.CreateProgram());

            for (int i = 0; i < compiledShaders.Length; i++)
            {
                Rendering.gl.AttachShader(glID, compiledShaders[i].glID);
            }
            Rendering.gl.LinkProgram(glID);

            for (int i = 0; i < compiledShaders.Length; i++)
            {
                Rendering.gl.DetachShader(glID, compiledShaders[i].glID);
            }

            Rendering.gl.GetProgram(glID, ProgramPropertyARB.ActiveUniforms, out int uniformCount);
            List<Texture> texs = new();

            for (uint i = 0; i < uniformCount; i++)
            {
                Rendering.gl.GetActiveUniform(glID, i, 64, out _, out _, out UniformType type, out string name);

                uniforms.Add(name, Rendering.gl.GetUniformLocation(glID, name));

                if (type == UniformType.Bool) boolValues.Add(name, GetBool(name));
                if (type == UniformType.Int) intValues.Add(name, GetInt(name));
                if (type == UniformType.Float) floatValues.Add(name, GetFloat(name));
                if (type == UniformType.FloatVec2) vec2Values.Add(name, GetVec2(name));
                if (type == UniformType.FloatVec3) vec3Values.Add(name, GetVec3(name));
                if (type == UniformType.FloatVec4) vec4Values.Add(name, GetVec4(name));
                if (type == UniformType.FloatMat3) mat3Values.Add(name, GetMat3(name));
                if (type == UniformType.FloatMat4) mat4Values.Add(name, GetMat4(name));

                if (type.ToString().StartsWith("Sampler"))
                {
                    SetInt(name, texs.Count);
                    textures.Item2.Add(name);

                    if (type == UniformType.Sampler2D)
                    {
                        if (name.EndsWith('W')) texs.Add(Texture2D.whiteTex);
                        else if (name.EndsWith('B')) texs.Add(Texture2D.blackTex);
                        else if (name.EndsWith('N')) texs.Add(Texture2D.normalTex);
                        else if (name.EndsWith('G')) texs.Add(Texture2D.grayTex);
                        else texs.Add(null);
                    }
                    else if (type == UniformType.SamplerCube)
                    {
                        if (name.EndsWith('W')) texs.Add(TextureCubemap.whiteTex);
                        else if (name.EndsWith('B')) texs.Add(TextureCubemap.blackTex);
                        else if (name.EndsWith('G')) texs.Add(TextureCubemap.grayTex);
                        else texs.Add(null);
                    }
                    else texs.Add(null);
                }
            }

            Utils.SetFieldWithReflection(this, "textures.Item1", texs.ToArray());
        }

        public void Bind()
        {
            Rendering.gl.UseProgram(glID);
        }
        public void BindTextures()
        {
            for (int i = 0; i < textures.Item1.Length; i++)
            {
                textures.Item1[i]?.Bind(i);
            }
        }
        public void UnbindTextures()
        {
            for (int i = 0; i < textures.Item1.Length; i++)
            {
                textures.Item1[i]?.Unbind(i);
            }
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Rendering.gl.DeleteProgram(glID);
        }
        public Shader this[ShaderType shaderType]
        {
            get { return compiledShaders.FirstOrDefault(s => s.type == shaderType); }
        }
        public Shader this[string shaderPath]
        {
            get { return compiledShaders.FirstOrDefault(s => s.filePath == shaderPath); }
        }

        public void SetTexture(string name, Texture tex)
        {
            if (!textures.Item2.Contains(name)) return;
            textures.Item1[textures.Item2.IndexOf(name)] = tex;
        }
        public Texture GetTexture(string name)
        {
            if (!textures.Item2.Contains(name)) return null;
            return textures.Item1[textures.Item2.IndexOf(name)];
        }
        public void SetBool(string name, bool val)
        {
            Rendering.gl.UseProgram(glID);
            Rendering.gl.Uniform1(uniforms[name], val ? 1 : 0);
        }
        public bool GetBool(string name)
        {
            Rendering.gl.UseProgram(glID);
            Rendering.gl.GetUniform(glID, uniforms[name], out int i);
            return i > 0;
        }
        public void SetInt(string name, int val)
        {
            Rendering.gl.UseProgram(glID);
            Rendering.gl.Uniform1(uniforms[name], val);
        }
        public int GetInt(string name)
        {
            Rendering.gl.UseProgram(glID);
            Rendering.gl.GetUniform(glID, uniforms[name], out int i);
            return i;
        }
        public void SetFloat(string name, float val)
        {
            Rendering.gl.UseProgram(glID);
            Rendering.gl.Uniform1(uniforms[name], val);
        }
        public float GetFloat(string name)
        {
            Rendering.gl.UseProgram(glID);
            Rendering.gl.GetUniform(glID, uniforms[name], out float i);
            return i;
        }
        public void SetVec2(string name, Vector2D<float> val)
        {
            Rendering.gl.UseProgram(glID);
            Rendering.gl.Uniform2(uniforms[name], val.X, val.Y);
        }
        public Vector2D<float> GetVec2(string name)
        {
            Rendering.gl.UseProgram(glID);
            float* i = stackalloc float[2];
            Rendering.gl.GetnUniform(glID, uniforms[name], 2 * sizeof(float), i);
            Vector2D<float> v = new(i[0], i[1]);
            return v;
        }
        public void SetVec3(string name, Vector3D<float> val)
        {
            Rendering.gl.UseProgram(glID);
            Rendering.gl.Uniform3(uniforms[name], val.X, val.Y, val.Z);
        }
        public Vector3D<float> GetVec3(string name)
        {
            Rendering.gl.UseProgram(glID);
            float* i = stackalloc float[3];
            Rendering.gl.GetnUniform(glID, uniforms[name], 3 * sizeof(float), i);
            Vector3D<float> v = new(i[0], i[1], i[2]);
            return v;
        }
        public void SetVec4(string name, Vector4D<float> val)
        {
            Rendering.gl.UseProgram(glID);
            Rendering.gl.Uniform4(uniforms[name], val.X, val.Y, val.Z, val.W);
        }
        public Vector4D<float> GetVec4(string name)
        {
            Rendering.gl.UseProgram(glID);
            float* i = stackalloc float[4];
            Rendering.gl.GetnUniform(glID, uniforms[name], 4 * sizeof(float), i);
            Vector4D<float> v = new(i[0], i[1], i[2], i[3]);
            return v;
        }
        public void SetMat3(string name, Matrix3X3<float> val)
        {
            Rendering.gl.UseProgram(glID);
            Span<float> span = new(&val.Row1.X, 9);
            Rendering.gl.UniformMatrix3(uniforms[name], 1, true, span);
        }
        public Matrix3X3<float> GetMat3(string name)
        {
            Rendering.gl.UseProgram(glID);
            float* i = stackalloc float[9];
            Rendering.gl.GetnUniform(glID, uniforms[name], 9 * sizeof(float), i);
            Matrix3X3<float> m = Matrix3X3.Transpose(new Matrix3X3<float>(i[0], i[1], i[2], i[3], i[4], i[5], i[6], i[7], i[8]));
            return m;
        }
        public void SetMat4(string name, Matrix4X4<float> val)
        {
            Rendering.gl.UseProgram(glID);
            Span<float> span = new(&val.Row1.X, 16);
            Rendering.gl.UniformMatrix4(uniforms[name], 1, false, span);
        }
        public Matrix4X4<float> GetMat4(string name)
        {
            Rendering.gl.UseProgram(glID);
            float* i = stackalloc float[16];
            Rendering.gl.GetnUniform(glID, uniforms[name], 16 * sizeof(float), i);
            Matrix4X4<float> m = Matrix4X4.Transpose(new Matrix4X4<float>(i[0], i[1], i[2], i[3],
                i[4], i[5], i[6], i[7], i[8], i[9], i[10], i[11], i[12], i[13], i[14], i[15]));
            return m;
        }

        public void SetBoolCache(string name, bool val)
        {
            if (!boolValues.ContainsKey(name)) return;
            if (boolValues[name] == val) return;
            SetBool(name, val);
            boolValues[name] = val;
        }
        public bool GetBoolCache(string name)
        {
            if (!boolValues.ContainsKey(name)) return default;
            return boolValues[name];
        }
        public void SetIntCache(string name, int val)
        {
            if (!intValues.ContainsKey(name)) return;
            if (intValues[name] == val) return;
            SetInt(name, val);
            intValues[name] = val;
        }
        public int GetIntCache(string name)
        {
            if (!intValues.ContainsKey(name)) return default;
            return intValues[name];
        }
        public void SetFloatCache(string name, float val)
        {
            if (!floatValues.ContainsKey(name)) return;
            if (floatValues[name] == val) return;
            SetFloat(name, val);
            floatValues[name] = val;
        }
        public float GetFloatCache(string name)
        {
            if (!floatValues.ContainsKey(name)) return default;
            return floatValues[name];
        }
        public void SetVec2Cache(string name, Vector2D<float> val)
        {
            if (!vec2Values.ContainsKey(name)) return;
            if (vec2Values[name] == val) return;
            SetVec2(name, val);
            vec2Values[name] = val;
        }
        public Vector2D<float> GetVec2Cache(string name)
        {
            if (!vec2Values.ContainsKey(name)) return default;
            return vec2Values[name];
        }
        public void SetVec3Cache(string name, Vector3D<float> val)
        {
            if (!vec3Values.ContainsKey(name)) return;
            if (vec3Values[name] == val) return;
            SetVec3(name, val);
            vec3Values[name] = val;
        }
        public Vector3D<float> GetVec3Cache(string name)
        {
            if (!vec3Values.ContainsKey(name)) return default;
            return vec3Values[name];
        }
        public void SetVec4Cache(string name, Vector4D<float> val)
        {
            if (!vec4Values.ContainsKey(name)) return;
            if (vec4Values[name] == val) return;
            SetVec4(name, val);
            vec4Values[name] = val;
        }
        public Vector4D<float> GetVec4Cache(string name)
        {
            if (!vec4Values.ContainsKey(name)) return default;
            return vec4Values[name];
        }
        public void SetMat3Cache(string name, Matrix3X3<float> val)
        {
            if (!mat3Values.ContainsKey(name)) return;
            if (mat3Values[name] == val) return;
            SetMat3(name, val);
            mat3Values[name] = val;
        }
        public Matrix3X3<float> GetMat3Cache(string name)
        {
            if (!mat3Values.ContainsKey(name)) return default;
            return mat3Values[name];
        }
        public void SetMat4Cache(string name, Matrix4X4<float> val)
        {
            if (!mat4Values.ContainsKey(name)) return;
            if (mat4Values[name] == val) return;
            SetMat4(name, val);
            mat4Values[name] = val;
        }
        public Matrix4X4<float> GetMat4Cache(string name)
        {
            if (!mat4Values.ContainsKey(name)) return default;
            return mat4Values[name];
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;
using Silk.NET.Windowing;
using HawkEngine.Core;
using HawkEngine.Editor;
using HawkEngine.Components;
using System.Drawing;
using Silk.NET.Maths;
using Silk.NET.Assimp;

namespace HawkEngine.Graphics
{
    public static class Rendering
    {
        public static GL gl { get; set; }

        public static event Action<DebugSource, DebugType, int, string> glDebugCallback;

        public static CameraComponent outputCam { get; set; }

        public static Mesh quad { get; private set; }
        public static ShaderProgram outputShader { get; private set; }
        public static readonly List<ShaderProgram> postProcessShaders = new();
        public static Framebuffer postProcessFB { get; private set; }

        public static Model skyboxModel { get; set; }
        public static TextureCubemap skyboxTexture { get; set; }

        public static float gamma { get; set; } = 2.2f;
        public static float exposure { get; set; } = 1f;
        public static float tonemapStrength { get; set; } = 1f;

        public static void Init()
        {
            gl = App.window.CreateOpenGL();

            App.window.FramebufferResize += OnWindowResize; ;

#if DEBUG
            gl.Enable(EnableCap.DebugOutputSynchronous);
            gl.DebugMessageCallback(GLDebugMessage, nint.Zero);
#endif

            gl.Enable(EnableCap.CullFace);
            gl.Enable(EnableCap.TextureCubeMapSeamless);
            gl.Enable(EnableCap.DepthTest);
            gl.Enable(EnableCap.Blend);

            gl.DepthFunc(DepthFunction.Lequal);

            CreateStandardResources();
        }
        private static void OnWindowResize(Vector2D<int> size)
        {
            gl.Viewport(size);
            CreatePPFB();
        }
        public static void CreatePPFB()
        {
            postProcessFB = new(new FramebufferTexture((uint)App.window.FramebufferSize.X, (uint)App.window.FramebufferSize.Y,
                FramebufferAttachment.ColorAttachment0, InternalFormat.Rgba16f, PixelFormat.Rgba));
        }
        public static void CreateStandardResources()
        {
            CreatePPFB();

            quad = new("../../../Resources/Models/Quad.obj");
            outputShader = new(Shader.Create("../../../Shaders/OutputVert.glsl", ShaderType.VertexShader),
                Shader.Create("../../../Shaders/OutputFrag.glsl", ShaderType.FragmentShader));

            skyboxModel = new(new(Shader.Create("../../../Shaders/SkyboxVert.glsl", ShaderType.VertexShader),
                Shader.Create("../../../Shaders/SkyboxFrag.glsl", ShaderType.FragmentShader)), new("../../../Resources/Models/Inverted Cube.obj"));
            skyboxTexture = new(new string[6]
                {
                    "../../../Resources/Images/Skybox/right.jpg",
                    "../../../Resources/Images/Skybox/left.jpg",
                    "../../../Resources/Images/Skybox/bottom.jpg",
                    "../../../Resources/Images/Skybox/top.jpg",
                    "../../../Resources/Images/Skybox/front.jpg",
                    "../../../Resources/Images/Skybox/back.jpg",
                });
        }
        public static unsafe void Render()
        {
            outputCam.framebuffer.Bind();
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            skyboxModel.shader.SetMat4Cache("uViewMat", outputCam.viewMat);
            skyboxModel.shader.SetMat4Cache("uProjMat", outputCam.projectionMat);
            skyboxModel.shader.textures.Item1[0] = skyboxTexture;
            skyboxModel.Render();

            List<MeshComponent> meshes = App.scene.FindComponents<MeshComponent>();
            List<LightComponent> lights = App.scene.FindComponents<LightComponent>();

            for (int m = 0; m < meshes.Count; m++)
            {
                IOrderedEnumerable<LightComponent> orderedLights =
                    lights.OrderBy(l => Math.Min((int)l.type, 1) * Vector3D.DistanceSquared(meshes[m].transform.position, l.transform.position));

                for (int l = 0; l < 5; l++)
                {
                    if (orderedLights.Count() > l)
                    {
                        LightComponent light = orderedLights.ElementAt(l);

                        meshes[m].shader.SetIntCache($"uLights[{l}].uType", (int)light.type);
                        meshes[m].shader.SetVec3Cache($"uLights[{l}].uPosition",
                            light.type == LightType.Directional ? light.transform.forward : light.transform.position);
                        meshes[m].shader.SetVec3Cache($"uLights[{l}].uColor", light.output);
                        meshes[m].shader.SetVec2Cache($"uLights[{l}].uFalloff", light.falloff);
                    }
                    else meshes[m].shader.SetVec3Cache($"uLights[{l}].uColor", Vector3D<float>.Zero);
                }
                
                meshes[m].SetUniforms();
                meshes[m].Render();
                meshes[m].Cleanup();
            }

            gl.BlitNamedFramebuffer(outputCam.framebuffer.id, postProcessFB.id, 0, 0, outputCam.size.X, outputCam.size.Y, 0, 0, App.window.FramebufferSize.X,
                App.window.FramebufferSize.Y, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            postProcessFB.Bind();

            for (int i = 0; i < postProcessShaders.Count; i++)
            {
                Model ppModel = new(postProcessShaders[i], quad);
                ppModel.shader.SetTexture("uColorTex", postProcessFB.attachments[0]);
                ppModel.Render();
            }

            postProcessFB.Unbind();
            gl.Clear(ClearBufferMask.ColorBufferBit);

            Model outputModel = new(outputShader, quad);
            outputModel.shader.SetTexture("uColorTex", postProcessFB.attachments[0]);
            outputModel.shader.SetFloatCache("uGamma", gamma);
            outputModel.shader.SetFloatCache("uExposure", exposure);
            outputModel.shader.SetFloatCache("uTonemapStrength", tonemapStrength);
            outputModel.Render();
        }
        private static unsafe void GLDebugMessage(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userParam)
        {
            DebugSource dSource = (DebugSource)source;
            DebugType dType = (DebugType)type;

            string dMessage = Encoding.UTF8.GetString((byte*)message.ToPointer(), length);

            Console.WriteLine($"{dType.ToString().TrimStart("DebugType".ToCharArray()).ToUpper()} {
                id} in {dSource.ToString().TrimStart("DebugSource".ToCharArray())}: {dMessage}");
            glDebugCallback?.Invoke(dSource, dType, id, dMessage);
        }
    }
}

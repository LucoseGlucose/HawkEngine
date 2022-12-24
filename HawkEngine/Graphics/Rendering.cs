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

namespace HawkEngine.Graphics
{
    public static class Rendering
    {
        public static GL gl { get; set; }

        public static event Action<DebugSource, DebugType, int, string> glDebugCallback;
        public static readonly Queue<Action> deletedObjects = new();

        public static CameraComponent outputCam { get; set; }

        public static Mesh quad { get; private set; }
        public static ShaderProgram outputShader { get; private set; }
        public static readonly Dictionary<string, ShaderProgram> postProcessShaders = new();
        public static Framebuffer postProcessFB { get; private set; }

        public static Mesh skyboxMesh { get; private set; }
        public static ShaderProgram skyboxShader { get; set; }
        public static Skybox skybox { get; set; }
        public static Vector3D<float> ambientColor { get; set; } = new(1f);

        public static void Init()
        {
            gl = App.window.CreateOpenGL();

            App.window.FramebufferResize += OnWindowResize;

#if DEBUG
            gl.Enable(EnableCap.DebugOutputSynchronous);
            gl.DebugMessageCallback(GLDebugMessage, nint.Zero);
#endif

            gl.Enable(EnableCap.CullFace);
            gl.Enable(EnableCap.TextureCubeMapSeamless);
            gl.Enable(EnableCap.DepthTest);
            gl.Enable(EnableCap.Blend);

            gl.DepthFunc(DepthFunction.Lequal);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            CreateStandardResources();
        }
        private static void OnWindowResize(Vector2D<int> size)
        {
            CreatePPFB();
        }
        public static void CreatePPFB()
        {
            postProcessFB = new(new FramebufferTexture(new Texture2D((uint)App.window.FramebufferSize.X, (uint)App.window.FramebufferSize.Y,
                InternalFormat.Rgba16f, PixelFormat.Rgba), FramebufferAttachment.ColorAttachment0));
        }
        public static void CreateStandardResources()
        {
            CreatePPFB();

            quad = new("Models/Quad.obj");
            outputShader = new(Shader.Create("Shaders/Post Processing/OutputVert.glsl", ShaderType.VertexShader),
                Shader.Create("Shaders/Post Processing/OutputFrag.glsl", ShaderType.FragmentShader));

            skyboxMesh = new("Models/Inverted Cube.obj");
            skyboxShader = new(Shader.Create("Shaders/Skybox/SkyboxVert.glsl", ShaderType.VertexShader),
                Shader.Create("Shaders/Skybox/SkyboxFrag.glsl", ShaderType.FragmentShader));

            postProcessShaders.Add("Color Adjustments", new(Shader.Create("Shaders/Post Processing/OutputVert.glsl",
                    ShaderType.VertexShader), Shader.Create("Shaders/Post Processing/ColorAdjustments.glsl", ShaderType.FragmentShader)));

            skybox = new("Images/limpopo_golf_course_4k.hdr", 2048u, 64u, 512u);
        }
        public static unsafe void Render()
        {
            List<MeshComponent> meshes = App.scene.FindComponents<MeshComponent>();
            List<LightComponent> lights = App.scene.FindComponents<LightComponent>();

            for (int l = 0; l < lights.Count; l++)
            {
                if (lights[l].shadowsEnabled) lights[l].RenderShadowMap(meshes);
            }

            outputCam.framebuffer.Bind();
            gl.Viewport(outputCam.size);
            gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Model skyboxModel = new(skyboxShader, skyboxMesh);

            skyboxShader.SetTexture("uSkyboxW", skybox.skybox);
            skyboxModel.shader.SetMat4Cache("uViewMat", outputCam.viewMat);
            skyboxModel.shader.SetMat4Cache("uProjMat", outputCam.projectionMat);
            skyboxModel.Render();

            for (int m = 0; m < meshes.Count; m++)
            {
                if (meshes[m].lightingEnabled)
                {
                    meshes[m].shader.SetTexture("uIrradianceCubeB", skybox.irradiance);
                    meshes[m].shader.SetTexture("uReflectionCubeB", skybox.specularReflections);
                    meshes[m].shader.SetTexture("uBrdfLutB", Texture2D.brdfTex);
                    meshes[m].shader.SetVec3Cache("uAmbientColor", ambientColor);

                    IOrderedEnumerable<LightComponent> orderedLights =
                        lights.OrderBy(l => Math.Min(l.type, 1) * Vector3D.DistanceSquared(meshes[m].transform.position, l.transform.position));

                    for (int l = 0; l < 5; l++)
                    {
                        if (orderedLights.Count() > l) orderedLights.ElementAt(l).SetUniforms($"uLights[{l}]", meshes[m].shader);
                        else meshes[m].shader.SetIntCache($"uLights[{l}].uType", 0);
                    }
                }

                meshes[m].SetUniforms();
                meshes[m].Render();
                meshes[m].Cleanup();
            }

            gl.BlitNamedFramebuffer(outputCam.framebuffer.id, postProcessFB.id, 0, 0, outputCam.size.X, outputCam.size.Y, 0, 0, App.window.FramebufferSize.X,
                App.window.FramebufferSize.Y, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            postProcessFB.Bind();

            foreach (KeyValuePair<string, ShaderProgram> shader in postProcessShaders)
            {
                Model ppModel = new(postProcessShaders[shader.Key], quad);
                ppModel.shader.SetTexture("uColorTex", postProcessFB[FramebufferAttachment.ColorAttachment0].texture);
                ppModel.Render();
            }

            postProcessFB.Unbind();
            gl.Clear(ClearBufferMask.ColorBufferBit);

            Model outputModel = new(outputShader, quad);
            outputModel.shader.SetTexture("uColorTex", postProcessFB[FramebufferAttachment.ColorAttachment0].texture);
            outputModel.Render();

            int deleteCount = deletedObjects.Count;
            for (int i = 0; i < deleteCount; i++)
            {
                deletedObjects.Dequeue()?.Invoke();
            }
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

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
        public static Model outputModel { get; private set; }

        public static Framebuffer postProcessFB { get; private set; }

        public delegate void Pass(List<MeshComponent> meshes, List<LightComponent> lights);
        public static readonly List<Pass> renderPasses = new();

        public static Mesh skyboxMesh { get; private set; }
        public static ShaderProgram skyboxShader { get; set; }
        public static Model skyboxModel { get; set; }

        public static Skybox skybox { get; set; }
        public static Vector3D<float> ambientColor { get; set; } = new(1f);

        public static ShaderProgram colorAdjustmentsShader { get; private set; }
        public static Bloom bloom { get; private set; }

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
            outputShader = new("Shaders/Post Processing/OutputVert.glsl", "Shaders/Post Processing/OutputFrag.glsl");
            outputModel = new(outputShader, quad);

            skyboxMesh = new("Models/Inverted Cube.obj");
            skyboxShader = new("Shaders/Skybox/SkyboxVert.glsl", "Shaders/Skybox/SkyboxFrag.glsl");
            skyboxModel = new(skyboxShader, skyboxMesh);

            skybox = new("Images/limpopo_golf_course_4k.hdr", 2048u, 64u, 512u);
            colorAdjustmentsShader = new("Shaders/Post Processing/OutputVert.glsl", "Shaders/Post Processing/ColorAdjustments.glsl");
            bloom = new(5);

            renderPasses.Add(RenderPass.shadowPass);
            renderPasses.Add(RenderPass.preMainDrawPass);
            renderPasses.Add(RenderPass.skyboxPass);
            renderPasses.Add(RenderPass.scenePass);
            renderPasses.Add(RenderPass.prePostProcessPass);
            renderPasses.Add(RenderPass.bloomPass);
            renderPasses.Add(RenderPass.BlitScreenPass(colorAdjustmentsShader));
            renderPasses.Add(RenderPass.outputPass);
        }
        public static unsafe void Render()
        {
            List<MeshComponent> meshes = App.scene.FindComponents<MeshComponent>();
            List<LightComponent> lights = App.scene.FindComponents<LightComponent>();

            foreach (Pass pass in renderPasses)
            {
                pass?.Invoke(meshes, lights);
            }

            CleanupDeletedObjects();
        }
        public static void CleanupDeletedObjects()
        {
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

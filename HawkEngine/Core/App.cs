using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Input;
using HawkEngine.Graphics;
using Silk.NET.GLFW;
using HawkEngine.Editor;

namespace HawkEngine.Core
{
    public static class App
    {
        public static IWindow window { get; private set; }
        public static IInputContext input { get; private set; }

        public static float deltaTime { get; private set; }
        public static float totalTime { get; private set; }

        public static Scene scene { get; set; }
        private static Func<Scene> sceneFunc;

        public static void Run(Func<Scene> func)
        {
            sceneFunc = func;

            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.API = new(ContextAPI.OpenGL, new(4, 6));
            windowOptions.Size = new Vector2D<int>(1280, 720);
            windowOptions.Title = "HawkEngine";
            windowOptions.PreferredDepthBufferBits = 0;
            windowOptions.PreferredStencilBufferBits = 0;

#if DEBUG
            GlfwProvider.GLFW.Value.WindowHint(WindowHintBool.OpenGLDebugContext, true);
#endif
            window = Window.Create(windowOptions);

            window.Load += OnLoad;
            window.Update += OnUpdate;
            window.Render += OnRender;
            window.Closing += OnClose;

            window.Run();
        }

        private static void OnLoad()
        {
            input = window.CreateInput();
            Rendering.Init();
            scene = sceneFunc();
        }
        private static void OnUpdate(double delta)
        {
            deltaTime = (float)delta;
            totalTime += deltaTime;

            scene.Update();
        }
        private static void OnRender(double delta)
        {
            Rendering.Render();
        }
        private static void OnClose()
        {
            window.Dispose();
        }
    }
}

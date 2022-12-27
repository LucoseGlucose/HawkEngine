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
using StbImageSharp;
using System.Drawing;
using System.IO;
using Silk.NET.Core;
using Silk.NET.OpenGL;

namespace HawkEngine.Core
{
    public static class App
    {
        public static IWindow window { get; private set; }
        public static IInputContext input { get; private set; }

        public static Scene scene { get; set; }
        private static Func<Scene> sceneFunc;

        public static bool canFullscreen { get; set; } = true;
        public static Key fullscreenKey { get; set; } = Key.F11;

        public static void Run(Func<Scene> func)
        {
            sceneFunc = func;

            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.API = new(ContextAPI.OpenGL, new(4, 6));
            windowOptions.Size = new Vector2D<int>(1280, 720);
            windowOptions.Title = "HawkEngine";
            windowOptions.PreferredDepthBufferBits = 0;
            windowOptions.PreferredStencilBufferBits = 0;
            windowOptions.VSync = false;

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
            GL gl = window.CreateOpenGL();

            input = window.CreateInput();
            if (input.Keyboards.Count > 0)
            {
                input.Keyboards[0].KeyDown += (keyboard, key, i) =>
                {
                    if (!canFullscreen || key != fullscreenKey) return;
                    window.WindowState = window.WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
                };
            }

            unsafe
            {
                using Stream file = File.OpenRead(Path.GetFullPath("../../../Resources/Images/HawkEngine Icon.png"));

                StbImage.stbi__result_info result;
                int width;
                int height;
                int components;

                void* data = StbImage.stbi__load_main(new StbImage.stbi__context(file), &width, &height, &components, 4, &result, 8);
                ReadOnlySpan<byte> span = new(data, width * height * 4);

                Memory<byte> mem = new(span.ToArray());
                RawImage icon = new(width, height, mem);
                window.SetWindowIcon(ref icon);
            }

#if DEBUG
            Editor.EditorGUI.Init(gl);
#endif

            Rendering.Init();
            scene = sceneFunc();

#if DEBUG
            Editor.HawkEditor.Init();
#endif
        }
        private static void OnUpdate(double delta)
        {
            Time.Update((float)delta);
            scene.Update();
#if DEBUG
            Editor.HawkEditor.Update();
#endif
        }
        private static void OnRender(double delta)
        {
            Rendering.Render();
#if DEBUG
            Editor.HawkEditor.Render();
#endif
        }
        private static void OnClose()
        {
#if DEBUG
            Editor.HawkEditor.Close();
#endif
            window.Dispose();
        }
    }
}

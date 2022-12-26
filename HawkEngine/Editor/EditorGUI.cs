#if DEBUG
using HawkEngine.Core;
using HawkEngine.Graphics;
using ImGuiNET;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static ImGuiNET.ImGui;

namespace HawkEngine.Editor
{
    public static class EditorGUI
    {
        public static ImGuiController imgui { get; private set; }
        public static ImGuiStylePtr style { get; private set; }
        public static ImGuiIOPtr io { get; private set; }
        public static Vector2D<int> renderWindowSize { get; private set; }
        public static readonly List<EditorWindow> windows = new();
        public static EditorWindow activeWindow { get; set; }

        public static void Init()
        {
            imgui = new(Rendering.gl, App.window, App.input);

            io = GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            io.ConfigWindowsMoveFromTitleBarOnly = true;

            style = GetStyle();
            style.WindowMenuButtonPosition = ImGuiDir.None;

            windows.Add(new EditorViewport(true));
        }
        public static void Update()
        {
            imgui.Update(Time.unscaledDeltaTime);

            BeginMainMenuBar();
            if (BeginMenu("Window"))
            {
                MenuItem("Viewport", "", ref FindWindow<EditorViewport>().open);
                EndMenu();
            }
            EndMainMenuBar();

            DockSpaceOverViewport();

            foreach (EditorWindow window in windows)
            {
                window.Update();
            }
        }
        public static void Render()
        {
            imgui.Render();
        }
        public static EditorWindow FindWindow<T>() where T : EditorWindow
        {
            return windows.OfType<T>().FirstOrDefault();
        }
        public static EditorWindow FindOpenWindow<T>() where T : EditorWindow
        {
            return windows.OfType<T>().Where(w => w.open).FirstOrDefault();
        }
    }
}
#endif
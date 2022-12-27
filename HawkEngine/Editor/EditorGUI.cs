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

        public static void Init(GL gl)
        {
            imgui = new(gl, App.window, App.input);
            LoadIniSettingsFromDisk("imgui.ini");

            io = GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            io.ConfigWindowsMoveFromTitleBarOnly = true;

            style = GetStyle();
            style.WindowMenuButtonPosition = ImGuiDir.None;

            windows.Add(EditorWindow.viewport);
            windows.Add(EditorWindow.sceneTree);

            Update();
        }
        public static void Update()
        {
            imgui.Update(Time.unscaledDeltaTime);

            BeginMainMenuBar();
            if (BeginMenu("Window"))
            {
                foreach (EditorWindow window in windows)
                {
                    MenuItem(window.title, "", ref FindWindow(window.title).open);
                }
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
        public static void Close()
        {
            SaveIniSettingsToDisk("imgui.ini");
        }
        public static EditorWindow FindWindow(string title)
        {
            return windows.FirstOrDefault(w => w.title == title);
        }
    }
}
#endif
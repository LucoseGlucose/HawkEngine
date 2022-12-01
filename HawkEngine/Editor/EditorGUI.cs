using HawkEngine.Core;
using HawkEngine.Graphics;
using ImGuiNET;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Editor
{
    public static class EditorGUI
    {
        public static ImGuiController imgui { get; private set; }

        public static void Init()
        {
            imgui = new(Rendering.gl, App.window, App.input);

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            ImGuiStylePtr style = ImGui.GetStyle();
            style.WindowMenuButtonPosition = ImGuiDir.None;
        }
        public static void Update()
        {
            imgui.Update(App.deltaTime);
        }
        public static void Render()
        {
            imgui.Render();
        }
    }
}

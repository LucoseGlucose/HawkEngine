#if DEBUG
using HawkEngine.Core;
using HawkEngine.Graphics;
using ImGuiNET;
using Silk.NET.Assimp;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static ImGuiNET.ImGui;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HawkEngine.Editor
{
    public static class EditorGUI
    {
        public static ImGuiController imgui { get; private set; }
        public static ImGuiStylePtr style { get; private set; }
        public static ImGuiIOPtr io { get; private set; }
        public static readonly List<EditorWindow> windows = new();
        public static EditorWindow activeWindow { get; set; }

        public static void Init(GL gl)
        {
            imgui = new(gl, App.window, App.input);
            LoadIniSettingsFromDisk("imgui.ini");

            io = GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            io.ConfigWindowsMoveFromTitleBarOnly = true;

            SetDefaultFont("Fonts/Ubuntu/Ubuntu-Regular.ttf", gl);
            style = GetStyle();
            style.WindowMenuButtonPosition = ImGuiDir.None;

            windows.Add(new EditorViewport());
            windows.Add(new EditorSceneTree());
            windows.Add(new EditorConsole());
            windows.Add(new EditorStats());

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
        public static T FindWindow<T>() where T : EditorWindow
        {
            return windows.OfType<T>().FirstOrDefault();
        }
        private static unsafe void RecreateFontDeviceTexture(GL gl)
        {
            io.Fonts.GetTexDataAsRGBA32(out nint pixels, out int width, out int height, out int bytesPerPixel);

            byte[] bytes = new byte[width * height * bytesPerPixel];
            Marshal.Copy(pixels, bytes, 0, bytes.Length);

            uint id = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, id);
            gl.TexImage2D<byte>(TextureTarget.Texture2D, 0, InternalFormat.Rgba8,
                (uint)width, (uint)height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, bytes);

            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (uint)GLEnum.Linear);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (uint)GLEnum.Linear);

            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (uint)GLEnum.ClampToEdge);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (uint)GLEnum.ClampToEdge);

            gl.BindTexture(TextureTarget.Texture2D, 0);
            io.Fonts.SetTexID((nint)id);
            io.Fonts.ClearTexData();
        }
        public static unsafe void SetDefaultFont(string font, GL gl)
        {
            io.NativePtr->FontDefault = io.Fonts.AddFontFromFileTTF(Path.GetFullPath("../../../Resources/" + font), 14f).NativePtr;
            RecreateFontDeviceTexture(gl);
        }
    }
}
#endif
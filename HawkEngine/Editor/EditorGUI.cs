#if DEBUG
using HawkEngine.Core;
using HawkEngine.Graphics;
using ImGuiNET;
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

namespace HawkEngine.Editor
{
    public static class EditorGUI
    {
        public static ImGuiController imgui { get; private set; }
        public static ImGuiStylePtr style { get; private set; }
        public static ImGuiIOPtr io { get; private set; }
        public static readonly List<EditorWindow> windows = new();
        public static EditorWindow activeWindow { get; set; }
        public static readonly Dictionary<string, EditorFont> availableFonts = new();

        public static unsafe void Init(GL gl)
        {
            imgui = new(gl, App.window, App.input);
            LoadIniSettingsFromDisk("imgui.ini");
            InitStyle();

            io = GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            io.ConfigWindowsMoveFromTitleBarOnly = true;

            string iconPath = Path.GetFullPath("../../../Resources/Fonts/Icons/kenney-icon-font.ttf");

            availableFonts.Add("Calibri", new
            (
                new EditorUtils.EditorFontData("C:/Windows/Fonts/calibri.ttf", 15f, EditorUtils.FontStyle.Regular, EditorUtils.FontSize.Medium)
                    .MergeFonts(iconPath, 15f, Kenney.IconMin, Kenney.IconMax),
                new EditorUtils.EditorFontData("C:/Windows/Fonts/calibrib.ttf", 15f, EditorUtils.FontStyle.Bold, EditorUtils.FontSize.Medium)
                    .MergeFonts(iconPath, 15f, Kenney.IconMin, Kenney.IconMax),
                new EditorUtils.EditorFontData("C:/Windows/Fonts/calibrii.ttf", 15f, EditorUtils.FontStyle.Italic, EditorUtils.FontSize.Medium)
                    .MergeFonts(iconPath, 15f, Kenney.IconMin, Kenney.IconMax),
                new EditorUtils.EditorFontData("C:/Windows/Fonts/calibribi.ttf", 15f, EditorUtils.FontStyle.BoldItalic, EditorUtils.FontSize.Medium)
                    .MergeFonts(iconPath, 15f, Kenney.IconMin, Kenney.IconMax),
                new EditorUtils.EditorFontData("C:/Windows/Fonts/calibri.ttf", 11f, EditorUtils.FontStyle.Regular, EditorUtils.FontSize.Small)
                    .MergeFonts(iconPath, 11f, Kenney.IconMin, Kenney.IconMax),
                new EditorUtils.EditorFontData("C:/Windows/Fonts/calibrib.ttf", 11f, EditorUtils.FontStyle.Bold, EditorUtils.FontSize.Small)
                    .MergeFonts(iconPath, 11f, Kenney.IconMin, Kenney.IconMax),
                new EditorUtils.EditorFontData("C:/Windows/Fonts/calibrii.ttf", 11f, EditorUtils.FontStyle.Italic, EditorUtils.FontSize.Small)
                    .MergeFonts(iconPath, 11f, Kenney.IconMin, Kenney.IconMax),
                new EditorUtils.EditorFontData("C:/Windows/Fonts/calibribi.ttf", 11f, EditorUtils.FontStyle.BoldItalic, EditorUtils.FontSize.Small)
                    .MergeFonts(iconPath, 11f, Kenney.IconMin, Kenney.IconMax),
                new EditorUtils.EditorFontData("C:/Windows/Fonts/calibri.ttf", 19f, EditorUtils.FontStyle.Regular, EditorUtils.FontSize.Large)
                    .MergeFonts(iconPath, 19f, Kenney.IconMin, Kenney.IconMax),
                new EditorUtils.EditorFontData("C:/Windows/Fonts/calibrib.ttf", 19f, EditorUtils.FontStyle.Bold, EditorUtils.FontSize.Large)
                    .MergeFonts(iconPath, 19f, Kenney.IconMin, Kenney.IconMax),
                new EditorUtils.EditorFontData("C:/Windows/Fonts/calibrii.ttf", 19f, EditorUtils.FontStyle.Italic, EditorUtils.FontSize.Large)
                    .MergeFonts(iconPath, 19f, Kenney.IconMin, Kenney.IconMax),
                new EditorUtils.EditorFontData("C:/Windows/Fonts/calibribi.ttf", 19f, EditorUtils.FontStyle.BoldItalic, EditorUtils.FontSize.Large)
                    .MergeFonts(iconPath, 19f, Kenney.IconMin, Kenney.IconMax)
            ));

            availableFonts["Calibri"].SetAsDefault();
            RecreateFontDeviceTexture(gl);

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

            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (uint)GLEnum.Nearest);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (uint)GLEnum.Nearest);

            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (uint)GLEnum.ClampToEdge);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (uint)GLEnum.ClampToEdge);

            gl.BindTexture(TextureTarget.Texture2D, 0);
            io.Fonts.SetTexID((nint)id);
            io.Fonts.ClearTexData();
        }
        private static void InitStyle()
        {
            style = GetStyle();

            style.WindowPadding = new(10f);
            style.FramePadding = new(8f, 4f);
            style.CellPadding = new(4f, 2f);
            style.ItemSpacing = new(8f, 4f);
            style.ItemInnerSpacing = new(4f);
            style.TouchExtraPadding = new(0f);
            style.IndentSpacing = 21f;
            style.ScrollbarSize = 12f;
            style.GrabMinSize = 10f;

            style.WindowBorderSize = 1f;
            style.ChildBorderSize = 0f;
            style.PopupBorderSize = 0f;
            style.FrameBorderSize = 0f;
            style.TabBorderSize = 0f;

            style.WindowRounding = 4f;
            style.ChildRounding = 4f;
            style.FrameRounding = 2f;
            style.ScrollbarRounding = 8f;
            style.GrabRounding = 2f;
            style.LogSliderDeadzone = 4f;
            style.TabRounding = 2f;

            style.WindowTitleAlign = new(0f, .5f);
            style.WindowMenuButtonPosition = ImGuiDir.None;
            style.ColorButtonPosition = ImGuiDir.Right;
            style.ButtonTextAlign = new(.5f);
            style.SelectableTextAlign = new(0f, .5f);
            style.DisplaySafeAreaPadding = new(3f);

            RangeAccessor<Vector4> colors = style.Colors;

            colors[(int)ImGuiCol.Text] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.06f, 0.06f, 0.06f, 1.00f);
            colors[(int)ImGuiCol.ChildBg] = new Vector4(0.06f, 0.06f, 0.06f, 1.00f);
            colors[(int)ImGuiCol.PopupBg] = new Vector4(0.06f, 0.06f, 0.06f, 0.78f);
            colors[(int)ImGuiCol.Border] = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.16f, 0.16f, 0.16f, 1.00f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.55f, 0.55f, 0.55f, 1.00f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.00f, 0.00f, 0.78f, 1.00f);
            colors[(int)ImGuiCol.TitleBg] = new Vector4(0.06f, 0.06f, 0.06f, 1.00f);
            colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.10f, 0.10f, 0.10f, 1.00f);
            colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.06f, 0.06f, 0.06f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.29f, 0.29f, 0.29f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.55f, 0.55f, 0.55f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.00f, 0.78f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.CheckMark] = new Vector4(0.90f, 0.90f, 0.90f, 1.00f);
            colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.90f, 0.78f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.90f, 0.90f, 0.90f, 1.00f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(1.00f, 0.59f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(1.00f, 0.00f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.Header] = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(1.00f, 0.59f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(1.00f, 0.00f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.Separator] = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.59f, 0.00f, 1.00f, 1.00f);
            colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.90f, 0.00f, 0.90f, 1.00f);
            colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.90f, 0.59f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.90f, 0.00f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.Tab] = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            colors[(int)ImGuiCol.TabHovered] = new Vector4(0.00f, 0.90f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.TabActive] = new Vector4(0.90f, 0.00f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.00f, 0.00f, 0.90f, 1.00f);
            colors[(int)ImGuiCol.DockingPreview] = new Vector4(0.59f, 0.00f, 0.90f, 1.00f);
            colors[(int)ImGuiCol.DockingEmptyBg] = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            colors[(int)ImGuiCol.PlotLines] = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(0.90f, 0.59f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(0.90f, 0.00f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.06f, 0.06f, 0.06f, 1.00f);
            colors[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            colors[(int)ImGuiCol.TableBorderLight] = new Vector4(0.59f, 0.59f, 0.59f, 1.00f);
            colors[(int)ImGuiCol.TableRowBg] = new Vector4(0.06f, 0.06f, 0.06f, 1.00f);
            colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(1.00f, 1.00f, 1.00f, 0.06f);
            colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.39f, 0.00f, 0.78f, 1.00f);
            colors[(int)ImGuiCol.DragDropTarget] = new Vector4(0.90f, 0.90f, 0.00f, 0.90f);
            colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.90f, 0.59f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
        }
    }
}
#endif
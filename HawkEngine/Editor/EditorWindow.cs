﻿#if DEBUG
using HawkEngine.Core;
using HawkEngine.Graphics;
using ImGuiNET;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static ImGuiNET.ImGui;

namespace HawkEngine.Editor
{
    public abstract class EditorWindow
    {
        public bool open = true;
        public readonly string title;
        public Vector2 minSize = new(180f, 180f);

        public Vector2 position { get; protected set; }
        public Vector2 size { get; protected set; }
        public Vector2 rectMin { get; protected set; }
        public Vector2 rectMax { get; protected set; }

        public event Action<Vector2> positionChanged;
        public event Action<Vector2> sizeChanged;
        public event Action<Vector2, Vector2> rectChanged;

        private bool changingSize;
        public event Action<Vector2> sizeChangeEnd;

        public EditorWindow(bool open, string title, Vector2 minSize)
        {
            this.open = open;
            this.title = title;
            this.minSize = minSize;
        }
        protected virtual void PreShowWindow() {  }
        protected abstract void ShowWindow();
        protected virtual void PostShowWindow() {  }
        public virtual unsafe void Update()
        {
            if (!open)
            {
                if (EditorGUI.activeWindow == this) EditorGUI.activeWindow = null;
                return;
            }

            EditorGUI.style.WindowMinSize.X = minSize.X;
            EditorGUI.style.WindowMinSize.Y = minSize.Y;
            PreShowWindow();

            if (Begin(title, ref open))
            {
                if (IsWindowFocused() && EditorGUI.activeWindow != this) EditorGUI.activeWindow = this;
                else if (EditorGUI.activeWindow == this) EditorGUI.activeWindow = null;

                Vector2 newPosition = GetWindowPos();
                Vector2 newSize = GetContentRegionAvail();
                Vector2 newRectMin = GetWindowContentRegionMin() + position;
                Vector2 newRectMax = GetWindowContentRegionMax() + position;

                if (newSize != size)
                {
                    sizeChanged?.Invoke(newSize);
                    changingSize = true;
                }
                else if (changingSize && !IsMouseDown(ImGuiMouseButton.Left))
                {
                    sizeChangeEnd?.Invoke(newSize);
                    changingSize = false;
                }

                if (newPosition != position) positionChanged?.Invoke(newPosition);
                if (newRectMin != rectMin || newRectMax != rectMax) rectChanged?.Invoke(newRectMin, newRectMax);

                position = newPosition;
                size = newSize;
                rectMin = newRectMin;
                rectMax = newRectMax;

                ShowWindow();
            }

            End();
            PostShowWindow();
        }
    }

    public class EditorViewport : EditorWindow
    {
        public EditorViewport() : base(true, "Viewport", new(320f, 180f))
        {

        }
        protected override void PreShowWindow()
        {
            PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
        }
        protected override void ShowWindow()
        {
            if (Rendering.outputCam == null) return;

            Vector2 availSpace = GetContentRegionAvail();
            float availAspect = availSpace.X / availSpace.Y;
            float srcAspect = (float)Rendering.outputCam.size.X / Rendering.outputCam.size.Y;

            float aspect = srcAspect / availAspect;
            Vector2 size = new(availSpace.X * Scalar.Min(aspect, 1f), availSpace.Y / Scalar.Max(aspect, 1f));

            Image((nint)Rendering.postProcessFB[FramebufferAttachment.ColorAttachment0].id, size, new(0f, 1f), new(1f, 0f));

            if (IsItemClicked(ImGuiMouseButton.Middle) || IsItemClicked(ImGuiMouseButton.Right))
            {
                SetWindowFocus();
                EditorGUI.activeWindow = this;
            }

            if (IsItemClicked(ImGuiMouseButton.Left))
            {
                Vector2 relativeMousePos = GetMousePos() - rectMin;
                relativeMousePos.Y = size.Y - relativeMousePos.Y;

                Span<float> firstHalf = stackalloc float[2];
                Span<float> secondHalf = stackalloc float[2];

                Rendering.gl.GetTextureSubImage(HawkEditor.objectIDFB[FramebufferAttachment.ColorAttachment0].id, 0, (int)relativeMousePos.X,
                    (int)relativeMousePos.Y, 0, 1, 1, 1, PixelFormat.RG, PixelType.Float, 64u, firstHalf);

                Rendering.gl.GetTextureSubImage(HawkEditor.objectIDFB[FramebufferAttachment.ColorAttachment1].id, 0, (int)relativeMousePos.X,
                    (int)relativeMousePos.Y, 0, 1, 1, 1, PixelFormat.RG, PixelType.Float, 64u, secondHalf);

                Vector4D<float> col = new(firstHalf[0], firstHalf[1], secondHalf[0], secondHalf[1]);
                ulong objID = EditorUtils.ColorToID(col);

                if (objID == 0)
                {
                    HawkEditor.selectedObjects.Clear();
                    return;
                }

                foreach (HawkObject obj in App.scene.objects)
                {
                    if (obj.engineID == objID)
                    {
                        if (!EditorGUI.io.KeyShift) HawkEditor.selectedObjects.Clear();
                        HawkEditor.selectedObjects.Add(obj);
                        break;
                    }
                }
            }
        }
        protected override void PostShowWindow()
        {
            PopStyleVar();
        }
    }

    public class EditorSceneTree : EditorWindow
    {
        public EditorSceneTree() : base(true, "Scene Tree", new(300f, 500f))
        {

        }
        protected override void ShowWindow()
        {
            List<SceneObject> objects = App.scene?.objects;
            if (objects == null) return;

            foreach (SceneObject obj in objects)
            {


                bool selected = HawkEditor.selectedObjects.Contains(obj);
                Selectable(obj.name, ref selected);

                if (selected)
                {
                    if (!HawkEditor.selectedObjects.Contains(obj))
                    {
                        if (HawkEditor.selectedObjects.Count > 0 && EditorGUI.io.KeyShift)
                        {
                            int lastIndex = objects.IndexOf((SceneObject)HawkEditor.selectedObjects.Last());
                            int currentIndex = objects.IndexOf(obj);

                            int diff = lastIndex - currentIndex;
                            int sign = Scalar.Sign(diff);

                            for (int i = 0; i < diff * sign; i++)
                            {
                                SceneObject toSelect = objects[lastIndex - i * sign];
                                if (!HawkEditor.selectedObjects.Contains(toSelect)) HawkEditor.selectedObjects.Add(toSelect);
                            }
                        }
                        else if (!EditorGUI.io.KeyCtrl) HawkEditor.selectedObjects.Clear();

                        HawkEditor.selectedObjects.Add(obj);
                    }
                }
                else if (HawkEditor.selectedObjects.Contains(obj)) HawkEditor.selectedObjects.Remove(obj);
            }
        }
    }

    public class EditorConsole : EditorWindow
    {
        private readonly List<EditorUtils.ConsoleMessage> messages = new();

        private bool collapseAll;
        private bool expandAll;

        private bool showErrors = true;
        private bool showWarnings = true;
        private bool showInfos = true;

        public EditorConsole() : base(true, "Console", new(400f, 150f))
        {

        }
        protected override unsafe void ShowWindow()
        {
            if (Button("Clear")) Clear();
            SameLine();
            if (Button("Collapse All")) collapseAll = true;
            SameLine();
            if (Button("Expand All")) expandAll = true;

            SameLine();
            Checkbox("Show Infos", ref showInfos);
            SameLine();
            Checkbox("Show Warnings", ref showWarnings);
            SameLine();
            Checkbox("Show Errors", ref showErrors);
            Separator();

            for (int m = 0; m < messages.Count; m++)
            {
                EditorUtils.ConsoleMessage message = messages[m];

                float space = GetContentRegionAvail().X;
                float textSize = CalcTextSize(message.message).X;

                if (textSize > space)
                {
                    char[] chars = message.message.ToCharArray();
                    int splitIndex = chars.Length - 1;

                    for (int i = chars.Length - 1; i >= 0; i--)
                    {
                        if (chars[i] == ' ')
                        {
                            float newSize = CalcTextSize(new string(chars.Take(i + 1).ToArray())).X;

                            if (newSize < space - 20f)
                            {
                                splitIndex = i;
                                break;
                            }
                        }
                    }

                    message.message = new string(chars.Take(splitIndex).ToArray());
                    message.extraInfo = new string(chars.Skip(splitIndex).ToArray()) + (message.extraInfo == null ? "" : $" | {message.extraInfo}");
                }

                if (message.severity == EditorUtils.MessageSeverity.Info && !showInfos) continue;
                else if (message.severity == EditorUtils.MessageSeverity.Warning && !showWarnings) continue;
                else if (message.severity == EditorUtils.MessageSeverity.Error && !showErrors) continue;

                if (message.extraInfo == null) SetNextItemOpen(false);
                else if (collapseAll) SetNextItemOpen(false);
                else if (expandAll) SetNextItemOpen(true);

                PushStyleColor(ImGuiCol.Text, message.severity switch
                {
                    EditorUtils.MessageSeverity.Warning => new(1f, 1f, 0f, 1f),
                    EditorUtils.MessageSeverity.Error => new(1f, 0f, 0f, 1f),
                    _ => Vector4.One,
                });

                ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.Framed;
                bool expanded = TreeNodeEx(message.id, flags, message.message);

                if (message.obj != null)
                {
                    SameLine(space - CalcTextSize(message.obj.name).X);
                    TextWrapped(message.obj.name);
                }

                if (expanded)
                {
                    if (message.extraInfo != null) TextWrapped(message.extraInfo);
                    TreePop();
                }

                PopStyleColor();
            }

            collapseAll = false;
            expandAll = false;
        }
        public void PrintMessage(EditorUtils.ConsoleMessage message)
        {
            messages.Add(message);
        }
        public void Clear()
        {
            messages.Clear();
        }
    }

    public class EditorStats : EditorWindow
    {
        public EditorStats() : base(true, "Stats", new(100f, 75f))
        {

        }
        protected override void ShowWindow()
        {
            Text($"FPS: {Scalar.Round(1f / Time.smoothUnscaledDeltaTime)}");
        }
    }
}
#endif
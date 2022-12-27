#if DEBUG
using HawkEngine.Core;
using HawkEngine.Graphics;
using ImGuiNET;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static ImGuiNET.ImGui;

namespace HawkEngine.Editor
{
    public class EditorWindow
    {
        public bool open = true;
        public readonly string title;
        public readonly Action[] styleVars;
        public readonly Action showAction;

        public Vector2 position { get; protected set; }
        public Vector2 size { get; protected set; }
        public Vector2 rectMin { get; protected set; }
        public Vector2 rectMax { get; protected set; }


        public event Action<Vector2> positionChanged;

        public event Action<Vector2> sizeChanged;

        public event Action<Vector2, Vector2> rectChanged;

        public EditorWindow(bool open, string title, Action[] styleVars, Action showAction)
        {
            this.open = open;
            this.title = title;
            this.styleVars = styleVars;
            this.showAction = showAction;
        }
        public void Update()
        {
            if (!open)
            {
                if (EditorGUI.activeWindow == this) EditorGUI.activeWindow = null;
                return;
            }

            for (int i = 0; i < styleVars.Length; i++)
            {
                styleVars[i]?.Invoke();
            }

            if (Begin(title, ref open))
            {
                if (IsWindowFocused()) EditorGUI.activeWindow = this;
                else if (EditorGUI.activeWindow == this) EditorGUI.activeWindow = null;

                Vector2 newPosition = GetWindowPos();
                Vector2 newSize = GetContentRegionAvail();
                Vector2 newRectMin = GetWindowContentRegionMin() + position;
                Vector2 newRectMax = GetWindowContentRegionMax() + position;

                if (newPosition != position) positionChanged?.Invoke(newPosition);
                if (newSize != size) sizeChanged?.Invoke(newSize);
                if (newRectMin != rectMin || newRectMax != rectMax) rectChanged?.Invoke(newRectMin, newRectMax);

                position = newPosition;
                size = newSize;
                rectMin = newRectMin;
                rectMax = newRectMax;

                showAction?.Invoke();
            }
            End();

            PopStyleVar(styleVars.Length);
        }

        public static readonly EditorWindow viewport = new(true, "Viewport",
        new Action[1] { () => PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero) },
        () =>
        {
            if (Rendering.outputCam == null) return;

            Vector2 availSpace = GetContentRegionAvail();
            float availAspect = availSpace.X / availSpace.Y;
            float srcAspect = (float)Rendering.outputCam.size.X / Rendering.outputCam.size.Y;

            float aspect = srcAspect / availAspect;
            Vector2 size = new(availSpace.X * Scalar.Min(aspect, 1f), availSpace.Y / Scalar.Max(aspect, 1f));

            Image((nint)Rendering.postProcessFB[FramebufferAttachment.ColorAttachment0].id, size, new(0f, 1f), new(1f, 0f));

            if (IsItemClicked(ImGuiMouseButton.Left))
            {
                Vector2 relativeMousePos = GetMousePos() - viewport.rectMin;
                relativeMousePos.Y = viewport.size.Y - relativeMousePos.Y;

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
        });

        public static readonly EditorWindow sceneTree = new(true, "Scene Tree", Array.Empty<Action>(),
        () =>
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
        });
    }
}
#endif
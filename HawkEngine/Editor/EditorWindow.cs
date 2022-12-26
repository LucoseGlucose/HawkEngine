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
                showAction?.Invoke();
            }
            End();

            PopStyleVar(styleVars.Length);
        }

        public static readonly EditorWindow viewport = new(true, "Viewport",
        new Action[1] { () => PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero) },
        () =>
        {
            Vector2 availSpace = GetContentRegionAvail();
            float availAspect = availSpace.X / availSpace.Y;
            float srcAspect = (float)Rendering.outputCam.size.X / Rendering.outputCam.size.Y;

            float aspect = srcAspect / availAspect;
            Vector2 size = new(availSpace.X * Scalar.Min(aspect, 1f), availSpace.Y / Scalar.Max(aspect, 1f));

            Image((nint)Rendering.postProcessFB[FramebufferAttachment.ColorAttachment0].id, size, new(0f, 1f), new(1f, 0f));
        });

        public static readonly EditorWindow sceneTree = new(true, "Scene Tree", Array.Empty<Action>(),
        () =>
        {
            List<SceneObject> objects = App.scene.objects;

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
                            int lastIndex = objects.IndexOf(HawkEditor.selectedObjects.Last());
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
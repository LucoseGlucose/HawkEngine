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
    public abstract class EditorWindow
    {
        public bool open = true;
        protected abstract string title { get; }
        protected abstract Action[] styleVars { get; }

        public EditorWindow(bool open)
        {
            this.open = open;
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
                Show();
            }
            End();

            PopStyleVar(styleVars.Length);
        }
        protected abstract void Show();
    }

    public class EditorViewport : EditorWindow
    {
        protected override string title => "Viewport";

        protected override Action[] styleVars => new Action[]
        {
            () => PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f)),
        };

        public EditorViewport(bool open) : base(open)
        {

        }
        protected override void Show()
        {
            Vector2 availSpace = GetContentRegionAvail();
            float availAspect = availSpace.X / availSpace.Y;
            float srcAspect = (float)Rendering.outputCam.size.X / Rendering.outputCam.size.Y;

            float aspect = srcAspect / availAspect;
            Vector2 size = new(availSpace.X * Scalar.Min(aspect, 1f), availSpace.Y / Scalar.Max(aspect, 1f));

            Image((nint)Rendering.postProcessFB[FramebufferAttachment.ColorAttachment0].id, size, new(0f, 1f), new(1f, 0f));
        }
    }
}
#endif
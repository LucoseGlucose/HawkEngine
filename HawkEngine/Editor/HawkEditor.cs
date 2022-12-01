using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HawkEngine.Core;
using HawkEngine.Graphics;

namespace HawkEngine.Editor
{
    public static class HawkEditor
    {
        public static void EditorCall(Action action)
        {
#if DEBUG
            action();
#endif
        }
        public static void BuildCall(Action action)
        {
#if !DEBUG
            action();
#endif
        }
        public static void Init()
        {
            EditorGUI.Init();
        }
        public static void Update(double deltaTime)
        {
            EditorGUI.Update();
        }
        public static void Render()
        {
            EditorGUI.Render();
        }
    }
}

using System;
using System.Collections.Generic;
using HawkEngine.Core;
using Silk.NET.Input;
using System.Numerics;
using Silk.NET.Maths;

namespace HawkEngine.Components
{
    public class CameraControllerComponent : Component
    {
        private Vector2 lastMousePos;        

        public float scrollSpeed = 1.5f;
        public float panSpeed = 1f;
        public float rotateSpeed = 50f;
        public float flySpeed = 25f;

        public override void Update()
        {
#if DEBUG
            if (Editor.EditorGUI.activeWindow == null || Editor.EditorGUI.activeWindow.title != "Viewport") return;
#endif

            IMouse mouse = App.input.Mice[0];
            Vector2 mouseDelta = mouse.Position - lastMousePos;

            if (mouse.IsButtonPressed(MouseButton.Right))
            {
                if (mouseDelta.LengthSquared() > 0)
                {
                    Vector3D<float> input = new Vector3D<float>(mouseDelta.Y, -mouseDelta.X, 0f) * Time.deltaTime * rotateSpeed * Conversions.degToRad;
                    transform.rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, input.Y) * transform.rotation;
                    transform.rotation = Quaternion<float>.CreateFromAxisAngle(transform.right, input.X) * transform.rotation;
                }

                IKeyboard keyboard = App.input.Keyboards[0];
                if (keyboard.IsKeyPressed(Key.W)) transform.position += transform.forward * flySpeed * Time.deltaTime;
                if (keyboard.IsKeyPressed(Key.A)) transform.position += transform.right * flySpeed * Time.deltaTime;
                if (keyboard.IsKeyPressed(Key.S)) transform.position += -transform.forward * flySpeed * Time.deltaTime;
                if (keyboard.IsKeyPressed(Key.D)) transform.position += -transform.right * flySpeed * Time.deltaTime;
                if (keyboard.IsKeyPressed(Key.Q)) transform.position += -transform.up * flySpeed * Time.deltaTime;
                if (keyboard.IsKeyPressed(Key.E)) transform.position += transform.up * flySpeed * Time.deltaTime;
            }

            if (mouse.IsButtonPressed(MouseButton.Middle) && mouseDelta.LengthSquared() > 0)
            {
                transform.position += transform.up * mouseDelta.Y * Time.deltaTime * panSpeed;
                transform.position += transform.right * mouseDelta.X * Time.deltaTime * panSpeed;
            }

            if (mouse.ScrollWheels[0].Y != 0) transform.position += transform.forward * mouse.ScrollWheels[0].Y * scrollSpeed;

            lastMousePos = mouse.Position;

            if (mouse.IsButtonPressed(MouseButton.Right) || mouse.IsButtonPressed(MouseButton.Middle)) mouse.Cursor.CursorMode = CursorMode.Hidden;
            else mouse.Cursor.CursorMode = CursorMode.Normal;
        }
    }
}

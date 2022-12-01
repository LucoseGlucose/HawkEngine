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
            IMouse mouse = App.input.Mice[0];
            Vector2 mouseDelta = mouse.Position - lastMousePos;

            if (mouse.IsButtonPressed(MouseButton.Right))
            {
                if (mouseDelta.LengthSquared() > 0)
                    transform.rotation += new Vector3D<float>(mouseDelta.Y, -mouseDelta.X, 0f) * App.deltaTime * rotateSpeed;

                IKeyboard keyboard = App.input.Keyboards[0];
                if (keyboard.IsKeyPressed(Key.W)) transform.position += transform.forward * flySpeed * App.deltaTime;
                if (keyboard.IsKeyPressed(Key.A)) transform.position += transform.right * flySpeed * App.deltaTime;
                if (keyboard.IsKeyPressed(Key.S)) transform.position += -transform.forward * flySpeed * App.deltaTime;
                if (keyboard.IsKeyPressed(Key.D)) transform.position += -transform.right * flySpeed * App.deltaTime;
                if (keyboard.IsKeyPressed(Key.Q)) transform.position += -transform.up * flySpeed * App.deltaTime;
                if (keyboard.IsKeyPressed(Key.E)) transform.position += transform.up * flySpeed * App.deltaTime;
            }

            if (mouse.IsButtonPressed(MouseButton.Middle) && mouseDelta.LengthSquared() > 0)
            {
                transform.position += transform.up * mouseDelta.Y * App.deltaTime * panSpeed;
                transform.position += transform.right * mouseDelta.X * App.deltaTime * panSpeed;
            }

            if (mouse.ScrollWheels[0].Y != 0) transform.position += transform.forward * mouse.ScrollWheels[0].Y * scrollSpeed;

            lastMousePos = mouse.Position;
        }
    }
}

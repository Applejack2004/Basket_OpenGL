using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace OpenBasket
{
    internal class Camera
    {
        private float SPEED = 8f;
        private float SCREENWIDTH;
        private float SCREENHEIGHT;
        private float SENSITIVITY = 100f;

        public Vector3 position;

        Vector3 up = Vector3.UnitY;
        Vector3 front = -Vector3.UnitZ;
        Vector3 right = Vector3.UnitX;
        public Vector3 Front => front;
        public Vector3 Right => right;
        public Vector3 Up => up;

        private float pitch;
        private float yaw = -90.0f;
        private bool firstMove = true;
        public Vector2 lastPos;

        public Camera(int width, int height, Vector3 position)
        {
            SCREENWIDTH = width;
            SCREENHEIGHT = height;
            this.position = position;
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(position, position + front, up);
        }
        public Matrix4 GetProjection()
        {
            return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), SCREENWIDTH / SCREENHEIGHT, 0.1f, 100f);
        }

        private void UpdateVectors()
        {

            if (pitch > 89.0f)
            {
                pitch = 89.0f;
            }
            if (pitch < -89.0f)
            {
                pitch = -89.0f;
            }

            front.X = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Cos(MathHelper.DegreesToRadians(yaw));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
            front.Z = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Sin(MathHelper.DegreesToRadians(yaw));

            front = Vector3.Normalize(front);
            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }

        public void InputController(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            Vector3 oldPosition = position;
            Vector3 horizontalFront = Vector3.Normalize(new Vector3(front.X, 0f, front.Z));
            Vector3 horizontalRight = Vector3.Normalize(new Vector3(right.X, 0f, right.Z));

            if (input.IsKeyDown(Keys.W))
            {
                position += horizontalFront * SPEED * (float)e.Time;
            }
            if (input.IsKeyDown(Keys.A))
            {
                position -= horizontalRight * SPEED * (float)e.Time;
            }
            if (input.IsKeyDown(Keys.S))
            {
                position -= horizontalFront * SPEED * (float)e.Time;
            }
            if (input.IsKeyDown(Keys.D))
            {
                position += horizontalRight * SPEED * (float)e.Time;
            }

            // --- Коллизия с бортиками ---
            // Размеры бортиков из Borders.cs: x0 = -5, x1 = 5, z0 = -5, z1 = 5, высота y0 = -1, y1 = 0
            // Камера может пройти только если Y > 0.95 (т.е. прыжок)
            float borderX0 = -5f + 0.2f, borderX1 = 5f - 0.2f; // небольшой зазор, чтобы не застревать
            float borderZ0 = -5f + 0.2f, borderZ1 = 5f - 0.2f;
            float borderY = 0.95f; // высота "перепрыгивания"
            if (position.X < borderX0 && position.Y < borderY) position.X = borderX0;
            if (position.X > borderX1 && position.Y < borderY) position.X = borderX1;
            if (position.Z < borderZ0 && position.Y < borderY) position.Z = borderZ0;
            if (position.Z > borderZ1 && position.Y < borderY) position.Z = borderZ1;

            if (firstMove)
            {
                lastPos = new Vector2(position.X, position.Y);
                firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - lastPos.X;
                var deltaY = mouse.Y - lastPos.Y;
                lastPos = new Vector2(mouse.X, mouse.Y);

                yaw += deltaX * SENSITIVITY * (float)e.Time;
                pitch -= deltaY * SENSITIVITY * (float)e.Time;
            }
            UpdateVectors();
        }
        public void Update(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            InputController(input, mouse, e);
        }


    }
}

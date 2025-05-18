using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace OpenBasket.Classes
{
    internal class Borders
    {
        public int vao;
        public int vbo;
        public int ebo;
        public uint[] indices;
        public float[] vertices;

        public void Load()
        {
            // Бортики: 4 стены вокруг площадки, высота по пояс (примерно 1м)
            float height = 1.0f;
            float y0 = -1.0f; // уровень пола для бортиков ниже
            float y1 = y0 + height;
            float x0 = -5f, x1 = 5f, z0 = -5f, z1 = 5f;
            // 4 стены, каждая из двух прямоугольников (по 2 треугольника)
            vertices = new float[] {
                // Передняя стена (z = z0)
                x0, y0, z0,
                x1, y0, z0,
                x1, y1, z0,
                x0, y1, z0,
                // Задняя стена (z = z1)
                x0, y0, z1,
                x1, y0, z1,
                x1, y1, z1,
                x0, y1, z1,
                // Левая стена (x = x0)
                x0, y0, z0,
                x0, y0, z1,
                x0, y1, z1,
                x0, y1, z0,
                // Правая стена (x = x1)
                x1, y0, z0,
                x1, y0, z1,
                x1, y1, z1,
                x1, y1, z0,
            };
            indices = new uint[] {
                // Передняя стена
                0, 1, 2, 2, 3, 0,
                // Задняя стена
                4, 5, 6, 6, 7, 4,
                // Левая стена
                8, 9,10,10,11, 8,
                // Правая стена
               12,13,14,14,15,12
            };
            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            ebo = GL.GenBuffer();
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(0);
            // Не используем текстурные координаты для бортиков
            GL.BindVertexArray(0);
        }
        public void Bind()
        {
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        }
        public void Unload()
        {
            GL.DeleteVertexArray(vao);
            GL.DeleteBuffer(vbo);
            GL.DeleteBuffer(ebo);
        }
    }
}

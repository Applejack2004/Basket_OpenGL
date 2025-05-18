using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;


namespace OpenBasket.Classes
{
    internal class Floor
    {
        public int floorVAO;
        public int floorVBO;
        public int floorEBO;

        float[] floorVertices =
        {
            -5f, -0.5f, -5f,  // Нижний левый
             5f, -0.5f, -5f,  // Нижний правый
            -5f, -0.5f,  5f,  // Верхний левый
             5f, -0.5f,  5f   // Верхний правый
        };

        public uint[] floorIndices =
        {
            0, 1, 2,
            2, 3, 1
        };

        float[] floorTexCoords =
        {
            0f, 1f,  // Верхний левый
            1f, 1f,  // Верхний правый
            0f, 0f,  // Нижний левый
            1f, 0f   // Нижний правый
        };

        int floorTextureVBO;
        public int floorTextureID;

        public void FloorTexture()
        {
            floorTextureID = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, floorTextureID);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            StbImage.stbi_set_flip_vertically_on_load(1);
            ImageResult floorTexture = ImageResult.FromStream(
                File.OpenRead(@"C:\basket_game_opengl\OpenGlBasket\OpenBasket\Texture\square.jpg"),
                ColorComponents.RedGreenBlueAlpha);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                floorTexture.Width, floorTexture.Height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, floorTexture.Data);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void FloorBind()
        {
            GL.BindVertexArray(floorVAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, floorEBO);
            GL.BindTexture(TextureTarget.Texture2D, floorTextureID);
        }

        public void FloorUnload()
        {
            GL.DeleteVertexArray(floorVAO);
            GL.DeleteBuffer(floorVBO);
            GL.DeleteBuffer(floorEBO);
            GL.DeleteTexture(floorTextureID);
        }

        public void FloorLoad()
        {
            // Инициализация VBO
            floorVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, floorVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, floorVertices.Length * sizeof(float), floorVertices, BufferUsageHint.StaticDraw);

            // Инициализация VAO
            floorVAO = GL.GenVertexArray();
            GL.BindVertexArray(floorVAO);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexArrayAttrib(floorVAO, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // Инициализация EBO
            floorEBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, floorEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, floorIndices.Length * sizeof(uint), floorIndices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            // Текстурные координаты
            floorTextureVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, floorTextureVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, floorTexCoords.Length * sizeof(float), floorTexCoords, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexArrayAttrib(floorVAO, 1);

            // Загрузка текстуры
            FloorTexture();
        }
    }
}

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
    internal class Ring
    {
        public int ringVAO;
        public int ringVBO;
        public int ringEBO;
        float[] ringVerties =
        {
            -0.5f, 0.5f, 0f,
            0.5f, 0.5f, 0f,
            -0.5f, -0.5f, 0f,
            0.5f, -0.5f, 0f

        };
        public uint[] ringIndices =
        {
            0, 1, 2,
            2, 3, 1
        };

        float[] ringTexCoords =
        {
        0f, 1f,//верхний левый
        1f, 1f,//верхний правый
        0f, 0f,//нижний левый
        1f, 0f//нижний правый

        };
        int ringTextureVBO;
        int ringTextureID;
        public void RingTexture()
        {
            ringTextureID = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, ringTextureID);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            StbImage.stbi_set_flip_vertically_on_load(1);
            ImageResult ceilingTexture = ImageResult.FromStream(File.OpenRead(@"C:\basket_game_opengl\OpenGlBasket\OpenBasket\Texture\ring.jpg"), ColorComponents.RedGreenBlueAlpha);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, ceilingTexture.Width, ceilingTexture.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ceilingTexture.Data);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        public void RingBind()
        {
            GL.BindVertexArray(ringVAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ringEBO);
            GL.BindTexture(TextureTarget.Texture2D, ringTextureID);
        }
        public void RingUnload()
        {
            GL.DeleteBuffer(ringVAO);
            GL.DeleteBuffer(ringVBO);
            GL.DeleteBuffer(ringEBO);
            GL.DeleteTexture(ringTextureID);
        }
        public void RingLoad()
        {
            ringVBO = GL.GenBuffer();
            //биндим поле vbo как буфуер
            GL.BindBuffer(BufferTarget.ArrayBuffer, ringVBO);
            //запихиваем данные в буфер берем переменные ЦЕЛЬ РАЗМЕР И ДАТА
            GL.BufferData(BufferTarget.ArrayBuffer, ringVerties.Length * sizeof(float), ringVerties, BufferUsageHint.StaticDraw);
            //теперь после того как мы сделали VBO займеся VAО
            ringVAO = GL.GenVertexArray();
            GL.BindVertexArray(ringVAO);
            //как я понял мы тут говорим что вот так то так то по три вершины берем
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            //ну а тут типо их совместили
            GL.EnableVertexArrayAttrib(ringVAO, 0);
            //теперь надо разбиндить буфер
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            //СОЗДАЕМ EBO НА ПОДОБИИ VBO
            ringEBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ringEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, ringIndices.Length * sizeof(uint), ringIndices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);



            //теперь делаем текстурный буфер
            ringTextureVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, ringTextureVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, ringTexCoords.Length * sizeof(float), ringTexCoords, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexArrayAttrib(ringVAO, 1);
            RingTexture();
        }

    }
}

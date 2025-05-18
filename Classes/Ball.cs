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
    internal class Ball
    {
        public int ballVAO;
        public int ballVBO;
        public int ballEBO;
        public int ballTextureID;

        private float[] ballvertices;
        public uint[] ballindices;

        public void BallLoad()
        {
            GenerateSphere(0.5f, 32, 32);
            SetupBuffers();
            LoadTexture();
        }

        private void GenerateSphere(float radius, int sectors, int stacks)
        {
            List<float> verts = new List<float>();

            float sectorStep = 2 * MathF.PI / sectors;
            float stackStep = MathF.PI / stacks;

            for (int i = 0; i <= stacks; ++i)
            {
                float stackAngle = MathF.PI / 2 - i * stackStep;
                float xy = radius * MathF.Cos(stackAngle);
                float z = radius * MathF.Sin(stackAngle);

                for (int j = 0; j <= sectors; ++j)
                {
                    float sectorAngle = j * sectorStep;

                    float x = xy * MathF.Cos(sectorAngle);
                    float y = xy * MathF.Sin(sectorAngle);
                    verts.Add(x); verts.Add(y); verts.Add(z);

                    verts.Add((float)j / sectors);
                    verts.Add((float)i / stacks);
                }
            }

            List<uint> inds = new List<uint>();
            for (int i = 0; i < stacks; ++i)
            {
                uint k1 = (uint)(i * (sectors + 1));
                uint k2 = (uint)(k1 + sectors + 1);

                for (int j = 0; j < sectors; ++j, ++k1, ++k2)
                {
                    if (i != 0)
                    {
                        inds.Add(k1);
                        inds.Add(k2);
                        inds.Add(k1 + 1);
                    }
                    if (i != stacks - 1)
                    {
                        inds.Add(k1 + 1);
                        inds.Add(k2);
                        inds.Add(k2 + 1);
                    }
                }
            }

            ballvertices = verts.ToArray();
            ballindices = inds.ToArray();
        }

        private void SetupBuffers()
        {
            ballVAO = GL.GenVertexArray();
            ballVBO = GL.GenBuffer();
            ballEBO = GL.GenBuffer();

            GL.BindVertexArray(ballVAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, ballVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, ballvertices.Length * sizeof(float), ballvertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ballEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, ballindices.Length * sizeof(uint), ballindices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }

        private void LoadTexture()
        {
            ballTextureID = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, ballTextureID);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            StbImage.stbi_set_flip_vertically_on_load(1);
            ImageResult texture = ImageResult.FromStream(
                File.OpenRead(@"C:\basket_game_opengl\OpenGlBasket\OpenBasket\Texture\ball.jpg"),
                ColorComponents.RedGreenBlueAlpha);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                texture.Width, texture.Height, 0, PixelFormat.Rgba,
                PixelType.UnsignedByte, texture.Data);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void BallBind()
        {
            GL.BindVertexArray(ballVAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ballEBO);
            GL.BindTexture(TextureTarget.Texture2D, ballTextureID);
        }

        public void BallUnload()
        {
            GL.DeleteVertexArray(ballVAO);
            GL.DeleteBuffer(ballVBO);
            GL.DeleteBuffer(ballEBO);
            GL.DeleteTexture(ballTextureID);
        }
    }
}
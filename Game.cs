using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenBasket;
using OpenBasket.Classes;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;

namespace OpenBasket
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Game game = new Game(1920, 1080))
            {
                game.Run();
            }
        }
    }

    class Shaders
    {
        public int shaderHandle;
        public void LoadShader()
        {
            shaderHandle = GL.CreateProgram();
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, LoadShaderSource("shader.vert"));
            GL.CompileShader(vertexShader);
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, LoadShaderSource("shader.frag"));
            GL.CompileShader(fragmentShader);

            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success1);
            if (success1 == 0)
            {

                string infoLog = GL.GetShaderInfoLog(vertexShader);
                Console.WriteLine(infoLog);
            }
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out
            int success2);
            if (success2 == 0)
            {
                string infoLog = GL.GetShaderInfoLog(fragmentShader);
                Console.WriteLine(infoLog);
            }

            GL.AttachShader(shaderHandle, vertexShader);
            GL.AttachShader(shaderHandle, fragmentShader);
            GL.LinkProgram(shaderHandle);
        }
        public static string LoadShaderSource(string filepath)
        {
            string shaderSource = "";
            try
            {
                using (StreamReader reader = new StreamReader(
    @"C:\basket_game_opengl\OpenGlBasket\OpenBasket\Shaders\" + filepath))
                {
                    shaderSource = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load shader source file: " + e.Message);
            }
            return shaderSource;
        }
        public void UseShader()
        {
            GL.UseProgram(shaderHandle);
        }
        public void DeleteShader()
        {
            GL.DeleteProgram(shaderHandle);
        }

    }

    internal class Game : GameWindow
    {
        int width, height;


        private Vector3 handOffset = new Vector3(0.5f, -0.5f, 1f);
        private bool isBallThrown = false;
        private Vector3 ballVelocity = Vector3.Zero;
        private float gravity = -9.81f;
        private float throwForce = 20f;

        int modelLocation; // Оставляем эти переменные, хотя для кольца они будут получены локально
        int viewLocation;
        int projectionLocation;


        Shaders shaderProgram = new Shaders();
        Camera? camera = null;
        Ring ring = new Ring();
        Floor floor = new Floor();
        Ball ball = new Ball();
        Borders borders = new Borders();
        Vector3 ballPosition = new Vector3(0f, 0.5f, -3f); // Исходная позиция мяча перед камерой
        public Game(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            this.CenterWindow(new Vector2i(width, height));
            this.height = height;
            this.width = width;
            throwForce = 15f; // Сила броска как в оригинале
        }

        int[] bgVAO = new int[4], bgVBO = new int[4], bgEBO = new int[4];
        int backgroundTextureID;
        void LoadBackground()
        {
            // Координаты для 4 сторон (большой размер)
            float zFar = -9.9f, zNear = 9.9f, yBot = -2f, yTop = 6f, xLeft = -7f, xRight = 7f;
            float[][] bgVertices = new float[4][];
            // Задняя (за кольцом z = zFar)
            bgVertices[0] = new float[] {
                xLeft, yTop, zFar, 0, 1,
                xRight, yTop, zFar, 1, 1,
                xLeft, yBot, zFar, 0, 0,
                xRight, yBot, zFar, 1, 0
            };
            // Передняя (z = zNear)
            bgVertices[1] = new float[] {
                xLeft, yTop, zNear, 0, 1,
                xRight, yTop, zNear, 1, 1,
                xLeft, yBot, zNear, 0, 0,
                xRight, yBot, zNear, 1, 0
            };
            // Левая (x = xLeft)
            bgVertices[2] = new float[] {
                xLeft, yTop, zNear, 0, 1,
                xLeft, yTop, zFar, 1, 1,
                xLeft, yBot, zNear, 0, 0,
                xLeft, yBot, zFar, 1, 0
            };
            // Правая (x = xRight)
            bgVertices[3] = new float[] {
                xRight, yTop, zNear, 0, 1,
                xRight, yTop, zFar, 1, 1,
                xRight, yBot, zNear, 0, 0,
                xRight, yBot, zFar, 1, 0
            };
            uint[] indices = { 0, 1, 2, 2, 3, 1 };
            for (int i = 0; i < 4; i++)
            {
                bgVAO[i] = GL.GenVertexArray();
                bgVBO[i] = GL.GenBuffer();
                bgEBO[i] = GL.GenBuffer();
                GL.BindVertexArray(bgVAO[i]);
                GL.BindBuffer(BufferTarget.ArrayBuffer, bgVBO[i]);
                GL.BufferData(BufferTarget.ArrayBuffer, bgVertices[i].Length * sizeof(float), bgVertices[i], BufferUsageHint.StaticDraw);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, bgEBO[i]);
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
                GL.EnableVertexAttribArray(1);
                GL.BindVertexArray(0);
            }
            // Одна текстура для всех сторон
            backgroundTextureID = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, backgroundTextureID);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            StbImageSharp.StbImage.stbi_set_flip_vertically_on_load(1);
            var img = StbImageSharp.ImageResult.FromStream(System.IO.File.OpenRead("c://opengl//Basket_OpenGL//Texture//fans.jpg"), StbImageSharp.ColorComponents.RedGreenBlueAlpha);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, img.Width, img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, img.Data);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        void DrawBackground()
        {
            int modelLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
            int viewLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "view");
            int projLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "projection");
            int colorLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "overrideColor");
            if (colorLoc != -1) GL.Uniform4(colorLoc, new Vector4(-1.0f));
            Matrix4 model = Matrix4.Identity;
            Matrix4 view = camera != null ? camera.GetViewMatrix() : Matrix4.Identity;
            Matrix4 projection = camera != null ? camera.GetProjection() : Matrix4.Identity;
            for (int i = 0; i < 4; i++)
            {
                GL.BindVertexArray(bgVAO[i]);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, bgEBO[i]);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, backgroundTextureID);
                GL.UniformMatrix4(modelLoc, true, ref model);
                GL.UniformMatrix4(viewLoc, true, ref view);
                GL.UniformMatrix4(projLoc, true, ref projection);
                GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            }
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            ring.RingLoad();
            floor.FloorLoad();
            ball.BallLoad();
            borders.Load();
            shaderProgram.LoadShader();
            LoadBackground();


            // Камера в исходной позиции
            camera = new Camera(width, height, Vector3.Zero);
            CursorState = CursorState.Grabbed;
            GL.Enable(EnableCap.DepthTest);

            // Настройка VAO, VBO и EBO для куба
            teamVAO = GL.GenVertexArray();
            teamVBO = GL.GenBuffer();
            teamEBO = GL.GenBuffer();

            GL.BindVertexArray(teamVAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, teamVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, squareVertices.Length * sizeof(float), squareVertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, teamEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, squareIndices.Length * sizeof(uint), squareIndices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);

            // Загрузка текстуры команды
            teamTextureID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, teamTextureID);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            StbImage.stbi_set_flip_vertically_on_load(1);
            using (var stream = File.OpenRead("Texture/team.jpg"))
            {
                var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
            }
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }



        protected override void OnUnload()
        {
            ring.RingUnload();
            floor.FloorUnload();
            ball.BallUnload();
            borders.Unload();
            for (int i = 0; i < 4; i++)
            {
                GL.DeleteVertexArray(bgVAO[i]);
                GL.DeleteBuffer(bgVBO[i]);
                GL.DeleteBuffer(bgEBO[i]);
            }
            GL.DeleteTexture(backgroundTextureID);
            shaderProgram.DeleteShader();


            base.OnUnload();
        }
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            shaderProgram.UseShader();
            DrawBackground();

            // --- ИЗМЕНЕНИЕ НАЧАЛО: Отрисовка колец ---
            // Вместо вызова RingDraw() вставляем его логику сюда и дублируем для второго кольца

            ring.RingBind(); // Связываем VAO/EBO/Текстуру кольца

            // Получаем ID uniform-переменных для шейдера (для колец)
            int ringmodelLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
            int ringviewLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "view");
            int ringprojectionLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "projection");

            // Получаем матрицы вида и проекции от камеры (одинаковы для всех объектов)
            Matrix4 currentView = camera != null ? camera.GetViewMatrix() : Matrix4.Identity;
            Matrix4 currentProjection = camera != null ? camera.GetProjection() : Matrix4.Identity;

            // Устанавливаем view и projection матрицы в шейдер (один раз для обоих колец)
            GL.UniformMatrix4(ringviewLocation, true, ref currentView);
            GL.UniformMatrix4(ringprojectionLocation, true, ref currentProjection);

            // --- Отрисовка ПЕРВОГО кольца ---
            Matrix4 ringtranslation1 = Matrix4.CreateTranslation(0f, 3.0f, -5f); // Увеличена высота первого кольца
            Matrix4 ringrotation = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(90f));
            Matrix4 ringscale = Matrix4.CreateScale(2f); // Уменьшен размер кольца
            Matrix4 ringmodel1 = ringscale * ringtranslation1 * ringrotation; // Порядок важен

            // Передаем модель первого кольца и рисуем
            GL.UniformMatrix4(ringmodelLocation, true, ref ringmodel1);
            GL.DrawElements(PrimitiveType.Triangles,
                           ring.ringIndices.Length,
                           DrawElementsType.UnsignedInt, 0);


            // --- Отрисовка ВТОРОГО кольца ---
            Matrix4 ringtranslation2 = Matrix4.CreateTranslation(0f, 3.0f, 5f); // Увеличена высота второго кольца
            // Используем те же scale и rotation
            Matrix4 ringmodel2 = ringscale * ringtranslation2 * ringrotation;

            // Передаем модель второго кольца и рисуем
            GL.UniformMatrix4(ringmodelLocation, true, ref ringmodel2);
            GL.DrawElements(PrimitiveType.Triangles,
                           ring.ringIndices.Length,
                           DrawElementsType.UnsignedInt, 0);

            // --- ИЗМЕНЕНИЕ КОНЕЦ ---


            if (camera != null)
            {
                DrawBorders();
            }
            DrawFloor(); // Отрисовка пола (без изменений)
            DrawBall(); // Отрисовка мяча (без изменений)

            // --- Отрисовка квадрата с текстурой команды ---
            Matrix4 teamTranslation = Matrix4.CreateTranslation(-3.5f, 0.5f, -5.5f); // Перемещение квадрата правее
            Matrix4 teamScale = Matrix4.CreateScale(3f); // Размер квадрата
            Matrix4 teamModel = teamScale * teamTranslation;

            GL.UniformMatrix4(modelLocation, true, ref teamModel);
            GL.BindTexture(TextureTarget.Texture2D, teamTextureID); // Привязка текстуры команды
            GL.BindVertexArray(teamVAO);
            GL.DrawElements(PrimitiveType.Triangles, squareIndices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            // --- Отрисовка синего 3D-столбика слева от первого щита ---
            Matrix4 poleTranslation1 = Matrix4.CreateTranslation(-5f, 1.5f, 0f); // Позиция первого столбика слева от поля
            Matrix4 poleRotation1 = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(90f)); // Поворот на 90 градусов
            Matrix4 poleScale = Matrix4.CreateScale(0.2f, 3f, 0.2f); // Размер столбика
            Matrix4 poleModel1 = poleScale * poleRotation1 * poleTranslation1;

            GL.UniformMatrix4(modelLocation, true, ref poleModel1);
            GL.BindTexture(TextureTarget.Texture2D, 0); // Без текстуры
            GL.Uniform4(GL.GetUniformLocation(shaderProgram.shaderHandle, "overrideColor"), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)); // Синий цвет
            GL.BindVertexArray(teamVAO); // Используем VAO квадрата для столбика
            GL.DrawElements(PrimitiveType.Triangles, squareIndices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            // --- Отрисовка синего 3D-столбика справа от второго щита ---
            Matrix4 poleTranslation2 = Matrix4.CreateTranslation(5f, 1.5f, 0f); // Позиция второго столбика справа от поля
            Matrix4 poleRotation2 = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(90f)); // Поворот на 90 градусов
            Matrix4 poleModel2 = poleScale * poleRotation2 * poleTranslation2;

            GL.UniformMatrix4(modelLocation, true, ref poleModel2);
            GL.BindTexture(TextureTarget.Texture2D, 0); // Без текстуры
            GL.Uniform4(GL.GetUniformLocation(shaderProgram.shaderHandle, "overrideColor"), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)); // Синий цвет
            GL.BindVertexArray(teamVAO); // Используем VAO квадрата для столбика
            GL.DrawElements(PrimitiveType.Triangles, squareIndices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);


            Context.SwapBuffers();
            base.OnRenderFrame(args);
        }
        protected void DrawBorders()
        {
            if (camera == null) return;
            borders.Bind();
            // Матрицы
            Matrix4 model = Matrix4.Identity;
            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = camera.GetProjection();
            int modelLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
            int viewLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "view");
            int projLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "projection");
            GL.UniformMatrix4(modelLoc, true, ref model);
            GL.UniformMatrix4(viewLoc, true, ref view);
            GL.UniformMatrix4(projLoc, true, ref projection);
            // Отключаем текстуру, задаём ЧЁРНЫЙ цвет вручную
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.UseProgram(shaderProgram.shaderHandle);
            int colorLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "overrideColor");
            if (colorLoc != -1)
                GL.Uniform4(colorLoc, new Vector4(0.0f, 0.0f, 0.0f, 1.0f)); // Чёрный
            GL.DrawElements(PrimitiveType.Triangles, borders.indices.Length, DrawElementsType.UnsignedInt, 0);
            // УДАЛЕНО: сброс overrideColor
        }
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            // Логика обновления осталась ТОЧНОЙ ЖЕ, как была в версии с черным фоном
            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }
            MouseState mouse = MouseState;
            KeyboardState input = KeyboardState;

            base.OnUpdateFrame(args);
            if (camera != null)
            {
                camera.Update(input, mouse, args);
            }


            if (mouse.IsButtonDown(MouseButton.Left) && !isBallThrown)
            {
                // Начальная позиция мяча берется из текущего положения "руки"
                ballPosition = CalculateHandPosition(); // Обновляем позицию перед броском
                if (camera != null)
                    ballVelocity = camera.Front * throwForce;
                isBallThrown = true;
            }

            // Возврат мяча
            if (mouse.IsButtonDown(MouseButton.Right))
            {
                isBallThrown = false;
                // Позиция мяча будет установлена в CalculateHandPosition() ниже
            }

            // Физика мяча
            if (isBallThrown)
            {
                ballVelocity.Y += gravity * (float)args.Time;
                ballPosition += ballVelocity * (float)args.Time;

                // Условие возврата мяча, если он упал слишком низко
                if (ballPosition.Y < -10f)
                {
                    isBallThrown = false;
                    // Позиция будет обновлена ниже
                }
            }

            // Если мяч не брошен, он следует за "рукой"
            if (!isBallThrown) // Эта проверка обновит позицию мяча если isBallThrown стал false выше
            {
                ballPosition = CalculateHandPosition();
            }

        }
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            this.width = e.Width;
            this.height = e.Height;
            // Логика камеры при ресайзе, если она есть, должна быть здесь (в оригинале не было)
        }

        protected void DrawFloor()
        {
            if (camera == null) return;
            // Сбросить overrideColor перед полом
            int colorLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "overrideColor");
            if (colorLoc != -1)
                GL.Uniform4(colorLoc, new Vector4(-1.0f));
            floor.FloorBind();
            Matrix4 floormodel = Matrix4.Identity;
            Matrix4 floorview = camera.GetViewMatrix();
            Matrix4 floorprojection = camera.GetProjection();
            Matrix4 floortranslation = Matrix4.CreateTranslation(0f, -0.5f, 0f);

            // --- ИЗМЕНЕНИЕ: Получаем uniform location для пола здесь ---
            int floormodelLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
            int floorviewLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "view");
            int floorprojectionLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "projection");
            // --- КОНЕЦ ИЗМЕНЕНИЯ ---

            // Комбинируем трансформации
            floormodel *= floortranslation;

            GL.UniformMatrix4(floormodelLocation, true, ref floormodel);
            GL.UniformMatrix4(floorviewLocation, true, ref floorview);
            GL.UniformMatrix4(floorprojectionLocation, true, ref floorprojection);

            GL.BindVertexArray(floor.floorVAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, floor.floorEBO);
            GL.DrawElements(PrimitiveType.Triangles,
                           floor.floorIndices.Length,
                           DrawElementsType.UnsignedInt, 0);
        }
        private Vector3 CalculateHandPosition()
        {
            if (camera == null) return Vector3.Zero;
            // Логика расчета позиции руки БЕЗ ИЗМЕНЕНИЙ
            return camera.position
                + camera.Right * handOffset.X
                + camera.Up * handOffset.Y
                + camera.Front * handOffset.Z;
        }
        protected void DrawBall()
        {
            if (camera == null) return;
            // Сбросить overrideColor перед мячом
            int colorLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "overrideColor");
            if (colorLoc != -1)
                GL.Uniform4(colorLoc, new Vector4(-1.0f));
            ball.BallBind();

            // Масштабирование мяча (радиус 0.5)
            Matrix4 scale = Matrix4.CreateScale(0.5f); // Было 0.7, теперь 0.5
            Matrix4 translation = Matrix4.CreateTranslation(ballPosition);
            Matrix4 model = scale * translation;

            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = camera.GetProjection();

            int modelLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
            int viewLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "view");
            int projLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "projection");

            GL.UniformMatrix4(modelLoc, true, ref model);
            GL.UniformMatrix4(viewLoc, true, ref view);
            GL.UniformMatrix4(projLoc, true, ref projection);

            GL.DrawElements(PrimitiveType.Triangles,
                ball.ballindices.Length,
                DrawElementsType.UnsignedInt, 0);
        }

        private int teamTextureID; // Идентификатор текстуры команды
        private uint[] squareIndices =
        {
            0, 1, 2,
            2, 3, 0
        };

        private int teamVAO, teamVBO, teamEBO; // Буферы для квадрата
        private float[] squareVertices =
        {
            // Позиции          // Текстурные координаты
            -0.5f, -0.5f, 0.0f,  0.0f, 0.0f,
             0.5f, -0.5f, 0.0f,  1.0f, 0.0f,
             0.5f,  0.5f, 0.0f,  1.0f, 1.0f,
            -0.5f,  0.5f, 0.0f,  0.0f, 1.0f
        };
    }
}

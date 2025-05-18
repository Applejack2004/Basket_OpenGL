
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
        Camera camera;
        Ring ring = new Ring();
        Floor floor = new Floor();
        Ball ball = new Ball();
        Vector3 ballPosition = new Vector3(0f, 0.5f, -3f); // Исходная позиция мяча перед камерой
        public Game(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            this.CenterWindow(new Vector2i(width, height));
            this.height = height;
            this.width = width;
            throwForce = 15f; // Сила броска как в оригинале
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            ring.RingLoad();
            floor.FloorLoad();
            ball.BallLoad();
            shaderProgram.LoadShader();


            // Камера в исходной позиции
            camera = new Camera(width, height, Vector3.Zero);
            CursorState = CursorState.Grabbed;
            GL.Enable(EnableCap.DepthTest);

        }



        protected override void OnUnload()
        {
            ring.RingUnload();
            floor.FloorUnload();
            ball.BallUnload();
            shaderProgram.DeleteShader();


            base.OnUnload();
        }
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            // Фон черный, как запрашивалось ранее
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shaderProgram.UseShader();

            // --- ИЗМЕНЕНИЕ НАЧАЛО: Отрисовка колец ---
            // Вместо вызова RingDraw() вставляем его логику сюда и дублируем для второго кольца

            ring.RingBind(); // Связываем VAO/EBO/Текстуру кольца

            // Получаем ID uniform-переменных для шейдера (для колец)
            int ringmodelLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
            int ringviewLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "view");
            int ringprojectionLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "projection");

            // Получаем матрицы вида и проекции от камеры (одинаковы для всех объектов)
            Matrix4 currentView = camera.GetViewMatrix();
            Matrix4 currentProjection = camera.GetProjection();

            // Устанавливаем view и projection матрицы в шейдер (один раз для обоих колец)
            GL.UniformMatrix4(ringviewLocation, true, ref currentView);
            GL.UniformMatrix4(ringprojectionLocation, true, ref currentProjection);

            // --- Отрисовка ПЕРВОГО кольца ---
            Matrix4 ringtranslation1 = Matrix4.CreateTranslation(0f, 2.5f, -5f); // Позиция первого кольца
            Matrix4 ringrotation = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(90f));
            Matrix4 ringscale = Matrix4.CreateScale(4f);
            Matrix4 ringmodel1 = ringscale * ringtranslation1 * ringrotation; // Порядок важен

            // Передаем модель первого кольца и рисуем
            GL.UniformMatrix4(ringmodelLocation, true, ref ringmodel1);
            GL.DrawElements(PrimitiveType.Triangles,
                           ring.ringIndices.Length,
                           DrawElementsType.UnsignedInt, 0);


            // --- Отрисовка ВТОРОГО кольца ---
            Matrix4 ringtranslation2 = Matrix4.CreateTranslation(0f, 2.5f, 5f); // Позиция второго кольца (изменена только Z)
            // Используем те же scale и rotation
            Matrix4 ringmodel2 = ringscale * ringtranslation2 * ringrotation;

            // Передаем модель второго кольца и рисуем
            GL.UniformMatrix4(ringmodelLocation, true, ref ringmodel2);
            GL.DrawElements(PrimitiveType.Triangles,
                           ring.ringIndices.Length,
                           DrawElementsType.UnsignedInt, 0);

            // --- ИЗМЕНЕНИЕ КОНЕЦ ---


            DrawFloor(); // Отрисовка пола (без изменений)
            DrawBall(); // Отрисовка мяча (без изменений)
            Context.SwapBuffers();
            base.OnRenderFrame(args);
        }
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            // Логика обновления осталась ТОЧНО ТАКОЙ ЖЕ, как была в версии с черным фоном
            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }
            MouseState mouse = MouseState;
            KeyboardState input = KeyboardState;

            base.OnUpdateFrame(args);
            camera.Update(input, mouse, args);


            if (mouse.IsButtonDown(MouseButton.Left) && !isBallThrown)
            {
                // Начальная позиция мяча берется из текущего положения "руки"
                ballPosition = CalculateHandPosition(); // Обновляем позицию перед броском
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

        // Оригинальный метод RingDraw() больше не используется, т.к. его логика встроена в OnRenderFrame
        /*
        protected void RingDraw()
        {
            // Этот метод больше не нужен
        }
        */

        protected void DrawFloor()
        {
            // Логика отрисовки пола осталась БЕЗ ИЗМЕНЕНИЙ
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
            // Логика расчета позиции руки БЕЗ ИЗМЕНЕНИЙ
            return camera.position
                + camera.Right * handOffset.X
                + camera.Up * handOffset.Y
                + camera.Front * handOffset.Z;
        }
        protected void DrawBall()
        {
            // Логика отрисовки мяча осталась БЕЗ ИЗМЕНЕНИЙ
            ball.BallBind();

            // Используем текущую позицию мяча (обновляется в OnUpdateFrame)
            Matrix4 model = Matrix4.CreateTranslation(ballPosition);

            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = camera.GetProjection();

            // --- ИЗМЕНЕНИЕ: Получаем uniform location для мяча здесь ---
            int modelLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
            int viewLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "view");
            int projLoc = GL.GetUniformLocation(shaderProgram.shaderHandle, "projection");
            // --- КОНЕЦ ИЗМЕНЕНИЯ ---

            GL.UniformMatrix4(modelLoc, true, ref model);
            GL.UniformMatrix4(viewLoc, true, ref view);
            GL.UniformMatrix4(projLoc, true, ref projection);

            GL.DrawElements(PrimitiveType.Triangles,
                ball.ballindices.Length,
                DrawElementsType.UnsignedInt, 0);
        }
    }
}
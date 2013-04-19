using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;

namespace GameWindow
{
    class GameWindowTest : OpenTK.GameWindow
    {
        Matrix4 proj;
        Matrix4 camera;
        Cube cube;
        HUD hud;
        float angle = 0;
        float[] mouseSpeed = new float[2];
        Vector2 mouseDelta = new Vector2();
        Vector3 location = new Vector3(0f, 0f, 10f);
        Vector3 up = Vector3.UnitY;
        float pitch = 0.0f;
        float facing = 0.0f;
        bool fps = false;
        
        // вершинний шейдер
        string vShaderSource = @"#version 150
                                 in vec3 position;
                                 in vec3 color;
                                 //in vec2 texcoord;
                                 out vec3 Color;
                                 //out vec2 Texcoord;
                                 uniform mat4 proj;
                                 uniform mat4 view;
                                 void main(){ 
                                    Color = color; 
                                    //Texcoord = texcoord;
                                    gl_Position = proj*view*vec4 (position, 1.0);
                                 }";
        // фрагментний шейдер
        string fShaderSource = @"#version 150 
                                 in vec3 Color;
                                 //in vec2 Texcoord;
                                 out vec4 outColor;
                                 uniform sampler2D tex;
                                 void main(){ 
                                    outColor = vec4(Color, 0.5);//*texture(tex, vec2(0.0, 1.0));
                                 }";
        // вершини і грані
        float[] vertexes = {
            //   x      y      z     r     g     b  
            0.5f,  0.5f,  0.5f, 1.0f, 0.0f, 0.0f,  // 0
            1.5f,  0.5f,  0.5f, 1.0f, 0.0f, 0.0f,  // 1
            1.5f, -0.5f,  0.5f, 0.0f, 0.0f, 1.0f,  // 2
            0.5f, -0.5f,  0.5f, 0.0f, 0.0f, 1.0f,  // 3
            0.5f,  0.5f, -0.5f, 0.0f, 1.0f, 0.0f,  // 4
            1.5f,  0.5f, -0.5f, 0.0f, 1.0f, 0.0f,  // 5
            1.5f, -0.5f, -0.5f, 0.0f, 1.0f, 1.0f,  // 6
            0.5f, -0.5f, -0.5f, 1.0f, 0.0f, 1.0f   // 7
        };
        byte[] elements = {
            1, 0, 2, // front
            3, 2, 0,
            4, 5, 6, // back
            4, 6, 7,
            0, 7, 3, // left
            0, 4, 7,
            5, 2, 6, // right
            1, 2, 5,
            0, 5, 4, // top
            0, 1, 5,
            2, 3, 6, // bottom
            3, 7, 6
        };

        public GameWindowTest() : base(800,600)
        {
            this.VSync = VSyncMode.Off;
            this.WindowBorder = OpenTK.WindowBorder.Fixed;
            Keyboard.KeyDown += Keyboard_KeyDown;
            Mouse.ButtonUp += Mouse_ButtonUp;
        }

        void Mouse_ButtonUp(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("Location = " + location);
            Console.WriteLine("Facing = " + facing + "; pitch = " + pitch);
        }

        void Keyboard_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
        {
            // v0.3
            switch (e.Key)
            {
                case Key.F:
                    this.WindowState = (this.WindowState == WindowState.Fullscreen) ? WindowState.Normal : OpenTK.WindowState.Fullscreen;
                    break;
                case Key.V:
                    this.VSync = (this.VSync == VSyncMode.On) ? VSyncMode.Off : VSyncMode.On;
                    break;
                case Key.Escape:
                    this.Exit();
                    break;
                case Key.Tilde:
                    fps = !fps;
                    break;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            hud = new HUD(ClientRectangle.Width, ClientRectangle.Height);
            cube = new Cube(vShaderSource, fShaderSource, vertexes, elements);
            camera = Matrix4.Identity;
            Cursor.Position = new Point((Bounds.Left + Bounds.Right)/2, (Bounds.Top + Bounds.Bottom)/2);
            Cursor.Hide();

            GL.ClearColor(Color.Black);
            GL.Viewport(0, 0, Width, Height);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            proj = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1f, 100f);
            if (!cube.IsInitShaders)
            {
                Console.WriteLine("Cannot initialize shaders!");
                this.Exit();
            };
        }
        
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (!this.Exists) return;
            Matrix4 view;
            angle = angle < 360f ? angle + 0.001f : 0f;
            Matrix4.CreateRotationZ(angle, out view);
            view *= camera; // Matrix4.LookAt(2, 2, 2, 0, 0, 0, 0, 0, 1);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            if (fps) hud.Draw("FPS: " + (int)this.RenderFrequency);
            cube.Draw(ref view, ref proj);
            
            this.SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            this.Title = "FPS: " + (int)this.RenderFrequency;
            if (Keyboard[Key.W])
            {
                location.X += (float)Math.Cos(facing) * 0.01f;
                location.Z += (float)Math.Sin(facing) * 0.01f;
            }
            if (Keyboard[Key.S])
            {
                location.X -= (float)Math.Cos(facing) * 0.01f;
                location.Z -= (float)Math.Sin(facing) * 0.01f;
            }
            if (Keyboard[Key.A])
            {
                location.X -= (float)Math.Cos(facing + Math.PI / 2) * 0.01f;
                location.Z -= (float)Math.Sin(facing + Math.PI / 2) * 0.01f;
            }
            if (Keyboard[Key.D])
            {
                location.X += (float)Math.Cos(facing + Math.PI / 2) * 0.01f;
                location.Z += (float)Math.Sin(facing + Math.PI / 2) * 0.01f;
            }

            var center = new Point((Bounds.Left + Bounds.Right) / 2, (Bounds.Top + Bounds.Bottom) / 2);
            mouseDelta = new Vector2(Mouse.X - PointToClient(center).X, Mouse.Y - PointToClient(center).Y);
            Cursor.Position = center;
            mouseSpeed[0] *= 0.9f;
            mouseSpeed[1] *= 0.9f;
            mouseSpeed[0] += mouseDelta.X / 1000f;
            mouseSpeed[1] -= mouseDelta.Y / 1000f;

            facing += mouseSpeed[0];
            pitch += mouseSpeed[1];
            Vector3 lookatPoint = new Vector3((float)Math.Cos(facing), (float)Math.Sin(pitch), (float)Math.Sin(facing));
            camera = Matrix4.LookAt(location, location + lookatPoint, up);
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            proj = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1f, 100f);
        }

        [STAThread]
        static void Main()
        {
            using (GameWindowTest test = new GameWindowTest())
            {
                test.Run();
            };
        }
    }

    public class Renderer
    {
        public int ShaderProgram
        {
            get;
            protected set;
        }

        public bool IsInitShaders
        {
            get;
            protected set;
        }

        protected uint vbo, vao, ebo;
        protected int vShader, fShader, texture;
        protected int posAttr, colAttr, texAttr;

        protected bool CreateShaders(string vSrc, string fSrc)
        {
            // створюємо вершинний шейдер
            vShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vShader, vSrc);
            GL.CompileShader(vShader);
            // створюємо фрагментний шейдер
            fShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fShader, fSrc);
            GL.CompileShader(fShader);
            // об'єднуємо шейдери в програму
            ShaderProgram = GL.CreateProgram();
            GL.AttachShader(ShaderProgram, vShader);
            GL.AttachShader(ShaderProgram, fShader);
            GL.BindFragDataLocation(ShaderProgram, 0, "outColor");
            GL.LinkProgram(ShaderProgram);
            // перевірки
            int status;
            GL.GetShader(vShader, ShaderParameter.CompileStatus, out status);
            if (status != 1) return false;
            GL.GetShader(fShader, ShaderParameter.CompileStatus, out status);
            if (status != 1) return false;
            GL.GetProgram(ShaderProgram, ProgramParameter.LinkStatus, out status);
            if (status != 1) return false;
            return true;
        }

        protected bool LoadTexture(string path)
        {
            if (System.IO.File.Exists(path))
            {
                Bitmap texBitmap = new Bitmap(path);
                System.Drawing.Imaging.BitmapData texData = texBitmap.LockBits(new Rectangle(0, 0, texBitmap.Width, texBitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                GL.GenTextures(1, out texture);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, texture);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, texBitmap.Width, texBitmap.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, texData.Scan0);
                GL.Uniform1(GL.GetUniformLocation(ShaderProgram, "tex"), 0);
                GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvMode.Modulate);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToEdge);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                texBitmap.UnlockBits(texData);
            }
            else return false;
            return true;
        }

        protected Renderer(string vSrc, string fSrc)
        {
            IsInitShaders = CreateShaders(vSrc, fSrc);
        }
    }

    public class Cube : Renderer
    {
        private float[] vertexes;
        private byte[] elements;

        public Cube(string vSrc, string fSrc, float[] vertexes, byte[] elements) : base(vSrc, fSrc)
        {
            this.vertexes = vertexes;
            this.elements = elements;
            
            GL.GenVertexArrays(1, out vao);
            GL.BindVertexArray(vao);

            GL.GenBuffers(1, out vbo);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(vertexes.Length * sizeof(float)), vertexes, BufferUsageHint.StaticDraw);
            
            GL.GenBuffers(1, out ebo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(elements.Length * sizeof(byte)), elements, BufferUsageHint.StaticDraw);

            int posAttr = GL.GetAttribLocation(ShaderProgram, "position");
            GL.EnableVertexAttribArray(posAttr);
            GL.VertexAttribPointer(posAttr, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            
            int colAttr = GL.GetAttribLocation(ShaderProgram, "color");
            GL.EnableVertexAttribArray(colAttr);
            GL.VertexAttribPointer(colAttr, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));

            // це текстура, вона грузиться, але не юзається
            int texAttr = GL.GetAttribLocation(ShaderProgram, "texcoord");
            GL.EnableVertexAttribArray(texAttr);
            GL.VertexAttribPointer(texAttr, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
            if (!LoadTexture("texture.png")) IsInitShaders = false;
        }

        public void Draw(ref Matrix4 view, ref Matrix4 proj)
        {
            int uniView = GL.GetUniformLocation(this.ShaderProgram, "view");
            GL.UniformMatrix4(uniView, false, ref view);
            int uniProj = GL.GetUniformLocation(this.ShaderProgram, "proj");
            GL.UniformMatrix4(uniProj, false, ref proj);

            GL.Enable(EnableCap.DepthTest);            
            GL.DepthMask(true);
            GL.Enable(EnableCap.CullFace);
            GL.BindVertexArray(vao); 
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.UseProgram(ShaderProgram);
            GL.DrawElements(BeginMode.Triangles, 36, DrawElementsType.UnsignedByte, 0);
        }
    }

    public sealed class HUD : Renderer
    {
        private static System.Drawing.Bitmap TextureBitmap; 
        private static readonly Font TextFont = new Font(FontFamily.GenericMonospace, 12);
        private RectangleF rect;

        private static float[] vertexes = {
                -1.0f,  1.0f, 0.0f, 0.0f,
                 1.0f,  1.0f, 1.0f, 0.0f,
                 1.0f, -1.0f, 1.0f, 1.0f,
                -1.0f, -1.0f, 0.0f, 1.0f 
            };
        private static uint[] elements = {
		        1, 0, 2,
		        3, 2, 0
	        };
        private static string vShaderSource = @"#version 150
                                                in vec2 texcoord;
                                                in vec2 position;
                                                out vec2 Texcoord;
                                                void main(){ 
                                                    Texcoord = texcoord;
                                                    gl_Position = vec4(position, 0.0, 1.0);
                                                }";
        private static string fShaderSource = @"#version 150 
                                                in vec2 Texcoord;
                                                out vec4 outColor;
                                                uniform sampler2D tex;
                                                void main(){ 
                                                    outColor = texture(tex, Texcoord);
                                                }";

        public HUD(float width, float height) : base(vShaderSource, fShaderSource)
        {
            rect.Width = width;
            rect.Height = height;
            
            GL.GenVertexArrays(1, out vao);
	        GL.BindVertexArray(vao);

	        GL.GenBuffers(1, out vbo);
        	GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
	        GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(vertexes.Length * sizeof(float)), vertexes, BufferUsageHint.StaticDraw);

	        GL.GenBuffers(1, out ebo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(elements.Length * sizeof(uint)), elements, BufferUsageHint.StaticDraw);
            
            int posAttr = GL.GetAttribLocation(ShaderProgram, "position");
            GL.EnableVertexAttribArray(posAttr);
            GL.VertexAttribPointer(posAttr, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            int texAttr = GL.GetAttribLocation(ShaderProgram, "texcoord");
            GL.EnableVertexAttribArray(texAttr);
            GL.VertexAttribPointer(texAttr, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            
            TextureBitmap = new Bitmap((int)width, (int)height);

            GL.Enable(EnableCap.Texture2D);
            texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, TextureBitmap.Width, TextureBitmap.Height, 0, PixelFormat.Bgra,
                            PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
        }

        public void Draw(String str)
        {
            using (Graphics gfx = Graphics.FromImage(TextureBitmap))
            {
                gfx.Clear(Color.Transparent);
                gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                gfx.DrawString(str, TextFont, Brushes.White, new PointF(0, 0));
            }

            System.Drawing.Imaging.BitmapData data = TextureBitmap.LockBits(new Rectangle(0, 0, TextureBitmap.Width, TextureBitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, TextureBitmap.Width, TextureBitmap.Height, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            TextureBitmap.UnlockBits(data);

            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Ortho(0, rect.Width, 0, rect.Height, -1, 1);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
            GL.Disable(EnableCap.DepthTest);
            GL.DepthMask(false);
            GL.Disable(EnableCap.CullFace);

            GL.BindVertexArray(vao); 
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.UseProgram(ShaderProgram);
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvParameter.CombineAlpha);
            GL.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedInt, 0);

            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix();
        }
    }

    public sealed class Screen : Renderer
    {
        private static float[] vertexes = {
                -1.0f,  1.0f, 0.0f, 0.0f,
                 1.0f,  1.0f, 1.0f, 0.0f,
                 1.0f, -1.0f, 1.0f, 1.0f,
                -1.0f, -1.0f, 0.0f, 1.0f 
            };
        private static uint[] elements = {
		        1, 0, 2,
		        3, 2, 0
	        };
        private static string vShaderSource = @"#version 150
                                                in vec2 texcoord;
                                                in vec2 position;
                                                out vec2 Texcoord;
                                                void main(){ 
                                                    Texcoord = texcoord;
                                                    gl_Position = vec4(position, 0.0, 1.0);
                                                }";
        private static string fShaderSource = @"#version 150 
                                                in vec2 Texcoord;
                                                out vec4 outColor;
                                                uniform sampler2D tex;
                                                void main(){ 
                                                    outColor = texture(tex, Texcoord);
                                                }";
        uint fbo, rbo;

        public Screen() : base(vShaderSource, fShaderSource)
        {
            GL.GenVertexArrays(1, out vao);
	        GL.BindVertexArray(vao);

	        GL.GenBuffers(1, out vbo);
        	GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
	        GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(vertexes.Length * sizeof(float)), vertexes, BufferUsageHint.StaticDraw);

	        GL.GenBuffers(1, out ebo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(elements.Length * sizeof(uint)), elements, BufferUsageHint.StaticDraw);
            
            int posAttr = GL.GetAttribLocation(ShaderProgram, "position");
            GL.EnableVertexAttribArray(posAttr);
            GL.VertexAttribPointer(posAttr, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            int texAttr = GL.GetAttribLocation(ShaderProgram, "texcoord");
            GL.EnableVertexAttribArray(texAttr);
            GL.VertexAttribPointer(texAttr, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            GL.GenFramebuffers(1, out fbo);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 800, 600, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture, 0);

            GL.GenRenderbuffers(1, out rbo);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, 800, 600);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, rbo);
        }

        public void Draw(Action Render)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            Render.Invoke();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Disable(EnableCap.DepthTest);
            GL.UseProgram(ShaderProgram);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.BindVertexArray(vao); 
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo); 
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        }
    }
}

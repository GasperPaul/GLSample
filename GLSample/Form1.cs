using System;
using System.ComponentModel;
//using System.Data;
using System.Drawing;
//using System.Text;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace GLSample
{
    public partial class Form1 : Form
    {
        bool isContext = false;
        Timer timer = new Timer();
        int c = 0;
        Matrix4 proj;
        Cube cube;
        float angle = 0;
        bool fullscr = false;
        
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // from v0.1, прибрати
        }

        void timer_Tick(object sender, EventArgs e)
        {
            this.Text = "FPS: " + c.ToString();
            c = 0;
        }

        void Application_Idle(object sender, EventArgs e)
        {
            glControl1.Invalidate();
        }

        private void glControl1_Load(object sender, EventArgs e)
        {
            isContext = true;
            cube = new Cube();
            GL.ClearColor(Color.Black);
            GL.Viewport(0, 0, glControl1.Width, glControl1.Height);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            proj = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, glControl1.Width / (float)glControl1.Height, 1f, 10f);
            if (!cube.IsInitShaders) 
            {
                MessageBox.Show("Cannot initialize shaders!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            };
            Application.Idle += new EventHandler(Application_Idle);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = 1000;
            timer.Start();
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            if (!isContext) return;
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            //DrawObjects();
            //GL.DrawArrays(BeginMode.Triangles, 0, 3);
            cube.Draw();
            Matrix4 view;
            angle = angle < 360f ? angle + 0.001f : 0f;
            Matrix4.CreateRotationZ(angle, out view); 
            view *= Matrix4.LookAt(2, 2, 2, 0, 0, 0, 0, 0, 1);
            int uniView = GL.GetUniformLocation(cube.shaderProgram, "view");
            GL.UniformMatrix4(uniView, false, ref view);
            int uniProj = GL.GetUniformLocation(cube.shaderProgram, "proj");
            GL.UniformMatrix4(uniProj, false, ref proj);
            glControl1.SwapBuffers();
            c++;
        }

        private static void DrawObjects()
        {
            // hello, копіпаста!
            // цей спосіб застарів!
            GL.Begin(BeginMode.Triangles);
            GL.Color3(Color.Red); // красный
            GL.Vertex3(-0.5f, -0.5f, 0.0f);
            GL.Color3(Color.Green); // зелений
            GL.Vertex3(0.5f, 0.0f, 0.0f);
            GL.Color3(Color.Blue); // синий
            GL.Vertex3(-0.5f, 0.5f, 0.0f);
            GL.End();
        }

        private void glControl1_Resize(object sender, EventArgs e)
        {
            GL.Viewport(0, 0, glControl1.Width, glControl1.Height);
            //glControl1.Invalidate();
        }

        private void glControl1_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            // v0.2, треба пізніше це переписати
            if (e.KeyChar.ToString().ToUpper()==Key.F.ToString()) 
            {
                if (!fullscr)
                {
                    this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    this.TopMost = true;
                    DisplayDevice.Default.ChangeResolution(DisplayDevice.Default.Width, DisplayDevice.Default.Height, 
                                                           DisplayDevice.Default.BitsPerPixel, DisplayDevice.Default.RefreshRate);
                    this.WindowState = FormWindowState.Maximized;
                }
                else
                {
                    this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                    this.TopMost = false;
                    DisplayDevice.Default.RestoreResolution();
                    this.WindowState = FormWindowState.Normal;
                }
                fullscr = !fullscr;
            };
        }
    }

    public class Cube
    {
        int ShaderProgram;
        public int shaderProgram
        {
            get { return ShaderProgram; }
        }

        private bool isInitShaders;
        public bool IsInitShaders
        {
            get { return isInitShaders; }
        }

        public Cube()
        {
            isInitShaders = InitShaders();
        }

        private bool InitShaders()
        {
            // вершини і буфер
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
            byte[] polygons = {
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
            uint vertexBuffer;
            uint vertexObject;
            uint elementBuffer;
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
            
            int status;

            GL.GenVertexArrays(1, out vertexObject);
            GL.BindVertexArray(vertexObject);
            GL.GenBuffers(1, out vertexBuffer);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(vertexes.Length*sizeof(float)), vertexes, BufferUsageHint.StaticDraw);
            GL.GenBuffers(1, out elementBuffer);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBuffer);
            GL.BufferData( BufferTarget.ElementArrayBuffer,new IntPtr(polygons.Length*sizeof(byte)),polygons, BufferUsageHint.StaticDraw);
            // створюємо вершинний шейдер
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vShaderSource);
            GL.CompileShader(vertexShader);
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out status);
            if (status != 1) return false;
            // створюємо фрагментний шейдер
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fShaderSource);
            GL.CompileShader(fragmentShader);
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out status);
            if (status != 1) return false;
            // об'єднуємо шейдери в програму
            ShaderProgram = GL.CreateProgram();
            GL.AttachShader(ShaderProgram, vertexShader);
            GL.AttachShader(ShaderProgram, fragmentShader);
            GL.BindFragDataLocation(ShaderProgram, 0, "outColor");
            GL.LinkProgram(ShaderProgram);
            GL.GetProgram(ShaderProgram, ProgramParameter.LinkStatus, out status);
            if (status != 1) return false; 
            GL.UseProgram(ShaderProgram);
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
            if (System.IO.File.Exists("texture.png"))
            {
                Bitmap texBitmap = new Bitmap("texture.png");
                System.Drawing.Imaging.BitmapData texData = texBitmap.LockBits(new Rectangle(0, 0, texBitmap.Width, texBitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                int textures;
                GL.GenTextures(1, out textures);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, textures);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, texBitmap.Width, texBitmap.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, texData.Scan0);
                GL.Uniform1(GL.GetUniformLocation(ShaderProgram, "tex"), 0);
                GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvMode.Modulate);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToEdge);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                texBitmap.UnlockBits(texData);
            } else return false;
            return true;
        }
        
        public void Draw()
        {
            GL.DrawElements(BeginMode.Triangles, 36, DrawElementsType.UnsignedByte, 0);
        }
    }
}

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace OpenGM;

/// <summary>
/// handles drawing using modern opengl
/// </summary>
public static class VertexManager
{
    public static int u_view;
    public static int u_doTex;
    public static int u_flipY;

    [StructLayout(LayoutKind.Explicit)]
    public struct Vertex
    {
        [FieldOffset(0 * sizeof(float))] public Vector2 pos;
        [FieldOffset(2 * sizeof(float))] public Vector4 color;
        [FieldOffset((2 + 4) * sizeof(float))] public Vector2 uv;

        public Vertex(Vector2d pos, Color4 color, Vector2d uv)
        {
            this.pos = (Vector2)pos;
            this.color = (Vector4)color;
            this.uv = (Vector2)uv;
        }
    }

    /// <summary>
    /// setup shader and buffer
    /// </summary>
    public static void Init()
    {
        // use one shader for everything
        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, File.ReadAllText("shader.vert"));
        GL.CompileShader(vertexShader);
        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out var code);
        if (code != (int)All.True)
        {
            var infoLog = GL.GetShaderInfoLog(vertexShader);
            throw new Exception($"Error while compiling shader.vert.\n\n{infoLog}");
        }

        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, File.ReadAllText("shader.frag"));
        GL.CompileShader(fragmentShader);
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out code);
        if (code != (int)All.True)
        {
            var infoLog = GL.GetShaderInfoLog(fragmentShader);
            throw new Exception($"Error while compiling shader.frag.\n\n{infoLog}");
        }

        var program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out code);
        if (code != (int)All.True)
        {
            var infoLog = GL.GetProgramInfoLog(program);
            throw new Exception($"Error while linking program.\n\n{infoLog}");
        }

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        u_view = GL.GetUniformLocation(program, "u_view");
        u_doTex = GL.GetUniformLocation(program, "u_doTex");
        u_flipY = GL.GetUniformLocation(program, "u_flipY");

        GL.UseProgram(program);


        // use one buffer for everything
        var vao = GL.GenVertexArray();
        var vbo = GL.GenBuffer();

        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<Vertex>(), 0 * sizeof(float));
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, Unsafe.SizeOf<Vertex>(), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<Vertex>(), (2 + 4) * sizeof(float));
        GL.EnableVertexAttribArray(2);
    }

    /// <summary>
    /// draw some vertices
    /// </summary>
    // TODO: dont have to allocate vertex array probably
    public static void Draw(PrimitiveType primitiveType, Vertex[] vertices)
    {
        // TODO: cache over frames?
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * Unsafe.SizeOf<Vertex>(), vertices, BufferUsageHint.StreamDraw);
        GL.DrawArrays(primitiveType, 0, vertices.Length);
    }
}
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenGM;

/// <summary>
/// handles drawing using modern opengl
/// </summary>
public static class GraphicsManager
{
    private static int u_view; // goes away when matrices are implemented
    public static int u_doTex; // TODO: replace with 1x1 white texture
    public static int u_flipY; // when matrices are implemented this becomes a check in GetProjMat

    public static int alphaTestEnabled;
    public static int alphaRefValue;

    [StructLayout(LayoutKind.Explicit)]
    public struct Vertex(Vector2d pos, Color4 color, Vector2d uv)
    {
        [FieldOffset(0 * sizeof(float))] public Vector2 pos = (Vector2)pos;
        [FieldOffset(2 * sizeof(float))] public Vector4 color = (Vector4)color;
        [FieldOffset((2 + 4) * sizeof(float))] public Vector2 uv = (Vector2)uv;
        // TODO: match format with gamemaker for when we do shaders
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

        alphaTestEnabled = GL.GetUniformLocation(program, "alphaTestEnabled");
        alphaRefValue = GL.GetUniformLocation(program, "alphaRefValue");

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
    public static void Draw(PrimitiveType primitiveType, Span<Vertex> vertices)
    {
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * Unsafe.SizeOf<Vertex>(), ref vertices.GetPinnableReference(), BufferUsageHint.StreamDraw);
        GL.DrawArrays(primitiveType, 0, vertices.Length);
    }

    public static Vector4i ViewPort { get; private set; }
    public static void SetViewPort(Vector4i viewport)
    {
        ViewPort = viewport;
        GL.Viewport(viewport.X, viewport.Y, viewport.Z, viewport.W);
    }
    public static void SetViewPort(int x, int y, int w, int h) => SetViewPort(new(x, y, w, h));

    public static Vector4 ViewArea { get; private set; }
    public static void SetViewArea(Vector4 viewArea)
    {
        ViewArea = viewArea;
        GL.Uniform4(u_view, ViewArea);
    }
    public static void SetViewArea(float x, float y, float w, float h) => SetViewArea(new(x, y, w, h));
}
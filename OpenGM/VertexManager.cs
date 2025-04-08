using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenGM.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace OpenGM;

/// <summary>
/// handles drawing using modern opengl
/// </summary>
public static class VertexManager
{
    /// <summary>
    /// setup shaders
    /// </summary>
    public static void Init()
    {
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

        // use the shader for everything
        GL.UseProgram(program);
        DebugLog.LogInfo("SHADER SUCCESS");
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Vertex
    {
        [FieldOffset(0 * sizeof(float))] public Vector2 pos;
        [FieldOffset(2 * sizeof(float))] public Vector4 color;
        [FieldOffset((2 + 4) * sizeof(float))] public Vector2 uv;

        public Vertex(Vector2 pos, Vector4 color, Vector2 uv)
        {
            this.pos = pos;
            this.color = color;
            this.uv = uv;
        }
    }

    /// <summary>
    /// draw some vertices
    /// </summary>
    public static void Draw(PrimitiveType primitiveType, Vertex[] vertices)
    {
        // TODO: cache over frames
        var vao = GL.GenVertexArray();
        var vbo = GL.GenBuffer();

        GL.BindVertexArray(vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        var vertexSize = Unsafe.SizeOf<Vertex>();
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * vertexSize, vertices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, vertexSize, 0 * sizeof(float));
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, vertexSize, 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, vertexSize, (2 + 4) * sizeof(float));
        GL.EnableVertexAttribArray(2);

        GL.DrawArrays(primitiveType, 0, vertices.Length);

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);

        GL.DeleteVertexArray(vao);
        GL.DeleteBuffer(vbo);
    }

    /// <summary>
    /// remove all unused buffers. mark used buffers as unused
    /// </summary>
    private static void ClearUnusedBuffers()
    {
    }
}
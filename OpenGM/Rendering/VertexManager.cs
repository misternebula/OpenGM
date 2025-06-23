using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenGM.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace OpenGM.Rendering;

/// <summary>
/// handles drawing using modern opengl
/// </summary>
public static class VertexManager
{
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
	    DebugLog.LogInfo($"Compiling shaders...");
	    ShaderManager.CompileShaders();

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
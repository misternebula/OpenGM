using OpenGM.IO;
using OpenGM.Rendering;
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
    [StructLayout(LayoutKind.Explicit)]
    public struct Vertex(Vector2d pos, Color4 color, Vector2d uv)
    {
        [FieldOffset(0 * sizeof(float))] public Vector2 pos = (Vector2)pos;
        [FieldOffset(2 * sizeof(float))] public Vector4 color = (Vector4)color;
        [FieldOffset((2 + 4) * sizeof(float))] public Vector2 uv = (Vector2)uv;
        // TODO: match format with gamemaker for when we do shaders
    }

    /// <summary>
    /// 1x1 white image is used when for things that dont need textures
    /// </summary>
    public static int DefaultTexture;

    /// <summary>
    /// True when drawing to the front buffer.
    /// </summary>
    public static bool RenderTargetActive;

    /// <summary>
    /// setup shader and buffer
    /// </summary>
    public static void Init()
    {
        DefaultTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, DefaultTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, new byte[] { 255, 255, 255, 255 });
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);

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

        ShaderManager.CompileShaders();
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
    }
    public static void SetViewArea(float x, float y, float w, float h) => SetViewArea(new(x, y, w, h));

    public static void SetFog(bool enable, int color, double start, double end)
    {
        GL.Uniform1(ShaderManager.gm_FogStart, start);

        var range = end - start;
        var rcpRange = range == 0 ? 0 : 1 / range;
        GL.Uniform1(ShaderManager.gm_RcpFogRange, rcpRange);

        GL.Uniform1(ShaderManager.gm_PS_FogEnabled, enable ? 1 : 0);
        GL.Uniform1(ShaderManager.gm_FogColour, color);
        GL.Uniform1(ShaderManager.gm_VS_FogEnabled, enable ? 1 : 0);
    }
}
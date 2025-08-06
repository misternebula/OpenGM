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
    public struct Vertex(Vector3d pos, Color4 color, Vector2d uv)
    {
        [FieldOffset(0 * sizeof(float))] public Vector3 pos = (Vector3)pos;
        [FieldOffset(3 * sizeof(float))] public Vector4 color = (Vector4)color;
        [FieldOffset((3 + 4) * sizeof(float))] public Vector2 uv = (Vector2)uv;
        // TODO: match format with gamemaker for when we do shaders
    }

    /// <summary>
    /// 1x1 white image is used when for things that dont need textures
    /// </summary>
    public static int DefaultTexture;

    public static double GR_Depth;
    public static bool ForceDepth;
    public static double ForcedDepth;

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
        CheckError();

        // use one buffer for everything
        var vao = GL.GenVertexArray();
        var vbo = GL.GenBuffer();
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        CheckError();

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Unsafe.SizeOf<Vertex>(), 0 * sizeof(float));
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, Unsafe.SizeOf<Vertex>(), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<Vertex>(), (3 + 4) * sizeof(float));
        GL.EnableVertexAttribArray(2);
        CheckError();
    }

    public static void CheckError(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = -1)
    {
        var error = GL.GetError();

        if (error == ErrorCode.NoError)
        {
            return;
        }

        DebugLog.LogError($"[GL Error] - {error} : {memberName} line {lineNumber} ({filePath})");
    }

    /// <summary>
    /// draw some vertices
    /// </summary>
    public static void Draw(PrimitiveType primitiveType, Span<Vertex> vertices)
    {
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * Unsafe.SizeOf<Vertex>(), ref vertices.GetPinnableReference(), BufferUsageHint.StreamDraw);
        CheckError();
        GL.DrawArrays(primitiveType, 0, vertices.Length);
        CheckError();
    }

    public static Vector4i ViewPort { get; private set; }
    public static void SetViewPort(Vector4i viewPort) => SetViewPort(viewPort.X, viewPort.Y, viewPort.Z, viewPort.W);
    public static void SetViewPort(int x, int y, int w, int h)
    {
        ViewPort = new(x, y, w, h);
        GL.Viewport(x, y, w, h);
        CheckError();
    }

    // rn we're just replicating g_isZeus = false, where instead of doing camera stuff we just have view area globals
    public static Vector4 ViewArea { get; private set; }
    public static void SetViewArea(Vector4 viewArea) => SetViewArea(viewArea.X, viewArea.Y, viewArea.Z, viewArea.W, 0);
    public static void SetViewArea(float x, float y, float w, float h, float angle)
    {
        ViewArea = new(x, y, w, h);
        // we dont preserve angle. oh well

        // literally just copy from camera building and applying matrices

        x = x + (w / 2);
        y = y + (h / 2);

        var pos = new Vector3(x, y, -16000);
        var at = new Vector3(x, y, 0);
        var up = new Vector3((float)Math.Sin(-angle * CustomMath.Deg2Rad), (float)Math.Cos(-angle * CustomMath.Deg2Rad),
            0);

        var view = Matrix4.LookAt(at, pos, up);
        var proj = Matrix4.CreateOrthographic(w, h, 0, 32000);

        if (!SurfaceManager.DrawingToBackbuffer())
        {
            // no flip when drawing to non-backbuffer
        }
        else
        {
            // flip when drawing to backbuffer
            var flipMat = Matrix4.Identity;
            flipMat[1, 1] = -1;

            proj = proj * flipMat; // TODO right way round?
        }

        var world = Matrix4.Identity;
        var worldView = world * view;
        var worldviewProjection = worldView * proj;

        var matrices = new Matrix4[] { view, proj, world, worldView, worldviewProjection };

        unsafe
        {
            fixed (Matrix4* ptr = &matrices[0])
            {
                GL.UniformMatrix4(ShaderManager.gm_Matrices, matrices.Length, false, (float*)ptr);
                CheckError();
            }
        }
    }

    public static void SetFog(bool enable, Color4 color, float start, float end)
    {
        GL.Uniform1(ShaderManager.gm_FogStart, start);

        var range = end - start;
        var rcpRange = range == 0 ? 0 : 1 / range;
        GL.Uniform1(ShaderManager.gm_RcpFogRange, rcpRange);

        GL.Uniform1(ShaderManager.gm_PS_FogEnabled, enable ? 1 : 0);
        GL.Uniform4(ShaderManager.gm_FogColour, color);
        GL.Uniform1(ShaderManager.gm_VS_FogEnabled, enable ? 1 : 0);
        CheckError();
    }
}
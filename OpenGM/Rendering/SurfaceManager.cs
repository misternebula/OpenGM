using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.IO;
using OpenTK.Graphics.OpenGL;

namespace OpenGM.Rendering;
public static class SurfaceManager
{
    public static int application_surface = -1;

    private static int _nextId = 1;

    private static Dictionary<int, int> _framebuffers = new();

    public static int CreateSurface(int width, int height, int format)
    {
        DebugLog.LogInfo($"CreateSurface width:{width} height:{height} format:{format}");

        // Generate framebuffer
        var buffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);

        // Generate texture to attach to framebuffer
        var newId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, newId);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (nint)null);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);

        // Attach texture to framebuffer
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, newId, 0);

        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
        {
            DebugLog.LogError($"ERROR: Framebuffer is not complete!");
        }

        // Unbind framebuffer
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        _framebuffers.Add(_nextId, buffer);

        return _nextId++;
    }

    public static void ResizeSurface(int id, int w, int h)
    {
        var bufferId = _framebuffers[id];
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, bufferId);

        // Generate texture to attach to framebuffer
        var newId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, newId);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, w, h, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (nint)null);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);

        // Attach texture to framebuffer
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, newId, 0);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public static int GetSurfaceWidth(int id)
    {
        var bufferId = _framebuffers[id];
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, bufferId);
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferParameterName.FramebufferAttachmentObjectName, out int textureId);
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out int width);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        return width;
    }

    public static int GetSurfaceHeight(int id)
    {
        var bufferId = _framebuffers[id];
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, bufferId);
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferParameterName.FramebufferAttachmentObjectName, out int textureId);
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out int height);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        return height;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.IO;
using OpenGM.VirtualMachine;
using OpenTK.Graphics.OpenGL;

// reference: https://learnopengl.com/Advanced-OpenGL/Framebuffers

namespace OpenGM.Rendering;
public static class SurfaceManager
{
    public static int application_surface = -1;

    private static int _nextId = 1;

    private static Dictionary<int, int> _framebuffers = new();
    public static Stack<int> SurfaceStack = new();

    public static bool surface_exists(int surface) => _framebuffers.ContainsKey(surface);

    public static bool surface_set_target(int surface)
    {
        if (!_framebuffers.ContainsKey(surface))
        {
            throw new NotImplementedException("Surface does not exist!");
        }

        SurfaceStack.Push(surface);
        var buffer = _framebuffers[surface];
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);
        // future draws will draw to this fbo
        return true;
    }

	public static bool surface_reset_target()
    {
        if (!SurfaceStack.TryPop(out var surface))
        {
            surface = application_surface;
        }
        var buffer = _framebuffers[surface]; // what happens if this buffer is deleted by the time we switch back to it?
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);
		return true;
	}

	public static int CreateSurface(int width, int height, int format)
    {
        // Generate framebuffer
        var buffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);

        // Generate texture to attach to framebuffer
        var newId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, newId);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (nint)null);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);

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

    public static void FreeSurface(int id)
    {
        var buffer = _framebuffers[id];
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferParameterName.FramebufferAttachmentObjectName, out int textureId);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.DeleteTexture(textureId);

        GL.DeleteFramebuffer(buffer);
        _framebuffers.Remove(id);
    }

    public static void ResizeSurface(int id, int w, int h)
    {
        var bufferId = _framebuffers[id];
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, bufferId);
        
        // delete existing texture if there is one
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferParameterName.FramebufferAttachmentObjectName, out int textureId);
        GL.DeleteTexture(textureId);

        // Generate texture to attach to framebuffer
        var newId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, newId);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, w, h, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (nint)null);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        // Attach texture to framebuffer
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, newId, 0);

        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
        {
            DebugLog.LogError($"ERROR: Framebuffer is not complete!");
        }
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public static int GetSurfaceWidth(int id)
    {
        var bufferId = _framebuffers[id];
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, bufferId);
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferParameterName.FramebufferAttachmentObjectName, out int textureId);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out int width);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        return width;
    }

    public static int GetSurfaceHeight(int id)
    {
        var bufferId = _framebuffers[id];
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, bufferId);
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferParameterName.FramebufferAttachmentObjectName, out int textureId);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out int height);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        return height;
    }

    // https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/yyWebGL.js#L3763
    public static void draw_surface(int id, double x, double y)
    {
        var buffer = _framebuffers[id];

        // we drew into this fbo earlier, get its texture data
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferParameterName.FramebufferAttachmentObjectName, out int textureId);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        var w = GetSurfaceWidth(id);
        var h = GetSurfaceHeight(id);

        // draw rectangle with that texture
        // TODO: this draws nothing for the tension bar. fuck
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GL.Begin(PrimitiveType.Quads);
        GL.TexCoord2(0, 0);
        GL.Vertex2(x, y);
        GL.TexCoord2(1, 0);
        GL.Vertex2(x + w, y);
        GL.TexCoord2(1, 1);
        GL.Vertex2(x + w, y + h);
        GL.TexCoord2(0, 1);
        GL.Vertex2(x, y + h);
        GL.End();
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.IO;
using OpenGM.VirtualMachine;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

// reference: https://learnopengl.com/Advanced-OpenGL/Framebuffers

namespace OpenGM.Rendering;
public static class SurfaceManager
{
    public static int application_surface = -1;

    private static int _nextId = 1;

    private static Dictionary<int, int> _framebuffers = new();
    public static Stack<int> SurfaceStack = new();

    public static bool surface_exists(int surface) => _framebuffers.ContainsKey(surface);

    // https://github.com/YoYoGames/GameMaker-HTML5/blob/96aa70d9ce66cdbf056747428a9902c2f57e9b25/scripts/functions/Function_Surface.js#L500
    public static bool surface_set_target(int surface)
    {
        if (!_framebuffers.ContainsKey(surface))
        {
            return false;
        }

        SurfaceStack.Push(surface);
        var buffer = _framebuffers[surface];
        // future draws will draw to this fbo
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);
        var width = GetSurfaceWidth(surface);
        var height = GetSurfaceHeight(surface);
        GL.Viewport(0, 0, width, height);
        // UpdateDefaultCamera in html5
        var matrix = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
        //GL.MatrixMode(MatrixMode.Projection);
        //GL.LoadMatrix(ref matrix);
        return true;
    }

	public static bool surface_reset_target()
    {
        SurfaceStack.Pop();
        if (SurfaceStack.TryPeek(out var surface))
        {
            var buffer = _framebuffers[surface]; // what happens if this buffer is deleted by the time we switch back to it?
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);
            // i am actually so confused on how any of this works, even looking at html5
            var x = (float)CustomWindow.Instance.X;
            var y = (float)CustomWindow.Instance.Y;
            var width = GetSurfaceWidth(surface);
            var height = GetSurfaceHeight(surface);
            GL.Viewport(0, 0, width, height);
            var matrix = Matrix4.CreateOrthographicOffCenter(x, x+width, y+height, y, 0, 1);
            // GL.MatrixMode(MatrixMode.Projection);
            // GL.LoadMatrix(ref matrix);
        }
        else
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            // ClientSize and FramebufferSize are the same
            var width = CustomWindow.Instance.ClientSize.X;
            var height = CustomWindow.Instance.ClientSize.Y;
            GL.Viewport(0, 0, width, height);
            var matrix = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
            // GL.MatrixMode(MatrixMode.Projection);
            // GL.LoadMatrix(ref matrix);
        }
        return true;
	}

	public static int CreateSurface(int width, int height, int format)
    {
        // Generate framebuffer
        var buffer = GL.GenFramebuffer();
        var prevBuffer = GL.GetInteger(GetPName.FramebufferBinding);
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
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevBuffer);

        _framebuffers.Add(_nextId, buffer);

        return _nextId++;
    }

    public static void FreeSurface(int id)
    {
        var buffer = _framebuffers[id];
        
        var prevBuffer = GL.GetInteger(GetPName.FramebufferBinding);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferParameterName.FramebufferAttachmentObjectName, out int textureId);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevBuffer);
        GL.DeleteTexture(textureId);

        GL.DeleteFramebuffer(buffer);
        _framebuffers.Remove(id);
    }

    public static void ResizeSurface(int id, int w, int h)
    {
        var bufferId = _framebuffers[id];
        var prevBuffer = GL.GetInteger(GetPName.FramebufferBinding);
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
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevBuffer);
    }

    public static int GetSurfaceWidth(int id)
    {
        var bufferId = _framebuffers[id];
        var prevBuffer = GL.GetInteger(GetPName.FramebufferBinding);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, bufferId);
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferParameterName.FramebufferAttachmentObjectName, out int textureId);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevBuffer);
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out int width);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        return width;
    }

    public static int GetSurfaceHeight(int id)
    {
        var bufferId = _framebuffers[id];
        var prevBuffer = GL.GetInteger(GetPName.FramebufferBinding);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, bufferId);
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferParameterName.FramebufferAttachmentObjectName, out int textureId);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevBuffer);
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out int height);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        return height;
    }

    // https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/functions/Function_Surface.js#L841
    // https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/yyWebGL.js#L3763
    public static void draw_surface(int id, double x, double y) => 
        draw_surface_stretched(id, x, y, GetSurfaceWidth(id), GetSurfaceHeight(id));

    public static void draw_surface_stretched(int id, double x, double y, double w, double h)
    {
        var buffer = _framebuffers[id];

        // we drew into this fbo earlier, get its texture data
        var prevBuffer = GL.GetInteger(GetPName.FramebufferBinding);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferParameterName.FramebufferAttachmentObjectName, out int textureId);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevBuffer);

        // draw rectangle with that texture
        // TODO: this draws nothing for the tension bar. fuck
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GL.Begin(PrimitiveType.Quads);
        // the order here is different from sprite drawing or else everything gets y flipped????
        GL.TexCoord2(0, 1);
        GL.Vertex2(x, y);
        GL.TexCoord2(1, 1);
        GL.Vertex2(x + w, y);
        GL.TexCoord2(1, 0);
        GL.Vertex2(x + w, y + h);
        GL.TexCoord2(0, 0);
        GL.Vertex2(x, y + h);
        GL.End();
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }
}

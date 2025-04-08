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

    // https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/functions/Function_Surface.js#L500
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
        GL.Viewport(0, 0, width, height); // draw to the entire framebuffer
        /*
        var matrix = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadMatrix(ref matrix); // map 1 unit to 1 surface pixel
        */
        // BUG: changing view causes uh problems with tp bar and attack ui
        // GL.Uniform4(VertexManager.u_view, new Vector4(0, 0, width, height));
        
        // application surface should do offsetting stuff i think. this corresponds to nothing in html5 so idk if this is right
        if (surface == application_surface)
        {
            // CustomWindow.Instance.UpdatePositionResolution();
        }

        return true;
    }

	public static bool surface_reset_target()
    {
        SurfaceStack.Pop();
        if (SurfaceStack.TryPeek(out var surface))
        {
            var buffer = _framebuffers[surface]; // what happens if this buffer is deleted by the time we switch back to it?
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);
            var width = GetSurfaceWidth(surface);
            var height = GetSurfaceHeight(surface);
            GL.Viewport(0, 0, width, height); // draw to the entire framebuffer
            /*
            var matrix = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref matrix); // map 1 unit to 1 surface pixel
            */
            // GL.Uniform4(VertexManager.u_view, new Vector4(0, 0, width, height));
            
            // application surface should do offsetting stuff i think. this corresponds to nothing in html5 so idk if this is right
            if (surface == application_surface)
            {
                // CustomWindow.Instance.UpdatePositionResolution();
            }
        }
        else
        {
            // draw to display
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            // just revert back to viewport and matrix set in CustomWindow
            // html5 just uses window size it seems so we do that here
            var width = CustomWindow.Instance.FramebufferSize.X;
            var height = CustomWindow.Instance.FramebufferSize.Y;
            GL.Viewport(0, 0, width, height);
            /*
            var matrix = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref matrix);
            */
            // GL.Uniform4(VertexManager.u_view, new Vector4(0, 0, width, height));
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

    public static void FreeSurface(int id, bool force)
    {
        if (force || application_surface != id)
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
    }

    public static bool NewApplicationSize;
    public static int NewApplicationWidth = -1;
    public static int NewApplicationHeight = -1;

    public static bool AppSurfaceEnabled;
    public static bool UsingAppSurface;

    public static int ApplicationWidth;
    public static int ApplicationHeight;

    // set in AppSurfaceEnable
    public static int OldApplicationWidth;
    public static int OldApplicationHeight;

    // TODO : get these
    public static int DeviceWidth;
    public static int DeviceHeight;

    public static void UpdateApplicationSurface()
    {
        if (!AppSurfaceEnabled)
        {
			ApplicationWidth = DeviceWidth;
			ApplicationHeight = DeviceHeight;

            if (surface_exists(application_surface))
            {
				FreeSurface(application_surface, true);
                application_surface = -17899859; // ??? why is this the magic number lol
			}
		}
        else
        {
			if (UsingAppSurface == false)
			{
			    ApplicationWidth = OldApplicationWidth;
				ApplicationHeight = OldApplicationHeight;
			}

            if (application_surface < 0 || !surface_exists(application_surface))
            {
                // creatingApplicationSurface = true
                application_surface = CreateSurface(ApplicationWidth, ApplicationHeight, -1);
				// wind_regionwidth = ApplicationWidth
				// creatingApplicationSurface = false
				// wind_regionheight = ApplicationHeight
			}

            if (NewApplicationSize)
            {
                NewApplicationSize = false;
                ResizeSurface(application_surface, NewApplicationWidth, NewApplicationHeight);
                ApplicationWidth = NewApplicationWidth;
                ApplicationHeight = NewApplicationHeight;
            }
		}

        UsingAppSurface = AppSurfaceEnabled;
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
        GL.Uniform1(VertexManager.u_doTex, 1);
        /*
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
        */
        VertexManager.Draw(PrimitiveType.TriangleFan, [
            new(new(x, y), Color4.White, new(0, 1)),
            new(new(x + w, y), Color4.White, new(1, 1)),
            new(new(x + w, y + h), Color4.White, new(1, 0)),
            new(new(x, y + h), Color4.White, new(0, 0)),
        ]);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        GL.Uniform1(VertexManager.u_doTex, 0);
    }
}

using OpenGM.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

// reference: https://learnopengl.com/Advanced-OpenGL/Framebuffers

namespace OpenGM.Rendering;
public static class SurfaceManager
{
    public static int application_surface = -1;

    private static int _nextId = 1;

    private static Dictionary<int, int> _framebuffers = new();
    private static int _currentSurfaceId = -1;
    public static Stack<(int PrevSurfaceId, Vector4i PrevViewPort, Vector4 PrevViewArea)> SurfaceStack = new();

    public static bool surface_exists(int surface) => _framebuffers.ContainsKey(surface);

    // https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/functions/Function_Surface.js#L500
    public static bool surface_set_target(int surface)
    {
        if (!_framebuffers.ContainsKey(surface))
        {
            return false;
        }

        GraphicsManager.PushMessage($"SurfaceSetTarget {surface}");

        SurfaceStack.Push((_currentSurfaceId, GraphicsManager.ViewPort,  GraphicsManager.ViewArea));
        _currentSurfaceId = surface;

        var buffer = _framebuffers[surface];
        // future draws will draw to this fbo
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);
        // draw to entire surface framebuffer
        var width = GetSurfaceWidth(surface);
        var height = GetSurfaceHeight(surface);
        GraphicsManager.SetViewPort(0, 0, width, height);
        GraphicsManager.SetViewArea(0, 0, width, height, 0);
        
        // even if drawing to a view surface or app surface, itll use the whole area

        GraphicsManager.PopMessage();

        return true;
    }

    public static int surface_get_target() => _currentSurfaceId;

    public static bool surface_reset_target()
    {
        GraphicsManager.PushMessage($"SurfaceResetTarget");

        var (prevSurfaceId, prevViewPort, prevViewArea) = SurfaceStack.Pop();
        _currentSurfaceId = prevSurfaceId;
        
        // -1 means draw to display. prev port/area will be framebuffer size set by OnFramebufferResize
        var buffer = prevSurfaceId == -1 ? 0 : _framebuffers[prevSurfaceId];
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);
        GraphicsManager.SetViewPort(prevViewPort);
        GraphicsManager.SetViewArea(prevViewArea);
        
        GraphicsManager.PopMessage(); // set target

        return true;
    }

    public static bool DrawingToBackbuffer() => GL.GetInteger(GetPName.FramebufferBinding) == 0;

    public static int CreateSurface(int width, int height, int format) // TODO: format
    {
        GraphicsManager.PushMessage($"CreateSurface width:{width}, height:{height} ({_nextId})");
        
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
        
        GraphicsManager.PopMessage();

        _framebuffers.Add(_nextId, buffer);

        return _nextId++;
    }

    public static void FreeSurface(int id, bool force) // force comes from cpp
    {
        if (id == -1)
        {
            return;
        }

        if (force || application_surface != id)
        {
            GraphicsManager.PushMessage($"FreeSurface {id}");
            var buffer = _framebuffers[id];
            var prevBuffer = GL.GetInteger(GetPName.FramebufferBinding);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);
            GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferParameterName.FramebufferAttachmentObjectName, out var textureId);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevBuffer);
            GL.DeleteTexture(textureId);
            GL.DeleteFramebuffer(buffer);
            _framebuffers.Remove(id);
            GraphicsManager.PopMessage();
        }
    }

    public static bool NewApplicationSize;
    public static int NewApplicationWidth = -1;
    public static int NewApplicationHeight = -1;

    /*
     * below is unused for now
     */
    public static bool AppSurfaceEnabled = true;
    public static bool UsingAppSurface = true;

    public static int ApplicationWidth;
    public static int ApplicationHeight;

    // set in AppSurfaceEnable/
    public static int OldApplicationWidth;
    public static int OldApplicationHeight;

    // TODO : is this right?
    public static int DeviceWidth => CustomWindow.Instance.FramebufferSize.X;
    public static int DeviceHeight => CustomWindow.Instance.FramebufferSize.Y;

    // https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/yyRoom.js#L3842
    // also copied from cpp
    public static void SetApplicationSurface()
    {
        GraphicsManager.PushMessage("SetApplicationSurface");
        // https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/_GameMaker.js#L1898
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

            //Create Application Surface?
            if (application_surface < 0 || !surface_exists(application_surface))
            {
                // creatingApplicationSurface = true
                application_surface = CreateSurface(ApplicationWidth, ApplicationHeight, -1);
                // wind_regionwidth = ApplicationWidth
                // creatingApplicationSurface = false
                // wind_regionheight = ApplicationHeight
            }

            //Resize the surface?
            if (NewApplicationSize)
            {
                NewApplicationSize = false;
                ResizeSurface(application_surface, NewApplicationWidth, NewApplicationHeight);
                ApplicationWidth = NewApplicationWidth;
                ApplicationHeight = NewApplicationHeight;
            }

            // Set to use the application surface        
            surface_set_target(application_surface);
        }

        UsingAppSurface = AppSurfaceEnabled;
        GraphicsManager.PopMessage();
    }

    public static void ResizeSurface(int id, int w, int h)
    {
        GraphicsManager.PushMessage($"ResizeSurface {id} {w}x{h}");
        
        var bufferId = _framebuffers[id];
        var prevBuffer = GL.GetInteger(GetPName.FramebufferBinding);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, bufferId);
        
        // delete existing texture if there is one
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferParameterName.FramebufferAttachmentObjectName, out var textureId);
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
            DebugLog.LogError($"ERROR: Framebuffer is not complete!\n{Environment.StackTrace}");
        }
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevBuffer);
        
        GraphicsManager.PopMessage();
    }

    public static int GetSurfaceWidth(int id)
    {
        if (!surface_exists(id))
        {
            return -1;
        }

        GraphicsManager.PushMessage($"GetSurfaceWidth {id}");
        BindSurfaceTexture(id);
        GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out int width);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        GraphicsManager.PopMessage();
        return width;
    }

    public static int GetSurfaceHeight(int id)
    {
        if (!surface_exists(id))
        {
            return -1;
        }

        GraphicsManager.PushMessage($"GetSurfaceHeight {id}");
        BindSurfaceTexture(id);
        GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out int height);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        GraphicsManager.PopMessage();
        return height;
    }

    // https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/functions/Function_Surface.js#L842
    public static void draw_surface(int id, double x, double y) => 
        draw_surface_stretched(id, x, y, GetSurfaceWidth(id), GetSurfaceHeight(id));

    public static void draw_surface_stretched(int id, double x, double y, double w, double h)
    {
        // draw rectangle with that texture
        BindSurfaceTexture(id);
        // we drew into this fbo earlier, get its texture data
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
        GraphicsManager.Draw(PrimitiveType.TriangleFan, [
            new(new(x, y, GraphicsManager.GR_Depth), Color4.White, new(0, 0)),
            new(new(x + w, y, GraphicsManager.GR_Depth), Color4.White, new(1, 0)),
            new(new(x + w, y + h, GraphicsManager.GR_Depth), Color4.White, new(1, 1)),
            new(new(x, y + h, GraphicsManager.GR_Depth), Color4.White, new(0, 1)),
        ]);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public static void draw_surface_ext(int id, double x, double y, double xscale, double yscale, double rot, int col, double alpha)
    {
        var w = GetSurfaceWidth(id);
        var h = GetSurfaceHeight(id);

        BindSurfaceTexture(id);

        var scaledWidth = w * xscale;
        var scaledHeight = h * yscale;
        var drawColor = col.ABGRToCol4(alpha);

        var pivot = new Vector2d(x, y);
        var vertexOne = new Vector3d(x, y, GraphicsManager.GR_Depth).RotateAroundPoint(pivot, rot);
        var vertexTwo = new Vector3d(x + scaledWidth, y, GraphicsManager.GR_Depth).RotateAroundPoint(pivot, rot);
        var vertexThree = new Vector3d(x + scaledWidth, y + scaledHeight, GraphicsManager.GR_Depth).RotateAroundPoint(pivot, rot);
        var vertexFour = new Vector3d(x, y + scaledHeight, GraphicsManager.GR_Depth).RotateAroundPoint(pivot, rot);

        GraphicsManager.Draw(PrimitiveType.TriangleFan, [
            new(vertexOne, drawColor, new(0, 0)),
            new(vertexTwo, drawColor, new(1, 0)),
            new(vertexThree, drawColor, new(1, 1)),
            new(vertexFour, drawColor, new(0, 1))
        ]);

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public static void draw_surface_part(int id, int left, int top, int w, int h, double x, double y)
    {
        var surfWidth = (double)GetSurfaceWidth(id);
        var surfHeight = (double)GetSurfaceHeight(id);

        BindSurfaceTexture(id);

        var vertexOne = new Vector3d(x, y, GraphicsManager.GR_Depth);
        var vertexTwo = new Vector3d(x + w, y, GraphicsManager.GR_Depth);
        var vertexThree = new Vector3d(x + w, y + h, GraphicsManager.GR_Depth);
        var vertexFour = new Vector3d(x, y + h, GraphicsManager.GR_Depth);

        var uvLeft = left / surfWidth;
        var uvRight = (left + w) / surfWidth;
        var uvTop = top / surfHeight;
        var uvBottom = (top + h) / surfHeight;

        var uvOne = new Vector2d(uvLeft, uvTop);
        var uvTwo = new Vector2d(uvRight, uvTop);
        var uvThree = new Vector2d(uvRight, uvBottom);
        var uvFour = new Vector2d(uvLeft, uvBottom);

        GraphicsManager.Draw(PrimitiveType.TriangleFan, [
            new(vertexOne, Color4.White, uvOne),
            new(vertexTwo, Color4.White, uvTwo),
            new(vertexThree, Color4.White, uvThree),
            new(vertexFour, Color4.White, uvFour)
        ]);

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public static void BindSurfaceTexture(int surfaceId)
    {
        GraphicsManager.PushMessage($"BindSurfaceTexture {surfaceId}");

        var buffer = _framebuffers[surfaceId];
       
        var prevBuffer = GL.GetInteger(GetPName.FramebufferBinding);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferParameterName.FramebufferAttachmentObjectName, out var textureId);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevBuffer);

        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GraphicsManager.PopMessage();
    }

    private static int _prevFrameBuffer;

    public static void BindSurfaceFramebuffer(int surfaceId)
    {
        GraphicsManager.PushMessage($"BindSurfaceFramebuffer {surfaceId}");
        var buffer = _framebuffers[surfaceId];
        _prevFrameBuffer = GL.GetInteger(GetPName.FramebufferBinding);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);

        GraphicsManager.PopMessage();
    }

    public static void BindPreviousFramebuffer()
    {
        GraphicsManager.PushMessage($"BindPreviousFramebuffer");
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _prevFrameBuffer);
        GraphicsManager.PopMessage();
    }

    public static int GetSurfaceTexture(int surfaceId)
    {
        GraphicsManager.PushMessage($"GetSurfaceTexture {surfaceId}");
        var buffer = _framebuffers[surfaceId];

        var prevBuffer = GL.GetInteger(GetPName.FramebufferBinding);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer);
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferParameterName.FramebufferAttachmentObjectName, out var textureId);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevBuffer);
        GraphicsManager.PopMessage();

        return textureId;
    }

    public static void Copy(int dest, int x, int y, int src, int xs, int ys, int ws, int hs)
    {
        if (!surface_exists(dest) || !surface_exists(src))
        {
            return;
        }

        var srcBuffer = _framebuffers[src];
        var dstBuffer = _framebuffers[dest];

        // https://ktstephano.github.io/rendering/opengl/dsa direct state access is cool
        GL.BlitNamedFramebuffer(
            srcBuffer, 
            dstBuffer,
            xs, ys, xs + ws, ys + hs,
            x, y, x + ws, y + hs,
            ClearBufferMask.ColorBufferBit,
            BlitFramebufferFilter.Nearest);
    }

    public static byte[] ReadPixels(int surfaceId, int x, int y, int w, int h)
    {
        BindSurfaceFramebuffer(surfaceId);

        var pixels = new byte[w * h * 4];
        GL.ReadPixels(x, y, w, h, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

        BindPreviousFramebuffer();
        return pixels;
    }

    // https://github.com/YoYoGames/GameMaker-HTML5/blob/7a976aad876481b2f7bbd4557ba4f5c35303b544/scripts/functions/Function_YoYo.js#L1207
    public static Vector4 FullScreenOffset()
    {
        var left = 0f;
        var top = 0f;
        float right;
        float bottom;

        var fbw = DeviceWidth;
        var fbh = DeviceHeight;

        if (UsingAppSurface) // TODO: only when "keep aspect ratio" is enabled
        {
            var w = (float)ApplicationWidth;
            var h = (float)ApplicationWidth;

            var aspect = w / h;
            var hh = fbw / aspect;
            float ww;

            if (hh < fbh)
            {
                aspect = h / w;
                hh = fbw * aspect;
                top = (fbh - hh) / 2;
                ww = fbw;
                hh += top;
            }
            else
            {
                aspect = w / h;
                ww = fbh * aspect;
                left = (fbw - ww) / 2;
                hh = fbh;
                ww += left;
            }

            right = ww;
            bottom = hh;
        }
        else
        {
            right = fbw;
            bottom = fbh;
        }

        return new(left, top, right - left, bottom - top);
    }
}

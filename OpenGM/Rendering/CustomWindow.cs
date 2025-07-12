using OpenGM.IO;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Text.RegularExpressions;

namespace OpenGM.Rendering;
public class CustomWindow : GameWindow
{
    public static CustomWindow Instance { get; private set; } = null!;

    public static List<GMBaseJob> DebugJobs = new();

    /*
     * below is for view, should be moved somewhere else
     */

    public uint Width;
    public uint Height;

    private double _x;
    public double X
    {
        get => _x;
        set
        {
            _x = value;
            // UpdatePositionResolution();
        }
    }

    private double _y;
    public double Y
    {
        get => _y;
        set
        {
            _y = value;
            // UpdatePositionResolution();
        }
    }

    public CustomWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, uint width, uint height)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        Instance = this;
        Width = width;
        Height = height;
        
        DebugLog.LogInfo($"-- CustomWindow .ctor --");
        DebugLog.LogInfo($"  Version: {nativeWindowSettings.API} {nativeWindowSettings.APIVersion}");
        DebugLog.LogInfo($"  Profile: {nativeWindowSettings.Profile}");
        DebugLog.LogInfo($"  Flags: {nativeWindowSettings.Flags}");
        DebugLog.LogInfo($"------------------------");

        GLFWProvider.SetErrorCallback((code, msg) => DebugLog.LogError($"GLFW error {code}: {msg}"));

        // https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/_GameMaker.js#L721
        SurfaceManager.ApplicationWidth = FramebufferSize.X;
        SurfaceManager.ApplicationHeight = FramebufferSize.Y;
        
        VertexManager.Init();
        
        /*
        GL.DebugMessageCallback((source, type, id, severity, length, messagePtr, param) =>
        {
            var message = MarshalTk.MarshalPtrToString(messagePtr);
            if (type == DebugType.DebugTypeError) throw new Exception($"GL error from {source}: {message}");
            DebugLog.LogInfo($"GL message from {source}: {message}");
        }, 0);
        GL.Enable(EnableCap.DebugOutput);

        GL.Enable(EnableCap.Texture2D); // always allow a texture to be drawn. does nothing if no texture is bound
        */
        GL.Enable(EnableCap.Blend); // always allow blending
        
        // bm_normal
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        DebugLog.LogInfo($"OnLoad()");
        // UpdatePositionResolution();
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);
        DebugLog.LogInfo($"OnFramebufferResize {e.Width}x{e.Height}");
        GL.Viewport(0, 0, e.Width, e.Height); // draw to entire framebuffer
    }

    /// <summary>
    /// set the view position
    /// </summary>
    public void SetPosition(double x, double y)
    {
        _x = x;
        _y = y;
        // UpdatePositionResolution();
    }

    /// <summary>
    /// set the view resolution
    /// </summary>
    public void SetResolution(int width, int height)
    {
        DebugLog.LogInfo($"SetResolution {Width}x{Height} -> {width}x{height}");
        Width = (uint)width;
        Height = (uint)height;
        // UpdatePositionResolution();
    }

    /// <summary>
    /// sets the view uniform
    /// </summary>
    public void UpdatePositionResolution()
    {
        /*
        var matrix = Matrix4.CreateOrthographicOffCenter((float)X, Width + (float)X, Height + (float)Y, (float)Y, 0, 1);
        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadMatrix(ref matrix);
        */
        GL.Uniform4(VertexManager.u_view, new Vector4((float)X, (float)Y, Width, Height));
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        CameraManager.UpdateViews();

        KeyboardHandler.UpdateMouseState(MouseState);
        KeyboardHandler.UpdateKeyboardState(KeyboardState);

        DrawManager.FixedUpdate();
        AudioManager.Update();

        foreach (var item in DebugJobs)
        {
            Draw(item);
        }

        DebugJobs.Clear();

        SwapBuffers();
    }

    public static void Draw(GMBaseJob baseJob)
    {
        switch (baseJob)
        {
            case GMTextJob textJob:
                Draw(textJob);
                return;
            case GMSpritePartJob spritePartJob:
                Draw(spritePartJob);
                return;
            case GMSpriteJob spriteJob:
                Draw(spriteJob);
                return;
            case GMLineJob lineJob:
                Draw(lineJob);
                return;
            case GMLinesJob linesJob:
                Draw(linesJob);
                return;
            case GMPolygonJob polygonJob:
                Draw(polygonJob);
                return;
            default:
                throw new NotImplementedException($"Don't know how to draw {baseJob}");
        }
    }

    public static void Draw(GMTextJob textJob)
    {
        if (string.IsNullOrEmpty(textJob.text))
        {
            return;
        }

        if (VersionManager.EngineVersion.Major == 1)
        {
            // Replace \# with #, replace # with newlines.
            textJob.text = Regex.Replace(textJob.text, @"(?<=[^\\]|^)#", Environment.NewLine);
            textJob.text = textJob.text.Replace(@"\#", "#");
        }

        var lines = textJob.text.SplitLines();
        var lineYOffset = 0;

        var textWidth = (int)(TextManager.StringWidth(textJob.text) * textJob.scale.X);
        var textHeight = (int)(TextManager.StringHeight(textJob.text) * textJob.scale.Y);

        var textLeft = textJob.halign switch
        {
            HAlign.fa_center => textJob.screenPos.X - (textWidth / 2),
            HAlign.fa_right => textJob.screenPos.X - textWidth,
            _ => textJob.screenPos.X
        };

        var textTop = textJob.valign switch
        {
            VAlign.fa_middle => textJob.screenPos.Y - (textHeight / 2),
            VAlign.fa_bottom => textJob.screenPos.Y - textHeight,
            _ => textJob.screenPos.Y
        };

        var textRight = textLeft + textWidth;
        var textBottom = textTop + textHeight;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var width = (int)(TextManager.StringWidth(line) * textJob.scale.X);

            var xOffset = 0d;
            if (textJob.halign == HAlign.fa_center)
            {
                xOffset = -(width / 2);
            }
            else if (textJob.halign == HAlign.fa_right)
            {
                xOffset = -width;
            }

            var yOffset = 0;
            if (textJob.valign == VAlign.fa_middle)
            {
                yOffset = -(textHeight / 2);
            }
            else if (textJob.valign == VAlign.fa_bottom)
            {
                yOffset = -textHeight;
            }

            yOffset += lineYOffset;
            lineYOffset += textJob.lineSep;

            var stringLeft = textJob.screenPos.X + xOffset;
            var stringRight = textJob.screenPos.X + xOffset + width;
            var stringTop = -textJob.screenPos.Y - yOffset;
            var stringBottom = -textJob.screenPos.Y - yOffset - (int)(TextManager.StringHeight(line) * textJob.scale.Y);

            var points = new Vector2d[]
            {
                (stringLeft, stringTop),
                (stringRight, stringTop),
                (stringRight, stringBottom),
                (stringLeft, stringBottom)
            };

            for (var j = 0; j < line.Length; j++)
            {
                var character = line[j];

                if (!textJob.asset.entriesDict.TryGetValue(character, out var glyph))
                {
                    // offset sprite text for spaces
                    if (textJob.asset.IsSpriteFont() && character == ' ')
                    {
                        xOffset += textJob.asset.Size;
                    }

                    continue;
                }

                var pageItem = textJob.asset.texture;
                if (textJob.asset.IsSpriteFont())
                {
                    pageItem = SpriteManager.GetSpritePageItem(textJob.asset.spriteIndex, glyph.frameIndex);
                }

                var (texturePage, pageId) = PageManager.TexturePages[pageItem!.Page];

                var pageX = 0;
                var pageY = 0;

                if (!glyph.IsSpriteBased())
                {
                    pageX = pageItem.SourceX;
                    pageY = pageItem.SourceY;
                }

                var topLeftX = textJob.screenPos.X + xOffset + glyph.xOffset;
                var topLeftY = textJob.screenPos.Y + yOffset + glyph.yOffset;

                var leftX = (pageX + glyph.x) / (float)texturePage.Width;
                var rightX = (pageX + glyph.x + glyph.w) / (float)texturePage.Width;
                var topY = (pageY + glyph.y) / (float)texturePage.Height;
                var bottomY = (pageY + glyph.y + glyph.h) / (float)texturePage.Height;

                GL.BindTexture(TextureTarget.Texture2D, pageId);

                var topLeftPos = new Vector2d(topLeftX, topLeftY);
                var topRightPos = new Vector2d(topLeftX + glyph.w * textJob.scale.X, topLeftY);
                var bottomRightPos = new Vector2d(topLeftX + glyph.w * textJob.scale.X, (topLeftY + glyph.h * textJob.scale.Y));
                var bottomLeftPos = new Vector2d(topLeftX, topLeftY + glyph.h * textJob.scale.Y);

                var topLeftCol = CustomMath.BlendBetweenPoints(topLeftPos, points, textJob.Colors);
                var topRightCol = CustomMath.BlendBetweenPoints(topRightPos, points, textJob.Colors);
                var bottomRightCol = CustomMath.BlendBetweenPoints(bottomRightPos, points, textJob.Colors);
                var bottomLeftCol = CustomMath.BlendBetweenPoints(bottomLeftPos, points, textJob.Colors);

                VertexManager.Draw(PrimitiveType.TriangleFan, [
                    new(topLeftPos, topLeftCol, new(leftX, topY)),
                    new(topRightPos, topRightCol, new(rightX, topY)),
                    new(bottomRightPos, bottomRightCol, new(rightX, bottomY)),
                    new(bottomLeftPos,bottomLeftCol, new(leftX, bottomY)),
                ]);

                xOffset += glyph.shift * textJob.scale.X;
                if (textJob.asset.IsSpriteFont())
                {
                    xOffset += textJob.asset.sep * textJob.scale.X;
                }
            }
        }
    }

    public static void Draw(GMSpriteJob spriteJob)
    {
        var (pageTexture, id) = PageManager.TexturePages[spriteJob.texture.Page];
        GL.BindTexture(TextureTarget.Texture2D, id);

        // Gonna define some terminology here to make this easer
        // "Full Sprite" is the sprite area with padding around the outside - the bounding box.
        // "Draw Area" is the part of the screen the actual data from the page is being drawn to - the target.

        var fullSpriteLeft = spriteJob.screenPos.X - (spriteJob.origin.X * spriteJob.scale.X);
        var fullSpriteTop = spriteJob.screenPos.Y - (spriteJob.origin.Y * spriteJob.scale.Y);

        var drawAreaLeft = fullSpriteLeft + (spriteJob.texture.TargetX * spriteJob.scale.X);
        var drawAreaTop = fullSpriteTop + (spriteJob.texture.TargetY * spriteJob.scale.Y);
        var drawAreaWidth = spriteJob.texture.TargetWidth * spriteJob.scale.X;
        var drawAreaHeight = spriteJob.texture.TargetHeight * spriteJob.scale.Y;

        var drawAreaTopLeft = new Vector2d(drawAreaLeft, drawAreaTop);
        var drawAreaTopRight = new Vector2d(drawAreaLeft + drawAreaWidth, drawAreaTop);
        var drawAreaBottomRight = new Vector2d(drawAreaLeft + drawAreaWidth, drawAreaTop + drawAreaHeight);
        var drawAreaBottomLeft = new Vector2d(drawAreaLeft, drawAreaTop + drawAreaHeight);

        var topLeftUV = new Vector2d(
            (double)spriteJob.texture.SourceX / pageTexture.Width,
            (double)spriteJob.texture.SourceY / pageTexture.Height);

        var UVWidth = (double)spriteJob.texture.SourceWidth / pageTexture.Width;
        var UVHeight = (double)spriteJob.texture.SourceHeight / pageTexture.Height;

        var topRightUV = new Vector2d(topLeftUV.X + UVWidth, topLeftUV.Y);
        var bottomRightUV = new Vector2d(topLeftUV.X + UVWidth, topLeftUV.Y + UVHeight);
        var bottomLeftUV = new Vector2d(topLeftUV.X, topLeftUV.Y + UVHeight);

        drawAreaTopLeft = drawAreaTopLeft.RotateAroundPoint(spriteJob.screenPos, spriteJob.angle);
        drawAreaTopRight = drawAreaTopRight.RotateAroundPoint(spriteJob.screenPos, spriteJob.angle);
        drawAreaBottomRight = drawAreaBottomRight.RotateAroundPoint(spriteJob.screenPos, spriteJob.angle);
        drawAreaBottomLeft = drawAreaBottomLeft.RotateAroundPoint(spriteJob.screenPos, spriteJob.angle);
        
        /*
        GL.TexCoord2(topLeftUV);
        GL.Vertex2(drawAreaTopLeft);
        GL.TexCoord2(topRightUV);
        GL.Vertex2(drawAreaTopRight);
        GL.TexCoord2(bottomRightUV);
        GL.Vertex2(drawAreaBottomRight);
        GL.TexCoord2(bottomLeftUV);
        GL.Vertex2(drawAreaBottomLeft);
        
        GL.End();
        */
        VertexManager.Draw(PrimitiveType.TriangleFan, [
            new(drawAreaTopLeft, spriteJob.Colors[0], topLeftUV),
            new(drawAreaTopRight, spriteJob.Colors[1], topRightUV),
            new(drawAreaBottomRight, spriteJob.Colors[2], bottomRightUV),
            new(drawAreaBottomLeft, spriteJob.Colors[3], bottomLeftUV),
        ]);
        
    }

    public static void Draw(GMSpritePartJob partJob)
    {
        var (pageTexture, id) = PageManager.TexturePages[partJob.texture.Page];
        GL.BindTexture(TextureTarget.Texture2D, id);

        var left = (double)partJob.left;
        var top = (double)partJob.top;
        var width = (double)partJob.width;
        var height = (double)partJob.height;
        var x = partJob.screenPos.X;
        var y = partJob.screenPos.Y;
        var xscale = partJob.scale.X;
        var yscale = partJob.scale.Y;

        var sinAngle = Math.Sin(partJob.angle);
        var cosAngle = Math.Cos(partJob.angle);

        double xUVOffset;
        var fVar7 = (double)partJob.texture.TargetX;
        if (fVar7 <= left)
        {
            xUVOffset = left - fVar7;
        }
        else
        {
            fVar7 -= left;
            xUVOffset = 0.0f;
            width -= fVar7;
            x += fVar7 * cosAngle * xscale;
            y -= fVar7 * sinAngle * yscale;
        }

        double yUVOffset;
        fVar7 = partJob.texture.TargetY;
        if (fVar7 <= top)
        {
            yUVOffset = top - fVar7;
        }
        else
        {
            fVar7 -= top;
            yUVOffset = 0.0f;
            height -= fVar7;
            x += fVar7 * sinAngle * xscale;
            y += fVar7 * cosAngle * yscale;
        }

        if (partJob.texture.TargetWidth < xUVOffset + width)
        {
            width = partJob.texture.TargetWidth - xUVOffset;
        }

        if (partJob.texture.TargetHeight < yUVOffset + height)
        {
            height = partJob.texture.TargetHeight - yUVOffset;
        }

        if ((0.0 < width) && (0.0 < height))
        {
            var widthScale = partJob.texture.SourceWidth / partJob.texture.TargetWidth;
            var heightScale = partJob.texture.SourceHeight / partJob.texture.TargetHeight;

            var uvLeft = (partJob.texture.SourceX + xUVOffset) / pageTexture.Width;
            var uvTop = (partJob.texture.SourceY + yUVOffset) / pageTexture.Height;
            var uvRight = (partJob.texture.SourceX + xUVOffset + widthScale * width) / pageTexture.Width;
            var uvBottom = (partJob.texture.SourceY + yUVOffset + heightScale * height) / pageTexture.Height;
            var uv0 = new Vector2d(uvLeft, uvTop);
            var uv1 = new Vector2d(uvRight, uvTop);
            var uv2 = new Vector2d(uvRight, uvBottom);
            var uv3 = new Vector2d(uvLeft, uvBottom);

            var widthCos = width * xscale * cosAngle;
            var widthSin = -width * xscale * sinAngle;
            var heightCos = height * yscale * cosAngle;
            var heightSin = height * yscale * sinAngle;

            var bottomVector = new Vector2d(heightSin, heightCos);

            var topLeft = new Vector2d(x, y);
            var bottomLeft = topLeft + bottomVector;

            var topRight = topLeft + new Vector2d(widthCos, widthSin);
            var bottomRight = topRight + bottomVector;

            VertexManager.Draw(PrimitiveType.TriangleFan, [
                new(topLeft, partJob.Colors[0], uv0),
                new(topRight, partJob.Colors[1], uv1),
                new(bottomRight, partJob.Colors[2], uv2),
                new(bottomLeft, partJob.Colors[3], uv3),
            ]);
        }

        // GL.End();
    }

    public static void Draw(GMLineJob lineJob)
    {
        // platform pixel adjustment
        lineJob.x1 += 1;
        lineJob.y1 += 1;
        lineJob.x2 += 1;
        lineJob.y2 += 1;

        var width = lineJob.x2 - lineJob.x1;
        var height = lineJob.y2 - lineJob.y1;
        var length = MathF.Sqrt((width * width) + (height * height));

        width = lineJob.width * 0.5f * width / length;
        height = lineJob.width * 0.5f * height / length;

        /*
        GL.Begin(PrimitiveType.Polygon);

        GL.Color4(lineJob.col1);
        GL.Vertex2(lineJob.x1 - height, lineJob.y1 + width);
        GL.Color4(lineJob.col2);
        GL.Vertex2(lineJob.x2 - height, lineJob.y2 + width);
        GL.Vertex2(lineJob.x2 + height, lineJob.y2 - width);
        GL.Color4(lineJob.col1);
        GL.Vertex2(lineJob.x1 + height, lineJob.y1 - width);

        GL.End();
        */
        GL.BindTexture(TextureTarget.Texture2D, VertexManager.DefaultTexture);
        VertexManager.Draw(PrimitiveType.TriangleFan, [
            new(new(lineJob.x1 - height, lineJob.y1 + width), lineJob.col1, Vector2.Zero),
            new(new(lineJob.x2 - height, lineJob.y2 + width), lineJob.col2, Vector2.Zero),
            new(new(lineJob.x2 + height, lineJob.y2 - width), lineJob.col2, Vector2.Zero),
            new(new(lineJob.x1 + height, lineJob.y1 - width), lineJob.col1, Vector2.Zero),
        ]);
    }

    public static void Draw(GMLinesJob linesJob)
    {
        GL.BindTexture(TextureTarget.Texture2D, VertexManager.DefaultTexture);
        var v = new VertexManager.Vertex[linesJob.Vertices.Length];
        for (var i = 0; i < linesJob.Vertices.Length; i++)
        {
            v[i] = new(linesJob.Vertices[i], linesJob.Colors[i], Vector2d.Zero);
        }

        VertexManager.Draw(PrimitiveType.LineStrip, v);
    }

    public static void Draw(GMPolygonJob polyJob)
    {
        GL.BindTexture(TextureTarget.Texture2D, VertexManager.DefaultTexture);
        var v = new VertexManager.Vertex[polyJob.Vertices.Length];
        for (var i = 0; i < polyJob.Vertices.Length; i++)
        {
            v[i] = new(polyJob.Vertices[i], polyJob.Colors[i], Vector2d.Zero);
        }

        // guessing polygon works with triangle fan since quad worked with that and polygons must be convex i think
        VertexManager.Draw(polyJob.Outline ? PrimitiveType.LineLoop : PrimitiveType.TriangleFan, v);
    }

    public static void Draw(GMTexturedPolygonJob texPolyJob)
    {
        var (pageTexture, id) = PageManager.TexturePages[texPolyJob.Texture.Page];
        GL.BindTexture(TextureTarget.Texture2D, id);

        var vArr = new VertexManager.Vertex[texPolyJob.Vertices.Length];
        for (var i = 0; i < texPolyJob.Vertices.Length; i++)
        {
            vArr[i] = new VertexManager.Vertex(texPolyJob.Vertices[i], texPolyJob.Colors[i], texPolyJob.UVs[i]);
        }
        VertexManager.Draw(PrimitiveType.TriangleFan, vArr);
    }
}

public class GMLineJob : GMBaseJob
{
    public required float x1;
    public required float y1;
    public required float x2;
    public required float y2;
    public required float width;
    public required Color4 col1;
    public required Color4 col2;
}

public class GMLinesJob : GMBaseJob
{
    public required Vector2d[] Vertices;
    public required Color4[] Colors;
}

public class GMSpriteJob : GMBaseJob
{
    public required Vector2d screenPos;
    public required SpritePageItem texture;
    public required Vector2d scale;
    public required double angle;
    public required Vector2 origin;
    public required Color4[] Colors;
}

public class GMSpritePartJob : GMSpriteJob
{
    public required float left;
    public required float top;
    public required float width;
    public required float height;
}

public class GMTextJob : GMBaseJob
{
    public required Vector2d screenPos;
    public required string text;
    public required Vector2d scale;
    public required HAlign halign;
    public required VAlign valign;
    public required double angle;
    public required FontAsset asset;
    public required int lineSep;
    public required Color4[] Colors;
}

public class GMPolygonJob : GMBaseJob
{
    public required Vector2d[] Vertices;
    public required Color4[] Colors;
    public required bool Outline;
}

public class GMTexturedPolygonJob : GMBaseJob
{
    public required Vector2d[] Vertices;
    public required Vector2d[] UVs;
    public required Color4[] Colors;
    public required SpritePageItem Texture;
}

public abstract class GMBaseJob;

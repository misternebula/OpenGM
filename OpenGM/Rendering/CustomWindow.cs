using OpenGM.IO;
using OpenGM.SerializedFiles;
using OpenTK.Core.Native;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using UndertaleModLib.Decompiler;

namespace OpenGM.Rendering;
public class CustomWindow : GameWindow
{
    public static CustomWindow Instance { get; private set; } = null!;

    public uint Width;
    public uint Height;

    private double _x;
    public double X
    {
        get => _x;
        set
        {
            _x = value;
            UpdatePositionResolution();
        }
    }

    private double _y;
    public double Y
    {
        get => _y;
        set
        {
            _y = value;
            UpdatePositionResolution();
        }
    }

    public CustomWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, uint width, uint height)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        Instance = this;
        Width = width;
        Height = height;
        
        GL.DebugMessageCallback((source, type, id, severity, length, messagePtr, param) =>
        {
            var message = MarshalTk.MarshalPtrToString(messagePtr);
            if (type == DebugType.DebugTypeError) throw new Exception($"GL error from {source}: {message}");
            DebugLog.LogInfo($"GL message from {source}: {message}");
        }, 0);
        GL.Enable(EnableCap.DebugOutput);

        GL.Enable(EnableCap.Texture2D); // always allow a texture to be drawn. does nothing if no texture is bound
        GL.Enable(EnableCap.Blend); // always allow blending
        
        // bm_normal
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        UpdatePositionResolution();
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        // "Resolution" below isnt really resolution, but just number of "pixels" (units) you can see
        // BUG: frame buffer should be fixed resolution thats just upscaled (for pixel perfect)
    }

    public void SetPosition(double x, double y)
    {
        _x = x;
        _y = y;
        UpdatePositionResolution();
    }

    public void SetResolution(int width, int height)
    {
        Width = (uint)width;
        Height = (uint)height;
        UpdatePositionResolution();
    }

    private void UpdatePositionResolution()
    {
        var matrix = Matrix4.CreateOrthographicOffCenter((float)X, Width + (float)X, Height + (float)Y, (float)Y, 0, 1);
        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadMatrix(ref matrix);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        KeyboardHandler.UpdateMouseState(MouseState);
        KeyboardHandler.UpdateKeyboardState(KeyboardState);

        DrawManager.FixedUpdate();
        AudioManager.Update();
        
        SwapBuffers();
    }

    public static void Draw(GMTextJob textJob)
    {
        if (string.IsNullOrEmpty(textJob.text))
        {
            return;
        }

        var lines = textJob.text.SplitLines();
        var textHeight = TextManager.StringHeight(textJob.text);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var width = TextManager.StringWidth(line);

            var xOffset = 0d;
            if (textJob.halign == HAlign.fa_center)
            {
                xOffset = -(width / 2f);
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

            var stringLeft = textJob.screenPos.X + xOffset;
            var stringRight = textJob.screenPos.X + xOffset + width;
            var stringTop = -textJob.screenPos.Y - yOffset;
            var stringBottom = -textJob.screenPos.Y - yOffset - TextManager.StringHeight(line);

            double map(double s, double a1, double a2, double b1, double b2)
            {
                return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
            }

            Color4 LerpBetweenColors(Color4 leftColor, Color4 rightColor, double left, double right, double value)
            {
                var distance = map(value, left, right, 0, 1);
                return Lerp(leftColor, rightColor, (float)distance);
            }

            Color4 Lerp(Color4 a, Color4 b, float t)
            {
                t = Math.Clamp(t, 0, 1);
                return new Color4(
                    a.R + (b.R - a.R) * t,
                    a.G + (b.G - a.G) * t,
                    a.B + (b.B - a.B) * t,
                    a.A + (b.A - a.A) * t);
            }

            for (var j = 0; j < line.Length; j++)
            {
                var character = line[j];

                if (textJob.asset.texture == null && textJob.asset.spriteIndex != -1)
                {
                    // sprite font

                    // TODO: Implement
                }
                else
                {
                    // normal font

                    if (!textJob.asset.entriesDict.TryGetValue(character, out var glyph))
                    {
                        continue;
                    }

                    var (texturePage, pageId) = PageManager.TexturePages[textJob.asset.texture!.Page];

                    var pageItem = textJob.asset.texture;
                    var pageX = pageItem.SourcePosX;
                    var pageY = pageItem.SourcePosY;

                    var topLeftX = textJob.screenPos.X + xOffset + glyph.offset;
                    var topLeftY = textJob.screenPos.Y + yOffset;

                    var leftX = (pageX + glyph.x) / (float)texturePage.Width;
                    var rightX = (pageX + glyph.x + glyph.w) / (float)texturePage.Width;
                    var topY = (pageY + glyph.y) / (float)texturePage.Height;
                    var bottomY = (pageY + glyph.y + glyph.h) / (float)texturePage.Height;

                    var c1 = textJob.c1;
                    var c2 = textJob.c2;
                    var c3 = textJob.c3;
                    var c4 = textJob.c4;
                    if (!textJob.isColor)
                    {
                        c1 = c2 = c3 = c4 = new Color4(textJob.blend.R, textJob.blend.G, textJob.blend.B, (float)textJob.alpha);
                    }


                    GL.BindTexture(TextureTarget.Texture2D, pageId);

                    GL.Begin(PrimitiveType.Quads);

                    // TODO : this will make the different lines of a string have the gradient applied seperately.

                    // top left of letter
                    GL.TexCoord2(leftX, topY);
                    GL.Color4(LerpBetweenColors(c1, c2, stringLeft, stringRight, topLeftX));
                    GL.Vertex2(topLeftX, topLeftY);

                    // top right of letter
                    GL.TexCoord2(rightX, topY);
                    GL.Color4(LerpBetweenColors(c1, c2, stringLeft, stringRight, topLeftX + glyph.w));
                    GL.Vertex2(topLeftX + glyph.w * textJob.scale.X, topLeftY);

                    // bottom right of letter
                    GL.TexCoord2(rightX, bottomY);
                    GL.Color4(LerpBetweenColors(c4, c3, stringLeft, stringRight, topLeftX + glyph.w));
                    GL.Vertex2(topLeftX + glyph.w * textJob.scale.X, topLeftY + glyph.h * textJob.scale.Y);

                    // bottom left of letter
                    GL.TexCoord2(leftX, bottomY);
                    GL.Color4(LerpBetweenColors(c4, c3, stringLeft, stringRight, topLeftX));
                    GL.Vertex2(topLeftX, topLeftY + glyph.h * textJob.scale.Y);

                    GL.End();

                    GL.BindTexture(TextureTarget.Texture2D, 0);

                    xOffset += glyph.shift * textJob.scale.X;
                }
            }
        }
    }

    public static void Draw(GMSpriteJob spriteJob)
    {
        var (pageTexture, id) = PageManager.TexturePages[spriteJob.texture.Page];

        GL.BindTexture(TextureTarget.Texture2D, id);

        GL.Begin(PrimitiveType.Quads);

        GL.Color4(new Color4(spriteJob.blend.R, spriteJob.blend.G, spriteJob.blend.B, (float)spriteJob.alpha));

        var spriteWidth = (float)spriteJob.texture.TargetSizeX;
        var spriteHeight = (float)spriteJob.texture.TargetSizeY;
        var left = 0d;
        var top = 0d;

        if (spriteJob is GMSpritePartJob partJob)
        {
            spriteWidth = partJob.width;
            spriteHeight = partJob.height;
            left = partJob.left;
            top = partJob.top;
        }

        var topLeft = new Vector2d(
	        spriteJob.screenPos.X - (spriteJob.origin.X * spriteJob.scale.X) + (spriteJob.texture.TargetPosX * spriteJob.scale.X),
	        spriteJob.screenPos.Y - (spriteJob.origin.Y * spriteJob.scale.Y) + (spriteJob.texture.TargetPosY * spriteJob.scale.Y));
        var topRight = new Vector2d(topLeft.X + spriteWidth * spriteJob.scale.X, topLeft.Y);
        var bottomRight = new Vector2d(topRight.X, topRight.Y + spriteHeight * spriteJob.scale.Y);
        var bottomLeft = new Vector2d(topLeft.X, bottomRight.Y);

        // in this house we dont use matrices
        if (spriteJob.angle != 0)
        {
            topLeft = topLeft.RotateAroundPoint(spriteJob.screenPos, spriteJob.angle);
            topRight = topRight.RotateAroundPoint(spriteJob.screenPos, spriteJob.angle);
            bottomRight = bottomRight.RotateAroundPoint(spriteJob.screenPos, spriteJob.angle);
            bottomLeft = bottomLeft.RotateAroundPoint(spriteJob.screenPos, spriteJob.angle);
        }

        var uvTopLeftX = (spriteJob.texture.SourcePosX + left) / pageTexture.Width;
        var uvTopLeftY = (spriteJob.texture.SourcePosY + top) / pageTexture.Height;

        var uvWidth = (double)spriteWidth / pageTexture.Width;
        var uvHeight = (double)spriteHeight / pageTexture.Height;

        // Top left
        GL.TexCoord2(uvTopLeftX, uvTopLeftY);
        GL.Vertex2(topLeft);

        // Top right
        GL.TexCoord2(uvTopLeftX + uvWidth, uvTopLeftY);
        GL.Vertex2(topRight);

        // Bottom right
        GL.TexCoord2(uvTopLeftX + uvWidth, uvTopLeftY + uvHeight);
        GL.Vertex2(bottomRight);

        // Bottom left
        GL.TexCoord2(uvTopLeftX, uvTopLeftY + uvHeight);
        GL.Vertex2(bottomLeft);

        GL.End();

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public static void Draw(GMLineJob lineJob)
    {
        var startToEndGradient = new Vector2(lineJob.end.X - lineJob.start.X, lineJob.end.Y - lineJob.start.Y);
        var perpendicularGradient = new Vector2(startToEndGradient.Y, -startToEndGradient.X);
        perpendicularGradient = Vector2.Normalize(perpendicularGradient);

        var p1 = lineJob.start + perpendicularGradient * (lineJob.width / 2f);
        var p2 = lineJob.start - perpendicularGradient * (lineJob.width / 2f);
        var p3 = lineJob.end - perpendicularGradient * (lineJob.width / 2f);
        var p4 = lineJob.end + perpendicularGradient * (lineJob.width / 2f);

        GL.Begin(PrimitiveType.Polygon);

        GL.Color4(lineJob.col1);
        GL.Vertex2(p1);
        GL.Vertex2(p2);
        GL.Color4(lineJob.col2);
        GL.Vertex2(p3);
        GL.Vertex2(p4);
        GL.Color4(lineJob.col1);

        GL.End();
    }

    public static void Draw(GMLinesJob linesJob)
    {
        GL.Begin(PrimitiveType.LineStrip);
        GL.Color4(new Color4(linesJob.blend.R, linesJob.blend.G, linesJob.blend.B, (float)linesJob.alpha));

        foreach (var vert in linesJob.Vertices)
        {
            GL.Vertex2(vert);
        }

        GL.End();
	}

    public static void Draw(GMPolygonJob polyJob)
    {
        if (polyJob.Outline)
        {
            GL.Begin(PrimitiveType.LineLoop);
        }
        else
        {
            GL.Begin(PrimitiveType.Polygon);
        }

        GL.Color4(new Color4(polyJob.blend.R, polyJob.blend.G, polyJob.blend.B, (float)polyJob.alpha));

        for (var i = 0; i < polyJob.Vertices.Length; i++)
        {
	        if (polyJob.Colors != null)
	        {
                var col = polyJob.Colors[i];
		        GL.Color4(new Color4(col.R, col.G, col.B, (float)polyJob.alpha));
			}

            GL.Vertex2(polyJob.Vertices[i]);
		}

        GL.End();
    }
}

public class GMLineJob : GMBaseJob
{
    public Vector2 start;
    public Vector2 end;
    public int width = 1;
    public Color4 col1;
    public Color4 col2;
}

public class GMLinesJob : GMBaseJob
{
	public Vector2d[] Vertices = null!;
}

public class GMSpriteJob : GMBaseJob
{
    public Vector2d screenPos;
    public SpritePageItem texture = null!;
    public Vector2d scale;
    public double angle;
    public Vector2 origin;
}

public class GMSpritePartJob : GMSpriteJob
{
    public float left;
    public float top;
    public float width;
    public float height;
}

public class GMTextJob : GMBaseJob
{
    public Vector2d screenPos;
    public string text = null!;
    public Vector2d scale;
    public HAlign halign;
    public VAlign valign;
    public double angle;
    public bool isColor;
    public Color4 c1 = Color4.White;
    public Color4 c2 = Color4.White;
    public Color4 c3 = Color4.White;
    public Color4 c4 = Color4.White;
    public FontAsset asset = null!;
    public int sep;
}

public class GMPolygonJob : GMBaseJob
{
    public Vector2d[] Vertices = null!;
    public Color4[] Colors = null!;
    public bool Outline;
}

public abstract class GMBaseJob
{
    public Color4 blend;
    public double alpha;
    public Color4 fogColor;
    public bool fogEnabled;
}

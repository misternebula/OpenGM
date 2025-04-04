﻿using OpenGM.IO;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using OpenTK.Core.Native;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Drawing;
using UndertaleModLib.Decompiler;

namespace OpenGM.Rendering;
public class CustomWindow : GameWindow
{
    public static CustomWindow Instance { get; private set; } = null!;

    public static List<GMBaseJob> DebugJobs = new();

    // Size of the Window
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

    public GamemakerObject? FollowInstance = null!;

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
        DebugLog.LogInfo($"OnLoad()");
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

        ViewportManager.UpdateViews();

		KeyboardHandler.UpdateMouseState(MouseState);
        KeyboardHandler.UpdateKeyboardState(KeyboardState);

        DrawManager.FixedUpdate();
        AudioManager.Update();

		UpdateInstanceFollow();

		foreach (var item in DebugJobs)
		{
			Draw(item);
		}

        DebugJobs.Clear();

		SwapBuffers();
    }

    public void UpdateInstanceFollow()
    {
	    if (FollowInstance == null)
	    {
		    if (RoomManager.CurrentRoom.FollowObject == null)
		    {
			    return;
			}

		    FollowInstance = RoomManager.CurrentRoom.FollowObject;
	    }

	    var x = FollowInstance.x + (FollowInstance.sprite_width / 2);
	    var y = FollowInstance.y + (FollowInstance.sprite_height / 2);

	    var roomWidth = RoomManager.CurrentRoom.SizeX;
        var roomHeight = RoomManager.CurrentRoom.SizeY;
        var viewWidth = RoomManager.CurrentRoom.CameraWidth;
        var viewHeight = RoomManager.CurrentRoom.CameraHeight;

        x -= viewWidth / 2d;
        y -= viewHeight / 2d;

        if (y <= 0) // top of screen
        {
            y = 0;
        }
        else if (y >= roomHeight - viewHeight) // bottom of screen
        {
	        y = roomHeight - viewHeight;
        }

        if (x <= 0) // left of screen
        {
	        x = 0;
        }
        else if (x >= roomWidth - viewWidth) // right of screen
        {
	        x = roomWidth - viewWidth;
        }

        SetPosition(x, y);
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
                    else
                    {
	                    c1.A = (float)textJob.alpha;
	                    c2.A = (float)textJob.alpha;
	                    c3.A = (float)textJob.alpha;
	                    c4.A = (float)textJob.alpha;
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

        // Gonna define some terminology here to make this easer
        // "Full Sprite" is the sprite area with padding around the outside - the bounding box.
        // "Draw Area" is the part of the screen the actual data from the page is being drawn to - the target.

        var fullSpriteLeft = spriteJob.screenPos.X - (spriteJob.origin.X * spriteJob.scale.X);
        var fullSpriteTop = spriteJob.screenPos.Y - (spriteJob.origin.Y * spriteJob.scale.Y);

        var drawAreaLeft = fullSpriteLeft + (spriteJob.texture.TargetPosX * spriteJob.scale.X);
        var drawAreaTop = fullSpriteTop + (spriteJob.texture.TargetPosY * spriteJob.scale.Y);
        var drawAreaWidth = spriteJob.texture.TargetSizeX * spriteJob.scale.X;
        var drawAreaHeight = spriteJob.texture.TargetSizeY * spriteJob.scale.Y;

        var drawAreaTopLeft = new Vector2d(drawAreaLeft, drawAreaTop);
        var drawAreaTopRight = new Vector2d(drawAreaLeft + drawAreaWidth, drawAreaTop);
        var drawAreaBottomRight = new Vector2d(drawAreaLeft + drawAreaWidth, drawAreaTop + drawAreaHeight);
        var drawAreaBottomLeft = new Vector2d(drawAreaLeft, drawAreaTop + drawAreaHeight);

        var topLeftUV = new Vector2d(
            (double)spriteJob.texture.SourcePosX / pageTexture.Width,
            (double)spriteJob.texture.SourcePosY / pageTexture.Height);

        var UVWidth = (double)spriteJob.texture.SourceSizeX / pageTexture.Width;
        var UVHeight = (double)spriteJob.texture.SourceSizeY / pageTexture.Height;

        var topRightUV = new Vector2d(topLeftUV.X + UVWidth, topLeftUV.Y);
        var bottomRightUV = new Vector2d(topLeftUV.X + UVWidth, topLeftUV.Y + UVHeight);
        var bottomLeftUV = new Vector2d(topLeftUV.X, topLeftUV.Y + UVHeight);

        drawAreaTopLeft = drawAreaTopLeft.RotateAroundPoint(spriteJob.screenPos, spriteJob.angle);
        drawAreaTopRight = drawAreaTopRight.RotateAroundPoint(spriteJob.screenPos, spriteJob.angle);
        drawAreaBottomRight = drawAreaBottomRight.RotateAroundPoint(spriteJob.screenPos, spriteJob.angle);
        drawAreaBottomLeft = drawAreaBottomLeft.RotateAroundPoint(spriteJob.screenPos, spriteJob.angle);

		GL.TexCoord2(topLeftUV);
        GL.Vertex2(drawAreaTopLeft);
        GL.TexCoord2(topRightUV);
        GL.Vertex2(drawAreaTopRight);
        GL.TexCoord2(bottomRightUV);
        GL.Vertex2(drawAreaBottomRight);
        GL.TexCoord2(bottomLeftUV);
        GL.Vertex2(drawAreaBottomLeft);
        
        GL.End();
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public static void Draw(GMSpritePartJob partJob)
    {
        var (pageTexture, id) = PageManager.TexturePages[partJob.texture.Page];
        GL.BindTexture(TextureTarget.Texture2D, id);
        GL.Begin(PrimitiveType.Quads);
        GL.Color4(new Color4(partJob.blend.R, partJob.blend.G, partJob.blend.B, (float)partJob.alpha));

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
		var fVar7 = (double)partJob.texture.TargetPosX;
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
        fVar7 = partJob.texture.TargetPosY;
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

        if (partJob.texture.TargetSizeX < xUVOffset + width)
        {
	        width = partJob.texture.TargetSizeX - xUVOffset;
        }

        if (partJob.texture.TargetSizeY < yUVOffset + height)
        {
	        height = partJob.texture.TargetSizeY - yUVOffset;
        }

        if ((0.0 < width) && (0.0 < height))
        {
	        var widthScale = partJob.texture.SourceSizeX / partJob.texture.TargetSizeX;
	        var heightScale = partJob.texture.SourceSizeY / partJob.texture.TargetSizeY;

			var uvLeft = (partJob.texture.SourcePosX + xUVOffset) / pageTexture.Width;
	        var uvTop = (partJob.texture.SourcePosY + yUVOffset) / pageTexture.Height;
	        var uvRight = (partJob.texture.SourcePosX + xUVOffset + widthScale * width) / pageTexture.Width;
            var uvBottom = (partJob.texture.SourcePosY + yUVOffset + heightScale * height) / pageTexture.Height;
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

	        GL.TexCoord2(uv0);
	        GL.Vertex2(topLeft);
	        GL.TexCoord2(uv1);
	        GL.Vertex2(topRight);
	        GL.TexCoord2(uv2);
	        GL.Vertex2(bottomRight);
	        GL.TexCoord2(uv3);
	        GL.Vertex2(bottomLeft);
		}

        GL.End();
        GL.BindTexture(TextureTarget.Texture2D, 0);
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

        GL.Begin(PrimitiveType.Polygon);

        GL.Color4(lineJob.col1);
        GL.Vertex2(lineJob.x1 - height, lineJob.y1 + width);
        GL.Color4(lineJob.col2);
		GL.Vertex2(lineJob.x2 - height, lineJob.y2 + width);
		GL.Vertex2(lineJob.x2 + height, lineJob.y2 - width);
		GL.Color4(lineJob.col1);
		GL.Vertex2(lineJob.x1 + height, lineJob.y1 - width);

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
	public float x1;
	public float y1;
	public float x2;
	public float y2;
    public float width = 1;
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
    public Vector2d scale = Vector2d.One;
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

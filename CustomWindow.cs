using DELTARUNITYStandalone.SerializedFiles;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using UndertaleModLib.Decompiler;
using Vector2 = OpenTK.Mathematics.Vector2;

namespace DELTARUNITYStandalone;
public class CustomWindow : GameWindow
{
	public static CustomWindow Instance { get; private set; }

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
	}

	protected override void OnLoad()
	{
		base.OnLoad();
		UpdatePositionResolution();
	}

	public void SetPosition(double x, double y)
	{
		_x = x;
		_y = y;
		UpdatePositionResolution();
	}

	public void SetResolution(uint width, uint height)
	{
		Width = width;
		Height = height;
		UpdatePositionResolution();
	}

	private void UpdatePositionResolution()
	{
		var matrix = Matrix4.CreateOrthographicOffCenter((float)X, Width + (float)X, Height + (float)Y, (float)Y, 0, 1);
		GL.MatrixMode(MatrixMode.Projection);
		GL.LoadMatrix(ref matrix);
	}

	public static List<GMBaseJob> RenderJobs = new();

	protected override void OnUpdateFrame(FrameEventArgs args)
	{
		base.OnUpdateFrame(args);

		GL.Enable(EnableCap.Blend);
		GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha); // bm_normal

		KeyboardHandler.UpdateKeyboardState(KeyboardState);

		RenderJobs.Clear();
		//RenderJobs.Add(new GMTriangleJob() { alpha = 0.5, blend = new Color4(255, 0, 0, 0), firstCorner = new Vector2(0, 0), secondCorner = new Vector2(100, 0), thirdCorner = new Vector2(0, 100) });
		//RenderJobs.Add(new GMRectangleJob() { alpha = 0.5, blend = new Color4(0, 255, 0, 0), height = 100, width = 200, screenPos = new Vector2(10, 10)});

		DrawManager.FixedUpdate();
		AudioManager.Update();
	}

	protected override void OnRenderFrame(FrameEventArgs e)
	{
		base.OnRenderFrame(e);

		GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


		foreach (var item in RenderJobs)
		{
			if (item is GMRectangleJob rectJob)
			{
				RenderRectangle(rectJob);
			}
			else if (item is GMTriangleJob triangleJob)
			{
				RenderTriangle(triangleJob);
			}
			else if (item is GMTextJob textJob)
			{
				RenderText(textJob);
			}
			else if (item is GMSpriteJob spriteJob)
			{
				RenderSprite(spriteJob);
			}
		}

		SwapBuffers();
	}

	private static void RenderRectangle(GMRectangleJob rectJob)
	{
		GL.Begin(BeginMode.Quads);
		GL.Color4(new Color4(rectJob.blend.R, rectJob.blend.G, rectJob.blend.B, (float)rectJob.alpha));
		GL.Vertex2(rectJob.screenPos.X, rectJob.screenPos.Y);
		GL.Vertex2(rectJob.screenPos.X + rectJob.width, rectJob.screenPos.Y);
		GL.Vertex2(rectJob.screenPos.X + rectJob.width, rectJob.screenPos.Y + rectJob.height);
		GL.Vertex2(rectJob.screenPos.X, rectJob.screenPos.Y + rectJob.height);
		GL.End();
	}

	private static void RenderTriangle(GMTriangleJob triangleJob)
	{
		GL.Begin(BeginMode.Triangles);
		GL.Color4(new Color4(triangleJob.blend.R, triangleJob.blend.G, triangleJob.blend.B, (float)triangleJob.alpha));
		GL.Vertex2(triangleJob.firstCorner.X, triangleJob.firstCorner.Y);
		GL.Vertex2(triangleJob.secondCorner.X, triangleJob.secondCorner.Y);
		GL.Vertex2(triangleJob.thirdCorner.X, triangleJob.thirdCorner.Y);
		GL.End();
	}

	private static void RenderText(GMTextJob textJob)
	{
		if (string.IsNullOrEmpty(textJob.text))
		{
			return;
		}

		var lines = textJob.text.Split(Environment.NewLine);
		var textHeight = TextManager.StringHeight(textJob.text);

		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			var width = TextManager.StringWidth(line);

			var xOffset = 0;
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

			var stringLeft = textJob.screenPos.X + xOffset;
			var stringRight = textJob.screenPos.X + xOffset + width;
			var stringTop = -textJob.screenPos.Y - yOffset;
			var stringBottom = -textJob.screenPos.Y - yOffset - TextManager.StringHeight(line);

			float map(float s, float a1, float a2, float b1, float b2)
			{
				return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
			}

			Color4 LerpBetweenColors(Color4 leftColor, Color4 rightColor, float left, float right, float value)
			{
				var distance = map(value, left, right, 0, 1);
				return Lerp(leftColor, rightColor, distance);
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

					var (texturePage, pageId) = PageManager.TexturePages[textJob.asset.texture.Page];

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


					GL.Enable(EnableCap.Texture2D);
					GL.BindTexture(TextureTarget.Texture2D, pageId);

					GL.Begin(BeginMode.Quads);

					// TODO : this will make the different lines of a string have the gradient applied seperately.

					// top left of letter
					GL.TexCoord2(leftX, topY);
					GL.Color4(LerpBetweenColors(c1, c2, stringLeft, stringRight, topLeftX));
					GL.Vertex2(topLeftX, topLeftY);

					// top right of letter
					GL.TexCoord2(rightX, topY);
					GL.Color4(LerpBetweenColors(c1, c2, stringLeft, stringRight, topLeftX + glyph.w));
					GL.Vertex2(topLeftX + glyph.w, topLeftY);

					// bottom right of letter
					GL.TexCoord2(rightX, bottomY);
					GL.Color4(LerpBetweenColors(c4, c3, stringLeft, stringRight, topLeftX + glyph.w));
					GL.Vertex2(topLeftX + glyph.w, topLeftY + glyph.h);

					// bottom left of letter
					GL.TexCoord2(leftX, bottomY);
					GL.Color4(LerpBetweenColors(c4, c3, stringLeft, stringRight, topLeftX));
					GL.Vertex2(topLeftX, topLeftY + glyph.h);

					GL.End();

					xOffset += glyph.shift;

					GL.Disable(EnableCap.Texture2D);
				}
			}
		}
	}

	private static void RenderSprite(GMSpriteJob spriteJob)
	{
		// TODO : implement GMSpritePartJob

		var (pageTexture, id) = PageManager.TexturePages[spriteJob.texture.Page];

		GL.Enable(EnableCap.Texture2D);
		GL.BindTexture(TextureTarget.Texture2D, id);

		GL.Begin(BeginMode.Quads);

		GL.Color4(new Color4(spriteJob.blend.R, spriteJob.blend.G, spriteJob.blend.B, (float)spriteJob.alpha));

		var topLeft = new Vector2(spriteJob.screenPos.X + (spriteJob.origin.X * spriteJob.scale.X), spriteJob.screenPos.Y + (spriteJob.origin.Y * spriteJob.scale.Y));
		var topRight = new Vector2(topLeft.X + (spriteJob.texture.TargetSizeX * spriteJob.scale.X), topLeft.Y);
		var bottomRight = new Vector2(topRight.X, topRight.Y + (spriteJob.texture.TargetSizeY * spriteJob.scale.Y));
		var bottomLeft = new Vector2(topLeft.X, bottomRight.Y);

		var uvTopLeftX = (double)spriteJob.texture.SourcePosX / pageTexture.Width;
		var uvTopLeftY = (double)spriteJob.texture.SourcePosY / pageTexture.Height;

		var uvWidth = (double)spriteJob.texture.SourceSizeX / pageTexture.Width;
		var uvHeight = (double)spriteJob.texture.SourceSizeY / pageTexture.Height;

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

		GL.Disable(EnableCap.Texture2D);
	}
}

public class GMLineJob : GMBaseJob
{
	public Vector2 start;
	public Vector2 end;
	public int width;
}

public class GMTriangleJob : GMBaseJob
{
	public Vector2 firstCorner;
	public Vector2 secondCorner;
	public Vector2 thirdCorner;
}

public class GMSpriteJob : GMBaseJob
{
	public Vector2 screenPos;
	public SpritePageItem texture;
	public Vector2 scale;
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

public class GMRectangleJob : GMBaseJob
{
	public Vector2 screenPos;
	public float width;
	public float height;
}

public class GMTextJob : GMBaseJob
{
	public Vector2 screenPos;
	public string text;
	public Vector2 scale;
	public HAlign halign;
	public VAlign valign;
	public float angle;
	public bool isColor;
	public Color4 c1 = Color4.White;
	public Color4 c2 = Color4.White;
	public Color4 c3 = Color4.White;
	public Color4 c4 = Color4.White;
	public FontAsset asset;
	public int sep;
}

public class GMBaseJob
{
	public Color4 blend;
	public double alpha;
	public Color4 fogColor;
	public bool fogEnabled;
}

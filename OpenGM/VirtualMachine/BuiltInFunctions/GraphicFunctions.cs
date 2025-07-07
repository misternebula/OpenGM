using NAudio.Codecs;
using OpenGM.IO;
using OpenGM.Rendering;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Xml.Linq;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class GraphicFunctions
    {
		// not in 2022.500, no idea where in the list it goes
		[GMLFunction("window_enable_borderless_fullscreen", GMLFunctionFlags.Stub)]
		public static object? window_enable_borderless_fullscreen(object?[] args)
		{
			// todo : implement
			return null;
		}

		[GMLFunction("display_get_width")]
		public static object display_get_width(object?[] args)
		{
			return Monitors.GetPrimaryMonitor().HorizontalResolution;
		}

		[GMLFunction("display_get_height")]
		public static object display_get_height(object?[] args)
		{
			return Monitors.GetPrimaryMonitor().VerticalResolution;
		}

		// display_get_frequency
		// display_get_orientation
		// diplay_reset
		// display_mouse_get_x
		// display_mouse_get_y
		// display_mouse_set
		// draw_enable_drawevent
		// display_set_windows_alternate_sync
		// display_set_ui_visibility
		// display_set_timing_method
		// display_get_timing_method
		// display_set_sleep_margin
		// display_get_sleep_margin

		[GMLFunction("display_set_gui_size", GMLFunctionFlags.Stub)]
		public static object? display_set_gui_size(object?[] args)
		{
			return null;
		}

		// window_set_visible
		// window_get_visible

		[GMLFunction("window_set_fullscreen")]
		public static object? window_set_fullscreen(object?[] args)
		{
			var full = args[0].Conv<bool>();
			CustomWindow.Instance.WindowState = full ? WindowState.Fullscreen : WindowState.Normal;
			// BUG: this fucks resolution
			return null;
		}

		[GMLFunction("window_get_fullscreen")]
		public static object window_get_fullscreen(object?[] args)
		{
			return CustomWindow.Instance.IsFullscreen;
		}

		// window_set_showborder
		// window_get_showborder
		// window_set_showicons
		// window_get_showicons
		// window_set_stayontop
		// window_get_stayontop
		// window_set_sizeable
		// window_get_sizeable

		[GMLFunction("window_set_caption")]
		public static object? window_set_caption(object?[] args)
		{
			var caption = args[0].Conv<string>();
			DebugLog.LogInfo($"window_set_caption : {caption}");
			CustomWindow.Instance.Title = caption;
			return null;
		}

		[GMLFunction("window_get_caption")]
		public static object? window_get_caption(object?[] args)
		{
			return CustomWindow.Instance.Title;
		}

		// window_set_cursor
		// window_get_cursor

		[GMLFunction("window_set_color", GMLFunctionFlags.Stub)]
		[GMLFunction("window_set_colour", GMLFunctionFlags.Stub)]
		public static object? window_set_color(object?[] args)
		{
			return null;
		}

		[GMLFunction("window_get_color", GMLFunctionFlags.Stub)]
		[GMLFunction("window_get_colour", GMLFunctionFlags.Stub)]
		public static object? window_get_color(object?[] args)
		{
			return null;
		}

		// window_set_min_width
		// window_set_max_width
		// window_set_min_height
		// window_set_max_height
		// window_set_position

		[GMLFunction("window_set_size")]
		public static object? window_set_size(object?[] args)
		{
			var w = args[0].Conv<int>();
			var h = args[1].Conv<int>();

			DebugLog.Log($"window_set_size {w} {h}");

			CustomWindow.Instance.ClientSize = new Vector2i(w, h);

			return null;
		}

		// window_set_rectangle

		[GMLFunction("window_center")]
		public static object? window_center(object?[] args)
		{
			CustomWindow.Instance.CenterWindow();

			return null;
		}

		// window_default
		// window_get_x
		// window_get_y

		[GMLFunction("window_get_width")]
		public static object window_get_width(object?[] args)
		{
			return CustomWindow.Instance.ClientSize.X;
		}

		[GMLFunction("window_get_height")]
		public static object window_get_height(object?[] args)
		{
			return CustomWindow.Instance.ClientSize.Y;
		}

		// window_get_visible_rects

		// ...

		// draw_getpixel
		// draw_getpixel_ext

		[GMLFunction("draw_set_color")]
		[GMLFunction("draw_set_colour")]
		public static object? draw_set_colour(object?[] args)
		{
			var color = args[0].Conv<int>();
			SpriteManager.DrawColor = color;
			return null;
		}

		[GMLFunction("draw_set_alpha")]
		public static object? draw_set_alpha(object?[] args)
		{
			var alpha = args[0].Conv<double>();
			SpriteManager.DrawAlpha = alpha;
			return null;
		}

		[GMLFunction("draw_get_color")]
		[GMLFunction("draw_get_colour")]
		public static object draw_get_colour(object?[] args)
		{
			return SpriteManager.DrawColor;
		}

		[GMLFunction("draw_get_alpha")]
		public static object? draw_get_alpha(object?[] args)
		{
			return SpriteManager.DrawAlpha;
		}

		[GMLFunction("make_color_rgb")]
		[GMLFunction("make_colour_rgb")]
		public static object make_color_rgb(params object?[] args)
		{
			var r = args[0].Conv<int>();
			var g = args[1].Conv<int>();
			var b = args[2].Conv<int>();

			return r | g << 8 | b << 16;
		}

		[GMLFunction("make_color_hsv")]
		[GMLFunction("make_colour_hsv")]
		public static object make_color_hsv(params object?[] args)
		{
			var hue = args[0].Conv<double>();
			var sat = args[1].Conv<double>() / 255;
			var val = args[2].Conv<double>() / 255;

			var hueDegree = (hue / 255) * 360;

			if (hueDegree >= 360)
			{
				hueDegree -= 360;
			}

			var chroma = val * sat;

			var hPrime = hueDegree / 60;

			var x = chroma * (1 - Math.Abs((hPrime % 2) - 1));

			var r = 0.0;
			var g = 0.0;
			var b = 0.0;

			switch (hPrime)
			{
				case >= 0 and < 1:
					r = chroma;
					g = x;
					b = 0;
					break;
				case >= 1 and < 2:
					r = x;
					g = chroma;
					b = 0;
					break;
				case >= 2 and < 3:
					r = 0;
					g = chroma;
					b = x;
					break;
				case >= 3 and < 4:
					r = 0;
					g = x;
					b = chroma;
					break;
				case >= 4 and < 5:
					r = x;
					g = 0;
					b = chroma;
					break;
				case >= 5 and < 6:
					r = chroma;
					g = 0;
					b = x;
					break;
			}

			var m = val - chroma;
			r += m;
			g += m;
			b += m;

			var rByte = (byte)(r * 255);
			var gByte = (byte)(g * 255);
			var bByte = (byte)(b * 255);

			return (bByte << 16) + (gByte << 8) + rByte;
		}

		// color_get_red
		// colour_get_red
		// color_get_green
		// colour_get_green
		// color_get_blue
		// colour_get_blue
		// color_get_hue
		// colour_get_hue
		// color_get_saturation
		// colour_get_saturation
		// color_get_value
		// colour_get_value

		[GMLFunction("merge_color")]
		[GMLFunction("merge_colour")]
		public static object merge_colour(params object?[] args)
		{
			var col1 = args[0].Conv<int>();
			var col2 = args[1].Conv<int>();
			var amount = args[2].Conv<double>();

			/*
			 * GameMaker stores colors in 3 bytes - BGR
			 * RED		: 255		: 00 00 FF
			 * ORANGE	: 4235519	: 40 A0 FF
			 * Alpha (or "blend") is not stored in colors.
			 */

			var oneBytes = BitConverter.GetBytes(col1);
			var twoBytes = BitConverter.GetBytes(col2);
			amount = Math.Clamp(amount, 0, 1);
			var mr = oneBytes[0] + (twoBytes[0] - oneBytes[0]) * amount;
			var mg = oneBytes[1] + (twoBytes[1] - oneBytes[1]) * amount;
			var mb = oneBytes[2] + (twoBytes[2] - oneBytes[2]) * amount;

			return BitConverter.ToInt32(new[] { (byte)mr, (byte)mg, (byte)mb, (byte)0 }, 0);
		}

		[GMLFunction("draw_clear")]
		public static object? draw_clear(object?[] args)
		{
			var col = args[0].Conv<int>();
			var colour = col.ABGRToCol4(0);
			GL.ClearColor(colour);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			GL.ClearColor(0, 0, 0, 0);
			return null;
		}

		[GMLFunction("draw_clear_alpha")]
		public static object? draw_clear_alpha(object?[] args)
		{
			var col = args[0].Conv<int>();
			var alpha = args[1].Conv<double>();

			GL.ClearColor(col.ABGRToCol4(alpha));
			GL.Clear(ClearBufferMask.ColorBufferBit);
			GL.ClearColor(0, 0, 0, 0);
			return null;
		}

		// draw_point

		[GMLFunction("draw_line")]
		public static object? draw_line(object?[] args)
		{
			var x1 = args[0].Conv<double>();
			var y1 = args[1].Conv<double>();
			var x2 = args[2].Conv<double>();
			var y2 = args[3].Conv<double>();

			var col = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha);

			CustomWindow.Draw(new GMLineJob()
			{
				col1 = col,
				col2 = col,
				width = 1,
				x1 = (float)x1,
				y1 = (float)y1,
				x2 = (float)x2,
				y2 = (float)y2
			});

			return null;
		}

		[GMLFunction("draw_line_width")]
		public static object? draw_line_width(object?[] args)
		{
			var x1 = args[0].Conv<double>();
			var y1 = args[1].Conv<double>();
			var x2 = args[2].Conv<double>();
			var y2 = args[3].Conv<double>();
			var w = args[4].Conv<int>();

			CustomWindow.Draw(new GMLineJob()
			{
				col1 = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha),
				col2 = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha),
				x1 = (float)x1,
				y1 = (float)y1,
				x2 = (float)x2,
				y2 = (float)y2,
				width = w
			});

			return null;
		}

		[GMLFunction("draw_rectangle")]
		public static object? draw_rectangle(params object?[] args)
		{
			var x1 = args[0].Conv<double>();
			var y1 = args[1].Conv<double>();
			var x2 = args[2].Conv<double>();
			var y2 = args[3].Conv<double>();
			var outline = args[4].Conv<bool>();

			x2 += 1;
			y2 += 1;

			var c = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha);

			CustomWindow.Draw(new GMPolygonJob()
			{
				Colors = [c, c, c, c],
				Vertices =
				[
					new(x1, y1),
					new(x2, y1),
					new(x2, y2),
					new(x1, y2)
				],
				Outline = outline
			});
			return null;
		}

		// draw_roundrect
		// draw_roundrect_ext

		[GMLFunction("draw_triangle")]
		public static object? draw_triangle(object?[] args)
		{
			var x1 = args[0].Conv<double>();
			var y1 = args[1].Conv<double>();
			var x2 = args[2].Conv<double>();
			var y2 = args[3].Conv<double>();
			var x3 = args[4].Conv<double>();
			var y3 = args[5].Conv<double>();
			var outline = args[6].Conv<bool>();

			var c = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha);

			CustomWindow.Draw(new GMPolygonJob()
			{
				Colors = [c, c, c],
				Vertices =
				[
					new(x1, y1),
					new(x2, y2),
					new(x3, y3)
				],
				Outline = outline
			});

			return null;
		}

		[GMLFunction("draw_circle")]
		public static object? draw_circle(object?[] args)
		{
			var x = args[0].Conv<double>();
			var y = args[1].Conv<double>();
			var r = args[2].Conv<double>();
			var outline = args[3].Conv<bool>();

			var angle = 360 / DrawManager.CirclePrecision;
			var c = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha);

			var points = new Vector2d[DrawManager.CirclePrecision];
			var colors = new Color4[DrawManager.CirclePrecision];
			for (var i = 0; i < DrawManager.CirclePrecision; i++)
			{
				points[i] = new Vector2d(x + (r * Math.Sin(angle * i * CustomMath.Deg2Rad)), y + (r * Math.Cos(angle * i * CustomMath.Deg2Rad)));
				colors[i] = c;
			}

			CustomWindow.Draw(new GMPolygonJob()
			{
				Colors = colors,
				Vertices = points,
				Outline = outline
			});

			return null;
		}

		[GMLFunction("draw_ellipse")]
		public static object? draw_ellipse(object?[] args)
		{
			var x1 = args[0].Conv<double>();
			var y1 = args[1].Conv<double>();
			var x2 = args[2].Conv<double>();
			var y2 = args[3].Conv<double>();
			var outline = args[4].Conv<bool>();

			var midpointX = (x1 + x2) / 2;
			var midpointY = (y1 + y2) / 2;

			var xRadius = Math.Abs((x1 - x2) / 2);
			var yRadius = Math.Abs((y1 - y2) / 2);

			var angle = 360 / DrawManager.CirclePrecision;
			var c = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha);

			var points = new Vector2d[DrawManager.CirclePrecision];
			var colors = new Color4[DrawManager.CirclePrecision];
			for (var i = 0; i < DrawManager.CirclePrecision; i++)
			{
				points[i] = new Vector2d(
					midpointX + (xRadius * Math.Sin(angle * i * CustomMath.Deg2Rad)),
					midpointY + (yRadius * Math.Cos(angle * i * CustomMath.Deg2Rad)));
				colors[i] = c;
			}

			CustomWindow.Draw(new GMPolygonJob()
			{
				Colors = colors,
				Vertices = points,
				Outline = outline
			});

			return null;
		}

		[GMLFunction("draw_arrow")]
		public static object? draw_arrow(object?[] args)
		{
			var x1 = args[0].Conv<double>();
			var y1 = args[1].Conv<double>();
			var x2 = args[2].Conv<double>();
			var y2 = args[3].Conv<double>();
			var size = args[4].Conv<double>();

			// todo : name all these variables better and refactor this

			var height = y2 - y1;
			var length = x2 - x1;

			var magnitude = Math.Sqrt((height * height) + (length * length));

			if (magnitude != 0)
			{
				// draw body of arrow

				var col = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha);

				CustomWindow.Draw(new GMLineJob()
				{
					col1 = col,
					col2 = col,
					width = 1,
					x1 = (float)x1,
					y1 = (float)y1,
					x2 = (float)x2,
					y2 = (float)y2
				});

				// draw head of arrow

				var headSize = magnitude;
				if (size <= magnitude)
				{
					headSize = size;
				}

				var headLength = (length * headSize) / magnitude;
				var headHeight = (height * headSize) / magnitude;

				var a = (x2 - headLength) - headHeight / 3;
				var b = (headLength / 3) + (y2 - headHeight);

				var c = (headHeight / 3) + (x2 - headLength);
				var d = (y2 - headHeight) - headLength / 3;

				CustomWindow.Draw(new GMPolygonJob()
				{
					Colors = [col, col, col],
					Vertices = [new(x2, y2), new(a, b), new(c, d)],
					Outline = false
				});
			}

			return null;
		}

		// draw_button

		[GMLFunction("draw_healthbar")]
		public static object? draw_healthbar(object?[] args)
		{
			var x1 = args[0].Conv<double>();
			var y1 = args[1].Conv<double>();
			var x2 = args[2].Conv<double>();
			var y2 = args[3].Conv<double>();
			var amount = args[4].Conv<double>();
			var backcol = args[5].Conv<int>();
			var mincol = args[6].Conv<int>();
			var maxcol = args[7].Conv<int>();
			var direction = args[8].Conv<int>();
			var showback = args[9].Conv<bool>();
			var showborder = args[10].Conv<bool>();

			var midcol = merge_colour(new object[] { mincol, maxcol, 0.5 });

			if (showback)
			{
				draw_rectangle_colour(new object?[] { x1, y1, x2, y2, backcol, backcol, backcol, backcol, false });

				if (showborder)
				{
					draw_rectangle_colour(new object?[] { x1, y1, x2, y2, 0, 0, 0, 0, true });
				}
			}

			amount = Math.Clamp(amount, 0, 100);

			var fraction = amount / 100;

			var barx1 = 0d;
			var bary1 = 0d;
			var barx2 = 0d;
			var bary2 = 0d;

			switch (direction)
			{
				case 0:
					barx1 = x1;
					bary1 = y1;
					barx2 = x1 + fraction * (x2 - x1);
					bary2 = y2;
					break;
				case 1:
					barx1 = x2 - fraction * (x2 - x1);
					bary1 = y1;
					barx2 = x2;
					bary2 = y2;
					break;
				case 2:
					barx1 = x1;
					bary1 = y1;
					barx2 = x2;
					bary2 = y1 + fraction * (y2 - y1);
					break;
				case 3:
					barx1 = x1;
					bary1 = y2 - fraction * (y2 - y1);
					barx2 = x2;
					bary2 = y2;
					break;
			}

			var col = 0;
			if (amount > 50)
			{
				col = merge_colour(new object[] { midcol, maxcol, (amount - 50) / 50 }).Conv<int>();
			}
			else
			{
				col = merge_colour(new object[] { mincol, midcol, amount / 50 }).Conv<int>();
			}

			draw_rectangle_colour(new object?[] { barx1, bary1, barx2, bary2, col, col, col, col, false });
			if (showborder)
			{
				draw_rectangle_colour(new object?[] { x1, y1, x2, y2, 0, 0, 0, 0, true });
			}

			return null;
		}

		[GMLFunction("draw_path")]
		public static object? draw_path(object?[] args)
		{
			var id = args[0].Conv<int>();
			var x = args[1].Conv<float>();
			var y = args[2].Conv<float>();
			var absolute = args[3].Conv<bool>();

			var path = PathManager.Paths[id];

			if (absolute)
			{
				PathManager.DrawPath(path, 0, 0, absolute);
			}
			else
			{
				PathManager.DrawPath(path, x, y, absolute);
			}

			return null;
		}

		// draw_point_color
		// draw_point_colour

		[GMLFunction("draw_line_color")]
		[GMLFunction("draw_line_colour")]
		public static object? draw_line_color(object?[] args)
		{
			var x1 = args[0].Conv<double>();
			var y1 = args[1].Conv<double>();
			var x2 = args[2].Conv<double>();
			var y2 = args[3].Conv<double>();
			var col1 = args[4].Conv<int>();
			var col2 = args[5].Conv<int>();

			CustomWindow.Draw(new GMLineJob()
			{
				col1 = col1.ABGRToCol4(SpriteManager.DrawAlpha),
				col2 = col2.ABGRToCol4(SpriteManager.DrawAlpha),
				width = 1,
				x1 = (float)x1,
				y1 = (float)y1,
				x2 = (float)x2,
				y2 = (float)y2
			});

			return null;
		}

		[GMLFunction("draw_line_width_color")]
		[GMLFunction("draw_line_width_colour")]
		public static object? draw_line_width_color(object?[] args)
		{
			var x1 = args[0].Conv<double>();
			var y1 = args[1].Conv<double>();
			var x2 = args[2].Conv<double>();
			var y2 = args[3].Conv<double>();
			var width = args[4].Conv<double>();
			var col1 = args[5].Conv<int>();
			var col2 = args[6].Conv<int>();

			CustomWindow.Draw(new GMLineJob()
			{
				x1 = (float)x1,
				y1 = (float)y1,
				x2 = (float)x2,
				y2 = (float)y2,
				width = (float)width,
				col1 = col1.ABGRToCol4(SpriteManager.DrawAlpha),
				col2 = col2.ABGRToCol4(SpriteManager.DrawAlpha)
			});

			return null;
		}

		[GMLFunction("draw_rectangle_color")]
		[GMLFunction("draw_rectangle_colour")]
		public static object? draw_rectangle_colour(object?[] args)
		{
			var x1 = args[0].Conv<double>();
			var y1 = args[1].Conv<double>();
			var x2 = args[2].Conv<double>();
			var y2 = args[3].Conv<double>();
			var col1 = args[4].Conv<int>();
			var col2 = args[5].Conv<int>();
			var col3 = args[6].Conv<int>();
			var col4 = args[7].Conv<int>();
			var outline = args[8].Conv<bool>();

			x2 += 1;
			y2 += 1;

			CustomWindow.Draw(new GMPolygonJob()
			{
				Outline = outline,
				Vertices = new[]
				{
					new Vector2d(x1, y1),
					new Vector2d(x2, y1),
					new Vector2d(x2, y2),
					new Vector2d(x1, y2)
				},
				Colors = new[]
				{
					col1.ABGRToCol4(SpriteManager.DrawAlpha),
					col2.ABGRToCol4(SpriteManager.DrawAlpha),
					col3.ABGRToCol4(SpriteManager.DrawAlpha),
					col4.ABGRToCol4(SpriteManager.DrawAlpha)
				}
			});

			return null;
		}

		// draw_roundrect_color
		// draw_roundrect_colour
		// draw_roundrect_color_ext
		// draw_roundrect_colour_ext

		[GMLFunction("draw_triangle_color")]
		[GMLFunction("draw_triangle_colour")]
		public static object? draw_triangle_color(object?[] args)
		{
			var x1 = args[0].Conv<double>();
			var y1 = args[1].Conv<double>();
			var x2 = args[2].Conv<double>();
			var y2 = args[3].Conv<double>();
			var x3 = args[4].Conv<double>();
			var y3 = args[5].Conv<double>();
			var col1 = args[6].Conv<int>();
			var col2 = args[7].Conv<int>();
			var col3 = args[8].Conv<int>();
			var outline = args[9].Conv<bool>();

			CustomWindow.Draw(new GMPolygonJob()
			{
				Outline = outline,
				Vertices = new[]
				{
					new Vector2d(x1, y1),
					new Vector2d(x2, y2),
					new Vector2d(x3, y3)
				},
				Colors = new[] {
					col1.ABGRToCol4(SpriteManager.DrawAlpha),
					col2.ABGRToCol4(SpriteManager.DrawAlpha),
					col3.ABGRToCol4(SpriteManager.DrawAlpha) }
			});

			return null;
		}

		[GMLFunction("draw_circle_color", GMLFunctionFlags.Stub)]
		[GMLFunction("draw_circle_colour", GMLFunctionFlags.Stub)]
		public static object? draw_circle_color(object?[] args)
		{
			return null;
		}

		// draw_ellipse_color
		// draw_ellipse_colour
		// draw_get_circle_precision

		[GMLFunction("draw_set_circle_precision")]
		public static object? draw_set_circle_precision(object?[] args)
		{
			var precision = args[0].Conv<int>();

			if (precision < 4)
			{
				precision = 4;
			}

			if (precision >= 65)
			{
				precision = 64;
			}

			// ensure is a multiple of 4
			precision = 4 * (int)Math.Truncate(precision / 4.0);

			DrawManager.CirclePrecision = precision;

			// TODO: html/c++ also caches sin/cos values. should we do that?

			return null;
		}

		public static PrimitiveType PrimType;
		public static List<VertexManager.Vertex> Vertices = new();

		[GMLFunction("draw_primitive_begin")]
		public static object? draw_primitive_begin(object?[] args)
		{
			var kind = args[0].Conv<int>();

			// cant just convert straight to PrimitiveType bc LineLoop isnt used grr
			PrimType = kind switch
			{
				1 => PrimitiveType.Points,
				2 => PrimitiveType.Lines,
				3 => PrimitiveType.LineStrip,
				4 => PrimitiveType.Triangles,
				5 => PrimitiveType.TriangleStrip,
				6 => PrimitiveType.TriangleFan,
				_ => throw new ArgumentOutOfRangeException(),
			};

			/* i think on c++ you could use draw_primitive_begin multiple times, and since it only
			 * sets g_NumPrims to 0, it should leave the newer vertices intact while overwritting older ones
			 * on html it just creates a new vbuffer so that doesnt happen
			 */

			Vertices = new();

			return null;
		}

		// draw_primitive_begin_texture

		[GMLFunction("draw_primitive_end")]
		public static object? draw_primitive_end(object?[] args)
		{
			VertexManager.Draw(PrimType, Vertices.ToArray());
			return null;
		}

		[GMLFunction("draw_vertex")]
		public static object? draw_vertex(object?[] args)
		{
			var x = args[0].Conv<double>();
			var y = args[1].Conv<double>();

			// TODO : C++ imposes a limit of 1001 vertices, should we do the same?

			Vertices.Add(new(new(x, y), SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha), new(0, 0)));
			return null;
		}

		[GMLFunction("draw_vertex_color")]
		[GMLFunction("draw_vertex_colour")]
		public static object? draw_vertex_colour(object?[] args)
		{
			var x = args[0].Conv<double>();
			var y = args[1].Conv<double>();
			var col = args[2].Conv<int>();
			var alpha = args[3].Conv<double>();

			// TODO : C++ imposes a limit of 1001 vertices, should we do the same?

			Vertices.Add(new(new(x, y), col.ABGRToCol4(alpha), new(0, 0)));
			return null;
		}

		// draw_vertex_texture
		// draw_vertex_texture_color
		// draw_vertex_texture_colour

		[GMLFunction("sprite_get_uvs", GMLFunctionFlags.Stub)]
		public static object? sprite_get_uvs(object?[] args)
		{
			var spr = args[0].Conv<int>();
			var subimg = args[0].Conv<int>();

			return new int[8];
		}

		// font_get_uvs
		// font_get_info
		// font_cache_glyph

		[GMLFunction("sprite_get_texture", GMLFunctionFlags.Stub)]
		public static object? sprite_get_texture(object?[] args)
		{
			var spr = args[0].Conv<int>();
			var subimg = args[0].Conv<int>();

			return 0;
		}

		// sprite_get_info
		// font_get_texture
		// texture_get_width
		// texture_get_height
		// texture_preload
		// texture_set_priority
		// texture_global_scale
		// texture_get_uvs

		[GMLFunction("draw_get_font")]
		public static object draw_get_font(object?[] args)
		{
			if (TextManager.fontAsset == null)
			{
				return -1;
			}

			return TextManager.fontAsset.AssetIndex;
		}

		[GMLFunction("draw_set_font")]
		public static object? draw_set_font(object?[] args)
		{
			var font = args[0].Conv<int>();

			var library = TextManager.FontAssets;
			var fontAsset = library.First(x => x.AssetIndex == font);
			TextManager.fontAsset = fontAsset;
			return null;
		}

		[GMLFunction("draw_get_halign")]
		public static object? draw_get_halign(object?[] args)
		{
			return (int)TextManager.halign;
		}

		[GMLFunction("draw_set_halign")]
		public static object? draw_set_halign(object?[] args)
		{
			var halign = args[0].Conv<int>();
			TextManager.halign = (HAlign)halign;
			return null;
		}

		[GMLFunction("draw_get_valign")]
		public static object? draw_get_valign(object?[] args)
		{
			return (int)TextManager.valign;
		}

		[GMLFunction("draw_set_valign")]
		public static object? draw_set_valign(object?[] args)
		{
			var valign = args[0].Conv<int>();
			TextManager.valign = (VAlign)valign;
			return null;
		}

		[GMLFunction("string_width")]
		public static object string_width(object?[] args)
		{
			var str = args[0].Conv<string>();

			return TextManager.StringWidth(str);
		}

		[GMLFunction("string_height")]
		public static object string_height(object?[] args)
		{
			var str = args[0].Conv<string>();

			if (TextManager.fontAsset == null)
			{
				return 1;
			}

			var lines = TextManager.SplitText(str, -1, TextManager.fontAsset);
			var textHeight = TextManager.TextHeight(str);
			if (lines == null)
			{
				return textHeight;
			}
			else
			{
				return textHeight * lines.Count;
			}
		}

		// string_width_ext
		// string_height_ext

		[GMLFunction("draw_text")]
		public static object? draw_text(object?[] args)
		{
			var x = args[0].Conv<double>();
			var y = args[1].Conv<double>();
			var str = args[2].Conv<string>();
			TextManager.DrawText(x, y, str);
			return null;
		}

		[GMLFunction("draw_text_ext")]
		public static object? draw_text_ext(object?[] args)
		{
			var x = args[0].Conv<double>();
			var y = args[1].Conv<double>();
			var str = args[2].Conv<string>();
			var sep = args[3].Conv<int>();
			var w = args[4].Conv<double>();

			var c = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha);

			CustomWindow.Draw(new GMTextJob()
			{
				Colors = [c, c, c, c],
				angle = 0,
				asset = TextManager.fontAsset,
				halign = TextManager.halign,
				valign = TextManager.valign,
				lineSep = sep,
				text = str,
				screenPos = new(x, y),
				scale = Vector2d.One
			});

			return null;
		}

		[GMLFunction("draw_text_transformed")]
		public static object? draw_text_transformed(object?[] args)
		{
			var x = args[0].Conv<double>();
			var y = args[1].Conv<double>();
			var str = args[2].Conv<string>();
			var xscale = args[3].Conv<double>();
			var yscale = args[4].Conv<double>();
			var angle = args[5].Conv<double>();
			TextManager.DrawTextTransformed(x, y, str, xscale, yscale, angle);
			return null;
		}

		[GMLFunction("draw_text_ext_transformed")]
		public static object? draw_text_ext_transformed(object?[] args)
		{
			var x = args[0].Conv<double>();
			var y = args[1].Conv<double>();
			var str = args[2].Conv<string>();
			var sep = args[3].Conv<int>();
			var w = args[4].Conv<int>(); // TODO : implement
			var xscale = args[5].Conv<double>();
			var yscale = args[6].Conv<double>();
			var angle = args[7].Conv<double>();

			var c = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha);

			CustomWindow.Draw(new GMTextJob()
			{
				screenPos = new(x, y),
				asset = TextManager.fontAsset,
				angle = angle,
				Colors = [c, c, c, c],
				halign = TextManager.halign,
				valign = TextManager.valign,
				scale = new(xscale, yscale),
				lineSep = sep,
				text = str
			});

			return null;
		}

		[GMLFunction("draw_text_color")]
		[GMLFunction("draw_text_colour")]
		public static object? draw_text_colour(object?[] args)
		{
			var x = args[0].Conv<double>();
			var y = args[1].Conv<double>();
			var str = args[2].Conv<string>();
			var c1 = args[3].Conv<int>();
			var c2 = args[4].Conv<int>();
			var c3 = args[5].Conv<int>();
			var c4 = args[6].Conv<int>();
			var alpha = args[7].Conv<double>();

			TextManager.DrawTextColor(x, y, str, c1, c2, c3, c4, alpha);

			return null;
		}

		[GMLFunction("draw_text_transformed_color", GMLFunctionFlags.Stub, stubLogType: DebugLog.LogType.Warning)]
		[GMLFunction("draw_text_transformed_colour", GMLFunctionFlags.Stub, stubLogType: DebugLog.LogType.Warning)]
		public static object? draw_text_transformed_colour(object?[] args)
		{
			return null;
		}

		// draw_text_ext_color
		// draw_text_ext_colour
		// draw_text_ext_transformed_color
		// draw_text_ext_transformed_colour

		[GMLFunction("draw_self")]
		public static object? draw_self(object?[] args)
		{
			SpriteManager.DrawSelf(VMExecutor.Self.GMSelf);
			return null;
		}

		[GMLFunction("draw_sprite")]
		public static object? draw_sprite(object?[] args)
		{
			var sprite = args[0].Conv<int>();
			var subimg = args[1].Conv<int>();
			var x = args[2].Conv<double>();
			var y = args[3].Conv<double>();

			if (subimg == -1)
			{
				subimg = (int)VMExecutor.Self.GMSelf.image_index;
			}

			SpriteManager.DrawSprite(sprite, subimg, x, y);
			return null;
		}

		[GMLFunction("draw_sprite_ext")]
		public static object? draw_sprite_ext(object?[] args)
		{
			var sprite = args[0].Conv<int>();
			var subimg = args[1].Conv<int>();
			var x = args[2].Conv<double>();
			var y = args[3].Conv<double>();
			var xscale = args[4].Conv<double>();
			var yscale = args[5].Conv<double>();
			var rot = args[6].Conv<double>();
			var colour = args[7].Conv<int>();
			var alpha = args[8].Conv<double>();

			if (subimg == -1)
			{
				subimg = (int)VMExecutor.Self.GMSelf.image_index;
			}

			SpriteManager.DrawSpriteExt(sprite, subimg, x, y, xscale, yscale, rot, colour, alpha);
			return null;
		}

		[GMLFunction("draw_sprite_pos")]
		public static object? draw_sprite_pos(object?[] args)
		{
			var sprite = args[0].Conv<int>();
			var subimg = args[1].Conv<int>();
			var x1 = args[2].Conv<double>();
			var y1 = args[3].Conv<double>();
			var x2 = args[4].Conv<double>();
			var y2 = args[5].Conv<double>();
			var x3 = args[6].Conv<double>();
			var y3 = args[7].Conv<double>();
			var x4 = args[8].Conv<double>();
			var y4 = args[9].Conv<double>();
			var alpha = args[10].Conv<double>();

			if (subimg == -1)
			{
				subimg = (int)VMExecutor.Self.GMSelf.image_index;
			}

			var col = 16777215.ABGRToCol4(alpha); // c_white
			var pageItem = SpriteManager.GetSpritePage(sprite, subimg);
			var (pageTexture, id) = PageManager.TexturePages[pageItem.Page];

			var x = (double)pageItem.SourcePosX / pageTexture.Width;
			var y = (double)pageItem.SourcePosY / pageTexture.Height;
			var w = (double)pageItem.SourceSizeX / pageTexture.Width;
			var h = (double)pageItem.SourceSizeY / pageTexture.Height;

			CustomWindow.Draw(new GMTexturedPolygonJob()
			{
				Texture = pageItem,
				Colors = [col, col, col],
				Vertices = [new(x1, y1), new(x2, y2), new(x3, y3)],
				UVs = [new(x, y), new(x + w, y), new(x + w, y + h)]
			});

			CustomWindow.Draw(new GMTexturedPolygonJob()
			{
				Texture = pageItem,
				Colors = [col, col, col],
				Vertices = [new(x3, y3), new(x4, y4), new(x1, y1)],
				UVs = [new(x + w, y + h), new(x, y + h), new(x, y)]
			});

			return null;
		}

		[GMLFunction("draw_sprite_stretched")]
		public static object? draw_sprite_stretched(object?[] args)
		{
			var sprite = args[0].Conv<int>();
			var subimg = args[1].Conv<int>();
			var x = args[2].Conv<double>();
			var y = args[3].Conv<double>();
			var w = args[4].Conv<double>();
			var h = args[5].Conv<double>();

			if (subimg == -1)
			{
				subimg = (int)VMExecutor.Self.GMSelf.image_index;
			}

			SpriteManager.draw_sprite_stretched(sprite, subimg, x, y, w, h, 0x00FFFFFF, 1);
			return null;
		}

		[GMLFunction("draw_sprite_stretched_ext")]
		public static object? draw_sprite_stretched_ext(object?[] args)
		{
			var sprite = args[0].Conv<int>();
			var subimg = args[1].Conv<int>();
			var x = args[2].Conv<double>();
			var y = args[3].Conv<double>();
			var w = args[4].Conv<double>();
			var h = args[5].Conv<double>();
			var colour = args[6].Conv<int>();
			var alpha = args[7].Conv<double>();

			if (subimg == -1)
			{
				subimg = (int)VMExecutor.Self.GMSelf.image_index;
			}

			SpriteManager.draw_sprite_stretched(sprite, subimg, x, y, w, h, colour, alpha);
			return null;
		}

		[GMLFunction("draw_sprite_part")]
		public static object? draw_sprite_part(object?[] args)
		{
			var sprite = args[0].Conv<int>();
			var subimg = args[1].Conv<int>();
			var left = args[2].Conv<int>();
			var top = args[3].Conv<int>();
			var width = args[4].Conv<int>();
			var height = args[5].Conv<int>();
			var x = args[6].Conv<double>();
			var y = args[7].Conv<double>();

			if (subimg == -1)
			{
				subimg = (int)VMExecutor.Self.GMSelf.image_index;
			}

			SpriteManager.DrawSpritePart(sprite, subimg, left, top, width, height, x, y);

			return null;
		}

		[GMLFunction("draw_sprite_part_ext")]
		public static object? draw_sprite_part_ext(object?[] args)
		{
			var sprite = args[0].Conv<int>();
			var subimg = args[1].Conv<int>();
			var left = args[2].Conv<int>();
			var top = args[3].Conv<int>();
			var width = args[4].Conv<int>();
			var height = args[5].Conv<int>();
			var x = args[6].Conv<double>();
			var y = args[7].Conv<double>();
			var xscale = args[8].Conv<double>();
			var yscale = args[9].Conv<double>();
			var colour = args[10].Conv<int>();
			var alpha = args[11].Conv<double>();

			if (subimg == -1)
			{
				subimg = (int)VMExecutor.Self.GMSelf.image_index;
			}

			SpriteManager.DrawSpritePartExt(sprite, subimg, left, top, width, height, x, y, xscale, yscale, colour, alpha);

			return null;
		}

		[GMLFunction("draw_sprite_general")]
		public static object? draw_sprite_general(object?[] args)
		{
			var sprite = args[0].Conv<int>();
			var subimg = args[1].Conv<int>();
			var left = args[2].Conv<int>();
			var top = args[3].Conv<int>();
			var width = args[4].Conv<int>();
			var height = args[5].Conv<int>();
			var x = args[6].Conv<double>();
			var y = args[7].Conv<double>();
			var xscale = args[8].Conv<double>();
			var yscale = args[9].Conv<double>();
			var rot = args[10].Conv<double>();
			var c1 = args[11].Conv<int>();
			var c2 = args[12].Conv<int>();
			var c3 = args[13].Conv<int>();
			var c4 = args[14].Conv<int>();
			var alpha = args[15].Conv<double>();

			CustomWindow.Draw(new GMSpritePartJob()
			{
				texture = SpriteManager.GetSpritePage(sprite, subimg),
				screenPos = new(x, y),
				angle = rot,
				scale = new(xscale, yscale),
				Colors = [c1.ABGRToCol4(alpha), c2.ABGRToCol4(alpha), c3.ABGRToCol4(alpha), c4.ABGRToCol4(alpha)],
				origin = Vector2.Zero,
				left = left,
				top = top,
				width = width,
				height = height
			});

			return null;
		}

		// draw_sprite_tiled

		[GMLFunction("draw_sprite_tiled_ext")]
		public static object? draw_sprite_tiled_ext(object?[] args)
		{
			var sprite = args[0].Conv<int>();
			var subimg = args[1].Conv<int>();
			var x = args[2].Conv<double>();
			var y = args[3].Conv<double>();
			var xscale = args[4].Conv<double>();
			var yscale = args[5].Conv<double>();
			var colour = args[6].Conv<int>();
			var alpha = args[7].Conv<double>();

			if (subimg == -1)
			{
				subimg = (int)VMExecutor.Self.GMSelf.image_index;
			}

			var spriteTex = SpriteManager.GetSpritePage(sprite, subimg);

			var sizeWidth = spriteTex.TargetSizeX;
			var sizeHeight = spriteTex.TargetSizeY;

			var tempX = x;
			var tempY = y;

			var viewTopLeftX = CustomWindow.Instance.X;
			var viewTopLeftY = CustomWindow.Instance.Y;

			while (tempX > viewTopLeftX)
			{
				tempX -= sizeWidth;
			}

			while (tempY > viewTopLeftY)
			{
				tempY -= sizeHeight;
			}

			// tempX and tempY are now the topleft-most co-ords that are offscreen

			var xOffscreenValue = viewTopLeftX - tempX;
			var yOffscreenValue = viewTopLeftY - tempY;

			var countToDrawHoriz = CustomMath.CeilToInt((RoomManager.CurrentRoom.CameraWidth + (float)xOffscreenValue) / sizeWidth);
			var countToDrawVert = CustomMath.CeilToInt((RoomManager.CurrentRoom.CameraHeight + (float)yOffscreenValue) / sizeHeight);

			for (var i = 0; i < countToDrawVert; i++)
			{
				for (var j = 0; j < countToDrawHoriz; j++)
				{
					SpriteManager.DrawSpriteExt(sprite, subimg, tempX + (j * sizeWidth), tempY + (i * sizeHeight), xscale, yscale, 0, colour, alpha);
				}
			}

			return null;
		}

		// shader_enable_corner_id

		[GMLFunction("surface_create")]
		public static object surface_create(object?[] args)
		{
			var w = args[0].Conv<int>();
			var h = args[1].Conv<int>();

			var format = 0; // TODO : work out if this actually is surface_rgba8unorm

			if (args.Length == 3)
			{
				format = args[2].Conv<int>();
			}

			return SurfaceManager.CreateSurface(w, h, format);
		}

		// surface_create_ext

		[GMLFunction("surface_resize")]
		public static object? surface_resize(object?[] args)
		{
			var surface_id = args[0].Conv<int>();
			var w = args[1].Conv<int>();
			var h = args[2].Conv<int>();

			if (w < 1 || h < 1 || w > 8192 || h > 8192)
			{
				throw new NotImplementedException("Invalid surface dimensions");
			}

			if (surface_id == SurfaceManager.application_surface)
			{
				SurfaceManager.NewApplicationSize = true;
				SurfaceManager.NewApplicationWidth = w;
				SurfaceManager.NewApplicationHeight = h;
				return null;
			}

			SurfaceManager.ResizeSurface(surface_id, w, h);
			return null;
		}

		[GMLFunction("surface_free")]
		public static object? surface_free(object?[] args)
		{
			var surface = args[0].Conv<int>();
			SurfaceManager.FreeSurface(surface, false);
			return null;
		}

		[GMLFunction("surface_exists")]
		public static object surface_exists(object?[] args)
		{
			var surface = args[0].Conv<int>();
			return SurfaceManager.surface_exists(surface);
		}

		[GMLFunction("surface_get_width")]
		public static object surface_get_width(object?[] args)
		{
			var surface_id = args[0].Conv<int>();
			return SurfaceManager.GetSurfaceWidth(surface_id);
		}

		[GMLFunction("surface_get_height")]
		public static object surface_get_height(object?[] args)
		{
			var surface_id = args[0].Conv<int>();
			return SurfaceManager.GetSurfaceHeight(surface_id);
		}

		[GMLFunction("surface_get_texture", GMLFunctionFlags.Stub, stubLogType: DebugLog.LogType.Warning)]
		public static object? surface_get_texture(object?[] args)
		{
			return -1;
		}

		// surface_get_target

		[GMLFunction("surface_set_target")]
		public static object surface_set_target(object?[] args)
		{
			var id = args[0].Conv<int>();

			if (args.Length == 2)
			{
				throw new NotImplementedException("depth surface passed uh oh");
			}

			return SurfaceManager.surface_set_target(id);
		}

		// surface_get_target_ext
		// surface_set_target_ext

		[GMLFunction("surface_reset_target")]
		public static object surface_reset_target(object?[] args)
		{
			return SurfaceManager.surface_reset_target();
		}

		// surface_depth_disable
		// surface_get_depth_disable

		[GMLFunction("draw_surface")]
		public static object? draw_surface(object?[] args)
		{
			var id = args[0].Conv<int>();
			var x = args[1].Conv<double>();
			var y = args[2].Conv<double>();

			SurfaceManager.draw_surface(id, x, y);

			return null;
		}

		[GMLFunction("draw_surface_ext")]
		public static object? draw_surface_ext(object?[] args)
		{
			var id = args[0].Conv<int>();
			var x = args[1].Conv<double>();
			var y = args[2].Conv<double>();
			var xscale = args[3].Conv<double>();
			var yscale = args[4].Conv<double>();
			var rot = args[5].Conv<double>();
			var col = args[6].Conv<int>();
			var alpha = args[7].Conv<double>();

			SurfaceManager.draw_surface_ext(id, x, y, xscale, yscale, rot, col, alpha);

			return null;
		}

		[GMLFunction("draw_surface_stretched")]
		public static object? draw_surface_stretched(object?[] args)
		{
			var id = args[0].Conv<int>();
			var x = args[1].Conv<double>();
			var y = args[2].Conv<double>();
			var w = args[3].Conv<double>();
			var h = args[4].Conv<double>();

			SurfaceManager.draw_surface_stretched(id, x, y, w, h);
			return null;
		}

		// draw_surface_stretched_ext
		// draw_surface_part
		// draw_surface_part_ext
		// draw_surface_general

		[GMLFunction("draw_surface_tiled", GMLFunctionFlags.Stub)]
		public static object? draw_surface_tiled(object?[] args)
		{
			return null;
		}

		// draw_surface_tiled_ext
		// surface_save
		// surface_save_part
		// surface_getpixel

		[GMLFunction("surface_getpixel_ext")]
		public static object? surface_getpixel_ext(object?[] args)
		{
			var surfaceid = args[0].Conv<int>();
			var x = args[1].Conv<int>();
			var y = args[2].Conv<int>();

			SurfaceManager.BindSurfaceTexture(surfaceid);

			var values = new byte[4];
			unsafe
			{
				fixed (byte* ptr = values)
					GL.ReadPixels(x, y, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)ptr);
			}
			GL.BindTexture(TextureTarget.Texture2D, 0);

			// TODO : check this
			var r = values[0];
			var g = values[1];
			var b = values[2];
			var a = values[3];
			return r | g << 8 | b << 16 | a << 24;
		}

		[GMLFunction("surface_copy")]
		public static object? surface_copy(object?[] args)
		{
			var destination = args[0].Conv<int>();
			var x = args[1].Conv<int>();
			var y = args[2].Conv<int>();
			var source = args[3].Conv<int>();

			SurfaceManager.Copy(destination, x, y, source, 0, 0, SurfaceManager.GetSurfaceWidth(source), SurfaceManager.GetSurfaceHeight(source));

			return null;
		}

		[GMLFunction("surface_copy_part")]
		public static object? surface_copy_part(object?[] args)
		{
			var destination = args[0].Conv<int>();
			var x = args[1].Conv<int>();
			var y = args[2].Conv<int>();
			var source = args[3].Conv<int>();
			var xs = args[4].Conv<int>();
			var ys = args[5].Conv<int>();
			var ws = args[6].Conv<int>();
			var hs = args[7].Conv<int>();

			SurfaceManager.Copy(destination, x, y, source, xs, ys, ws, hs);

			return null;
		}

		[GMLFunction("application_surface_draw_enable", GMLFunctionFlags.Stub)]
		public static object? application_surface_draw_enable(object?[] args)
		{
			var enabled = args[0].Conv<bool>();

			return null;
		}

		// skeleton stuff
	}
}

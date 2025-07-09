using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenTK.Mathematics;

namespace OpenGM;
public static class TextManager
{
	public static List<FontAsset> FontAssets = new();

	public static FontAsset fontAsset = null!;

	public static HAlign halign;
	public static VAlign valign;

	public static void DrawText(double x, double y, string text)
	{
		DrawTextTransformed(x, y, text, 1, 1, 0);
	}

	public static void DrawTextTransformed(double x, double y, string text, double xscale, double yscale, double angle)
	{
		var c = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha);
		CustomWindow.Draw(new GMTextJob()
		{
			text = text,
			screenPos = new Vector2d(x, y),
			Colors = [c, c, c, c],
			halign = halign,
			valign = valign,
			scale = new Vector2d(xscale, yscale),
			angle = angle,
			asset = fontAsset,
			// gamemaker is weird
			// "A value of -1 for the line separation argument will default to a separation based on the height of the "M" character in the chosen font."
			lineSep = FontHeight()
		});
	}

	public static int FontHeight()
	{
		if (fontAsset.entriesDict.TryGetValue('M', out var glyph))
		{
			return glyph.h;
		}

		return fontAsset.entries[0].h;
	}

	// https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/yyFont.js#L1797
	public static List<string> SplitText(string str, int lineWidth, FontAsset font)
	{
		if (lineWidth < 0)
		{
			lineWidth = 10000000;
		}

		var whitespace = ' ';
		var newline = '\n';
		var newline2 = '\r';

		var splitLines = new List<string>();

		var len = str.Length;
		var pNew = str;
		var lastChar = pNew[0];
		var start = 0;
		var end = 0;

		while (start < len)
		{
			var total = 0;

			if (lineWidth == 10000000)
			{
				// If width < 0 (i.e. no wrapping required), then we DON'T strip spaces from the start... we just copy it!
				// (sounds wrong.. but its what they do...)

				while (end < len && pNew[end] != newline && pNew[end] != newline2)
				{
					end++;
					if (end < len)
					{
						lastChar = pNew[end];
					}
					else
					{
						lastChar = (char)0x0;
					}
				}

				char c;
				if (end < len)
				{
					c = pNew[end];
				}
				else
				{
					c = (char)0x0;
				}

				if ((newline == lastChar) && (newline2 == pNew[end]))
				{ end++; continue; } // ignore, we've already split the line

				if ((newline2 == lastChar) && (newline == pNew[end]))
				{ end++; continue; } // ignore, we've already split the line

				if (end == len)
				{
					lastChar = (char)0;
				}
				else
				{
					lastChar = pNew[end];
				}
				
				splitLines.Add(pNew[start..end]);
			}
			else
			{
				// Skip leading whitespace
				while (end < len && total < lineWidth)
				{
					var c = pNew[end];
					if (pNew[end] != whitespace)
					{
						break;
					}

					total += font.entriesDict[c].shift;
					end++;
				}

				// Loop through string and get the number of chars that will fit in the line.
				while (end < len && total < lineWidth)
				{
					var c = pNew[end];
					if (c == '\n')
					{
						break;
					}

					total += font.entriesDict[c].shift;
					end++;
				}

				// If we shot past the end, then move back a bit until we fit.
				if (total > lineWidth)
				{
					end--;
					total -= font.entriesDict[pNew[end]].shift;
				}

				if (pNew[end] == '\n')
				{
					// END of line
					splitLines.Add(pNew[start..(end + 1)]);
				}
				else
				{
					// NOT a new line, but we didn't move on... fatal error. Probably a single char doesn't even fit!
					if (end == start) return splitLines;

					// If we don't END on a "space", OR if the next character isn't a space AS WELL. 
					// then backtrack to the start of the last "word"
					if (end != len)
					{
						if ((pNew[end] != ' ') || (pNew[end] != ' ' && pNew[end + 1] != ' '))
						{
							var e = end;
							while (e < start)
							{
								if (pNew[--e] == ' ')
									break;
							}

							if (e != start)
							{
								end = e;
							}
							else
							{
								while (pNew[end] != ' ' && end < len)
									end++;
							}
						}
					}

					var _end = end;
					if (_end > start)
					{
						while (pNew[_end - 1] == ' ' && _end > 0)
							_end--;
					}

					if (_end != start)
						splitLines.Add(pNew[start..(end + 1)]);
				}
			}
			start = ++end;
		}

		return splitLines;
	}

	public static double TextHeight(string text)
	{
		if (text == null || text.Length == 0)
		{
			return 0;
		}

		// BUG : GM uses ScaleX for the height here. What???

		if (fontAsset.texture == null)
		{
			// runtime created
			return fontAsset.Size * fontAsset.ScaleX;
		}
		else
		{
			var maxGlyphHeight = fontAsset.entries.MaxBy(x => x.h)!.h;
			return maxGlyphHeight * fontAsset.ScaleX;
		}
	}

	public static int StringHeight(string text)
	{
		// TODO : wtf is this?

		var lines = text.SplitLines();

		// get tallest character in last line
		var tallestChar = 0;
		foreach (var character in lines[^1])
		{
			if (!fontAsset.entriesDict.TryGetValue(character, out var glyph))
			{
				continue;
			}

			var height = glyph.h;

			if (height > tallestChar)
			{
				tallestChar = height;
			}
		}

		return ((lines.Length - 1) * FontHeight()) + tallestChar;
	}

	public static int StringWidth(string text)
	{
		var lines = text.SplitLines();

		var longestLine = 0;
		foreach (var line in lines)
		{
			var totalWidth = 0;
			for (var i = 0; i < line.Length; i++)
			{
				var asciiIndex = (int)line[i];

				if (!fontAsset.entriesDict.TryGetValue(asciiIndex, out Glyph? entry))
				{
					// offset sprite text for spaces
					if (fontAsset.IsSpriteFont() && asciiIndex == ' ')
					{
						totalWidth += (int)fontAsset.Size;
					}
					continue;
				}

				totalWidth += (int)(entry.shift * fontAsset.ScaleX);
			}

			if (totalWidth > longestLine)
			{
				longestLine = totalWidth;
			}
		}

		return longestLine;
	}

	public static void DrawTextColor(double x, double y, string text, int c1, int c2, int c3, int c4, double alpha)
	{
		CustomWindow.Draw(new GMTextJob()
		{
			text = text,
			screenPos = new Vector2d(x, y),
			Colors = [c1.ABGRToCol4(alpha), c2.ABGRToCol4(alpha), c3.ABGRToCol4(alpha), c4.ABGRToCol4(alpha)],
			halign = halign,
			valign = valign,
			scale = Vector2.One,
			angle = 0,
			asset = fontAsset,
			// gamemaker is weird
			// "A value of -1 for the line separation argument will default to a separation based on the height of the "M" character in the chosen font."
			lineSep = FontHeight()
		});
	}
}

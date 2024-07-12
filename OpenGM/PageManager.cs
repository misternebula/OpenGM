using OpenTK.Graphics.OpenGL;
using StbImageSharp;

namespace OpenGM;
public static class PageManager
{
	public static Dictionary<string, (ImageResult image, int id)> TexturePages = new();

	public static void BindTextures()
	{
		foreach (var item in TexturePages)
		{
			var image = item.Value.image;

			var newId = GL.GenTexture();

			GL.BindTexture(TextureTarget.Texture2D, newId);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);

			TexturePages[item.Key] = (image, newId);
		}
	}
}

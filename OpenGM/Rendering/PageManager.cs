using OpenTK.Graphics.OpenGL;
using StbImageSharp;

namespace OpenGM.Rendering;
public static class PageManager
{
    public static Dictionary<string, (ImageResult image, int id)> TexturePages = new();

    public static void UnbindTextures()
    {
	    if (TexturePages.Count == 0 || CustomWindow.Instance == null)
	    {
		    return;
	    }

	    Console.Write("Unbinding textures...");

	    GL.BindTexture(TextureTarget.Texture2D, 0);

		foreach (var item in TexturePages)
	    {
            GL.DeleteTexture(item.Value.id);
	    }
	}

    public static void BindTextures()
    {
        Console.Write("Binding textures...");
        
        foreach (var item in TexturePages)
        {
	        BindTexture(item.Key, item.Value.image);
        }
        
        GC.Collect(); // gc to remove above

        Console.WriteLine(" Done!");
    }

    public static void BindTexture(string name, ImageResult image)
    {
	    var newId = GL.GenTexture();

	    GL.BindTexture(TextureTarget.Texture2D, newId);
	    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
	    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
	    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
	    GL.BindTexture(TextureTarget.Texture2D, 0);

	    image.Data = null; // let this get gc'd since it was uploaded to the gpu

	    TexturePages[name] = (image, newId);
    }
}

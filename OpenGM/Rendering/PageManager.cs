using OpenGM.SerializedFiles;
using OpenTK.Graphics.OpenGL;

namespace OpenGM.Rendering;
public static class PageManager
{
    public static Dictionary<string, (TexturePage page, int id)> TexturePages = new();

    public static void BindTextures()
    {
        Console.Write("Binding textures...");
        
        foreach (var item in TexturePages)
        {
            var page = item.Value.page;

            var newId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, newId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, page.Width, page.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, page.Data);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            TexturePages[item.Key] = (page, newId);
        }
        
        Console.WriteLine(" Done!");
    }
}

using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace OpenGM.Rendering;
public static class PageManager
{
    // we probably shouldnt be storing image result here, we literally only use it for width/height and you can get that from opengl
    public static Dictionary<string, (ImageResult image, int id)> TexturePages = new();

    public static void UnbindTextures()
    {
        if (TexturePages.Count == 0 || CustomWindow.Instance == null)
        {
            return;
        }

        Console.Write("Unbinding textures...");

        GL.BindTexture(TextureTarget.Texture2D, 0);
        
        foreach (var name in TexturePages.Keys)
        {
            DeleteTexture(name);
        }
        
        Console.WriteLine(" Done!");
    }

    public static void BindTextures()
    {
        Console.Write("Binding textures...");
        
        foreach (var item in TexturePages)
        {
            UploadTexture(item.Key, item.Value.image);
        }
        
        GC.Collect(); // gc to remove cpu texture data

        Console.WriteLine(" Done!");
    }

    public static void UploadTexture(string name, ImageResult image)
    {
        GraphicsManager.PushMessage($"UploadTexture Name:{name}");

        GL.CreateTextures(TextureTarget.Texture2D, 1, out int texture);
        GraphicsManager.LabelObject(ObjectLabelIdentifier.Texture, texture, name);
        GL.TextureStorage2D(texture, 1, SizedInternalFormat.Rgba8, image.Width, image.Height);
        GL.TextureSubImage2D(texture, 0, 0, 0, image.Width, image.Height, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
        GL.TextureParameter(texture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TextureParameter(texture,  TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        
        image.Data = null; // let this get gc'd since it was uploaded to the gpu

        TexturePages[name] = (image, texture);
        GraphicsManager.PopMessage();
    }

    public static void DeleteTexture(string name)
    {
        GraphicsManager.PushMessage($"DeleteTexture {name}");
        if (TexturePages.Remove(name, out var value))
        {
            GL.DeleteTexture(value.id);
        }
        GraphicsManager.PopMessage();
    }
}

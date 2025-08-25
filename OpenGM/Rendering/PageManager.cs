﻿using OpenTK.Graphics.OpenGL4;
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
        var newId = GL.GenTexture();

        GL.BindTexture(TextureTarget.Texture2D, newId);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        image.Data = null; // let this get gc'd since it was uploaded to the gpu

        TexturePages[name] = (image, newId);
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

namespace OpenGM.SerializedFiles;

public class Background
{
    public required int AssetIndex;

    public required string Name;
    public bool Transparent;
    public bool Smooth;
    public bool Preload;
    public required SpritePageItem? Texture;
}

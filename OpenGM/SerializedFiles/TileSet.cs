namespace OpenGM.SerializedFiles;

public class TileSet
{
    public string Name = null!;
    public int AssetIndex;
    public SpritePageItem Texture = null!;
    public int TileWidth;
    public int TileHeight;
    public int OutputBorderX;
    public int OutputBorderY;
    public int TileColumns;
    public int FramesPerTile;
    public int TileCount;
    public int FrameTime;
    public int[] TileIds = null!;
}

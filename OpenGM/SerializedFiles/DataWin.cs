using MemoryPack;
using OpenGM.VirtualMachine;

namespace OpenGM.SerializedFiles;

[MemoryPackable]
public partial class DataWin
{
	public List<VMScript> Scripts = new();
	public List<ObjectDefinition> Objects = new();
	public List<Room> Rooms = new();
	public List<SpriteData> Sprites = new();
	public List<FontAsset> Fonts = new();
	public List<TexturePage> TexturePages = new();
	public List<TextureGroup> TextureGroups = new();
	public List<TileSet> TileSets = new();
	public List<SoundAsset> Sounds = new();
}

[MemoryPackable]
public partial class TexturePage
{
	public string Name = null!;
	public byte[] PngData = null!;
}

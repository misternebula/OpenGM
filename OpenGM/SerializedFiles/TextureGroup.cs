using MemoryPack;

namespace OpenGM.SerializedFiles;

[MemoryPackable]
public partial class TextureGroup
{
	public string GroupName = null!;
	public string[] TexturePages = null!;
	public int[] Sprites = null!;
	public int[] Fonts = null!;
}

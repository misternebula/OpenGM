using MemoryPack;

namespace OpenGM.SerializedFiles;

[MemoryPackable]
public partial class TexturePage
{
	public string Name = null!;
	public int Width;
	public int Height;
	public byte[] Data = null!;
}

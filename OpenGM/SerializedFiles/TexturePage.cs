using MemoryPack;

namespace OpenGM.SerializedFiles;

[MemoryPackable]
public partial class TexturePage
{
	public string Name = null!;
	public byte[] PngData = null!;
}

using MemoryPack;

namespace OpenGM.SerializedFiles;

[MemoryPackable]
public partial class SoundAsset
{
	public int AssetID;
	public string Name = null!;
	public string File = null!;
	public float Volume;
	public float Pitch;
}

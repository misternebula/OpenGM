using MemoryPack;

namespace OpenGM.SerializedFiles;

[MemoryPackable]
public partial class SoundAsset
{
	public int AssetID;
	public string Name = null!;
	public float Volume;
	public float Pitch;

	public bool IsWav;
	public bool Stereo;
	public int Freq;
	public byte[] Data = null!;
}

namespace OpenGM.SerializedFiles;

[Serializable]
public class SoundAsset
{
	public int AssetID;
	public string Name = null!;
	public string File = null!;
	public float Volume;
	public float Pitch;
}

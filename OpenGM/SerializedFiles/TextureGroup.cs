namespace OpenGM.SerializedFiles;

[Serializable]
public class TextureGroup
{
	public string GroupName = null!;
	public string[] TexturePages = null!;
	public int[] Sprites = null!;
	public int[] Fonts = null!;
}

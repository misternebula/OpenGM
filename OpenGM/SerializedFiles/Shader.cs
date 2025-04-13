using MemoryPack;

namespace OpenGM.SerializedFiles;

[MemoryPackable]
public partial class Shader
{
	public int AssetIndex;
	public string Name = null!;
	public int ShaderType;
	public string VertexSource = null!;
	public string FragmentSource = null!;
	public string[] ShaderAttributes = null!;
}

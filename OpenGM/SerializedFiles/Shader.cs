namespace OpenGM.SerializedFiles;

public class Shader
{
    public int AssetIndex;
    public string Name = null!;
    public int ShaderType;
    public string VertexSource = null!;
    public string FragmentSource = null!;
    public string[] ShaderAttributes = null!;
}

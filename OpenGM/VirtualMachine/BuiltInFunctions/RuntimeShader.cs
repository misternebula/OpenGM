using OpenTK.Graphics.OpenGL;

namespace OpenGM.VirtualMachine.BuiltInFunctions;
public class RuntimeShader
{
    public int ShaderIndex;
    public int ProgramID;
    public Dictionary<string, Uniform> Uniforms = new();
}

public class Uniform
{
    public int Location;
    public string Name = "";
    public int Size;
    public ActiveUniformType Type;
}

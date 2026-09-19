using OpenGM.SerializedFiles;

namespace OpenGM.VirtualMachine;
internal class Method
{
    public object? inst;
    public VMScript func = null!;

    public Method(VMScript func) => this.func = func;

    public override string ToString()
    {
        return $"{{ Method - func: {func} }}";
    }
}

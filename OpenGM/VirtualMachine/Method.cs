namespace OpenGM.VirtualMachine;
internal class Method
{
    public IStackContextSelf? inst;
    public VMScript func = null!;

    public Method(VMScript func) => this.func = func;
}

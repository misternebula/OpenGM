using UndertaleModLib.Models;

namespace OpenGM.VirtualMachine;

/// <summary>
/// stack that also stores the type
/// </summary>
public class DataStack : Stack<(object? value, UndertaleInstruction.DataType type)>
{
    public DataStack() : base() { }
    public DataStack(DataStack stack) : base(stack) { } // maybe cloning isnt needed

    public void Push(object? value, UndertaleInstruction.DataType type)
    {
        base.Push((value, type));
    }

    /// <summary>
    /// checks size before popping
    /// </summary>
    public object? Pop(UndertaleInstruction.DataType typeToPop)
    {
        var (poppedValue, typeOfPopped) = base.Pop();

        // do strict size checking
        if (VMExecutor.VMTypeToSize(typeOfPopped) != VMExecutor.VMTypeToSize(typeToPop))
        {
            throw new NotImplementedException($"Popped value {poppedValue} is type {typeOfPopped}, which can't be converted to {typeToPop}!");
        }

        return poppedValue;
    }

    [Obsolete("dont use this version")]
    public new void Push((object? value, UndertaleInstruction.DataType type) x) => base.Push(x);

    [Obsolete("dont use this version")]
    public new (object? value, UndertaleInstruction.DataType type) Pop() => base.Pop();
}

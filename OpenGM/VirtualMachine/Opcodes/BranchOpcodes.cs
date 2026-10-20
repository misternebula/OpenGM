using OpenGM.SerializedFiles;

namespace OpenGM.VirtualMachine;

public static partial class VMExecutor
{
    public static (ExecutionResult, object?) B(VMCodeInstruction instruction)
    {
        if (instruction.JumpToEnd)
        {
            return (ExecutionResult.JumpedToEnd, null);
        }

        return (ExecutionResult.JumpedToLabel, instruction.IntData);
    }

    public static (ExecutionResult, object?) BT(VMCodeInstruction instruction)
    {
        var boolValue = Call.Stack.Pop(VMType.b).Conv<bool>();
        if (!boolValue)
        {
            return (ExecutionResult.Success, null);
        }

        if (instruction.JumpToEnd)
        {
            return (ExecutionResult.JumpedToEnd, null);
        }

        return (ExecutionResult.JumpedToLabel, instruction.IntData);
    }

    public static (ExecutionResult, object?) BF(VMCodeInstruction instruction)
    {
        var boolValue = Call.Stack.Pop(VMType.b).Conv<bool>();
        if (boolValue)
        {
            return (ExecutionResult.Success, null);
        }

        if (instruction.JumpToEnd)
        {
            return (ExecutionResult.JumpedToEnd, null);
        }

        return (ExecutionResult.JumpedToLabel, instruction.IntData);
    }
}

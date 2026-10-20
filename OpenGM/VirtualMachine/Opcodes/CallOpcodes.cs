using OpenGM.IO;
using OpenGM.SerializedFiles;

namespace OpenGM.VirtualMachine;

public static partial class VMExecutor
{
    public static (ExecutionResult, object?) CALL(VMCodeInstruction instruction)
    {
        var args = new object?[instruction.FunctionArgumentCount];

        for (var i = 0; i < instruction.FunctionArgumentCount; i++)
        {
            // args are always pushed as rvalues
            args[i] = Call.Stack.Pop(VMType.v);
        }

        if (ScriptResolver.ScriptsByName.TryGetValue(instruction.FunctionName, out var scriptName))
        {
            Call.Stack.Push(ExecuteCode(scriptName.GetCode()!, Self, Other, args), VMType.v);
            return (ExecutionResult.Success, null);
        }

        if (ScriptResolver.BuiltInFunctions.TryGetValue(instruction.FunctionName, out var builtInFunction))
        {
            Call.Stack!.Push(builtInFunction!(args), VMType.v);
            return (ExecutionResult.Success, null);
        }

        return (ExecutionResult.Failed, $"Can't resolve script {instruction.FunctionName} !");
    }

    public static (ExecutionResult, object?) CALLV(VMCodeInstruction instruction)
    {
        var method = Method.FetchMethod(Call.Stack.Pop(VMType.v), instruction.IntData);
        var self = Call.Stack.Pop(VMType.v);

        var args = new object?[instruction.IntData];

        for (var i = 0; i < instruction.IntData; i++)
        {
            // args are always pushed as rvalues
            args[i] = Call.Stack.Pop(VMType.v);
        }

        if (method == null)
        {
            throw new NotImplementedException("method is null");
        }

        IStackContextSelf? context = null;

        if (method.inst is null)
        {
            if (self is not null)
            {
                context = FetchSelf(self);
            }
        }
        else
        {
            if (method.inst is int)
            {
                var i = method.inst.Conv<int>();

                if (i == GMConstants.self)
                {
                    context = Self;
                }
                else if (i >= GMConstants.FIRST_INSTANCE_ID)
                {
                    context = InstanceManager.FindByInstanceId(i);
                    DebugLog.Log($"CALLV bound instance lookup: id={i}, found={context != null}");
                }
                else
                {
                    throw new NotImplementedException($"CallV - inst: {method.inst.Conv<int>()}, type: {method.inst.GetType().Name}");
                }
            }
            else if (method.inst is IStackContextSelf stackSelf)
            {
                context = stackSelf;
            }
            else
            {
                throw new NotImplementedException($"CallV - inst: {method.inst.Conv<int>()}, type: {method.inst.GetType().Name}");
            }
        }

        Call.Stack.Push(
            ExecuteCode(
                method.func.GetCode()!,
                context,
                Self,
                args: args),
            VMType.v);

        return (ExecutionResult.Success, null);
    }
}

using OpenGM.SerializedFiles;

namespace OpenGM.VirtualMachine;

public class EnvFrame
{
    public IStackContextSelf? beforeSelf;
    public IStackContextSelf? beforeOther;

    public Stack<(IStackContextSelf? self, IStackContextSelf? other)> Stack = new();

    public override string ToString()
    {
        var str = "";
        foreach (var item in Stack)
        {
            str += $"Self: {item.self}, Other: {item.other}{Environment.NewLine}";
        }

        return str;
    }
}

public static partial class VMExecutor
{
    public static Stack<EnvFrame> EnvStack = new();

    public static (ExecutionResult, object?) PUSHENV(VMCodeInstruction instruction)
    {
        var id = Call.Stack.Pop(VMType.i).Conv<int>();

        if (id == GMConstants.stacktop)
        {
            var top = Call.Stack.Peek();
            if (top.value is not GMLObject)
            {
                id = Call.Stack.Pop(VMType.v).Conv<int>();
            }
        }

        var frame = new EnvFrame();
        frame.beforeSelf = Self;
        frame.beforeOther = Other;

        if (id == GMConstants.noone)
        {
            EnvStack.Push(frame);

            // run the code for no one
            if (instruction.JumpToEnd)
            {
                return (ExecutionResult.JumpedToEnd, null);
            }

            return (ExecutionResult.JumpedToLabel, instruction.IntData);
        }
        else if (id == GMConstants.other)
        {
            throw new NotImplementedException("other");
        }
        else if (id == GMConstants.self)
        {
            throw new NotImplementedException("self");
        }
        else if (id == GMConstants.stacktop)
        {
            throw new NotImplementedException("stacktop");
        }
        else if (id is GMConstants.all)
        {
            throw new NotImplementedException("all");
        }
        else if (id is GMConstants.global)
        {
            throw new NotImplementedException("global");
        }
        else if (id < GMConstants.FIRST_INSTANCE_ID)
        {
            // asset id
            var instances = InstanceManager.FindByAssetId(id);
            instances.Reverse();

            // dont run anything if no instances
            if (instances.Count == 0)
            {
                EnvStack.Push(frame);

                if (instruction.JumpToEnd)
                {
                    return (ExecutionResult.JumpedToEnd, null);
                }

                return (ExecutionResult.JumpedToLabel, instruction.IntData);
            }

            foreach (var instance in instances)
            {
                frame.Stack.Push((instance, Self));
            }

            var top = frame.Stack.Peek();
            Self = top.self;
            Other = top.other;
        }
        else
        {
            var instance = InstanceManager.FindByInstanceId(id);

            if (instance == null)
            {
                EnvStack.Push(frame);

                if (instruction.JumpToEnd)
                {
                    return (ExecutionResult.JumpedToEnd, null);
                }

                return (ExecutionResult.JumpedToLabel, instruction.IntData);
            }

            frame.Stack.Push((instance, Self));

            Self = instance;
            Other = frame.beforeSelf;
        }

        EnvStack.Push(frame);
        return (ExecutionResult.Success, null);
    }

    public static (ExecutionResult, object?) POPENV(VMCodeInstruction instruction)
    {
        var frame = EnvStack.Peek();

        if (instruction.Drop || frame.Stack.Count == 0)
        {
            EnvStack.Pop();
            Self = frame.beforeSelf;
            Other = frame.beforeOther;
            return (ExecutionResult.Success, null);
        }

        frame.Stack.Pop();
        var nextInstanceExists = frame.Stack.TryPeek(out var nextInstance);

        if (!nextInstanceExists)
        {
            EnvStack.Pop();
            Self = frame.beforeSelf;
            Other = frame.beforeOther;
            return (ExecutionResult.Success, null);
        }

        Self = nextInstance.self;
        Other = nextInstance.other;

        if (instruction.JumpToEnd)
        {
            return (ExecutionResult.JumpedToEnd, null);
        }

        return (ExecutionResult.JumpedToLabel, instruction.IntData);
    }
}

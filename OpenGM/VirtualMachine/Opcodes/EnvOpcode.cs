using OpenGM.IO;
using OpenGM.SerializedFiles;
using System.Diagnostics;

namespace OpenGM.VirtualMachine;

public static partial class VMExecutor
{
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

        var newEnvFrame = new VMEnvFrame { IsIterator = true, IteratorStack = new() };

        EnvStack.Push(newEnvFrame);

        if (VerboseStackLogs) DebugLog.Log($"Pushenv {id}");

        if (id == GMConstants.noone)
        {
            // run the code for no one
            if (instruction.JumpToEnd)
            {
                return (ExecutionResult.JumpedToEnd, null);
            }

            return (ExecutionResult.JumpedToLabel, instruction.IntData);
        }
        else if (id == GMConstants.other)
        {
            var newCtx = new VMEnvFrame
            {
                Self = Other.Self,
                ObjectDefinition = Other.ObjectDefinition,
            };

            newEnvFrame.IteratorStack.Push(newCtx);
        }
        else if (id == GMConstants.self)
        {
            var newCtx = new VMEnvFrame
            {
                Self = Self.Self,
                ObjectDefinition = Self.ObjectDefinition,
            };

            newEnvFrame.IteratorStack.Push(newCtx);
        }
        else if (id == GMConstants.stacktop)
        {
            // TODO: this doesn't account for legacy values (all, etc)
            var value = Call.Stack.Pop(VMType.v);
            if (value is GMLObject obj)
            {
                var newCtx = new VMEnvFrame
                {
                    Self = obj,
                    ObjectDefinition = null,
                };

                newEnvFrame.IteratorStack.Push(newCtx);
            }
            else
            {
                throw new UnreachableException($"we check stacktop id above");
            }
        }
        else if (id is GMConstants.all)
        {
	        foreach (var inst in InstanceManager.FindByLegacyValue(GMConstants.all))
	        {
		        var newCtx = new VMEnvFrame()
		        {
			        Self = inst,
			        ObjectDefinition = inst.Definition
		        };

                newEnvFrame.IteratorStack.Push(newCtx);
            }
        }
        else if (id is GMConstants.global)
        {
            throw new NotImplementedException($"Don't know how to pushenv global");
        }
        else if (id < 0)
        {
            // some other negative number??
            DebugLog.LogError($"wtf! other negative number {id} in pushenv!!!");
            if (instruction.JumpToEnd)
            {
                return (ExecutionResult.JumpedToEnd, null);
            }

            return (ExecutionResult.JumpedToLabel, instruction.IntData);
        }
        else if (id < GMConstants.FIRST_INSTANCE_ID)
        {
            // asset id
            var instances = InstanceManager.FindByAssetId(id);
            instances.Reverse();

            // dont run anything if no instances
            if (instances.Count == 0)
            {
                if (VerboseStackLogs) DebugLog.Log($"no instances!");
                
                if (instruction.JumpToEnd)
                {
                    return (ExecutionResult.JumpedToEnd, null);
                }

                return (ExecutionResult.JumpedToLabel, instruction.IntData);
            }

            foreach (var instance in instances)
            {
                var newCtx = new VMEnvFrame
                {
                    Self = instance,
                    ObjectDefinition = instance.Definition,
                };

                if (VerboseStackLogs) DebugLog.Log($"Pushing {instance.instanceId}");
                newEnvFrame.IteratorStack.Push(newCtx);
            }
        }
        else
        {
            var instance = InstanceManager.FindByInstanceId(id);

            if (instance == null)
            {
                if (instruction.JumpToEnd)
                {
                    return (ExecutionResult.JumpedToEnd, null);
                }

                return (ExecutionResult.JumpedToLabel, instruction.IntData);
            }

            var newCtx = new VMEnvFrame
            {
                Self = instance,
                ObjectDefinition = instance.Definition,
            };

            newEnvFrame.IteratorStack.Push(newCtx);
        }

        return (ExecutionResult.Success, null);
    }

    public static (ExecutionResult, object?) POPENV(VMCodeInstruction instruction)
    {
        var frame = EnvStack.Peek();

        if (!frame.IsIterator)
        {
            throw new NotImplementedException("Trying to run PopEnv when the top of the stack isn't an iterator frame!");
        }

        if (instruction.Drop)
        {
            EnvStack.Pop();
            return (ExecutionResult.Success, null);
        }

        if (frame.IteratorStack.Count == 0)
        {
            // Nothing left in iterator stack.
            EnvStack.Pop();
            return (ExecutionResult.Success, null);
        }

        frame.IteratorStack.Pop();
        var nextInstanceExists = frame.IteratorStack.TryPeek(out var nextInstance);

        if (!nextInstanceExists)
        {
            EnvStack.Pop();
            return (ExecutionResult.Success, null);
        }

        // run block with next instance
        if (instruction.JumpToEnd)
        {
            return (ExecutionResult.JumpedToEnd, null);
        }

        return (ExecutionResult.JumpedToLabel, instruction.IntData);
    }
}

using OpenGM.IO;
using System.Diagnostics;

namespace OpenGM.VirtualMachine;

public static partial class VMExecutor
{
	public static (ExecutionResult, object?) PUSHENV(VMCodeInstruction instruction)
	{
		var id = Call.Stack.Pop(VMType.i).Conv<int>();

		if (id == GMConstants.stacktop)
		{
			id = Call.Stack.Pop(VMType.v).Conv<int>();
		}

		// marks the beginning of the instances pushed. popenv will stop jumping when it reaches this
		// SUPER HACKY. there HAS to be a better way of doing this
		EnvStack.Push(null);

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

			EnvStack.Push(newCtx);
		}
		else if (id == GMConstants.self)
		{
			var newCtx = new VMEnvFrame
			{
				Self = Self.Self,
				ObjectDefinition = Self.ObjectDefinition,
			};

			EnvStack.Push(newCtx);
		}
		else if (id is GMConstants.global or GMConstants.all)
		{
			throw new NotImplementedException($"Don't know how to pushenv {id}");
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
				EnvStack.Push(newCtx);
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

			EnvStack.Push(newCtx);
		}

		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object?) POPENV(VMCodeInstruction instruction)
	{
		var currentInstance = EnvStack.Pop();
		var nextInstance = EnvStack.Peek();

		if (instruction.Drop)
		{
			while (currentInstance != null)
			{
				currentInstance = EnvStack.Pop();
			}

			return (ExecutionResult.Success, null);
		}

		// no instances pushed
		if (currentInstance == null)
		{
			return (ExecutionResult.Success, null);
		}

		// no instances left
		if (nextInstance == null)
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

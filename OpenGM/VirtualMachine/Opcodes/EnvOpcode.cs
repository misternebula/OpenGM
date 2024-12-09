using OpenGM.IO;
using System.Diagnostics;

namespace OpenGM.VirtualMachine;

public static partial class VMExecutor
{
	public static (ExecutionResult, object?) PUSHENV(VMCodeInstruction instruction)
	{
		var id = Ctx.Stack.Pop(VMType.i).Conv<int>();

		if (id == GMConstants.stacktop)
		{
			id = Ctx.Stack.Pop(VMType.v).Conv<int>();
		}

		var currentContext = Ctx;

		// marks the beginning of the instances pushed. popenv will stop jumping when it reaches this
		// SUPER HACKY. there HAS to be a better way of doing this
		EnvironmentStack.Push(null!);

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
			// TODO: how does return work??
			var newCtx = new VMScriptExecutionContext
			{
				Self = Other.Self,
				ObjectDefinition = Other.ObjectDefinition,
				// TODO: why copy? is with statement a separate block?
				Stack = new(currentContext.Stack),
				//Locals = new(currentContext.Locals),
				ReturnValue = currentContext.ReturnValue,
				EventType = currentContext.EventType,
				EventIndex = currentContext.EventIndex,
			};

			EnvironmentStack.Push(newCtx);
		}
		else if (id == GMConstants.self)
		{
			// TODO: how does return work??
			var newCtx = new VMScriptExecutionContext
			{
				Self = Self.Self,
				ObjectDefinition = Self.ObjectDefinition,
				// TODO: why copy? is with statement a separate block?
				Stack = new(currentContext.Stack),
				//Locals = new(currentContext.Locals),
				ReturnValue = currentContext.ReturnValue,
				EventType = currentContext.EventType,
				EventIndex = currentContext.EventIndex,
			};

			EnvironmentStack.Push(newCtx);
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
				// TODO: how does return work??
				var newCtx = new VMScriptExecutionContext
				{
					Self = instance,
					ObjectDefinition = instance.Definition,
					// TODO: why copy? is with statement a separate block?
					Stack = new(currentContext.Stack),
					//Locals = new(currentContext.Locals),
					ReturnValue = currentContext.ReturnValue,
					EventType = currentContext.EventType,
					EventIndex = currentContext.EventIndex,
				};

				if (VerboseStackLogs) DebugLog.Log($"Pushing {instance.instanceId}");
				EnvironmentStack.Push(newCtx);
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

			// TODO: how does return work??
			var newCtx = new VMScriptExecutionContext
			{
				Self = instance,
				ObjectDefinition = instance.Definition,
				// TODO: why copy? is with statement a separate block?
				Stack = new(currentContext.Stack),
				//Locals = new(currentContext.Locals),
				ReturnValue = currentContext.ReturnValue,
				EventType = currentContext.EventType,
				EventIndex = currentContext.EventIndex,
			};

			EnvironmentStack.Push(newCtx);
		}

		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object?) POPENV(VMCodeInstruction instruction)
	{
		var currentInstance = EnvironmentStack.Pop();
		var nextInstance = Ctx;

		if (instruction.Drop)
		{
			while (currentInstance != null)
			{
				currentInstance = EnvironmentStack.Pop();
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
			EnvironmentStack.Pop();
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

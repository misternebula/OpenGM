using System.Collections;
using OpenGM.IO;

namespace OpenGM.VirtualMachine;

public static partial class VMExecutor
{
	// TODO: pop builtin/array. yes, it is used in ch2
	
	public static void PopToGlobal(string varName, object? value)
	{
		VariableResolver.GlobalVariables[varName] = value;
	}

	public static void PopToLocal(string varName, object? value)
	{
		//Ctx.Locals[varName] = value;
		CurrentCall.Locals[varName] = value;
	}

	public static void PopToGlobalArray(string varName, int index, object? value)
	{
		VariableResolver.ArraySet(
			index,
			value,
			() => VariableResolver.GlobalVariables.TryGetValue(varName, out var array) ? array as IList : null,
			array => VariableResolver.GlobalVariables[varName] = array);
	}

	public static void PopToLocalArray(string varName, int index, object? value)
	{
		VariableResolver.ArraySet(
			index,
			value,
			() => CurrentCall.Locals.TryGetValue(varName, out var array) ? array as IList : null,
			array => CurrentCall.Locals[varName] = array);
	}

	public static void PopToSelf(IStackContextSelf self, string varName, object? value)
	{
		if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var gettersetter) && self is GamemakerObject gm)
		{
			gettersetter.setter!(gm, value);
		}
		else
		{
			self.SelfVariables[varName] = value;
		}
	}

	public static void PopToSelfArray(IStackContextSelf self, string varName, int index, object? value)
	{
		if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var gettersetter) && self is GamemakerObject gm)
		{
			VariableResolver.ArraySet(
				index,
				value,
				() => gettersetter.getter(gm) as IList, // already did TryGetValue above
				array => gettersetter.setter!(gm, array));
		}
		else
		{
			VariableResolver.ArraySet(
				index,
				value,
				() => self.SelfVariables.TryGetValue(varName, out var array) ? array as IList : null,
				array => self.SelfVariables[varName] = array);
		}
	}

	public static void PopToIndex(int assetId, string varName, object? value)
	{
		if (assetId == GMConstants.builtin)
		{
			if (Self.Self == null)
			{
				// for global scripts
				PopToGlobal(varName, value);
				return;
			}

			if (!Self.Self.SelfVariables.TryAdd(varName, value))
			{
				Self.Self.SelfVariables[varName] = value;
			}

			VariableResolver.BuiltInSelfVariables.Add(varName, (
					(obj) => obj.SelfVariables[varName]!,
					(obj, val) => obj.SelfVariables[varName] = val));
			return;
		}

		if (assetId < GMConstants.FIRST_INSTANCE_ID)
		{
			// Asset Index
			var instances = InstanceManager.FindByAssetId(assetId);

			foreach (var instance in instances)
			{
				if (instance == null)
				{
					throw new NotImplementedException();
				}

				PopToSelf(instance, varName, value);
			}
		}
		else
		{
			// Instance Id
			var instance = InstanceManager.FindByInstanceId(assetId);

			if (instance == null)
			{
				throw new NotImplementedException($"Instance {assetId} couldn't be found!");
			}

			// TODO : double check this is always self. might be local as well????
			PopToSelf(instance, varName, value);
		}
	}

	public static void PopToArgument(int index, object? value)
	{
		var args = (object?[])CurrentCall.Locals["arguments"].Conv<IList>();

		if (index >= args.Length)
		{
			Array.Resize(ref args, index + 1);
		}
		
		args[index] = value;
		CurrentCall.Locals["arguments"] = args;
	}

	public static void PopToOther(string varName, object? value)
	{
		PopToSelf(Other.Self, varName, value);
	}

	public static void PopToBuiltIn(string varName, object? value)
	{
		VariableResolver.BuiltInVariables[varName].setter!(value);
	}

	public static (ExecutionResult, object?) DoPop(VMCodeInstruction instruction)
	{
		if (instruction.TypeOne == VMType.e)
		{
			// weird swap thingy
			throw new NotImplementedException();
		}

		GetVariableInfo(instruction.StringData, out string variableName, out VariableType variableType, out VariablePrefix variablePrefix, out int assetId);

		if (variablePrefix == VariablePrefix.None)
		{
			// we're just popping to a normal variable. thank god.
			var dataPopped = Self.Stack.Pop(instruction.TypeTwo);

			if (variableType == VariableType.Global)
			{
				PopToGlobal(variableName, dataPopped);
				return (ExecutionResult.Success, null);
			}
			else if (variableType == VariableType.Local)
			{
				PopToLocal(variableName, dataPopped);
				return (ExecutionResult.Success, null);
			}
			else if (variableType == VariableType.Self)
			{
				PopToSelf(Self.Self, variableName, dataPopped);
				return (ExecutionResult.Success, null);
			}
			else if (variableType == VariableType.Index)
			{
				PopToIndex(assetId, variableName, dataPopped);
				return (ExecutionResult.Success, null);
			}
			else if (variableType == VariableType.Argument)
			{
				var strIndex = variableName[8..]; // skip "argument"
				var index = int.Parse(strIndex);
				PopToArgument(index, dataPopped);
				return (ExecutionResult.Success, null);
			}
			else if (variableType == VariableType.BuiltIn)
			{
				PopToBuiltIn(variableName, dataPopped);
				return (ExecutionResult.Success, null);
			}
			else if (variableType == VariableType.Other)
			{
				PopToOther(variableName, dataPopped);
				return (ExecutionResult.Success, null);
			}
		}
		else if (variablePrefix == VariablePrefix.Array || variablePrefix == VariablePrefix.ArrayPopAF || variablePrefix == VariablePrefix.ArrayPushAF)
		{
			// pop appears to not support ArrayPopAF or ArrayPushAF

			if (variablePrefix == VariablePrefix.Array)
			{
				int index;
				int instanceId;
				object? value;
				if (instruction.TypeOne == VMType.v)
				{
					index = Self.Stack.Pop(VMType.i).Conv<int>();
					instanceId = Self.Stack.Pop(VMType.i).Conv<int>();
					if (instanceId == GMConstants.stacktop)
					{
						instanceId = Self.Stack.Pop(VMType.v).Conv<int>();
					}
					value = Self.Stack.Pop(instruction.TypeTwo);
				}
				else
				{
					value = Self.Stack.Pop(instruction.TypeTwo);
					index = Self.Stack.Pop(VMType.i).Conv<int>();
					instanceId = Self.Stack.Pop(VMType.i).Conv<int>();
					if (instanceId == GMConstants.stacktop)
					{
						instanceId = Self.Stack.Pop(VMType.v).Conv<int>();
					}
				}

				if (variableType == VariableType.Self)
				{
					if (instanceId == GMConstants.global)
					{
						PopToGlobalArray(variableName, index, value);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.local)
					{
						PopToLocalArray(variableName, index, value);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.self)
					{
						PopToSelfArray(Self.Self, variableName, index, value);
						return (ExecutionResult.Success, null);
					}
					else
					{
						if (instanceId < GMConstants.FIRST_INSTANCE_ID)
						{
							// asset id
							var gm = InstanceManager.FindByAssetId(instanceId).MinBy(x => x.instanceId)!;
							PopToSelfArray(gm, variableName, index, value);
							return (ExecutionResult.Success, null);
						}
						else
						{
							// instance id
							var gm = InstanceManager.FindByInstanceId(instanceId)!;
							PopToSelfArray(gm, variableName, index, value);
							return (ExecutionResult.Success, null);
						}
					}
				}
				else if (variableType == VariableType.Global)
				{
					if (instanceId == GMConstants.global)
					{
						PopToGlobalArray(variableName, index, value);
						return (ExecutionResult.Success, null);
					}
				}
				else if (variableType == VariableType.Local)
				{
					if (instanceId == GMConstants.local)
					{
						PopToLocalArray(variableName, index, value);
						return (ExecutionResult.Success, null);
					}
				}

				return (ExecutionResult.Failed, $"Don't know how to execute {instruction.Raw} (index={index}, instanceId={instanceId}, value={value})");
			}
		}
		else if (variablePrefix == VariablePrefix.Stacktop)
		{
			// TODO : Check if 'self' is the only context where [stacktop] is used.
			// TODO : clean this shit up lol

			if (variableType == VariableType.Self)
			{
				int id = 0;
				object? value = null;

				if (instruction.TypeOne == VMType.i)
				{
					value = Self.Stack.Pop(instruction.TypeTwo);

					id = Self.Stack.Pop(VMType.i).Conv<int>();
					if (id == GMConstants.stacktop)
					{
						var popped = Self.Stack.Pop(VMType.v);

						if (popped is GMLObject gmlo)
						{
							PopToSelf(gmlo, variableName, value);
							return (ExecutionResult.Success, null);
						}
						else
						{
							id = popped.Conv<int>();
						}
					}
				}
				else
				{
					id = Self.Stack.Pop(VMType.i).Conv<int>();
					if (id == GMConstants.stacktop)
					{
						var popped = Self.Stack.Pop(VMType.v);

						if (popped is GMLObject gmlo)
						{
							value = Self.Stack.Pop(instruction.TypeTwo);
							PopToSelf(gmlo, variableName, value);
							return (ExecutionResult.Success, null);
						}
						else
						{
							id = popped.Conv<int>();
						}
					}

					value = Self.Stack.Pop(instruction.TypeTwo);
				}

				if (id == GMConstants.global)
				{
					PopToGlobal(variableName, value);
					return (ExecutionResult.Success, null);
				}
				else if (id == GMConstants.self)
				{
					if (Self.Self == null)
					{
						// for global scripts
						PopToGlobal(variableName, value);
					}
					else
					{
						PopToSelf(Self.Self, variableName, value);
					}

					return (ExecutionResult.Success, null);
				}
				else if (id == GMConstants.noone)
				{
					// uh what the fuck
					DebugLog.LogWarning($"Tried to pop {value} into {variableName} on no object???");
					return (ExecutionResult.Success, null);
				}

				PopToIndex(id, variableName, value);
				return (ExecutionResult.Success, null);
			}
		}

		return (ExecutionResult.Failed, $"Don't know how to execute {instruction.Raw}");
	}
}

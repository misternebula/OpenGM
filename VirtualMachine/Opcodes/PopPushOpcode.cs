namespace DELTARUNITYStandalone.VirtualMachine;

public static partial class VMExecutor
{
	private static void GetVariableInfo(string instructionStringData, out string variableName, out VariableType variableType, out VariablePrefix prefix, out int assetIndex)
	{
		variableName = instructionStringData;
		prefix = VariablePrefix.None;

		var indexingArray = variableName.StartsWith("[array]");
		if (indexingArray)
		{
			prefix = VariablePrefix.Array;
			variableName = variableName[7..]; // skip [array]
		}

		var stackTop = variableName.StartsWith("[stacktop]");
		if (stackTop)
		{
			prefix = VariablePrefix.Stacktop;
			variableName = variableName[10..]; // skip [stacktop]
		}

		variableType = VariableType.None;

		assetIndex = -1;
		var split = variableName.Split('.');
		var context = split[0];
		variableName = split[1];

		if (context == "global")
		{
			variableType = VariableType.Global;
		}
		else if (context == "local")
		{
			variableType = VariableType.Local;
		}
		else if (context == "self")
		{
			variableType = VariableType.Self;
		}
		else if (context == "other")
		{
			variableType = VariableType.Other;
		}
		else if (int.TryParse(context, out var index))
		{
			variableType = VariableType.Index;
			assetIndex = index;
		}
		else
		{
			throw new NotImplementedException($"Unknown variable type : {context}");
		}
	}

	public static (ExecutionResult, object) PUSH(VMScriptInstruction instruction)
	{
		switch (instruction.TypeOne)
		{
			case VMType.i:
				Ctx.Stack.Push(instruction.IntData);
				break;
			case VMType.l:
				Ctx.Stack.Push(instruction.LongData);
				break;
			case VMType.v:
				// TODO: [stacktop] and [array] should use data stack (array might not always tho?)
				// TODO: is self the only thing to use [stacktop]?

				GetVariableInfo(instruction.StringData, out var variableName, out var variableType, out var prefix, out var assetIndex);

				if (variableType == VariableType.Global)
				{
					if (prefix == VariablePrefix.Array)
					{
						var index = Conv<int>(Ctx.Stack.Pop());
						var instanceId = Conv<int>(Ctx.Stack.Pop());

						Ctx.Stack.Push(VariableResolver.GetGlobalArrayIndex(variableName, index));
						return (ExecutionResult.Success, null);
					}
					else
					{
						Ctx.Stack.Push(VariableResolver.GetGlobalVariable(variableName));
						return (ExecutionResult.Success, null);
					}
				}
				else if (variableType == VariableType.Local)
				{
					if (prefix == VariablePrefix.Array)
					{
						var index = Conv<int>(Ctx.Stack.Pop());
						var instanceId = Conv<int>(Ctx.Stack.Pop());

						Ctx.Stack.Push(VariableResolver.ArrayGet(index, () => (List<object>)Ctx.Locals[variableName]));
						return (ExecutionResult.Success, null);
					}
					else
					{
						Ctx.Stack.Push(Ctx.Locals[variableName]);
						return (ExecutionResult.Success, null);
					}
				}
				else if (variableType == VariableType.Self)
				{
					if (prefix == VariablePrefix.Array)
					{
						var index = Conv<int>(Ctx.Stack.Pop());
						var instanceId = Conv<int>(Ctx.Stack.Pop());

						if (instanceId == GMConstants.self)
						{
							if (variableName == "alarm")
							{
								Ctx.Stack.Push(Ctx.Self.alarm[index]);
								return (ExecutionResult.Success, null);
							}
							else
							{
								Ctx.Stack.Push(VariableResolver.ArrayGet(index,
									() => (List<object>)VariableResolver.GetSelfVariable(Ctx.Self, Ctx.Locals, variableName)));
								return (ExecutionResult.Success, null);
							}
						}
						else
						{
							var instance = InstanceManager.FindByInstanceId(instanceId);

							if (variableName == "alarm")
							{
								Ctx.Stack.Push(instance.alarm[index]);
								return (ExecutionResult.Success, null);
							}
							else
							{
								Ctx.Stack.Push(VariableResolver.ArrayGet(index,
									() => (List<object>)VariableResolver.GetSelfVariable(instance, Ctx.Locals, variableName)));
								return (ExecutionResult.Success, null);
							}
						}
					}
					else if (prefix == VariablePrefix.Stacktop)
					{
						var stackTopValue = Conv<int>(Ctx.Stack.Pop());

						GamemakerObject instance = null;
						if (stackTopValue < GMConstants.FIRST_INSTANCE_ID)
						{
							instance = InstanceManager.FindByAssetId(stackTopValue).FirstOrDefault();
						}
						else
						{
							instance = InstanceManager.FindByInstanceId(stackTopValue);
						}

						Ctx.Stack.Push(VariableResolver.GetSelfVariable(instance, Ctx.Locals, variableName));
						return (ExecutionResult.Success, null);
					}
					else
					{
						Ctx.Stack.Push(VariableResolver.GetSelfVariable(Ctx.Self, Ctx.Locals, variableName));
						return (ExecutionResult.Success, null);
					}
				}
				else if (variableType == VariableType.Index)
				{
					// TODO : GM probably gets the "first" instance by lowest instance id or something.
					var firstInstance = InstanceManager.FindByAssetId(assetIndex).First();

					if (prefix == VariablePrefix.Array)
					{
						// TODO : work out how this works
						return (ExecutionResult.Failed, $"Don't know how to push arrayed asset index variable {variableName}");
					}
					else
					{
						Ctx.Stack.Push(VariableResolver.GetSelfVariable(firstInstance, Ctx.Locals, variableName));
						return (ExecutionResult.Success, null);
					}
				}
				else if (variableType == VariableType.Other)
				{
					var temp = EnvironmentStack.Pop();
					var instance = EnvironmentStack.Peek();
					EnvironmentStack.Push(temp);

					if (prefix == VariablePrefix.Array)
					{

					}
					else
					{
						Ctx.Stack.Push(VariableResolver.GetSelfVariable(instance.Self, Ctx.Locals, variableName));
						return (ExecutionResult.Success, null);
					}
				}

				DebugLog.LogError($"Don't know how to push variable! raw:{instruction.StringData} variableType:{variableType} prefix:{prefix}");

				DebugLog.Log("::Stack::");
				foreach (var item in Ctx.Stack)
				{
					DebugLog.Log(item);
				}

				DebugLog.Log("::Environment Stack::");
				foreach (var item in EnvironmentStack)
				{
					DebugLog.Log(item);
				}

				return (ExecutionResult.Failed, null);

				break;
			case VMType.b:
				Ctx.Stack.Push(instruction.BoolData);
				break;
			case VMType.d:
				Ctx.Stack.Push(instruction.DoubleData);
				break;
			case VMType.e:
				// i think this is just an int always???
				Ctx.Stack.Push(instruction.IntData);
				break;
			case VMType.s:
				Ctx.Stack.Push(instruction.StringData);
				break;
			case VMType.None:
			default:
				throw new ArgumentOutOfRangeException();
		}

		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object) POP(VMScriptInstruction instruction)
	{
		// TODO: [stacktop] and [array] should use data stack (array might not always tho?)
		// TODO: is self the only thing to use [stacktop]?

		GetVariableInfo(instruction.StringData, out var variableName, out var variableType, out var prefix, out var assetIndex);

		if (variableType == VariableType.Global)
		{
			if (prefix == VariablePrefix.Array)
			{
				int index = 0;
				int instanceId = 0;
				object value = null;

				if (instruction.TypeOne != VMType.i && instruction.TypeOne != VMType.v)
				{
					// uhhhhhhhh
					return (ExecutionResult.Failed, $"POP : not i.X or v.X - {instruction.Raw}");
				}

				if (instruction.TypeOne == VMType.i)
				{
					value = Ctx.Stack.Pop();
					index = Conv<int>(Ctx.Stack.Pop());
					instanceId = Conv<int>(Ctx.Stack.Pop());
				}
				else
				{
					index = Conv<int>(Ctx.Stack.Pop());
					instanceId = Conv<int>(Ctx.Stack.Pop());
					value = Ctx.Stack.Pop();
				}

				VariableResolver.SetGlobalArrayIndex(variableName, index, value);
				return (ExecutionResult.Success, null);
			}
			else
			{
				var value = Ctx.Stack.Pop();
				VariableResolver.SetGlobalVariable(variableName, value);
				return (ExecutionResult.Success, null);
			}
		}
		else if (variableType == VariableType.Local)
		{
			if (prefix == VariablePrefix.Array)
			{
				int index = 0;
				int instanceId = 0;
				object value = null;

				if (instruction.TypeOne == VMType.i)
				{
					value = Ctx.Stack.Pop();
					index = Conv<int>(Ctx.Stack.Pop());
					instanceId = Conv<int>(Ctx.Stack.Pop());
				}
				else
				{
					index = Conv<int>(Ctx.Stack.Pop());
					instanceId = Conv<int>(Ctx.Stack.Pop());
					value = Ctx.Stack.Pop();
				}

				VariableResolver.ArraySet(index, value,
					() => Ctx.Locals[variableName],
					list => Ctx.Locals[variableName] = list,
					() => Ctx.Locals.ContainsKey(variableName));
				return (ExecutionResult.Success, null);
			}
			else
			{
				var value = Ctx.Stack.Pop();
				Ctx.Locals[variableName] = value;
				return (ExecutionResult.Success, null);
			}
		}
		else if (variableType == VariableType.Self)
		{
			if (prefix == VariablePrefix.Array)
			{
				int index = 0;
				int instanceId = 0;
				object value = null;

				if (instruction.TypeOne == VMType.i)
				{
					value = Ctx.Stack.Pop();
					index = Conv<int>(Ctx.Stack.Pop());
					instanceId = Conv<int>(Ctx.Stack.Pop());
				}
				else
				{
					index = Conv<int>(Ctx.Stack.Pop());
					instanceId = Conv<int>(Ctx.Stack.Pop());
					value = Ctx.Stack.Pop();
				}

				if (instanceId == GMConstants.self)
				{
					if (variableName == "alarm")
					{
						Ctx.Self.alarm[index] = Conv<int>(value);
						return (ExecutionResult.Success, null);
					}
					else
					{
						VariableResolver.ArraySet(index, value,
							() => VariableResolver.GetSelfVariable(Ctx.Self, Ctx.Locals, variableName),
							list => VariableResolver.SetSelfVariable(Ctx.Self, variableName, list),
							() => VariableResolver.ContainsSelfVariable(Ctx.Self, Ctx.Locals, variableName));
						return (ExecutionResult.Success, null);
					}
				}
				else
				{
					GamemakerObject instance;

					if (instanceId < GMConstants.FIRST_INSTANCE_ID)
					{
						// TODO : GM probably gets the "first" instance by lowest instance id or something.
						instance = InstanceManager.FindByAssetId(instanceId).First();
					}
					else
					{
						instance = InstanceManager.FindByInstanceId(instanceId);
					}

					if (variableName == "alarm")
					{
						instance.alarm[index] = Conv<int>(value);
						return (ExecutionResult.Success, null);
					}
					else
					{
						VariableResolver.ArraySet(index, value,
							() => VariableResolver.GetSelfVariable(instance, Ctx.Locals, variableName),
							list => VariableResolver.SetSelfVariable(instance, variableName, list),
							() => VariableResolver.ContainsSelfVariable(instance, Ctx.Locals, variableName));
						return (ExecutionResult.Success, null);
					}
				}
			}
			else if (prefix == VariablePrefix.Stacktop)
			{
				int instanceId = 0;
				object value = null;

				if (instruction.TypeOne == VMType.i)
				{
					value = Ctx.Stack.Pop();
					instanceId = Conv<int>(Ctx.Stack.Pop());
				}
				else
				{
					instanceId = Conv<int>(Ctx.Stack.Pop());
					value = Ctx.Stack.Pop();
				}

				var instance = InstanceManager.FindByInstanceId(instanceId);
				VariableResolver.SetSelfVariable(instance, variableName, value);
				return (ExecutionResult.Success, null);
			}
			else
			{
				var value = Ctx.Stack.Pop();
				VariableResolver.SetSelfVariable(Ctx.Self, variableName, value);
				return (ExecutionResult.Success, null);
			}
		}
		else if (variableType == VariableType.Index)
		{
			// TODO : GM probably gets the "first" instance by lowest instance id or something.
			var firstInstance = InstanceManager.FindByAssetId(assetIndex).First();

			if (prefix == VariablePrefix.Array)
			{
				// TODO : work out how this works
				return (ExecutionResult.Failed, $"Don't know how to pop arrayed asset index variable {variableName}");
			}
			else
			{
				var value = Ctx.Stack.Pop();
				VariableResolver.SetSelfVariable(firstInstance, variableName, value);
				return (ExecutionResult.Success, null);
			}
		}
		else if (variableType == VariableType.Other)
		{
			var temp = EnvironmentStack.Pop();
			var instance = EnvironmentStack.Peek();
			EnvironmentStack.Push(temp);

			if (prefix == VariablePrefix.Array)
			{

			}
			else
			{
				var value = Ctx.Stack.Pop();
				VariableResolver.SetSelfVariable(instance.Self, variableName, value);
				return (ExecutionResult.Success, null);
			}
		}

		DebugLog.LogError($"Don't know how to pop to variable! raw:{instruction.StringData} variableType:{variableType} prefix:{prefix}");

		DebugLog.Log("::Stack::");
		foreach (var item in Ctx.Stack)
		{
			DebugLog.Log(item);
		}

		DebugLog.Log("::Environment Stack::");
		foreach (var item in EnvironmentStack)
		{
			DebugLog.Log(item);
		}

		return (ExecutionResult.Failed, null);
	}
}

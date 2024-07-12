using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.VirtualMachine;

public static partial class VMExecutor
{
	public static void GetVariableInfo(string instructionStringData, out string variableName, out VariableType variableType, out VariablePrefix prefix, out int assetIndex)
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

		var arraypopaf = variableName.StartsWith("[arraypopaf]");
		if (arraypopaf)
		{
			prefix = VariablePrefix.ArrayPopAF;
			variableName = variableName[12..]; // skip [arraypopaf]
		}

		var arraypushaf = variableName.StartsWith("[arraypushaf]");
		if (arraypushaf)
		{
			prefix = VariablePrefix.ArrayPushAF;
			variableName = variableName[13..]; // skip [arraypushaf]
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
		else if (context == "builtin")
		{
			variableType = VariableType.BuiltIn;
		}
		else if (context == "arg")
		{
			variableType = VariableType.Argument;
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

	public static void PushGlobal(string varName)
	{
		Ctx.Stack.Push(VariableResolver.GlobalVariables[varName], VMType.v);
	}

	public static void PushGlobalArrayIndex(string varName, int index)
	{
		var array = VariableResolver.GlobalVariables[varName].Conv<IList>();
		Ctx.Stack.Push(array[index], VMType.v);
	}

	public static void PushLocalArrayIndex(string varName, int index)
	{
		var array = Ctx.Locals[varName].Conv<IList>();
		Ctx.Stack.Push(array[index], VMType.v);
	}

	public static void PushLocal(string varName)
	{
		Ctx.Stack.Push(Ctx.Locals[varName], VMType.v);
	}

	public static void PushBuiltin(string varName)
	{
		var value = VariableResolver.BuiltInVariables[varName].getter();
		Ctx.Stack.Push(value, VMType.v);
	}

	public static void PushSelf(GamemakerObject self, string varName)
	{
		if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var gettersetter))
		{
			Ctx.Stack.Push(gettersetter.getter(self), VMType.v);
		}
		else
		{
			Ctx.Stack.Push(self.SelfVariables[varName], VMType.v);
		}
	}

	public static void PushSelfArrayIndex(GamemakerObject self, string varName, int index)
	{
		if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var gettersetter))
		{
			var array = gettersetter.getter(self).Conv<IList>();
			Ctx.Stack.Push(array[index], VMType.v);
		}
		else
		{
			var array = self.SelfVariables[varName].Conv<IList>();
			Ctx.Stack.Push(array[index], VMType.v);
		}
	}

	public static void PushArgument(int index)
	{
		var arguments = Ctx.Locals["arguments"].Conv<IList>();
		Ctx.Stack.Push(arguments[index], VMType.v);
	}

	public static void PushIndex(int assetId, string varName)
	{
		if (assetId <= GMConstants.FIRST_INSTANCE_ID)
		{
			// Asset Id

			var asset = InstanceManager.FindByAssetId(assetId).MinBy(x => x.instanceId)!;
			PushSelf(asset, varName);
		}
		else
		{
			// Instance Id
			var asset = InstanceManager.FindByInstanceId(assetId)!; // lets hope its not null
			PushSelf(asset, varName);
		}
	}

	public static (ExecutionResult, object?) DoPush(VMScriptInstruction instruction)
	{
		switch (instruction.TypeOne)
		{
			case VMType.i:
				Ctx.Stack.Push(instruction.IntData, VMType.i);
				return (ExecutionResult.Success, null);
			case VMType.e:
				Ctx.Stack.Push(instruction.ShortData, VMType.e);
				return (ExecutionResult.Success, null);
			case VMType.l:
				Ctx.Stack.Push(instruction.LongData, VMType.l);
				return (ExecutionResult.Success, null);
			case VMType.b:
				Ctx.Stack.Push(instruction.BoolData, VMType.b);
				return (ExecutionResult.Success, null);
			case VMType.d:
				Ctx.Stack.Push(instruction.DoubleData, VMType.d);
				return (ExecutionResult.Success, null);
			case VMType.s:
				Ctx.Stack.Push(instruction.StringData, VMType.s);
				return (ExecutionResult.Success, null);
			case VMType.v:
				return DoPushV(instruction);
		}

		return (ExecutionResult.Failed, $"Don't know how to push {instruction.Raw}");
	}

	public static (ExecutionResult, object?) DoPushV(VMScriptInstruction instruction)
	{
		GetVariableInfo(instruction.StringData, out string variableName, out VariableType variableType, out VariablePrefix variablePrefix, out int assetId);

		if (variablePrefix == VariablePrefix.None)
		{
			if (variableType == VariableType.Global)
			{
				PushGlobal(variableName);
				return (ExecutionResult.Success, null);
			}
			else if (variableType == VariableType.Local)
			{
				PushLocal(variableName);
				return (ExecutionResult.Success, null);
			}
			else if (variableType == VariableType.BuiltIn)
			{
				PushBuiltin(variableName);
				return (ExecutionResult.Success, null);
			}
			else if (variableType == VariableType.Self)
			{
				PushSelf(Ctx.Self, variableName);
				return (ExecutionResult.Success, null);
			}
			else if (variableType == VariableType.Argument)
			{
				var strIndex = variableName[8..]; // skip "argument"
				var index = int.Parse(strIndex);
				PushArgument(index);
				return (ExecutionResult.Success, null);
			}
			else if (variableType == VariableType.Index)
			{
				PushIndex(assetId, variableName);
				return (ExecutionResult.Success, null);
			}
		}
		else if (variablePrefix == VariablePrefix.Array || variablePrefix == VariablePrefix.ArrayPopAF || variablePrefix == VariablePrefix.ArrayPushAF)
		{
			if (variablePrefix == VariablePrefix.Array)
			{
				if (variableType == VariableType.Self)
				{
					var index = Ctx.Stack.Pop(VMType.i).Conv<int>();
					var instanceId = Ctx.Stack.Pop(VMType.i).Conv<int>();

					if (instanceId == GMConstants.global)
					{
						PushGlobalArrayIndex(variableName, index);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.local)
					{
						PushLocalArrayIndex(variableName, index);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.argument)
					{
						PushArgument(index);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.self)
					{
						PushSelfArrayIndex(Ctx.Self, variableName, index);
						return (ExecutionResult.Success, null);
					}

					return (ExecutionResult.Failed, $"Don't know how to push {instruction.Raw} index:{index} instanceid:{instanceId}");
				}
			}
			else if (variablePrefix == VariablePrefix.ArrayPopAF)
			{
				if (variableType == VariableType.Self)
				{
					var index = Ctx.Stack.Pop(VMType.i).Conv<int>();
					var instanceId = Ctx.Stack.Pop(VMType.i).Conv<int>();

					// TODO: make into methods and move out duplicated code
					if (instanceId == GMConstants.global)
					{
						VariableResolver.ArraySet(index, null,
							() => VariableResolver.GlobalVariables.TryGetValue(variableName, out var array) ? array as IList : null,
							array => VariableResolver.GlobalVariables[variableName] = array);

						var array = VariableResolver.GlobalVariables[variableName].Conv<IList>();

						var arrayReference = new ArrayReference
						{
							Name = variableName,
							Value = array,
							IsGlobal = true
						};

						Ctx.Stack.Push(arrayReference, VMType.v);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.local)
					{
						VariableResolver.ArraySet(index, null,
							() => Ctx.Locals.TryGetValue(variableName, out var array) ? array as IList : null,
							array => Ctx.Locals[variableName] = array);

						var array = Ctx.Locals[variableName].Conv<IList>();

						var arrayReference = new ArrayReference
						{
							Name = variableName,
							Value = array,
							IsLocal = true
						};

						Ctx.Stack.Push(arrayReference, VMType.v);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.self)
					{
						// TODO: check builtin self var
						VariableResolver.ArraySet(index, null,
							() => Ctx.Self.SelfVariables.TryGetValue(variableName, out var array) ? array as IList : null,
							array => Ctx.Self.SelfVariables[variableName] = array);

						var array = Ctx.Self.SelfVariables[variableName].Conv<IList>();

						var arrayReference = new ArrayReference
						{
							Name = variableName,
							Value = array,
							Instance = Ctx.Self
						};

						Ctx.Stack.Push(arrayReference, VMType.v);
						return (ExecutionResult.Success, null);
					}
				}
			}
			else if (variablePrefix == VariablePrefix.ArrayPushAF)
			{
				if (variableType == VariableType.Self)
				{
					var index = Ctx.Stack.Pop(VMType.i).Conv<int>(); // BUG: this is unused??? something is definitely wrong here
					var instanceId = Ctx.Stack.Pop(VMType.i).Conv<int>();

					if (instanceId == GMConstants.global)
					{
						var array = VariableResolver.GlobalVariables[variableName].Conv<IList>();

						var arrayReference = new ArrayReference
						{
							Name = variableName,
							Value = array,
							IsGlobal = true
						};

						Ctx.Stack.Push(arrayReference, VMType.v);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.local)
					{
						var array = Ctx.Locals[variableName].Conv<IList>();

						var arrayReference = new ArrayReference
						{
							Name = variableName,
							Value = array,
							IsLocal = true
						};

						Ctx.Stack.Push(arrayReference, VMType.v);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.self)
					{
						// TODO: check builtin self var
						var array = Ctx.Self.SelfVariables[variableName].Conv<IList>();

						var arrayReference = new ArrayReference
						{
							Name = variableName,
							Value = array,
							Instance = Ctx.Self
						};

						Ctx.Stack.Push(arrayReference, VMType.v);
						return (ExecutionResult.Success, null);
					}
				}
			}
		}
		else if (variablePrefix == VariablePrefix.Stacktop)
		{
			if (variableType == VariableType.Self)
			{
				var id = Ctx.Stack.Pop(VMType.i).Conv<int>();

				if (id == GMConstants.stacktop)
				{
					id = Ctx.Stack.Pop(VMType.v).Conv<int>();
				}

				PushIndex(id, variableName);
				return (ExecutionResult.Success, null);
			}
		}

		return (ExecutionResult.Failed, $"Don't know how to push {instruction.Raw}");
	}
}

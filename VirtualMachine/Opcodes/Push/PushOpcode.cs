using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.VirtualMachine;

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
		Ctx.Stack.Push(VariableResolver.GlobalVariables[varName]);
	}

	public static void PushGlobalArrayIndex(string varName, int index)
	{
		var array = (List<RValue>)VariableResolver.GlobalVariables[varName].Value!;
		Ctx.Stack.Push(array[index]);
	}

	public static void PushLocalArrayIndex(string varName, int index)
	{
		var array = (List<RValue>)Ctx.Locals[varName].Value!;
		Ctx.Stack.Push(array[index]);
	}

	public static void PushLocal(string varName)
	{
		Ctx.Stack.Push(Ctx.Locals[varName]);
	}

	public static void PushBuiltin(string varName)
	{
		var value = VariableResolver.BuiltInVariables[varName].getter(null);

		if (value is RValue r)
		{
			Ctx.Stack.Push(r);
		}
		else
		{
			Ctx.Stack.Push(new RValue(value));
		}
	}

	public static void PushSelf(GamemakerObject self, string varName)
	{
		Ctx.Stack.Push(self.SelfVariables[varName]);
	}

	public static void PushSelfArrayIndex(GamemakerObject self, string varName, int index)
	{
		var array = (List<RValue>)self.SelfVariables[varName].Value!;
		Ctx.Stack.Push(array[index]);
	}

	public static void PushArgument(int index)
	{
		var arguments = (List<RValue>)Ctx.Locals["arguments"].Value!;
		Ctx.Stack.Push(arguments[index]);
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
			var asset = InstanceManager.FindByInstanceId(assetId);
			PushSelf(asset, varName);
		}
	}

	public static (ExecutionResult, object?) DoPush(VMScriptInstruction instruction)
	{
		switch (instruction.TypeOne)
		{
			case VMType.i:
			case VMType.e:
				Ctx.Stack.Push(instruction.IntData);
				return (ExecutionResult.Success, null);
			case VMType.l:
				Ctx.Stack.Push(instruction.LongData);
				return (ExecutionResult.Success, null);
			case VMType.b:
				Ctx.Stack.Push(instruction.BoolData);
				return (ExecutionResult.Success, null);
			case VMType.d:
				Ctx.Stack.Push(instruction.DoubleData);
				return (ExecutionResult.Success, null);
			case VMType.s:
				Ctx.Stack.Push(instruction.StringData);
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
					var index = Ctx.Stack.Pop<int>(VMType.i);
					var instanceId = Ctx.Stack.Pop<int>(VMType.i);

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
		}

		return (ExecutionResult.Failed, $"Don't know how to push {instruction.Raw}");
	}
}

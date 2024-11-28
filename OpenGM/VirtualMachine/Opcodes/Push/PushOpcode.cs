using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenGM.IO;

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

		if (split.Length == 3)
		{
			// weird thing
			var instanceId = GMConstants.FIRST_INSTANCE_ID + int.Parse(split[0]);
			variableName = split[2];
			if (split[1] != "[instance]self")
			{
				throw new NotImplementedException();
			}

			assetIndex = instanceId;
			variableType = VariableType.Index;
			return;
		}

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
		var array = CurrentCall.Locals[varName].Conv<IList>();
		Ctx.Stack.Push(array[index], VMType.v);
	}

	public static void PushLocal(string varName)
	{
		Ctx.Stack.Push(CurrentCall.Locals[varName], VMType.v);
	}

	public static void PushBuiltin(string varName)
	{
		var value = VariableResolver.BuiltInVariables[varName].getter();
		Ctx.Stack.Push(value, VMType.v);
	}

	public static void PushBuiltinArrayIndex(string varName, int index)
	{
		var array = VariableResolver.BuiltInVariables[varName].getter().Conv<IList>();
		Ctx.Stack.Push(array[index], VMType.v);
	}

	public static void PushSelf(IStackContextSelf self, string varName)
	{
		if (VariableResolver.BuiltInVariables.TryGetValue(varName, out var builtin_gettersetter))
		{
			Ctx.Stack.Push(builtin_gettersetter.getter(), VMType.v);
		}
		else if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var selfbuiltin_gettersetter) && self is GamemakerObject gm)
		{
			Ctx.Stack.Push(selfbuiltin_gettersetter.getter(gm), VMType.v);
		}
		else
		{
			if (self.SelfVariables.ContainsKey(varName))
			{
				Ctx.Stack.Push(self.SelfVariables[varName], VMType.v);
			}
			else
			{
				if (self is GamemakerObject gmo)
				{
					DebugLog.LogError($"Variable {varName} doesn't exist in {gmo.instanceId} {gmo.Definition.Name}, pushing undefined.");
				}
				else
				{
					DebugLog.LogError($"Variable {varName} doesn't exist in non-GMO self, pushing undefined.");
				}

				DebugLog.LogError($"--Stacktrace--");
				foreach (var item in CallStack)
				{
					DebugLog.LogError($" - {item.Code.Name}");
				}

				self.SelfVariables[varName] = null;
				Ctx.Stack.Push(self.SelfVariables[varName], VMType.v);
			}
		}
	}

	public static void PushSelfArrayIndex(IStackContextSelf self, string varName, int index)
	{
		if (VariableResolver.BuiltInVariables.TryGetValue(varName, out var bi_gettersetter))
		{
			var array = bi_gettersetter.getter().Conv<IList>();
			Ctx.Stack.Push(array[index], VMType.v);
		}
		else if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var bis_gettersetter) && self is GamemakerObject gm)
		{
			var array = bis_gettersetter.getter(gm).Conv<IList>();
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
		var arguments = CurrentCall.Locals["arguments"].Conv<IList>();

		if (index >= arguments.Count)
		{
			// Scripts can be called with fewer than normal arguments.
			// They just get set to Undefined.
			Ctx.Stack.Push(null, VMType.v);
			return;
		}

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
			var asset = InstanceManager.FindByInstanceId(assetId);

			if (asset == null)
			{
				DebugLog.LogError($"Tried to push variable {varName} from instanceid {assetId}, which doesnt exist!!");
				Ctx.Stack.Push(null, VMType.v);
				return;
			}

			PushSelf(asset, varName);
		}
	}

	public static void PushOther(string varName)
	{
		var stackArray = EnvironmentStack.ToArray();

		if (stackArray.Contains(null))
		{
			// iterate backwards until we find the null value

			var i = 0;

			while (stackArray[i] != null)
			{
				i++;
			}

			// i now holds index of null value. next value is the one calling pushenv

			PushSelf(stackArray[i + 1].Self, varName);
		}
		else
		{
			PushSelf(stackArray[1].Self, varName);
		}
	}

	public static (ExecutionResult, object?) DoPush(VMCodeInstruction instruction)
	{
		switch (instruction.TypeOne)
		{
			case VMType.i:

				if (instruction.StringData != null)
				{
					if (AssetIndexManager.NameToIndex.ContainsKey(instruction.StringData))
					{
						Ctx.Stack.Push(AssetIndexManager.NameToIndex[instruction.StringData], VMType.i);
					}
					else if (instruction.StringData.StartsWith($"gml_Script_"))
					{
						Ctx.Stack.Push(ScriptResolver.ScriptFunctions.Keys.ToList().IndexOf(instruction.StringData), VMType.i);
					}
					else
					{
						throw new NotImplementedException();
					}
				}
				else
				{
					Ctx.Stack.Push(instruction.IntData, VMType.i);
				}
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

	public static (ExecutionResult, object?) DoPushV(VMCodeInstruction instruction)
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
			else if (variableType == VariableType.Other)
			{
				PushOther(variableName);
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

					if (instanceId == GMConstants.stacktop)
					{
						instanceId = Ctx.Stack.Pop(VMType.v).Conv<int>();
					}

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
					else if (instanceId == GMConstants.builtin)
					{
						PushBuiltinArrayIndex(variableName, index);
						return (ExecutionResult.Success, null);
					}
					else
					{
						if (instanceId < GMConstants.FIRST_INSTANCE_ID)
						{
							// asset id
							var self = InstanceManager.FindByAssetId(instanceId).MinBy(x => x.instanceId)!;
							PushSelfArrayIndex(self, variableName, index);
							return (ExecutionResult.Success, null);
						}
						else
						{
							// instance id
							var self = InstanceManager.FindByInstanceId(instanceId)!;
							PushSelfArrayIndex(self, variableName, index);
							return (ExecutionResult.Success, null);
						}
					}

					//return (ExecutionResult.Failed, $"Don't know how to push {instruction.Raw} index:{index} instanceid:{instanceId}");
				}
				else if (variableType == VariableType.Global)
				{
					var index = Ctx.Stack.Pop(VMType.i).Conv<int>();
					var instanceId = Ctx.Stack.Pop(VMType.i).Conv<int>();

					if (instanceId == GMConstants.global)
					{
						PushGlobalArrayIndex(variableName, index);
						return (ExecutionResult.Success, null);
					}
				}
				else if (variableType == VariableType.Local)
				{
					var index = Ctx.Stack.Pop(VMType.i).Conv<int>();
					var instanceId = Ctx.Stack.Pop(VMType.i).Conv<int>();

					if (instanceId == GMConstants.local)
					{
						PushLocalArrayIndex(variableName, index);
						return (ExecutionResult.Success, null);
					}
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
						VariableResolver.ArraySet(index, new List<object?>(),
							() => VariableResolver.GlobalVariables.TryGetValue(variableName, out var array) ? array as IList : null,
							array => VariableResolver.GlobalVariables[variableName] = array,
							onlyGrow: true);

						var array = VariableResolver.GlobalVariables[variableName].Conv<IList>();
						Ctx.Stack.Push(array[index], VMType.v);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.local)
					{
						VariableResolver.ArraySet(index, new List<object?>(),
							() => CurrentCall.Locals.TryGetValue(variableName, out var array) ? array as IList : null,
							array => CurrentCall.Locals[variableName] = array,
							onlyGrow: true);

						var array = CurrentCall.Locals[variableName].Conv<IList>();
						Ctx.Stack.Push(array[index], VMType.v);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.self)
					{
						// TODO: check builtin self var
						VariableResolver.ArraySet(index, new List<object?>(),
							() => Ctx.Self.SelfVariables.TryGetValue(variableName, out var array) ? array as IList : null,
							array => Ctx.Self.SelfVariables[variableName] = array,
							onlyGrow: true);

						var array = Ctx.Self.SelfVariables[variableName].Conv<IList>();
						Ctx.Stack.Push(array[index], VMType.v);
						return (ExecutionResult.Success, null);
					}
				}
			}
			else if (variablePrefix == VariablePrefix.ArrayPushAF)
			{
				if (variableType == VariableType.Self)
				{
					var index = Ctx.Stack.Pop(VMType.i).Conv<int>();
					var instanceId = Ctx.Stack.Pop(VMType.i).Conv<int>();

					if (instanceId == GMConstants.global)
					{
						var array = VariableResolver.GlobalVariables[variableName].Conv<IList>();
						Ctx.Stack.Push(array[index], VMType.v);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.local)
					{
						var array = CurrentCall.Locals[variableName].Conv<IList>();
						Ctx.Stack.Push(array[index], VMType.v);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.self)
					{
						// TODO: check builtin self var
						var array = Ctx.Self.SelfVariables[variableName].Conv<IList>();
						Ctx.Stack.Push(array[index], VMType.v);
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
					var popped = Ctx.Stack.Pop(VMType.v);

					if (popped is GMLObject gmlo)
					{
						PushSelf(gmlo, variableName);
						return (ExecutionResult.Success, null);
					}
					else
					{
						id = popped.Conv<int>();
					}
				}

				if (id == GMConstants.other)
				{
					PushOther(variableName);
					return (ExecutionResult.Success, null);
				}

				PushIndex(id, variableName);
				return (ExecutionResult.Success, null);
			}
		}

		return (ExecutionResult.Failed, $"Don't know how to push {instruction.Raw}");
	}
}

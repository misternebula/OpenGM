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
		else if (context == "stacktop")
		{
			variableType = VariableType.Stacktop;
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
		Self.Stack.Push(VariableResolver.GlobalVariables[varName], VMType.v);
	}

	public static void PushGlobalArrayIndex(string varName, int index)
	{
		var array = VariableResolver.GlobalVariables[varName].Conv<IList>();
		Self.Stack.Push(array[index], VMType.v);
	}

	public static void PushLocalArrayIndex(string varName, int index)
	{
		var array = CurrentCall.Locals[varName].Conv<IList>();
		Self.Stack.Push(array[index], VMType.v);
	}

	public static void PushLocal(string varName)
	{
		Self.Stack.Push(CurrentCall.Locals[varName], VMType.v);
	}

	public static void PushBuiltin(string varName)
	{
		if (VariableResolver.BuiltInVariables.ContainsKey(varName))
		{
			var value = VariableResolver.BuiltInVariables[varName].getter();
			Self.Stack.Push(value, VMType.v);
		}
		else if (VariableResolver.BuiltInSelfVariables.ContainsKey(varName))
		{
			var value = VariableResolver.BuiltInSelfVariables[varName].getter(Self.GMSelf);
			Self.Stack.Push(value, VMType.v);
		}
		else if (Self.Self.SelfVariables.ContainsKey(varName))
		{
			var value = Self.Self.SelfVariables[varName];
			Self.Stack.Push(value, VMType.v);
		}
		else
		{
			throw new NotImplementedException();
		}
	}

	public static void PushBuiltinArrayIndex(string varName, int index)
	{
		if (VariableResolver.BuiltInVariables.ContainsKey(varName))
		{
			var array = VariableResolver.BuiltInVariables[varName].getter().Conv<IList>();
			Self.Stack.Push(array[index], VMType.v);
		}
		else if (VariableResolver.BuiltInSelfVariables.ContainsKey(varName))
		{
			var array = VariableResolver.BuiltInSelfVariables[varName].getter(Self.GMSelf).Conv<IList>();
			Self.Stack.Push(array[index], VMType.v);
		}
		else
		{
			throw new NotImplementedException();
		}
	}

	public static void PushSelf(IStackContextSelf self, string varName)
	{
		if (self == null)
		{
			DebugLog.LogError($"Null self given to PushSelf varName:{varName} - {VMExecutor.CurrentInstruction!.Raw}");

			DebugLog.LogError($"--Stacktrace--");
			foreach (var item in CallStack)
			{
				DebugLog.LogError($" - {item.Code.Name}");
			}

			Self.Stack.Push(null, VMType.v);
			return;
		}

		if (VariableResolver.BuiltInVariables.TryGetValue(varName, out var builtin_gettersetter))
		{
			Self.Stack.Push(builtin_gettersetter.getter(), VMType.v);
		}
		else if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var selfbuiltin_gettersetter) && self is GamemakerObject gm)
		{
			Self.Stack.Push(selfbuiltin_gettersetter.getter(gm), VMType.v);
		}
		else
		{
			if (self.SelfVariables.ContainsKey(varName))
			{
				Self.Stack.Push(self.SelfVariables[varName], VMType.v);
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
				Self.Stack.Push(self.SelfVariables[varName], VMType.v);
			}
		}
	}

	public static void PushSelfArrayIndex(IStackContextSelf self, string varName, int index)
	{
		if (VariableResolver.BuiltInVariables.TryGetValue(varName, out var bi_gettersetter))
		{
			var array = bi_gettersetter.getter().Conv<IList>();
			Self.Stack.Push(array[index], VMType.v);
		}
		else if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var bis_gettersetter) && self is GamemakerObject gm)
		{
			var array = bis_gettersetter.getter(gm).Conv<IList>();
			Self.Stack.Push(array[index], VMType.v);
		}
		else
		{
			var array = self.SelfVariables[varName].Conv<IList>();
			Self.Stack.Push(array[index], VMType.v);
		}
	}

	public static void PushArgument(int index)
	{
		var arguments = CurrentCall.Locals["arguments"].Conv<IList>();

		if (index >= arguments.Count)
		{
			// Scripts can be called with fewer than normal arguments.
			// They just get set to Undefined.
			Self.Stack.Push(null, VMType.v);
			return;
		}

		Self.Stack.Push(arguments[index], VMType.v);
	}

	public static void PushIndex(int assetId, string varName)
	{
		if (assetId <= GMConstants.FIRST_INSTANCE_ID)
		{
			// Asset Id

			var asset = InstanceManager.FindByAssetId(assetId).MinBy(x => x.instanceId)!;

			if (asset == null)
			{
				DebugLog.LogError($"Couldn't find any instances of {AssetIndexManager.GetName(AssetType.objects, assetId)}!");
				Self.Stack.Push(null, VMType.v);
				return;
			}

			PushSelf(asset, varName);
		}
		else
		{
			// Instance Id
			var asset = InstanceManager.FindByInstanceId(assetId);

			if (asset == null)
			{
				DebugLog.LogError($"Tried to push variable {varName} from instanceid {assetId}, which doesnt exist!!");

				DebugLog.LogError($"--Stacktrace--");
				foreach (var item in CallStack)
				{
					DebugLog.LogError($" - {item.Code.Name}");
				}

				DebugLog.LogError(Environment.StackTrace);

				Self.Stack.Push(null, VMType.v);
				return;
			}

			PushSelf(asset, varName);
		}
	}

	public static void PushOther(string varName)
	{
		PushSelf(Other.Self, varName);
	}

	public static void PushStacktop(string varName)
	{
		var instanceId = Self.Stack.Pop(VMType.v).Conv<int>();
		PushIndex(instanceId, varName);
	}

	public static void PushArgumentArrayIndex(string varName, int index)
	{
		var argIndex = int.Parse(varName.Replace("argument", ""));

		var arguments = CurrentCall.Locals["arguments"].Conv<IList>();

		if (argIndex >= arguments.Count)
		{
			Self.Stack.Push(null, VMType.v);
			return;
		}

		var array = arguments[argIndex].Conv<IList>();

		Self.Stack.Push(array[index], VMType.v);
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
						Self.Stack.Push(AssetIndexManager.NameToIndex[instruction.StringData], VMType.i);
					}
					else if (instruction.StringData.StartsWith($"gml_Script_"))
					{
						var funcIndex = ScriptResolver.ScriptFunctions.Keys.ToList().IndexOf(instruction.StringData);
						Self.Stack.Push(funcIndex, VMType.i);
					}
					else
					{
						throw new NotImplementedException();
					}
				}
				else
				{
					Self.Stack.Push(instruction.IntData, VMType.i);
				}
				return (ExecutionResult.Success, null);
			case VMType.e:
				Self.Stack.Push(instruction.ShortData, VMType.e);
				return (ExecutionResult.Success, null);
			case VMType.l:
				Self.Stack.Push(instruction.LongData, VMType.l);
				return (ExecutionResult.Success, null);
			case VMType.b:
				Self.Stack.Push(instruction.BoolData, VMType.b);
				return (ExecutionResult.Success, null);
			case VMType.d:
				Self.Stack.Push(instruction.DoubleData, VMType.d);
				return (ExecutionResult.Success, null);
			case VMType.s:
				Self.Stack.Push(instruction.StringData, VMType.s);
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
				PushSelf(Self.Self, variableName);
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
			else if (variableType == VariableType.Stacktop)
			{
				PushStacktop(variableName);
				return (ExecutionResult.Success, null);
			}
		}
		else if (variablePrefix == VariablePrefix.Array || variablePrefix == VariablePrefix.ArrayPopAF || variablePrefix == VariablePrefix.ArrayPushAF)
		{
			if (variablePrefix == VariablePrefix.Array)
			{
				if (variableType == VariableType.Self)
				{
					var index = Self.Stack.Pop(VMType.i).Conv<int>();
					var instanceId = Self.Stack.Pop(VMType.i).Conv<int>();

					if (instanceId == GMConstants.stacktop)
					{
						instanceId = Self.Stack.Pop(VMType.v).Conv<int>();
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
						if (variableName == "argument")
						{
							PushArgument(index);
						}
						else
						{
							PushArgumentArrayIndex(variableName, index);
						}
						
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.self)
					{
						PushSelfArrayIndex(Self.Self, variableName, index);
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

							if (self == null)
							{
								DebugLog.LogError($"Couldn't find any instances of {AssetIndexManager.GetName(AssetType.objects, instanceId)}");
								Self.Stack.Push(null, VMType.v);
								return (ExecutionResult.Success, null);
							}

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
					var index = Self.Stack.Pop(VMType.i).Conv<int>();
					var instanceId = Self.Stack.Pop(VMType.i).Conv<int>();

					if (instanceId == GMConstants.global)
					{
						PushGlobalArrayIndex(variableName, index);
						return (ExecutionResult.Success, null);
					}
				}
				else if (variableType == VariableType.Local)
				{
					var index = Self.Stack.Pop(VMType.i).Conv<int>();
					var instanceId = Self.Stack.Pop(VMType.i).Conv<int>();

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
					var index = Self.Stack.Pop(VMType.i).Conv<int>();
					var instanceId = Self.Stack.Pop(VMType.i).Conv<int>();

					// TODO: make into methods and move out duplicated code
					if (instanceId == GMConstants.global)
					{
						VariableResolver.ArraySet(index, new List<object?>(),
							() => VariableResolver.GlobalVariables.TryGetValue(variableName, out var array) ? array as IList : null,
							array => VariableResolver.GlobalVariables[variableName] = array,
							onlyGrow: true);

						var array = VariableResolver.GlobalVariables[variableName].Conv<IList>();
						Self.Stack.Push(array[index], VMType.v);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.local)
					{
						VariableResolver.ArraySet(index, new List<object?>(),
							() => CurrentCall.Locals.TryGetValue(variableName, out var array) ? array as IList : null,
							array => CurrentCall.Locals[variableName] = array,
							onlyGrow: true);

						var array = CurrentCall.Locals[variableName].Conv<IList>();
						Self.Stack.Push(array[index], VMType.v);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.self)
					{
						// TODO: check builtin self var
						VariableResolver.ArraySet(index, new List<object?>(),
							() => Self.Self.SelfVariables.TryGetValue(variableName, out var array) ? array as IList : null,
							array => Self.Self.SelfVariables[variableName] = array,
							onlyGrow: true);

						var array = Self.Self.SelfVariables[variableName].Conv<IList>();
						Self.Stack.Push(array[index], VMType.v);
						return (ExecutionResult.Success, null);
					}
				}
			}
			else if (variablePrefix == VariablePrefix.ArrayPushAF)
			{
				if (variableType == VariableType.Self)
				{
					var index = Self.Stack.Pop(VMType.i).Conv<int>();
					var instanceId = Self.Stack.Pop(VMType.i).Conv<int>();

					if (instanceId == GMConstants.global)
					{
						var array = VariableResolver.GlobalVariables[variableName].Conv<IList>();
						Self.Stack.Push(array[index], VMType.v);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.local)
					{
						var array = CurrentCall.Locals[variableName].Conv<IList>();
						Self.Stack.Push(array[index], VMType.v);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.self)
					{
						// TODO: check builtin self var
						var array = Self.Self.SelfVariables[variableName].Conv<IList>();
						Self.Stack.Push(array[index], VMType.v);
						return (ExecutionResult.Success, null);
					}
				}
			}
		}
		else if (variablePrefix == VariablePrefix.Stacktop)
		{
			if (variableType == VariableType.Self)
			{
				var id = Self.Stack.Pop(VMType.i).Conv<int>();

				if (id == GMConstants.stacktop)
				{
					var popped = Self.Stack.Pop(VMType.v);

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

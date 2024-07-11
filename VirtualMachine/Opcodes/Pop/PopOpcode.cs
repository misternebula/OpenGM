using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.VirtualMachine;

public static partial class VMExecutor
{
	public static void PopToGlobal(string varName, object? value)
	{
		VariableResolver.GlobalVariables[varName] = value;
	}

	public static void PopToLocal(string varName, object? value)
	{
		Ctx.Locals[varName] = value;
	}

	public static void PopToGlobalArray(string varName, int index, object? value)
	{
		VariableResolver.ArraySet(
			index,
			value,
			() => VariableResolver.GlobalVariables.TryGetValue(varName, out var value) ? value as IList : null,
			array => VariableResolver.GlobalVariables[varName] = array);
	}

	public static void PopToSelf(GamemakerObject self, string varName, object? value)
	{
		if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var variable))
		{
			variable.setter!(self, value);
		}
		else
		{
			self.SelfVariables[varName] = value;
		}
	}

	public static void PopToSelfArray(GamemakerObject self, string varName, int index, object? value)
	{
		if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var variable))
		{
			// dont need to ArraySet because this should never grow and it already exists so doesnt need to be created
			variable.setter!(self, variable);
		}
		else
		{
			VariableResolver.ArraySet(
				index,
				value,
				() => self.SelfVariables.TryGetValue(varName, out var value) ? value as IList : null,
				array => self.SelfVariables[varName] = array);
		}
	}

	public static void PopToIndex(int assetId, string varName, object? value)
	{
		GamemakerObject? instance;
		if (assetId < GMConstants.FIRST_INSTANCE_ID)
		{
			// Asset Index
			instance = InstanceManager.FindByAssetId(assetId).MinBy(x => x.instanceId);
		}
		else
		{
			// Instance Id
			instance = InstanceManager.FindByInstanceId(assetId);
		}

		if (instance == null)
		{
			throw new NotImplementedException();
		}

		// TODO : double check this is always self. might be local as well????
		PopToSelf(instance, varName, value);
	}

	public static (ExecutionResult, object?) DoPop(VMScriptInstruction instruction)
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
			var dataPopped = Ctx.Stack.Pop(instruction.TypeTwo);

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
				PopToSelf(Ctx.Self, variableName, dataPopped);
				return (ExecutionResult.Success, null);
			}
			else if (variableType == VariableType.Index)
			{
				PopToIndex(assetId,  variableName, dataPopped);
				return (ExecutionResult.Success, null);
			}
		}
		else if (variablePrefix == VariablePrefix.Array || variablePrefix == VariablePrefix.ArrayPopAF || variablePrefix == VariablePrefix.ArrayPushAF)
		{
			if (variablePrefix == VariablePrefix.Array)
			{
				if (variableType == VariableType.Self)
				{
					int index;
					int instanceId;
					object? value;
					if (instruction.TypeOne == VMType.v)
					{
						index = Ctx.Stack.Pop(VMType.i).Conv<int>();
						instanceId = Ctx.Stack.Pop(VMType.i).Conv<int>();
						value = Ctx.Stack.Pop(instruction.TypeTwo);
					}
					else
					{
						value = Ctx.Stack.Pop(instruction.TypeTwo);
						index = Ctx.Stack.Pop(VMType.i).Conv<int>();
						instanceId = Ctx.Stack.Pop(VMType.i).Conv<int>();
					}
					
					if (instanceId == GMConstants.global)
					{
						PopToGlobalArray(variableName, index, value);
						return (ExecutionResult.Success, null);
					}
					else if (instanceId == GMConstants.local)
					{
						throw new NotImplementedException();
					}
					else if (instanceId == GMConstants.self)
					{
						PopToSelfArray(Ctx.Self, variableName, index, value);
						return (ExecutionResult.Success, null);
					}
				}
			}
		}
		else if (variablePrefix == VariablePrefix.Stacktop)
		{
			// TODO : Check if 'self' is the only context where [stacktop] is used.

			if (variableType == VariableType.Self)
			{
				int index = 0;
				object? value = null;

				if (instruction.TypeOne == VMType.i)
				{
					value = Ctx.Stack.Pop(instruction.TypeTwo);

					index = Ctx.Stack.Pop(VMType.i).Conv<int>();
					if (index == GMConstants.stacktop)
					{
						index = Ctx.Stack.Pop(VMType.v).Conv<int>();
					}
				}
				else
				{
					index = Ctx.Stack.Pop(VMType.i).Conv<int>();
					if (index == GMConstants.stacktop)
					{
						index = Ctx.Stack.Pop(VMType.v).Conv<int>();
					}

					value = Ctx.Stack.Pop(instruction.TypeTwo);
				}

				PopToIndex(index, variableName, value);
				return (ExecutionResult.Success, null);
			}
		}

		return (ExecutionResult.Failed, $"Don't know how to execute {instruction.Raw}");
	}
}

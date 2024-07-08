using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.VirtualMachine;

public static partial class VMExecutor
{
	public static void PopToGlobal(string varName, object obj)
	{
		if (obj is RValue r)
		{
			VariableResolver.GlobalVariables[varName] = r;
		}
		else
		{
			VariableResolver.GlobalVariables[varName] = new RValue(obj);
		}
	}

	public static void PopToLocal(string varName, object obj)
	{
		if (obj is RValue r)
		{
			Ctx.Locals[varName] = r;
		}
		else
		{
			Ctx.Locals[varName] = new RValue(obj);
		}
	}

	public static void PopToGlobalArray(string varName, int index, object obj)
	{
		RValue valueToSet;
		if (obj is RValue r)
		{
			valueToSet = r;
		}
		else
		{
			valueToSet = new RValue(obj);
		}

		VariableResolver.ArraySet(
			index, 
			valueToSet, 
			() => VariableResolver.GlobalVariables.TryGetValue(varName, out var val) ? val.Value as List<RValue> : null,
			list => VariableResolver.GlobalVariables[varName] = new RValue(list));
	}

	public static void PopToSelf(GamemakerObject self, string varName, object obj)
	{
		if (obj is RValue r)
		{
			self.SelfVariables[varName] = r;
		}
		else
		{
			self.SelfVariables[varName] = new RValue(obj);
		}
	}

	public static void PopToSelfArray(GamemakerObject self, string varName, int index, object obj)
	{
		RValue valueToSet;
		if (obj is RValue r)
		{
			valueToSet = r;
		}
		else
		{
			valueToSet = new RValue(obj);
		}

		VariableResolver.ArraySet(
			index,
			valueToSet,
			() => self.SelfVariables.TryGetValue(varName, out var val) ? val.Value as List<RValue> : null,
			list => self.SelfVariables[varName] = new RValue(list));
	}

	public static void PopToBuiltInArray(GamemakerObject self, string varName, int index, object obj)
	{
		RValue valueToSet;
		if (obj is RValue r)
		{
			valueToSet = r;
		}
		else
		{
			valueToSet = new RValue(obj);
		}

		VariableResolver.ArraySet(
			index,
			valueToSet,
			() => VariableResolver.BuiltInVariables.TryGetValue(varName, out var val) ? val.getter(self) as List<RValue> : null,
			list => VariableResolver.BuiltInVariables[varName].setter(self, list)
		);
	}

	public static (ExecutionResult, object) DoPop(VMScriptInstruction instruction)
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
		}
		else if (variablePrefix == VariablePrefix.Array || variablePrefix == VariablePrefix.ArrayPopAF || variablePrefix == VariablePrefix.ArrayPushAF)
		{
			int index;
			int instanceId;
			object value;
			if (instruction.TypeOne == VMType.v)
			{
				index = Ctx.Stack.Pop<int>(VMType.i);
				instanceId = Ctx.Stack.Pop<int>(VMType.i);
				value = Ctx.Stack.Pop(instruction.TypeTwo);
			}
			else
			{
				value = Ctx.Stack.Pop(instruction.TypeTwo);
				index = Ctx.Stack.Pop<int>(VMType.i);
				instanceId = Ctx.Stack.Pop<int>(VMType.i);
			}

			if (instanceId == GMConstants.global)
			{
				if (variablePrefix == VariablePrefix.Array)
				{
					PopToGlobalArray(variableName, index, value);
					return (ExecutionResult.Success, null);
				}
			}
			else if (instanceId == GMConstants.self)
			{
				if (variablePrefix == VariablePrefix.Array)
				{
					// Built-in instance variables are "self".
					if (VariableResolver.BuiltInVariables.ContainsKey(variableName))
					{
						PopToBuiltInArray(Ctx.Self, variableName, index, value);
						return (ExecutionResult.Success, null);
					}

					PopToSelfArray(Ctx.Self, variableName, index, value);
					return (ExecutionResult.Success, null);
				}
			}

		}

		return (ExecutionResult.Failed, $"Don't know how to execute {instruction.Raw}");
	}
}

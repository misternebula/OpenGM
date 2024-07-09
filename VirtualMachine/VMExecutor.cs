﻿using System.Diagnostics;
using DELTARUNITYStandalone.SerializedFiles;
using System.Collections;

namespace DELTARUNITYStandalone.VirtualMachine;

/// <summary>
/// contains data about each script execution and also environment
/// </summary>
public class VMScriptExecutionContext
{
	public GamemakerObject Self;
	public ObjectDefinition ObjectDefinition;
	public DataStack Stack;
	public Dictionary<string, object> Locals;
	public object ReturnValue;
	public EventType EventType;
	public int EventIndex;

	public override string ToString()
	{
		if (Self == null)
		{
			return "NULL SELF";
		}

		var ret = $"{Self.object_index} ({Self.instanceId})\r\nStack:";
		foreach (var item in Stack)
		{
			ret += $"- {item}\r\n";
		}

		return ret;
	}
}

/// <summary>
/// TODO: remove and replace with just using `object[]` and eventually `RValue[]`
///		  OR revert this if it explodes
/// </summary>
public class Arguments
{
	public VMScriptExecutionContext Ctx => VMExecutor.Ctx;
	public object[] Args;
}

public static partial class VMExecutor
{
	public static Stack<VMScriptExecutionContext> EnvironmentStack = new();
	/// <summary>
	/// gets the top level environment / execution context
	/// </summary>
	public static VMScriptExecutionContext Ctx => EnvironmentStack.Peek();

	public static Stack<VMScript> currentExecutingScript = new();

	public static object ExecuteScript(VMScript script, GamemakerObject obj, ObjectDefinition objectDefinition = null, EventType eventType = EventType.None, int eventIndex = 0, Arguments arguments = null, int startingIndex = 0)
	{
		if (script.Instructions.Count == 0)
		{
			return null!;
		}

		//if (!script.IsGlobalInit)
		//{
			DebugLog.LogInfo($"------------------------------ {script.Name} ------------------------------ ");
		//}

		var newCtx = new VMScriptExecutionContext
		{
			Self = obj,
			ObjectDefinition = objectDefinition,
			Stack = new(),
			Locals = new(),
			ReturnValue = null!,
			EventType = eventType,
			EventIndex = eventIndex
		};

		foreach (var item in script.LocalVariables)
		{
			newCtx.Locals.Add(item, null!);
		}

		if (arguments != null)
		{
			// conv should be able to handle list to array via casting to ICollection
			newCtx.Locals["arguments"] = arguments.Args;
		}

		// Make the current object the current instance
		EnvironmentStack.Push(newCtx);

		var instructionIndex = startingIndex;
		var lastJumpedLabel = 0; // just for debugging

		currentExecutingScript.Push(script);

		while (true)
		{
			ExecutionResult executionResult;
			object? data;

			try
			{
				(executionResult, data) = ExecuteInstruction(script.Instructions[instructionIndex]);

				if (Ctx != null)
				{
					var stackStr = "{ ";
					foreach (var item in Ctx.Stack)
					{
						stackStr += $"{item}, ";
					}

					stackStr += "}";
					DebugLog.LogInfo($"STACK: {stackStr}");
				}
			}
			catch (Exception e)
			{
				executionResult = ExecutionResult.Failed;
				data = e;
			}

			if (executionResult == ExecutionResult.Failed)
			{
				DebugLog.LogError($"Execution of instruction {script.Instructions[instructionIndex].Raw} (Index: {instructionIndex}, Last jumped label: {lastJumpedLabel}) in script {script.Name} failed : {data}");

				/*DebugLog.LogError($"--Stacktrace--");
				foreach (var item in currentExecutingScript)
				{
					DebugLog.LogError($" - {item.Name}");
				}*/

				//Debug.Break();
				break;
			}

			if (executionResult == ExecutionResult.Success)
			{
				if (instructionIndex == script.Instructions.Count - 1)
				{
					// script finished!
					break;
				}

				instructionIndex++;
				continue;
			}

			if (executionResult == ExecutionResult.JumpedToEnd)
			{
				break;
			}

			if (executionResult == ExecutionResult.JumpedToLabel)
			{
				var label = (int)data!;
				instructionIndex = script.Labels[label].InstructionIndex;
				lastJumpedLabel = label;
				continue;
			}

			if (executionResult == ExecutionResult.ReturnedValue)
			{
				Ctx.ReturnValue = data!;
				break;
			}
		}

		// Current object has finished executing, remove from stack
		var returnValue = Ctx.ReturnValue;
		EnvironmentStack.Pop();

		currentExecutingScript.Pop();

		return returnValue;
	}

	// BUG: throws sometimes instead of returning ExecutionResult.Failure
	public static (ExecutionResult result, object? data) ExecuteInstruction(VMScriptInstruction instruction)
	{
		DebugLog.LogInfo($" - {instruction.Raw}");

		switch (instruction.Opcode)
		{
			case VMOpcode.B:
				{
					if (instruction.JumpToEnd)
					{
						return (ExecutionResult.JumpedToEnd, null);
					}

					return (ExecutionResult.JumpedToLabel, instruction.IntData);
				}
			case VMOpcode.BT:
				{
					var boolValue = Ctx.Stack.Pop<bool>(VMType.b);
					if (!boolValue)
					{
						break;
					}

					if (instruction.JumpToEnd)
					{
						return (ExecutionResult.JumpedToEnd, null);
					}

					return (ExecutionResult.JumpedToLabel, instruction.IntData);
				}
			case VMOpcode.BF:
				{
					var boolValue = Ctx.Stack.Pop<bool>(VMType.b);
					if (boolValue)
					{
						break;
					}

					if (instruction.JumpToEnd)
					{
						return (ExecutionResult.JumpedToEnd, null);
					}

					return (ExecutionResult.JumpedToLabel, instruction.IntData);
				}
			case VMOpcode.CMP:

				var second = Ctx.Stack.Pop(instruction.TypeTwo);
				var first = Ctx.Stack.Pop(instruction.TypeOne);

				// first ??= 0;
				// second ??= 0;
				
				// TODO: array and undefined cmp

				// TODO: handle all numerical types
				if (second is bool or int or double or long && first is bool or int or double or long)
				{
					var firstNumber = Conv<double>(first);
					var secondNumber = Conv<double>(second);

					var equal = CustomMath.ApproxEqual(firstNumber, secondNumber);

					switch (instruction.Comparison)
					{
						case VMComparison.LT:
							Ctx.Stack.Push(firstNumber < secondNumber, VMType.b);
							break;
						case VMComparison.LTE:
							Ctx.Stack.Push(firstNumber <= secondNumber, VMType.b);
							break;
						case VMComparison.EQ:
							Ctx.Stack.Push(equal, VMType.b);
							break;
						case VMComparison.NEQ:
							Ctx.Stack.Push(!equal, VMType.b);
							break;
						case VMComparison.GTE:
							Ctx.Stack.Push(firstNumber >= secondNumber, VMType.b);
							break;
						case VMComparison.GT:
							Ctx.Stack.Push(firstNumber > secondNumber, VMType.b);
							break;
						case VMComparison.None:
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
				else
				{
					// this should handle strings and whatever else

					if (instruction.Comparison == VMComparison.EQ)
					{
						Ctx.Stack.Push(first.Equals(second), VMType.b);
					}
					else if (instruction.Comparison == VMComparison.NEQ)
					{
						Ctx.Stack.Push(!first.Equals(second), VMType.b);
					}
					else
					{
						// ??? no idea if this is what GM does
						Ctx.Stack.Push(false, VMType.b);
					}
				}
				break;
			case VMOpcode.PUSHGLB:
			case VMOpcode.PUSHLOC:
			case VMOpcode.PUSHBLTN:
			case VMOpcode.PUSHI:
			case VMOpcode.PUSH:
				return DoPush(instruction);
			case VMOpcode.POP:
				return DoPop(instruction);
			case VMOpcode.RET:
				// ret value is always stored as rvalue
				return (ExecutionResult.ReturnedValue, Ctx.Stack.Pop(VMType.v));
			case VMOpcode.CONV:
				// dont actually convert, just tell the stack we're a different type
				// since we have to conv everywhere else later anyway with rvalue
				Ctx.Stack.Push(Ctx.Stack.Pop(instruction.TypeOne), instruction.TypeTwo);
				break;
			case VMOpcode.POPZ:
				Ctx.Stack.Pop(instruction.TypeOne);
				break;
			case VMOpcode.CALL:
				var arguments = new Arguments
				{
					Args = new object[instruction.FunctionArgumentCount]
				};

				for (var i = 0; i < instruction.FunctionArgumentCount; i++)
				{
					// args are always pushed as rvalues
					arguments.Args[i] = Ctx.Stack.Pop(VMType.v);
				}

				if (ScriptResolver.BuiltInFunctions.TryGetValue(instruction.FunctionName, out var builtInFunction))
				{
					if (builtInFunction == null)
					{
						DebugLog.LogError($"NULL FUNC");
					}

					if (Ctx == null)
					{
						DebugLog.LogError($"NULL CTX");
					}

					if (Ctx.Stack == null)
					{
						DebugLog.LogError($"NULL STACK");
					}

					Ctx.Stack.Push(builtInFunction(arguments), VMType.v);
					break;
				}

				if (ScriptResolver.Scripts.TryGetValue(instruction.FunctionName, out var scriptName))
				{
					Ctx.Stack.Push(ExecuteScript(scriptName, Ctx.Self, Ctx.ObjectDefinition, arguments: arguments), VMType.v);
					break;
				}

				if (ScriptResolver.ScriptFunctions.TryGetValue(instruction.FunctionName, out var scriptFunction))
				{
					var (script, instructionIndex) = scriptFunction;
					Ctx.Stack.Push(ExecuteScript(script, Ctx.Self, Ctx.ObjectDefinition, arguments: arguments, startingIndex: instructionIndex), VMType.v);
					break;
				}

				return (ExecutionResult.Failed, $"Can't resolve script {instruction.FunctionName} !");
			case VMOpcode.PUSHENV:
				return PUSHENV(instruction);
			case VMOpcode.POPENV:
				return POPENV(instruction);
			case VMOpcode.DUP:
				return DoDup(instruction);
			case VMOpcode.ADD:
				return ADD(instruction);
			case VMOpcode.SUB:
				return SUB(instruction);
			case VMOpcode.MUL:
				return MUL(instruction);
			case VMOpcode.DIV:
				return DIV(instruction);
			case VMOpcode.REM:
				return REM(instruction);
			case VMOpcode.MOD:
				return MOD(instruction);
			case VMOpcode.NEG:
				return NEG(instruction);
			case VMOpcode.AND:
				return AND(instruction);
			case VMOpcode.OR:
				return OR(instruction);
			case VMOpcode.XOR:
				return XOR(instruction);
			case VMOpcode.NOT:
				return NOT(instruction);
			// TODO: move to MathOpcodes.cs
			case VMOpcode.SHL:
				{
					// is this the right order?
					var intTwo = Conv<int>(Ctx.Stack.Pop());
					var intOne = Conv<int>(Ctx.Stack.Pop());

					Ctx.Stack.Push(intOne << intTwo);
					break;
				}
			case VMOpcode.SHR:
				{
					// is this the right order?
					var intTwo = Conv<int>(Ctx.Stack.Pop());
					var intOne = Conv<int>(Ctx.Stack.Pop());

					Ctx.Stack.Push(intOne >> intTwo);
					break;
				}
			case VMOpcode.CHKINDEX:
			{
				// unused in ch2???? no clue what this does
				
				var index = Ctx.Stack.Peek();

				if (index is int)
				{
					break;
				}

				throw new Exception($"CHKINDEX failed - {index}");
			}
			case VMOpcode.EXIT:
				return (ExecutionResult.ReturnedValue, null!);
			case VMOpcode.SETOWNER:
				// seems to always push.i before
				var id = Conv<int>(Ctx.Stack.Pop());
				break;
			// TODO: fix these eventually
			case VMOpcode.POPAF:
			{
				var index = Conv<int>(Ctx.Stack.Pop());
				var array = Conv<ArrayReference>(Ctx.Stack.Pop());
				var value = Ctx.Stack.Pop();

				if (array.Array.Count <= index)
				{
					array.Array.AddRange(new object[array.Array.Count + 1 - index]);
				}

				array.Array[index] = value; // this is definitely wrong and still broke. make it work with rvalue (just make arrayreference be rvalue actually)

				if (array.IsGlobal)
				{
					VariableResolver.GlobalVariables[array.ArrayName] = array.Array;
				}
				else
				{
					array.Instance.SelfVariables[array.ArrayName] = array.Array;
				}

				break;
			}
			case VMOpcode.PUSHAF: 
			{
				var index = Conv<int>(Ctx.Stack.Pop());
				var array = Conv<ArrayReference>(Ctx.Stack.Pop());

				var value = array.Array[index];

				if (value is IEnumerable)
				{
					throw new NotImplementedException();
				}

				Ctx.Stack.Push(value, VMType.v);

				break;
			}
			case VMOpcode.CALLV:
			case VMOpcode.BREAK:
			default:
				return (ExecutionResult.Failed, $"Unknown opcode {instruction.Opcode}");
		}

		return (ExecutionResult.Success, null);
	}

	public static int VMTypeToSize(VMType type) => type switch
	{
		VMType.v => 16,
		VMType.d => 8,
		VMType.l => 8,
		VMType.i => 4,
		VMType.b => 4,
		VMType.s => 4,
		VMType.e => 4
	};

	public static T Conv<T>(this object @this)
	{
		// TODO: i want to try removing this and see how Conv<object> reacts
		if (typeof(T) != typeof(object))
		{
			return (T)Conv(@this, typeof(T));
		}
		else
		{
			return (T)Conv(@this, Activator.CreateInstance(typeof(T), null).GetType());
		}
	}

	public static object Conv(this object @this, Type type)
	{
		if (@this == null)
		{
			throw new NullReferenceException("null/undefined passed into conv");
		}

		if (type == typeof(object))
		{
			return @this;
		}

		if (type == typeof(RValue))
		{
			return new RValue(@this);
		}

		if (@this is Undefined && type == typeof(bool))
		{
			return false;
		}

		if (@this is Undefined && (type == typeof(int) || type == typeof(double) || type == typeof(long)))
		{
			return 0;
		}

		if (@this is Undefined && type == typeof(List<object>))
		{
			return new List<object>();
		}

		if (@this is Undefined)
		{
			DebugLog.LogError($"Trying to convert undefined to {type}! Current script:{currentExecutingScript.First().Name}");
			return null!;
		}

		if (@this.GetType() == type)
		{
			return @this;
		}

		try
		{
			if (@this is RValue r)
			{
				//DebugLog.Log($"Converting RValue {r} to {type}");
				return Conv(r.Value, type);
			}
			else if (@this is string s)
			{
				// not sure how to implement numeric -> string properly

				// "numbers, minus signs, decimal points and exponential parts in the string are taken into account,
				// while other characters (such as letters) will cause an error to be thrown."

				if (type == typeof(int))
				{
					return int.Parse(s);
				}

				if (type == typeof(double))
				{
					return double.Parse(s);
				}

				if (type == typeof(bool))
				{
					return bool.Parse(s); // dunno if "true" or "false" should convert properly, since bools are just ints?
				}
			}
			else if (@this is int or long)
			{
				var i = System.Convert.ToInt64(@this);

				if (type == typeof(int))
				{
					return (int)i;
				}

				if (type == typeof(long))
				{
					return i;
				}

				if (type == typeof(bool))
				{
					return i > 0;
				}

				if (type == typeof(double))
				{
					return (double)i;
				}

				if (type == typeof(string))
				{
					return i.ToString(); // not sure if positive numbers need to have a "+" in front?
				}
			}
			else if (@this is bool b)
			{
				if (type == typeof(int))
				{
					return (int)(b ? 1 : 0);
				}

				if (type == typeof(double))
				{
					return (double)(b ? 1 : 0);
				}

				if (type == typeof(string))
				{
					return b ? "1" : "0"; // GM represents bools as integers
				}
			}
			else if (@this is double or float)
			{
				var d = System.Convert.ToDouble(@this);

				if (type == typeof(double) || type == typeof(float))
				{
					return d;
				}

				if (type == typeof(bool))
				{
					return d > 0.5; // https://manual.yoyogames.com/GameMaker_Language/GML_Reference/Variable_Functions/bool.htm
				}

				if (type == typeof(int))
				{
					return (int)d;
				}

				if (type == typeof(string))
				{
					var isInt = Math.Abs(d % 1) <= (double.Epsilon * 100);
					// https://manual.yoyogames.com/GameMaker_Language/GML_Reference/Strings/string.htm
					return isInt ? d.ToString("0") : (object)d.ToString("0.00");
				}
			}
		}
		catch
		{
			throw new Exception($"Exception while converting {@this} ({@this.GetType().FullName}) to {type}");
		}

		DebugLog.LogError($"Don't know how to convert {@this} ({@this.GetType().FullName}) to {type}");
		return null!;
	}
}

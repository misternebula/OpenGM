using System.Diagnostics;
using DELTARUNITYStandalone.SerializedFiles;
using System.Collections;

namespace DELTARUNITYStandalone.VirtualMachine;

/// <summary>
/// contains data about each script execution and also environment
/// </summary>
public class VMScriptExecutionContext
{
	public GamemakerObject Self = null!; // can be null for global scripts but those shouldnt run functions that need it
	public ObjectDefinition? ObjectDefinition;
	public DataStack Stack = null!;
	public Dictionary<string, object?> Locals = null!;
	public object? ReturnValue;
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

public static partial class VMExecutor
{
	public static Stack<VMScriptExecutionContext> EnvironmentStack = new();
	/// <summary>
	/// gets the top level environment / execution context
	/// </summary>
	public static VMScriptExecutionContext Ctx => EnvironmentStack.Peek();

	public static Stack<VMScript> currentExecutingScript = new();

	public static object? ExecuteScript(VMScript script, GamemakerObject? obj, ObjectDefinition? objectDefinition = null, EventType eventType = EventType.None, int eventIndex = 0, object?[]? args = null, int startingIndex = 0)
	{
		if (script.Instructions.Count == 0)
		{
			return null;
		}

		//if (!script.IsGlobalInit)
		//{
			DebugLog.LogInfo($"------------------------------ {script.Name} ------------------------------ ");
		//}

		var newCtx = new VMScriptExecutionContext
		{
			Self = obj!,
			ObjectDefinition = objectDefinition,
			Stack = new(),
			Locals = new(),
			ReturnValue = null,
			EventType = eventType,
			EventIndex = eventIndex
		};

		foreach (var item in script.LocalVariables)
		{
			newCtx.Locals.Add(item, null);
		}

		if (args != null)
		{
			// conv should be able to handle list to array via casting to ICollection
			newCtx.Locals["arguments"] = args;
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
				Ctx!.ReturnValue = data!;
				break;
			}
		}

		// Current object has finished executing, remove from stack
		var returnValue = Ctx!.ReturnValue;
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
					var boolValue = Ctx.Stack.Pop(VMType.b).Conv<bool>();
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
					var boolValue = Ctx.Stack.Pop(VMType.b).Conv<bool>();
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

				// TODO: handle all numeric primitives
				if (second is bool or int or double or long && first is bool or int or double or long)
				{
					var firstNumber = first.Conv<double>();
					var secondNumber = second.Conv<double>();

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
						Ctx.Stack.Push(first?.Equals(second), VMType.b);
					}
					else if (instruction.Comparison == VMComparison.NEQ)
					{
						Ctx.Stack.Push(first?.Equals(second), VMType.b);
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
				var args = new object?[instruction.FunctionArgumentCount];

				for (var i = 0; i < instruction.FunctionArgumentCount; i++)
				{
					// args are always pushed as rvalues
					args[i] = Ctx.Stack.Pop(VMType.v);
				}

				if (ScriptResolver.BuiltInFunctions.TryGetValue(instruction.FunctionName, out var builtInFunction))
				{
					// goofy null checks
					if (builtInFunction == null)
					{
						DebugLog.LogError($"NULL FUNC");
					}

					if (Ctx == null)
					{
						DebugLog.LogError($"NULL CTX");
					}

					if (Ctx!.Stack == null)
					{
						DebugLog.LogError($"NULL STACK");
					}

					Ctx.Stack!.Push(builtInFunction!(args), VMType.v);
					break;
				}

				if (ScriptResolver.Scripts.TryGetValue(instruction.FunctionName, out var scriptName))
				{
					Ctx.Stack.Push(ExecuteScript(scriptName, Ctx.Self, Ctx.ObjectDefinition, args: args), VMType.v);
					break;
				}

				if (ScriptResolver.ScriptFunctions.TryGetValue(instruction.FunctionName, out var scriptFunction))
				{
					var (script, instructionIndex) = scriptFunction;
					Ctx.Stack.Push(ExecuteScript(script, Ctx.Self, Ctx.ObjectDefinition, args: args, startingIndex: instructionIndex), VMType.v);
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
				return SHL(instruction);
			case VMOpcode.SHR:
				return SHR(instruction);
			case VMOpcode.CHKINDEX:
			{
				// unused in ch2???? no clue what this does
				
				var (index, type) = Ctx.Stack.Peek();

				if (index is int || type is VMType.i) // do we check type idk
				{
					break;
				}

				throw new Exception($"CHKINDEX failed - {index}");
			}
			case VMOpcode.EXIT:
				return (ExecutionResult.ReturnedValue, null);
			case VMOpcode.SETOWNER:
				// seems to always push.i before
				var id = Ctx.Stack.Pop(VMType.i).Conv<int>();
				break;
			// TODO: fix these eventually
			/*
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
			*/
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
		VMType.e => 4,
		_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
	};

	public static void Joe()
	{
		
	}

	public static T Conv<T>(this object? @this)
	{
		// TODO: i want to try removing this and see how Conv<object> reacts
		if (typeof(T) != typeof(object))
		{
			return (T)@this.Conv(typeof(T));
		}
		else
		{
			throw new Exception("trying to conv to object lol");
			// return (T)@this.Conv(Activator.CreateInstance(typeof(T), null)!.GetType());
		}
	}

	public static object Conv(this object? @this, Type type)
	{
		// TODO: collections
		// TODO: check all numeric primitives
		// TODO: cleanup
		
		if (@this is null)
		{
			// throw new NullReferenceException("null/undefined passed into conv");
		}

		if (@this is not null && type == typeof(object)) // gorp. this shouldnt happen
		{
			return @this;
		}

		if (@this is null && type.Is<bool>())
		{
			return false;
		}

		if (@this is null && (type.Is<int>() || type.Is<double>() || type.Is<float>() || type.Is<long>() || type.Is<short>()))
		{
			return 0;
		}

		if (@this is null && type.Is<IEnumerable>())
		{
			return new List<object>();
		}

		if (@this is null)
		{
			throw new ArgumentException($"Trying to convert undefined to {type}! Current script:{currentExecutingScript.First().Name}");
		}

		if (@this.GetType() == type)
		{
			return @this;
		}

		try
		{
			if (@this is string s)
			{
				// not sure how to implement numeric -> string properly

				// "numbers, minus signs, decimal points and exponential parts in the string are taken into account,
				// while other characters (such as letters) will cause an error to be thrown."

				if (type.Is<int>()) return int.Parse(s);
				if (type.Is<short>()) return short.Parse(s);
				if (type.Is<double>()) return double.Parse(s);
				if (type.Is<float>()) return float.Parse(s);
				if (type.Is<bool>()) return bool.Parse(s); // dunno if "true" or "false" should convert properly, since bools are just ints?
			}
			else if (@this is int or long or short)
			{
				var l = Convert.ToInt64(@this); // can we cast instead?

				if (type.Is<int>()) return (int)l;
				if (type.Is<short>()) return (short)l;
				if (type.Is<long>()) return l;
				if (type.Is<bool>()) return l > 0;
				if (type.Is<double>()) return (double)l;
				if (type.Is<string>()) return l.ToString(); // not sure if positive numbers need to have a "+" in front?
			}
			else if (@this is bool b)
			{
				if (type.Is<int>()) return (int)(b ? 1 : 0);
				if (type.Is<long>()) return (long)(b ? 1 : 0);
				if (type.Is<short>()) return (short)(b ? 1 : 0);
				if (type.Is<double>()) return (double)(b ? 1 : 0);
				if (type.Is<float>()) return (double)(b ? 1 : 0);
				if (type.Is<string>()) return b ? "1" : "0"; // GM represents bools as integers
			}
			else if (@this is double or float)
			{
				var d = Convert.ToDouble(@this);

				if (type.Is<double>()) return (double)d;
				if (type.Is<float>()) return (float)d;

				if (type.Is<bool>()) return d > 0.5; // https://manual.yoyogames.com/GameMaker_Language/GML_Reference/Variable_Functions/bool.htm

				if (type.Is<int>()) return (int)d;
				if (type.Is<long>()) return (long)d;
				if (type.Is<short>()) return (short)d;

				if (type.Is<string>())
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

		throw new ArgumentException($"Don't know how to convert {@this} ({@this.GetType().FullName}) to {type}");
	}
}

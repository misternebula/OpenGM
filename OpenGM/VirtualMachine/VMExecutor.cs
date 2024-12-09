using System.Diagnostics;
using OpenGM.SerializedFiles;
using System.Collections;
using OpenGM.IO;

namespace OpenGM.VirtualMachine;

/// <summary>
/// contains data about each script execution and also environment
/// </summary>
public class VMScriptExecutionContext
{
	public IStackContextSelf Self = null!; // can be null for global scripts but those shouldnt run functions that need it
	public GamemakerObject GMSelf => (GamemakerObject)Self;
	public ObjectDefinition? ObjectDefinition;
	public DataStack Stack = null!;
	//public Dictionary<string, object?> Locals = null!;
	public object? ReturnValue;
	public EventType EventType;
	public int EventIndex;

	public override string ToString()
	{
		if (Self == null)
		{
			return "NULL SELF";
		}

		if (Self is GamemakerObject gm)
		{
			var ret = $"{gm.object_index} ({gm.instanceId})\r\nStack:";
			foreach (var item in Stack)
			{
				ret += $"- {item}\r\n";
			}

			return ret;
		}
		else
		{
			return $"Self: {Self.GetType()}";
		}
	}
}

public class VMCall
{
	public VMCode Code = null!;
	public VMScriptExecutionContext Ctx = null!;
	public Dictionary<string, object?> Locals = new();

	public VMCall(VMCode code, VMScriptExecutionContext ctx)
	{
		Code = code;
		Ctx = ctx;
	}
}

public static partial class VMExecutor
{
	public static Stack<VMScriptExecutionContext> EnvironmentStack = new();
	/// <summary>
	/// gets the top level environment / execution context
	/// </summary>
	public static VMScriptExecutionContext Ctx => EnvironmentStack.Peek();

	public static Stack<VMCall> CallStack = new();
	public static VMCall CurrentCall => CallStack.Peek();

	public static VMScriptExecutionContext Self
	{
		get
		{
			if (Ctx == null)
			{
				// Null at top of stack, in WITH statement. Next value is self.
				return EnvironmentStack.ToArray()[1];
			}

			return Ctx;
		}
	}

	public static VMScriptExecutionContext Other
	{
		get
		{
			if (EnvironmentStack.Count == 1)
			{
				return Self;
			}

			if (Ctx == null)
			{
				// Null at top of stack, in WITH statement. Next value is self, then next value is other.
				return EnvironmentStack.ToArray()[2];
			}

			var stack = EnvironmentStack.ToArray();
			if (stack.Contains(null))
			{
				var i = 0;

				while (stack[i] != null)
				{
					i++;
				}

				i++; // we found the null, so previous one is the ctx that called PUSHENV
				return stack[i];
			}

			return stack[1];
		}
	}

	public static bool VerboseStackLogs;
	public static VMCodeInstruction? CurrentInstruction;
	
	// private static IList? _temporaryArrayStorage = null;

	public static object? ExecuteCode(VMCode? code, IStackContextSelf? obj, ObjectDefinition? objectDefinition = null, EventType eventType = EventType.None, int eventIndex = 0, object?[]? args = null, int startingIndex = 0)
	{
		if (code == null)
		{
			DebugLog.LogError($"Tried to run null code!");
			return null;
		}

		if (code.Instructions.Count == 0)
		{
			return null;
		}

		if (VerboseStackLogs)
		{
			//if (!script.IsGlobalInit)
			//{
			var space = "   ";
			var count = CallStack.Count;
			var leftPadding = string.Concat(Enumerable.Repeat(space, count));

			DebugLog.LogInfo($"{leftPadding}------------------------------ {code.Name} ------------------------------ ");
			//}
		}

		var newCtx = new VMScriptExecutionContext
		{
			Self = obj!,
			ObjectDefinition = objectDefinition,
			Stack = new(),
			//Locals = new(),
			ReturnValue = null,
			EventType = eventType,
			EventIndex = eventIndex
		};

		// Make the current object the current instance
		EnvironmentStack.Push(newCtx);

		var instructionIndex = startingIndex;
		var lastJumpedLabel = 0; // just for debugging

		var call = new VMCall(code, newCtx);
		CallStack.Push(call);

		foreach (var item in code.LocalVariables)
		{
			call.Locals.Add(item, null);
		}

		if (args != null)
		{
			// conv should be able to handle list to array via casting to IList
			call.Locals["arguments"] = args;
		}

		code.CodeExecuted();

		while (true)
		{
			ExecutionResult executionResult;
			object? data;

			try
			{
				(executionResult, data) = ExecuteInstruction(code.Instructions[instructionIndex]);

				if (VerboseStackLogs && Ctx != null)
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
				DebugLog.LogError($"Execution of instruction {code.Instructions[instructionIndex].Raw} (Index: {instructionIndex}, Last jumped label: {lastJumpedLabel}) in script {code.Name} failed : {data}");

				DebugLog.LogError($"--Stacktrace--");
				foreach (var item in CallStack)
				{
					DebugLog.LogError($" - {item.Code.Name}");
				}

				//Debug.Break();
				break;
			}

			if (executionResult == ExecutionResult.Success)
			{
				if (instructionIndex == code.Instructions.Count - 1)
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
				instructionIndex = code.Labels[label];
				lastJumpedLabel = label;
				continue;
			}

			if (executionResult == ExecutionResult.ReturnedValue)
			{
				Ctx!.ReturnValue = data;
				break;
			}
		}

		// Current object has finished executing, remove from stack
		var returnValue = Ctx!.ReturnValue;
		EnvironmentStack.Pop();

		CallStack.Pop();

		if (VerboseStackLogs)
		{
			//if (!script.IsGlobalInit)
			//{
			var space = "   ";
			var count = CallStack.Count;
			var leftPadding = string.Concat(Enumerable.Repeat(space, count));

			DebugLog.LogInfo($"{leftPadding}-#-#-#-#-#-#-#-#-#-#-#-#-#-#-- {code.Name} --#-#-#-#-#-#-#-#-#-#-#-#-#-#- ");
			//}
		}

		return returnValue;
	}

	// BUG: throws sometimes instead of returning ExecutionResult.Failure
	public static (ExecutionResult result, object? data) ExecuteInstruction(VMCodeInstruction instruction)
	{
		if (VerboseStackLogs) DebugLog.LogInfo($" - {instruction.Raw}");
		CurrentInstruction = instruction;

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

				var second = Ctx.Stack.Pop(instruction.TypeOne);
				var first = Ctx.Stack.Pop(instruction.TypeTwo);

				// first ??= 0;
				// second ??= 0;
				
				// TODO: array and undefined cmp

				if (second is bool or int or short or long or double or float && first is bool or int or short or long or double or float)
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
						if (first is null && second is null)
						{
							Ctx.Stack.Push(true, VMType.b);
						}
						else if (first is null || second is null)
						{
							Ctx.Stack.Push(false, VMType.b);
						}
						else
						{
							Ctx.Stack.Push(first?.Equals(second), VMType.b);
						}
					}
					else if (instruction.Comparison == VMComparison.NEQ)
					{
						if (first is null && second is null)
						{
							Ctx.Stack.Push(false, VMType.b);
						}
						else if (first is null || second is null)
						{
							Ctx.Stack.Push(true, VMType.b);
						}
						else
						{
							Ctx.Stack.Push(!first?.Equals(second), VMType.b);
						}
					}
					else
					{
						var firstValue = first is string s1 ? double.Parse(s1) : first.Conv<double>();
						var secondValue = second is string s2 ? double.Parse(s2) : second.Conv<double>();

						switch (instruction.Comparison)
						{
							case VMComparison.LT:
								Ctx.Stack.Push(firstValue < secondValue, VMType.b);
								break;
							case VMComparison.LTE:
								Ctx.Stack.Push(firstValue <= secondValue, VMType.b);
								break;
							case VMComparison.GTE:
								Ctx.Stack.Push(firstValue >= secondValue, VMType.b);
								break;
							case VMComparison.GT:
								Ctx.Stack.Push(firstValue > secondValue, VMType.b);
								break;
							case VMComparison.None:
							default:
								throw new ArgumentOutOfRangeException();
						}

						//return (ExecutionResult.Failed, $"cant cmp {instruction.Comparison} on {first?.GetType()} {first} and {second?.GetType()} {second}");
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

				if (ScriptResolver.ScriptFunctions.TryGetValue(instruction.FunctionName, out var scriptFunction))
				{
					var (script, instructionIndex) = scriptFunction;
					Ctx.Stack.Push(ExecuteCode(script, Ctx.GMSelf, Ctx.ObjectDefinition, args: args, startingIndex: instructionIndex), VMType.v);
					break;
				}

				if (ScriptResolver.Scripts.TryGetValue(instruction.FunctionName, out var scriptName))
				{
					Ctx.Stack.Push(ExecuteCode(scriptName.GetCode(), Ctx.GMSelf, Ctx.ObjectDefinition, args: args), VMType.v);
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
			case VMOpcode.SHL:
				return SHL(instruction);
			case VMOpcode.SHR:
				return SHR(instruction);
			case VMOpcode.CHKINDEX:
			{
				// unused in ch2???? no clue what this does
				// used for multi-dimensional array bounds checking. c# does that anyway so its probably fine
				
				var (index, type) = Ctx.Stack.Peek();

				if (index is int || type is VMType.i || type is VMType.e) // do we check type idk
				{
					break;
				}

				throw new Exception($"CHKINDEX failed - {index} ({type})");
			}
			case VMOpcode.EXIT:
				return (ExecutionResult.ReturnedValue, instruction.TypeOne switch
				{
					VMType.i or VMType.e or VMType.d or VMType.l => 0,
					VMType.b => false,
					VMType.s => "",
					VMType.v => null,
					_ => throw new ArgumentOutOfRangeException()
				});
			case VMOpcode.SETOWNER:
				// seems to always push.i before
				// apparently used for COW array stuff. does that mean this subtley breaks everything because arrays expect to copy?
				var id = Ctx.Stack.Pop(VMType.i).Conv<int>();
				break;
			case VMOpcode.POPAF:
			{
				var index = Ctx.Stack.Pop(VMType.i).Conv<int>();
				var array = Ctx.Stack.Pop(VMType.v).Conv<IList>();
				
				var value = Ctx.Stack.Pop(VMType.v);
				
				// by the magic of reference types this will be set properly
				VariableResolver.ArraySet(index, value,
					() => array,
					_ => throw new UnreachableException("this is called when getter is null"));

				break;
			}
			case VMOpcode.PUSHAF: 
			{
				var index = Ctx.Stack.Pop(VMType.i).Conv<int>();
				var array = Ctx.Stack.Pop(VMType.v).Conv<IList>();

				var value = array[index];

				Ctx.Stack.Push(value, VMType.v);

				break;
			}
			case VMOpcode.SAVEAREF:
			{
				// doing what the comment in Underanalyzer says makes everything break???
				
				// if (_temporaryArrayStorage != null) throw new Exception("savearef - array already stored");
				// var wtfIsThis = Ctx.Stack.Pop(VMType.i);
				// _temporaryArrayStorage = Ctx.Stack.Pop(VMType.v).Conv<IList>();
				break;
			}
			case VMOpcode.RESTOREAREF:
			{
				// doing what the comment in Underanalyzer says makes everything break???
				
				// if (_temporaryArrayStorage == null) throw new Exception("savearef - array not stored");
				// Ctx.Stack.Push(_temporaryArrayStorage, VMType.v);
				// _temporaryArrayStorage = null;
				break;
			}
			case VMOpcode.CALLV:
				throw new NotImplementedException("callv");
			case VMOpcode.BREAK:
				throw new UnreachableException("break is used as an extended opcode marker, so it should never show up as an instruction");
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

	/// <summary>
	/// custom cast function.
	/// works with: all number types, string, bool, IList
	/// </summary>
	public static T Conv<T>(this object? @this) => (T)@this.Conv(typeof(T));
	
	private static object Conv(this object? @this, Type type)
	{
		// TODO: check all numeric primitives

		if (@this is null)
		{
			// Bool is the only thing that undefined can be converted to
			// (YYGetBool is the only function that checks for 0x5)
			if (type == typeof(bool)) return false;

			throw new ArgumentException($"Trying to convert undefined to {type}! Current script:{CallStack.First().Code.Name}");
		}

		if (@this.GetType() == type)
		{
			return @this;
		}

		if (@this is string s)
		{
			// not sure how to implement numeric -> string properly

			// "numbers, minus signs, decimal points and exponential parts in the string are taken into account,
			// while other characters (such as letters) will cause an error to be thrown."

			if (type == typeof(int)) return int.Parse(s);
			if (type == typeof(short)) return short.Parse(s);
			if (type == typeof(long)) return long.Parse(s);
			
			if (type == typeof(double)) return double.Parse(s);
			if (type == typeof(float)) return float.Parse(s);
			
			if (type == typeof(bool)) return bool.Parse(s); // dunno if "true" or "false" should convert properly, since bools are just ints?
		}
		else if (@this is int or long or short)
		{
			var l = Convert.ToInt64(@this);

			if (type == typeof(int)) return (int)l;
			if (type == typeof(short)) return (short)l;
			if (type == typeof(long)) return (long)l;
			
			if (type == typeof(double)) return (double)l;
			if (type == typeof(float)) return (float)l;
			
			if (type == typeof(bool)) return l > 0;
			
			if (type == typeof(string)) return l.ToString(); // not sure if positive numbers need to have a "+" in front?
		}
		else if (@this is bool b)
		{
			if (type == typeof(int)) return (int)(b ? 1 : 0);
			if (type == typeof(long)) return (long)(b ? 1 : 0);
			if (type == typeof(short)) return (short)(b ? 1 : 0);
			
			if (type == typeof(double)) return (double)(b ? 1 : 0);
			if (type == typeof(float)) return (float)(b ? 1 : 0);
			
			if (type == typeof(string)) return b ? "1" : "0"; // GM represents bools as integers
		}
		else if (@this is double or float)
		{
			var d = Convert.ToDouble(@this);

			if (type == typeof(double)) return (double)d;
			if (type == typeof(float)) return (float)d;

			if (type == typeof(int)) return (int)d;
			if (type == typeof(long)) return (long)d;
			if (type == typeof(short)) return (short)d;
			
			if (type == typeof(bool)) return d > 0.5; // https://manual.yoyogames.com/GameMaker_Language/GML_Reference/Variable_Functions/bool.htm

			if (type == typeof(string))
			{
				var isInt = Math.Abs(d % 1) <= (double.Epsilon * 100);
				// https://manual.yoyogames.com/GameMaker_Language/GML_Reference/Strings/string.htm
				return isInt ? d.ToString("0") : d.ToString("0.00");
			}
		} 
		else if (@this is IList array && type == typeof(IList))
		{
			return array;
		}

		throw new ArgumentException($"Don't know how to convert {@this} ({@this.GetType().FullName}) to {type}");
	}
}

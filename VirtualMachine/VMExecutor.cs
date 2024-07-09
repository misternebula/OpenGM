using System.Diagnostics;
using DELTARUNITYStandalone.SerializedFiles;

namespace DELTARUNITYStandalone.VirtualMachine;

/// <summary>
/// contains data about each script execution and also environment
/// </summary>
public class VMScriptExecutionContext
{
	public GamemakerObject Self;
	public ObjectDefinition ObjectDefinition;
	public DataStack Stack;
	public Dictionary<string, RValue> Locals;
	public RValue ReturnValue;
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
	/// <summary>
	/// `object` here is `RValue.Value`
	/// </summary>
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

	public static RValue ExecuteScript(VMScript script, GamemakerObject obj, ObjectDefinition objectDefinition = null, EventType eventType = EventType.None, int eventIndex = 0, Arguments arguments = null, int startingIndex = 0)
	{
		if (script.Instructions.Count == 0)
		{
			return new RValue(Undefined.Value);
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
			ReturnValue = new RValue(Undefined.Value),
			EventType = eventType,
			EventIndex = eventIndex
		};

		foreach (var item in script.LocalVariables)
		{
			newCtx.Locals.Add(item, new RValue(Undefined.Value));
		}

		if (arguments != null)
		{
			// turn list of rvalue.value into rvalue of list of values
			// this can be removed later when we just use rvalues for all functions
			newCtx.Locals["arguments"] = new RValue(arguments.Args.Select(x => new RValue(x)).ToList());
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
				Ctx.ReturnValue = (RValue)data!;
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

				var second = Ctx.Stack.Pop();
				var first = Ctx.Stack.Pop();

				if (second is RValue r2)
				{
					second = r2.Value;
				}

				if (first is RValue r1)
				{
					first = r1.Value;
				}

				first ??= 0;
				second ??= 0;
				
				// TODO: array and undefined cmp

				if (second is bool or int or double or long && first is bool or int or double or long)
				{
					var firstNumber = Conv<double>(first);
					var secondNumber = Conv<double>(second);

					var equal = CustomMath.ApproxEqual(firstNumber, secondNumber);

					switch (instruction.Comparison)
					{
						case VMComparison.LT:
							Ctx.Stack.Push(firstNumber < secondNumber);
							break;
						case VMComparison.LTE:
							Ctx.Stack.Push(firstNumber <= secondNumber);
							break;
						case VMComparison.EQ:
							Ctx.Stack.Push(equal);
							break;
						case VMComparison.NEQ:
							Ctx.Stack.Push(!equal);
							break;
						case VMComparison.GTE:
							Ctx.Stack.Push(firstNumber >= secondNumber);
							break;
						case VMComparison.GT:
							Ctx.Stack.Push(firstNumber > secondNumber);
							break;
						case VMComparison.None:
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
				else
				{
					if (instruction.Comparison == VMComparison.EQ)
					{
						Ctx.Stack.Push(first.Equals(second));
					}
					else if (instruction.Comparison == VMComparison.NEQ)
					{
						Ctx.Stack.Push(!first.Equals(second));
					}
					else
					{
						// ??? no idea if this is what GM does
						Ctx.Stack.Push(false);
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
				return (ExecutionResult.ReturnedValue, Ctx.Stack.Pop<RValue>(VMType.v));
			case VMOpcode.CONV:
				// Ctx.Stack.Push(ConvertTypes(Ctx.Stack.Pop(instruction.TypeOne), instruction.TypeOne, instruction.TypeTwo));
				var toType = GetType(instruction.TypeTwo);
				Ctx.Stack.Push(Convert(Ctx.Stack.Pop(), toType));
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
					arguments.Args[i] = Ctx.Stack.Pop<RValue>(VMType.v).Value;
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

					Ctx.Stack.Push(new RValue(builtInFunction(arguments)));
					break;
				}

				if (ScriptResolver.Scripts.TryGetValue(instruction.FunctionName, out var scriptName))
				{
					Ctx.Stack.Push(ExecuteScript(scriptName, Ctx.Self, Ctx.ObjectDefinition, arguments: arguments));
					break;
				}

				if (ScriptResolver.ScriptFunctions.ContainsKey(instruction.FunctionName))
				{
					var (script, instructionIndex) = ScriptResolver.ScriptFunctions[instruction.FunctionName];
					Ctx.Stack.Push(ExecuteScript(script, Ctx.Self, Ctx.ObjectDefinition, arguments: arguments, startingIndex: instructionIndex));
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
				return (ExecutionResult.ReturnedValue, new RValue(Undefined.Value));
			case VMOpcode.SETOWNER:
				// seems to always push.i before
				var id = Conv<int>(Ctx.Stack.Pop());
				break;
			case VMOpcode.POPAF:
			{
				var index = Conv<int>(Ctx.Stack.Pop());
				var array = Conv<ArrayReference>(Ctx.Stack.Pop());
				var value = Conv<RValue>(Ctx.Stack.Pop());

				if (array.Array.Count <= index)
				{
					array.Array.AddRange(new object[array.Array.Count + 1 - index]);
				}

				array.Array[index] = value; // this is definitely wrong and still broke. make it work with rvalue (just make arrayreference be rvalue actually)

				if (array.IsGlobal)
				{
					VariableResolver.GlobalVariables[array.ArrayName] = new RValue(array.Array);
				}
				else
				{
					array.Instance.SelfVariables[array.ArrayName] = new RValue(array.Array);
				}

				break;
			}
			case VMOpcode.PUSHAF: 
			{
				var index = Conv<int>(Ctx.Stack.Pop());
				var array = Conv<ArrayReference>(Ctx.Stack.Pop());

				var value = array.Array[index];

				if (value is List<RValue>)
				{
					throw new NotImplementedException();
				}

				Ctx.Stack.Push(value);

				break;
			}
			case VMOpcode.CALLV:
			case VMOpcode.BREAK:
			default:
				return (ExecutionResult.Failed, $"Unknown opcode {instruction.Opcode}");
		}

		return (ExecutionResult.Success, null);
	}

	private static Type GetType(VMType type) => type switch
	{
		VMType.s => typeof(string),
		VMType.i => typeof(int),
		VMType.e => typeof(int),
		VMType.b => typeof(bool),
		VMType.d => typeof(double),
		VMType.l => typeof(long),
		VMType.v => typeof(RValue),
		_ => throw new NotImplementedException("what")
	};
	
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

	public static VMType GetTypeOfObject(object obj) => obj switch
	{
		int => VMType.i,
		short => VMType.e, // no idea????? int16s are stored in 4 bytes on the stack.
		string => VMType.s,
		bool => VMType.b,
		double => VMType.d,
		long => VMType.l,
		RValue => VMType.v,
		_ => throw new NotImplementedException($"Can't get type of {obj}")
	};
	
	// TODO: remove this and upgrade
	public static object ConvertTypes(object obj, VMType from, VMType to)
	{
		if (from == to)
		{
			return obj;
		}

		if (from == VMType.i)
		{
			if (to == VMType.b)
			{
				return (int)obj != 0;
			}
			else if (to == VMType.e)
			{
				// todo : cap to int16?
				return (int)obj;
			}
		}
		else if (from == VMType.d)
		{
			if (to == VMType.v)
			{
				return new RValue((double)obj);
			}
		}
		else if (from == VMType.s)
		{
			if (to == VMType.v)
			{
				return new RValue((string)obj);
			}
		}

		throw new NotImplementedException($"Don't know how to convert from {from} to {to}");
	}
	
	/*
	* old conv functions are below here. we should move away from these,
	* bitcasting/poptyping most of the time and conving only when we should
	*/
	
	// TODO: make this more strict, only work with the actual VMTypes (int, long, double, bool, string, RValue)
	// TODO: move Conv into opcodes so the proper types go onto the stack, rather than deferring conversion until the value is needed
	[Obsolete("use direct cast")]
	public static T Conv<T>(object obj)
	{
		if (typeof(T) != typeof(object))
		{
			return (T)Convert(obj, typeof(T));
		}
		else
		{
			return (T)Convert(obj, Activator.CreateInstance(typeof(T), null).GetType());
		}
	}

	[Obsolete("use direct cast")]
	public static object Convert(object obj, Type type)
	{
		if (obj is null)
		{
			DebugLog.LogError("null value passed into convert, should be Undefined!");
			obj = Undefined.Value;
		}

		if (type == typeof(object))
		{
			return obj;
		}

		if (type == typeof(RValue))
		{
			return new RValue(obj);
		}

		if (obj is Undefined && type == typeof(bool))
		{
			return false;
		}

		if (obj is Undefined && (type == typeof(int) || type == typeof(double) || type == typeof(long)))
		{
			return 0;
		}

		if (obj is Undefined && type == typeof(List<object>))
		{
			return new List<object>();
		}

		if (obj is Undefined)
		{
			DebugLog.LogError($"Trying to convert undefined to {type}! Current script:{currentExecutingScript.First().Name}");
			return Undefined.Value;
		}

		if (obj.GetType() == type)
		{
			return obj;
		}

		try
		{
			if (obj is RValue r)
			{
				//DebugLog.Log($"Converting RValue {r} to {type}");
				return Convert(r.Value, type);
			}
			else if (obj is string s)
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
			else if (obj is int or long)
			{
				var i = System.Convert.ToInt64(obj);

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
			else if (obj is bool b)
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
			else if (obj is double or float)
			{
				var d = System.Convert.ToDouble(obj);

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
			throw new Exception($"Exception while converting {obj} ({obj.GetType().FullName}) to {type}");
		}

		DebugLog.LogError($"Don't know how to convert {obj} ({obj.GetType().FullName}) to {type}");
		return Undefined.Value;
	}
}

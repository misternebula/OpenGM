using OpenGM.IO;
using OpenGM.Loading;
using OpenGM.SerializedFiles;
using System.Collections;
using System.Diagnostics;

namespace OpenGM.VirtualMachine;

/// <summary>
/// environment frame.
/// contains data about the current object. is pushed alongside the call frame. can have multiple of these per call frame.
/// </summary>
public class VMEnvFrame
{
	public IStackContextSelf Self = null!; // can be null for global scripts but those shouldnt run functions that need it
	public GamemakerObject GMSelf => (GamemakerObject)Self; // shortcut for cast since we do this often
	public ObjectDefinition? ObjectDefinition;

	public override string ToString()
	{
		if (Self == null)
		{
			return "NULL SELF";
		}

		if (Self is GamemakerObject gm)
		{
			var ret = $"{gm.object_index} ({gm.instanceId})\r\nStack:";
			/*
			foreach (var item in Stack)
			{
				ret += $"- {item}\r\n";
			}
			*/

			return ret;
		}
		else
		{
			return $"Self: {Self.GetType()}";
		}
	}
}

/// <summary>
/// call frame.
/// contains data about the currently executing function. is pushed alongside the environment frame.
/// </summary>
public class VMCallFrame
{
	public string CodeName = null!;
	
	public DataStack Stack = null!;
	public Dictionary<string, object?> Locals = null!;
	public object? ReturnValue;
	public EventType EventType;
	public int EventIndex;
	public FunctionDefinition? Function; // undertale doesnt have script functions, only script assets, so this will be null
}

public static partial class VMExecutor
{
	public static Stack<VMEnvFrame?> EnvStack = new();

	public static Stack<VMCallFrame> CallStack = new();
	/// <summary>
	/// the top level call frame
	/// </summary>
	public static VMCallFrame Call => CallStack.Peek();

	/// <summary>
	/// the top level environment frame, for the current object.
	/// has logic for handling goofy null frame for `with`.
	/// </summary>
	public static VMEnvFrame Self
	{
		get
		{
			var top = EnvStack.Peek();

			if (top == null)
			{
				// Null at top of stack, in WITH statement. Next value is self.

				return EnvStack.ToArray()[1]!;
			}

			return top;
		}
	}

	public static VMEnvFrame Other
	{
		get
		{
			if (EnvStack.Count == 1)
			{
				return Self;
			}

			var top = EnvStack.Peek();

			if (top == null)
			{
				// Null at top of stack, in WITH statement. Next value is self, then next value is other.
				return EnvStack.ToArray()[2]!;
			}

			var stack = EnvStack.ToArray();
			if (stack.Contains(null))
			{
				var i = 0;

				while (stack[i] != null)
				{
					i++;
				}

				i++; // we found the null, so previous one is the ctx that called PUSHENV
				return stack[i]!;
			}

			return stack[1]!;
		}
	}

	public static bool VerboseStackLogs;
	public static bool DebugMode;
	public static VMCodeInstruction? CurrentInstruction;
	
	// private static IList? _temporaryArrayStorage = null;

	public static object? ExecuteCode(VMCode? code, IStackContextSelf? obj, ObjectDefinition? objectDefinition = null, EventType eventType = EventType.None, int eventIndex = 0, object?[]? args = null)
	{
		object? defaultReturnValue = VersionManager.EngineVersion.Major == 1 ? 0 : null;
		// TODO: this actually changed to being undefined in probably 2.3? don't know how to check that rn, so just going with 2.0
		if (VersionManager.EngineVersion.Major == 1)
		{
			defaultReturnValue = 0;
		}

		if (code == null)
		{
			DebugLog.LogError($"Tried to run null code!");
			return defaultReturnValue;
		}

		var codeName = code.Name; // grab script function name and use that instead of script asset name
		var instructionIndex = 0;
		FunctionDefinition? func = null;
		if (code.ParentAssetId != -1) // deltarune calls script functions (empty code) that point to the script asset
		{
			var parentCode = GameLoader.Codes[code.ParentAssetId];
			// TODO: potentially speed up lookup here, profile to see if thats needed
			func = parentCode.Functions.First(x => x.FunctionName == codeName);
			instructionIndex = func.InstructionIndex;
			code = parentCode;
		}

		if (code.Instructions.Count == 0)
		{
			return defaultReturnValue;
		}

		if (VerboseStackLogs)
		{
			//if (!script.IsGlobalInit)
			//{
			var space = "   ";
			var count = CallStack.Count;
			var leftPadding = string.Concat(Enumerable.Repeat(space, count));

			DebugLog.LogInfo($"{leftPadding}------------------------------ {codeName} ------------------------------ ");
			//}
		}

		var newCtx = new VMEnvFrame
		{
			Self = obj!,
			ObjectDefinition = objectDefinition,
		};

		// Make the current object the current instance
		EnvStack.Push(newCtx);

		var lastJumpedLabel = 0; // just for debugging

		var call = new VMCallFrame
		{
			CodeName = codeName,
			
			Stack = new(),
			Locals = new(),
			ReturnValue = defaultReturnValue,
			EventType = eventType,
			EventIndex = eventIndex,
			Function = func
		};
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

				if (VerboseStackLogs && Self != null)
				{
					var stackStr = "{ ";
					foreach (var item in Call.Stack)
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
				DebugLog.LogError($"Execution of instruction {code.Instructions[instructionIndex].Raw} (Index: {instructionIndex}, Last jumped label: {lastJumpedLabel}) in script {codeName} failed : {data}");

				DebugLog.LogError($"--Stacktrace--");
				foreach (var item in CallStack)
				{
					DebugLog.LogError($" - {item.CodeName}");
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
				Call.ReturnValue = data;
				break;
			}
		}

		// Current object has finished executing, remove from stack
		var returnValue = Call.ReturnValue;
		EnvStack.Pop();

		CallStack.Pop();

		if (VerboseStackLogs)
		{
			//if (!script.IsGlobalInit)
			//{
			var space = "   ";
			var count = CallStack.Count;
			var leftPadding = string.Concat(Enumerable.Repeat(space, count));

			DebugLog.LogInfo($"{leftPadding}-#-#-#-#-#-#-#-#-#-#-#-#-#-#-- {codeName} --#-#-#-#-#-#-#-#-#-#-#-#-#-#- ");
			//}
		}

		return returnValue;
	}

	// BUG: throws sometimes instead of returning ExecutionResult.Failure
	public static (ExecutionResult result, object? data) ExecuteInstruction(VMCodeInstruction instruction)
	{
		if (VerboseStackLogs) 
			DebugLog.LogInfo($" - {instruction.Raw}");
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
					var boolValue = Call.Stack.Pop(VMType.b).Conv<bool>();
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
					var boolValue = Call.Stack.Pop(VMType.b).Conv<bool>();
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

				var second = Call.Stack.Pop(instruction.TypeOne);
				var first = Call.Stack.Pop(instruction.TypeTwo);

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
							Call.Stack.Push(CustomMath.ApproxLessThan(firstNumber, secondNumber), VMType.b);
							break;
						case VMComparison.LTE:
							Call.Stack.Push(CustomMath.ApproxLessThanEqual(firstNumber, secondNumber), VMType.b);
							break;
						case VMComparison.EQ:
							Call.Stack.Push(equal, VMType.b);
							break;
						case VMComparison.NEQ:
							Call.Stack.Push(!equal, VMType.b);
							break;
						case VMComparison.GTE:
							Call.Stack.Push(CustomMath.ApproxGreaterThanEqual(firstNumber, secondNumber), VMType.b);
							break;
						case VMComparison.GT:
							Call.Stack.Push(CustomMath.ApproxGreaterThan(firstNumber, secondNumber), VMType.b);
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
							Call.Stack.Push(true, VMType.b);
						}
						else if (first is null || second is null)
						{
							Call.Stack.Push(false, VMType.b);
						}
						else
						{
							Call.Stack.Push(first?.Equals(second), VMType.b);
						}
					}
					else if (instruction.Comparison == VMComparison.NEQ)
					{
						if (first is null && second is null)
						{
							Call.Stack.Push(false, VMType.b);
						}
						else if (first is null || second is null)
						{
							Call.Stack.Push(true, VMType.b);
						}
						else
						{
							Call.Stack.Push(!first?.Equals(second), VMType.b);
						}
					}
					else
					{
						var firstValue = first is string s1 ? double.Parse(s1) : first.Conv<double>();
						var secondValue = second is string s2 ? double.Parse(s2) : second.Conv<double>();

						switch (instruction.Comparison)
						{
							case VMComparison.LT:
								Call.Stack.Push(CustomMath.ApproxLessThan(firstValue, secondValue), VMType.b);
								break;
							case VMComparison.LTE:
								Call.Stack.Push(CustomMath.ApproxLessThanEqual(firstValue, secondValue), VMType.b);
								break;
							case VMComparison.GTE:
								Call.Stack.Push(CustomMath.ApproxGreaterThanEqual(firstValue, secondValue), VMType.b);
								break;
							case VMComparison.GT:
								Call.Stack.Push(CustomMath.ApproxGreaterThan(firstValue, secondValue), VMType.b);
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
				return (ExecutionResult.ReturnedValue, Call.Stack.Pop(VMType.v));
			case VMOpcode.CONV:
				// dont actually convert, just tell the stack we're a different type
				// since we have to conv everywhere else later anyway with rvalue
				Call.Stack.Push(Call.Stack.Pop(instruction.TypeOne), instruction.TypeTwo);
				break;
			case VMOpcode.POPZ:
				Call.Stack.Pop(instruction.TypeOne);
				break;
			case VMOpcode.CALL:
			{
				var args = new object?[instruction.FunctionArgumentCount];

				for (var i = 0; i < instruction.FunctionArgumentCount; i++)
				{
					// args are always pushed as rvalues
					args[i] = Call.Stack.Pop(VMType.v);
				}

				if (ScriptResolver.ScriptsByName.TryGetValue(instruction.FunctionName, out var scriptName))
				{
					Call.Stack.Push(ExecuteCode(scriptName.GetCode(), Self.Self, Self.ObjectDefinition, args: args), VMType.v);
					break;
				}

				if (ScriptResolver.BuiltInFunctions.TryGetValue(instruction.FunctionName, out var builtInFunction))
				{
					Call.Stack!.Push(builtInFunction!(args), VMType.v);
					break;
				}

				return (ExecutionResult.Failed, $"Can't resolve script {instruction.FunctionName} !");
			}
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
				
				var (index, type) = Call.Stack.Peek();

				if (index is int || type is VMType.i || type is VMType.e) // do we check type idk
				{
					break;
				}

				throw new Exception($"CHKINDEX failed - {index} ({type})");
			}
            case VMOpcode.CHKNULLISH:
			{
				// TODO: is this right? remains to be seen
				var value = Call.Stack.Pop(VMType.v);
				Call.Stack.Push(value == null, VMType.b);
				break;
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
				var id = Call.Stack.Pop(VMType.i).Conv<int>();
				break;
			case VMOpcode.POPAF:
			{
				var index = Call.Stack.Pop(VMType.i).Conv<int>();
				var array = Call.Stack.Pop(VMType.v).Conv<IList>();
				
				var value = Call.Stack.Pop(VMType.v);
				
				// by the magic of reference types this will be set properly
				VariableResolver.ArraySet(index, value,
					() => array,
					_ => throw new UnreachableException("this is called when getter is null"));

				break;
			}
			case VMOpcode.PUSHAF: 
			{
				var index = Call.Stack.Pop(VMType.i).Conv<int>();
				var array = Call.Stack.Pop(VMType.v).Conv<IList>();

				var value = array[index];

				Call.Stack.Push(value, VMType.v);

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
			{
				var method = Call.Stack.Pop(VMType.v) as Method;
				var self = Call.Stack.Pop(VMType.v).Conv<int>(); // TODO: if method.inst is null, use this as self (https://manual.gamemaker.io/lts/en/GameMaker_Language/GML_Reference/Variable_Functions/method_get_self.htm)

				var args = new object?[instruction.IntData];

				for (var i = 0; i < instruction.IntData; i++)
				{
					// args are always pushed as rvalues
					args[i] = Call.Stack.Pop(VMType.v);
				}

				if (method == null)
				{
					throw new NotImplementedException("method is null");
				}

				//DebugLog.LogInfo($"CALLV {method.code.Name} self:{gmSelf.Definition.Name} argCount:{args.Length}");

				Call.Stack.Push(ExecuteCode(method.func.GetCode(), method.inst, method.inst is GamemakerObject gml ? gml.Definition : null, args: args), VMType.v);

				break;
			}
			case VMOpcode.ISSTATICOK:
			{
				var currentFunc = Call.Function;

				if (currentFunc == null)
				{
					// uhhhhh fuckin uhhh
					throw new NotImplementedException();
				}

				Call.Stack.Push(currentFunc.HasStaticInitRan, VMType.b);
				break;
			}
			case VMOpcode.SETSTATIC:
			{
				var currentFunc = Call.Function;

				if (currentFunc == null)
				{
					// uhhhhh fuckin uhhh
					throw new NotImplementedException();
				}

				currentFunc.HasStaticInitRan = true;
				break;
			}
			case VMOpcode.PUSHREF:
			{
				var encodedInt = instruction.IntData;

				var assetReferenceId = encodedInt & 0xFFFFFF;
				var assetReferenceType = (AssetType)(encodedInt >> 24);

				// TODO actually push an asset reference object! this is super hacky and dumb and bad and will inevitably break
				Call.Stack.Push(assetReferenceId, VMType.v);

				break;
			}
			case VMOpcode.PUSHAC:
			{
				var index = Call.Stack.Pop(VMType.i).Conv<int>();
				var array = Call.Stack.Pop(VMType.v).Conv<IList>();
				
				var value = array[index];

				Call.Stack.Push(value, VMType.v);
				break;
			}
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

			throw new ArgumentException($"Trying to convert undefined to {type}! Current script:{CallStack.First().CodeName}");
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone;

/// <summary>
/// contains data about each script execution and also environment
/// </summary>
public class VMScriptExecutionContext
{
	public GamemakerObject Self;
	public ObjectDefinition ObjectDefinition;
	public Stack<object> Stack;
	public Dictionary<string, object> Locals;
	public object ReturnValue;
	public EventType EventType;
	public uint EventIndex;

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

public class Arguments
{
	public VMScriptExecutionContext Ctx;
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

	public static object ExecuteScript(VMScript script, GamemakerObject obj, ObjectDefinition objectDefinition = null, EventType eventType = EventType.None, uint eventIndex = 0, Arguments arguments = null)
	{
		var newCtx = new VMScriptExecutionContext
		{
			Self = obj,
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
		if (arguments != null)
		{
			newCtx.Locals["arguments"] = arguments.Args.ToList();
		}

		// Make the current object the current instance
		EnvironmentStack.Push(newCtx);

		// Setup variables to start execution at label [0]
		var instructionIndex = script.Labels[0]; // this *should* always be 0, but idk.
		var lastJumpedLabel = 0; // just for debugging

		currentExecutingScript.Push(script);

		while (true)
		{
			ExecutionResult executionResult;
			object data;

			try
			{
				(executionResult, data) = ExecuteInstruction(script.Instructions[instructionIndex]);
			}
			catch (Exception e)
			{
				executionResult = ExecutionResult.Failed;
				data = e;
			}

			if (executionResult == ExecutionResult.Failed)
			{
				DebugLog.LogError($"Execution of instruction {script.Instructions[instructionIndex].Raw} (Index: {instructionIndex}, Last jumped label: {lastJumpedLabel}) in script {script.Name} failed : {data}");
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
				var label = (int)data;
				instructionIndex = script.Labels[label];
				lastJumpedLabel = label;
				continue;
			}

			if (executionResult == ExecutionResult.ReturnedValue)
			{
				Ctx.ReturnValue = data;
				break;
			}
		}

		// Current object has finished executing, remove from stack
		var returnValue = Ctx.ReturnValue;
		EnvironmentStack.Pop();

		currentExecutingScript.Pop();

		if (RoomManager.ChangeRoomAfterEventExecution)
		{
			if (RoomManager.ScriptName == null || RoomManager.ScriptName == script.Name)
			{
				RoomManager.ChangeToWaitingRoom();
			}
		}

		return returnValue;
	}

	// BUG: throws sometimes instead of returning ExecutionResult.Failure
	public static (ExecutionResult result, object data) ExecuteInstruction(VMScriptInstruction instruction)
	{
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
					var boolValue = Conv<bool>(Ctx.Stack.Pop());
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
					var boolValue = Conv<bool>(Ctx.Stack.Pop());
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

				first ??= 0;
				second ??= 0;

				if (second is bool or int or double && first is bool or int or double)
				{
					var firstNumber = Conv<double>(first);
					var secondNumber = Conv<double>(second);
					switch (instruction.Comparison)
					{
						case VMComparison.LT:
							Ctx.Stack.Push(firstNumber < secondNumber);
							break;
						case VMComparison.LTE:
							Ctx.Stack.Push(firstNumber <= secondNumber);
							break;
						case VMComparison.EQ:
							Ctx.Stack.Push(firstNumber == secondNumber);
							break;
						case VMComparison.NEQ:
							Ctx.Stack.Push(firstNumber != secondNumber);
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
				return PUSH(instruction);
			case VMOpcode.POP:
				return POP(instruction);
			case VMOpcode.RET:
				return (ExecutionResult.ReturnedValue, Ctx.Stack.Pop());
			case VMOpcode.CONV:
				var toType = GetType(instruction.TypeTwo);
				Ctx.Stack.Push(Convert(Ctx.Stack.Pop(), toType));
				break;
			case VMOpcode.POPZ:
				Ctx.Stack.Pop();
				break;
			case VMOpcode.CALL:
				var arguments = new Arguments
				{
					Ctx = Ctx,
					Args = new object[instruction.FunctionArgumentCount]
				};

				for (var i = 0; i < instruction.FunctionArgumentCount; i++)
				{
					arguments.Args[i] = Ctx.Stack.Pop();
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

					Ctx.Stack.Push(builtInFunction(arguments));
					break;
				}

				if (ScriptResolver.Scripts.TryGetValue(instruction.FunctionName, out var scriptName))
				{
					Ctx.Stack.Push(ExecuteScript(scriptName, Ctx.Self, Ctx.ObjectDefinition, arguments: arguments));
					break;
				}

				return (ExecutionResult.Failed, $"Can't resolve script {instruction.FunctionName} !");
			case VMOpcode.PUSHENV:
				return PUSHENV(instruction);
			case VMOpcode.POPENV:
				return POPENV(instruction);
			case VMOpcode.DUP:
				{
					var indexBack = instruction.IntData;

					for (var i = 0; i <= indexBack; i++)
					{
						Ctx.Stack.Push(Ctx.Stack.Skip(indexBack).First());
					}

					break;
				}
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
				var index = Ctx.Stack.Peek();

				if (index is int)
				{
					break;
				}

				throw new Exception($"CHKINDEX failed - {index}");
			case VMOpcode.CALLV:
			case VMOpcode.BREAK:
			case VMOpcode.EXIT:
			default:
				return (ExecutionResult.Failed, $"Unknown opcode {instruction.Opcode}");
		}

		return (ExecutionResult.Success, null);
	}

	private static Type GetType(VMType type)
	{
		switch (type)
		{
			case VMType.s:
				return typeof(string);
			case VMType.i:
				return typeof(int);
			case VMType.b:
				return typeof(bool);
			case VMType.d:
				return typeof(double);
			case VMType.l:
				return typeof(long);
			case VMType.e:
			case VMType.v:
			default:
				return typeof(object);
		}
	}

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
	public static object Convert(object obj, Type type)
	{
		if (type == typeof(object))
		{
			return obj;
		}

		if (obj is null && type == typeof(bool))
		{
			return false;
		}

		if (obj is null && (type == typeof(int) || type == typeof(double)))
		{
			return 0;
		}

		if (obj.GetType() == type)
		{
			return obj;
		}

		try
		{
			if (obj is string s)
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
		return default;
	}
}

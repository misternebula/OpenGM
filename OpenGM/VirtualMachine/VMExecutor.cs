using OpenGM.IO;
using OpenGM.Loading;
using OpenGM.SerializedFiles;
using System.Collections;
using System.Diagnostics;
using UndertaleModLib.Models;

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
    public static UndertaleInstruction? CurrentInstruction;
    
    // private static IList? _temporaryArrayStorage = null;

    public static object? ExecuteCode(UndertaleCode? code, IStackContextSelf? obj, ObjectDefinition? objectDefinition = null, EventType eventType = EventType.None, int eventIndex = 0, object?[]? args = null)
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

        var codeName = code.Name.Content; // grab script function name and use that instead of script asset name
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
                var lastLabel = 0;
                foreach (var (label, index) in code.Labels)
                {
                    if (index <= instructionIndex && lastLabel < label)
                    {
                        lastLabel = label;
                    }
                }

                DebugLog.LogError($"Execution of instruction {code.Instructions[instructionIndex].Raw} (Index: {instructionIndex}, Label: {lastLabel}) in script {codeName} failed : {data}");

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
    public static (ExecutionResult result, object? data) ExecuteInstruction(UndertaleInstruction instruction)
    {
        if (VerboseStackLogs) 
            DebugLog.LogInfo($" - {instruction}");
        CurrentInstruction = instruction;

        switch (instruction.Kind)
        {
            case UndertaleInstruction.Opcode.B:
                {
                    if (instruction.JumpToEnd)
                    {
                        return (ExecutionResult.JumpedToEnd, null);
                    }

                    return (ExecutionResult.JumpedToLabel, instruction.ValueInt);
                }
            case UndertaleInstruction.Opcode.Bt:
                {
                    var boolValue = Call.Stack.Pop(UndertaleInstruction.DataType.Boolean).Conv<bool>();
                    if (!boolValue)
                    {
                        break;
                    }

                    if (instruction.JumpToEnd)
                    {
                        return (ExecutionResult.JumpedToEnd, null);
                    }

                    return (ExecutionResult.JumpedToLabel, instruction.ValueInt);
                }
            case UndertaleInstruction.Opcode.Bf:
                {
                    var boolValue = Call.Stack.Pop(UndertaleInstruction.DataType.Boolean).Conv<bool>();
                    if (boolValue)
                    {
                        break;
                    }

                    if (instruction.JumpToEnd)
                    {
                        return (ExecutionResult.JumpedToEnd, null);
                    }

                    return (ExecutionResult.JumpedToLabel, instruction.ValueInt);
                }
            case UndertaleInstruction.Opcode.Cmp:

                var second = Call.Stack.Pop(instruction.Type1);
                var first = Call.Stack.Pop(instruction.Type2);

                // first ??= 0;
                // second ??= 0;
                
                // TODO: array and undefined cmp

                if (second is bool or int or short or long or double or float && first is bool or int or short or long or double or float)
                {
                    var firstNumber = first.Conv<double>();
                    var secondNumber = second.Conv<double>();

                    var equal = CustomMath.ApproxEqual(firstNumber, secondNumber);

                    switch (instruction.ComparisonKind)
                    {
                        case UndertaleInstruction.ComparisonType.LT:
                            Call.Stack.Push(CustomMath.ApproxLessThan(firstNumber, secondNumber), UndertaleInstruction.DataType.Boolean);
                            break;
                        case UndertaleInstruction.ComparisonType.LTE:
                            Call.Stack.Push(CustomMath.ApproxLessThanEqual(firstNumber, secondNumber), UndertaleInstruction.DataType.Boolean);
                            break;
                        case UndertaleInstruction.ComparisonType.EQ:
                            Call.Stack.Push(equal, UndertaleInstruction.DataType.Boolean);
                            break;
                        case UndertaleInstruction.ComparisonType.NEQ:
                            Call.Stack.Push(!equal, UndertaleInstruction.DataType.Boolean);
                            break;
                        case UndertaleInstruction.ComparisonType.GTE:
                            Call.Stack.Push(CustomMath.ApproxGreaterThanEqual(firstNumber, secondNumber), UndertaleInstruction.DataType.Boolean);
                            break;
                        case UndertaleInstruction.ComparisonType.GT:
                            Call.Stack.Push(CustomMath.ApproxGreaterThan(firstNumber, secondNumber), UndertaleInstruction.DataType.Boolean);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    // this should handle strings and whatever else

                    if (instruction.ComparisonKind == UndertaleInstruction.ComparisonType.EQ)
                    {
                        if (first is null && second is null)
                        {
                            Call.Stack.Push(true, UndertaleInstruction.DataType.Boolean);
                        }
                        else if (first is null || second is null)
                        {
                            Call.Stack.Push(false, UndertaleInstruction.DataType.Boolean);
                        }
                        else
                        {
                            Call.Stack.Push(first?.Equals(second), UndertaleInstruction.DataType.Boolean);
                        }
                    }
                    else if (instruction.ComparisonKind == UndertaleInstruction.ComparisonType.NEQ)
                    {
                        if (first is null && second is null)
                        {
                            Call.Stack.Push(false, UndertaleInstruction.DataType.Boolean);
                        }
                        else if (first is null || second is null)
                        {
                            Call.Stack.Push(true, UndertaleInstruction.DataType.Boolean);
                        }
                        else
                        {
                            Call.Stack.Push(!first?.Equals(second), UndertaleInstruction.DataType.Boolean);
                        }
                    }
                    else
                    {
                        var firstValue = first is string s1 ? double.Parse(s1) : first.Conv<double>();
                        var secondValue = second is string s2 ? double.Parse(s2) : second.Conv<double>();

                        switch (instruction.ComparisonKind)
                        {
                            case UndertaleInstruction.ComparisonType.LT:
                                Call.Stack.Push(CustomMath.ApproxLessThan(firstValue, secondValue), UndertaleInstruction.DataType.Boolean);
                                break;
                            case UndertaleInstruction.ComparisonType.LTE:
                                Call.Stack.Push(CustomMath.ApproxLessThanEqual(firstValue, secondValue), UndertaleInstruction.DataType.Boolean);
                                break;
                            case UndertaleInstruction.ComparisonType.GTE:
                                Call.Stack.Push(CustomMath.ApproxGreaterThanEqual(firstValue, secondValue), UndertaleInstruction.DataType.Boolean);
                                break;
                            case UndertaleInstruction.ComparisonType.GT:
                                Call.Stack.Push(CustomMath.ApproxGreaterThan(firstValue, secondValue), UndertaleInstruction.DataType.Boolean);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        //return (ExecutionResult.Failed, $"cant cmp {instruction.Comparison} on {first?.GetType()} {first} and {second?.GetType()} {second}");
                    }
                }
                break;
            case UndertaleInstruction.Opcode.PushGlb:
            case UndertaleInstruction.Opcode.PushLoc:
            case UndertaleInstruction.Opcode.PushBltn:
            case UndertaleInstruction.Opcode.PushI:
            case UndertaleInstruction.Opcode.Push:
                return DoPush(instruction);
            case UndertaleInstruction.Opcode.Pop:
                return DoPop(instruction);
            case UndertaleInstruction.Opcode.Ret:
                // ret value is always stored as rvalue
                return (ExecutionResult.ReturnedValue, Call.Stack.Pop(UndertaleInstruction.DataType.Variable));
            case UndertaleInstruction.Opcode.Conv:
                // dont actually convert, just tell the stack we're a different type
                // since we have to conv everywhere else later anyway with rvalue
                Call.Stack.Push(Call.Stack.Pop(instruction.Type1), instruction.Type2);
                break;
            case UndertaleInstruction.Opcode.Popz:
                Call.Stack.Pop(instruction.Type1);
                break;
            case UndertaleInstruction.Opcode.Call:
            {
                var args = new object?[instruction.ValueInt];

                for (var i = 0; i < instruction.ArgumentsCount; i++)
                {
                    // args are always pushed as rvalues
                    args[i] = Call.Stack.Pop(UndertaleInstruction.DataType.Variable);
                }

                if (ScriptResolver.ScriptsByName.TryGetValue(instruction.ValueFunction.Name.Content, out var scriptName))
                {
                    Call.Stack.Push(ExecuteCode(scriptName.GetCode(), Self.Self, Self.ObjectDefinition, args: args), UndertaleInstruction.DataType.Variable);
                    break;
                }

                if (ScriptResolver.BuiltInFunctions.TryGetValue(instruction.ValueFunction.Name.Content, out var builtInFunction))
                {
                    Call.Stack!.Push(builtInFunction!(args), UndertaleInstruction.DataType.Variable);
                    break;
                }

                return (ExecutionResult.Failed, $"Can't resolve script {instruction.ValueFunction.Name.Content} !");
            }
            case UndertaleInstruction.Opcode.PushEnv:
                return PUSHENV(instruction);
            case UndertaleInstruction.Opcode.PopEnv:
                return POPENV(instruction);
            case UndertaleInstruction.Opcode.Dup:
                return DoDup(instruction);
            case UndertaleInstruction.Opcode.Add:
                return ADD(instruction);
            case UndertaleInstruction.Opcode.Sub:
                return SUB(instruction);
            case UndertaleInstruction.Opcode.Mul:
                return MUL(instruction);
            case UndertaleInstruction.Opcode.Div:
                return DIV(instruction);
            case UndertaleInstruction.Opcode.Rem:
                return REM(instruction);
            case UndertaleInstruction.Opcode.Mod:
                return MOD(instruction);
            case UndertaleInstruction.Opcode.Neg:
                return NEG(instruction);
            case UndertaleInstruction.Opcode.And:
                return AND(instruction);
            case UndertaleInstruction.Opcode.Or:
                return OR(instruction);
            case UndertaleInstruction.Opcode.Xor:
                return XOR(instruction);
            case UndertaleInstruction.Opcode.Not:
                return NOT(instruction);
            case UndertaleInstruction.Opcode.Shl:
                return SHL(instruction);
            case UndertaleInstruction.Opcode.Shr:
                return SHR(instruction);
            case UndertaleInstruction.Opcode.CHKINDEX:
            {
                // unused in ch2???? no clue what this does
                // used for multi-dimensional array bounds checking. c# does that anyway so its probably fine
                
                var (index, type) = Call.Stack.Peek();

                if (index is int || type is UndertaleInstruction.DataType.Int32 || type is UndertaleInstruction.DataType.Int16) // do we check type idk
                {
                    break;
                }

                throw new Exception($"CHKINDEX failed - {index} ({type})");
            }
            case UndertaleInstruction.Opcode.CHKNULLISH:
            {
                // TODO: is this right? remains to be seen
                var value = Call.Stack.Pop(UndertaleInstruction.DataType.Variable);
                Call.Stack.Push(value == null, UndertaleInstruction.DataType.Boolean);
                break;
            }
            case UndertaleInstruction.Opcode.Exit:
                return (ExecutionResult.ReturnedValue, instruction.Type1 switch
                {
                    UndertaleInstruction.DataType.Int32 or UndertaleInstruction.DataType.Int16 or UndertaleInstruction.DataType.Double or UndertaleInstruction.DataType.Int64 => 0,
                    UndertaleInstruction.DataType.Boolean => false,
                    UndertaleInstruction.DataType.String => "",
                    UndertaleInstruction.DataType.Variable => null,
                    _ => throw new ArgumentOutOfRangeException()
                });
            case UndertaleInstruction.Opcode.SETOWNER:
                // seems to always push.i before
                // apparently used for COW array stuff. does that mean this subtley breaks everything because arrays expect to copy?
                var id = Call.Stack.Pop(UndertaleInstruction.DataType.Int32).Conv<int>();
                break;
            case UndertaleInstruction.Opcode.POPAF:
            {
                var index = Call.Stack.Pop(UndertaleInstruction.DataType.Int32).Conv<int>();
                var array = Call.Stack.Pop(UndertaleInstruction.DataType.Variable).Conv<IList>();
                
                var value = Call.Stack.Pop(UndertaleInstruction.DataType.Variable);
                
                // by the magic of reference types this will be set properly
                VariableResolver.ArraySet(index, value,
                    () => array,
                    _ => throw new UnreachableException("this is called when getter is null"));

                break;
            }
            case UndertaleInstruction.Opcode.PUSHAF: 
            {
                var index = Call.Stack.Pop(UndertaleInstruction.DataType.Int32).Conv<int>();
                var array = Call.Stack.Pop(UndertaleInstruction.DataType.Variable).Conv<IList>();

                var value = array[index];

                Call.Stack.Push(value, UndertaleInstruction.DataType.Variable);

                break;
            }
            case UndertaleInstruction.Opcode.SAVEAREF:
            {
                // doing what the comment in Underanalyzer says makes everything break???
                
                // if (_temporaryArrayStorage != null) throw new Exception("savearef - array already stored");
                // var wtfIsThis = Ctx.Stack.Pop(UndertaleInstruction.DataType.Int32);
                // _temporaryArrayStorage = Ctx.Stack.Pop(UndertaleInstruction.DataType.Variable).Conv<IList>();
                break;
            }
            case UndertaleInstruction.Opcode.RESTOREAREF:
            {
                // doing what the comment in Underanalyzer says makes everything break???
                
                // if (_temporaryArrayStorage == null) throw new Exception("savearef - array not stored");
                // Ctx.Stack.Push(_temporaryArrayStorage, UndertaleInstruction.DataType.Variable);
                // _temporaryArrayStorage = null;
                break;
            }
            case UndertaleInstruction.Opcode.CallV:
            {
                var method = Call.Stack.Pop(UndertaleInstruction.DataType.Variable) as Method;
                var self = Call.Stack.Pop(UndertaleInstruction.DataType.Variable).Conv<int>(); // TODO: if method.inst is null, use this as self (https://manual.gamemaker.io/lts/en/GameMaker_Language/GML_Reference/Variable_Functions/method_get_self.htm)

                var args = new object?[instruction.ValueInt];

                for (var i = 0; i < instruction.ValueInt; i++)
                {
                    // args are always pushed as rvalues
                    args[i] = Call.Stack.Pop(UndertaleInstruction.DataType.Variable);
                }

                if (method == null)
                {
                    throw new NotImplementedException("method is null");
                }

                //DebugLog.LogInfo($"CALLV {method.code.Name} self:{gmSelf.Definition.Name} argCount:{args.Length}");

                Call.Stack.Push(ExecuteCode(method.func.GetCode(), method.inst, method.inst is GamemakerObject gml ? gml.Definition : null, args: args), UndertaleInstruction.DataType.Variable);

                break;
            }
            case UndertaleInstruction.Opcode.ISSTATICOK:
            {
                var currentFunc = Call.Function;

                if (currentFunc == null)
                {
                    // uhhhhh fuckin uhhh
                    throw new NotImplementedException();
                }

                Call.Stack.Push(currentFunc.HasStaticInitRan, UndertaleInstruction.DataType.Boolean);
                break;
            }
            case UndertaleInstruction.Opcode.SETSTATIC:
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
            case UndertaleInstruction.Opcode.PUSHREF:
            {
                var encodedInt = instruction.ValueInt;

                var assetReferenceId = encodedInt & 0xFFFFFF;
                var assetReferenceType = (AssetType)(encodedInt >> 24);

                // TODO actually push an asset reference object! this is super hacky and dumb and bad and will inevitably break
                Call.Stack.Push(assetReferenceId, UndertaleInstruction.DataType.Variable);

                break;
            }
            case UndertaleInstruction.Opcode.PUSHAC:
            {
                var index = Call.Stack.Pop(UndertaleInstruction.DataType.Int32).Conv<int>();
                var array = Call.Stack.Pop(UndertaleInstruction.DataType.Variable).Conv<IList>();
                
                var value = array[index];

                Call.Stack.Push(value, UndertaleInstruction.DataType.Variable);
                break;
            }
            case UndertaleInstruction.Opcode.Break:
                throw new UnreachableException("break is used as an extended opcode marker, so it should never show up as an instruction");
            default:
                return (ExecutionResult.Failed, $"Unknown opcode {instruction.Kind}");
        }

        return (ExecutionResult.Success, null);
    }

    public static int VMTypeToSize(UndertaleInstruction.DataType type) => type switch
    {
        UndertaleInstruction.DataType.Variable => 16,
        UndertaleInstruction.DataType.Double => 8,
        UndertaleInstruction.DataType.Int64 => 8,
        UndertaleInstruction.DataType.Int32 => 4,
        UndertaleInstruction.DataType.Boolean => 4,
        UndertaleInstruction.DataType.String => 4,
        UndertaleInstruction.DataType.Int16 => 4,
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

            if (type == typeof(bool))
            {
                if (bool.TryParse(s, out var result))
                {
                    return result;
                }
                else if (s == "1")
                {
                    return true;
                }
                else if (s == "0")
                {
                    return false;
                }
            }
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
        else if (@this is Method)
        {
            if (type == typeof(bool)) return true; // methods are always evaluated to true i think?
        }
            
        throw new ArgumentException($"Don't know how to convert {@this} ({@this.GetType().FullName}) to {type}");
    }
}

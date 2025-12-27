using OpenGM.IO;
using OpenGM.Loading;
using OpenGM.SerializedFiles;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;

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
            var ret = $"{gm.Definition.Name} ({gm.object_index}, instance {gm.instanceId})";
            return ret;
        }
        else if (Self is GMLObject obj)
        {
            return "[" + obj.ToString() + "]";
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
    public VMEnvFrame EnvFrame = null!;
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
    public static bool ForceVerboseStackLogs = false;
    public static bool DebugMode;
    public static Regex? ScriptFilter;
    public static VMCodeInstruction? CurrentInstruction;

    /// <summary>
    /// True if we are currently executing Global Init code.
    /// </summary>
    public static bool GlobalInit;
    
    // private static IList? _temporaryArrayStorage = null;

    public static object? ExecuteCode(VMCode? code, IStackContextSelf? obj, ObjectDefinition? objectDefinition = null, EventType eventType = EventType.None, int eventIndex = 0, object?[]? args = null)
    {
        object? defaultReturnValue = CompatFlags.ZeroReturnValue ? 0 : null;

        if (code == null)
        {
            DebugLog.LogError($"Tried to run null code!");
            return defaultReturnValue;
        }

        if (ScriptFilter?.IsMatch(code.Name) ?? false)
        {
            DebugLog.LogVerbose($"Script name \"{code.Name}\" matches filter, returning.");
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

        var newCtx = new VMEnvFrame
        {
            Self = obj!,
            ObjectDefinition = objectDefinition,
        };

        // Make the current object the current instance
        EnvStack.Push(newCtx);

        if (VerboseStackLogs)
        {
            //if (!script.IsGlobalInit)
            //{
            var space = "   ";
            var count = CallStack.Count;
            var leftPadding = string.Concat(Enumerable.Repeat(space, count));

            DebugLog.LogInfo($"{leftPadding}------------------------------ {codeName} ------------------------------ ");
            DebugLog.LogInfo($"ENV STACK:");

            var i = 1;
            foreach (var env in EnvStack)
            {
                DebugLog.LogInfo($"{i}. {env?.ToString() ?? "null"}");
                i++;
            }

            DebugLog.LogInfo($"");
        }

        var call = new VMCallFrame
        {
            CodeName = codeName,
            
            Stack = new(),
            Locals = new(),
            ReturnValue = defaultReturnValue,
            EventType = eventType,
            EventIndex = eventIndex,
            Function = func,
            EnvFrame = newCtx
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

            if (CallStack == null || CallStack.Count == 0)
            {
                DebugLog.LogWarning("CallStack is null or empty! Returning...");
                return null;
            }

            try
            {
                (executionResult, data) = ExecuteInstruction(code.Instructions[instructionIndex]);

                if (VerboseStackLogs && Self != null)
                {
                    var stackStr = "{ ";
                    foreach (var item in Call.Stack)
                    {
                        if (item.value is string str)
                        {
                            if (str.Length > 80)
                            {
                                str = str[..80] + $"[{str.Length - 80} more characters...]";
                                str = str.Replace("\n", "\\n");
                            }

                            stackStr += $"{str}, ";
                            continue;
                        }

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
                DebugLog.PrintCallStack(DebugLog.LogType.Error);

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
                
                // TODO: array cmp

                if (first == null || second == null)
                {
                    switch (instruction.Comparison)
                    {
                        case VMComparison.LT:
                        case VMComparison.GT:
                            Call.Stack.Push(false, VMType.b);
                            break;
                        case VMComparison.LTE:
                        case VMComparison.GTE:
                        case VMComparison.EQ:
                            Call.Stack.Push((first == null) && (second == null), VMType.b);
                            break;
                        case VMComparison.NEQ:
                            Call.Stack.Push((first == null) != (second == null), VMType.b);
                            break;
                        case VMComparison.None:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (second is bool or int or short or long or double or float && first is bool or int or short or long or double or float)
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

                    double firstDouble;
                    double secondDouble;
                    bool firstIsNumerical;
                    bool secondIsNumerical;

                    if (first is bool or int or short or long or double or float)
                    {
                        firstDouble = first.Conv<double>();
                        firstIsNumerical = true;
                    }
                    else
                    {
                        firstIsNumerical = double.TryParse(first as string, out firstDouble);
                    }

                    if (second is bool or int or short or long or double or float)
                    {
                        secondDouble = second.Conv<double>();
                        secondIsNumerical = true;
                    }
                    else
                    {
                        secondIsNumerical = double.TryParse(second as string, out secondDouble);
                    }

                    if (firstIsNumerical && secondIsNumerical)
                    {
                        // both represent numbers - easy comparison

                        var equal = CustomMath.ApproxEqual(firstDouble, secondDouble);

                        switch (instruction.Comparison)
                        {
                            case VMComparison.LT:
                                Call.Stack.Push(CustomMath.ApproxLessThan(firstDouble, secondDouble), VMType.b);
                                break;
                            case VMComparison.LTE:
                                Call.Stack.Push(CustomMath.ApproxLessThanEqual(firstDouble, secondDouble), VMType.b);
                                break;
                            case VMComparison.EQ:
                                Call.Stack.Push(equal, VMType.b);
                                break;
                            case VMComparison.NEQ:
                                Call.Stack.Push(!equal, VMType.b);
                                break;
                            case VMComparison.GTE:
                                Call.Stack.Push(CustomMath.ApproxGreaterThanEqual(firstDouble, secondDouble), VMType.b);
                                break;
                            case VMComparison.GT:
                                Call.Stack.Push(CustomMath.ApproxGreaterThan(firstDouble, secondDouble), VMType.b);
                                break;
                            case VMComparison.None:
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (firstIsNumerical != secondIsNumerical)
                    {
                        // only one represents a number - only EQ and NEQ is valid here

                        // TODO: can these ever be anything other than these hardcoded values?
                        if (instruction.Comparison == VMComparison.EQ)
                        {
                            Call.Stack.Push(false, VMType.b);
                        }
                        else if (instruction.Comparison == VMComparison.NEQ)
                        {
                            Call.Stack.Push(true, VMType.b);
                        }
                        else
                        {
                            throw new NotImplementedException("Only EQ and NEQ is valid between a numerical value and a non-numerical value.");
                        }
                    }
                    else
                    {
                        // neither represent a number - pure string comparison

                        var s1 = first as string;
                        var s2 = second as string;

                        if (instruction.Comparison == VMComparison.EQ)
                        {
                            Call.Stack.Push(s1 == s2, VMType.b);
                        }
                        else if (instruction.Comparison == VMComparison.NEQ)
                        {
                            Call.Stack.Push(s1 != s2, VMType.b);
                        }
                        else
                        {
                            // TODO: implement
                            throw new NotImplementedException();
                        }
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
                    () => array);

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
                var method = FetchMethod(Call.Stack.Pop(VMType.v));
                var self = Call.Stack.Pop(VMType.v);

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

                var context = method.inst;
                if (method.inst is null && self is not null)
                {
                    context = FetchSelf(self);
                }

                //DebugLog.LogInfo($"CALLV {method.code.Name} self:{gmSelf.Definition.Name} argCount:{args.Length}");

                Call.Stack.Push(ExecuteCode(method.func.GetCode(), context, method.inst is GamemakerObject gml ? gml.Definition : null, args: args), VMType.v);

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

    public static IStackContextSelf? FetchSelf(object? value)
    {
        if (value is null)
        {
            throw new ArgumentException($"Trying to fetch IStackContextSelf for undefined! Current script:{CallStack.First().CodeName}");
        }
        else if (value is IStackContextSelf self)
        {
            return self;
        }
        else if (value is int or long or short)
        {
            return InstanceManager.Find((int)value, all: true);
        }

        throw new ArgumentException($"Don't know how to fetch IStackContextSelf for {value} ({value.GetType().FullName})");
    }

    internal static Method? FetchMethod(object? value)
    {
        if (value is null)
        {
            throw new ArgumentException($"Trying to fetch method for undefined! Current script:{CallStack.First().CodeName}");
        }
        else if (value is Method method)
        {
            return method;
        }
        else if (value is int or long or short)
        {
            // i really hope built in functions cant be used with method
            return new Method(ScriptResolver.ScriptsByIndex[value.Conv<int>() - GMConstants.FIRST_INSTANCE_ID]);
        }

        throw new ArgumentException($"Don't know how to fetch method for {value} ({value.GetType().FullName})");
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

    public static object? DictHash(object? key)
    {
        // GenHash in cpp
        
        var result = key;

        switch (key)
        {
            case int or long or short or double or bool:
                result = key.Conv<double>();
                break;
        }

        return result;
    }

    /// <summary>
    /// custom cast function.
    /// works with: all number types, string, bool, IList
    /// </summary>
    public static T Conv<T>(this object? @this) => (T)@this.Conv(typeof(T));
    
    public static object Conv(this object? @this, Type type)
    {
        /*
         * FOR REFERENCE
         * 
         * rvalue kinds via KindName:
         * 0: number
         * 1: string
         * 2: array
         * 3: ptr
         * 4: vec3
         * 5: undefined
         * 6: struct (or method if its callable i.e. YYObjectBase is CScriptRef)
         * 7: int32
         * 8: vec4
         * 9: vec44 (matrix44)
         * 10: int64
         * 11: accessor
         * 12: null
         * 13: bool
         * 14: iterator
         *
         * actual converters:
         * YYGetBool / BOOL_RValue
         * YYGetInt32 / INT32_RValue
         * YYGetInt64 / INT64_RValue
         * YYGetReal / REAL_RValue
         * YYGetFloat
         * YYGetString
         * YYGetStruct
         * YYGetUint32
         * YYGetPtr / PTR_RValue
         * YYGetPtrOrInt
         */
        
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
        else if (@this is int or long or short or Enum or uint)
        {
            var l = Convert.ToInt64(@this);

            if (type == typeof(uint)) return (uint)l;
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
            
        throw new ArgumentException($"Don't know how to convert {@this} ({@this.GetType()}) to {type}");
    }

    /// <summary>
    /// Conv elements of array. Used instead of Cast because Cast does not convert types (e.g. int to float)
    /// </summary>
    public static IEnumerable<T> ConvAll<T>(this IEnumerable @this) => @this.Cast<object?>().Select(e => e.Conv<T>());
}

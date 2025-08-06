using OpenGM.IO;
using System.Collections;

namespace OpenGM.VirtualMachine;

public static partial class VMExecutor
{
    public static void PushGlobal(string varName)
    {
        Call.Stack.Push(VariableResolver.GlobalVariables[varName], VMType.v);
    }

    public static void PushGlobalArrayIndex(string varName, int index)
    {
        var array = VariableResolver.GlobalVariables[varName].Conv<IList>();
        Call.Stack.Push(array[index], VMType.v);
    }

    public static void PushLocalArrayIndex(string varName, int index)
    {
        var array = Call.Locals[varName].Conv<IList>();
        Call.Stack.Push(array[index], VMType.v);
    }

    public static void PushLocal(string varName)
    {
        Call.Stack.Push(Call.Locals[varName], VMType.v);
    }

    public static void PushBuiltin(string varName)
    {
        if (VariableResolver.BuiltInVariables.ContainsKey(varName))
        {
            var value = VariableResolver.BuiltInVariables[varName].getter();
            Call.Stack.Push(value, VMType.v);
        }
        else if (VariableResolver.BuiltInSelfVariables.ContainsKey(varName))
        {
            var value = VariableResolver.BuiltInSelfVariables[varName].getter(Self.GMSelf);
            Call.Stack.Push(value, VMType.v);
        }
        else if (Self.Self.SelfVariables.ContainsKey(varName))
        {
            var value = Self.Self.SelfVariables[varName];
            Call.Stack.Push(value, VMType.v);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static void PushBuiltinArrayIndex(string varName, int index)
    {
        if (VariableResolver.BuiltInVariables.ContainsKey(varName))
        {
            var array = VariableResolver.BuiltInVariables[varName].getter().Conv<IList>();
            Call.Stack.Push(array[index], VMType.v);
        }
        else if (VariableResolver.BuiltInSelfVariables.ContainsKey(varName))
        {
            var array = VariableResolver.BuiltInSelfVariables[varName].getter(Self.GMSelf).Conv<IList>();
            Call.Stack.Push(array[index], VMType.v);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public static void PushSelf(IStackContextSelf self, string varName)
    {
        if (self == null)
        {
            DebugLog.LogError($"Null self given to PushSelf varName:{varName} - {VMExecutor.CurrentInstruction!.Raw}");

            DebugLog.LogError($"--Stacktrace--");
            foreach (var item in CallStack)
            {
                DebugLog.LogError($" - {item.CodeName}");
            }

            Call.Stack.Push(null, VMType.v);
            return;
        }

        if (VariableResolver.BuiltInVariables.TryGetValue(varName, out var builtin_gettersetter))
        {
            Call.Stack.Push(builtin_gettersetter.getter(), VMType.v);
        }
        else if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var selfbuiltin_gettersetter) && self is GamemakerObject gm)
        {
            Call.Stack.Push(selfbuiltin_gettersetter.getter(gm), VMType.v);
        }
        else
        {
            if (self.SelfVariables.ContainsKey(varName))
            {
                Call.Stack.Push(self.SelfVariables[varName], VMType.v);
            }
            else
            {
                if (self is GamemakerObject gmo)
                {
                    DebugLog.LogError($"Variable {varName} doesn't exist in {gmo.instanceId} {gmo.Definition.Name}, pushing undefined.");
                }
                else
                {
                    DebugLog.LogError($"Variable {varName} doesn't exist in non-GMO self, pushing undefined.");
                }

                DebugLog.LogError($"--Stacktrace--");
                foreach (var item in CallStack)
                {
                    DebugLog.LogError($" - {item.CodeName}");
                }

                self.SelfVariables[varName] = null;
                Call.Stack.Push(self.SelfVariables[varName], VMType.v);
            }
        }
    }

    public static void PushSelfArrayIndex(IStackContextSelf self, string varName, int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException($"index into {varName} is negative: {index}");
        }

        IList array;

        if (VariableResolver.BuiltInVariables.TryGetValue(varName, out var bi_gettersetter))
        {
            array = bi_gettersetter.getter().Conv<IList>();
        }
        else if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var bis_gettersetter) && self is GamemakerObject gm)
        {
            array = bis_gettersetter.getter(gm).Conv<IList>();
        }
        else
        {
            array = self.SelfVariables[varName].Conv<IList>();
        }

        if (index >= array.Count)
        {
            throw new ArgumentOutOfRangeException($"index into {varName} is bigger than array: {index} array size: {array.Count}");
        }

        Call.Stack.Push(array[index], VMType.v);
    }

    public static void PushArgument(int index)
    {
        var arguments = Call.Locals["arguments"].Conv<IList>();

        if (index >= arguments.Count)
        {
            // Scripts can be called with fewer than normal arguments.
            // They just get set to Undefined.
            Call.Stack.Push(null, VMType.v);
            return;
        }

        Call.Stack.Push(arguments[index], VMType.v);
    }

    public static void PushIndex(object? item, string varName)
    {
        var inst = FetchSelf(item);

        if (inst == null)
        {
            DebugLog.LogError($"Tried to push variable {varName} from {item} ({item?.GetType().Name ?? "null"}), which isn't a valid self!!");

            DebugLog.LogError($"--Stacktrace--");
            foreach (var stackItem in CallStack)
            {
                DebugLog.LogError($" - {stackItem.CodeName}");
            }

            DebugLog.LogError(Environment.StackTrace);

            Call.Stack.Push(null, VMType.v);
            return;
        }

        PushSelf(inst, varName);
    }

    public static void PushOther(string varName)
    {
        PushSelf(Other.Self, varName);
    }

    public static void PushStacktop(string varName)
    {
        var top = Call.Stack.Pop(VMType.v);
        PushIndex(top, varName);
    }

    public static void PushArgumentArrayIndex(string varName, int index)
    {
        var argIndex = int.Parse(varName.Replace("argument", ""));

        var arguments = Call.Locals["arguments"].Conv<IList>();

        if (argIndex >= arguments.Count)
        {
            Call.Stack.Push(null, VMType.v);
            return;
        }

        var array = arguments[argIndex].Conv<IList>();

        Call.Stack.Push(array[index], VMType.v);
    }

    public static void PushStatic(string varname)
    {
        var currentFunc = Call.Function;

        if (currentFunc == null)
        {
            // uhhhhh fuckin uhhh
            throw new NotImplementedException();
        }

        if (!currentFunc.HasStaticInitRan)
        {
            throw new NotImplementedException($"Tried to push static variable {varname} before static initialization!");
        }

        if (!currentFunc.StaticVariables.TryGetValue(varname, out var value))
        {
            throw new NotImplementedException("StaticVariables should contain every static variable after initialization!?");
        }

        Call.Stack.Push(value, VMType.v);
    }

    public static (ExecutionResult, object?) DoPush(VMCodeInstruction instruction)
    {
        switch (instruction.TypeOne)
        {
            case VMType.i:

                if (instruction.StringData != null)
                {
                    if (instruction.PushFunction)
                    {
                        Call.Stack.Push(AssetIndexManager.GetIndex(AssetType.scripts, instruction.StringData), VMType.i);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    Call.Stack.Push(instruction.IntData, VMType.i);
                }
                return (ExecutionResult.Success, null);
            case VMType.e:
                Call.Stack.Push(instruction.ShortData, VMType.e);
                return (ExecutionResult.Success, null);
            case VMType.l:
                Call.Stack.Push(instruction.LongData, VMType.l);
                return (ExecutionResult.Success, null);
            case VMType.b:
                Call.Stack.Push(instruction.BoolData, VMType.b);
                return (ExecutionResult.Success, null);
            case VMType.d:
                Call.Stack.Push(instruction.DoubleData, VMType.d);
                return (ExecutionResult.Success, null);
            case VMType.s:
                Call.Stack.Push(instruction.StringData, VMType.s);
                return (ExecutionResult.Success, null);
            case VMType.v:
                return DoPushV(instruction);
        }

        return (ExecutionResult.Failed, $"Don't know how to push {instruction.Raw}");
    }

    public static (ExecutionResult, object?) DoPushV(VMCodeInstruction instruction)
    {
        var variableName = instruction.variableName;
        var variableType = instruction.variableType;
        var variablePrefix = instruction.variablePrefix;
        var assetId = instruction.assetId;

        if (variablePrefix == VariablePrefix.None)
        {
            if (variableType == VariableType.Global)
            {
                PushGlobal(variableName);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.Local)
            {
                PushLocal(variableName);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.BuiltIn)
            {
                PushBuiltin(variableName);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.Self)
            {
                PushSelf(Self.Self, variableName);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.Argument)
            {
                var strIndex = variableName[8..]; // skip "argument"
                var index = int.Parse(strIndex);
                PushArgument(index);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.Index)
            {
                PushIndex(assetId, variableName);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.Other)
            {
                PushOther(variableName);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.Stacktop)
            {
                PushStacktop(variableName);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.Static)
            {
                PushStatic(variableName);
                return (ExecutionResult.Success, null);
            }
        }
        else if (variablePrefix == VariablePrefix.Array || variablePrefix == VariablePrefix.ArrayPopAF || variablePrefix == VariablePrefix.ArrayPushAF)
        {
            if (variablePrefix == VariablePrefix.Array)
            {
                if (variableType == VariableType.Self)
                {
                    var index = Call.Stack.Pop(VMType.i).Conv<int>();
                    var instanceId = Call.Stack.Pop(VMType.i).Conv<int>();

                    if (instanceId == GMConstants.stacktop)
                    {
                        var context = Call.Stack.Pop(VMType.v);

                        if (context is GMLObject obj)
                        {
                            PushSelfArrayIndex(obj, variableName, index);
                            return (ExecutionResult.Success, null);
                        }
                        else
                        {
                            instanceId = context.Conv<int>();
                        }
                    }

                    if (instanceId == GMConstants.global)
                    {
                        PushGlobalArrayIndex(variableName, index);
                        return (ExecutionResult.Success, null);
                    }
                    else if (instanceId == GMConstants.local)
                    {
                        PushLocalArrayIndex(variableName, index);
                        return (ExecutionResult.Success, null);
                    }
                    else if (instanceId == GMConstants.argument)
                    {
                        if (variableName == "argument")
                        {
                            PushArgument(index);
                        }
                        else
                        {
                            PushArgumentArrayIndex(variableName, index);
                        }
                        
                        return (ExecutionResult.Success, null);
                    }
                    else if (instanceId == GMConstants.self)
                    {
                        PushSelfArrayIndex(Self.Self, variableName, index);
                        return (ExecutionResult.Success, null);
                    }
                    else if (instanceId == GMConstants.builtin)
                    {
                        PushBuiltinArrayIndex(variableName, index);
                        return (ExecutionResult.Success, null);
                    }
                    else
                    {
                        if (instanceId < GMConstants.FIRST_INSTANCE_ID)
                        {
                            // asset id
                            var self = InstanceManager.FindByAssetId(instanceId).MinBy(x => x.instanceId)!;

                            if (self == null)
                            {
                                DebugLog.LogError($"Couldn't find any instances of {AssetIndexManager.GetName(AssetType.objects, instanceId)}");
                                Call.Stack.Push(null, VMType.v);
                                return (ExecutionResult.Success, null);
                            }

                            PushSelfArrayIndex(self, variableName, index);
                            return (ExecutionResult.Success, null);
                        }
                        else
                        {
                            // instance id
                            var self = InstanceManager.FindByInstanceId(instanceId)!;
                            PushSelfArrayIndex(self, variableName, index);
                            return (ExecutionResult.Success, null);
                        }
                    }

                    //return (ExecutionResult.Failed, $"Don't know how to push {instruction.Raw} index:{index} instanceid:{instanceId}");
                }
                else if (variableType == VariableType.Global)
                {
                    var index = Call.Stack.Pop(VMType.i).Conv<int>();
                    var instanceId = Call.Stack.Pop(VMType.i).Conv<int>();

                    if (instanceId == GMConstants.global)
                    {
                        PushGlobalArrayIndex(variableName, index);
                        return (ExecutionResult.Success, null);
                    }
                }
                else if (variableType == VariableType.Local)
                {
                    var index = Call.Stack.Pop(VMType.i).Conv<int>();
                    var instanceId = Call.Stack.Pop(VMType.i).Conv<int>();

                    if (instanceId == GMConstants.local)
                    {
                        PushLocalArrayIndex(variableName, index);
                        return (ExecutionResult.Success, null);
                    }
                }
            }
            else if (variablePrefix == VariablePrefix.ArrayPopAF)
            {
                if (variableType == VariableType.Self)
                {
                    var index = Call.Stack.Pop(VMType.i).Conv<int>();
                    var instanceId = Call.Stack.Pop(VMType.i).Conv<int>();

                    // TODO: make into methods and move out duplicated code
                    if (instanceId == GMConstants.global)
                    {
                        VariableResolver.ArraySet(index, new List<object?>(),
                            () => VariableResolver.GlobalVariables.GetValueOrDefault(variableName),
                            array => VariableResolver.GlobalVariables[variableName] = array,
                            onlyGrow: true);

                        var array = VariableResolver.GlobalVariables[variableName].Conv<IList>();
                        Call.Stack.Push(array[index], VMType.v);
                        return (ExecutionResult.Success, null);
                    }
                    else if (instanceId == GMConstants.local)
                    {
                        VariableResolver.ArraySet(index, new List<object?>(),
                            () => Call.Locals.GetValueOrDefault(variableName),
                            array => Call.Locals[variableName] = array,
                            onlyGrow: true);

                        var array = Call.Locals[variableName].Conv<IList>();
                        Call.Stack.Push(array[index], VMType.v);
                        return (ExecutionResult.Success, null);
                    }
                    else if (instanceId == GMConstants.self)
                    {
                        // TODO: check builtin self var
                        VariableResolver.ArraySet(index, new List<object?>(),
                            () => Self.Self.SelfVariables.GetValueOrDefault(variableName),
                            array => Self.Self.SelfVariables[variableName] = array,
                            onlyGrow: true);

                        var array = Self.Self.SelfVariables[variableName].Conv<IList>();
                        Call.Stack.Push(array[index], VMType.v);
                        return (ExecutionResult.Success, null);
                    }
                }
            }
            else if (variablePrefix == VariablePrefix.ArrayPushAF)
            {
                if (variableType == VariableType.Self)
                {
                    var index = Call.Stack.Pop(VMType.i).Conv<int>();
                    var instanceId = Call.Stack.Pop(VMType.i).Conv<int>();

                    if (instanceId == GMConstants.global)
                    {
                        var array = VariableResolver.GlobalVariables[variableName].Conv<IList>();
                        Call.Stack.Push(array[index], VMType.v);
                        return (ExecutionResult.Success, null);
                    }
                    else if (instanceId == GMConstants.local)
                    {
                        var array = Call.Locals[variableName].Conv<IList>();
                        Call.Stack.Push(array[index], VMType.v);
                        return (ExecutionResult.Success, null);
                    }
                    else if (instanceId == GMConstants.self)
                    {
                        // TODO: check builtin self var
                        var array = Self.Self.SelfVariables[variableName].Conv<IList>();
                        Call.Stack.Push(array[index], VMType.v);
                        return (ExecutionResult.Success, null);
                    }
                    else if (instanceId < 0)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        var inst = InstanceManager.Find(instanceId);

                        if (inst == null)
                        {
                            DebugLog.LogError($"Tried to push variable {variableName} from {instanceId}, which doesn't exist!!");

                            DebugLog.LogError($"--Stacktrace--");
                            foreach (var item in CallStack)
                            {
                                DebugLog.LogError($" - {item.CodeName}");
                            }

                            DebugLog.LogError(Environment.StackTrace);

                            Call.Stack.Push(null, VMType.v);
                            return (ExecutionResult.Failed, null);
                        }

                        var array = inst.SelfVariables[variableName].Conv<IList>();
                        Call.Stack.Push(array[index], VMType.v);
                        return (ExecutionResult.Success, null);
                    }
                }
            }
        }
        else if (variablePrefix == VariablePrefix.Stacktop)
        {
            if (variableType == VariableType.Self)
            {
                var id = Call.Stack.Pop(VMType.i).Conv<int>();

                if (id == GMConstants.stacktop)
                {
                    var popped = Call.Stack.Pop(VMType.v);

                    if (popped is GMLObject gmlo)
                    {
                        PushSelf(gmlo, variableName);
                        return (ExecutionResult.Success, null);
                    }
                    else
                    {
                        id = popped.Conv<int>();
                    }
                }
                else if (id == GMConstants.other)
                {
                    PushOther(variableName);
                    return (ExecutionResult.Success, null);
                }

                PushIndex(id, variableName);
                return (ExecutionResult.Success, null);
            }
        }

        return (ExecutionResult.Failed, $"Don't know how to push {instruction.Raw}");
    }
}

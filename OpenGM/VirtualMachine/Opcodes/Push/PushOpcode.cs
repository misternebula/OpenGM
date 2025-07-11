using OpenGM.IO;
using System.Collections;
using UndertaleModLib.Models;

namespace OpenGM.VirtualMachine;

public static partial class VMExecutor
{
    public static void PushGlobal(string varName)
    {
        Call.Stack.Push(VariableResolver.GlobalVariables[varName], UndertaleInstruction.DataType.Variable);
    }

    public static void PushGlobalArrayIndex(string varName, int index)
    {
        var array = VariableResolver.GlobalVariables[varName].Conv<IList>();
        Call.Stack.Push(array[index], UndertaleInstruction.DataType.Variable);
    }

    public static void PushLocalArrayIndex(string varName, int index)
    {
        var array = Call.Locals[varName].Conv<IList>();
        Call.Stack.Push(array[index], UndertaleInstruction.DataType.Variable);
    }

    public static void PushLocal(string varName)
    {
        Call.Stack.Push(Call.Locals[varName], UndertaleInstruction.DataType.Variable);
    }

    public static void PushBuiltin(string varName)
    {
        if (VariableResolver.BuiltInVariables.ContainsKey(varName))
        {
            var value = VariableResolver.BuiltInVariables[varName].getter();
            Call.Stack.Push(value, UndertaleInstruction.DataType.Variable);
        }
        else if (VariableResolver.BuiltInSelfVariables.ContainsKey(varName))
        {
            var value = VariableResolver.BuiltInSelfVariables[varName].getter(Self.GMSelf);
            Call.Stack.Push(value, UndertaleInstruction.DataType.Variable);
        }
        else if (Self.Self.SelfVariables.ContainsKey(varName))
        {
            var value = Self.Self.SelfVariables[varName];
            Call.Stack.Push(value, UndertaleInstruction.DataType.Variable);
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
            Call.Stack.Push(array[index], UndertaleInstruction.DataType.Variable);
        }
        else if (VariableResolver.BuiltInSelfVariables.ContainsKey(varName))
        {
            var array = VariableResolver.BuiltInSelfVariables[varName].getter(Self.GMSelf).Conv<IList>();
            Call.Stack.Push(array[index], UndertaleInstruction.DataType.Variable);
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
            DebugLog.LogError($"Null self given to PushSelf varName:{varName} - {VMExecutor.CurrentInstruction!}");

            DebugLog.LogError($"--Stacktrace--");
            foreach (var item in CallStack)
            {
                DebugLog.LogError($" - {item.CodeName}");
            }

            Call.Stack.Push(null, UndertaleInstruction.DataType.Variable);
            return;
        }

        if (VariableResolver.BuiltInVariables.TryGetValue(varName, out var builtin_gettersetter))
        {
            Call.Stack.Push(builtin_gettersetter.getter(), UndertaleInstruction.DataType.Variable);
        }
        else if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var selfbuiltin_gettersetter) && self is GamemakerObject gm)
        {
            Call.Stack.Push(selfbuiltin_gettersetter.getter(gm), UndertaleInstruction.DataType.Variable);
        }
        else
        {
            if (self.SelfVariables.ContainsKey(varName))
            {
                Call.Stack.Push(self.SelfVariables[varName], UndertaleInstruction.DataType.Variable);
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
                Call.Stack.Push(self.SelfVariables[varName], UndertaleInstruction.DataType.Variable);
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

        Call.Stack.Push(array[index], UndertaleInstruction.DataType.Variable);
    }

    public static void PushArgument(int index)
    {
        var arguments = Call.Locals["arguments"].Conv<IList>();

        if (index >= arguments.Count)
        {
            // Scripts can be called with fewer than normal arguments.
            // They just get set to Undefined.
            Call.Stack.Push(null, UndertaleInstruction.DataType.Variable);
            return;
        }

        Call.Stack.Push(arguments[index], UndertaleInstruction.DataType.Variable);
    }

    public static void PushIndex(int assetId, string varName)
    {
        if (assetId < GMConstants.FIRST_INSTANCE_ID)
        {
            // Asset Id

            var asset = InstanceManager.FindByAssetId(assetId).MinBy(x => x.instanceId)!;

            if (asset == null)
            {
                DebugLog.LogError($"Couldn't find any instances of {AssetIndexManager.GetName(AssetType.objects, assetId)}!");
                Call.Stack.Push(null, UndertaleInstruction.DataType.Variable);
                return;
            }

            PushSelf(asset, varName);
        }
        else
        {
            // Instance Id
            var asset = InstanceManager.FindByInstanceId(assetId);

            if (asset == null)
            {
                DebugLog.LogError($"Tried to push variable {varName} from instanceid {assetId}, which doesnt exist!!");

                DebugLog.LogError($"--Stacktrace--");
                foreach (var item in CallStack)
                {
                    DebugLog.LogError($" - {item.CodeName}");
                }

                DebugLog.LogError(Environment.StackTrace);

                Call.Stack.Push(null, UndertaleInstruction.DataType.Variable);
                return;
            }

            PushSelf(asset, varName);
        }
    }

    public static void PushOther(string varName)
    {
        PushSelf(Other.Self, varName);
    }

    public static void PushStacktop(string varName)
    {
        var instanceId = Call.Stack.Pop(UndertaleInstruction.DataType.Variable).Conv<int>();
        PushIndex(instanceId, varName);
    }

    public static void PushArgumentArrayIndex(string varName, int index)
    {
        var argIndex = int.Parse(varName.Replace("argument", ""));

        var arguments = Call.Locals["arguments"].Conv<IList>();

        if (argIndex >= arguments.Count)
        {
            Call.Stack.Push(null, UndertaleInstruction.DataType.Variable);
            return;
        }

        var array = arguments[argIndex].Conv<IList>();

        Call.Stack.Push(array[index], UndertaleInstruction.DataType.Variable);
    }

    public static (ExecutionResult, object?) DoPush(UndertaleInstruction instruction)
    {
        switch (instruction.Type1)
        {
            case UndertaleInstruction.DataType.Int32:

                if (instruction.ValueString.Resource.Content != null)
                {
                    if (instruction.PushFunction)
                    {
                        Call.Stack.Push(AssetIndexManager.GetIndex(AssetType.scripts, instruction.ValueString.Resource.Content), UndertaleInstruction.DataType.Int32);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    Call.Stack.Push(instruction.ValueInt, UndertaleInstruction.DataType.Int32);
                }
                return (ExecutionResult.Success, null);
            case UndertaleInstruction.DataType.Int16:
                Call.Stack.Push(instruction.ValueShort, UndertaleInstruction.DataType.Int16);
                return (ExecutionResult.Success, null);
            case UndertaleInstruction.DataType.Int64:
                Call.Stack.Push(instruction.ValueLong, UndertaleInstruction.DataType.Int64);
                return (ExecutionResult.Success, null);
            case UndertaleInstruction.DataType.Boolean:
                Call.Stack.Push(instruction.ValueInt, UndertaleInstruction.DataType.Boolean);
                return (ExecutionResult.Success, null);
            case UndertaleInstruction.DataType.Double:
                Call.Stack.Push(instruction.ValueDouble, UndertaleInstruction.DataType.Double);
                return (ExecutionResult.Success, null);
            case UndertaleInstruction.DataType.String:
                Call.Stack.Push(instruction.ValueString.Resource.Content, UndertaleInstruction.DataType.String);
                return (ExecutionResult.Success, null);
            case UndertaleInstruction.DataType.Variable:
                return DoPushV(instruction);
        }

        return (ExecutionResult.Failed, $"Don't know how to push {instruction}");
    }

    public static (ExecutionResult, object?) DoPushV(UndertaleInstruction instruction)
    {
        var variableName = instruction.ValueVariable.Name.Content;
        var variableType = instruction.ValueVariable.InstanceType;
        var variablePrefix = instruction.ReferenceType;
        var assetId = instruction.assetId;

        if (variablePrefix == UndertaleInstruction.VariableType.Normal)
        {
            if (variableType == UndertaleInstruction.InstanceType.Global)
            {
                PushGlobal(variableName);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == UndertaleInstruction.InstanceType.Local)
            {
                PushLocal(variableName);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == UndertaleInstruction.InstanceType.Builtin)
            {
                PushBuiltin(variableName);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == UndertaleInstruction.InstanceType.Self)
            {
                PushSelf(Self.Self, variableName);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == UndertaleInstruction.InstanceType.Arg)
            {
                var strIndex = variableName[8..]; // skip "argument"
                var index = int.Parse(strIndex);
                PushArgument(index);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == UndertaleInstruction.InstanceType.Index)
            {
                PushIndex(assetId, variableName);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == UndertaleInstruction.InstanceType.Other)
            {
                PushOther(variableName);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == UndertaleInstruction.InstanceType.Stacktop)
            {
                PushStacktop(variableName);
                return (ExecutionResult.Success, null);
            }
        }
        else if (variablePrefix == UndertaleInstruction.VariableType.Array || variablePrefix == UndertaleInstruction.VariableType.ArrayPopAF || variablePrefix == UndertaleInstruction.VariableType.ArrayPushAF)
        {
            if (variablePrefix == UndertaleInstruction.VariableType.Array)
            {
                if (variableType == UndertaleInstruction.InstanceType.Self)
                {
                    var index = Call.Stack.Pop(UndertaleInstruction.DataType.Int32).Conv<int>();
                    var instanceId = Call.Stack.Pop(UndertaleInstruction.DataType.Int32).Conv<int>();

                    if (instanceId == GMConstants.stacktop)
                    {
                        var context = Call.Stack.Pop(UndertaleInstruction.DataType.Variable);

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
                                Call.Stack.Push(null, UndertaleInstruction.DataType.Variable);
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
                else if (variableType == UndertaleInstruction.InstanceType.Global)
                {
                    var index = Call.Stack.Pop(UndertaleInstruction.DataType.Int32).Conv<int>();
                    var instanceId = Call.Stack.Pop(UndertaleInstruction.DataType.Int32).Conv<int>();

                    if (instanceId == GMConstants.global)
                    {
                        PushGlobalArrayIndex(variableName, index);
                        return (ExecutionResult.Success, null);
                    }
                }
                else if (variableType == UndertaleInstruction.InstanceType.Local)
                {
                    var index = Call.Stack.Pop(UndertaleInstruction.DataType.Int32).Conv<int>();
                    var instanceId = Call.Stack.Pop(UndertaleInstruction.DataType.Int32).Conv<int>();

                    if (instanceId == GMConstants.local)
                    {
                        PushLocalArrayIndex(variableName, index);
                        return (ExecutionResult.Success, null);
                    }
                }
            }
            else if (variablePrefix == UndertaleInstruction.VariableType.ArrayPopAF)
            {
                if (variableType == UndertaleInstruction.InstanceType.Self)
                {
                    var index = Call.Stack.Pop(UndertaleInstruction.DataType.Int32).Conv<int>();
                    var instanceId = Call.Stack.Pop(UndertaleInstruction.DataType.Int32).Conv<int>();

                    // TODO: make into methods and move out duplicated code
                    if (instanceId == GMConstants.global)
                    {
                        VariableResolver.ArraySet(index, new List<object?>(),
                            () => VariableResolver.GlobalVariables.TryGetValue(variableName, out var array) ? array as IList : null,
                            array => VariableResolver.GlobalVariables[variableName] = array,
                            onlyGrow: true);

                        var array = VariableResolver.GlobalVariables[variableName].Conv<IList>();
                        Call.Stack.Push(array[index], UndertaleInstruction.DataType.Variable);
                        return (ExecutionResult.Success, null);
                    }
                    else if (instanceId == GMConstants.local)
                    {
                        VariableResolver.ArraySet(index, new List<object?>(),
                            () => Call.Locals.TryGetValue(variableName, out var array) ? array as IList : null,
                            array => Call.Locals[variableName] = array,
                            onlyGrow: true);

                        var array = Call.Locals[variableName].Conv<IList>();
                        Call.Stack.Push(array[index], UndertaleInstruction.DataType.Variable);
                        return (ExecutionResult.Success, null);
                    }
                    else if (instanceId == GMConstants.self)
                    {
                        // TODO: check builtin self var
                        VariableResolver.ArraySet(index, new List<object?>(),
                            () => Self.Self.SelfVariables.TryGetValue(variableName, out var array) ? array as IList : null,
                            array => Self.Self.SelfVariables[variableName] = array,
                            onlyGrow: true);

                        var array = Self.Self.SelfVariables[variableName].Conv<IList>();
                        Call.Stack.Push(array[index], UndertaleInstruction.DataType.Variable);
                        return (ExecutionResult.Success, null);
                    }
                }
            }
            else if (variablePrefix == UndertaleInstruction.VariableType.ArrayPushAF)
            {
                if (variableType == UndertaleInstruction.InstanceType.Self)
                {
                    var index = Call.Stack.Pop(UndertaleInstruction.DataType.Int32).Conv<int>();
                    var instanceId = Call.Stack.Pop(UndertaleInstruction.DataType.Int32).Conv<int>();

                    if (instanceId == GMConstants.global)
                    {
                        var array = VariableResolver.GlobalVariables[variableName].Conv<IList>();
                        Call.Stack.Push(array[index], UndertaleInstruction.DataType.Variable);
                        return (ExecutionResult.Success, null);
                    }
                    else if (instanceId == GMConstants.local)
                    {
                        var array = Call.Locals[variableName].Conv<IList>();
                        Call.Stack.Push(array[index], UndertaleInstruction.DataType.Variable);
                        return (ExecutionResult.Success, null);
                    }
                    else if (instanceId == GMConstants.self)
                    {
                        // TODO: check builtin self var
                        var array = Self.Self.SelfVariables[variableName].Conv<IList>();
                        Call.Stack.Push(array[index], UndertaleInstruction.DataType.Variable);
                        return (ExecutionResult.Success, null);
                    }
                }
            }
        }
        else if (variablePrefix == UndertaleInstruction.VariableType.StackTop)
        {
            if (variableType == UndertaleInstruction.InstanceType.Self)
            {
                var id = Call.Stack.Pop(UndertaleInstruction.DataType.Int32).Conv<int>();

                if (id == GMConstants.stacktop)
                {
                    var popped = Call.Stack.Pop(UndertaleInstruction.DataType.Variable);

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

        return (ExecutionResult.Failed, $"Don't know how to push {instruction}");
    }
}

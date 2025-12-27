using OpenGM.IO;
using OpenGM.SerializedFiles;
using System.Collections;

namespace OpenGM.VirtualMachine;

public static partial class VMExecutor
{
    public static void PopToGlobal(string varName, object? value)
    {
        VariableResolver.GlobalVariables[varName] = value;
    }

    public static void PopToLocal(string varName, object? value)
    {
        //Ctx.Locals[varName] = value;
        Call.Locals[varName] = value;
    }

    public static void PopToGlobalArray(string varName, int index, object? value)
    {
        VariableResolver.ArraySet(
            index,
            value,
            () => VariableResolver.GlobalVariables.GetValueOrDefault(varName),
            array => VariableResolver.GlobalVariables[varName] = array);
    }

    public static void PopToLocalArray(string varName, int index, object? value)
    {
        VariableResolver.ArraySet(
            index,
            value,
            () => Call.Locals.GetValueOrDefault(varName),
            array => Call.Locals[varName] = array);
    }

    public static void PopToSelf(IStackContextSelf self, string varName, object? value)
    {
        // check built in variables beforehand
        if (VariableResolver.BuiltInVariables.ContainsKey(varName))
        {
            VariableResolver.BuiltInVariables[varName].setter!(value);
            return;
        }

        if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var gettersetter) && self is GamemakerObject gm)
        {
            gettersetter.setter!(gm, value);
        }
        else
        {
            self.SelfVariables[varName] = value;
        }
    }

    public static void PopToSelfArray(IStackContextSelf self, string varName, int index, object? value)
    {
        if (VariableResolver.BuiltInVariables.TryGetValue(varName, out var gs))
        {
            VariableResolver.ArraySet(
                index,
                value,
                () => gs.getter(),
                array => gs.setter!(array));
        }
        else if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var gsSelf) && self is GamemakerObject gm)
        {
            VariableResolver.ArraySet(
                index,
                value,
                () => gsSelf.getter(gm),
                array => gsSelf.setter!(gm, array));
        }
        else
        {
            VariableResolver.ArraySet(
                index,
                value,
                () => self.SelfVariables.GetValueOrDefault(varName),
                array => self.SelfVariables[varName] = array);
        }
    }

    public static void PopToIndex(int assetId, string varName, object? value)
    {
        if (assetId < GMConstants.FIRST_INSTANCE_ID)
        {
            // Asset Index
            var instances = InstanceManager.FindByAssetId(assetId);

            foreach (var instance in instances)
            {
                if (instance == null)
                {
                    throw new NotImplementedException();
                }

                PopToSelf(instance, varName, value);
            }
        }
        else
        {
            // Instance Id
            var instance = InstanceManager.FindByInstanceId(assetId);

            if (instance == null)
            {
                throw new NotImplementedException($"Instance {assetId} couldn't be found!");
            }

            // TODO : double check this is always self. might be local as well????
            PopToSelf(instance, varName, value);
        }
    }

    public static void PopToArgument(int index, object? value)
    {
        var args = (object?[])Call.Locals["arguments"].Conv<IList>();

        if (index >= args.Length) // could change ExecuteCode to make args a list, but its fine
        {
            Array.Resize(ref args, index + 1);
        }
        
        args[index] = value;
        Call.Locals["arguments"] = args;
    }

    public static void PopToOther(string varName, object? value)
    {
        PopToSelf(Other.Self, varName, value);
    }

    public static void PopToBuiltIn(string varName, object? value)
    {
        if (!VariableResolver.BuiltInVariables.TryGetValue(varName, out var builtinGetSet))
        {
            // "Welcome to hell" - colin
            // At some point, self started only being used when explicitly doing "self.", and builtin is used instead.
            PopToSelf(Self.Self, varName, value);
        }
        else
        {
            builtinGetSet.setter!(value);
        }
    }

    public static void PopToBuiltInArray(string varName, int index, object? value)
    {
        if (VariableResolver.BuiltInVariables.TryGetValue(varName, out var gettersetter))
        {
            VariableResolver.ArraySet(
                index,
                value,
                () => gettersetter.getter() ,
                array => gettersetter.setter!(array));
        }
        else if (VariableResolver.BuiltInSelfVariables.TryGetValue(varName, out var selfgettersetter))
        {
            VariableResolver.ArraySet(
                index,
                value,
                () => selfgettersetter.getter(Self.GMSelf),
                array => selfgettersetter.setter!(Self.GMSelf, array));
        }
        else
        {
            throw new ArgumentException($"No such builtin array \"{varName}\"");
        }
    }

    public static void PopToStatic(string varName, object? value)
    {
        var currentFunc = Call.Function;

        if (currentFunc == null)
        {
            // uhhhhh fuckin uhhh
            throw new NotImplementedException();
        }

        if (currentFunc.StaticVariables == null)
        {
            currentFunc.StaticVariables = new();
        }

        if (!currentFunc.StaticVariables.ContainsKey(varName) && currentFunc.HasStaticInitRan)
        {
            throw new NotImplementedException("StaticVariables should contain every static variable after initialization!?");
        }

        currentFunc.StaticVariables[varName] = value;
    }

    public static (ExecutionResult, object?) DoPop(VMCodeInstruction instruction)
    {
        if (instruction.TypeOne == VMType.e)
        {
            // weird swap thingy
            throw new NotImplementedException();
        }

        var variableName = instruction.variableName;
        var variableType = instruction.variableType;
        var variablePrefix = instruction.variablePrefix;
        var assetId = instruction.assetId;

        if (variablePrefix == VariablePrefix.None)
        {
            // we're just popping to a normal variable. thank god.
            var dataPopped = Call.Stack.Pop(instruction.TypeTwo);

            if (variableType == VariableType.Global)
            {
                PopToGlobal(variableName, dataPopped);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.Local)
            {
                PopToLocal(variableName, dataPopped);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.Self)
            {
                if (GlobalInit)
                {
                    PopToGlobal(variableName, dataPopped);
                    return (ExecutionResult.Success, null);
                }

                PopToSelf(Self.Self, variableName, dataPopped);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.Index)
            {
                PopToIndex(assetId, variableName, dataPopped);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.Argument)
            {
                var strIndex = variableName[8..]; // skip "argument"
                var index = int.Parse(strIndex);
                PopToArgument(index, dataPopped);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.BuiltIn)
            {
                PopToBuiltIn(variableName, dataPopped);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.Other)
            {
                PopToOther(variableName, dataPopped);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.Static)
            {
                PopToStatic(variableName, dataPopped);
                return (ExecutionResult.Success, null);
            }
        }
        else if (variablePrefix == VariablePrefix.Array || variablePrefix == VariablePrefix.ArrayPopAF || variablePrefix == VariablePrefix.ArrayPushAF)
        {
            // pop appears to not support ArrayPopAF or ArrayPushAF

            if (variablePrefix == VariablePrefix.Array)
            {
                int index;
                //int instanceId;
                object? context;
                object? value;
                if (instruction.TypeOne == VMType.i) // flips value and id pop
                {
                    value = Call.Stack.Pop(instruction.TypeTwo);

                    index = Call.Stack.Pop(VMType.i).Conv<int>();
                    var instanceId = Call.Stack.Pop(VMType.i).Conv<int>();
                    if (instanceId == GMConstants.stacktop)
                    {
                        context = Call.Stack.Pop(VMType.v);
                    }
                    else
                    {
                        context = instanceId;
                    }
                }
                else // v
                {
                    index = Call.Stack.Pop(VMType.i).Conv<int>();
                    var instanceId = Call.Stack.Pop(VMType.i).Conv<int>();
                    if (instanceId == GMConstants.stacktop)
                    {
                        context = Call.Stack.Pop(VMType.v);
                    }
                    else
                    {
                        context = instanceId;
                    }

                    value = Call.Stack.Pop(instruction.TypeTwo);
                }

                if (variableType == VariableType.Self)
                {
                    if (context is GMLObject obj)
                    {
                        PopToSelfArray(obj, variableName, index, value);
                        return (ExecutionResult.Success, null);
                    }
                    else
                    {
                        var instanceId = context.Conv<int>();
                        if (instanceId == GMConstants.global)
                        {
                            PopToGlobalArray(variableName, index, value);
                            return (ExecutionResult.Success, null);
                        }
                        else if (instanceId == GMConstants.local)
                        {
                            PopToLocalArray(variableName, index, value);
                            return (ExecutionResult.Success, null);
                        }
                        else if (instanceId == GMConstants.self)
                        {
                            PopToSelfArray(Self.Self, variableName, index, value);
                            return (ExecutionResult.Success, null);
                        }
                        else if (instanceId == GMConstants.builtin)
                        {
                            PopToBuiltInArray(variableName, index, value);
                            return (ExecutionResult.Success, null);
                        }
                        else
                        {
                            if (instanceId < GMConstants.FIRST_INSTANCE_ID)
                            {
                                // asset id
                                var gm = InstanceManager.FindByAssetId(instanceId).MinBy(x => x.instanceId)!;
                                PopToSelfArray(gm, variableName, index, value);
                                return (ExecutionResult.Success, null);
                            }
                            else
                            {
                                // instance id
                                var gm = InstanceManager.FindByInstanceId(instanceId)!;
                                PopToSelfArray(gm, variableName, index, value);
                                return (ExecutionResult.Success, null);
                            }
                        }
                    }
                }
                else if (variableType == VariableType.Global)
                {
                    if (context is GMLObject obj)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        var instanceId = context.Conv<int>();
                        if (instanceId == GMConstants.global)
                        {
                            PopToGlobalArray(variableName, index, value);
                            return (ExecutionResult.Success, null);
                        }
                    }
                }
                else if (variableType == VariableType.Local)
                {
                    if (context is GMLObject obj)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        var instanceId = context.Conv<int>();
                        if (instanceId == GMConstants.local)
                        {
                            PopToLocalArray(variableName, index, value);
                            return (ExecutionResult.Success, null);
                        }
                    }
                }

                return (ExecutionResult.Failed, $"Don't know how to execute {instruction.Raw} (index={index}, context={context}, value={value})");
            }
        }
        else if (variablePrefix == VariablePrefix.Stacktop)
        {
            // TODO : Check if 'self' is the only context where [stacktop] is used.
            // TODO : clean this shit up lol

            if (variableType == VariableType.Self)
            {
                int id;
                object? value;
                if (instruction.TypeOne == VMType.i) // flips value and id pop
                {
                    value = Call.Stack.Pop(instruction.TypeTwo);

                    id = Call.Stack.Pop(VMType.i).Conv<int>();
                    if (id == GMConstants.stacktop)
                    {
                        var popped = Call.Stack.Pop(VMType.v);

                        if (popped is GMLObject gmlo)
                        {
                            PopToSelf(gmlo, variableName, value);
                            return (ExecutionResult.Success, null);
                        }
                        else
                        {
                            id = popped.Conv<int>();
                        }
                    }
                }
                else // v
                {
                    id = Call.Stack.Pop(VMType.i).Conv<int>();
                    if (id == GMConstants.stacktop)
                    {
                        var popped = Call.Stack.Pop(VMType.v);

                        if (popped is GMLObject gmlo)
                        {
                            value = Call.Stack.Pop(instruction.TypeTwo);
                            PopToSelf(gmlo, variableName, value);
                            return (ExecutionResult.Success, null);
                        }
                        else
                        {
                            id = popped.Conv<int>();
                        }
                    }

                    value = Call.Stack.Pop(instruction.TypeTwo);
                }

                if (id == GMConstants.global)
                {
                    PopToGlobal(variableName, value);
                    return (ExecutionResult.Success, null);
                }
                else if (id == GMConstants.self)
                {
                    if (Self.Self == null)
                    {
                        // for global scripts
                        PopToGlobal(variableName, value);
                    }
                    else
                    {
                        PopToSelf(Self.Self, variableName, value);
                    }

                    return (ExecutionResult.Success, null);
                }
                else if (id == GMConstants.noone)
                {
                    // uh what the fuck
                    DebugLog.LogWarning($"Tried to pop {value} into {variableName} on no object???");
                    return (ExecutionResult.Success, null);
                }
                else if (id == GMConstants.builtin)
                {
                    if (Self.Self == null)
                    {
                        // for global scripts
                        PopToGlobal(variableName, value);
                        return (ExecutionResult.Success, null);
                    }

                    // todo : wtf is this?? what was i on when i wrote this
                    if (!Self.Self.SelfVariables.TryAdd(variableName, value))
                    {
                        Self.Self.SelfVariables[variableName] = value;
                    }

                    DebugLog.LogWarning($"idk what's meant to happen here aaaa!!!! varname:{variableName} value:{value}");
                    DebugLog.PrintCallStack(DebugLog.LogType.Warning);

                    /*if (VariableResolver.BuiltInSelfVariables.ContainsKey(variableName))
                    {
                        VariableResolver.BuiltInSelfVariables[variableName].setter!(Self.GMSelf, value);
                    }
                    else
                    {
                        VariableResolver.BuiltInSelfVariables.Add(variableName, (
                            (obj) => obj.SelfVariables[variableName]!,
                            (obj, val) => obj.SelfVariables[variableName] = val));
                    }*/

                    return (ExecutionResult.Success, null);
                }

                PopToIndex(id, variableName, value);
                return (ExecutionResult.Success, null);
            }
            else if (variableType == VariableType.Static)
            {
                int id;
                object? value;

                if (instruction.TypeOne == VMType.i) // flips value and id pop
                {
                    value = Call.Stack.Pop(instruction.TypeTwo);

                    id = Call.Stack.Pop(VMType.i).Conv<int>();
                    if (id == GMConstants.stacktop)
                    {
                        var popped = Call.Stack.Pop(VMType.v);

                        if (popped is GMLObject gmlo)
                        {
                            PopToSelf(gmlo, variableName, value);
                            return (ExecutionResult.Success, null);
                        }
                        else
                        {
                            id = popped.Conv<int>();
                        }
                    }
                }
                else // v
                {
                    id = Call.Stack.Pop(VMType.i).Conv<int>();
                    if (id == GMConstants.stacktop)
                    {
                        var popped = Call.Stack.Pop(VMType.v);

                        if (popped is GMLObject gmlo)
                        {
                            value = Call.Stack.Pop(instruction.TypeTwo);
                            PopToSelf(gmlo, variableName, value);
                            return (ExecutionResult.Success, null);
                        }
                        else
                        {
                            id = popped.Conv<int>();
                        }
                    }

                    value = Call.Stack.Pop(instruction.TypeTwo);
                }

                if (id == GMConstants.@static)
                {
                    // TODO: do proper static stuff here
                    // static variables are global per function definition
                    PopToGlobal($"static {Call.CodeName} {variableName}", value);
                    return (ExecutionResult.Success, null);
                }
            }
        }

        return (ExecutionResult.Failed, $"Don't know how to execute {instruction.Raw}");
    }
}

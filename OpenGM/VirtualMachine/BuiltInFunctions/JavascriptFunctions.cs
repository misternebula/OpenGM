using OpenGM.IO;
using OpenGM.Loading;
using System.Diagnostics;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class JavascriptFunctions
    {
        [GMLFunction("@@NewGMLObject@@")]
        public static object? NewGMLObject(object?[] args)
        {
            var ctor = VMExecutor.FetchMethod(args[0]);
            var values = args[1..];
            var obj = new GMLObject();

            var script = ctor!.func;
            var code = script.GetCode()!;
            VMExecutor.ExecuteCode(code, obj, args: values);

            // TODO: this is definitely NOT how static variables are supposed to work
            if (code.ParentAssetId != -1)
            {
                var parent = GameLoader.Codes[code.ParentAssetId];
                var def = parent.Functions.FirstOrDefault(x => x.FunctionName == code.Name);

                if (def is not null)
                {
                    foreach (var kv in def.StaticVariables)
                    {
                        obj.SelfVariables[kv.Key] = kv.Value;
                    }
                }
            }

            return obj;
        }

        [GMLFunction("@@NewGMLArray@@")]
        public static object NewGMLArray(object?[] args)
        {
            return args.ToList(); // needs to be resizeable, e.g. initializing __objectID2Depth
        }

        [GMLFunction("@@This@@")]
        public static object This(object?[] args)
        {
            if (VMExecutor.Self.Self is GamemakerObject)
            {
                return VMExecutor.Self.GMSelf.instanceId;
            }

            return VMExecutor.Self.Self;
        }

        [GMLFunction("@@Global@@")]
        public static object Global(object?[] args)
        {
            return VariableResolver.GlobalVariables;
        }

        // TODO : implement try/catches?

        [GMLFunction("@@try_hook@@", GMLFunctionFlags.Stub)]
        public static object? TryHook(object?[] args) => null;

        [GMLFunction("@@try_unhook@@", GMLFunctionFlags.Stub)]
        public static object? TryUnhook(object?[] args) => null;

        [GMLFunction("@@throw@@", GMLFunctionFlags.Stub)]
        public static object? Throw(object?[] args) => null;

        [GMLFunction("@@finish_catch@@", GMLFunctionFlags.Stub)]
        public static object? FinishCatch(object?[] args) => null;

        [GMLFunction("@@finish_finally@@", GMLFunctionFlags.Stub)]
        public static object? FinishFinally(object?[] args) => null;
        // $PRINT
        // $FAIL
        // $ERROR
        // ERROR
        // testFailed
        // @@typeof@@
        // @@new@@
        // @@delete@@
        // exception_unhandled_handler
        // @@instanceof@@
        // @@Null@@

        [GMLFunction("@@NullObject@@")]
        public static object? NullObject(object?[] args)
        {
            return null;
        }

        [GMLFunction("@@Other@@")]
        public static object Other(object?[] args)
        {
            if (VMExecutor.Other.Self is GamemakerObject)
            {
                return VMExecutor.Other.GMSelf.instanceId;
            }

            return VMExecutor.Other.Self;
        }

        [GMLFunction("@@GetInstance@@")]
        public static object? GetInstance(object?[] args)
        {
            var id = args[0].Conv<int>();

            if (id < GMConstants.FIRST_INSTANCE_ID)
            {
                return InstanceManager.FindByAssetId(id).First().instanceId;
            }
            else
            {
                return InstanceManager.FindByInstanceId(id)?.instanceId;
            }
        }

        // @@GlobalScope@@
        // @@NewObject@@
        // @@NewProperty@@

        [GMLFunction("@@CopyStatic@@")]
        public static object? CopyStatic(object?[] args)
        {
            var func = args[0].Conv<int>();
            var script = ScriptResolver.ScriptsByIndex[func - GMConstants.FIRST_INSTANCE_ID];
            var code = script.GetCode();

            if (code == null)
            {
                throw new NotImplementedException();
            }

            var parent = GameLoader.Codes[code.ParentAssetId];
            var def = parent.Functions.FirstOrDefault(x => x.FunctionName == code.Name);

            if (def == null)
            {
                throw new NotImplementedException();
            }

            foreach (var item in def.StaticVariables)
            {
                VMExecutor.Call.Function!.StaticVariables.Add(item.Key, item.Value);
            }

            return null;
        }
    }
}

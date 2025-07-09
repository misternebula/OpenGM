namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class JavascriptFunctions
    {
        [GMLFunction("@@NewGMLObject@@")]
        public static object? NewGMLObject(object?[] args)
        {
            var ctor = args[0];
            var values = args[1..];
            var obj = new GMLObject();

            if (ctor is Method m)
            {
                VMExecutor.ExecuteCode(m.func.GetCode(), obj, args: values);
            }
            else
            {
                var constructorIndex = ctor.Conv<int>();
                VMExecutor.ExecuteCode(ScriptResolver.ScriptsByIndex[constructorIndex].GetCode(), obj, args: values);
            }

            return obj;
        }

        [GMLFunction("@@NewGMLArray@@")]
        public static object NewGMLArray(object?[] args)
        {
            return args.ToList(); // needs to be resizeable, e.g. initializing __objectID2Depth
        }

        [GMLFunction("@@This@@")]
        public static object This(object?[] args) => VMExecutor.Self.GMSelf.instanceId;

        // @@Global@@

        // TODO : implement try/catches?

        [GMLFunction("@@try_hook@@")]
        public static object? TryHook(object?[] args) => null;

        [GMLFunction("@@try_unhook@@")]
        public static object? TryUnhook(object?[] args) => null;

        [GMLFunction("@@throw@@")]
        public static object? Throw(object?[] args) => null;

        [GMLFunction("@@finish_catch@@")]
        public static object? FinishCatch(object?[] args) => null;

        [GMLFunction("@@finish_finally@@")]
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
        public static object Other(object?[] args) => VMExecutor.Other.GMSelf.instanceId;
        // @@GetInstance@@
        // @@GlobalScope@@
        // @@NewObject@@
        // @@NewProperty@@
        // @@CopyStatic@@
    }
}

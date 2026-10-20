using OpenGM.SerializedFiles;

namespace OpenGM.VirtualMachine;
internal class Method
{
    public object? inst;
    public VMScript func = null!;

    public Method(VMScript func) => this.func = func;

    public override string ToString()
    {
        return $"{{ Method - func: {func}, inst: {inst}}}";
    }

    public static Method? FetchMethod(object? value, int nArgs)
    {
        if (value is null)
        {
            throw new ArgumentException($"Trying to fetch method for undefined! Current script:{VMExecutor.CallStack.First().CodeName}");
        }
        else if (value is Method method)
        {
            return method;
        }
        else if (value is int or long or short)
        {
            var index = value.Conv<int>();

            if (index < GMConstants.FIRST_INSTANCE_ID)
            {
                var builtin = ScriptResolver.BuiltInFunctions.ElementAt(index);

                var script = new VMScript { CustomCode = GenerateCodeForBuiltIn(builtin.Key, nArgs) };

                return new Method(script);
            }
            else
            {
                return new Method(ScriptResolver.ScriptsByIndex[value.Conv<int>() - GMConstants.FIRST_INSTANCE_ID]);
            }
        }

        throw new ArgumentException($"Don't know how to fetch method for {value} ({value.GetType().FullName})");
    }

    internal static VMCode GenerateCodeForBuiltIn(string builtin, int nArgs)
    {
        var code = new VMCode { LocalVariables = [], Name = $"GENERATED CALLV FOR {builtin}" };

        for (var i = nArgs - 1; i >= 0; i--)
        {
            var pushInstruction = new VMCodeInstruction
            {
                Opcode = VMOpcode.PUSH,
                TypeOne = VMType.v,
                variablePrefix = VariablePrefix.None,
                variableType = VariableType.Argument,
                variableName = $"argument{i}"
            };
            code.Instructions.Add(pushInstruction);
        }

        code.Instructions.Add(new VMCodeInstruction
        {
            Opcode = VMOpcode.CALL,
            TypeOne = VMType.i,
            FunctionName = builtin,
            FunctionArgumentCount = nArgs
        });

        code.Instructions.Add(new VMCodeInstruction
        {
            Opcode = VMOpcode.RET,
            TypeOne = VMType.v,
        });

        return code;
    }
}

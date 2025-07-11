using UndertaleModLib.Models;

namespace OpenGM.VirtualMachine;

public static partial class VMExecutor
{
    // see https://github.com/UnderminersTeam/Underanalyzer/blob/fa8fd0da89c5a4fe1bb050fe62bc5106210b1c13/Underanalyzer/VMDataTypeExtensions.cs#L22
    public static UndertaleInstruction.DataType GetMathReturnType(UndertaleInstruction instruction)
    {
        if (instruction.Kind == UndertaleInstruction.Opcode.Cmp)
        {
            // Comparisons are always boolean.
            return UndertaleInstruction.DataType.Boolean;
        }

        /*if (instruction.Type1 == VMType.None)
        {
            return instruction.Type1;
        }*/

        // Choose whichever type has a higher bias, or if equal, the smaller numerical data type value.
        var bias1 = StackTypeBias(instruction.Type1);
        var bias2 = StackTypeBias(instruction.Type2);
        return bias1 == bias2
            ? (UndertaleInstruction.DataType)CustomMath.Min((int)instruction.Type1, (int)instruction.Type2)
            : (bias1 > bias2) ? instruction.Type1 : instruction.Type2;
    }

    public static int StackTypeBias(UndertaleInstruction.DataType type)
    {
        return type switch
        {
            UndertaleInstruction.DataType.Int32 or UndertaleInstruction.DataType.Boolean or UndertaleInstruction.DataType.String => 0,
            UndertaleInstruction.DataType.Double or UndertaleInstruction.DataType.Int64 => 1,
            UndertaleInstruction.DataType.Variable => 2,
            _ => throw new NotImplementedException()
        };
    }

    public static (ExecutionResult, object?) ADD(UndertaleInstruction instruction)
    {
        var valTwo = Call.Stack.Pop(instruction.Type1);
        var valOne = Call.Stack.Pop(instruction.Type2);

        var retType = GetMathReturnType(instruction);

        var hasString = instruction.Type1 == UndertaleInstruction.DataType.String || instruction.Type2 == UndertaleInstruction.DataType.String;
        var variableIsString = valOne is string || valTwo is string;

        if (hasString || variableIsString)
        {
            // strings need to concat
            Call.Stack.Push(valOne.Conv<string>() + valTwo.Conv<string>(), retType);
        }
        else
        {
            Call.Stack.Push(valOne.Conv<double>() + valTwo.Conv<double>(), retType);
        }

        return (ExecutionResult.Success, null);
    }

    public static (ExecutionResult, object?) SUB(UndertaleInstruction instruction)
    {
        var retType = GetMathReturnType(instruction);
        var numTwo = Call.Stack.Pop(instruction.Type1).Conv<double>();
        var numOne = Call.Stack.Pop(instruction.Type2).Conv<double>();

        Call.Stack.Push(numOne - numTwo, retType);
        return (ExecutionResult.Success, null);
    }

    public static (ExecutionResult, object?) MUL(UndertaleInstruction instruction)
    {
        var retType = GetMathReturnType(instruction);
        var numTwo = Call.Stack.Pop(instruction.Type1).Conv<double>();
        var numOne = Call.Stack.Pop(instruction.Type2).Conv<double>();

        Call.Stack.Push(numOne * numTwo, retType);
        return (ExecutionResult.Success, null);
    }

    public static (ExecutionResult, object?) DIV(UndertaleInstruction instruction)
    {
        var retType = GetMathReturnType(instruction);
        var numTwo = Call.Stack.Pop(instruction.Type1).Conv<double>();
        var numOne = Call.Stack.Pop(instruction.Type2).Conv<double>();

        Call.Stack.Push(numOne / numTwo, retType);
        return (ExecutionResult.Success, null);
    }

    // "a / b"        in GML is compiled to    "push.v a, push.v b, div.v.v"
    // "a div b"    in GML is compiled to    "push.v a, push.v b, rem.v.v"
    // "a % b"        in GML is compiled to    "push.v a, push.v b, mod.v.v"
    // "a mod b"    in GML is compiled to    "push.v a, push.v b, mod.v.v"

    public static (ExecutionResult, object?) REM(UndertaleInstruction instruction)
    {
        var retType = GetMathReturnType(instruction);
        var numTwo = Call.Stack.Pop(instruction.Type1).Conv<double>();
        var numOne = Call.Stack.Pop(instruction.Type2).Conv<double>();

        var doubleResult = numOne / numTwo;

        Call.Stack.Push(Math.Round(doubleResult, MidpointRounding.ToZero), retType);
        return (ExecutionResult.Success, null);
    }

    public static (ExecutionResult, object?) MOD(UndertaleInstruction instruction)
    {
        var retType = GetMathReturnType(instruction);
        var numTwo = Call.Stack.Pop(instruction.Type1).Conv<double>();
        var numOne = Call.Stack.Pop(instruction.Type2).Conv<double>();

        Call.Stack.Push(CustomMath.Mod(numOne, numTwo), retType);
        return (ExecutionResult.Success, null);
    }

    public static (ExecutionResult, object?) NEG(UndertaleInstruction instruction)
    {
        var retType = GetMathReturnType(instruction);
        Call.Stack.Push(-Call.Stack.Pop(instruction.Type1).Conv<double>(), retType);
        return (ExecutionResult.Success, null);
    }

    public static (ExecutionResult, object?) AND(UndertaleInstruction instruction)
    {
        var retType = GetMathReturnType(instruction);
        // should other binary types handle ops?
        var intTwo = Call.Stack.Pop(instruction.Type1).Conv<long>();
        var intOne = Call.Stack.Pop(instruction.Type2).Conv<long>();

        Call.Stack.Push(intOne & intTwo, retType);
        return (ExecutionResult.Success, null);
    }

    public static (ExecutionResult, object?) OR(UndertaleInstruction instruction)
    {
        var retType = GetMathReturnType(instruction);
        var intTwo = Call.Stack.Pop(instruction.Type1).Conv<long>();
        var intOne = Call.Stack.Pop(instruction.Type2).Conv<long>();

        Call.Stack.Push(intOne | intTwo, retType);
        return (ExecutionResult.Success, null);
    }

    public static (ExecutionResult, object?) XOR(UndertaleInstruction instruction)
    {
        var retType = GetMathReturnType(instruction);
        var intTwo = Call.Stack.Pop(instruction.Type1).Conv<long>();
        var intOne = Call.Stack.Pop(instruction.Type2).Conv<long>();

        Call.Stack.Push(intOne ^ intTwo, retType);
        return (ExecutionResult.Success, null);
    }

    public static (ExecutionResult, object?) NOT(UndertaleInstruction instruction)
    {
        switch (instruction.Type1)
        {
            case UndertaleInstruction.DataType.Boolean:
                Call.Stack.Push(!Call.Stack.Pop(UndertaleInstruction.DataType.Boolean).Conv<bool>(), UndertaleInstruction.DataType.Boolean);
                return (ExecutionResult.Success, null);
            default:
                Call.Stack.Push(~Call.Stack.Pop(instruction.Type1).Conv<long>(), instruction.Type2);
                return (ExecutionResult.Success, null);
        }
    }
    public static (ExecutionResult, object?) SHL(UndertaleInstruction instruction)
    {
        var retType = GetMathReturnType(instruction);
        // is this the right order?
        var intTwo = Call.Stack.Pop(instruction.Type1).Conv<int>();
        var intOne = Call.Stack.Pop(instruction.Type2).Conv<long>();

        Call.Stack.Push(intOne << intTwo, retType);
        return (ExecutionResult.Success, null);
    }
    
    public static (ExecutionResult, object?) SHR(UndertaleInstruction instruction)
    {
        var retType = GetMathReturnType(instruction);
        // is this the right order?
        var intTwo = Call.Stack.Pop(instruction.Type1).Conv<int>();
        var intOne = Call.Stack.Pop(instruction.Type2).Conv<long>();

        Call.Stack.Push(intOne >> intTwo, retType);
        return (ExecutionResult.Success, null);
    }
}

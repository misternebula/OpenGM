﻿using System.Drawing.Drawing2D;

namespace OpenGM.VirtualMachine;

public static partial class VMExecutor
{
	// see https://github.com/UnderminersTeam/Underanalyzer/blob/main/Underanalyzer/Decompiler/AST/Nodes/BinaryNode.cs#L40
	public static VMType GetMathReturnType(VMCodeInstruction instruction)
	{
		if (instruction.Opcode == VMOpcode.CMP)
		{
			// Comparisons are always boolean.
			return VMType.b;
		}

		if (instruction.TypeTwo == VMType.None)
		{
			return instruction.TypeOne;
		}

		// Choose whichever type has a higher bias, or if equal, the smaller numerical data type value.
		var bias1 = StackTypeBias(instruction.TypeOne);
		var bias2 = StackTypeBias(instruction.TypeTwo);
		return bias1 == bias2
			? (VMType)CustomMath.Min((int)instruction.TypeOne, (int)instruction.TypeTwo) // BUG: this is different enum order than UnderAnalyzer 
			: (bias1 > bias2) ? instruction.TypeOne : instruction.TypeTwo;
	}

	public static int StackTypeBias(VMType type)
	{
		return type switch
		{
			VMType.i or VMType.b or VMType.s => 0,
			VMType.d or VMType.l => 1,
			VMType.v => 2,
			_ => throw new NotImplementedException()
		};
	}

	public static (ExecutionResult, object?) ADD(VMCodeInstruction instruction)
	{
		var valTwo = Ctx.Stack.Pop(instruction.TypeOne);
		var valOne = Ctx.Stack.Pop(instruction.TypeTwo);

		var retType = GetMathReturnType(instruction);

		var hasString = instruction.TypeOne == VMType.s || instruction.TypeTwo == VMType.s;
		var variableIsString = valOne is string || valTwo is string;

		if (hasString || variableIsString)
		{
			// strings need to concat
			Ctx.Stack.Push(valOne.Conv<string>() + valTwo.Conv<string>(), retType);
		}
		else
		{
			Ctx.Stack.Push(valOne.Conv<double>() + valTwo.Conv<double>(), retType);
		}

		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object?) SUB(VMCodeInstruction instruction)
	{
		var retType = GetMathReturnType(instruction);
		var numTwo = Ctx.Stack.Pop(instruction.TypeOne).Conv<double>();
		var numOne = Ctx.Stack.Pop(instruction.TypeTwo).Conv<double>();

		Ctx.Stack.Push(numOne - numTwo, retType);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object?) MUL(VMCodeInstruction instruction)
	{
		var retType = GetMathReturnType(instruction);
		var numTwo = Ctx.Stack.Pop(instruction.TypeOne).Conv<double>();
		var numOne = Ctx.Stack.Pop(instruction.TypeTwo).Conv<double>();

		Ctx.Stack.Push(numOne * numTwo, retType);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object?) DIV(VMCodeInstruction instruction)
	{
		var retType = GetMathReturnType(instruction);
		var numTwo = Ctx.Stack.Pop(instruction.TypeOne).Conv<double>();
		var numOne = Ctx.Stack.Pop(instruction.TypeTwo).Conv<double>();

		Ctx.Stack.Push(numOne / numTwo, retType);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object?) REM(VMCodeInstruction instruction)
	{
		var retType = GetMathReturnType(instruction);
		var numTwo = Ctx.Stack.Pop(instruction.TypeOne).Conv<double>();
		var numOne = Ctx.Stack.Pop(instruction.TypeTwo).Conv<double>();

		Ctx.Stack.Push(numOne % numTwo, retType);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object?) MOD(VMCodeInstruction instruction)
	{
		var retType = GetMathReturnType(instruction);
		var numTwo = Ctx.Stack.Pop(instruction.TypeOne).Conv<double>();
		var numOne = Ctx.Stack.Pop(instruction.TypeTwo).Conv<double>();

		Ctx.Stack.Push(CustomMath.Mod(numOne, numTwo), retType);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object?) NEG(VMCodeInstruction instruction)
	{
		var retType = GetMathReturnType(instruction);
		Ctx.Stack.Push(-Ctx.Stack.Pop(instruction.TypeOne).Conv<double>(), retType);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object?) AND(VMCodeInstruction instruction)
	{
		var retType = GetMathReturnType(instruction);
		// should other binary types handle ops?
		var intTwo = Ctx.Stack.Pop(instruction.TypeOne).Conv<long>();
		var intOne = Ctx.Stack.Pop(instruction.TypeTwo).Conv<long>();

		Ctx.Stack.Push(intOne & intTwo, retType);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object?) OR(VMCodeInstruction instruction)
	{
		var retType = GetMathReturnType(instruction);
		var intTwo = Ctx.Stack.Pop(instruction.TypeOne).Conv<long>();
		var intOne = Ctx.Stack.Pop(instruction.TypeTwo).Conv<long>();

		Ctx.Stack.Push(intOne | intTwo, retType);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object?) XOR(VMCodeInstruction instruction)
	{
		var retType = GetMathReturnType(instruction);
		var intTwo = Ctx.Stack.Pop(instruction.TypeOne).Conv<long>();
		var intOne = Ctx.Stack.Pop(instruction.TypeTwo).Conv<long>();

		Ctx.Stack.Push(intOne ^ intTwo, retType);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object?) NOT(VMCodeInstruction instruction)
	{
		switch (instruction.TypeOne)
		{
			case VMType.b:
				Ctx.Stack.Push(!Ctx.Stack.Pop(VMType.b).Conv<bool>(), VMType.b);
				return (ExecutionResult.Success, null);
			default:
				Ctx.Stack.Push(~Ctx.Stack.Pop(instruction.TypeOne).Conv<long>(), instruction.TypeOne);
				return (ExecutionResult.Success, null);
		}
	}
	public static (ExecutionResult, object?) SHL(VMCodeInstruction instruction)
	{
		var retType = GetMathReturnType(instruction);
		// is this the right order?
		var intTwo = Ctx.Stack.Pop(instruction.TypeOne).Conv<int>();
		var intOne = Ctx.Stack.Pop(instruction.TypeTwo).Conv<long>();

		Ctx.Stack.Push(intOne << intTwo, retType);
		return (ExecutionResult.Success, null);
	}
	
	public static (ExecutionResult, object?) SHR(VMCodeInstruction instruction)
	{
		var retType = GetMathReturnType(instruction);
		// is this the right order?
		var intTwo = Ctx.Stack.Pop(instruction.TypeOne).Conv<int>();
		var intOne = Ctx.Stack.Pop(instruction.TypeTwo).Conv<long>();

		Ctx.Stack.Push(intOne >> intTwo, retType);
		return (ExecutionResult.Success, null);
	}
}

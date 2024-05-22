namespace DELTARUNITYStandalone;

public static partial class VMExecutor
{
	public static (ExecutionResult, object) ADD(VMScriptInstruction instruction)
	{
		var valTwo = Ctx.Stack.Pop();
		var valOne = Ctx.Stack.Pop();

		var hasString = instruction.TypeOne == VMType.s || instruction.TypeTwo == VMType.s;
		var variableIsString = (instruction.TypeOne == VMType.v && valOne is string) || (instruction.TypeTwo == VMType.v && valTwo is string);

		if (hasString || variableIsString)
		{
			// strings need to concat
			var stringOne = Conv<string>(valOne);
			var stringTwo = Conv<string>(valTwo);
			Ctx.Stack.Push(stringOne + stringTwo);
		}
		else
		{
			// technically should convert using TypeOne and TypeTwo, but later instructions convert anyway so it's fine
			Ctx.Stack.Push(Conv<double>(valOne) + Conv<double>(valTwo));
		}

		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object) SUB(VMScriptInstruction instruction)
	{
		var numTwo = Conv<double>(Ctx.Stack.Pop());
		var numOne = Conv<double>(Ctx.Stack.Pop());

		Ctx.Stack.Push(numOne - numTwo);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object) MUL(VMScriptInstruction instruction)
	{
		var numTwo = Conv<double>(Ctx.Stack.Pop());
		var numOne = Conv<double>(Ctx.Stack.Pop());

		Ctx.Stack.Push(numOne * numTwo);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object) DIV(VMScriptInstruction instruction)
	{
		var numTwo = Conv<double>(Ctx.Stack.Pop());
		var numOne = Conv<double>(Ctx.Stack.Pop());

		Ctx.Stack.Push(numOne / numTwo);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object) REM(VMScriptInstruction instruction)
	{
		var numTwo = Conv<double>(Ctx.Stack.Pop());
		var numOne = Conv<double>(Ctx.Stack.Pop());

		Ctx.Stack.Push(numOne % numTwo);
		return (ExecutionResult.Success, null);
	}

	// TODO: distinguish between above and below
	// Remainder and Modulus have the same value for positive values.
	// % in C# is NOT modulo - it's remainder.
	// Modulus always has the same sign as the divisor, and remainder has the same sign as the dividend
	// (dividend / divisor = quotient)
	// 10 REM 3 = 1
	// -10 REM 3 = -1
	// 10 REM -3 = 1
	// -10 REM -3 = -1
	// 10 MOD 3 = 1
	// -10 MOD 3 = 2
	// 10 MOD -3 = -2
	// -10 MOD -3 = -1

	public static (ExecutionResult, object) MOD(VMScriptInstruction instruction)
	{
		var numTwo = Conv<double>(Ctx.Stack.Pop());
		var numOne = Conv<double>(Ctx.Stack.Pop());

		Ctx.Stack.Push(numOne % numTwo);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object) NEG(VMScriptInstruction instruction)
	{
		Ctx.Stack.Push(-Conv<double>(Ctx.Stack.Pop()));
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object) AND(VMScriptInstruction instruction)
	{
		// should other binary types handle ops?
		var intTwo = Conv<int>(Ctx.Stack.Pop());
		var intOne = Conv<int>(Ctx.Stack.Pop());

		Ctx.Stack.Push(intOne & intTwo);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object) OR(VMScriptInstruction instruction)
	{
		var intTwo = Conv<int>(Ctx.Stack.Pop());
		var intOne = Conv<int>(Ctx.Stack.Pop());

		Ctx.Stack.Push(intOne | intTwo);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object) XOR(VMScriptInstruction instruction)
	{
		var intTwo = Conv<int>(Ctx.Stack.Pop());
		var intOne = Conv<int>(Ctx.Stack.Pop());

		Ctx.Stack.Push(intOne ^ intTwo);
		return (ExecutionResult.Success, null);
	}

	public static (ExecutionResult, object) NOT(VMScriptInstruction instruction)
	{
		switch (instruction.TypeOne)
		{
			case VMType.b:
				Ctx.Stack.Push(!Conv<bool>(Ctx.Stack.Pop()));
				return (ExecutionResult.Success, null);
			default:
				DebugLog.LogError($"Don't know how to NOT {instruction.TypeOne}");
				return (ExecutionResult.Success, null);
		}
	}
}

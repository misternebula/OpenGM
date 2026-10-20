using OpenGM.SerializedFiles;

namespace OpenGM.VirtualMachine;

public static partial class VMExecutor
{
    public static (ExecutionResult, object?) CMP(VMCodeInstruction instruction)
    {
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

        return (ExecutionResult.Success, null);
    }
}

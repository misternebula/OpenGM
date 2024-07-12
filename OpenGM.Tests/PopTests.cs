using OpenGM.VirtualMachine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;

namespace OpenGM.Tests;

[TestClass]
public class PopTests
{
	[TestMethod]
	public void TestPushGlobal()
	{
		var script = new VMScript();

		script.Name = "PushGlobal";
		script.LocalVariables = new();
		script.Instructions = new List<VMScriptInstruction>()
		{
			new()
			{
				Opcode = VMOpcode.PUSHI, TypeOne = VMType.i,
				IntData = 5
			},

			new()
			{
				Opcode = VMOpcode.CONV, TypeOne = VMType.i, TypeTwo = VMType.v
			},

			new()
			{
				Opcode = VMOpcode.POP, TypeOne = VMType.v, TypeTwo = VMType.v,
				StringData = "global.testVar"
			}
		};

		VMExecutor.ExecuteScript(script, null);

		Assert.IsTrue(VariableResolver.GlobalVariables.ContainsKey("testVar"));

		Assert.AreEqual(5, VariableResolver.GlobalVariables["testVar"].Conv<int>());
	}

	[TestMethod]
	public void TestPushGlobalArrayIndex()
	{
		var script = new VMScript();

		script.Name = "PushGlobalArrayIndex";
		script.LocalVariables = new();
		script.Instructions = new List<VMScriptInstruction>()
		{
			new() { Opcode = VMOpcode.PUSH, TypeOne = VMType.s, StringData = "Test String 0" },
			new() { Opcode = VMOpcode.CONV, TypeOne = VMType.s, TypeTwo = VMType.v },
			new() { Opcode = VMOpcode.PUSHI, TypeOne = VMType.e, ShortData = -5 },
			new() { Opcode = VMOpcode.PUSHI, TypeOne = VMType.e, ShortData = 0 },
			new() { Opcode = VMOpcode.POP, TypeOne = VMType.v, TypeTwo = VMType.v, StringData = "[array]self.testArray" },
			new() { Opcode = VMOpcode.PUSH, TypeOne = VMType.s, StringData = "Test String 1" },
			new() { Opcode = VMOpcode.CONV, TypeOne = VMType.s, TypeTwo = VMType.v },
			new() { Opcode = VMOpcode.PUSHI, TypeOne = VMType.e, ShortData = -5 },
			new() { Opcode = VMOpcode.PUSHI, TypeOne = VMType.e, ShortData = 1 },
			new() { Opcode = VMOpcode.POP, TypeOne = VMType.v, TypeTwo = VMType.v, StringData = "[array]self.testArray" }
		};

		VMExecutor.ExecuteScript(script, null);

		Assert.IsTrue(VariableResolver.GlobalVariables.ContainsKey("testArray"));

		var array = VariableResolver.GlobalVariables["testArray"].Conv<IList>();

		Assert.AreEqual("Test String 0", array[0]);
		Assert.AreEqual("Test String 1", array[1]);
	}

	[TestMethod]
	public void TestPushMultiDimensionalArray()
	{
		var script = new VMScript();

		script.Name = "PushMultiDimensionalArray";
		script.LocalVariables = new();
		script.Instructions = new List<VMScriptInstruction>()
		{
			new() { Raw = "push.s \"Test String 1 1\"", Opcode = VMOpcode.PUSH, TypeOne = VMType.s, StringData = "Test String 1 1" },
			new() { Raw = "conv.s.v", Opcode = VMOpcode.CONV, TypeOne = VMType.s, TypeTwo = VMType.v },

			new() { Raw = "pushi.e -5", Opcode = VMOpcode.PUSHI, TypeOne = VMType.e, ShortData = -5 },
			new() { Raw = "pushi.e 1", Opcode = VMOpcode.PUSHI, TypeOne = VMType.e, ShortData = 1 },

			new() { Raw = "push.v [arraypopaf]self.mdArray", Opcode = VMOpcode.PUSH, TypeOne = VMType.v, StringData = "[arraypopaf]self.mdArray" },
			new() { Raw = "pushi.e 1", Opcode = VMOpcode.PUSHI, TypeOne = VMType.e, ShortData = 1 },
			new() { Raw = "popaf.e", Opcode = VMOpcode.POPAF, TypeOne = VMType.e},
		};

		VMExecutor.ExecuteScript(script, null);

		Assert.IsTrue(VariableResolver.GlobalVariables.ContainsKey("mdArray"));

		var array = VariableResolver.GlobalVariables["mdArray"].Conv<IList>();

		Assert.AreEqual("Test String 1 1", array[1].Conv<IList>()[1]);
	}
}

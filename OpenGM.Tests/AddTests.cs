using OpenGM.VirtualMachine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace OpenGM.Tests;

[TestClass]
public class AddTests
{
	[TestMethod]
	public void TestAddSimple()
	{
		/*
		var script = new VMScript();

		script.Name = "AddSimple";
		script.LocalVariables = new();
		script.Instructions = new List<VMScriptInstruction>()
		{
			new()
			{
				Opcode = VMOpcode.PUSHI, TypeOne = VMType.i,
				IntData = 1
			},

			new()
			{
				Opcode = VMOpcode.PUSHI, TypeOne = VMType.i,
				IntData = 1
			},

			new()
			{
				Opcode = VMOpcode.ADD, TypeOne = VMType.i, TypeTwo = VMType.i
			},

			new()
			{
				Opcode = VMOpcode.CONV,
				TypeOne = VMType.i,
				TypeTwo = VMType.v
			},

			new()
			{
				Opcode = VMOpcode.RET, TypeOne = VMType.v
			}
		};

		Assert.AreEqual(2, VMExecutor.ExecuteScript(script, null).Conv<int>());
		*/

		var result = TestUtils.ExecuteScript(
			"AddSimple",
			"""
			:[0]
			pushi.i 1
			pushi.i 1
			add.i.i
			conv.i.v
			ret.v
			"""
		).Conv<int>();

		Assert.AreEqual(2, result);
	}
}

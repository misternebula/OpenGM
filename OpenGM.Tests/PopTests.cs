﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenGM.VirtualMachine;
using System.Collections;

namespace OpenGM.Tests;

[TestClass]
public class PopTests
{
	[TestMethod]
	public void TestPushGlobal()
	{
		TestUtils.ExecuteScript(
			"global.testVar = 5",
			"""
			:[0]
			pushi.i 5
			conv.i.v
			pop.v.v global.testVar
			"""
		);

		Assert.IsTrue(VariableResolver.GlobalVariables.ContainsKey("testVar"));

		Assert.AreEqual(5, VariableResolver.GlobalVariables["testVar"].Conv<int>());
	}

	[TestMethod]
	public void TestPushGlobalArrayIndex()
	{
		TestUtils.ExecuteScript(
			"testArray[0] = \"Test String 0\"; testArray[1] = \"Test String 1\"",
			"""
			:[0]
			push.s "Test String 0"@0
			conv.s.v

			pushi.e -5
			pushi.e 0
			pop.v.v [array]self.testArray

			push.s "Test String 1"@0
			conv.s.v

			pushi.e -5
			pushi.e 1
			pop.v.v [array]self.testArray
			"""
		);

		Assert.IsTrue(VariableResolver.GlobalVariables.ContainsKey("testArray"));

		var array = VariableResolver.GlobalVariables["testArray"].Conv<IList<object?>>();

		Assert.AreEqual("Test String 0", array[0]);
		Assert.AreEqual("Test String 1", array[1]);
	}

	[TestMethod]
	public void TestPushMultiDimensionalArray()
	{
		TestUtils.ExecuteScript(
			"global.mdArray[1][1] = \"Test String 1 1\"",
			"""
			:[0]
			push.s "Test String 1 1"@0
			conv.s.v
			
			pushi.e -5
			pushi.e 1
			push.v [arraypopaf]self.mdArray
			
			pushi.e 1
			popaf.e
			"""
		);

		Assert.IsTrue(VariableResolver.GlobalVariables.ContainsKey("mdArray"));

		var array = VariableResolver.GlobalVariables["mdArray"].Conv<IList<object?>>();

		Assert.AreEqual("Test String 1 1", array[1].Conv<IList<object?>>()[1]);
	}

	[TestMethod]
	public void ArrayTestFromGame()
	{
		VariableResolver.GlobalVariables["NAMEX"] = new object?[] { new object?[] { "name x" } };
		VariableResolver.GlobalVariables["NAMEY"] = new object?[] { new object?[] { "name y" } };

		TestUtils.ExecuteScript(
			"global.HEARTX = global.NAMEX[0][0]; global.HEARTY = global.NAMEY[0][0]",
			"""
			:[0]
			pushi.e -5
			pushi.e 0
			push.v [arraypushaf]self.NAMEX

			pushi.e 0
			pushaf.e

			pop.v.v global.HEARTX

			pushi.e -5
			pushi.e 0
			push.v [arraypushaf]self.NAMEY

			pushi.e 0
			pushaf.e

			pop.v.v global.HEARTY
			"""
		);

		Assert.AreEqual(VariableResolver.GlobalVariables["HEARTX"], VariableResolver.GlobalVariables["NAMEX"].Conv<IList<object?>>()[0].Conv<IList<object?>>()[0]);
		Assert.AreEqual(VariableResolver.GlobalVariables["HEARTY"], VariableResolver.GlobalVariables["NAMEY"].Conv<IList<object?>>()[0].Conv<IList<object?>>()[0]);
	}
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenGM.VirtualMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.Tests;

[TestClass]
public class StructTests
{
	[TestMethod]
	public void TestSingleStruct()
	{
		TestUtils.ExecuteScript(
			"a = 1; b = \"test\"; c = new scr_struct(a, b); d = c.a; e = c.b;",
			"""
			:[0]
			pushi.e 1
			conv.e.v
			pop.v.v local.a
			pushi.s "test"@1
			conv.s.v
			pop.v.v local.b
			
			pushloc.v local.b
			pushloc.v local.a
			push.i gml_Script_scr_struct
			conv.i.v
			call.i @@NewGMLObject@@(argc=3)
			pop.v.v global.c
			
			pushloc.v global.c
			pushi.e -9
			push.v [stacktop]self.a
			pop.v.v global.d
			
			pushloc.v global.c
			pushi.e -9
			push.v [stacktop]self.b
			pop.v.v global.e
			
			b [end]
			
			> gml_Script_scr_struct (locals=0, argc=2)
			:[23]
			push.v arg.argument0
			pop.v.v self.a
			push.v arg.argument1
			pop.v.v self.b
			exit.i
			
			:[end]
			"""
		);

		Assert.IsTrue(VariableResolver.GlobalVariables.ContainsKey("c"));
		Assert.IsTrue(VariableResolver.GlobalVariables.ContainsKey("d"));
		Assert.IsTrue(VariableResolver.GlobalVariables["d"].Conv<int>() == 1);
		Assert.IsTrue(VariableResolver.GlobalVariables["e"].Conv<string>() == "test");
	}
}

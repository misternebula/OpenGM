using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenGM.VirtualMachine;

namespace OpenGM.Tests;

[TestClass]
public class AddTests
{
	[TestMethod]
	public void TestAddSimple()
	{
		var result = TestUtils.ExecuteScript(
			"1 + 1 = 2",
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

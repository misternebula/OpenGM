using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenGM.Loading;
using OpenGM.VirtualMachine;

namespace OpenGM.Tests;

[TestClass]
public class MessagePackTests
{
	[TestMethod]
	public void TestStream()
	{
		var script = new VMScript
		{
			Name = "hello"
		};

		Console.WriteLine("write");
		using (var stream = File.OpenWrite("test file"))
		{
			stream.Write(1);
			stream.Write(2);
			stream.Write("hello");
			stream.Write(script);
		}

		Console.WriteLine("read");
		using (var stream = File.OpenRead("test file"))
		{
			Assert.AreEqual(1, stream.Read<int>());
			Assert.AreEqual(2, stream.Read<int>());
			Assert.AreEqual("hello", stream.Read<string>());
			Assert.AreEqual("hello", stream.Read<VMScript>().Name);
		}
	}

	[TestCleanup]
	public void Cleanup() => File.Delete("test file");
}

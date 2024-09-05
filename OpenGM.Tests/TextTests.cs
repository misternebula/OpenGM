using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using OpenGM.SerializedFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.Tests;

[TestClass]
public class TextTests
{
	[TestMethod]
	public void NoWrapNoNewline()
	{
		var str = "123456789";

		var fontPath = @"C:\Users\Henry\Documents\GitHub\GMRunner\OpenGM.Tests\fnt_main.json";
		var text = File.ReadAllText(fontPath);
		var asset = JsonConvert.DeserializeObject<FontAsset>(text)!;

		TextManager.fontAsset = asset;

		var lines = TextManager.SplitText(str, -1, TextManager.fontAsset);

		Assert.IsTrue(lines.Count == 1);
		Assert.IsTrue(lines[0] == str);
	}

	[TestMethod]
	public void NoWrapOneNewline()
	{
		var str = "12345\n6789";

		var fontPath = @"C:\Users\Henry\Documents\GitHub\GMRunner\OpenGM.Tests\fnt_main.json";
		var text = File.ReadAllText(fontPath);
		var asset = JsonConvert.DeserializeObject<FontAsset>(text)!;

		TextManager.fontAsset = asset;

		var lines = TextManager.SplitText(str, -1, TextManager.fontAsset);

		Assert.IsTrue(lines.Count == 2);
	}

	[TestMethod]
	public void WrappingNoNewline()
	{
		var str = "123456789";

		var fontPath = @"C:\Users\Henry\Documents\GitHub\GMRunner\OpenGM.Tests\fnt_main.json";
		var text = File.ReadAllText(fontPath);
		var asset = JsonConvert.DeserializeObject<FontAsset>(text)!;

		TextManager.fontAsset = asset;

		var lines = TextManager.SplitText(str, 20, TextManager.fontAsset);

		Assert.IsTrue(lines.Count == 3);
	}
}

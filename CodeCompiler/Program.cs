using Newtonsoft.Json;
using OpenGM.Loading;
using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace CodeCompiler;

internal class Program
{
	static void Main(string[] args)
	{
		var gamePath = Path.GetFullPath(Environment.CurrentDirectory + "../../../OpenGM/bin/game");

		Console.WriteLine($"Creating FileStream...");
		using var stream = new FileStream(Path.Combine(gamePath, "chapter2_windows/data.win"), FileMode.Open, FileAccess.Read);
		Console.WriteLine($"Reading data.win...");
		var data = UndertaleIO.Read(stream);

		Console.WriteLine($"Finding GML scripts...");
		var scriptFiles = Directory.GetFiles("Scripts");

		var asmFolder = Path.Combine(gamePath, "replacement_scripts");
		Directory.CreateDirectory(asmFolder);

		Console.WriteLine($"Clearing output folder...");
		foreach (var file in Directory.GetFiles(asmFolder))
		{
			File.Delete(file);
		}

		foreach (var file in scriptFiles)
		{
			Console.WriteLine(file);

			var code = data.Code.First(x => x.Name.Content == Path.GetFileNameWithoutExtension(file));
			Console.WriteLine($" - Found code, replacing GML...");
			code.ReplaceGML(File.ReadAllText(file), data);
			Console.WriteLine($" - Dissassembling...");
			var asmFile = code.Disassemble(data.Variables, data.CodeLocals.For(code));
			Console.WriteLine($" - Converting to VMCode...");
			var vmCode = GameConverter.ConvertAssembly(asmFile);
			vmCode.Name = code.Name.Content;
			Console.WriteLine($" - Serializing and writing...");
			var json = JsonConvert.SerializeObject(vmCode);
			File.WriteAllText(Path.Combine(asmFolder, $"{code.Name.Content}.json"), json);
			File.WriteAllText(Path.Combine(asmFolder, $"{code.Name.Content}.asm"), asmFile);
		}
	}
}

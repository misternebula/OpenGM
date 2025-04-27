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

		var compileGroup = new CompileGroup(data);
		var codeNames = new List<string>();

		foreach (var file in scriptFiles)
		{
			Console.WriteLine(file);

			var codeName = Path.GetFileNameWithoutExtension(file);
			codeNames.Add(codeName);

			var code = data.Code.First(x => x.Name.Content == codeName);
			compileGroup.QueueCodeReplace(code, File.ReadAllText(file));
		}

		var result = compileGroup.Compile();

		if (!result.Successful)
		{
			throw new Exception();
		}

		foreach (var codeName in codeNames)
		{
			var code = data.Code.First(x => x.Name.Content == codeName);

			var asmFile = code.Disassemble(data.Variables, data.CodeLocals.For(code));
			var vmCode = GameConverter.ConvertAssembly(asmFile);
			vmCode.Name = code.Name.Content;
			var json = JsonConvert.SerializeObject(vmCode);
			File.WriteAllText(Path.Combine(asmFolder, $"{code.Name.Content}.json"), json);
			File.WriteAllText(Path.Combine(asmFolder, $"{code.Name.Content}.asm"), asmFile);
		}
	}
}

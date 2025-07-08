using System.Drawing;
using Newtonsoft.Json;
using OpenGM.Loading;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;

namespace CodeCompiler;

internal class Program
{
	private static string GamePath = "";

	static void Main(string[] args)
	{
		GamePath = Path.GetFullPath(Environment.CurrentDirectory + "../../../OpenGM/bin/game");

		CompileScripts(Path.Combine(GamePath, "data.win"), Directory.GetFiles("Scripts"));
		CompileScripts(GetChapterDataWin(1), GetChapterScripts(1));
		CompileScripts(GetChapterDataWin(2), GetChapterScripts(2));
		CompileScripts(GetChapterDataWin(3), GetChapterScripts(3));
		CompileScripts(GetChapterDataWin(4), GetChapterScripts(4));
	}

	public static string GetChapterDataWin(int chapterNum)
	{
		return Path.Combine(GamePath, $"chapter{chapterNum}_windows", "data.win");
	}

	public static string[]? GetChapterScripts(int chapterNum)
	{
		var scriptsFolder = Path.Combine("Scripts", $"chapter{chapterNum}_windows");
		return !Path.Exists(scriptsFolder) ? null : Directory.GetFiles(scriptsFolder);
	}

	public static void CompileScripts(string winPath, string[]? scriptPaths)
	{
		if (scriptPaths == null || scriptPaths.Length == 0)
		{
			return;
		}

		Console.WriteLine($"Creating FileStream...");
		using var stream = new FileStream(winPath, FileMode.Open, FileAccess.Read);
		Console.WriteLine($"Reading data.win...");
		var data = UndertaleIO.Read(stream);

		var asmFolder = Path.Combine(Path.GetDirectoryName(winPath)!, "replacement_scripts");
		Directory.CreateDirectory(asmFolder);

		Console.WriteLine($"Clearing output folder...");
		foreach (var file in Directory.GetFiles(asmFolder))
		{
			File.Delete(file);
		}

		var compileGroup = new CompileGroup(data);
		var codeNames = new List<string>();

		foreach (var file in scriptPaths)
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
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(result.PrintAllErrors(true));
			Console.ResetColor();
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

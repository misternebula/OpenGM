using OpenGM.SerializedFiles;
using Newtonsoft.Json.Linq;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Collections;
using System.Reflection;
using UndertaleModLib.Models;
using OpenTK.Graphics.OpenGL;
using OpenGM.Rendering;
using OpenGM.IO;
using OpenGM.Loading;
using StbVorbisSharp;

namespace OpenGM.VirtualMachine;
public static class ScriptResolver
{
	public static Dictionary<string, VMScript> ScriptsByName = new();
	public static Dictionary<int, VMScript> ScriptsByIndex = new();
	public static List<VMCode> GlobalInit = new();

	public static Dictionary<string, Func<object?[], object?>> BuiltInFunctions = new();

	public static void InitGMLFunctions()
	{
		var assembly = Assembly.GetExecutingAssembly();
		var methods = assembly.GetTypes()
			.SelectMany(t => t.GetMethods())
			.Where(m => m.GetCustomAttributes(typeof(GMLFunctionAttribute), false).Length > 0)
			.ToArray();

		foreach (var methodInfo in methods)
		{
			var func = (Func<object?[], object?>)Delegate.CreateDelegate(typeof(Func<object?[], object?>), methodInfo);
			var attributes = (GMLFunctionAttribute[])methodInfo.GetCustomAttributes(typeof(GMLFunctionAttribute), false);

			foreach (var attribute in attributes)
			{
				BuiltInFunctions.Add(attribute.FunctionName, func);
			}
		}

		DebugLog.LogInfo($"Registered {BuiltInFunctions.Count} GML functions.");
	}

	// any functions in here aren't in 2022.500 so idk where they go rn

	[GMLFunction("draw_background")]
	public static object? draw_background(object?[] args)
	{
		var index = args[0].Conv<int>();
		var x = args[1].Conv<double>();
		var y = args[2].Conv<double>();

		var background = GameLoader.Backgrounds[index];

		// TODO : handle tiling
		// TODO : handle strech
		// TODO : handle foreground
		// TODO : handle speed

		var sprite = background.Texture;

		if (sprite == null)
		{
			return null;
		}

		CustomWindow.Draw(new GMSpriteJob()
		{
			texture = sprite,
			screenPos = new Vector2d(x, y),
			blend = Color4.White
		});

		return null;
	}

	[GMLFunction("steam_initialised")]
	public static object? steam_initialised(object?[] args)
	{
		// todo : implement
		return false;
	}

	[GMLFunction("instance_create")]
	public static object? instance_create(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var obj = args[2].Conv<int>();

		return InstanceManager.instance_create(x, y, obj);
	}

	[GMLFunction("joystick_exists")]
	public static object? joystick_exists(object?[] args) => false; // TODO : implement

	[GMLFunction("game_change")]
	public static object? game_change(object?[] args)
	{
		var working_directory = args[0].Conv<string>();
		var launch_parameters = args[1].Conv<string>();

		var winLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game" + working_directory, "data.win");
		DebugLog.LogInfo($"game_change path:{winLocation} launch_parameters:{launch_parameters}");

		Entry.LoadGame(winLocation, launch_parameters.Split(" "));

		return null;
	}

	[GMLFunction("string_split")]
	public static object? string_split(object?[] args)
	{
		var str = args[0].Conv<string>();
		var delimiter = args[1].Conv<string>();

		var remove_empty = false;
		if (args.Length > 2)
		{
			remove_empty = args[2].Conv<bool>();
		}

		var max_splits = -1;
		if (args.Length > 3)
		{
			max_splits = args[3].Conv<int>();
		}

		var option = remove_empty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;

		var splits = str.Split(delimiter, option);

		if (max_splits != -1)
		{
			throw new NotImplementedException();
		}

		return splits;
	}

	[GMLFunction("steam_is_screenshot_requested")]
	public static object? steam_is_screenshot_requested(object?[] args)
	{
		// TODO : implement
		return false;
	}
}
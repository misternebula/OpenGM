using OpenTK.Mathematics;
using System.Reflection;
using OpenGM.Rendering;
using OpenGM.IO;
using OpenGM.Loading;

namespace OpenGM.VirtualMachine;
public static class ScriptResolver
{
	public static Dictionary<string, VMScript> ScriptsByName = new();
	public static Dictionary<int, VMScript> ScriptsByIndex = new();
	public static List<VMCode> GlobalInit = new();
	
	public static Dictionary<string, GMLFunctionType> BuiltInFunctions = new();

	public static void InitGMLFunctions()
	{
		GMLFunctionType MakeStubFunction(GMLFunctionType function, string functionName) 
		{
			return (object?[] args) =>
			{
				DebugLog.LogVerbose($"{functionName} not implemented.");
				return function.Invoke(args);
			};
		};

		var assembly = Assembly.GetExecutingAssembly();
		var methods = assembly.GetTypes()
			.SelectMany(t => t.GetMethods())
			.Where(m => m.GetCustomAttributes(typeof(GMLFunctionAttribute), false).Length > 0)
			.ToArray();

		var addedCount = 0;
		var stubCount = 0;

		foreach (var methodInfo in methods)
		{
			var func = (GMLFunctionType)Delegate.CreateDelegate(typeof(GMLFunctionType), methodInfo);
			var attributes = (GMLFunctionAttribute[])methodInfo.GetCustomAttributes(typeof(GMLFunctionAttribute), false);

			foreach (var attribute in attributes)
			{
				// game version isnt being initialized at this point yet so this is commented out for now

				/*
				if (attribute.AddedVersion != null) {
					if (VersionManager.IsGMS1() && attribute.AddedVersion > VersionManager.WADVersion)
					{
						continue;
					}
				}

				if (attribute.RemovedVersion != null) {
					if (VersionManager.IsGMS1() && attribute.RemovedVersion <= VersionManager.WADVersion)
					{
						continue;
					}

					if (VersionManager.IsGMS2() && attribute.RemovedVersion == GMVersion.GMS2)
					{
						continue;
					}
				}
				*/

				var newFunc = func;
				if (attribute.FunctionFlags.HasFlag(GMLFunctionFlags.Stub))
				{
					newFunc = MakeStubFunction(func, attribute.FunctionName);
					stubCount++;
				}

				BuiltInFunctions.Add(attribute.FunctionName, newFunc);
				addedCount++;
			}
		}

		var totalCount = BuiltInFunctions.Count;
		var realCount = addedCount - stubCount;
		DebugLog.LogInfo($"Registered {addedCount}/{totalCount} GML functions ({realCount} implemented, {stubCount} stubbed.)");
	}

	// any functions in here aren't in 2022.500 so idk where they go rn

	[GMLFunction("draw_background", before: "2.0.0.0")]
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

	[GMLFunction("instance_create", before: "2.0.0.0")]
	public static object? instance_create(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var obj = args[2].Conv<int>();

		return InstanceManager.instance_create(x, y, obj);
	}

	[GMLFunction("joystick_exists", GMLFunctionFlags.Stub)]
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

	[GMLFunction("steam_is_screenshot_requested", GMLFunctionFlags.Stub)]
	public static object? steam_is_screenshot_requested(object?[] args)
	{
		// TODO : implement
		return false;
	}

	public delegate object? GMLFunctionType(object?[] args);
}
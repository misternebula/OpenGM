using OpenGM.VirtualMachine;
using Newtonsoft.Json;
using UndertaleModLib.Models;

namespace OpenGM.SerializedFiles;

/// <summary>
/// "template" for an object. analagous to a class
/// </summary>
[Serializable]
public class ObjectDefinition
{
	public int AssetId;

	public string Name = null!;
	public int sprite = -1;
	public bool visible = true;
	public bool solid;
	public bool persistent;
	public int textureMaskId = -1;

	/// <summary>
	/// Stores index data that is used to populate the rest of this class
	/// </summary>
	public ObjectDefinitionStorage FileStorage = null!;

	/// <summary>
	/// analogous to a superclass
	/// </summary>
	[JsonIgnore]
	public ObjectDefinition? parent;

	[JsonIgnore]
	public VMScript? CreateScript;
	[JsonIgnore]
	public VMScript? DestroyScript;

	[JsonIgnore]
	public Dictionary<int, VMScript> AlarmScript = new();
	[JsonIgnore]
	public Dictionary<EventSubtypeStep, VMScript> StepScript = new();
	[JsonIgnore]
	public Dictionary<int, VMScript> CollisionScript = new();
	//keyboard
	//mouse
	[JsonIgnore]
	public Dictionary<EventSubtypeOther, VMScript> OtherScript = new();
	[JsonIgnore]
	public Dictionary<EventSubtypeDraw, VMScript> DrawScript = new();
	//keypress
	//keyrelease
	//trigger
	[JsonIgnore]
	public VMScript? CleanUpScript;
	//gesture
	[JsonIgnore]
	public VMScript? PreCreateScript;
}

[Serializable]
public class ObjectDefinitionStorage
{
	public int ParentID;
	public int CreateScriptID;
	public int DestroyScriptID;
	public Dictionary<int, int> AlarmScriptIDs = new();
	public Dictionary<EventSubtypeStep, int> StepScriptIDs = new();
	public Dictionary<int, int> CollisionScriptIDs = new();
	//keyboard
	//mouse
	public Dictionary<EventSubtypeOther, int> OtherScriptIDs = new();
	public Dictionary<EventSubtypeDraw, int> DrawScriptIDs = new();
	//keypress
	//keyrelease
	//trigger
	public int CleanUpScriptID;
	//gesture
	public int PreCreateScriptID;
}

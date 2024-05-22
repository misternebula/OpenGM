using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UndertaleModLib.Models;

namespace DELTARUNITYStandalone;

[Serializable]
public class ObjectDefinition
{
	public int AssetId;

	public string Name;
	public int sprite = -1;
	public bool visible = true;
	public bool solid;
	public bool persistent;
	public int textureMaskId = -1;

	// Stores index data that is used to populate the rest of this class
	public ObjectDefinitionStorage FileStorage;

	/// <summary>
	/// analogous to a superclass
	/// </summary>
	[JsonIgnore]
	public ObjectDefinition parent;

	[JsonIgnore]
	public VMScript CreateScript;
	[JsonIgnore]
	public VMScript DestroyScript;

	[JsonIgnore]
	public Dictionary<uint, VMScript> AlarmScript = new();
	[JsonIgnore]
	public Dictionary<EventSubtypeStep, VMScript> StepScript = new();
	[JsonIgnore]
	public Dictionary<uint, VMScript> CollisionScript = new();
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
	public VMScript CleanUpScript;
	//gesture
	[JsonIgnore]
	public VMScript PreCreateScript;
}

[Serializable]
public class ObjectDefinitionStorage
{
	public int ParentID;
	public int CreateScriptID;
	public int DestroyScriptID;
	public Dictionary<uint, int> AlarmScriptIDs = new();
	public Dictionary<EventSubtypeStep, int> StepScriptIDs = new();
	public Dictionary<uint, int> CollisionScriptIDs = new();
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

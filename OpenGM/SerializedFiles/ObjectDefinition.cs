﻿using MemoryPack;
using OpenGM.VirtualMachine;
using Newtonsoft.Json;
using UndertaleModLib.Models;

namespace OpenGM.SerializedFiles;

/// <summary>
/// "template" for an object. analagous to a class
/// </summary>
[MemoryPackable]
public partial class ObjectDefinition
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
	[MemoryPackIgnore]
	public ObjectDefinition? parent;

	[MemoryPackIgnore]
	public VMCode? CreateCode;
	[MemoryPackIgnore]
	public VMCode? DestroyScript;

	[MemoryPackIgnore]
	public Dictionary<int, VMCode> AlarmScript = new();
	[MemoryPackIgnore]
	public Dictionary<EventSubtypeStep, VMCode> StepScript = new();
	[MemoryPackIgnore]
	public Dictionary<int, VMCode> CollisionScript = new();
	//keyboard
	//mouse
	[MemoryPackIgnore]
	public Dictionary<EventSubtypeOther, VMCode> OtherScript = new();
	[MemoryPackIgnore]
	public Dictionary<EventSubtypeDraw, VMCode> DrawScript = new();
	//keypress
	//keyrelease
	//trigger
	[MemoryPackIgnore]
	public VMCode? CleanUpScript;
	//gesture
	[MemoryPackIgnore]
	public VMCode? PreCreateScript;
}

[MemoryPackable]
public partial class ObjectDefinitionStorage
{
	public int ParentID;
	public int CreateCodeID;
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

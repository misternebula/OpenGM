using OpenGM.VirtualMachine;
using UndertaleModLib.Models;

namespace OpenGM.SerializedFiles;

/// <summary>
/// "template" for an object. analagous to a class.
/// since GM objects have parent-child hierarchy we need this separation between the class and the instance (the class has the functions, the instance has the variables)
/// </summary>
public class ObjectDefinition
{
    public int AssetId;

    public string Name = null!;
    public int sprite = -1;
    public bool visible = true;
    public bool solid;
    public int depth;
    public bool persistent;
    public int textureMaskId = -1;

    /// <summary>
    /// Stores index data that is used to populate the rest of this class
    /// </summary>
    public ObjectDefinitionStorage FileStorage = null!;

    /// <summary>
    /// analogous to a superclass
    /// </summary>
    public ObjectDefinition? parent;

    public UndertaleCode? CreateCode;
    public UndertaleCode? DestroyScript;

    public Dictionary<int, UndertaleCode> AlarmScript = new();
    public Dictionary<EventSubtypeStep, UndertaleCode> StepScript = new();
    public Dictionary<int, UndertaleCode> CollisionScript = new();
    public Dictionary<EventSubtypeKey, UndertaleCode> KeyboardScripts = new();
    //mouse
    public Dictionary<EventSubtypeOther, UndertaleCode> OtherScript = new();
    public Dictionary<EventSubtypeDraw, UndertaleCode> DrawScript = new();
    public Dictionary<EventSubtypeKey, UndertaleCode> KeyPressScripts = new();
    public Dictionary<EventSubtypeKey, UndertaleCode> KeyReleaseScripts = new();
    //trigger
    public UndertaleCode? CleanUpScript;
    //gesture
    public UndertaleCode? PreCreateScript;
}

public class ObjectDefinitionStorage
{
    public int ParentID;
    public int CreateCodeID;
    public int DestroyScriptID;
    public Dictionary<int, int> AlarmScriptIDs = new();
    public Dictionary<EventSubtypeStep, int> StepScriptIDs = new();
    public Dictionary<int, int> CollisionScriptIDs = new();
    public Dictionary<EventSubtypeKey, int> KeyboardScriptIDs = new();
    //mouse
    public Dictionary<EventSubtypeOther, int> OtherScriptIDs = new();
    public Dictionary<EventSubtypeDraw, int> DrawScriptIDs = new();
    public Dictionary<EventSubtypeKey, int> KeyPressScriptIDs = new();
    public Dictionary<EventSubtypeKey, int> KeyReleaseScriptIDs = new();
    //trigger
    public int CleanUpScriptID;
    //gesture
    public int PreCreateScriptID;
}

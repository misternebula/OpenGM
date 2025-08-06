using OpenGM.IO;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using OpenTK.Mathematics;
using UndertaleModLib.Models;
using EventType = OpenGM.VirtualMachine.EventType;

namespace OpenGM;

/// <summary>
/// instance of an ObjectDefinition
///
/// CInstance in cpp
///
/// TODO: make this derive from YYObjectBase, remove interface
/// </summary>
public class GamemakerObject : DrawWithDepth, IStackContextSelf
{
    public override void Draw()
    {
        
    }

    /// <summary>
    /// CHECK BUILTIN SELF VARS BEFORE THIS!!
    /// </summary>
    public Dictionary<string, object?> SelfVariables { get; } = new();

    public ObjectDefinition Definition;

    public int object_index => Definition.AssetId;
    public int[] alarm = Enumerable.Repeat(-1, 12).ToArray();

    public bool persistent = false;

    private double _x;
    public double x
    {
        get => _x;

        set
        {
            _x = value;
            bbox_dirty = true;
        }
    }
    private double _y;
    public double y
    {
        get => _y;

        set
        {
            _y = value;
            bbox_dirty = true;
        }
    }
    public double xprevious;
    public double yprevious;
    public double xstart;
    public double ystart;
    public bool visible = true;
    public Vector4i margins = Vector4i.Zero;
    public double image_speed = 1;

    private double _image_xscale = 1;
    public double image_xscale
    {
        get => _image_xscale;
        set
        {
            _image_xscale = value;
            bbox_dirty = true;
        }
    }

    private double _image_yscale = 1;
    public double image_yscale
    {
        get => _image_yscale;
        set
        {
            _image_yscale = value;
            bbox_dirty = true;
        }
    }

    private double _image_angle;
    public double image_angle
    {
        get => _image_angle;
        set
        {
            _image_angle = value;
            bbox_dirty = true;
        }
    }

    private double _image_index;
    public double image_index
    {
        get => _image_index;
        set
        {
            var prevValue = _image_index;
            _image_index = value;

            if ((int)_image_index == (int)prevValue)
            {
                return;
            }

            if (mask_index != -1)
            {
                return;
            }

            bbox_dirty = true;
/*
            if (margins != Vector4.Zero)
            {
                var spriteAsset = mask_index == -1
                    ? SpriteManager.GetSpriteAsset(sprite_index)
                    : SpriteManager.GetSpriteAsset(mask_index);

                if (spriteAsset != null && spriteAsset.CollisionMasks.Count == 1 && CollisionManager. colliders.Any(x => x.GMObject == this))
                {
                    // Don't regenerate collider when theres only one mask, dummy
                    return;
                }

                CollisionManager.RegisterCollider(this, margins);
            }*/
        }
    }

    private int _cachedSpriteWidth;
    private int _cachedSpriteHeight;
    public double sprite_width => _cachedSpriteWidth * image_xscale;
    public double sprite_height => _cachedSpriteHeight * image_yscale;

    // todo : optimize!!!

    public bool bbox_dirty;

    //public BBox bbox => CollisionManager.colliders.First(x => x.GMObject == this).BBox;

    private BBox? _bbox = null;
    public BBox bbox
    {
        get
        {
            if (_bbox == null || bbox_dirty)
            { 
                _bbox = CollisionManager.CalculateBoundingBox(this);
            }

            return _bbox;
        }

        set => _bbox = value;
    }
    public double bbox_left => bbox.left;
    public double bbox_right => bbox.right;
    public double bbox_top => bbox.top;
    public double bbox_bottom => bbox.bottom;

    private double _cached_sprite_xoffset;
    public double sprite_xoffset
        => sprite_index == -1
            ? x
            : _cached_sprite_xoffset;

    private double _cached_sprite_yoffset;
    public double sprite_yoffset
        => sprite_index == -1
            ? y
            : _cached_sprite_yoffset;

    private int _sprite_index = -1;
    public int sprite_index
    {
        get => _sprite_index;
        set
        {
            if (_sprite_index == value)
            {
                return;
            }

            if (value < 0)
            {
                DebugLog.LogWarning($"Tried to set sprite_index of {Definition.Name} to {value}!");
                return;
            }

            _sprite_index = value;

            var sprite = SpriteManager.GetSpriteAsset(_sprite_index);

            if (sprite == null)
            {
                DebugLog.LogWarning($"Couldn't find sprite for index {_sprite_index}!");
                return;
            }

            _cachedSpriteWidth = sprite!.Textures[0].TargetWidth;
            _cachedSpriteHeight = sprite.Textures[0].TargetHeight;
            _cached_sprite_xoffset = sprite.Origin.X;
            _cached_sprite_yoffset = sprite.Origin.Y;

            if (mask_index != -1)
            {
                return;
            }

            if (sprite == null)
            {
                return;
            }

            margins = sprite.Margins;

            /*if (margins != Vector4.Zero)
            {
                CollisionManager.RegisterCollider(this, margins);
            }*/

            bbox_dirty = true;
        }
    }

    private int _mask_id = -1;
    public int mask_index
    {
        get => _mask_id;
        set
        {
            _mask_id = value;

            if (value == -1)
            {
                sprite_index = sprite_index; // force the setter to run again
                return;
            }

            var maskSprite = SpriteManager.GetSpriteAsset(_mask_id);

            if (maskSprite == null)
            {
                return;
            }

            margins = maskSprite.Margins;
            _cached_sprite_xoffset = maskSprite.Origin.X;
            _cached_sprite_yoffset = maskSprite.Origin.Y;

            /*if (margins != Vector4.Zero)
            {
                CollisionManager.RegisterCollider(this, margins);
            }*/

            bbox_dirty = true;
        }
    }

    public double gravity = 0;
    public double gravity_direction = 270;
    public double friction = 0;

    private double _speed;
    public double speed
    {
        get => _speed;
        set
        {
            if (_speed == value)
            {
                return;
            }

            _speed = value;
            ComputeHVSpeed();
        }
    }

    private double _hspeed;
    public double hspeed
    {
        get => _hspeed;
        set
        {
            if (_hspeed == value)
            {
                return;
            }

            _hspeed = value;
            ComputeSpeed();
        }
    }

    private double _vspeed;
    public double vspeed
    {
        get => _vspeed;
        set
        {
            if (_vspeed == value)
            {
                return;
            }

            _vspeed = value;
            ComputeSpeed();
        }
    }

    private double _direction;
    public double direction
    {
        get
        {
            return _direction;
        }

        set
        {
            var val = value;
            while (val < 0)
            {
                val += 360;
            }

            while (val > 360)
            {
                val -= 360;
            }

            _direction = CustomMath.FMod((float)val, 360);
            ComputeHVSpeed();
        }
    }

    // https://github.com/YoYoGames/GameMaker-HTML5/blob/9122bcd3a811bd4878a1f0bbce9e6b04b31bee31/scripts/yyInstance.js#L1131
    public void ComputeSpeed()
    {
        if (hspeed == 0)
        {
            if (vspeed > 0)
            {
                _direction = 270;
            }
            else if (vspeed < 0)
            {
                _direction = 90;
            }
        }
        else
        {
            var dd = CustomMath.ClampFloat((float)(180 * (Math.Atan2(vspeed, hspeed)) / Math.PI));
            _direction = dd <= 0
                ? (double)-dd
                : 360.0 - dd;
        }

        if (Math.Abs(_direction - CustomMath.Round(_direction)) < 0.0001)
        {
            _direction = CustomMath.Round(_direction);
        }
        _direction = CustomMath.FMod((float)_direction, 360f);

        _speed = Math.Sqrt((hspeed * hspeed) + (vspeed * vspeed));
        if (Math.Abs(_speed - CustomMath.Round(_speed)) < 0.0001)
        {
            _speed = CustomMath.Round(_speed);
        }
    }

    // https://github.com/YoYoGames/GameMaker-HTML5/blob/9122bcd3a811bd4878a1f0bbce9e6b04b31bee31/scripts/yyInstance.js#L1175
    public void ComputeHVSpeed()
    {
        _hspeed = speed * CustomMath.ClampFloat((float)Math.Cos(direction * CustomMath.Deg2Rad));
        _vspeed = -speed * CustomMath.ClampFloat((float)Math.Sin(direction * CustomMath.Deg2Rad));

        if (Math.Abs(_hspeed - CustomMath.Round(_hspeed)) < 0.0001)
        { 
            _hspeed = CustomMath.Round(_hspeed);
        }

        if (Math.Abs(_vspeed - CustomMath.Round(_vspeed)) < 0.0001)
        { 
            _vspeed = CustomMath.Round(_vspeed); 
        }
    }

    public int image_blend = 16777215;
    public double image_alpha = 1;

    public bool _createRan;

    public bool Destroyed;

    public int path_index = -1;
    public double path_position = 0;
    public double path_previousposition;
    public PathEndAction path_endaction;
    public double path_speed;
    public double path_scale;
    public double path_xstart;
    public double path_ystart;
    public double path_orientation;

    public bool Active = true;
    public bool NextActive = true; // Whether the object should be active or not next frame
    public bool Marked = false; // Marked for deletion at the end of the frame
    public bool IsOutsideRoom = false; // Store state so event isn't called multiple times

    public int Layer = -1;

    public GamemakerObject(ObjectDefinition obj, double x, double y, int depth, int instanceId, int spriteIndex, bool visible, bool persistent, int maskIndex)
    {
        Definition = obj;
        this.x = x;
        this.y = y;
        this.depth = depth;
        this.instanceId = instanceId;
        this.sprite_index = spriteIndex;
        this.visible = visible;
        this.persistent = persistent;
        this.mask_index = maskIndex;

        xstart = x;
        ystart = y;

        Register();
    }

    public override void Destroy()
    {
        Destroyed = true;
        Unregister();
    }

    /// <summary>
    /// Adds this instance to the instance pool and registers it with DrawManager.
    /// </summary>
    public void Register()
    {
        InstanceManager.AddInstance(this);
        DrawManager.Register(this);

        /*if (margins != Vector4.Zero)
        {
            CollisionManager.RegisterCollider(this, margins);
        }*/
        bbox_dirty = true;
    }

    /// <summary>
    /// Removes this instance from the instance pool and unregisters it from DrawManager.
    /// </summary>
    public void Unregister()
    {
        InstanceManager.RemoveInstance(this);
        DrawManager.Unregister(this);
        //CollisionManager.UnregisterCollider(this);
    }

    public double frame_overflow;

    public void Animate()
    {
        image_index += GetIndexIncrement();

        var sprite = SpriteManager.GetSpriteAsset(sprite_index);
        if (sprite != null && image_index + GetIndexIncrement() >= sprite.Textures.Count)
        {
            ExecuteEvent(this, Definition, EventType.Other, (int)EventSubtypeOther.AnimationEnd);
        }
    }

    public double GetIndexIncrement()
    {
        var sprite = SpriteManager.GetSpriteAsset(sprite_index);
        var increment = 0.0d;

        if (sprite == null)
        {
            increment += image_speed;
        }
        else
        {
            if (sprite.PlaybackSpeedType == AnimSpeedType.FramesPerGameFrame)
            {
                increment += image_speed * sprite.PlaybackSpeed;
            }
            else
            {
                increment += image_speed * sprite.PlaybackSpeed / Entry.GameSpeed; // TODO : this should be fps, not game speed
            }
        }

        return increment;
    }

    public static bool ExecuteEvent(GamemakerObject obj, ObjectDefinition? definition, EventType eventType, int eventNumber = 0)
    {
        if (definition == null)
        {
            DebugLog.LogError($"Tried to execute event {eventType} {eventNumber} on null definition! obj:{obj}");
            //Debug.Break();
            return false;
        }

        // HACK: 
        //   as a temporary fix for objects which destroy themselves within
        //   their own destroy events, we mark the object first then allow 
        //   Destroy and CleanUp events to pass. 
        //
        //   `InstanceManager.MarkForDestruction()` prevents these events from
        //   being called again for destruction if the object is already marked.

        if (obj.Marked && eventType != EventType.Destroy && eventType != EventType.CleanUp)
        {
            return false;
        }

        // TODO:
        //   objects that change active status shouldn't acknowledge that change
        //   until next frame
        if (!obj.Active)
        {
            return false;
        }

        //DebugLog.LogInfo($"Trying to execute {eventType} {eventNumber} on {obj.object_index} with definition {definition.Name}");

        bool TryExecute(VMCode? code)
        {
            if (code != null)
            {
                VMExecutor.ExecuteCode(code, obj, definition, eventType, eventNumber);
                return true;
            }
            else if (definition.parent != null)
            {
                return ExecuteEvent(obj, definition.parent, eventType, eventNumber);
            }

            // event not found, and no parent to check
            return false;
        }

        bool TryExecuteDict<T>(Dictionary<T, VMCode> dict, T index) where T : notnull
        {
            if (dict.TryGetValue(index, out var script))
            {
                VMExecutor.ExecuteCode(script, obj, definition, eventType, eventNumber);
                return true;
            }
            else if (definition.parent != null)
            {
                return ExecuteEvent(obj, definition.parent, eventType, eventNumber);
            }

            // event not found, and no parent to check
            return false;
        }

        switch (eventType)
        {
            case EventType.Create:
                return TryExecute(definition.CreateCode);
            case EventType.Destroy:
                return TryExecute(definition.DestroyScript);
            case EventType.Alarm:
                return TryExecuteDict(definition.AlarmScript, eventNumber);
            case EventType.Step:
                return TryExecuteDict(definition.StepScript, (EventSubtypeStep)eventNumber);
            case EventType.Collision:
                return TryExecuteDict(definition.CollisionScript, eventNumber);
            case EventType.Keyboard:
                return TryExecuteDict(definition.KeyboardScripts, (EventSubtypeKey)eventNumber);
            // mouse
            case EventType.Other:
                return TryExecuteDict(definition.OtherScript, (EventSubtypeOther)eventNumber);
            case EventType.Draw:
                return TryExecuteDict(definition.DrawScript, (EventSubtypeDraw)eventNumber);
            case EventType.KeyPress:
                return TryExecuteDict(definition.KeyPressScripts, (EventSubtypeKey)eventNumber);
            case EventType.KeyRelease:
                return TryExecuteDict(definition.KeyReleaseScripts, (EventSubtypeKey)eventNumber);
            // trigger
            case EventType.CleanUp:
                return TryExecute(definition.CleanUpScript);
            // gesture
            case EventType.PreCreate:
                return TryExecute(definition.PreCreateScript);
            case EventType.Trigger:
            case EventType.Gesture:
            case EventType.Mouse:
            default:
                DebugLog.LogError($"Event type {eventType} not implemented.");
                return false;
        }
    }

    public void UpdateAlarms()
    {
        for (var i = 0; i < alarm.Length; i++)
        {
            if (alarm[i] != -1)
            {
                alarm[i]--;

                if (alarm[i] == 0)
                {
                    ExecuteEvent(this, Definition, EventType.Alarm, i);

                    if (alarm[i] == 0)
                    {
                        alarm[i] = -1;
                    }
                }
            }
        }
    }

    private double VectorToDir(double gmHoriz, double gmVert)
    {
        if (gmHoriz >= 0 && gmVert == 0)
        {
            return 0;
        }

        if (gmHoriz > 0 && gmVert == 0)
        {
            return 0;
        }

        if (gmHoriz == 0 && gmVert < 0)
        {
            return 90;
        }

        // +gmVert means down, -gmVert means up
        gmVert = -gmVert;

        var angle = Math.Atan(gmVert / gmHoriz) * CustomMath.Rad2Deg;

        if (gmVert > 0)
        {
            if (gmHoriz > 0)
            {
                return angle;
            }

            return angle + 180;
        }

        if (gmHoriz > 0)
        {
            return 360 + angle;
        }

        return 180 + angle;
    }

    public void AdaptSpeed()
    {
        if (friction != 0)
        {
            var newSpeed = speed > 0
                ? speed - friction
                : speed + friction;

            if (speed > 0 && newSpeed < 0)
            {
                speed = 0;
            }
            else if (speed < 0 && newSpeed > 0)
            {
                speed = 0;
            }
            else if (speed != 0)
            {
                speed = newSpeed;
            }
        }

        if (gravity != 0)
        {
            vspeed -= Math.Sin(CustomMath.Deg2Rad * gravity_direction) * gravity;
            hspeed += Math.Cos(CustomMath.Deg2Rad * gravity_direction) * gravity;
        }
    }

    public void AssignPath(int pathIndex, double pathSpeed, double pathScale, double pathOrientation, bool absolute, PathEndAction endAction)
    {
        path_index = -1;

        if (pathIndex < 0)
        {
            return;
        }

        var path = PathManager.Paths[pathIndex];

        if (path == null)
        {
            return;
        }

        if (path.length <= 0)
        {
            return;
        }

        if (pathScale < 0)
        {
            return;
        }

        path_index = pathIndex;

        path_speed = pathSpeed;
        if (path_speed >= 0)
        {
            path_position = 0;
        }
        else
        {
            path_position = 1;
        }

        path_previousposition = path_position;
        path_scale = pathScale;

        path_orientation = pathOrientation;
        path_endaction = endAction;

        if (absolute)
        {
            x = path.XPosition(path_speed);
            y = path.YPosition(path_speed);

            path_xstart = path.XPosition(0);
            path_ystart = path.YPosition(0);
        }
        else
        {
            path_xstart = x;
            path_ystart = y;
        }
    }

    public bool AdaptPath()
    {
        if (path_index < 0)
        {
            return false;
        }

        if (!PathManager.Paths.ContainsKey(path_index))
        {
            return false;
        }

        var path = PathManager.Paths[path_index];

        if (path.length <= 0)
        {
            return false;
        }

        var atPathEnd = false;
        var orient = path_orientation * Math.PI / 180.0;

        var point = PathManager.GetPosition(path, path_position);
        var sp = point.speed;

        sp /= (100 * path_scale); // scale speed
        path_position += path_speed * sp / path.length; // increase position

        var point0 = PathManager.GetPosition(path, 0);

        if (path_position >= 1 || path_position <= 0)
        {
            atPathEnd = path_speed != 0;
            switch (path_endaction)
            {
                case PathEndAction.path_action_stop:
                    // TODO : why is this check not in the default case?
                    if (path_speed != 0)
                    {
                        path_position = 1;
                        path_index = -1;
                    }

                    break;
                case PathEndAction.path_action_restart:
                    if (path_position < 0)
                    {
                        path_position++;
                    }
                    else
                    {
                        path_position--;
                    }

                    break;
                case PathEndAction.path_action_continue:
                    var point1 = PathManager.GetPosition(path, 1);
                    var dx = point1.x - point0.x;
                    var dy = point1.y - point0.y;

                    var xdif = path_scale * ((dx * Math.Cos(orient)) + (dy * Math.Sin(orient)));
                    var ydif = path_scale * ((dy * Math.Cos(orient)) - (dx * Math.Sin(orient)));

                    if (path_position < 0)
                    {
                        path_xstart -= xdif;
                        path_ystart -= ydif;
                        path_position++;
                    }
                    else
                    {
                        path_xstart += xdif;
                        path_ystart += ydif;
                        path_position--;
                    }

                    break;
                case PathEndAction.path_action_reverse:
                    if (path_position < 0)
                    {
                        path_position = -path_position;
                        path_speed = Math.Abs(path_speed);
                    }
                    else
                    {
                        path_position = 2 - path_position;
                        path_speed = -Math.Abs(path_speed);
                    }

                    break;
                default:
                    path_position = 1;
                    path_index = -1;
                    break;
            }
        }

        point = PathManager.GetPosition(path, path_position);
        var xx = point.x - point0.x;
        var yy = point.y - point0.y;

        var newx = path_xstart + path_scale * ((xx * Math.Cos(orient)) + (yy * Math.Sin(orient)));
        var newy = path_ystart + path_scale * ((yy * Math.Cos(orient)) - (xx * Math.Sin(orient)));

        // trick to set the direction
        hspeed = newx - x;
        vspeed = newy - y;
        speed = 0;

        x = newx;
        y = newy;

        return atPathEnd;
    }

    public bool HasEvent(EventType eventType, int eventSubtype)
    {
        return eventType switch
        {
            EventType.Create => Definition.CreateCode != null,
            EventType.Destroy => Definition.CreateCode != null,
            EventType.Alarm => Definition.AlarmScript.ContainsKey(eventSubtype),
            EventType.Step => Definition.StepScript.ContainsKey((EventSubtypeStep)eventSubtype),
            EventType.Collision => Definition.CollisionScript.ContainsKey(eventSubtype),
            EventType.Keyboard => Definition.KeyboardScripts.ContainsKey((EventSubtypeKey)eventSubtype),
            EventType.Other => Definition.OtherScript.ContainsKey((EventSubtypeOther)eventSubtype),
            EventType.Draw => Definition.DrawScript.ContainsKey((EventSubtypeDraw)eventSubtype),
            EventType.KeyPress => Definition.KeyPressScripts.ContainsKey((EventSubtypeKey)eventSubtype),
            EventType.KeyRelease => Definition.KeyReleaseScripts.ContainsKey((EventSubtypeKey)eventSubtype),
            EventType.CleanUp => Definition.CleanUpScript != null,
            EventType.PreCreate => Definition.CreateCode != null,
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null),
        };
    }
}

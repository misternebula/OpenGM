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
/// </summary>
public class GamemakerObject : DrawWithDepth, IStackContextSelf
{
	/// <summary>
	/// CHECK BUILTIN SELF VARS BEFORE THIS!!
	/// </summary>
	public Dictionary<string, object?> SelfVariables { get; set; } = new();

	public ObjectDefinition Definition;

	public int object_index => Definition.AssetId;
	public object?[] alarm = Enumerable.Repeat((object?)-1, 12).ToArray(); // doubles will be ArraySet here

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

			_sprite_index = value;

			if (value == -1)
			{
				return;
			}

			var sprite = SpriteManager.GetSpriteAsset(_sprite_index);
			_cachedSpriteWidth = sprite!.Textures[0].TargetSizeX;
			_cachedSpriteHeight = sprite.Textures[0].TargetSizeY;
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
			_speed = value;
			var d = direction;
			_hspeed = Math.Cos(d * CustomMath.Deg2Rad) * value;
			_vspeed = -Math.Sin(d * CustomMath.Deg2Rad) * value;
		}
	}

	private double _hspeed;
	public double hspeed
	{
		get => _hspeed;
		set
		{
			_hspeed = value;
			_direction = VectorToDir(hspeed, vspeed);
			_speed = CustomMath.Sign(_speed) * Math.Sqrt(Math.Pow(_hspeed, 2) + Math.Pow(_vspeed, 2));
		}
	}

	private double _vspeed;
	public double vspeed
	{
		get => _vspeed;
		set
		{
			_vspeed = value;
			_direction = VectorToDir(hspeed, vspeed);
			_speed = CustomMath.Sign(_speed) * Math.Sqrt(Math.Pow(_hspeed, 2) + Math.Pow(_vspeed, 2));
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
			_direction = value;
			_hspeed = Math.Cos(value * CustomMath.Deg2Rad) * speed;
			_vspeed = -Math.Sin(value * CustomMath.Deg2Rad) * speed;
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
	public bool Marked = false; // Marked for deletion at the end of the frame

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

		InstanceManager.RegisterInstance(this);
		DrawManager.Register(this);

		/*if (margins != Vector4.Zero)
		{
			CollisionManager.RegisterCollider(this, margins);
		}*/
		bbox_dirty = true;
	}

	public override void Destroy()
	{
		Destroyed = true;
		DrawManager.Unregister(this);
		//CollisionManager.UnregisterCollider(this);
	}

	public double frame_overflow;

	public sealed override void Draw()
	{
		if (!_createRan || !RoomManager.RoomLoaded)
		{
			return;
		}

		AdaptSpeed();

		if (AdaptPath())
		{
			ExecuteEvent(this, Definition, EventType.Other, (int)EventSubtypeOther.EndOfPath);
		}

		if (hspeed != 0 || vspeed != 0)
		{
			x += hspeed;
			y += vspeed;
		}

		var asset = SpriteManager.GetSpriteAsset(sprite_index);
		if (asset != null)
		{
			var playbackType = asset.PlaybackSpeedType;
			var playbackSpeed = asset.PlaybackSpeed * image_speed;

			if (playbackType == AnimSpeedType.FramesPerGameFrame)
			{
				image_index += playbackSpeed;
			}
			else
			{
				image_index += playbackSpeed / Entry.GameSpeed; // TODO : this should be fps, not game speed
			}

			var number = SpriteManager.GetNumberOfFrames(sprite_index);

			if (image_index >= number)
			{
				frame_overflow += number;
				image_index -= number;

				ExecuteEvent(this, Definition, EventType.Other, (int)EventSubtypeOther.AnimationEnd);
			}
			else if (image_index < 0)
			{
				frame_overflow -= number;
				image_index += number;

				ExecuteEvent(this, Definition, EventType.Other, (int)EventSubtypeOther.AnimationEnd);
			}
		}
		else
		{
			// lol this is dumb but i guess it's what GM does???
			image_index += image_speed;
		}
	}

	public static bool ExecuteEvent(GamemakerObject obj, ObjectDefinition? definition, EventType eventType, int eventNumber = 0)
	{
		if (definition == null)
		{
			DebugLog.LogError($"Tried to execute event {eventType} {eventNumber} on null definition! obj:{obj}");
			//Debug.Break();
			return false;
		}

		if (obj.Marked)
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
				DebugLog.LogError($"{eventType} not implemented!");
				return false;
		}
	}

	public void UpdateAlarms()
	{
		for (var i = 0; i < alarm.Length; i++)
		{
			if (alarm[i].Conv<int>() != -1)
			{
				alarm[i] = alarm[i].Conv<int>() - 1;

				if (alarm[i].Conv<int>() == 0)
				{
					ExecuteEvent(this, Definition, EventType.Alarm, i);

					if (alarm[i].Conv<int>() == 0)
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

		var path = PathManager.Paths[path_index];
		if (path == null)
		{
			return false;
		}

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
}

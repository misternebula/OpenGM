using DELTARUNITYStandalone.SerializedFiles;
using DELTARUNITYStandalone.VirtualMachine;
using OpenTK.Mathematics;
using UndertaleModLib.Models;
using EventType = DELTARUNITYStandalone.VirtualMachine.EventType;

namespace DELTARUNITYStandalone;

/// <summary>
/// instance of an ObjectDefinition
/// </summary>
public class GamemakerObject : DrawWithDepth
{
	public Dictionary<string, object> SelfVariables = new();

	public ObjectDefinition Definition;

	public int object_index => Definition.AssetId;
	public int[] alarm = Enumerable.Repeat(-1, 12).ToArray();

	public bool persistent = false;
	public double x;
	public double y;
	public double xstart;
	public double ystart;
	public bool visible = true;
	public Vector4i margins = Vector4i.Zero;
	public double image_speed = 1;

	private double _image_xscale;
	public double image_xscale
	{
		get => _image_xscale;
		set
		{
			_image_xscale = value;
			//CollisionManager.UpdateRotationMask(this);
		}
	}

	private double _image_yscale;
	public double image_yscale
	{
		get => _image_yscale;
		set
		{
			_image_yscale = value;
			//CollisionManager.UpdateRotationMask(this);
		}
	}

	private double _image_angle;
	public double image_angle
	{
		get => _image_angle;
		set
		{
			_image_angle = value;
			//CollisionManager.UpdateRotationMask(this);
		}
	}

	private double _image_index;
	public double image_index
	{
		get => _image_index;
		set
		{
			if ((int)_image_index == (int)value)
			{
				return;
			}

			_image_index = value;

			if (mask_id != -1)
			{
				return;
			}

			if (margins != Vector4.Zero)
			{
				//CollisionManager.RegisterCollider(this, margins);
			}
		}
	}

	private int _cachedSpriteWidth;
	private int _cachedSpriteHeight;
	public double sprite_width => _cachedSpriteWidth * image_xscale;
	public double sprite_height => _cachedSpriteHeight * image_yscale;

	public double bbox_left => x + (margins.X * image_xscale) - (sprite_xoffset * image_xscale);
	public double bbox_right => x + (margins.Y * image_xscale) - (sprite_xoffset * image_xscale);
	public double bbox_top => -(-y - (margins.W * image_yscale)) - (sprite_yoffset * image_yscale);
	public double bbox_bottom => -(-y - (margins.Z * image_yscale)) - (sprite_yoffset * image_yscale);

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
			_cachedSpriteWidth = sprite.Textures[0].TargetSizeX;
			_cachedSpriteHeight = sprite.Textures[0].TargetSizeY;
			_cached_sprite_xoffset = sprite.Origin.X;
			_cached_sprite_yoffset = sprite.Origin.Y;

			if (mask_id != -1)
			{
				return;
			}

			if (sprite == null)
			{
				return;
			}

			margins = sprite.Margins;

			if (margins != Vector4.Zero)
			{
				//CollisionManager.RegisterCollider(this, margins);
			}
		}
	}

	private int _mask_id = -1;
	public int mask_id
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

			if (margins != Vector4.Zero)
			{
				//CollisionManager.RegisterCollider(this, margins);
			}
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
			_speed = Math.Sign((float)_speed) * Math.Sqrt(Math.Pow(_hspeed, 2) + Math.Pow(_vspeed, 2));
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
			_speed = Math.Sign((float)_speed) * Math.Sqrt(Math.Pow(_hspeed, 2) + Math.Pow(_vspeed, 2));
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

	public GamemakerObject(ObjectDefinition obj, double x, double y, int depth, int instanceId, int spriteIndex, bool visible, bool persistent, int maskId)
	{
		Definition = obj;
		this.x = x;
		this.y = y;
		this.depth = depth;
		this.instanceId = instanceId;
		this.sprite_index = spriteIndex;
		this.visible = visible;
		this.persistent = persistent;
		// mask id

		xstart = x;
		ystart = y;

		InstanceManager.RegisterInstance(this);
		DrawManager.Register(this);

		if (margins != Vector4.Zero)
		{
			//CollisionManager.RegisterCollider(this, margins);
		}
	}

	private int _updateCounter;

	public sealed override void Draw()
	{
		if (!_createRan || !RoomManager.RoomLoaded)
		{
			return;
		}

		if (friction != 0)
		{
			if (speed > 0)
			{
				if (speed - friction < 0)
				{
					speed = 0;
				}
				else
				{
					speed -= friction;
				}
			}
			else if (speed < 0)
			{
				if (speed + friction > 0)
				{
					speed = 0;
				}
				else
				{
					speed += friction;
				}
			}
		}

		if (gravity != 0)
		{
			//vspeed += gravity;
			vspeed += -Math.Sin(CustomMath.Deg2Rad * gravity_direction) * gravity;
			hspeed += Math.Cos(CustomMath.Deg2Rad * gravity_direction) * gravity;
		}

		x += hspeed;
		y += vspeed;

		var asset = SpriteManager.GetSpriteAsset(sprite_index);
		if (asset != null)
		{
			var playbackType = asset.PlaybackSpeedType;
			var playbackSpeed = asset.PlaybackSpeed * image_speed;

			_updateCounter++;

			var deltaTime = 1 / 30f;
			var shouldIncrement = playbackType == AnimSpeedType.FramesPerSecond
				? _updateCounter >= 1f / (deltaTime / (1f / playbackSpeed))
				: _updateCounter >= 1f / playbackSpeed;

			if (shouldIncrement)
			{
				_updateCounter = 0;
				if (image_index + 1 == SpriteManager.GetNumberOfFrames(sprite_index))
				{
					ExecuteScript(this, Definition, EventType.Other, (uint)EventSubtypeOther.AnimationEnd);
					image_index = 0;
				}
				else
				{
					image_index++;
				}
			}
		}
	}

	public static bool ExecuteScript(GamemakerObject obj, ObjectDefinition definition, EventType type, uint otherData = 0)
	{
		if (definition == null)
		{
			DebugLog.LogError($"Tried to execute event {type} {otherData} on null definition! obj:{obj}");
			//Debug.Break();
			return false;
		}

		//Debug.Log($"Trying to execute {type} {otherData} on {obj.object_index} with definition {definition.name}");

		bool TryExecute(VMScript script)
		{
			if (script != null)
			{
				VMExecutor.ExecuteScript(script, obj, definition, type, otherData);
				return true;
			}
			else if (definition.parent != null)
			{
				return ExecuteScript(obj, definition.parent, type, otherData);
			}

			// event not found, and no parent to check
			return false;
		}

		bool TryExecuteDict<T>(Dictionary<T, VMScript> dict, T index)
		{
			if (dict.TryGetValue(index, out var script))
			{
				VMExecutor.ExecuteScript(script, obj, definition, type, otherData);
				return true;
			}
			else if (definition.parent != null)
			{
				return ExecuteScript(obj, definition.parent, type, otherData);
			}

			// event not found, and no parent to check
			return false;
		}

		switch (type)
		{
			case EventType.Create:
				return TryExecute(definition.CreateScript);
			case EventType.Destroy:
				return TryExecute(definition.DestroyScript);
			case EventType.Alarm:
				return TryExecuteDict(definition.AlarmScript, otherData);
			case EventType.Step:
				return TryExecuteDict(definition.StepScript, (EventSubtypeStep)otherData);
			case EventType.Collision:
				return TryExecuteDict(definition.CollisionScript, otherData);
			// keyboard
			// mouse
			case EventType.Other:
				return TryExecuteDict(definition.OtherScript, (EventSubtypeOther)otherData);
			case EventType.Draw:
				return TryExecuteDict(definition.DrawScript, (EventSubtypeDraw)otherData);
			// keypress
			// keyrelease
			// trigger
			case EventType.CleanUp:
				return TryExecute(definition.CleanUpScript);
			// gesture
			case EventType.PreCreate:
				return TryExecute(definition.PreCreateScript);
			case EventType.KeyPress:
			case EventType.KeyRelease:
			case EventType.Trigger:
			case EventType.Gesture:
			case EventType.Keyboard:
			case EventType.Mouse:
			default:
				DebugLog.LogError($"{type} not implemented!");
				return false;
		}
	}

	public void UpdateAlarms()
	{
		for (var i = 0u; i < alarm.Length; i++)
		{
			if (alarm[i] != -1)
			{
				alarm[i] -= 1;

				if (alarm[i] == 0)
				{
					ExecuteScript(this, Definition, EventType.Alarm, i);

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
}

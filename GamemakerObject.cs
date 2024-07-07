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
	public Dictionary<string, RValue> SelfVariables = new();

	public ObjectDefinition Definition;

	public int object_index => Definition.AssetId;
	public List<RValue> alarm = new(Enumerable.Repeat(new RValue(-1), 12).ToArray());

	public bool persistent = false;
	public double x;
	public double y;
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
			CollisionManager.UpdateRotationMask(this);
		}
	}

	private double _image_yscale = 1;
	public double image_yscale
	{
		get => _image_yscale;
		set
		{
			_image_yscale = value;
			CollisionManager.UpdateRotationMask(this);
		}
	}

	private double _image_angle;
	public double image_angle
	{
		get => _image_angle;
		set
		{
			_image_angle = value;
			CollisionManager.UpdateRotationMask(this);
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

			if (mask_id != -1)
			{
				return;
			}

			if (margins != Vector4.Zero)
			{
				CollisionManager.RegisterCollider(this, margins);
			}
		}
	}

	private int _cachedSpriteWidth;
	private int _cachedSpriteHeight;
	public double sprite_width => _cachedSpriteWidth * image_xscale;
	public double sprite_height => _cachedSpriteHeight * image_yscale;

	// todo : optimize!!!
	public double bbox_left => CollisionManager.colliders.First(x => x.GMObject == this).BBox.left;
	public double bbox_right => CollisionManager.colliders.First(x => x.GMObject == this).BBox.right;
	public double bbox_top => CollisionManager.colliders.First(x => x.GMObject == this).BBox.top;
	public double bbox_bottom => CollisionManager.colliders.First(x => x.GMObject == this).BBox.bottom;

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
				CollisionManager.RegisterCollider(this, margins);
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
				CollisionManager.RegisterCollider(this, margins);
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

	public GamemakerObject(ObjectDefinition obj, double x, double y, int depth, uint instanceId, int spriteIndex, bool visible, bool persistent, int maskId)
	{
		Definition = obj;
		this.x = x;
		this.y = y;
		this.depth = depth;
		this.instanceId = instanceId;
		this.sprite_index = spriteIndex;
		this.visible = visible;
		this.persistent = persistent;
		this.mask_id = maskId;

		xstart = x;
		ystart = y;

		InstanceManager.RegisterInstance(this);
		DrawManager.Register(this);

		if (margins != Vector4.Zero)
		{
			CollisionManager.RegisterCollider(this, margins);
		}
	}

	public override void Destroy()
	{
		Destroyed = true;
		DrawManager.Unregister(this);
		CollisionManager.UnregisterCollider(this);
	}

	private int _updateCounter;

	public sealed override void Draw()
	{
		if (!_createRan || !RoomManager.RoomLoaded)
		{
			return;
		}

		/*var collider = CollisionManager.colliders.FirstOrDefault(x => x.GMObject == this);
		if (collider != null && (object_index == 331 || object_index == 81 || object_index == 336))
		{
			CustomWindow.DebugRenderJobs.Add(new GMPolygonJob()
			{
				blend = Color4.Yellow,
				alpha = 1,
				Outline = true,
				Vertices = new[]
				{
					new Vector2((float)x + (float)(margins.X * image_xscale), (float)y + (float)(margins.W * image_yscale)),
					new Vector2((float)x + (float)((margins.Y + 1) * image_xscale), (float)y + (float)(margins.W * image_yscale)),
					new Vector2((float)x + (float)((margins.Y + 1) * image_xscale), (float)y + (float)((margins.Z + 1) * image_yscale)),
					new Vector2((float)x + (float)(margins.X * image_xscale), (float)y + (float)((margins.Z + 1) * image_yscale))
				}
			});
		}*/

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
				if (CustomMath.ApproxEqual(image_index + 1, SpriteManager.GetNumberOfFrames(sprite_index)))
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
		for (var i = 0; i < alarm.Count; i++)
		{
			if ((int)alarm[i].Value != -1)
			{
				alarm[i] = new RValue((int)alarm[i].Value - 1);

				if ((int)alarm[i].Value == 0)
				{
					ExecuteScript(this, Definition, EventType.Alarm, (uint)i);

					if ((int)alarm[i].Value == 0)
					{
						alarm[i] = new RValue(-1);
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

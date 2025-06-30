using OpenGM.IO;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class LayerFunctions
    {
		// CLayerManager::InitLayerFunctions

		[GMLFunction("layer_get_id")]
		public static object layer_get_id(object?[] args)
		{
			var layer_name = args[0].Conv<string>();

			var layer = RoomManager.CurrentRoom.Layers.Values.FirstOrDefault(x => x.Name == layer_name);
			return layer == default ? -1 : layer.ID;
		}

		[GMLFunction("layer_get_id_at_depth")]
		public static object? layer_get_id_at_depth(object?[] args)
		{
			var depth = args[0].Conv<int>();

			var retList = new List<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers.Values)
			{
				if (layer.Depth == depth)
				{
					retList.Add(layer.ID);
				}
			}

			if (retList.Count == 0)
			{
				retList.Add(-1);
			}

			return retList;
		}

		[GMLFunction("layer_get_depth")]
		public static object layer_get_depth(object?[] args)
		{
			var layer_id = args[0];
			var layer = RoomManager.CurrentRoom.GetLayer(layer_id);

			if (layer == null)
			{
				throw new Exception();
			}

			return layer.Depth;
		}

		[GMLFunction("layer_create")]
		public static object? layer_create(object?[] args)
		{
			var depth = args[0].Conv<int>();
			var name = "";
			if (args.Length > 1)
			{
				name = args[1].Conv<string>();
			}

			var newLayerId = RoomManager.CurrentRoom.Layers.Keys.Max() + 1;

			if (string.IsNullOrEmpty(name))
			{
				name = $"_layer_{Guid.NewGuid()}";
			}

			var layerContainer = new LayerContainer(new SerializedFiles.Layer() { LayerName = name, LayerDepth = depth, LayerID = newLayerId });

			RoomManager.CurrentRoom.Layers.Add(newLayerId, layerContainer);

			return newLayerId;
		}

		// layer_destroy
		// layer_destroy_instances
		// layer_add_instance
		// layer_has_instance

		[GMLFunction("layer_set_visible")]
		public static object? layer_set_visible(object?[] args)
		{
			var layer_value = args[0];
			LayerContainer layer;
			if (layer_value is string s)
			{
				layer = RoomManager.CurrentRoom.Layers.FirstOrDefault(x => x.Value.Name == s).Value;
				if (layer == null)
				{
					DebugLog.Log($"layer_set_visible() - could not find specified layer in current room");
					return null;
				}
			}
			else
			{
				layer = RoomManager.CurrentRoom.Layers[args[0].Conv<int>()];
			}

			var visible = args[1].Conv<bool>();

			layer.Visible = visible;
			return null;
		}

		[GMLFunction("layer_get_visible")]
		public static object? layer_get_visible(object?[] args)
		{
			var layer_value = args[0];

			if (layer_value is string s)
			{
				return RoomManager.CurrentRoom.Layers.FirstOrDefault(x => x.Value.Name == s).Value.Visible;
			}
			else
			{
				return RoomManager.CurrentRoom.Layers[args[0].Conv<int>()].Visible;
			}
		}

		[GMLFunction("layer_exists")]
		public static object? layer_exists(object?[] args)
		{
			var layer = args[0];

			if (layer is string)
			{
				var layer_name = layer.Conv<string>();
				var actual_layer = RoomManager.CurrentRoom.Layers.Values.FirstOrDefault(x => x.Name == layer_name);
				return actual_layer != null;
			}
			else
			{
				var layer_id = layer.Conv<int>();
				return RoomManager.CurrentRoom.Layers.ContainsKey(layer_id);
			}
		}

		[GMLFunction("layer_x")]
		public static object? layer_x(object?[] args)
		{
			var layer_id = args[0];
			var x = args[1].Conv<double>();

			var layer = RoomManager.CurrentRoom.GetLayer(layer_id);

			if (layer == null)
			{
				return null;
			}

			layer.X = (float)x;
			return null;
		}

		[GMLFunction("layer_y")]
		public static object? layer_y(object?[] args)
		{
			var layer_id = args[0];
			var y = args[1].Conv<double>();

			var layer = RoomManager.CurrentRoom.GetLayer(layer_id);

			if (layer == null)
			{
				return null;
			}

			layer.Y = (float)y;
			return null;
		}

		[GMLFunction("layer_get_x")]
		public static object layer_get_x(object?[] args)
		{
			var layer_id = args[0];

			var layer = RoomManager.CurrentRoom.GetLayer(layer_id);

			if (layer == null)
			{
				return 0;
			}

			return layer.X;
		}

		[GMLFunction("layer_get_y")]
		public static object layer_get_y(object?[] args)
		{
			var layer_id = args[0];
			var layer = RoomManager.CurrentRoom.GetLayer(layer_id);

			if (layer == null)
			{
				return 0;
			}

			return layer.Y;
		}

		[GMLFunction("layer_hspeed")]
		public static object? layer_hspeed(object?[] args)
		{
			var layer_id = args[0];
			var hspd = args[1].Conv<double>();

			var layer = RoomManager.CurrentRoom.GetLayer(layer_id);

			if (layer == null)
			{
				return null;
			}

			layer.HSpeed = (float)hspd;
			return null;
		}

		[GMLFunction("layer_vspeed")]
		public static object? layer_vspeed(object?[] args)
		{
			var layer_id = args[0];
			var vspd = args[1].Conv<double>();

			var layer = RoomManager.CurrentRoom.GetLayer(layer_id);

			if (layer == null)
			{
				return null;
			}

			layer.VSpeed = (float)vspd;
			return null;
		}

		[GMLFunction("layer_get_hspeed")]
		public static object layer_get_hspeed(object?[] args)
		{
			var layer_id = args[0];

			var layer = RoomManager.CurrentRoom.GetLayer(layer_id);

			if (layer == null)
			{
				return 0;
			}

			return layer.HSpeed;
		}

		[GMLFunction("layer_get_vspeed")]
		public static object layer_get_vspeed(object?[] args)
		{
			var layer_id = args[0];

			var layer = RoomManager.CurrentRoom.GetLayer(layer_id);

			if (layer == null)
			{
				return 0;
			}

			return layer.VSpeed;
		}

		// layer_script_begin
		// layer_script_end
		// layer_shader
		// layer_get_script_begin
		// layer_get_script_end
		// layer_get_shader
		// layer_set_target_room
		// layer_get_target_room
		// layer_reset_target_room

		[GMLFunction("layer_get_all")]
		public static object layer_get_all(object?[] args)
		{
			return RoomManager.CurrentRoom.Layers.Values.Select(x => x.ID).ToList();
		}

		[GMLFunction("layer_get_all_elements")]
		public static object layer_get_all_elements(object?[] args)
		{
			var layer = RoomManager.CurrentRoom.GetLayer(args[0]);

			if (layer == null)
			{
				throw new Exception();
			}

			return layer.ElementsToDraw.Select(x => x.instanceId).ToList();
		}

		[GMLFunction("layer_get_name")]
		public static object layer_get_name(object?[] args)
		{
			var layer_id = args[0].Conv<int>();
			return RoomManager.CurrentRoom.Layers[layer_id].Name;
		}

		[GMLFunction("layer_depth")]
		public static object? layer_depth(object?[] args)
		{
			var layer_id = args[0];
			var depth = args[1].Conv<int>();

			var layer = RoomManager.CurrentRoom.GetLayer(layer_id);

			if (layer == null)
			{
				return null;
			}

			layer.Depth = depth;
			return null;
		}

		// layer_get_element_layer

		[GMLFunction("layer_get_element_type")]
		public static object layer_get_element_type(object?[] args)
		{
			var element_id = args[0].Conv<int>();

			CLayerElementBase baseElement = null!;
			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.LayerAsset.Elements)
				{
					if (element.Id == element_id)
					{
						baseElement = element;
						break;
					}
				}

				if (baseElement != null)
				{
					break;
				}
			}

			if (baseElement == null)
			{
				return (int)ElementType.Undefined;
			}

			return (int)baseElement.Type;
		}

		// layer_element_move

		[GMLFunction("layer_force_draw_depth")]
		public static object? layer_force_draw_depth(object?[] args)
		{
			var force = args[0].Conv<bool>();
			var depth = args[1].Conv<int>();
			//Debug.Log($"layer_force_draw_depth force:{force} depth:{depth}");

			// not implementing yet because uhhhhhhhhhhhhhhhhhhh

			DebugLog.LogWarning("layer_force_draw_depth not implemented.");

			return null;
		}

		// layer_is_draw_depth_forced
		// layer_get_forced_depth

		[GMLFunction("layer_background_get_id")]
		public static object? layer_background_get_id(object?[] args)
		{
			var layer_id = args[0].Conv<int>();

			if (!RoomManager.CurrentRoom.Layers.TryGetValue(layer_id, out var layer))
			{
				return -1;
			}

			foreach (var element in layer.ElementsToDraw)
			{
				if (element is GMBackground back)
				{
					return back.Element.Id;
				}
			}

			return -1;
		}

		[GMLFunction("layer_background_exists")]
		public static object? layer_background_exists(object?[] args)
		{
			var layer_id = args[0].Conv<int>();
			var background_element_id = args[1].Conv<int>();

			if (!RoomManager.CurrentRoom.Layers.TryGetValue(layer_id, out var layer))
			{
				return false;
			}

			foreach (var element in layer.ElementsToDraw)
			{
				if (element is GMBackground back && back.Element.Id == background_element_id)
				{
					return true;
				}
			}

			return false;
		}

		[GMLFunction("layer_background_create")]
		public static object layer_background_create(object?[] args)
		{
			var layer_id = args[0];
			var sprite = args[1].Conv<int>();

			LayerContainer layer;
			if (layer_id is string s)
			{
				layer = RoomManager.CurrentRoom.Layers.FirstOrDefault(x => x.Value.Name == s).Value;
			}
			else
			{
				var id = layer_id.Conv<int>();
				layer = RoomManager.CurrentRoom.Layers[id];
			}

			var item = new CLayerBackgroundElement();
			item.Index = sprite;
			item.Visible = true;
			item.Color = 0xFFFFFFFF;
			item.Layer = layer;

			var background = new GMBackground(item)
			{
				depth = layer.Depth
			};

			layer.ElementsToDraw.Add(background);

			return item.Id;
		}

		// layer_background_destroy

		[GMLFunction("layer_background_visible")]
		public static object? layer_background_visible(object?[] args)
		{
			var background_element_id = args[0].Conv<int>();
			var visible = args[1].Conv<bool>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMBackground tilemap && tilemap.Element.Id == background_element_id)
					{
						tilemap.Element.Visible = visible;
					}
				}
			}

			return null;
		}

		[GMLFunction("layer_background_htiled")]
		public static object? layer_background_htiled(object?[] args)
		{
			var background_element_id = args[0].Conv<int>();
			var htiled = args[1].Conv<bool>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMBackground tilemap && tilemap.Element.Id == background_element_id)
					{
						tilemap.Element.HTiled = htiled;
					}
				}
			}

			return null;
		}

		[GMLFunction("layer_background_vtiled")]
		public static object? layer_background_vtiled(object?[] args)
		{
			var background_element_id = args[0].Conv<int>();
			var vtiled = args[1].Conv<bool>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMBackground tilemap && tilemap.Element.Id == background_element_id)
					{
						tilemap.Element.VTiled = vtiled;
					}
				}
			}

			return null;
		}

		[GMLFunction("layer_background_xscale")]
		public static object? layer_background_xscale(object?[] args)
		{
			var background_element_id = args[0].Conv<int>();
			var xscale = args[1].Conv<double>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMBackground tilemap && tilemap.Element.Id == background_element_id)
					{
						tilemap.Element.XScale = xscale;
					}
				}
			}

			return null;
		}

		[GMLFunction("layer_background_yscale")]
		public static object? layer_background_yscale(object?[] args)
		{
			var background_element_id = args[0].Conv<int>();
			var yscale = args[1].Conv<double>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMBackground tilemap && tilemap.Element.Id == background_element_id)
					{
						tilemap.Element.YScale = yscale;
					}
				}
			}

			return null;
		}

		[GMLFunction("layer_background_stretch")]
		public static object? layer_background_stretch(object?[] args)
		{
			var background_element_id = args[0].Conv<int>();
			var stretch = args[1].Conv<bool>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMBackground tilemap && tilemap.Element.Id == background_element_id)
					{
						tilemap.Element.Stretch = stretch;
					}
				}
			}

			return null;
		}

		[GMLFunction("layer_background_blend")]
		public static object? layer_background_blend(object?[] args)
		{
			var background_element_id = args[0].Conv<int>();
			var blend = args[1].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMBackground back && back.Element.Id == background_element_id)
					{
						// this doesnt set alpha on purpose
						back.Element.Color = (uint)blend;
					}
				}
			}

			return null;
		}

		[GMLFunction("layer_background_alpha")]
		public static object? layer_background_alpha(object?[] args)
		{
			var background_element_id = args[0].Conv<int>();
			var alpha = args[0].Conv<double>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMBackground back && back.Element.Id == background_element_id)
					{
						back.Element.Alpha = alpha;
					}
				}
			}

			return null;
		}


		[GMLFunction("layer_background_index")]
		public static object? layer_background_index(object?[] args)
		{
			var background_element_id = args[0].Conv<int>();
			var index = args[0].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMBackground back && back.Element.Id == background_element_id)
					{
						back.Element.Index = index;
					}
				}
			}

			return null;
		}

		// layer_background_speed
		// layer_background_sprite
		// layer_background_change
		// layer_background_get_visible
		// layer_background_get_sprite
		// layer_background_get_htiled
		// layer_background_get_vtiled
		// layer_background_get_xscale
		// layer_background_get_yscale
		// layer_background_get_stretch

		[GMLFunction("layer_background_get_blend")]
		public static object? layer_background_get_blend(object?[] args)
		{
			var background_element_id = args[0].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMBackground back && back.Element.Id == background_element_id)
					{
						return back.Element.Color;
					}
				}
			}

			return 0;
		}

		[GMLFunction("layer_background_get_alpha")]
		public static object? layer_background_get_alpha(object?[] args)
		{
			var background_element_id = args[0].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMBackground back && back.Element.Id == background_element_id)
					{
						return back.Element.Alpha;
					}
				}
			}

			return 0;
		}

		// layer_background_get_index
		// layer_background_get_speed

		// layer_sprite_get_id
		// layer_sprite_exists
		// layer_sprite_create

		[GMLFunction("layer_sprite_destroy")]
		public static object? layer_sprite_destroy(object?[] args)
		{
			var element_id = args[0].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				DrawWithDepth? elementToDestroy = null;

				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMSprite sprite && sprite.Element.Id == element_id)
					{
						elementToDestroy = element;
					}
				}

				if (elementToDestroy == null)
				{
					continue;
				}

				layer.Value.ElementsToDraw.Remove(elementToDestroy);
				elementToDestroy.Destroy();
			}

			return null;
		}

		[GMLFunction("layer_sprite_change")]
		public static object? layer_sprite_change(object?[] args)
		{
			var element_id = args[0].Conv<int>();
			var sprite_id = args[0].Conv<int>();

			DebugLog.LogWarning("layer_sprite_change not implemented.");
			return null;
		}

		// layer_sprite_index

		[GMLFunction("layer_sprite_speed")]
		public static object? layer_sprite_speed(object?[] args)
		{
			var element_id = args[0].Conv<int>();
			var speed = args[0].Conv<double>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMSprite sprite && sprite.Element.Id == element_id)
					{
						sprite.AnimationSpeed = speed;
					}
				}
			}

			return null;
		}

		// layer_sprite_xscale
		// layer_sprite_yscale
		// layer_sprite_angle
		// layer_sprite_blend
		// layer_sprite_alpha
		// layer_sprite_x
		// layer_sprite_y

		[GMLFunction("layer_sprite_get_sprite")]
		public static object? layer_sprite_get_sprite(object?[] args)
		{
			var element_id = args[0].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMSprite sprite && sprite.Element.Id == element_id)
					{
						return sprite.Definition;
					}
				}
			}

			return -1;
		}

		[GMLFunction("layer_sprite_get_index")]
		public static object? layer_sprite_get_index(object?[] args)
		{
			var element_id = args[0].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMSprite sprite && sprite.Element.Id == element_id)
					{
						return sprite.FrameIndex;
					}
				}
			}

			return -1;
		}

		[GMLFunction("layer_sprite_get_id")]
		public static object? layer_sprite_get_id(object?[] args)
		{
			var layer_id = args[0].Conv<int>();

			if (!RoomManager.CurrentRoom.Layers.TryGetValue(layer_id, out var layer))
			{
				return -1;
			}

			foreach (var element in layer.ElementsToDraw)
			{
				if (element is GMSprite sprite)
				{
					return sprite.Element.Id;
				}
			}

			return -1;
		}

		[GMLFunction("layer_sprite_get_speed")]
		public static object? layer_sprite_get_speed(object?[] args)
		{
			var element_id = args[0].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMSprite sprite && sprite.Element.Id == element_id)
					{
						return sprite.AnimationSpeed;
					}
				}
			}

			return -1;
		}

		[GMLFunction("layer_sprite_get_xscale")]
		public static object? layer_sprite_get_xscale(object?[] args)
		{
			var element_id = args[0].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMSprite sprite && sprite.Element.Id == element_id)
					{
						return sprite.XScale;
					}
				}
			}

			return 0;
		}

		[GMLFunction("layer_sprite_get_yscale")]
		public static object? layer_sprite_get_yscale(object?[] args)
		{
			var element_id = args[0].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMSprite sprite && sprite.Element.Id == element_id)
					{
						return sprite.YScale;
					}
				}
			}

			return 0;
		}


		// layer_sprite_get_angle
		// layer_sprite_get_blend
		// layer_sprite_get_alpha

		[GMLFunction("layer_sprite_get_x")]
		public static object? layer_sprite_get_x(object?[] args)
		{
			var element_id = args[0].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMSprite sprite && sprite.Element.Id == element_id)
					{
						return sprite.X;
					}
				}
			}

			return 0;
		}

		[GMLFunction("layer_sprite_get_y")]
		public static object? layer_sprite_get_y(object?[] args)
		{
			var element_id = args[0].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMSprite sprite && sprite.Element.Id == element_id)
					{
						return sprite.Y;
					}
				}
			}

			return 0;
		}

		// instance_activate_layer
		// instance_deactivate_layer

		[GMLFunction("layer_tilemap_get_id")]
		public static object layer_tilemap_get_id(object?[] args)
		{
			var layer_id = args[0].Conv<int>();

			if (!RoomManager.CurrentRoom.Layers.ContainsKey(layer_id))
			{
				DebugLog.Log($"layer_tilemap_get_id() - specified tilemap not found");
				return -1;
			}

			var layer = RoomManager.CurrentRoom.Layers[layer_id];

			var layerElements = layer.LayerAsset.Elements;
			var element = layerElements.FirstOrDefault(x => x is CLayerTilemapElement);
			if (element == default)
			{
				return -1;
			}
			else
			{
				return element.Id;
			}
		}

		// layer_tilemap_exists
		// layer_tilemap_create
		// layer_tilemap_destroy
		// tilemap_tileset

		[GMLFunction("tilemap_x")]
		public static object? tilemap_x(object?[] args)
		{
			var tilemap_element_id = args[0].Conv<int>();
			var x = args[1].Conv<double>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMTilesLayer tilemap && tilemap.Element.Id == tilemap_element_id)
					{
						tilemap.Element.x = x;
					}
				}
			}

			return null;
		}

		[GMLFunction("tilemap_y")]
		public static object? tilemap_y(object?[] args)
		{
			var tilemap_element_id = args[0].Conv<int>();
			var y = args[1].Conv<double>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMTilesLayer tilemap && tilemap.Element.Id == tilemap_element_id)
					{
						tilemap.Element.y = y;
					}
				}
			}

			return null;
		}

		// tilemap_set
		// tilemap_set_at_pixel
		// tilemap_get_texture
		// tilemap_get_uvs
		// tilemap_get_name
		// tilemap_get_tileset
		// tilemap_get_tile_width
		// tilemap_get_tile_height
		// tilemap_get_width
		// tilemap_get_height
		// tilemap_set_width
		// tilemap_set_height

		[GMLFunction("tilemap_get_x")]
		public static object tilemap_get_x(object?[] args)
		{
			var tilemap_element_id = args[0].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMTilesLayer tilemap && tilemap.Element.Id == tilemap_element_id)
					{
						return tilemap.Element.x;
					}
				}
			}

			return 0;
		}

		[GMLFunction("tilemap_get_y")]
		public static object tilemap_get_y(object?[] args)
		{
			var tilemap_element_id = args[0].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers)
			{
				foreach (var element in layer.Value.ElementsToDraw)
				{
					if (element is GMTilesLayer tilemap && tilemap.Element.Id == tilemap_element_id)
					{
						return tilemap.Element.x;
					}
				}
			}

			return 0;
		}

		// tilemap_get
		// tilemap_get_at_pixel
		// tilemap_get_cell_x_at_pixel
		// tilemap_get_cell_y_at_pixel
		// tilemap_clear

		[GMLFunction("draw_tilemap")]
		public static object? draw_tilemap(object?[] args)
		{
			var element_id = args[0].Conv<int>();
			var x = args[1].Conv<double>();
			var y = args[1].Conv<double>();

			foreach (var layer in RoomManager.CurrentRoom.Layers.Values)
			{
				foreach (var element in layer.ElementsToDraw)
				{
					if (element is GMTilesLayer tilemap && tilemap.Element.Id == element_id)
					{
						var oldDepth = tilemap.depth;
						var oldX = tilemap.Element.x;
						var oldY = tilemap.Element.y;
						var wasVisible = tilemap.Element.Layer.Visible;

						// TODO - whats the point of setting depth? isn't that just for drawing ordering?
						tilemap.depth = VMExecutor.Self.GMSelf.depth;
						tilemap.Element.x = x;
						tilemap.Element.y = y;
						tilemap.Element.Layer.Visible = true;

						tilemap.Draw();

						tilemap.depth = oldDepth;
						tilemap.Element.x = oldX;
						tilemap.Element.y = oldY;
						tilemap.Element.Layer.Visible = wasVisible;
					}
				}
			}

			return null;
		}

		// draw_tile
		// tilemap_set_global_mask
		// tilemap_get_global_mask
		// tilemap_set_mask
		// tilemap_get_mask
		// tilemap_get_frame
		// tile_set_empty
		// tile_set_index
		// tile_set_flip
		// tile_set_mirror
		// tile_set_rotate
		// tile_get_empty
		// tile_get_index
		// tile_get_flip
		// tile_get_mirror
		// tile_get_rotate
		// layer_tile_exists
		// layer_tile_create
		// layer_tile_destroy
		// layer_tile_change
		// layer_tile_xscale
		// layer_tile_yscale
		// layer_tile_blend

		[GMLFunction("layer_tile_alpha")]
		public static object? layer_tile_alpha(object?[] args)
		{
			var __index = args[0].Conv<int>();
			var __alpha = args[1].Conv<double>();

			__alpha = Math.Clamp(__alpha, 0, 1);

			foreach (var layer in RoomManager.CurrentRoom.Layers.Values)
			{
				foreach (var element in layer.ElementsToDraw)
				{
					if (element is GMTile tile && tile.instanceId == __index)
					{
						var col = tile.Color.ABGRToCol4();
						col.A = (float)__alpha;
						tile.Color = col.Col4ToABGR();
					}
				}
			}

			return null;
		}

		[GMLFunction("layer_tile_x")]
		public static object? layer_tile_x(object?[] args)
		{
			var __index = args[0].Conv<int>();
			var x = args[1].Conv<double>();

			foreach (var layer in RoomManager.CurrentRoom.Layers.Values)
			{
				foreach (var element in layer.ElementsToDraw)
				{
					if (element is GMTile tile && tile.instanceId == __index)
					{
						tile.X = x;
					}
				}
			}

			return null;
		}

		[GMLFunction("layer_tile_y")]
		public static object? layer_tile_y(object?[] args)
		{
			var __index = args[0].Conv<int>();
			var y = args[1].Conv<double>();

			foreach (var layer in RoomManager.CurrentRoom.Layers.Values)
			{
				foreach (var element in layer.ElementsToDraw)
				{
					if (element is GMTile tile && tile.instanceId == __index)
					{
						tile.Y = y;
					}
				}
			}

			return null;
		}

		// layer_tile_region
		// layer_tile_visible
		// layer_tile_get_sprite
		// layer_tile_get_xscale
		// layer_tile_get_yscale
		// layer_tile_get_blend
		// layer_tile_get_alpha

		[GMLFunction("layer_tile_get_x")]
		public static object? layer_tile_get_x(object?[] args)
		{
			var __index = args[0].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers.Values)
			{
				foreach (var element in layer.ElementsToDraw)
				{
					if (element is GMTile tile && tile.instanceId == __index)
					{
						return tile.X;
					}
				}
			}

			return 0;
		}

		[GMLFunction("layer_tile_get_y")]
		public static object? layer_tile_get_y(object?[] args)
		{
			var __index = args[0].Conv<int>();

			foreach (var layer in RoomManager.CurrentRoom.Layers.Values)
			{
				foreach (var element in layer.ElementsToDraw)
				{
					if (element is GMTile tile && tile.instanceId == __index)
					{
						return tile.Y;
					}
				}
			}

			return 0;
		}

		// layer_tile_get_region
		// layer_tile_get_visible
		// layer_instance_get_instance

		// sequence stuff
	}
}

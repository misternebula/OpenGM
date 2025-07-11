using OpenGM.Loading;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class ResourceFunctions
    {
        [GMLFunction("sprite_exists")]
        public static object sprite_exists(object?[] args)
        {
            var index = args[0].Conv<int>();
            return SpriteManager._spriteDict.ContainsKey(index);
        }

        [GMLFunction("sprite_get_name")]
        public static object? sprite_get_name(object?[] args)
        {
            var index = args[0].Conv<int>();

            return SpriteManager._spriteDict[index].Name;
        }

        [GMLFunction("sprite_get_number")]
        public static object sprite_get_number(object?[] args)
        {
            var index = args[0].Conv<int>();
            return SpriteManager.GetNumberOfFrames(index);
        }

        [GMLFunction("sprite_get_width")]
        public static object sprite_get_width(object?[] args)
        {
            var index = args[0].Conv<int>();
            var sprite = SpriteManager._spriteDict[index];
            return sprite.Width;
        }

        [GMLFunction("sprite_get_height")]
        public static object sprite_get_height(object?[] args)
        {
            var index = args[0].Conv<int>();
            var sprite = SpriteManager._spriteDict[index];
            return sprite.Height;
        }

        [GMLFunction("sprite_get_xoffset")]
        public static object sprite_get_xoffset(object?[] args)
        {
            var index = args[0].Conv<int>();
            var sprite = SpriteManager._spriteDict[index];
            return sprite.OriginX;
        }

        [GMLFunction("sprite_get_yoffset")]
        public static object sprite_get_yoffset(object?[] args)
        {
            var index = args[0].Conv<int>();
            var sprite = SpriteManager._spriteDict[index];
            return sprite.OriginY;
        }

        // sprite_get_bbox_mode

        [GMLFunction("sprite_get_bbox_left")]
        public static object sprite_get_bbox_left(object?[] args)
        {
            var ind = args[0].Conv<int>();
            var sprite = SpriteManager._spriteDict[ind];
            return sprite.MarginLeft;
        }

        [GMLFunction("sprite_get_bbox_right")]
        public static object sprite_get_bbox_right(object?[] args)
        {
            var ind = args[0].Conv<int>();
            var sprite = SpriteManager._spriteDict[ind];
            return sprite.MarginRight;
        }

        [GMLFunction("sprite_get_bbox_top")]
        public static object sprite_get_bbox_top(object?[] args)
        {
            var ind = args[0].Conv<int>();
            var sprite = SpriteManager._spriteDict[ind];
            return sprite.MarginTop;
        }

        [GMLFunction("sprite_get_bbox_bottom")]
        public static object sprite_get_bbox_bottom(object?[] args)
        {
            var ind = args[0].Conv<int>();
            var sprite = SpriteManager._spriteDict[ind];
            return sprite.MarginBottom;
        }

        // sprite_collision_mask
        // sprite_set_cache_size
        // sprite_set_cache_size_ext
        // font_set_cache_size
        // sprite_get_tpe

        [GMLFunction("sprite_set_offset")]
        public static object? sprite_set_offset(object?[] args)
        {
            var ind = args[0].Conv<int>();
            var xoff = args[1].Conv<int>();
            var yoff = args[2].Conv<int>();

            var data = SpriteManager._spriteDict[ind];
            data.OriginX = xoff;
            data.OriginY = yoff;
            return null;
        }

        // sprite_set_bbox_mode
        // sprite_set_bbox
        // sprite_set_alpha_from_sprite
        // sprite_add

        [GMLFunction("sprite_create_from_surface")]
        public static object sprite_create_from_surface(object?[] args)
        {
            var index = args[0].Conv<int>();
            var x = args[1].Conv<double>(); // unused by html5. probably sourcepos but deltarune doesnt use it so idc to find out
            var y = args[2].Conv<double>();
            var w = args[3].Conv<int>();
            var h = args[4].Conv<int>();
            var removeback = args[5].Conv<bool>(); // TODO: implement
            var smooth = args[6].Conv<bool>();
            var xorig = args[7].Conv<int>();
            var yorig = args[8].Conv<int>();

            return SpriteManager.sprite_create_from_surface(index, x, y, w, h, removeback, smooth, xorig, yorig);
        }

        // sprite_sprite_add_from_surface
        // sprite_replace
        // sprite_save_strip

        [GMLFunction("sprite_delete")]
        public static object? sprite_delete(object?[] args)
        {
            var index = args[0].Conv<int>();

            return SpriteManager.sprite_delete(index);
        }

        // sprite_duplicate
        // sprite_assign
        // sprite_merge
        // sprite_save

        [GMLFunction("sprite_prefetch", GMLFunctionFlags.Stub)]
        public static object? sprite_prefetch(object?[] args)
        {
            // TODO : implement?
            return 0;
        }

        // sprite_prefetch_multi
        // sprite_flush
        // sprite_flush_multi
        // sprite_set_speed
        // sprite_get_speed_type
        // sprite_get_speed
        // sprite_get_nineslice
        // sprite_set_nineslice
        // sprite_nineslice_create

        [GMLFunction("texture_is_ready", GMLFunctionFlags.Stub)]
        public static object? texture_is_ready(object?[] args)
        {
            // todo : implement?
            return true;
        }

        [GMLFunction("texture_prefetch", GMLFunctionFlags.Stub)]
        public static object? texture_prefetch(object?[] args)
        {
            var tex_id = args[0].Conv<string>();
            // TODO : Implement? Or not?
            return null;
        }

        [GMLFunction("texture_flush", GMLFunctionFlags.Stub)]
        public static object? texture_flush(object?[] args)
        {
            var tex_id = args[0].Conv<string>();
            // TODO : Implement? Or not?
            return null;
        }

        [GMLFunction("texturegroup_get_textures")]
        public static object texturegroup_get_textures(object?[] args)
        {
            var tex_id = args[0].Conv<string>();

            if (!GameLoader.TexGroups.TryGetValue(tex_id, out var texGroup))
            {
                return Array.Empty<string>();
            }

            return texGroup.TexturePages;
        }

        // texturegroup_get_sprites
        // texturegroup_get_fonts
        // texturegroup_get_tilesets
        // texture_debug_messages
        // font_exists
        // font_get_name
        // font_get_fontname
        // font_get_size
        // font_get_bold
        // font_get_italic
        // font_get_first
        // font_get_last
        // font_add_enable_aa
        // font_add_get_enable_aa
        // font_add
        // font_add_sprite

        [GMLFunction("font_add_sprite_ext")]
        public static object font_add_sprite_ext(object?[] args)
        {
            var spriteAssetIndex = args[0].Conv<int>();
            var string_map = args[1].Conv<string>();
            var prop = args[2].Conv<bool>();
            var sep = args[3].Conv<int>();

            var spriteAsset = SpriteManager.GetSpriteAsset(spriteAssetIndex)!;

            var index = AssetIndexManager.Register(AssetType.fonts, $"fnt_{spriteAsset.Name}");

            var newFont = new FontAsset
            {
                AssetIndex = index,
                name = $"fnt_{spriteAsset.Name}",
                spriteIndex = spriteAssetIndex,
                sep = sep,
                Size = spriteAsset.Width,
                ScaleX = 1,
                ScaleY = 1
            };

            for (var i = 0; i < string_map.Length; i++)
            {
                var page = SpriteManager.GetSpritePageItem(spriteAssetIndex, i);

                var fontAssetEntry = new Glyph
                {
                    characterIndex = string_map[i],
                    frameIndex = i,
                    x = page.SourceX, 
                    y = page.SourceY,
                    w = page.SourceWidth,
                    h = page.SourceHeight,
                    shift = page.SourceWidth,
                    // this looks wrong for some reason so commenting it out for now
                    // xOffset = page.TargetX,
                    yOffset = page.TargetY
                };

                newFont.entries.Add(fontAssetEntry);
                newFont.entriesDict.Add(fontAssetEntry.characterIndex, fontAssetEntry);
            }

            TextManager.FontAssets.Add(newFont);

            return newFont.AssetIndex;
        }

        // font_replace_sprite
        // font_replace_sprite_ext
        // font_delete
        // script_exists
        // script_get_name

        [GMLFunction("script_execute")]
        public static object? script_execute(object?[] args)
        {
            var scriptAssetId = args[0].Conv<int>();
            var scriptArgs = args[1..];

            return VMExecutor.ExecuteCode(ScriptResolver.ScriptsByIndex[scriptAssetId].GetCode(), VMExecutor.Self.Self, VMExecutor.Self.ObjectDefinition, args: scriptArgs);
        }

        // script_execute_ext

        [GMLFunction("path_exists")]
        public static object? path_exists(object?[] args)
        {
            var index = args[0].Conv<int>();

            return PathManager.Paths.ContainsKey(index);
        }

        // path_get_name
        // path_get_length
        // path_get_kind
        // path_get_closed
        // path_get_precision
        // path_get_number
        // path_get_point_x
        // path_get_point_y
        // path_get_point_speed

        [GMLFunction("path_get_x")]
        public static object? path_get_x(object?[] args)
        {
            var index = args[0].Conv<int>();
            var pos = args[0].Conv<float>();

            if (!PathManager.Paths.TryGetValue(index, out var path))
            {
                return -1; // this isn't documented anywhere, smh gamemaker
            }

            return path.XPosition(pos);
        }

        [GMLFunction("path_get_y")]
        public static object? path_get_y(object?[] args)
        {
            var index = args[0].Conv<int>();
            var pos = args[0].Conv<float>();

            if (!PathManager.Paths.TryGetValue(index, out var path))
            {
                return -1; // this isn't documented anywhere, smh gamemaker
            }

            return path.YPosition(pos);
        }

        // path_get_speed

        [GMLFunction("path_set_kind")]
        public static object? path_set_kind(object?[] args)
        {
            var index = args[0].Conv<int>();
            var val = args[1].Conv<int>();

            var path = PathManager.Paths[index];
            path.kind = val;
            return null;
        }

        [GMLFunction("path_set_closed")]
        public static object? path_set_closed(object?[] args)
        {
            var id = args[0].Conv<int>();
            var value = args[1].Conv<bool>();

            PathManager.Paths[id].closed = value;
            PathManager.ComputeInternal(PathManager.Paths[id]);
            return null;
        }

        [GMLFunction("path_set_precision")]
        public static object? path_set_precision(object?[] args)
        {
            var id = args[0].Conv<int>();
            var value = args[1].Conv<int>();

            PathManager.Paths[id].precision = value;
            PathManager.ComputeInternal(PathManager.Paths[id]);
            return null;
        }

        [GMLFunction("path_add")]
        public static object path_add(object?[] args)
        {
            return PathManager.PathAdd();
        }

        // path_duplicate
        // path_assign
        // path_append

        [GMLFunction("path_delete")]
        public static object? path_delete(object?[] args)
        {
            var index = args[0].Conv<int>();
            PathManager.PathDelete(index);
            return null;
        }

        [GMLFunction("path_add_point")]
        public static object? path_add_point(object?[] args)
        {
            var id = args[0].Conv<int>();
            var x = args[1].Conv<float>();
            var y = args[2].Conv<float>();
            var speed = args[3].Conv<float>();

            var path = PathManager.Paths[id];
            PathManager.AddPoint(path, x, y, speed);

            return null;
        }

        // path_insert_point
        // path_change_point
        // path_delete_point
        // path_clear_points
        // path_reverse
        // path_mirror
        // path_flip
        // path_rotate
        // path_rescale
        // path_shift
        // timeline_exists
        // timeline_get_name
        // timeline_add
        // timeline_delete
        // timeline_moment_clear
        // timeline_clear
        // timeline_moment_add
        // timeline_moment_add_script
        // timeline_size
        // timeline_max_moment
        // object_exists

        [GMLFunction("object_get_name")]
        public static object object_get_name(object?[] args)
        {
            var obj = args[0].Conv<int>();
            return InstanceManager.ObjectDefinitions[obj].Name;
        }

        [GMLFunction("object_get_sprite")]
        public static object object_get_sprite(object?[] args)
        {
            var obj = args[0].Conv<int>();
            return InstanceManager.ObjectDefinitions[obj].sprite;
        }

        // object_get_solid
        // object_get_visible
        // object_get_persistent
        // object_get_mask

        [GMLFunction("object_get_parent")]
        public static object? object_get_parent(object?[] args)
        {
            var obj = args[0].Conv<int>();

            if (!InstanceManager.ObjectDefinitions.TryGetValue(obj, out var objectDef))
            {
                return -1;
            }

            if (objectDef.parent == null)
            {
                return -100; // lmao
            }

            return objectDef.parent.AssetId;
        }

        // object_get_physics

        [GMLFunction("object_is_ancestor")]
        public static object object_is_ancestor(object?[] args)
        {
            var obj = args[0].Conv<int>();
            var par = args[1].Conv<int>();

            var objDef = InstanceManager.ObjectDefinitions[obj];
            var parDef = InstanceManager.ObjectDefinitions[par];

            var currentParent = objDef;
            while (currentParent != null)
            {
                if (currentParent.AssetId == parDef.AssetId)
                {
                    return true;
                }

                currentParent = currentParent.parent;
            }

            return false;
        }

        // object_set_sprite
        // object_set_solid
        // object_set_visible
        // object_set_persistent
        // object_set_mask
        // object_set_parent
        // object_add
        // object_delete
        // object_event_clear
        // object_event_add

        [GMLFunction("room_exists")]
        public static object? room_exists(object?[] args)
        {
            var index = args[0].Conv<int>();
            return RoomManager.RoomList.ContainsKey(index);
        }

        [GMLFunction("room_get_name")]
        public static object room_get_name(object?[] args)
        {
            var index = args[0].Conv<int>();

            return RoomManager.RoomList[index].Name;
        }

        // room_set_width
        // room_set_height
        // room_set_caption

        [GMLFunction("room_set_persistent")]
        public static object? room_set_persistent(object?[] args)
        {
            var roomIndex = args[0].Conv<int>();
            var persistent = args[1].Conv<bool>();

            var room = RoomManager.RoomList[roomIndex];
            room.Persistent = persistent;

            return null;
        }

        // room_set_background_color
        // room_set_background_colour
        // room_set_viewport
        // room_get_viewport
        // room_set_view_enabled
        // room_add
        // room_duplicate
        // room_assign
        // room_instance_add
        // room_instance_clear
        // room_get_camera
        // room_set_camera

        [GMLFunction("asset_get_index")]
        public static object asset_get_index(object?[] args)
        {
            var name = args[0].Conv<string>();
            return AssetIndexManager.GetIndex(name);
        }

        // asset_get_type
    }
}

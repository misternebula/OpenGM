using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.IO;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
	public static class ParticlesFunctions
    {
	    [GMLFunction("part_type_create")]
	    public static object? part_type_create(object?[] args)
	    {
		    DebugLog.LogWarning("part_type_create not implemented.");
		    return null;
	    }

		// part_type_destroy
		// part_type_exists
		// part_type_clear
		// part_type_shape

		[GMLFunction("part_type_sprite")]
		public static object? part_type_sprite(object?[] args)
		{
			DebugLog.LogWarning("part_type_sprite not implemented.");
			return null;
		}

		[GMLFunction("part_type_size")]
		public static object? part_type_size(object?[] args)
		{
			DebugLog.LogWarning("part_type_size not implemented.");
			return null;
		}

		// part_type_scale

		[GMLFunction("part_type_life")]
		public static object? part_type_life(object?[] args)
		{
			DebugLog.LogWarning("part_type_life not implemented.");
			return null;
		}

		// part_type_step
		// part_type_death

		[GMLFunction("part_type_speed")]
		public static object? part_type_speed(object?[] args)
		{
			DebugLog.LogWarning("part_type_speed not implemented.");
			return null;
		}

		[GMLFunction("part_type_direction")]
		public static object? part_type_direction(object?[] args)
		{
			DebugLog.LogWarning("part_type_direction not implemented.");
			return null;
		}

		// part_type_orientation
		// part_type_gravity
		// part_type_color_mix
		// part_type_color_rgb
		// part_type_color_hsv
		// part_type_color1
		// part_type_color2
		// part_type_color3
		// part_type_colour_mix
		// part_type_colour_rgb
		// part_type_colour_hsv
		// part_type_colour1
		// part_type_colour2
		// part_type_colour3
		// part_type_alpha1
		// part_type_alpha2

		[GMLFunction("part_type_alpha3")]
		public static object? part_type_alpha3(object?[] args)
		{
			DebugLog.LogWarning("part_type_alpha3 not implemented.");
			return null;
		}

		[GMLFunction("part_type_blend")]
		public static object? part_type_blend(object?[] args)
		{
			DebugLog.LogWarning("part_type_blend not implemented.");
			return null;
		}

		[GMLFunction("part_system_create")]
		public static object? part_system_create(object?[] args)
		{
			DebugLog.LogWarning("part_system_create not implemented.");
			return null;
		}

		[GMLFunction("part_system_destroy")]
		public static object? part_system_destroy(object?[] args)
		{
			DebugLog.LogWarning("part_system_destroy not implemented.");
			return null;
		}

		// part_system_exists
		// part_system_clear
		// part_system_draw_order
		// part_system_depth
		// part_system_position
		// part_system_automatic_update

		[GMLFunction("part_system_automatic_draw")]
		public static object? part_system_automatic_draw(object?[] args)
		{
			DebugLog.LogWarning("part_system_automatic_draw not implemented");
			return null;
		}

		[GMLFunction("part_system_update")]
		public static object? part_system_update(object?[] args)
		{
			DebugLog.LogWarning("part_system_update not implemented");
			return null;
		}

		[GMLFunction("part_system_drawit")]
		public static object? part_system_drawit(object?[] args)
		{
			DebugLog.LogWarning("part_system_drawit not implemented");
			return null;
		}

		// part_system_create_layer
		// part_system_get_layer
		// part_system_layer
		// part_particles_create
		// part_particles_create_color
		// part_particles_create_colour
		// part_particles_clear
		// part_particles_count

		[GMLFunction("part_emitter_create")]
		public static object? part_emitter_create(object?[] args)
		{
			DebugLog.LogWarning("part_emitter_create not implemented");
			return null;
		}

		// part_emitter_destroy

		[GMLFunction("part_emitter_destroy_all")]
		public static object? part_emitter_destroy_all(object?[] args)
		{
			DebugLog.LogWarning("part_emitter_destroy_all not implemented");
			return null;
		}

		// part_emitter_exists
		// part_emitter_clear

		[GMLFunction("part_emitter_region")]
		public static object? part_emitter_region(object?[] args)
		{
			DebugLog.LogWarning("part_emitter_region not implemented");
			return null;
		}

		// part_emitter_burst

		[GMLFunction("part_emitter_stream")]
		public static object? part_emitter_stream(object?[] args)
		{
			DebugLog.LogWarning("part_emitter_stream not implemented");
			return null;
		}

		// effect_create_blow
		// effect_create_above
		// effect_clear
	}
}

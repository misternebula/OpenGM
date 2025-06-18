using OpenGM.VirtualMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.IO
{
	public static class ShaderFunctions
	{
		[GMLFunction("shader_set")]
		public static object? shader_set(object?[] args)
		{
			var shaderId = args[0].Conv<int>();
			DebugLog.LogWarning("shader_set not implemented.");
			return null;
		}

		// shader_get_name

		[GMLFunction("shader_reset")]
		public static object? shader_reset(object?[] args)
		{
			DebugLog.LogWarning("shader_reset not implemented.");
			return null;
		}

		// shader_current

		[GMLFunction("shader_get_uniform")]
		public static object? shader_get_uniform(object?[] args)
		{
			DebugLog.LogWarning("shader_get_uniform not implemented.");
			return null;
		}

		[GMLFunction("shader_get_sampler_index")]
		public static object? shader_get_sampler_index(object?[] args)
		{
			DebugLog.LogWarning("shader_get_sampler_index not implemented.");
			return -1;
		}

		// shader_set_uniform_i
		// shader_set_uniform_i_array

		[GMLFunction("shader_set_uniform_f")]
		public static object? shader_set_uniform_f(object?[] args)
		{
			DebugLog.LogWarning("shader_set_uniform_f not implemented.");
			return null;
		}

		// shader_set_uniform_f_array
		// shader_set_uniform_matrix
		// shader_set_uniform_matrix_array
		// shader_is_compiled
		// shaders_are_supported

		[GMLFunction("texture_set_stage")]
		public static object? texture_set_stage(object?[] args)
		{
			DebugLog.LogWarning("texture_set_stage not implemented.");
			return null;
		}

		[GMLFunction("texture_get_texel_width")]
		public static object? texture_get_texel_width(object?[] args)
		{
			DebugLog.LogWarning("texture_get_texel_width not implemented.");
			return 0;
		}

		[GMLFunction("texture_get_texel_height")]
		public static object? texture_get_texel_height(object?[] args)
		{
			DebugLog.LogWarning("texture_get_texel_height not implemented.");
			return 0;
		}
	}
}

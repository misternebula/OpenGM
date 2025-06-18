using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.IO
{
    public static class VertexFormatFunctions
    {
		[GMLFunction("vertex_format_begin")]
	    public static object? vertex_format_begin(object?[] args)
	    {
		    DebugLog.LogWarning("vertex_format_begin not implemented.");
		    return null;
	    }

		// vertex_format_delete

		[GMLFunction("vertex_format_end")]
		public static object? vertex_format_end(object?[] args)
		{
			DebugLog.LogWarning("vertex_format_end not implemented.");
			return null;
		}

		[GMLFunction("vertex_format_add_position")]
		public static object? vertex_format_add_position(object?[] args)
		{
			DebugLog.LogWarning("vertex_format_add_position not implemented.");
			return null;
		}

		// vertex_format_add_position_3d

		[GMLFunction("vertex_format_add_color")]
		[GMLFunction("vertex_format_add_colour")]
		public static object? vertex_format_add_colour(object?[] args)
		{
			DebugLog.LogWarning("vertex_format_add_colour not implemented.");
			return null;
		}

		[GMLFunction("vertex_format_add_normal")]
		public static object? vertex_format_add_normal(object?[] args)
		{
			DebugLog.LogWarning("vertex_format_add_normal not implemented.");
			return null;
		}

		// vertex_format_add_textcoord
		// vertex_format_add_texcoord
		// vertex_format_add_custom
	}
}

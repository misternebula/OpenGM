using OpenGM.IO;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class VertexFormatFunctions
    {
        public static VertexFormat? NewFormat;

        [GMLFunction("vertex_format_begin")]
        public static object? vertex_format_begin(object?[] args)
        {
            if (NewFormat != null)
            {
                throw new Exception("Already creating format!");
            }

            NewFormat = new();
            return null;
        }

        // vertex_format_delete

        [GMLFunction("vertex_format_end", GMLFunctionFlags.Stub, stubLogType: DebugLog.LogType.Warning)]
        public static object? vertex_format_end(object?[] args)
        {
            NewFormat = null;
            return null;
        }

        [GMLFunction("vertex_format_add_position")]
        public static object? vertex_format_add_position(object?[] args)
        {
            if (NewFormat == null)
            {
                throw new Exception("No format is under construction!");
            }

            NewFormat.AddPosition();
            return null;
        }

        // vertex_format_add_position_3d

        [GMLFunction("vertex_format_add_color")]
        [GMLFunction("vertex_format_add_colour")]
        public static object? vertex_format_add_colour(object?[] args)
        {
            if (NewFormat == null)
            {
                throw new Exception("No format is under construction!");
            }

            NewFormat.AddColor();
            return null;
        }

        [GMLFunction("vertex_format_add_normal")]
        public static object? vertex_format_add_normal(object?[] args)
        {
            if (NewFormat == null)
            {
                throw new Exception("No format is under construction!");
            }

            NewFormat.AddNormal();
            return null;
        }

        // vertex_format_add_textcoord
        // vertex_format_add_texcoord
        // vertex_format_add_custom
    }
}

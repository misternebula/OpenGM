using OpenGM.VirtualMachine;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class ShaderFunctions
    {
        [GMLFunction("shader_set", GMLFunctionFlags.Stub)]
        public static object? shader_set(object?[] args)
        {
            var shaderId = args[0].Conv<int>();
            return null;
        }

        // shader_get_name

        [GMLFunction("shader_reset", GMLFunctionFlags.Stub)]
        public static object? shader_reset(object?[] args)
        {
            return null;
        }

        [GMLFunction("shader_current", GMLFunctionFlags.Stub)]
        public static object? shader_current(object?[] args)
        {
            return null;
        }

        [GMLFunction("shader_get_uniform", GMLFunctionFlags.Stub)]
        public static object? shader_get_uniform(object?[] args)
        {
            return null;
        }

        [GMLFunction("shader_get_sampler_index", GMLFunctionFlags.Stub)]
        public static object? shader_get_sampler_index(object?[] args)
        {
            return -1;
        }

        [GMLFunction("shader_set_uniform_i", GMLFunctionFlags.Stub)]
        public static object? shader_set_uniform_i(object?[] args)
        {
            return null;
        }

        // shader_set_uniform_i_array

        [GMLFunction("shader_set_uniform_f", GMLFunctionFlags.Stub)]
        public static object? shader_set_uniform_f(object?[] args)
        {
            return null;
        }

        // shader_set_uniform_f_array
        // shader_set_uniform_matrix
        // shader_set_uniform_matrix_array
        // shader_is_compiled
        // shaders_are_supported

        [GMLFunction("texture_set_stage", GMLFunctionFlags.Stub)]
        public static object? texture_set_stage(object?[] args)
        {
            return null;
        }

        [GMLFunction("texture_get_texel_width", GMLFunctionFlags.Stub)]
        public static object? texture_get_texel_width(object?[] args)
        {
            return 0;
        }

        [GMLFunction("texture_get_texel_height", GMLFunctionFlags.Stub)]
        public static object? texture_get_texel_height(object?[] args)
        {
            return 0;
        }
    }
}

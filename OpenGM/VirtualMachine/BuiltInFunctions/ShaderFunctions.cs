using OpenGM.IO;
using OpenGM.Rendering;
using OpenTK.Graphics.OpenGL;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class ShaderFunctions
    {
        [GMLFunction("shader_set")]
        public static object? shader_set(object?[] args)
        {
            var shaderId = args[0].Conv<int>();
            ShaderManager.ShaderSet(shaderId);
            return null;
        }

        // shader_get_name

        [GMLFunction("shader_reset")]
        public static object? shader_reset(object?[] args)
        {
            ShaderManager.ShaderReset();
            return null;
        }

        [GMLFunction("shader_current")]
        public static object? shader_current(object?[] args)
        {
            return ShaderManager.CurrentShaderIndex;
        }

        [GMLFunction("shader_get_uniform")]
        public static object? shader_get_uniform(object?[] args)
        {
            var shader = args[0].Conv<int>();
            var uniform = args[1].Conv<string>();

            var runtimeShader = ShaderManager.Shaders[shader];

            if (!runtimeShader.Uniforms.TryGetValue(uniform, out var shaderUniform))
            {
                return -1;
            }

            return shaderUniform.Location;
        }

        [GMLFunction("shader_get_sampler_index", GMLFunctionFlags.Stub, stubLogType: DebugLog.LogType.Warning)]
        public static object? shader_get_sampler_index(object?[] args)
        {
            return -1;
        }

        [GMLFunction("shader_set_uniform_i")]
        public static object? shader_set_uniform_i(object?[] args)
        {
            var handle = args[0].Conv<int>();

            if (handle == -1)
            {
                return null;
            }

            var valueCount = args.Length - 1;
            switch (valueCount)
            {
                case 1:
                    GL.Uniform1(handle, args[1].Conv<int>());
                    break;
                case 2:
                    GL.Uniform2(handle, args[1].Conv<int>(), args[2].Conv<int>());
                    break;
                case 3:
                    GL.Uniform3(handle, args[1].Conv<int>(), args[2].Conv<int>(), args[3].Conv<int>());
                    break;
                case 4:
                    GL.Uniform4(handle, args[1].Conv<int>(), args[2].Conv<int>(), args[3].Conv<int>(), args[4].Conv<int>());
                    break;
                default:
                    throw new NotImplementedException();
            }

            return null;
        }

        // shader_set_uniform_i_array

        [GMLFunction("shader_set_uniform_f")]
        public static object? shader_set_uniform_f(object?[] args)
        {
            var handle = args[0].Conv<int>();

            var valueCount = args.Length - 1;
            switch (valueCount)
            {
                case 1:
                    GL.Uniform1(handle, args[1].Conv<float>());
                    break;
                case 2:
                    GL.Uniform2(handle, args[1].Conv<float>(), args[2].Conv<float>());
                    break;
                case 3:
                    GL.Uniform3(handle, args[1].Conv<float>(), args[2].Conv<float>(), args[3].Conv<float>());
                    break;
                case 4:
                    GL.Uniform4(handle, args[1].Conv<float>(), args[2].Conv<float>(), args[3].Conv<float>(), args[4].Conv<float>());
                    break;
                default:
                    throw new NotImplementedException();
            }

            return null;
        }

        // shader_set_uniform_f_array
        // shader_set_uniform_matrix
        // shader_set_uniform_matrix_array
        // shader_is_compiled
        // shaders_are_supported

        [GMLFunction("texture_set_stage", GMLFunctionFlags.Stub, stubLogType: DebugLog.LogType.Warning)]
        public static object? texture_set_stage(object?[] args)
        {
            return null;
        }

        [GMLFunction("texture_get_texel_width", GMLFunctionFlags.Stub, stubLogType: DebugLog.LogType.Warning)]
        public static object? texture_get_texel_width(object?[] args)
        {
            return 0;
        }

        [GMLFunction("texture_get_texel_height", GMLFunctionFlags.Stub, stubLogType: DebugLog.LogType.Warning)]
        public static object? texture_get_texel_height(object?[] args)
        {
            return 0;
        }
    }
}

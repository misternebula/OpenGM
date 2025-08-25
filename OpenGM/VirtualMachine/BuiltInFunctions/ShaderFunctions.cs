﻿using OpenGM.IO;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenTK.Graphics.OpenGL4;
using System.Collections;

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

        [GMLFunction("shader_get_sampler_index")]
        public static object? shader_get_sampler_index(object?[] args)
        {
            var shader = args[0].Conv<int>();
            var sampler = args[1].Conv<string>();

            var runtimeShader = ShaderManager.Shaders[shader];

            var index = runtimeShader.TextureStages.IndexOf(sampler);
            return CustomMath.Max(index, 0);
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

            GraphicsManager.CheckError();

            return null;
        }

        [GMLFunction("shader_set_uniform_i_array")]
        public static object? shader_set_uniform_i_array(object?[] args)
        {
            var handle = args[0].Conv<int>();
            var arr = args[1].Conv<IList>().ConvAll<int>().ToArray();

            if (handle == -1)
            {
                return null;
            }

            GL.Uniform1(handle, arr.Length, arr);

            GraphicsManager.CheckError();

            return null;
        }

        [GMLFunction("shader_set_uniform_f")]
        public static object? shader_set_uniform_f(object?[] args)
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

            GraphicsManager.CheckError();

            return null;
        }

        [GMLFunction("shader_set_uniform_f_array")]
        public static object? shader_set_uniform_f_array(object?[] args)
        {
            var handle = args[0].Conv<int>();
            var arr = args[1].Conv<IList>().ConvAll<float>().ToArray();

            if (handle == -1)
            {
                return null;
            }

            GL.Uniform1(handle, arr.Length, arr);

            GraphicsManager.CheckError();

            return null;
        }

        // shader_set_uniform_matrix
        // shader_set_uniform_matrix_array
        // shader_is_compiled
        // shaders_are_supported

        [GMLFunction("texture_set_stage")]
        public static object? texture_set_stage(object?[] args)
        {
            var stage = args[0].Conv<int>();
            var tex = args[1] as SpritePageItem;

            if (tex == null)
            {
                throw new NotImplementedException();
            }

            var (image, id) = PageManager.TexturePages[tex.Page];

            GL.ActiveTexture(TextureUnit.Texture0 + stage);
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.ActiveTexture(TextureUnit.Texture0);
            GraphicsManager.CheckError();
            return null;
        }

        [GMLFunction("texture_get_texel_width")]
        public static object? texture_get_texel_width(object?[] args)
        {
            var tex = args[0] as SpritePageItem;

            if (tex == null)
            {
                throw new NotImplementedException();
            }

            var (image, id) = PageManager.TexturePages[tex.Page];

            return 1.0 / image.Width;
        }

        [GMLFunction("texture_get_texel_height")]
        public static object? texture_get_texel_height(object?[] args)
        {
            var tex = args[0] as SpritePageItem;

            if (tex == null)
            {
                throw new NotImplementedException();
            }

            var (image, id) = PageManager.TexturePages[tex.Page];

            return 1.0 / image.Height;
        }
    }
}

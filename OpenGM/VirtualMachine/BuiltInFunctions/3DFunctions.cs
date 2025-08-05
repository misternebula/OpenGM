using OpenGM.IO;
using OpenGM.Rendering;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Collections;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class _3DFunctions
    {
        public static Matrix4 ViewMatrix;
        public static Matrix4 ProjectionMatrix;
        public static Matrix4 WorldMatrix;

        private static Matrix4 ListToMatrix(IList list)
        {
            if (list.Count != 16)
            {
                throw new Exception("Array must contain exactly 16 elements");
            }

            var matrix = new Matrix4();
            for (var i = 0; i < 4; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    matrix[i, j] = list[(i * 4) + j].Conv<float>();
                }
            }

            return matrix;
        }

        private static IList MatrixToList(Matrix4 matrix)
        {
            var array = new double[16];

            for (var i = 0; i < 4; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    array[(i * 4) + j] = matrix[i, j];
                }
            }

            return array;
        }

        [GMLFunction("matrix_get")]
        public static object? matrix_get(object?[] args)
        {
            var type = args[0].Conv<int>();

            return type switch
            {
                0 => MatrixToList(ViewMatrix),
                1 => MatrixToList(ProjectionMatrix),
                2 => MatrixToList(WorldMatrix),
                _ => throw new Exception("Illegal matrix type"),
            };
        }

        [GMLFunction("matrix_set")]
        public static object? matrix_set(object?[] args)
        {
            var type = args[0].Conv<int>();
            var value = ListToMatrix(args[1].Conv<IList>());

            switch (type)
            {
                case 0:
                    ViewMatrix = value;
                    break;
                case 1:
                    ProjectionMatrix = value;
                    break;
                case 2:
                    WorldMatrix = value;
                    break;
                default:
                    throw new Exception("Illegal matrix type");
            }

            return null;
        }

        [GMLFunction("matrix_build_identity")]
        public static object? matrix_build_identity(object?[] args)
        {
            return MatrixToList(Matrix4.Identity);
        }

        [GMLFunction("matrix_build")]
        public static object? matrix_build(object?[] args)
        {
            var x = args[0].Conv<double>();
            var y = args[1].Conv<double>();
            var z = args[2].Conv<double>();
            var xrot = args[3].Conv<double>();
            var yrot = args[4].Conv<double>();
            var zrot = args[5].Conv<double>();
            var xscale = args[6].Conv<double>();
            var yscale = args[7].Conv<double>();
            var zscale = args[8].Conv<double>();

            // TODO : there is probably a built in function for this, but i forgor
            // https://github.com/YoYoGames/GameMaker-HTML5/blob/3560546dc80cb3c0ec4627021d720a08fc58a95f/scripts/functions/Function_D3D.js#L839

            xrot = -xrot * CustomMath.Deg2Rad;
            yrot = -yrot * CustomMath.Deg2Rad;
            zrot = -zrot * CustomMath.Deg2Rad;

            var sinx = Math.Sin(xrot);
            var cosx = Math.Cos(xrot);
            var siny = Math.Sin(yrot);
            var cosy = Math.Cos(yrot);
            var sinz = Math.Sin(zrot);
            var cosz = Math.Cos(zrot);

            var sinzsinx = -sinz * -sinx;
            var coszsinx = cosz * -sinx;

            var ret = new double[16];

            ret[0] = ((cosz * cosy) + (sinzsinx * -siny)) * xscale;
            ret[4] = -sinz * cosx * xscale;
            ret[8] = ((cosz * siny) + (sinzsinx * cosy)) * xscale;
            ret[12] = x;

            ret[1] = ((sinz * cosy) + (coszsinx * -siny)) * yscale;
            ret[5] = cosz * cosx * yscale;
            ret[9] = ((sinz * siny) + (coszsinx * cosy)) * yscale;
            ret[13] = y;

            ret[2] = cosx * -siny * zscale;
            ret[6] = sinx * zscale;
            ret[10] = cosx * cosy * zscale;
            ret[14] = z;

            ret[3] = ret[7] = ret[11] = 0.0;
            ret[15] = 1.0;

            return ret;
        }

        [GMLFunction("matrix_build_lookat")]
        public static object? matrix_build_lookat(object?[] args)
        {
            var xfrom = (float)args[0].Conv<double>();
            var yfrom = (float)args[1].Conv<double>();
            var zfrom = (float)args[2].Conv<double>();
            var xto = (float)args[3].Conv<double>();
            var yto = (float)args[4].Conv<double>();
            var zto = (float)args[5].Conv<double>();
            var xup = (float)args[6].Conv<double>();
            var yup = (float)args[7].Conv<double>();
            var zup = (float)args[8].Conv<double>();

            return MatrixToList(Matrix4.LookAt(xfrom, yfrom, zfrom, xto, yto, zto, xup, yup, zup));
        }

        [GMLFunction("matrix_build_projection_ortho")]
        public static object? matrix_build_projection_ortho(object?[] args)
        {
            var w = (float)args[0].Conv<double>();
            var h = (float)args[1].Conv<double>();
            var znear = (float)args[2].Conv<double>();
            var zfar = (float)args[3].Conv<double>();

            return MatrixToList(Matrix4.CreateOrthographic(w, h, znear, zfar));
        }

        [GMLFunction("matrix_build_projection_perspective")]
        public static object? matrix_build_projection_perspective(object?[] args)
        {
            var w = (float)args[0].Conv<double>();
            var h = (float)args[1].Conv<double>();
            var znear = (float)args[2].Conv<double>();
            var zfar = (float)args[3].Conv<double>();

            // https://github.com/YoYoGames/GameMaker-HTML5/blob/3560546dc80cb3c0ec4627021d720a08fc58a95f/scripts/Matrix.js#L240

            // TODO - built in function for this?
            // calculate FOV from w/h and near plane?
            // or just easier to do this?

            return new double[16]
            {
                (znear + znear) / w, 0, 0, 0,
                0, (znear + znear) / h, 0, 0,
                0, 0, zfar / (zfar - znear), 1,
                0, 0, -znear * zfar / (zfar - znear), 0
            };
        }

        [GMLFunction("matrix_build_projection_perspective_fov")]
        public static object? matrix_build_projection_perspective_fov(object?[] args)
        {
            var fov = args[0].Conv<double>();
            var aspect = args[1].Conv<double>();
            var znear = (float)args[2].Conv<double>();
            var zfar = (float)args[3].Conv<double>();

            // https://github.com/YoYoGames/GameMaker-HTML5/blob/3560546dc80cb3c0ec4627021d720a08fc58a95f/scripts/Matrix.js#L200

            fov *= CustomMath.Deg2Rad;
            var yScale = 1 / Math.Tan(fov / 2);
            var xScale = yScale / aspect;

            return new double[16]
            {
                xScale, 0, 0, 0,
                0, yScale, 0, 0,
                0, 0, zfar / (zfar - znear), 1,
                0, 0, -znear * zfar / (zfar - znear), 0
            };
        }

        [GMLFunction("matrix_multiply")]
        public static object? matrix_multiply(object?[] args)
        {
            var matrix1 = ListToMatrix(args[0].Conv<IList>());
            var matrix2 = ListToMatrix(args[1].Conv<IList>());

            return MatrixToList(Matrix4.Mult(matrix1, matrix2));
        }

        [GMLFunction("matrix_transform_vertex")]
        public static object? matrix_transform_vertex(object?[] args)
        {
            var matrix = ListToMatrix(args[0].Conv<IList>());
            var x = (float)args[1].Conv<double>();
            var y = (float)args[2].Conv<double>();
            var z = (float)args[3].Conv<double>();

            var vector = new Vector4(x, y, z, 1); // TODO : double check this is meant to be 1 bc im tired when writing this
            var ret = vector * matrix;
            return new double[] { ret.X, ret.Y, ret.Z };
        }

        // draw_texture_flush

        [GMLFunction("draw_flush", GMLFunctionFlags.Stub)]
        public static object? draw_flush(object?[] args)
        {
            return null;
        }

        // matrix_stack_push
        // matrix_stack_pop
        // matrix_stack_set
        // matrix_stack_clear
        // matrix_stack_top
        // matrix_stack_is_empty

        [GMLFunction("gpu_set_blendenable")]
        public static object? gpu_set_blendenable(object?[] args)
        {
            var enable = args[0].Conv<bool>();

            // TODO : is this right?

            if (enable)
            {
                GL.Enable(EnableCap.Blend);
            }
            else
            {
                GL.Disable(EnableCap.Blend);
            }

            return null;
        }

        // gpu_set_ztestenable
        // gpu_set_zfunc
        // gpu_set_zwriteenable

        [GMLFunction("gpu_set_fog")]
        public static object? gpu_set_fog(object?[] args)
        {
            var enable = args[0].Conv<bool>();
            var colour = args[1].Conv<int>();
            var start = args[2].Conv<float>();
            var end = args[3].Conv<float>();

            GraphicsManager.SetFog(enable, colour.ABGRToCol4(), start, end);

            return null;
        }

        // gpu_set_cullmode

        [GMLFunction("gpu_set_blendmode")]
        public static object? gpu_set_blendmode(object?[] args)
        {
            var mode = args[0].Conv<int>();

            switch (mode)
            {
                case 0:
                    // bm_normal
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                    GL.BlendEquation(BlendEquationMode.FuncAdd);
                    break;
                case 1:
                    // bm_add
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                    GL.BlendEquation(BlendEquationMode.FuncAdd);
                    break;
                case 2:
                    // bm_max
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcColor);
                    GL.BlendEquation(BlendEquationMode.FuncAdd);
                    break;
                case 3:
                    // bm_subtract
                    // https://github.com/YoYoGames/GameMaker-Bugs/issues/11061#issuecomment-3005485747
                    GL.BlendFunc(BlendingFactor.Zero, BlendingFactor.OneMinusSrcColor);
                    GL.BlendEquation(BlendEquationMode.FuncAdd);
                    break;
                case 4:
                    // bm_min
                    GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
                    GL.BlendEquation(BlendEquationMode.Min);
                    break;
                case 5:
                    // bm_reverse_subtract
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                    GL.BlendEquation(BlendEquationMode.FuncReverseSubtract);
                    break;
            }

            return null;
        }

        [GMLFunction("gpu_set_blendmode_ext")]
        public static object? gpu_set_blendmode_ext(object?[] args)
        {
            var src = args[0].Conv<int>();
            var dst = args[1].Conv<int>();

            BlendingFactor GetBlend(int arg) => arg switch
            {
                1 => BlendingFactor.Zero,
                2 => BlendingFactor.One,
                3 => BlendingFactor.SrcColor,
                4 => BlendingFactor.OneMinusSrcColor,
                5 => BlendingFactor.SrcAlpha,
                6 => BlendingFactor.OneMinusSrcAlpha,
                7 => BlendingFactor.DstAlpha,
                8 => BlendingFactor.OneMinusDstAlpha,
                9 => BlendingFactor.DstColor,
                10 => BlendingFactor.OneMinusDstColor,
                11 => BlendingFactor.SrcAlphaSaturate,
                _ => throw new ArgumentException(),
            };

            GL.BlendFunc(GetBlend(src), GetBlend(dst));
            return null;
        }

        [GMLFunction("gpu_set_blendmode_ext_sepalpha")]
        public static object? gpu_set_blendmode_ext_sepalpha(object?[] args)
        {
            var src = args[0].Conv<int>();
            var dest = args[1].Conv<int>();
            var alphasrc = args[2].Conv<int>();
            var alphadest = args[3].Conv<int>();

            // TODO : theres gotta be a better way then repeating all these switch cases

            BlendingFactorSrc GetBlendSrc(int arg) => arg switch
            {
                1 => BlendingFactorSrc.Zero,
                2 => BlendingFactorSrc.One,
                3 => BlendingFactorSrc.SrcColor,
                4 => BlendingFactorSrc.OneMinusSrcColor,
                5 => BlendingFactorSrc.SrcAlpha,
                6 => BlendingFactorSrc.OneMinusSrcAlpha,
                7 => BlendingFactorSrc.DstAlpha,
                8 => BlendingFactorSrc.OneMinusDstAlpha,
                9 => BlendingFactorSrc.DstColor,
                10 => BlendingFactorSrc.OneMinusDstColor,
                11 => BlendingFactorSrc.SrcAlphaSaturate,
                _ => throw new ArgumentException(),
            };

            BlendingFactorDest GetBlendDest(int arg) => arg switch
            {
                1 => BlendingFactorDest.Zero,
                2 => BlendingFactorDest.One,
                3 => BlendingFactorDest.SrcColor,
                4 => BlendingFactorDest.OneMinusSrcColor,
                5 => BlendingFactorDest.SrcAlpha,
                6 => BlendingFactorDest.OneMinusSrcAlpha,
                7 => BlendingFactorDest.DstAlpha,
                8 => BlendingFactorDest.OneMinusDstAlpha,
                9 => BlendingFactorDest.DstColor,
                10 => BlendingFactorDest.OneMinusDstColor,
                11 => BlendingFactorDest.SrcAlphaSaturate,
                _ => throw new ArgumentException(),
            };

            GL.BlendFuncSeparate(GetBlendSrc(src), GetBlendDest(src), GetBlendSrc(src), GetBlendDest(src));
            return null;
        }

        [GMLFunction("gpu_set_colorwriteenable")]
        [GMLFunction("gpu_set_colourwriteenable")]
        public static object? gpu_set_colourwriteenable(object?[] args)
        {
            bool r;
            bool g;
            bool b;
            bool a;

            if (args.Length == 4)
            {
                r = args[0].Conv<bool>();
                g = args[1].Conv<bool>();
                b = args[2].Conv<bool>();
                a = args[3].Conv<bool>();
            }
            else
            {
                var array = args[0].Conv<IList>();
                r = array[0].Conv<bool>();
                g = array[1].Conv<bool>();
                b = array[2].Conv<bool>();
                a = array[3].Conv<bool>();
            }

            GL.ColorMask(r, g, b, a);
            return null;
        }

        [GMLFunction("gpu_set_alphatestenable")]
        public static object? gpu_set_alphatestenable(object?[] args)
        {
            var enabled = args[0].Conv<bool>();
            GL.Uniform1(ShaderManager.gm_AlphaTestEnabled, enabled ? 1 : 0);
            return null;
        }

        [GMLFunction("gpu_set_alphatestref")]
        public static object? gpu_set_alphatestref(object?[] args)
        {
            var alphaRef = args[0].Conv<int>();
            GL.Uniform1(ShaderManager.gm_AlphaRefValue, alphaRef / 255f);
            return null;
        }

        [GMLFunction("gpu_set_texfilter", GMLFunctionFlags.Stub)]
        public static object? gpu_set_texfilter(object?[] args)
        {
            return null;
        }

        [GMLFunction("gpu_set_texfilter_ext", GMLFunctionFlags.Stub)]
        public static object? gpu_set_texfilter_ext(object?[] args)
        {
            return null;
        }

        // gpu_set_texrepeat
        // gpu_set_texrepeat_ext
        // gpu_set_tex_filter
        // gpu_set_tex_filter_ext
        // gpu_set_tex_repeat
        // gpu_set_tex_repeat_ext
        // gpu_set_tex_mip_filter
        // gpu_set_tex_mip_filter_ext
        // gpu_set_tex_mip_bias
        // gpu_set_tex_mip_bias_ext
        // gpu_set_tex_min_mip
        // gpu_set_tex_min_mip_ext
        // gpu_set_tex_max_mip
        // gpu_set_tex_max_mip_ext
        // gpu_set_tex_max_aniso
        // gpu_set_tex_max_aniso_ext
        // gpu_set_tex_mip_enable
        // gpu_set_tex_mip_enable_ext

        [GMLFunction("gpu_get_blendenable")]
        public static object? gpu_get_blendenable(object?[] args)
        {
            return GL.GetBoolean(GetPName.Blend); // https://registry.khronos.org/OpenGL-Refpages/gl4/html/glBlendFunc.xhtml
        }

        // gpu_get_ztestenable
        // gpu_get_zfunc
        // gpu_get_zwriteenable
        // gpu_get_fog
        // gpu_get_cullmode
        // gpu_get_blendmode
        // gpu_get_blendmode_ext
        // gpu_get_blendmode_ext_sepalpha
        // gpu_get_blendmode_src
        // gpu_get_blendmode_dest
        // gpu_get_blendmode_srcalpha
        // gpu_get_blendmode_destalpha

        [GMLFunction("gpu_get_colorwriteenable")]
        [GMLFunction("gpu_get_colourwriteenable")]
        public static object? gpu_get_colourwriteenable(object?[] args)
        {
            var bools = new bool[4];
            GL.GetBoolean(GetPName.ColorWritemask, bools); // https://registry.khronos.org/OpenGL-Refpages/gl4/html/glColorMask.xhtml
            return bools;
        }

        // gpu_get_alphatestenable
        // gpu_get_alphatestref
        // gpu_get_texfilter
        // gpu_get_texfilter_ext
        // gpu_get_texrepeat
        // gpu_get_texrepeat_ext
        // gpu_get_tex_filter
        // gpu_get_tex_filter_ext
        // gpu_get_tex_repeat
        // gpu_get_tex_repeat_ext
        // gpu_get_tex_mip_filter
        // gpu_get_tex_mip_filter_ext
        // gpu_get_tex_mip_bias
        // gpu_get_tex_mip_bias_ext
        // gpu_get_tex_min_mip
        // gpu_get_tex_min_mip_ext
        // gpu_get_tex_max_mip
        // gpu_get_tex_max_mip_ext
        // gpu_get_tex_max_aniso
        // gpu_get_tex_max_aniso_ext
        // gpu_get_tex_mip_enable
        // gpu_get_tex_mip_enable_ext
        // gpu_push_state
        // gpu_pop_state
        // gpu_get_state
        // gpu_set_state
        // draw_light_define_ambient
        // draw_light_define_direction
        // draw_light_define_point
        // draw_light_enable
        // draw_set_lighting
        // draw_light_get_ambient
        // draw_light_get
        // draw_get_lighting
    }
}

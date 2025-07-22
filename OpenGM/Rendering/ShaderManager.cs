using OpenGM.IO;
using OpenGM.Loading;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.Rendering;

public static class ShaderManager
{
    public static int DefaultProgram;

    private static Dictionary<int, int> ShaderPrograms = new();

    public static void CompileShaders()
    {
        DefaultProgram = CompileShader(File.ReadAllText("shader.vsh"), File.ReadAllText("shader.fsh"));

        foreach (var (shaderIndex, shader) in GameLoader.Shaders)
        {
            var program = CompileShader(shader.VertexSource, shader.FragmentSource);
            ShaderPrograms.Add(shaderIndex, program);
        }

        ShaderReset(); // use default
    }

    private static int CompileShader(string vertSource, string fragSource)
    {
        var vertexShader = CompileShaderHalf(ShaderType.VertexShader, vertSource);
        var fragmentShader = CompileShaderHalf(ShaderType.FragmentShader, fragSource);

        var program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
        if (code != (int)All.True)
        {
            var infoLog = GL.GetProgramInfoLog(program);
            throw new Exception($"Error while linking program.\n\n{infoLog}");
        }

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        return program;
    }

    private static int CompileShaderHalf(ShaderType type, string source)
    {
        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
        if (code != (int)All.True)
        {
            var infoLog = GL.GetShaderInfoLog(shader);
            throw new Exception($"Error while compiling {type}\n\n{infoLog}");
        }

        return shader;
    }

    public static int gm_Matrices;
    public static int gm_AlphaTestEnabled;
    public static int gm_AlphaRefValue;
    public static int gm_FogStart;
    public static int gm_RcpFogRange;
    public static int gm_PS_FogEnabled;
    public static int gm_FogColour;
    public static int gm_VS_FogEnabled;

    public static void ShaderSet(int index)
    {
        var program = ShaderPrograms[index];
        GL.UseProgram(program);
        AttachUniforms(program);
    }

    public static void ShaderReset()
    {
        GL.UseProgram(DefaultProgram);
        AttachUniforms(DefaultProgram);
    }

    private static void AttachUniforms(int program)
    {
        gm_Matrices = GL.GetUniformLocation(program, "gm_Matrices");
        gm_AlphaTestEnabled = GL.GetUniformLocation(program, "gm_AlphaTestEnabled");
        gm_AlphaRefValue = GL.GetUniformLocation(program, "gm_AlphaRefValue");
        gm_FogStart = GL.GetUniformLocation(program, "gm_FogStart");
        gm_RcpFogRange = GL.GetUniformLocation(program, "gm_RcpFogRange");
        gm_PS_FogEnabled = GL.GetUniformLocation(program, "gm_PS_FogEnabled");
        gm_FogColour = GL.GetUniformLocation(program, "gm_FogColour");
        gm_VS_FogEnabled = GL.GetUniformLocation(program, "gm_VS_FogEnabled");
    }

    public static void LoadMatrices(Camera camera)
    {
        var matrices = new Matrix4[]
        {
            camera.ViewMatrix, 
            camera.ProjectionMatrix,
            // world
            // world view
            // world view projection
        };

        unsafe
        {
            fixed (Matrix4* ptr = &matrices[0])
            {
                GL.UniformMatrix4(gm_Matrices, matrices.Length, false, (float*)ptr);
            }
        }
    }
}
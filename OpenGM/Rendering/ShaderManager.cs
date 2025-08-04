using OpenGM.IO;
using OpenGM.Loading;
using OpenGM.VirtualMachine.BuiltInFunctions;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace OpenGM.Rendering;

public static class ShaderManager
{
    public static int DefaultProgram;

    public static Dictionary<int, RuntimeShader> Shaders = new();

    public static void CompileShaders()
    {
        DefaultProgram = CompileShader(File.ReadAllText("shader.vsh"), File.ReadAllText("shader.fsh"));

        foreach (var (shaderIndex, shader) in GameLoader.Shaders)
        {
            var program = CompileShader(shader.VertexSource, shader.FragmentSource);

            var runtimeShader = new RuntimeShader();
            runtimeShader.Name = shader.Name;
            runtimeShader.ShaderIndex = shaderIndex;
            runtimeShader.ProgramID = program;
            FindUniforms(runtimeShader);

            Shaders.Add(shaderIndex, runtimeShader);
        }

        ShaderReset(); // use default
    }

    public static void FindUniforms(RuntimeShader shader)
    {
        GL.GetProgram(shader.ProgramID, GetProgramParameterName.ActiveUniforms, out var count);

        GL.GetProgram(shader.ProgramID, GetProgramParameterName.ActiveUniformMaxLength, out var maxLength);

        for (var i = 0; i < count; i++)
        {
            GL.GetActiveUniform(shader.ProgramID, i, maxLength, out _, out var size, out var type, out var name);
            shader.Uniforms.Add(name, new()
            {
                Location = i,
                Name = name,
                Size = size,
                Type = type
            });
        }
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

    public static int CurrentShaderIndex;

    public static void ShaderSet(int index)
    {
        CurrentShaderIndex = index;
        var shader = Shaders[index];
        GL.UseProgram(shader.ProgramID);
        AttachUniforms(shader.ProgramID);
        GraphicsManager.SetViewArea(GraphicsManager.ViewArea); // hack to keep gm_matrices values when changing shader
    }

    public static void ShaderReset()
    {
        CurrentShaderIndex = -1;
        GL.UseProgram(DefaultProgram);
        AttachUniforms(DefaultProgram);
        GraphicsManager.SetViewArea(GraphicsManager.ViewArea); // hack to keep gm_matrices values when changing shader
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
        var world = Matrix4.Identity;
        var worldView = world * camera.ViewMatrix;
        var worldviewProjection = worldView * camera.ProjectionMatrix;

        var matrices = new Matrix4[]
        {
            camera.ViewMatrix, 
            camera.ProjectionMatrix,
            world,
            worldView,
            worldviewProjection
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
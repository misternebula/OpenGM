using OpenTK.Graphics.OpenGL4;
using System.Diagnostics;

namespace OpenGM;
/// <summary>
/// for cpp:
/// fps = count frames in a second like here. so +1 per new frame. includes the waiting at the end of the frame.
/// fps_real = times just the frame, without waiting and without counting. also without window event handling like inputs.
/// </summary>
public static class TimingManager
{
    public static double FPS;
    public static double FPSReal;
    public static double DeltaTime;

    public static TimeSpan CPUTime, GPUTime; // for debugging

    private static int _frameCounter;
    private static Stopwatch _oneSecondStopwatch = new();
    private static Stopwatch _frameStopwatch = new();
    private static int _query = 0;

    public static void Initialize()
    {
        FPS = Entry.GameSpeed;
        _oneSecondStopwatch.Restart();
    }

    public static void BeginFrame(double dt)
    {
        DeltaTime = dt;

        if (_query == 0)
        {
            GL.CreateQueries(QueryTarget.TimeElapsed, 1, out _query);
        }
        // game_change is recursive :P so have to end the existing query
        GL.GetQuery(QueryTarget.TimeElapsed, GetQueryParam.CurrentQuery, out var currentQuery);
        if (currentQuery != 0)
        {
            GL.EndQuery(QueryTarget.TimeElapsed);
        }
        GL.BeginQuery(QueryTarget.TimeElapsed, _query);

        if (_oneSecondStopwatch.Elapsed.TotalSeconds >= 1)
        {
            FPS = _frameCounter;
            _frameCounter = 0;
            _oneSecondStopwatch.Restart();
        }
        else
        {
            _frameCounter++;
        }

        _frameStopwatch.Restart();
    }

    public static void EndFrame()
    {
        _frameStopwatch.Stop();
        FPSReal = 1 / _frameStopwatch.Elapsed.TotalSeconds;
        
        CPUTime = _frameStopwatch.Elapsed;
        GL.EndQuery(QueryTarget.TimeElapsed);
        GL.GetQueryObject(_query, GetQueryObjectParam.QueryResult, out int nanoseconds);
        GPUTime = TimeSpan.FromSeconds(nanoseconds * 1e-9);
    }
}

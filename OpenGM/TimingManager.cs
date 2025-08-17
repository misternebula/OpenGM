using System.Diagnostics;

namespace OpenGM;
public static class TimingManager
{
    public static double FPS;

    private static int _frameCounter;
    private static Stopwatch _stopwatch = new();

    public static void Initialize()
    {
        _stopwatch.Restart();
        FPS = Entry.GameSpeed;
    }

    public static void StartOfFrame()
    {
        if (_stopwatch.ElapsedMilliseconds >= 1000)
        {
            FPS = _frameCounter;
            _frameCounter = 0;
            _stopwatch.Restart();
        }
        else
        {
            _frameCounter++;
        }
    }
}

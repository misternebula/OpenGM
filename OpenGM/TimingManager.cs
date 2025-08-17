using System.Diagnostics;

namespace OpenGM;
/*
 * right now this is mimicking what html5 does to track fps.
 *
 * cpp does it totally different.
 * fps = time including the waiting period. we can just use opentk's elapsed thing that we use for deltatime. otherwise we have to edit opentk to do the stopwatch ourselves.
 * fps_real = time WITHOUT the waiting period. we can just use a stopwatch in OnUpdateFrame there. it wont include window events but it's close enough unless we edit opentk.
 * these values change PER FRAME, unlike html5 and here, making them fluctuate rapidly
 *
 * none of this actually matters because this is only for debug because of how noisy the value is.
 */
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

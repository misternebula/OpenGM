using OpenTK.Mathematics;

namespace OpenGM.IO;

public class AudioEmitter
{
    public int ID;

    // TODO: C++ sets these to -99999?? or is that just the old runner we reference?
    public Vector3 Position = Vector3.Zero;
    public Vector3 Velocity = Vector3.Zero;
    public bool Active = true;
    public float FalloffRef = 100;
    public float FalloffMax = 100000;
    public float FalloffFac = 1;
    public float Gain = 1;
    public float Pitch = 1;
    public uint ListenerMask = 1;

    public List<AudioInstance> AttachedSounds = new();

    public void Reset()
    {
        Position = Vector3.Zero;
        Velocity = Vector3.Zero;
        Active = true;
        FalloffRef = 100;
        FalloffMax = 100000;
        FalloffFac = 1;
        Gain = 1;
        Pitch = 1;
        ListenerMask = 1;
        AttachedSounds = new();
    }
}

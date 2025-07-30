using OpenTK.Mathematics;

namespace OpenGM.IO;
public class AudioListener
{
    public int ID;

    public Vector3 Position = Vector3.Zero;
    public Vector3 Velocity = Vector3.Zero;

    // why these values? no idea! c++ says so
    public Vector3 At = new Vector3(0, 0, -1);
    public Vector3 Up = new Vector3(0, 0, 1);

    public float Gain = 1;
}

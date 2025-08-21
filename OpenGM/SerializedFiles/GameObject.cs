using MemoryPack;

namespace OpenGM.SerializedFiles;

[MemoryPackable]
public partial class GameObject : CLayerElementBase
{
    public float X;
    public float Y;
    public int DefinitionID;
    public int InstanceID;
    public int CreationCodeID = -1;
    public float ScaleX;
    public float ScaleY;
    public int Color;
    public float Rotation;
    public int FrameIndex;
    public float ImageSpeed;
    public int PreCreateCodeID = -1;
}

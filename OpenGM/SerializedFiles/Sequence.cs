using MemoryPack;
using System;
using System.Collections.Generic;
using System.Text;
using UndertaleModLib.Models;

namespace OpenGM.SerializedFiles;

[MemoryPackable]
public partial class Sequence
{
    public int AssetIndex;

    public string Name = "";
    public UndertaleSequence.PlaybackType Loopmode;
    public float PlaybackSpeed;
    public AnimSpeedType PlaybackSpeedType;
    public float Length;
    public float XOrigin;
    public float YOrigin;
    public float Volume;
    public float Width;
    public float Height;
    public List<Keyframe> BroadcastMessages = new();
    public List<Keyframe> Moments = new();
    public List<Track> Tracks = new();
    public List<int> FunctionIDs = new();
}

[MemoryPackable]
public partial class Keyframe
{
    public float Key;
    public float Length;
    public bool Stretch;
    public bool Disabled;
    public List<KeyframeData> Channels = new();
}

[MemoryPackable]
public partial class KeyframeData
{
    public int Channel;

    public int SpriteIndex = -1;            // Graphic
    public int SoundIndex = -1;             // Audio
    public int PlaybackMode;                // Audio
    public int CurveIndex = -1;             // Real
    public float Value;                     // Real
    public float[] Colour = new float[4];   // Colour
    public int SequenceIndex = -1;          // Sequence
    public int ObjectIndex = -1;            // Instance
    public List<string> Events = new();     // Message
    public int Event = -1;                  // Moment

    // Text
    public string Text = "";
    public bool Wrap;
    public int AlignmentH;
    public int AlignmentV;
    public int FontIndex;
}

[MemoryPackable]
public partial class Track
{
    public string ModelName = null!;
    public string Name = null!;
    public UndertaleSequence.Track.TrackBuiltinName BuiltinName;
    public UndertaleSequence.Track.TrackTraits Traits;
    public bool IsCreationTrack;
    public List<int> Tags = new();
    public List<Track> Tracks = new();
    public List<Keyframe> Keyframes = new();
    // owned resources

    public int Interpolation;
}

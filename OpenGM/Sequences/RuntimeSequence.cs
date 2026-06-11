using OpenGM.VirtualMachine;
using System.Collections;
using UndertaleModLib.Models;

namespace OpenGM.Sequences;

public class RuntimeSequence : GMLObject
{
    public OpenGM.SerializedFiles.Sequence BackingSequence = null!;

    public RuntimeSequence(OpenGM.SerializedFiles.Sequence sequence)
    {
        BackingSequence = sequence;

        name = sequence.Name;
        loopmode = sequence.Loopmode;
        playbackSpeed = sequence.PlaybackSpeed;
        playbackSpeedType = (int)sequence.PlaybackSpeedType;
        length = sequence.Length;
        volume = sequence.Volume;
        xorigin = sequence.XOrigin;
        yorigin = sequence.YOrigin;

        momentKeyframes = new RuntimeKeyframe[sequence.Moments.Count];
        for (var i = 0; i < sequence.Moments.Count; i++)
        {
            momentKeyframes[i] = new RuntimeKeyframe(sequence.Moments[i]);
        }

        tracks = new RuntimeTrack[sequence.Tracks.Count];
        for (var i = 0; i < sequence.Tracks.Count; i++)
        {
            tracks[i] = new RuntimeTrack(sequence.Tracks[i]);
        }
    }

    public string name
    {
        get => SelfVariables["name"].Conv<string>();
        set => SelfVariables["name"] = value.Conv<string>();
    }

    public UndertaleSequence.PlaybackType loopmode
    {
        get => (UndertaleSequence.PlaybackType)SelfVariables["loopmode"].Conv<int>();
        set => SelfVariables["loopmode"] = value.Conv<int>();
    }

    public float playbackSpeed
    {
        get => SelfVariables["playbackSpeed"].Conv<float>();
        set => SelfVariables["playbackSpeed"] = value.Conv<float>();
    }

    public int playbackSpeedType
    {
        get => SelfVariables["playbackSpeedType"].Conv<int>();
        set => SelfVariables["playbackSpeedType"] = value.Conv<int>();
    }

    public float length
    {
        get => SelfVariables["length"].Conv<float>();
        set => SelfVariables["length"] = value.Conv<float>();
    }

    public float volume
    {
        get => SelfVariables["volume"].Conv<float>();
        set => SelfVariables["volume"] = value.Conv<float>();
    }

    public float xorigin
    {
        get => SelfVariables["xorigin"].Conv<float>();
        set => SelfVariables["xorigin"] = value.Conv<float>();
    }

    public float yorigin
    {
        get => SelfVariables["yorigin"].Conv<float>();
        set => SelfVariables["yorigin"] = value.Conv<float>();
    }

    // messageEventKeyframes
    // momentKeyframes

    public RuntimeKeyframe[] momentKeyframes
    {
        get => (RuntimeKeyframe[])SelfVariables["momentKeyframes"].Conv<IList>();
        set => SelfVariables["momentKeyframes"] = value.Conv<IList>();
    }

    public RuntimeTrack[] tracks
    {
        get => (RuntimeTrack[])SelfVariables["tracks"].Conv<IList>();
        set => SelfVariables["tracks"] = value.Conv<IList>();
    }
}

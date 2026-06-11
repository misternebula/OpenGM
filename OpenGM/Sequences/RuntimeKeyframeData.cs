using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using System.Collections;
using OpenGM.Loading;

namespace OpenGM.Sequences;

public class RuntimeKeyframeData : GMLObject
{
    public KeyframeData BackingKeyframeData = null!;

    public RuntimeKeyframeData(KeyframeData keyframeData)
    {
        BackingKeyframeData = keyframeData;

        channel = keyframeData.Channel;
        spriteIndex = keyframeData.SpriteIndex;
        soundIndex = keyframeData.SoundIndex;
        playbackMode = keyframeData.PlaybackMode;
        curve = keyframeData.CurveIndex;
        value = keyframeData.Value;

        if (keyframeData.SequenceIndex != -1)
        {
	        sequence = GameLoader.Sequences[keyframeData.SequenceIndex];
        }

        objectIndex = keyframeData.ObjectIndex;
        events = keyframeData.Events.ToArray();
        @event = keyframeData.Event;

        text = keyframeData.Text;
        fontIndex = keyframeData.FontIndex;
    }

    public int channel
    {
        get => SelfVariables["channel"].Conv<int>();
        set => SelfVariables["channel"] = value.Conv<int>();
    }

    public int spriteIndex
    {
        get => SelfVariables["spriteIndex"].Conv<int>();
        set => SelfVariables["spriteIndex"] = value.Conv<int>();
    }

    public int soundIndex
    {
        get => SelfVariables["soundIndex"].Conv<int>();
        set => SelfVariables["soundIndex"] = value.Conv<int>();
    }

    public int playbackMode
    {
        get => SelfVariables["playbackMode"].Conv<int>();
        set => SelfVariables["playbackMode"] = value.Conv<int>();
    }

    public int curve
    {
        get => SelfVariables["curve"].Conv<int>();
        set => SelfVariables["curve"] = value.Conv<int>();
    }

    public float value
    {
        get => SelfVariables["value"].Conv<float>();
        set => SelfVariables["value"] = value.Conv<float>();
    }

    // colour

    public RuntimeSequence sequence
    {
	    get => (RuntimeSequence)SelfVariables["objectIndex"].Conv<GMLObject>();
	    set => SelfVariables["objectIndex"] = value.Conv<GMLObject>();
    }

    public int objectIndex
    {
        get => SelfVariables["objectIndex"].Conv<int>();
        set => SelfVariables["objectIndex"] = value.Conv<int>();
    }

    public string[] events
    {
        get => (string[])SelfVariables["events"].Conv<IList>();
        set => SelfVariables["events"] = value.Conv<IList>();
    }

    public int @event
    {
        get => SelfVariables["event"].Conv<int>();
        set => SelfVariables["event"] = value.Conv<int>();
    }

    public string text
    {
        get => SelfVariables["text"].Conv<string>();
        set => SelfVariables["text"] = value.Conv<string>();
    }

    public int fontIndex
    {
        get => SelfVariables["fontIndex"].Conv<int>();
        set => SelfVariables["fontIndex"] = value.Conv<int>();
    }
}

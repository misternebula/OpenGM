using OpenGM.VirtualMachine;
using System.Collections;
using OpenGM.SerializedFiles;

namespace OpenGM.Sequences;

public class RuntimeTrack : GMLObject
{
    public Track BackingTrack = null!;

    public RuntimeTrack(Track track)
    {
        BackingTrack = track;
        name = track.Name;

        switch (track.ModelName)
        {
            case "GMAudioTrack":
                type = TrackType.Audio;
                break;
            case "GMInstanceTrack":
                type = TrackType.Instance;
                break;
            case "GMGraphicTrack":
                type = TrackType.Graphic;
                break;
            case "GMTextTrack":
                type = TrackType.Text;
                break;
            case "GMClipMaskTrack":
                type = TrackType.Clipmask;
                break;
            case "GMSequenceTrack":
                type = TrackType.Sequence;
                break;
            case "GMGroupTrack":
                type = TrackType.Group;
                break;
            case "GMRealTrack":
                type = TrackType.Real;
                break;
            case "GMColourTrack":
                type = TrackType.Colour;
                break;
            case "GMClipMask_Mask":
                type = TrackType.ClipmaskMask;
                break;
            case "GMClipMask_Subject":
                type = TrackType.ClipmaskSubject;
                break;
            default:
                throw new NotImplementedException($"{track.ModelName} not implemented.");
        }

        tracks = new RuntimeTrack[track.Tracks.Count];
        for (var i = 0; i < track.Tracks.Count; i++)
        {
            tracks[i] = new RuntimeTrack(track.Tracks[i]);
        }

        keyframes = new RuntimeKeyframe[track.Keyframes.Count];
        for (var i = 0; i < track.Keyframes.Count; i++)
        {
            keyframes[i] = new RuntimeKeyframe(track.Keyframes[i]);
        }
    }

    public TrackType type
    {
        get => (TrackType)SelfVariables["type"].Conv<int>();
        set => SelfVariables["type"] = value.Conv<int>();
    }

    public string name
    {
        get => SelfVariables["name"].Conv<string>();
        set => SelfVariables["name"] = value.Conv<string>();
    }

    public RuntimeTrack[] tracks
    {
        get => (RuntimeTrack[])SelfVariables["tracks"].Conv<IList>();
        set => SelfVariables["tracks"] = value.Conv<IList>();
    }

    /*
    public bool visible
    {
        get => SelfVariables["visible"].Conv<bool>();
        set => SelfVariables["visible"] = value.Conv<bool>();
    }*/

    public RuntimeKeyframe[] keyframes
    {
        get => (RuntimeKeyframe[])SelfVariables["keyframes"].Conv<IList>();
        set => SelfVariables["keyframes"] = value.Conv<IList>();
    }

    /*public bool enabled
    {
        get => SelfVariables["enabled"].Conv<bool>();
        set => SelfVariables["enabled"] = value.Conv<bool>();
    }*/
}

using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;

namespace OpenGM.Sequences;

public class RuntimeTrackInstance : GMLObject
{
    public float posx
    {
        get => SelfVariables["posx"].Conv<float>();
        set => SelfVariables["posx"] = value.Conv<float>();
    }

    public float posy
    {
        get => SelfVariables["posy"].Conv<float>();
        set => SelfVariables["posy"] = value.Conv<float>();
    }

    public float rotation
    {
        get => SelfVariables["rotation"].Conv<float>();
        set => SelfVariables["rotation"] = value.Conv<float>();
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

    // matrix

    // parent

    public RuntimeTrack track
    {
        get => (RuntimeTrack)SelfVariables["track"].Conv<GMLObject>();
        set => SelfVariables["track"] = value.Conv<GMLObject>();
    }

    // activeTracks

    public float scalex
    {
        get => SelfVariables["scalex"].Conv<float>();
        set => SelfVariables["scalex"] = value.Conv<float>();
    }

    public float scaley
    {
        get => SelfVariables["scaley"].Conv<float>();
        set => SelfVariables["scaley"] = value.Conv<float>();
    }

    //colouradd / coloradd

    // Same value, different access names

    public int colourmultiply
    {
        get => SelfVariables["colourmultiply"].Conv<int>();
        set => SelfVariables["colourmultiply"] = value.Conv<int>();
    }

    public int colormultiply
    {
        get => SelfVariables["colourmultiply"].Conv<int>();
        set => SelfVariables["colourmultiply"] = value.Conv<int>();
    }

    public int spriteIndex
    {
        get => SelfVariables["spriteIndex"].Conv<int>();
        set => SelfVariables["spriteIndex"] = value.Conv<int>();
    }

    public float imageindex
    {
        get => SelfVariables["imageindex"].Conv<float>();
        set => SelfVariables["imageindex"] = value.Conv<float>();
    }

    public float imagespeed
    {
        get => SelfVariables["imagespeed"].Conv<float>();
        set => SelfVariables["imagespeed"] = value.Conv<float>();
    }

    public int instanceID
    {
        get => SelfVariables["instanceID"].Conv<int>();
        set => SelfVariables["instanceID"] = value.Conv<int>();
    }

    public int sequenceID
    {
	    get => SelfVariables["sequenceID"].Conv<int>();
	    set => SelfVariables["sequenceID"] = value.Conv<int>();
    }

    public CLayerSequenceElement sequenceElement = null!;

    // sequence

    // frameSizeX
    // frameSizeY

    public float characterSpacing
    {
        get => SelfVariables["characterSpacing"].Conv<float>();
        set => SelfVariables["characterSpacing"] = value.Conv<float>();
    }

    public float lineSpacing
    {
        get => SelfVariables["lineSpacing"].Conv<float>();
        set => SelfVariables["lineSpacing"] = value.Conv<float>();
    }

    // paragraphSpacing

    public int soundIndex
    {
        get => SelfVariables["soundIndex"].Conv<int>();
        set => SelfVariables["soundIndex"] = value.Conv<int>();
    }

    public int emitterIndex
    {
        get => SelfVariables["emitterIndex"].Conv<int>();
        set => SelfVariables["emitterIndex"] = value.Conv<int>();
    }

    public float gain
    {
        get => SelfVariables["gain"].Conv<float>();
        set => SelfVariables["gain"] = value.Conv<float>();
    }

    public float pitch
    {
        get => SelfVariables["pitch"].Conv<float>();
        set => SelfVariables["pitch"] = value.Conv<float>();
    }

    // falloff
    // falloffRef
    // falloffMax
    // falloffFactor
}

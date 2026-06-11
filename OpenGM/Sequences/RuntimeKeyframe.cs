using System.Collections;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;

namespace OpenGM.Sequences;

public class RuntimeKeyframe : GMLObject
{
	public Keyframe BackingKeyframe = null!;

	public RuntimeKeyframe(Keyframe keyframe)
	{
		BackingKeyframe = keyframe;

		frame = keyframe.Key;
		length = keyframe.Length;
		stretch = keyframe.Stretch;

		channels = new RuntimeKeyframeData[keyframe.Channels.Count];
		for (var i = 0; i < keyframe.Channels.Count; i++)
		{
			channels[i] = new RuntimeKeyframeData(keyframe.Channels[i]);
		}
	}

	public float frame
	{
		get => SelfVariables["frame"].Conv<float>();
		set => SelfVariables["frame"] = value.Conv<float>();
	}

	public float length
	{
		get => SelfVariables["length"].Conv<float>();
		set => SelfVariables["length"] = value.Conv<float>();
	}

	public bool stretch
	{
		get => SelfVariables["stretch"].Conv<bool>();
		set => SelfVariables["stretch"] = value.Conv<bool>();
	}

	public RuntimeKeyframeData[] channels
	{
		get => (RuntimeKeyframeData[])SelfVariables["channels"].Conv<IList>();
		set => SelfVariables["channels"] = value.Conv<IList>();
	}
}
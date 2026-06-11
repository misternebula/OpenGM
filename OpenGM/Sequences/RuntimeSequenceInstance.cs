using System.Collections;
using OpenGM.VirtualMachine;

namespace OpenGM.Sequences;

public class RuntimeSequenceInstance : GMLObject
{
	public int sequence
	{
		get => SelfVariables["sequence"].Conv<int>();
		set => SelfVariables["sequence"] = value.Conv<int>();
	}

	public float headPosition
	{
		get => SelfVariables["headPosition"].Conv<float>();
		set => SelfVariables["headPosition"] = value.Conv<float>();
	}

	public int headDirection
	{
		get => SelfVariables["headDirection"].Conv<int>();
		set => SelfVariables["headDirection"] = value.Conv<int>();
	}

	public float speedScale
	{
		get => SelfVariables["speedScale"].Conv<float>();
		set => SelfVariables["speedScale"] = value.Conv<float>();
	}

	public bool paused
	{
		get => SelfVariables["paused"].Conv<bool>();
		set => SelfVariables["paused"] = value.Conv<bool>();
	}

	public bool finished
	{
		get => SelfVariables["finished"].Conv<bool>();
		set => SelfVariables["finished"] = value.Conv<bool>();
	}

	public int elementID
	{
		get => SelfVariables["elementID"].Conv<int>();
		set => SelfVariables["elementID"] = value.Conv<int>();
	}

	public RuntimeTrackInstance[] activeTracks
	{
		get => (RuntimeTrackInstance[])SelfVariables["activeTracks"].Conv<IList>();
		set => SelfVariables["activeTracks"] = value.Conv<IList>();
	}
}

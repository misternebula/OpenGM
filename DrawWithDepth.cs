namespace DELTARUNITYStandalone;

[Serializable]
public abstract class DrawWithDepth
{
	public uint instanceId;
	public double depth;

	public abstract void Draw();
}

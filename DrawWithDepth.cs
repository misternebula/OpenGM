namespace DELTARUNITYStandalone;

[Serializable]
public abstract class DrawWithDepth
{
	public int instanceId;
	public double depth;

	public abstract void Draw();

	public abstract void Destroy();
}

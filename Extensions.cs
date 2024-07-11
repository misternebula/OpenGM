using OpenTK.Mathematics;

namespace DELTARUNITYStandalone;
public static class Extensions
{
	public static Color4 BGRToColor(this int bgr)
	{
		var bytes = BitConverter.GetBytes(bgr);
		return BitConverter.IsLittleEndian
			? new Color4(bytes[0], bytes[1], bytes[2], 255)
			: new Color4(bytes[2], bytes[1], bytes[0], 255);
	}

	public static bool Is<T>(this Type @this) => @this.Is(typeof(T));
	public static bool Is(this Type @this, Type type) => type.IsAssignableFrom(@this);
}

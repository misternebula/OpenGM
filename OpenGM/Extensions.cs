using OpenTK.Mathematics;

namespace OpenGM;
public static class Extensions
{
	public static Color4 ABGRToCol4(this int bgr)
	{
		var bytes = BitConverter.GetBytes(bgr);
		return BitConverter.IsLittleEndian
			? new Color4(bytes[0], bytes[1], bytes[2], bytes[3])
			: new Color4(bytes[3], bytes[2], bytes[1], bytes[0]);
	}

	// better safe than sorry
	public static string FixCRLF(this string @this) => @this.Replace("\r\n", "\n");
}

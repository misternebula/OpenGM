using MemoryPack;
using OpenTK.Mathematics;
using System.Buffers;

namespace OpenGM;

public static class Extensions
{
	public static Color4 ABGRToCol4(this uint bgr)
	{
		var bytes = BitConverter.GetBytes(bgr);
		return BitConverter.IsLittleEndian
			? new Color4(bytes[0], bytes[1], bytes[2], bytes[3])
			: new Color4(bytes[3], bytes[2], bytes[1], bytes[0]);
	}

	public static Color4 ABGRToCol4(this uint bgr, double alpha)
	{
		var bytes = BitConverter.GetBytes(bgr);
		return BitConverter.IsLittleEndian
			? new Color4(bytes[0], bytes[1], bytes[2], (byte)(alpha * 255))
			: new Color4(bytes[3], bytes[2], bytes[1], (byte)(alpha * 255));
	}

	public static Color4 ABGRToCol4(this int bgr, double alpha)
	{
		alpha = Math.Clamp(alpha, 0, 1);

		var bytes = BitConverter.GetBytes(bgr);
		return BitConverter.IsLittleEndian
			? new Color4(bytes[0], bytes[1], bytes[2], (byte)(alpha * 255))
			: new Color4(bytes[3], bytes[2], bytes[1], (byte)(alpha * 255));
	}

	public static uint Col4ToABGR(this Color4 col)
	{
		var byteCol = (System.Drawing.Color)col; // kinda dumb but easy way to get the bytes back

		return (uint)(byteCol.R | (byteCol.G << 8) | (byteCol.B << 16) | (byteCol.A << 24));
	}

	// sometimes things use \r\n, sometimes they use \n
	public static string[] SplitLines(this string @this) => @this.Replace("\r\n", "\n").Split('\n');

	public static T ReadMemoryPack<T>(this BinaryReader @this)
	{
		var length = @this.ReadInt32();
		var bytes = @this.ReadBytes(length);
		return MemoryPackSerializer.Deserialize<T>(bytes)!;
	}

	public static void WriteMemoryPack<T>(this BinaryWriter @this, T value)
	{
		var bytes = MemoryPackSerializer.Serialize(value);
		@this.Write(bytes.Length);
		@this.Write(bytes);
	}

	// https://stackoverflow.com/a/1262619/17543401
	private static Random rng = new Random();  
	public static void Shuffle<T>(this IList<T> list)
	{
		int n = list.Count;
		while (n > 1)
		{
			n--;
			int k = rng.Next(n + 1);
			T value = list[k];
			list[k] = list[n];
			list[n] = value;
		}
	}
}

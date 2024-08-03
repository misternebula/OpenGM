using MemoryPack;
using OpenTK.Mathematics;
using System.Buffers;

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

	public static T Read<T>(this BinaryReader @this)
	{
		var length = @this.ReadInt32();
		var bytes = @this.ReadBytes(length);
		return MemoryPackSerializer.Deserialize<T>(bytes)!;
	}

	public static void Write<T>(this BinaryWriter @this, T value)
	{
		var bytes = MemoryPackSerializer.Serialize(value);
		@this.Write(bytes.Length);
		@this.Write(bytes);
	}
}

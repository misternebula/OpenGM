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

	public static T Read<T>(this Stream @this)
	{
		Span<byte> lengthBytes = stackalloc byte[sizeof(int)];
		@this.ReadExactly(lengthBytes);
		var length = BitConverter.ToInt32(lengthBytes);

		var resultBytes = ArrayPool<byte>.Shared.Rent(length);
		var resultSpan = resultBytes.AsSpan(0, length);
		@this.ReadExactly(resultSpan);
		var result = MemoryPackSerializer.Deserialize<T>(resultSpan)!;
		ArrayPool<byte>.Shared.Return(resultBytes);

		return result;
	}

	public static void Write<T>(this Stream @this, T value)
	{
		var bytes = MemoryPackSerializer.Serialize(value);
		@this.Write(BitConverter.GetBytes(bytes.Length));
		@this.Write(bytes);
	}
}

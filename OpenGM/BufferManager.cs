﻿using System.Text;

namespace OpenGM;
public static class BufferManager
{
	private static int _nextId;
	public static Dictionary<int, Buffer> Buffers = new();

	public static int CreateBuffer(int size, BufferType type, int alignment, byte[]? srcBuffer = null)
	{
		var buffer = new Buffer
		{
			Data = new byte[size],
			Alignment = alignment,
			Type = type
		};

		if (srcBuffer != null && srcBuffer.Length > 0)
		{
			var fillSize = CustomMath.Min(srcBuffer.Length, size);
			buffer.UsedSize = fillSize;
			for (var i = 0; i < fillSize; i++)
			{
				buffer.Data[i] = srcBuffer[i];
			}
		}

		Buffers.Add(_nextId, buffer);
		return _nextId++;
	}

	public static void SaveBuffer(int bufferIndex, string filename)
	{
		var buffer = Buffers[bufferIndex];

		// TODO : what happens if the filename is already taken? overwritten? error? find out
		var filepath = Path.Combine(Directory.GetCurrentDirectory(), filename);
		File.WriteAllBytes(filepath, buffer.Data);
	}

	public static int LoadBuffer(string filename)
	{
		var filepath = Path.Combine(Directory.GetCurrentDirectory(), filename);

		if (!File.Exists(filepath))
		{
			DebugLog.LogError($"LoadBuffer: {filepath} doesnt exist.");
			return -1;
		}

		var bytes = File.ReadAllBytes(filepath);

		var newId = CreateBuffer(bytes.Length, BufferType.Grow, 1, bytes);
		return newId;
	}

	public static object ReadBuffer(int bufferIndex, BufferDataType type)
	{
		DebugLog.Log($"ReadBuffer index:{bufferIndex} type:{type}");

		var buffer = Buffers[bufferIndex];

		// Deal with basic alignment first
		buffer.SeekPosition = (((buffer.SeekPosition + buffer.AlignmentOffset) + (buffer.Alignment - 1)) & ~(buffer.Alignment - 1)) - buffer.AlignmentOffset;

		if (buffer.SeekPosition >= buffer.Data.Length - 1 && buffer.Type == BufferType.Wrap)
		{
			// while loop incase its a stupid alignment on a small buffer
			while (buffer.SeekPosition >= buffer.Data.Length - 1)
			{
				buffer.CalculateNextAlignmentOffset();
				buffer.SeekPosition -= buffer.Data.Length - 1;
			}
		}

		// Out of space?
		if (buffer.SeekPosition >= buffer.Data.Length - 1)
		{
			return type == BufferDataType.buffer_string ? "" : -3;
		}

		object res = null!;

		// TODO : endianness might be fucked here. double check this, or make it endian agnostic

		switch (type)
		{
			case BufferDataType.buffer_bool:
				var byteVal = buffer.Data[buffer.SeekPosition++];
				if (byteVal == 1)
				{
					res = true;
				}
				else
				{
					res = false;
				}
				break;
			case BufferDataType.buffer_u8:
				res = buffer.Data[buffer.SeekPosition++];
				break;
			case BufferDataType.buffer_string:
			case BufferDataType.buffer_text:
				var strbuilder = new StringBuilder();
				string? chr;
				byte chrCode = 0;

				while (buffer.SeekPosition < buffer.UsedSize)
				{
					var v = 0;
					chr = null;
					chrCode = buffer.Data[buffer.SeekPosition++];

					if ((chrCode & 0x80) == 0)
					{
						v = chrCode;
					}
					else if ((chrCode & 0xe0) == 0xc0)
					{
						v = (chrCode & 0x1f) << 6;
						chrCode = buffer.Data[buffer.SeekPosition++];
						v |= (chrCode & 0x3f);
					}
					else if ((chrCode & 0xf0) == 0xe0)
					{
						v = (chrCode & 0x0f) << 12;
						chrCode = buffer.Data[buffer.SeekPosition++];
						v |= (chrCode & 0x3f) << 6;
						chrCode = buffer.Data[buffer.SeekPosition++];
						v |= (chrCode & 0x3f);
					}
					else
					{
						v = (chrCode & 0x07) << 18;
						chrCode = buffer.Data[buffer.SeekPosition++];
						v |= (chrCode & 0x3f) << 12;
						chrCode = buffer.Data[buffer.SeekPosition++];
						v |= (chrCode & 0x3f) << 6;
						chrCode = buffer.Data[buffer.SeekPosition++];
						v |= (chrCode & 0x3f);
						chr = new string((char)((v >> 10) + 0xD7C0), 1) + new string((char)((v & 0x3FF) | 0xDC00), 1);
					}

					if (v == 0x00)
						break;

					if (chr == null)
					{
						chr = new string((char)v, 1);
					}

					strbuilder.Append(chr);
				}

				res = strbuilder.ToString();

				break;
			case BufferDataType.buffer_s8:
				res = (sbyte)buffer.Data[buffer.SeekPosition++];
				break;
			case BufferDataType.buffer_u16:
			{
				var bytes = new[]
				{
					buffer.Data[buffer.SeekPosition++], 
					buffer.Data[buffer.SeekPosition++]
				};
				if (BitConverter.IsLittleEndian)
				{ Array.Reverse(bytes); }
				res = BitConverter.ToUInt16(bytes, 0);
				break;
			}
			case BufferDataType.buffer_s16:
			{
				var bytes = new[]
				{
					buffer.Data[buffer.SeekPosition++], 
					buffer.Data[buffer.SeekPosition++]
				};
				if (BitConverter.IsLittleEndian)
				{ Array.Reverse(bytes); }
				res = BitConverter.ToInt16(bytes, 0);
				break;
			}
			case BufferDataType.buffer_f16:
				throw new NotImplementedException();
			case BufferDataType.buffer_u32:
			{
				var bytes = new[]
				{
					buffer.Data[buffer.SeekPosition++],
					buffer.Data[buffer.SeekPosition++],
					buffer.Data[buffer.SeekPosition++],
					buffer.Data[buffer.SeekPosition++]
				};
				if (BitConverter.IsLittleEndian)
				{ Array.Reverse(bytes); }
				res = BitConverter.ToUInt32(bytes, 0);
				break;
			}
			case BufferDataType.buffer_s32:
			{
				var bytes = new[]
				{
					buffer.Data[buffer.SeekPosition++], 
					buffer.Data[buffer.SeekPosition++], 
					buffer.Data[buffer.SeekPosition++], 
					buffer.Data[buffer.SeekPosition++]
				};
				if (BitConverter.IsLittleEndian)
				{ Array.Reverse(bytes); }
				res = BitConverter.ToInt32(bytes, 0);
				break;
			}
			case BufferDataType.buffer_f32:
				throw new NotImplementedException();
			case BufferDataType.buffer_u64:
			{
				var bytes = new[]
				{
					buffer.Data[buffer.SeekPosition++],
					buffer.Data[buffer.SeekPosition++],
					buffer.Data[buffer.SeekPosition++],
					buffer.Data[buffer.SeekPosition++],
					buffer.Data[buffer.SeekPosition++],
					buffer.Data[buffer.SeekPosition++],
					buffer.Data[buffer.SeekPosition++],
					buffer.Data[buffer.SeekPosition++]
				};
				if (BitConverter.IsLittleEndian)
				{ Array.Reverse(bytes); }
				res = BitConverter.ToUInt64(bytes, 0);
				break;
			}
			case BufferDataType.buffer_f64:
				throw new NotImplementedException();
		}

		return res;
	}

	public static int BufferDataTypeToSize(BufferDataType type)
	{
		switch (type)
		{
			case BufferDataType.buffer_u8:
			case BufferDataType.buffer_s8:
			case BufferDataType.buffer_bool:
				return 1;
			case BufferDataType.buffer_u16:
			case BufferDataType.buffer_s16:
			case BufferDataType.buffer_f16:
				return 2;
			case BufferDataType.buffer_u32:
			case BufferDataType.buffer_s32:
			case BufferDataType.buffer_f32:
				return 4;
			case BufferDataType.buffer_u64:
			case BufferDataType.buffer_f64:
				return 8;
			default:
				return 0;
		}
	}

	// Based on https://github.com/YoYoGames/GameMaker-HTML5/blob/ecfb27da2508b11aeeb4d73ed743629aef785d67/scripts/yyBuffer.js#L189-L208
	public static int SimplifyAlignment(int alignment)
	{
		var newAlignment = 1;

		while (newAlignment <= 1024)
		{
			if (alignment <= newAlignment)
			{
				return newAlignment;
			}

			newAlignment <<= 1;
		}

		return 1024;
	}
}
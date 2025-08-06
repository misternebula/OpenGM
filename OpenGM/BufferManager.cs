using OpenGM.IO;
using OpenGM.Rendering;
using OpenGM.VirtualMachine;
using OpenTK.Graphics.ES11;
using System.Text;

namespace OpenGM;
public static class BufferManager
{
    private static int _nextId;
    public static Dictionary<int, Buffer> Buffers = new();

    public static BufferAsyncGroup? AsyncGroup = null;
    public static int NextAsyncId = 0;

    public static int CreateBuffer(int size, BufferType type, int alignment, byte[]? srcBuffer = null)
    {
        var buffer = new Buffer(size, type, alignment);

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
        var filepath = Path.Combine(Entry.DataWinFolder, filename);
        File.WriteAllBytes(filepath, buffer.Data);
    }

    public static int LoadBuffer(string filename)
    {
        var filepath = Path.Combine(Entry.DataWinFolder, filename);

        if (!File.Exists(filepath))
        {
            DebugLog.LogError($"LoadBuffer: {filepath} doesnt exist.");
            return -1;
        }

        var bytes = File.ReadAllBytes(filepath);

        var newId = CreateBuffer(bytes.Length, BufferType.Grow, 1, bytes);
        return newId;
    }

    // https://github.com/YoYoGames/GameMaker-HTML5/blob/12fd29600c6cd12f6663059b7a7c15fb8740080f/scripts/yyBuffer.js#L512
    public static object ReadBuffer(int bufferIndex, BufferDataType type)
    {
        DebugLog.Log($"ReadBuffer index:{bufferIndex} type:{type}");

        var buffer = Buffers[bufferIndex];

        // Deal with basic alignment first
        buffer.BufferIndex = (((buffer.BufferIndex + buffer.AlignmentOffset) + (buffer.Alignment - 1)) & ~(buffer.Alignment - 1)) - buffer.AlignmentOffset;

        if (buffer.BufferIndex >= buffer.Size && buffer.Type == BufferType.Wrap)
        {
            // while loop incase its a stupid alignment on a small buffer
            while (buffer.BufferIndex >= buffer.Size)
            {
                buffer.CalculateNextAlignmentOffset();
                buffer.BufferIndex -= buffer.Size;
            }
        }

        // Out of space?
        if (buffer.BufferIndex >= buffer.Size)
        {
            return type == BufferDataType.buffer_string ? "" : -3; // eBuffer_OutOfBounds
        }

        object res = null!;

        // TODO : endianness might be fucked here. double check this, or make it endian agnostic

        switch (type)
        {
            case BufferDataType.buffer_bool:
                var byteVal = buffer.Data[buffer.BufferIndex++];
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
                res = buffer.Data[buffer.BufferIndex++];
                break;
            case BufferDataType.buffer_string:
            case BufferDataType.buffer_text:
                var strbuilder = new StringBuilder();
                string? chr;
                byte chrCode = 0;

                while (buffer.BufferIndex < buffer.UsedSize)
                {
                    var v = 0;
                    chr = null;
                    chrCode = buffer.Data[buffer.BufferIndex++];

                    if ((chrCode & 0x80) == 0)
                    {
                        v = chrCode;
                    }
                    else if ((chrCode & 0xe0) == 0xc0)
                    {
                        v = (chrCode & 0x1f) << 6;
                        chrCode = buffer.Data[buffer.BufferIndex++];
                        v |= (chrCode & 0x3f);
                    }
                    else if ((chrCode & 0xf0) == 0xe0)
                    {
                        v = (chrCode & 0x0f) << 12;
                        chrCode = buffer.Data[buffer.BufferIndex++];
                        v |= (chrCode & 0x3f) << 6;
                        chrCode = buffer.Data[buffer.BufferIndex++];
                        v |= (chrCode & 0x3f);
                    }
                    else
                    {
                        v = (chrCode & 0x07) << 18;
                        chrCode = buffer.Data[buffer.BufferIndex++];
                        v |= (chrCode & 0x3f) << 12;
                        chrCode = buffer.Data[buffer.BufferIndex++];
                        v |= (chrCode & 0x3f) << 6;
                        chrCode = buffer.Data[buffer.BufferIndex++];
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
                res = (sbyte)buffer.Data[buffer.BufferIndex++];
                break;
            case BufferDataType.buffer_u16:
            {
                var bytes = new[]
                {
                    buffer.Data[buffer.BufferIndex++], 
                    buffer.Data[buffer.BufferIndex++]
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
                    buffer.Data[buffer.BufferIndex++], 
                    buffer.Data[buffer.BufferIndex++]
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
                    buffer.Data[buffer.BufferIndex++],
                    buffer.Data[buffer.BufferIndex++],
                    buffer.Data[buffer.BufferIndex++],
                    buffer.Data[buffer.BufferIndex++]
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
                    buffer.Data[buffer.BufferIndex++], 
                    buffer.Data[buffer.BufferIndex++], 
                    buffer.Data[buffer.BufferIndex++], 
                    buffer.Data[buffer.BufferIndex++]
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
                    buffer.Data[buffer.BufferIndex++],
                    buffer.Data[buffer.BufferIndex++],
                    buffer.Data[buffer.BufferIndex++],
                    buffer.Data[buffer.BufferIndex++],
                    buffer.Data[buffer.BufferIndex++],
                    buffer.Data[buffer.BufferIndex++],
                    buffer.Data[buffer.BufferIndex++],
                    buffer.Data[buffer.BufferIndex++]
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

    public static object? WriteBuffer(int bufferIndex, object value, BufferDataType type)
    {
        DebugLog.Log($"WriteBuffer index:{bufferIndex} value:{value.GetType().Name} type:{type}");

        var buffer = Buffers[bufferIndex];

        // Deal with basic alignment first
        buffer.BufferIndex = (((buffer.BufferIndex + buffer.AlignmentOffset) + (buffer.Alignment - 1)) & ~(buffer.Alignment - 1)) - buffer.AlignmentOffset;

        if (buffer.BufferIndex >= buffer.Size && buffer.Type == BufferType.Wrap)
        {
            // while loop incase its a stupid alignment on a small buffer
            while (buffer.BufferIndex >= buffer.Size)
            {
                buffer.CalculateNextAlignmentOffset();
                buffer.BufferIndex -= buffer.Size;
            }
        }

        // Out of space?
        if (buffer.BufferIndex >= buffer.Size)
        {
            return type == BufferDataType.buffer_string ? "" : -3; // eBuffer_OutOfBounds
        }

        var sizeNeeded = BufferDataTypeToSize(type);
        // TODO : do that unicode -> utf8 stuff
        if (type is BufferDataType.buffer_string)
        {
            sizeNeeded = value.Conv<string>().Length;
        }

        if (type is BufferDataType.buffer_text)
        {
            sizeNeeded = value.Conv<string>().Length + 1;
        }

        if (buffer.BufferIndex + sizeNeeded > buffer.Size) {
            // Resize buffer...
            if (buffer.Type is BufferType.Grow) {
                var newSize = buffer.Size;

                if (newSize < 4)
                {
                    newSize = 4;
                }

                while (buffer.BufferIndex + sizeNeeded > newSize) {
                    newSize <<= 1; 
                }

                buffer.Resize(newSize);
            } else {
                if (buffer.Type is not BufferType.Wrap) {
                    return type == BufferDataType.buffer_string ? "" : -2;      // out of space
                }
            }
        }

        // TODO : endianness might be fucked here. double check this, or make it endian agnostic

        switch (type)
        {
            case BufferDataType.buffer_bool:
                value = value.Conv<bool>() ? 1 : 0;
                goto case BufferDataType.buffer_u8;
            case BufferDataType.buffer_u8:
                buffer.Data[buffer.BufferIndex++] = Convert.ToByte(value);
                break;  
            case BufferDataType.buffer_string:
            case BufferDataType.buffer_text:
                var str = value.Conv<string>();
                foreach (var chr in str)
                {
                    buffer.Data[buffer.BufferIndex++] = (byte)chr;
                }
                // "text" mode doesn't add a NULL at the end.
                if (type is BufferDataType.buffer_text)
                {
                    buffer.Data[buffer.BufferIndex++] = 0;
                }
                break;
            case BufferDataType.buffer_s8:
                // casting *should* account for sign and wraparound here
                buffer.Data[buffer.BufferIndex++] = Convert.ToByte(value);
                break;
            case BufferDataType.buffer_u16:
            {
                var bytes = BitConverter.GetBytes(Convert.ToUInt16(value));
                if (BitConverter.IsLittleEndian)
                { Array.Reverse(bytes); }
                for (var i = 0; i < bytes.Length; i++)
                {
                    buffer.Data[buffer.BufferIndex++] = bytes[i];
                }
                break;
            }
            case BufferDataType.buffer_s16:
            {
                var bytes = BitConverter.GetBytes(Convert.ToInt16(value));
                if (BitConverter.IsLittleEndian)
                { Array.Reverse(bytes); }
                for (var i = 0; i < bytes.Length; i++)
                {
                    buffer.Data[buffer.BufferIndex++] = bytes[i];
                }
                break;
            }
            case BufferDataType.buffer_f16:
                throw new NotImplementedException();
            case BufferDataType.buffer_u32:
            {
                var bytes = BitConverter.GetBytes(Convert.ToUInt32(value));
                if (BitConverter.IsLittleEndian)
                { Array.Reverse(bytes); }
                for (var i = 0; i < bytes.Length; i++)
                {
                    buffer.Data[buffer.BufferIndex++] = bytes[i];
                }
                break;
            }
            case BufferDataType.buffer_s32:
            {
                var bytes = BitConverter.GetBytes(Convert.ToInt32(value));
                if (BitConverter.IsLittleEndian)
                { Array.Reverse(bytes); }
                for (var i = 0; i < bytes.Length; i++)
                {
                    buffer.Data[buffer.BufferIndex++] = bytes[i];
                }
                break;
            }
            case BufferDataType.buffer_f32:
                throw new NotImplementedException();
            case BufferDataType.buffer_u64:
            {
                var bytes = BitConverter.GetBytes(Convert.ToUInt64(value));
                if (BitConverter.IsLittleEndian)
                { Array.Reverse(bytes); }
                for (var i = 0; i < bytes.Length; i++)
                {
                    buffer.Data[buffer.BufferIndex++] = bytes[i];
                }
                break;
            }
            case BufferDataType.buffer_f64:
                throw new NotImplementedException();
        }

        return null;
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

    public static void BufferSetSurface(int bufferId, int surfaceId, int offset)
    {
        // Copy buffer data to surface

        var buffer = Buffers[bufferId];

        if (buffer == null || !SurfaceManager.surface_exists(surfaceId))
        {
            return;
        }

        var w = SurfaceManager.GetSurfaceWidth(surfaceId);
        var h = SurfaceManager.GetSurfaceHeight(surfaceId);

        if (offset + (w * h * 4) > buffer.Data.Length)
        {
            return;
        }

        SurfaceManager.BindSurfaceTexture(surfaceId);
        // TODO : account for offset
        GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, w, h, 0, PixelFormat.Rgba, PixelType.UnsignedByte, buffer.Data);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        GraphicsManager.CheckError();
    }

    public static void BufferGetSurface(int bufferId, int surfaceId, int offset)
    {
        // Copy surface data to buffer

        var buffer = Buffers[bufferId];

        if (buffer == null || !SurfaceManager.surface_exists(surfaceId))
        {
            return;
        }

        var pixels = SurfaceManager.ReadPixels(surfaceId, 0, 0, SurfaceManager.GetSurfaceWidth(surfaceId), SurfaceManager.GetSurfaceHeight(surfaceId));

        if (buffer.Type == BufferType.Grow && offset + pixels.Length > buffer.Size)
        {
            buffer.Resize(offset + pixels.Length);
        }

        for (var i = 0; i < pixels.Length; i++)
        {
            buffer.Poke(BufferDataType.buffer_u8, i, pixels[i]);
        }

        pixels = null; // probably not needed
    }
}

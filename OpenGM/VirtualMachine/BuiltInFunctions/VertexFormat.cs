using OpenTK.Graphics.OpenGL4;

namespace OpenGM.VirtualMachine.BuiltInFunctions;
public class VertexFormat
{
    private uint _formatBit = 1;

    public List<VertexElement> Format = new();
    public uint BitMask;
    public uint ByteSize;

    public void AddPosition()
    {
        Add(VertexType.FLOAT2, VertexUsage.POSITION, _formatBit);
        _formatBit <<= 1;
    }

    public void AddColor()
    {
        Add(VertexType.COLOR, VertexUsage.COLOR, _formatBit);
        _formatBit <<= 1;
    }

    public void AddNormal()
    {
        Add(VertexType.FLOAT3, VertexUsage.NORMAL, _formatBit);
        _formatBit <<= 1;
    }

    public void Add(VertexType type, VertexUsage usage, uint formatBit)
    {
        BitMask |= formatBit;

        var element = new VertexElement()
        {
            Type = type,
            Usage = usage,
            Bit = _formatBit
        };

        switch (type)
        {
            case VertexType.COLOR:
                ByteSize += 4;
                element.GLType = PixelType.UnsignedByte;
                element.GLComponents = 4;
                element.Normalized = true;
                break;
            case VertexType.UBYTE4:
                ByteSize += 4;
                element.GLType = PixelType.UnsignedByte;
                element.GLComponents = 4;
                element.Normalized = false;
                break;
            case VertexType.FLOAT1:
                ByteSize += 4;
                element.GLType = PixelType.Float;
                element.GLComponents = 1;
                element.Normalized = false;
                break;
            case VertexType.FLOAT2:
                ByteSize += 8;
                element.GLType = PixelType.Float;
                element.GLComponents = 2;
                element.Normalized = false;
                break;
            case VertexType.FLOAT3:
                ByteSize += 12;
                element.GLType = PixelType.Float;
                element.GLComponents = 3;
                element.Normalized = false;
                break;
            case VertexType.FLOAT4:
                ByteSize += 16;
                element.GLType = PixelType.Float;
                element.GLComponents = 4;
                element.Normalized = false;
                break;
        }

        Format.Add(element);
    }
}

public class VertexElement
{
    public int Offset;
    public VertexType Type;
    // c++ padding
    public VertexUsage Usage;
    public uint Bit;

    // not in c++?
    public uint GLComponents;
    public bool Normalized;
    public PixelType GLType; // TODO: is this the right enum??
}

public enum VertexType
{
    FLOAT1,
    FLOAT2,
    FLOAT3,
    FLOAT4,
    COLOR,
    UBYTE4
}

public enum VertexUsage
{
    POSITION,
    COLOR,
    NORMAL,
    TEXCOORD,
    BLENDWEIGHT,
    BLENDINDICES,
    PSIZE,
    TANGENT,
    BINORMAL,
    TESSFACTOR,
    POSITIONT,
    FOG,
    DEPTH
}

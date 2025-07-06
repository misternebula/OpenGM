namespace OpenGM;
public class Buffer
{
	public byte[] Data = null!;
	public BufferType Type;
	public int Alignment;
	public int AlignmentOffset;
	public int SeekPosition;
	public int UsedSize = 0;

	public void CalculateNextAlignmentOffset()
	{
		AlignmentOffset = (AlignmentOffset + Data.Length - 1) % Alignment;
	}

	public void UpdateUsedSize(int size = -1, bool reset = false)
	{
		var newSize = size;
		if (newSize == -1)
		{
			newSize = SeekPosition;
		}

		if (reset)
		{
			UsedSize = newSize;
		}
		else
		{
			UsedSize = CustomMath.Max(UsedSize, newSize);
			UsedSize = CustomMath.Max(UsedSize, Data.Length - 1);
		}
	}

	// Based on https://github.com/YoYoGames/GameMaker-HTML5/blob/95d8f5643efbdb74ffce2bfae4b82bc3426b2b54/scripts/yyBuffer.js#L1807-L1830
	public int Seek(BufferSeek basePosition, int offset)
	{
		// Actual wrapping of position is handled when reading from the data.
		// Wrapping does not apply when going backwards, only forwards.

		switch (basePosition)
		{
			case BufferSeek.SeekStart:
				if (offset < 0)
				{
					offset = 0;
				}

				SeekPosition = offset;
				break;
			case BufferSeek.SeekRelative:
				SeekPosition += offset;

				if (SeekPosition < 0)
				{
					SeekPosition = 0;
				}

				break;
			case BufferSeek.SeekEnd:
				// Shouldnt all this be the other way around?
				// The other two cases check for negative seek positions, but this one doesnt.
				// A positive offset moves the seek position BACKWARDS now? wtf gamemaker

				SeekPosition = Data.Length - 1 - offset;
				if (SeekPosition > Data.Length - 1)
				{
					SeekPosition = Data.Length - 1;
				}

				break;
		}

		return SeekPosition;
	}
}

public enum BufferType
{
	Fixed,
	Grow,
	Wrap,
	Fast
}

public enum BufferSeek
{
	SeekStart,
	SeekRelative,
	SeekEnd
}

public enum BufferDataType
{
	None,
	buffer_u8,
	buffer_s8,
	buffer_u16,
	buffer_s16,
	buffer_u32,
	buffer_s32,
	buffer_f16,
	buffer_f32,
	buffer_f64,
	buffer_bool,
	buffer_string,
	buffer_u64,
	buffer_text
}

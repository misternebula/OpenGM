using System;
using FFmpeg.AutoGen;

namespace MotionTK
{
	public unsafe class VideoPacket : Packet
	{

		public IntPtr RgbaBuffer { get; private set; }
		public TimeSpan Timestamp { get; private set; }

		public VideoPacket(byte* rgbaBuffer, TimeSpan timestamp)
		{
			Timestamp = timestamp;
			RgbaBuffer = new IntPtr(rgbaBuffer);
		}

		public override void Dispose()
		{
			if (RgbaBuffer == IntPtr.Zero)
				return;
			ffmpeg.av_free(RgbaBuffer.ToPointer());

			RgbaBuffer = IntPtr.Zero;
			Timestamp = TimeSpan.Zero;
		}
	}
}
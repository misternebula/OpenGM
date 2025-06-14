using System.Net.Sockets;

namespace MotionTK
{
	public unsafe class AudioPacket : Packet
	{
		public readonly short[] SampleBuffer;
		public readonly int TotalSampleCount;

		public AudioPacket(byte* sampleBuffer, int sampleCount, int channelCount)
		{
			// I have no idea why the * 2 is necessary, but without it only half of each packet is played
			TotalSampleCount = sampleCount * channelCount * 2;
			SampleBuffer = new short[TotalSampleCount];

			// copy buffer
			for (int i = 0; i < TotalSampleCount; i++)
			{
				SampleBuffer[i] = ((short*)sampleBuffer)[i];
			}
		}
	}
}
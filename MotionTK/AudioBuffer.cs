using System.Collections.Generic;
using System.Linq;
using OpenTK.Audio.OpenAL;

namespace MotionTK
{
	public class AudioBuffer
	{
		private static readonly Dictionary<int, AudioBuffer> Buffers = new Dictionary<int, AudioBuffer>();
		private static readonly List<AudioBuffer> Free = new List<AudioBuffer>();

		public readonly int Id;
		public readonly int Handle;

		public short[] Data;
		public ALFormat Format = ALFormat.Stereo16;
		public int SampleRate;

		private AudioBuffer(int size, ALFormat format, int sampleRate)
		{
			Id = Buffers.Count;
			Handle = AL.GenBuffer();

			Init(size, format, sampleRate);
			Bind();

			Buffers.Add(Handle, this);
			//Console.WriteLine("Allocated buffer " + Id);
		}

		public void Init(int size, ALFormat format, int sampleRate)
		{
			Format = format;
			SampleRate = sampleRate;
			if (!Resize(size))
				Clear();
		}

		public void Clear()
		{
			for (int i = 0; i < Data.Length; i++)
			{
				Data[0] = 0;
			}
		}

		public bool Resize(int size)
		{
			if (Data != null && Data.Length == size)
				return false;

			Data = new short[size];
			return true;
		}

		public void Bind()
		{
			AL.BufferData(Handle, Format, Data, 44100);
		}

		public void MakeAvailable()
		{
			lock (Free)
				Free.Add(this);
		}

		public void Dispose()
		{
			AL.DeleteBuffer(Handle);
			foreach (var pair in Buffers)
			{
				if (pair.Value != this)
					continue;
				Buffers.Remove(pair.Key);
				break;
			}
		}

		~AudioBuffer() => Dispose();

		public static AudioBuffer ByHandle(int handle)
		{
			return Buffers[handle];
		}

		public static AudioBuffer Get(int size, ALFormat format, int sampleRate)
		{
			AudioBuffer buffer;
			lock (Free)
			{
				buffer = Free.FirstOrDefault(b => b.Data.Length == size);
				if (buffer != null)
				{
					Free.Remove(buffer);
					//Console.WriteLine("Reusing buffer " + buffer.Id);
				}
				else if (Free.Count > 0)
				{
					buffer = Free[0];
					//Console.WriteLine("Reusing buffer " + buffer.Id);
				}
				else
					buffer = new AudioBuffer(size, format, sampleRate);
			}
			buffer.Init(size, format, sampleRate);
			return buffer;
		}
	}
}
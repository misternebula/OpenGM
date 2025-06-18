using System.Linq;
using OpenTK.Audio.OpenAL;

namespace MotionTK
{
	public class AudioPlayback : Playback<AudioPacket>
	{

		public int SourceHandle { get; protected set; }

		protected int _channelCount;
		protected int _sampleRate;

		internal AudioPlayback(DataSource dataSource) : base(dataSource)
		{
			SourceHandle = AL.GenSource();
		}

		public override void Dispose()
		{
			if (SourceHandle == -1)
				return;

			// free queued buffers
			AL.SourceStop(SourceHandle);
			AL.GetSource(SourceHandle, ALGetSourcei.BuffersQueued, out int queued);
			if (queued > 0)
			{
				var buffers = new int[queued];
				AL.SourceUnqueueBuffers(SourceHandle, queued, buffers);
				foreach (int b in buffers)
				{
					AL.DeleteBuffer(b);
				}
			}
			AL.DeleteSource(SourceHandle);
			SourceHandle = -1;

			base.Dispose();
		}

		internal override void SourceReloaded()
		{
			if (!DataSource.HasAudio)
				return;
			_channelCount = DataSource.AudioChannelCount;
			_sampleRate = DataSource.AudioSampleRate;
			StateChanged(DataSource.State, DataSource.State);
		}

		internal override void StateChanged(PlayState oldState, PlayState newState)
		{
			switch (newState)
			{
				case PlayState.Playing when DataSource.HasAudio:
					AL.SourcePlay(SourceHandle);
					break;
				case PlayState.Paused when DataSource.HasAudio:
					AL.SourcePause(SourceHandle);
					break;
				case PlayState.Stopped:
					AL.SourceStop(SourceHandle);
					while (PacketQueue.Any())
					{
						PacketQueue.Take();
					}
					break;
			}
		}

		internal void Update()
		{
			const int maxQueue = 40;

			AL.GetSource(SourceHandle, ALGetSourcei.SourceState, out int state);
			AL.GetSource(SourceHandle, ALGetSourcei.BuffersQueued, out int queued);
			AL.GetSource(SourceHandle, ALGetSourcei.BuffersProcessed, out int processed);

			// unqueue processed buffers
			if (processed > 0)
			{
				var processedBuffers = new int[processed];
				AL.SourceUnqueueBuffers(SourceHandle, processed, processedBuffers);
				foreach (int bufferHandle in processedBuffers)
				{
					AudioBuffer.ByHandle(bufferHandle).MakeAvailable();
				}
			}

			// queue new buffers
			while (queued++ < maxQueue && PacketQueue.TryTake(out var packet))
			{
				var buffer = AudioBuffer.Get(packet.TotalSampleCount, DataSource.AudioChannelCount == 2 ? ALFormat.Stereo16 : ALFormat.Mono16, _sampleRate);
				buffer.Data = packet.SampleBuffer;
				buffer.Bind();

				AL.SourceQueueBuffer(SourceHandle, buffer.Handle);
			}

			// restart if necessary
			if ((ALSourceState)state != ALSourceState.Playing && DataSource.State == PlayState.Playing)
			{
				//Console.WriteLine("Playing source (" + (ALSourceState)state + ", " + processed + "/" + queued + ")");
				//AL.SourcePause(_sourceHandle);
				AL.SourcePlay(SourceHandle);
			}
		}
	}
}
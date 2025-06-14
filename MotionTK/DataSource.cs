using System;
using System.Drawing;
using System.Threading;
using FFmpeg.AutoGen;

namespace MotionTK
{
	public unsafe class DataSource : IDisposable
	{
		private const int MaxAudioSamples = 192000;
		private const int MaxQueuedPackets = 10;

		#region Fields

		private int _videoStreamId = -1;
		private Size _videoSize = Size.Empty;
		private AVCodecContext* _videoContext;
		private AVCodec* _videoCodec;
		private AVFrame* _videoRawFrame;
		private AVFrame* _videoRgbaFrame;
		private byte* _videoRawBuffer;
		private byte* _videoRgbaBuffer;
		private SwsContext* _videoSwContext;

		private int _audioStreamId = -1;
		private AVCodecContext* _audioContext;
		private AVCodec* _audioCodec;
		private AVFrame* _audioRawBuffer;
		private byte* _audioPcmBuffer;
		private SwrContext* _audioSwContext;

		private AVFormatContext* _formatContext;
		private Thread _decodeThread;
		private bool _runDecodeThread;
		private bool _playingToEof;
		private TimeSpan _playingOffset = TimeSpan.Zero;
		private DateTime _lastUpdate;

		#endregion

		#region Properties

		public bool HasVideo => _videoStreamId != -1;
		public bool HasAudio => _audioStreamId != -1;
		public VideoPlayback VideoPlayback { get; private set; }
		public AudioPlayback AudioPlayback { get; private set; }

		public Size VideoSize => new Size(_videoSize.Width, _videoSize.Height);
		public PlayState State { get; private set; } = PlayState.Stopped;
		public int AudioChannelCount { get; private set; } = -1;
		public int AudioSampleRate => HasAudio ? _audioContext->sample_rate : -1;
		public TimeSpan FileLength { get; private set; } = TimeSpan.Zero;
		public bool IsEndOfFileReached { get; private set; }

		public bool IsFull => (!HasVideo || VideoPlayback.QueuedPackets >= MaxQueuedPackets)
						   && (!HasAudio || AudioPlayback.QueuedPackets >= MaxQueuedPackets);

		public TimeSpan PlayingOffset
		{
			get => _playingOffset;
			set
			{
				if (!HasVideo && !HasAudio)
					return;
				bool startDecode = _runDecodeThread;
				bool startPlaying = State == PlayState.Playing;
				StopDecodeThread();
				_playingOffset = value;
				_playingToEof = false;

				if (State != PlayState.Stopped)
				{
					IsEndOfFileReached = true;
					NotifyStateChanged(PlayState.Stopped);
					State = PlayState.Stopped;
					IsEndOfFileReached = false;
				}

				if (HasVideo)
				{
					var timebase = _formatContext->streams[_videoStreamId]->time_base;
					float ftb = (float)timebase.den / timebase.num;
					long pos = (long)(PlayingOffset.TotalSeconds * ftb);
					ffmpeg.av_seek_frame(_formatContext, _videoStreamId, pos, ffmpeg.AVSEEK_FLAG_ANY);
					ffmpeg.avcodec_flush_buffers(_videoContext);
					VideoPlayback.Flush();
				}

				if (HasAudio)
				{
					var timebase = _formatContext->streams[_audioStreamId]->time_base;
					float ftb = (float)timebase.den / timebase.num;
					long pos = (long)(PlayingOffset.TotalSeconds * ftb);
					ffmpeg.av_seek_frame(_formatContext, _audioStreamId, pos, ffmpeg.AVSEEK_FLAG_ANY);
					ffmpeg.avcodec_flush_buffers(_audioContext);
					AudioPlayback.Flush();
				}

				if (startDecode)
					StartDecodeThread();
				if (startPlaying)
					Play();
			}
		}

		public TimeSpan FrameDuration
		{
			get
			{
				if (!HasVideo)
					return TimeSpan.Zero;

				var a = _formatContext->streams[_videoStreamId]->avg_frame_rate;
				if (a.num != 0 || a.den != 0)
					return TimeSpan.FromTicks(TimeSpan.TicksPerSecond * a.den / a.num);

				var r = _formatContext->streams[_videoStreamId]->r_frame_rate;
				if (r.num != 0 || r.den != 0)
					return TimeSpan.FromTicks(TimeSpan.TicksPerSecond * r.den / r.num);

				double tickCount = _formatContext->streams[_videoStreamId]->duration;
				double frameCount = _formatContext->streams[_videoStreamId]->nb_frames;
				double ticksPerFrame = tickCount / frameCount;
				double tickDuration = (double)_formatContext->streams[_videoStreamId]->time_base.num / _formatContext->streams[_videoStreamId]->time_base.den;

				double frameDuration = ticksPerFrame * tickDuration;
				return TimeSpan.FromTicks((long)(frameDuration * TimeSpan.TicksPerSecond));
			}
		}

		#endregion

		#region Setup

		public DataSource(string path, bool enableVideo = true, bool enableAudio = true)
		{
			//av_register_all();
			AVFormatContext* formatContext;
			if (ffmpeg.avformat_open_input(&formatContext, path, null, null) != 0)
			{
				Console.WriteLine("Motion: Failed to open file: " + path);
				return;
			}
			_formatContext = formatContext;
			if (ffmpeg.avformat_find_stream_info(formatContext, null) != 0)
			{
				Console.WriteLine("Motion: Failed to find stream information: " + path);
				return;
			}
			for (int i = 0; i < formatContext->nb_streams; i++)
			{
				switch (formatContext->streams[i]->codecpar->codec_type)
				{
					case AVMediaType.AVMEDIA_TYPE_VIDEO:
						if (_videoStreamId == -1 && enableVideo)
							_videoStreamId = i;
						break;
					case AVMediaType.AVMEDIA_TYPE_AUDIO:
						if (_audioStreamId == -1 && enableAudio)
							_audioStreamId = i;
						break;
				}
			}
			InitVideo();
			InitAudio();
			if (_formatContext->duration != ffmpeg.AV_NOPTS_VALUE)
				FileLength = TimeSpan.FromTicks((long)(_formatContext->duration / 1000d * TimeSpan.TicksPerMillisecond));
			if (HasVideo || HasAudio)
			{
				StartDecodeThread();
				VideoPlayback?.SourceReloaded();
				AudioPlayback?.SourceReloaded();
			}
			else
			{
				Console.WriteLine("Motion: Failed to load audio or video");
				Dispose();
			}
		}

		private void InitVideo()
		{
			if (!HasVideo)
				return;

			_videoContext = _formatContext->streams[_videoStreamId]->codec;
			if (_videoContext == null)
			{
				Console.WriteLine("Motion: Failed to get video codec context");
				_videoStreamId = -1;
				return;
			}

			_videoCodec = ffmpeg.avcodec_find_decoder(_videoContext->codec_id);
			if (_videoCodec == null)
			{
				Console.WriteLine("Motion: Failed to find video codec");
				_videoStreamId = -1;
				return;
			}

			if (ffmpeg.avcodec_open2(_videoContext, _videoCodec, null) != 0)
			{
				Console.WriteLine("Motion: Failed to load video codec");
				_videoStreamId = -1;
				return;
			}

			_videoSize = new Size(_videoContext->width, _videoContext->height);
			_videoRawFrame = CreateVideoFrame(_videoContext->pix_fmt, _videoSize.Width, _videoSize.Height, ref _videoRawBuffer);
			_videoRgbaFrame = CreateVideoFrame(AVPixelFormat.AV_PIX_FMT_RGBA, _videoSize.Width, _videoSize.Height, ref _videoRgbaBuffer);

			if (_videoRawFrame == null || _videoRgbaFrame == null)
			{
				Console.WriteLine("Motion: Failed to create video frames");
				_videoStreamId = -1;
				return;
			}

			int swapmode = ffmpeg.SWS_FAST_BILINEAR;
			if (_videoSize.Width * _videoSize.Height <= 500000 && _videoSize.Width % 8 != 0)
				swapmode |= ffmpeg.SWS_ACCURATE_RND;
			_videoSwContext = ffmpeg.sws_getCachedContext(null, _videoSize.Width, _videoSize.Height, _videoContext->pix_fmt, _videoSize.Width, _videoSize.Height, AVPixelFormat.AV_PIX_FMT_RGBA, swapmode, null, null, null);

			if (_videoSwContext == null)
			{
				Console.WriteLine("Motion: Failed to create video scaling context");
				_videoStreamId = -1;
			}

			VideoPlayback = new VideoPlayback(this);
		}

		private void InitAudio()
		{
			if (!HasAudio)
				return;

			_audioContext = _formatContext->streams[_audioStreamId]->codec;
			if (_audioContext == null)
			{
				Console.WriteLine("Motion: Failed to get audio codec context");
				_audioStreamId = -1;
				return;
			}

			_audioCodec = ffmpeg.avcodec_find_decoder(_audioContext->codec_id);
			if (_audioCodec == null)
			{
				Console.WriteLine("Motion: Failed to find audio codec");
				_audioStreamId = -1;
				return;
			}

			if (ffmpeg.avcodec_open2(_audioContext, _audioCodec, null) != 0)
			{
				Console.WriteLine("Motion: Failed to load audio codec");
				_audioStreamId = -1;
				return;
			}

			_audioRawBuffer = ffmpeg.av_frame_alloc();
			if (_audioRawBuffer == null)
			{
				Console.WriteLine("Motion: Failed to allocate audio buffer");
				_audioStreamId = -1;
				return;
			}

			var audioPcmBuffer = _audioPcmBuffer;
			if (ffmpeg.av_samples_alloc(&audioPcmBuffer, null, _audioContext->channels, ffmpeg.av_samples_get_buffer_size(null, _audioContext->channels, MaxAudioSamples, AVSampleFormat.AV_SAMPLE_FMT_S16, 0), AVSampleFormat.AV_SAMPLE_FMT_S16, 0) < 0)
			{
				Console.WriteLine("Motion: Failed to create audio samples buffer");
				_audioStreamId = -1;
				return;
			}
			_audioPcmBuffer = audioPcmBuffer;

			ffmpeg.av_frame_unref(_audioRawBuffer);
			_audioSwContext = ffmpeg.swr_alloc();
			if (_videoSwContext == null)
			{
				Console.WriteLine("Motion: Failed to create audio resampling context");
				_audioStreamId = -1;
				return;
			}

			ulong inchanlayout = _audioContext->channel_layout;
			if (inchanlayout == 0)
				inchanlayout = (ulong)ffmpeg.av_get_default_channel_layout(_audioContext->channels);
			ulong outchanlayout = inchanlayout;
			if (outchanlayout != ffmpeg.AV_CH_LAYOUT_MONO)
				outchanlayout = ffmpeg.AV_CH_LAYOUT_STEREO;
			ffmpeg.av_opt_set_int(_audioSwContext, "in_channel_layout", (long)inchanlayout, 0);
			ffmpeg.av_opt_set_int(_audioSwContext, "out_channel_layout", (long)outchanlayout, 0);
			ffmpeg.av_opt_set_int(_audioSwContext, "in_sample_rate", _audioContext->sample_rate, 0);
			ffmpeg.av_opt_set_int(_audioSwContext, "out_sample_rate", _audioContext->sample_rate, 0);
			ffmpeg.av_opt_set_sample_fmt(_audioSwContext, "in_sample_fmt", _audioContext->sample_fmt, 0);
			ffmpeg.av_opt_set_sample_fmt(_audioSwContext, "out_sample_fmt", AVSampleFormat.AV_SAMPLE_FMT_S16, 0);
			ffmpeg.swr_init(_audioSwContext);
			AudioChannelCount = ffmpeg.av_get_channel_layout_nb_channels(outchanlayout);

			AudioPlayback = new AudioPlayback(this);
		}

		~DataSource() => Dispose();

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Stop();
			StopDecodeThread();
			_playingOffset = TimeSpan.Zero;
			FileLength = TimeSpan.Zero;
			_videoSize = Size.Empty;
			_videoStreamId = -1;
			_audioStreamId = -1;
			AudioChannelCount = -1;

			if (_videoContext != null)
			{
				ffmpeg.avcodec_close(_videoContext);
				_videoContext = null;
			}
			_videoCodec = null;
			if (_audioContext != null)
			{
				ffmpeg.avcodec_close(_audioContext);
				_audioContext = null;
			}
			_audioCodec = null;
			if (_videoRawFrame != null)
			{
				DestroyVideoFrame(ref _videoRawFrame, ref _videoRawBuffer);
				_videoRawFrame = null;
			}
			if (_videoRgbaFrame != null)
			{
				DestroyVideoFrame(ref _videoRgbaFrame, ref _videoRgbaBuffer);
				_videoRgbaFrame = null;
			}
			if (_audioRawBuffer != null)
			{
				var audioRawBuffer = _audioRawBuffer;
				ffmpeg.av_frame_free(&audioRawBuffer);
				_audioRawBuffer = null;
			}
			if (_audioPcmBuffer != null)
			{
				ffmpeg.av_free(_audioPcmBuffer);
				_audioPcmBuffer = null;
			}
			if (_videoSwContext != null)
			{
				ffmpeg.sws_freeContext(_videoSwContext);
				_videoSwContext = null;
			}
			if (_audioSwContext != null)
			{
				var audioSwContext = _audioSwContext;
				ffmpeg.swr_free(&audioSwContext);
				_audioSwContext = null;
			}
			if (_formatContext != null)
			{
				var formatContext = _formatContext;
				ffmpeg.avformat_close_input(&formatContext);
				_formatContext = null;
			}

			VideoPlayback?.Dispose();
			AudioPlayback?.Dispose();
			VideoPlayback = null;
			AudioPlayback = null;
		}

		private static AVFrame* CreateVideoFrame(AVPixelFormat format, int width, int height, ref byte* buffer)
		{
			var frame = ffmpeg.av_frame_alloc();
			if (ClearBuffer(frame, format, width, height, ref buffer))
				return frame;

			ffmpeg.av_frame_free(&frame);
			return null;
		}

		private static bool ClearBuffer(AVFrame* frame, AVPixelFormat format, int width, int height, ref byte* buffer)
		{
			if (frame == null)
				return false;

			ulong size = (ulong)ffmpeg.av_image_get_buffer_size(format, width, height, sizeof(int));
			buffer = (byte*)ffmpeg.av_malloc(size);
			if (buffer == null)
				return false;

			var data = new byte_ptrArray4();
			var linesize = new int_array4();
			ffmpeg.av_image_fill_arrays(ref data, ref linesize, buffer, format, width, height, sizeof(int));
			for (uint i = 0; i < 4; i++)
			{
				frame->data[i] = data[i];
				frame->linesize[i] = linesize[i];
			}
			return true;
		}

		private static void DestroyVideoFrame(ref AVFrame* frame, ref byte* buffer)
		{
			var f = frame;
			ffmpeg.av_frame_free(&f);
			frame = null;
			ffmpeg.av_free(buffer);
			buffer = null;
		}

		#endregion

		#region Playpack

		public void Play()
		{
			if (!HasVideo && !HasAudio || State == PlayState.Playing)
				return;
			IsEndOfFileReached = false;
			_lastUpdate = DateTime.Now;
			NotifyStateChanged(PlayState.Playing);
			State = PlayState.Playing;
		}

		public void Pause()
		{
			if (State != PlayState.Playing)
				return;
			NotifyStateChanged(PlayState.Paused);
			State = PlayState.Paused;
		}

		public void Stop()
		{
			if (State == PlayState.Stopped)
				return;
			IsEndOfFileReached = true;
			NotifyStateChanged(PlayState.Stopped);
			State = PlayState.Stopped;
			StopDecodeThread();
			IsEndOfFileReached = false;
			PlayingOffset = TimeSpan.Zero;
		}

		public void Update()
		{
			if (PlayingOffset > FileLength)
			{
				Stop();
				IsEndOfFileReached = true;
			}
			var now = DateTime.Now;
			var deltaTime = now - _lastUpdate;
			_lastUpdate = now;
			if (State == PlayState.Playing)
				_playingOffset += deltaTime;

			// avoid huge jumps
			//if(deltaTime < TimeSpan.FromMilliseconds(100))
			VideoPlayback?.Update(deltaTime);
			AudioPlayback?.Update();
		}

		private void NotifyStateChanged(PlayState newState)
		{
			VideoPlayback?.StateChanged(State, newState);
			AudioPlayback?.StateChanged(State, newState);
		}

		#endregion

		#region Decode

		private void StartDecodeThread()
		{
			if (_decodeThread != null)
				return;
			_runDecodeThread = true;
			_decodeThread = new Thread(DecodeThreadRun) { Name = "Video Decode" };
			_decodeThread.Start();
		}

		private void StopDecodeThread()
		{
			if (_decodeThread == null || !_runDecodeThread)
				return;
			_runDecodeThread = false;
			_decodeThread.Join();
			_decodeThread = null;
		}

		private void DecodeThreadRun()
		{
			var packet = ffmpeg.av_packet_alloc();
			while (_runDecodeThread)
			{
				while (!IsFull && _runDecodeThread && !_playingToEof)
				{
					bool validPacket = false;
					while (!validPacket && _runDecodeThread)
					{
						ffmpeg.av_init_packet(packet);
						if (ffmpeg.av_read_frame(_formatContext, packet) == 0)
						{
							if (packet->stream_index == _videoStreamId)
							{
								validPacket = DecodeVideo(packet);
							}
							else if (packet->stream_index == _audioStreamId)
							{
								validPacket = DecodeAudio(packet);
							}
							else
								Console.WriteLine("Discarding packet for stream " + packet->stream_index);
						}
						else
						{
							_playingToEof = true;
							validPacket = true;
						}
						ffmpeg.av_packet_unref(packet);
					}
				}
				Thread.Sleep(50);
			}
			ffmpeg.av_packet_free(&packet);
		}

		private bool DecodeVideo(AVPacket* packet)
		{

			if (ffmpeg.avcodec_send_packet(_videoContext, packet) < 0)
				return false;
			if (ffmpeg.avcodec_receive_frame(_videoContext, _videoRawFrame) < 0)
				return false;

			if (ffmpeg.sws_scale(_videoSwContext, _videoRawFrame->data, _videoRawFrame->linesize, 0, _videoContext->height, _videoRgbaFrame->data, _videoRgbaFrame->linesize) == 0)
				return false;

			var videoPacket = new VideoPacket(_videoRgbaFrame->data[0], TimeSpan.Zero);
			VideoPlayback.PushPacket(videoPacket);
			ClearBuffer(_videoRgbaFrame, AVPixelFormat.AV_PIX_FMT_RGBA, _videoSize.Width, _videoSize.Height, ref _videoRgbaBuffer);

			return true;
		}

		private bool DecodeAudio(AVPacket* packet)
		{

			if (ffmpeg.avcodec_send_packet(_audioContext, packet) < 0)
				return false;
			if (ffmpeg.avcodec_receive_frame(_audioContext, _audioRawBuffer) < 0)
				return false;

			var audioPcmBuffer = _audioPcmBuffer;
			int convertlength = ffmpeg.swr_convert(_audioSwContext, &audioPcmBuffer, _audioRawBuffer->nb_samples, _audioRawBuffer->extended_data, _audioRawBuffer->nb_samples);

			if (convertlength <= 0)
				return false;

			var audioPacket = new AudioPacket(_audioPcmBuffer, convertlength, AudioChannelCount);
			AudioPlayback.PushPacket(audioPacket);

			return true;
		}

		#endregion
	}

	public enum PlayState
	{
		Stopped, Playing, Paused
	}
}
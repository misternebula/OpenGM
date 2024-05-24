using DELTARUNITYStandalone.SerializedFiles;
using NAudio.Wave;
using Newtonsoft.Json;
using NVorbis;
using OpenTK.Audio.OpenAL;
using System.Runtime.CompilerServices;

namespace DELTARUNITYStandalone;

// TODO: copy from https://github.com/misternebula/DELTARUNITY/blob/main/Assets/Scripts/AudioManager/AudioManager.cs
/*
 * installation:
 * download release from https://github.com/kcat/openal-soft
 * rename bin/Win64/guy to OpenAL32.dll
 * copy into build folder
 * 
 * resources i used:
 * https://indiegamedev.net/2020/02/15/the-complete-guide-to-openal-with-c-part-1-playing-a-sound/
 * https://gist.github.com/kamiyaowl/32fb397e0141c65792e1
 * https://www.openal.org/documentation/OpenAL_Programmers_Guide.pdf
 *
 * if openal sucks too much, could try using naudio
 */

public class AudioInstance
{
	public AudioAsset Asset;
	public int SoundInstanceId;
	public int Source;
}

public class AudioAsset
{
	public int AssetIndex;
	public int Buffer;
	public double Gain;
	public double Pitch;
}

public static class AudioManager
{
	private static ALDevice _device;
	private static ALContext _context;

	private static List<AudioInstance> _audioSources = new();
	private static Dictionary<int, AudioAsset> _audioClips = new();

	public static void Init()
	{
		_device = ALC.OpenDevice(null);
		CheckALCError();
		_context = ALC.CreateContext(_device, new ALContextAttributes());
		CheckALCError();
		ALC.MakeContextCurrent(_context);
		CheckALCError();

		Console.WriteLine(AL.Get(ALGetString.Version));
		CheckALError();
		Console.WriteLine(AL.Get(ALGetString.Vendor));
		CheckALError();
		Console.WriteLine(AL.Get(ALGetString.Extensions));
		CheckALError();

		// test
		if (false)
		{
			/*
			* these are like clips
			* we can probably alloc one for each sound on init
			* otherwise just alloc and dealloc as needed
			*/
			AL.GenBuffer(out var buffer);
			CheckALError();
			var bufferData = new double[44100 * 2];
			for (var i = 0; i < bufferData.Length; i += 2)
			{
				bufferData[i] = Math.Sin(i * (2 * Math.PI / 44100) * 440);
				bufferData[i + 1] = Math.Sin(i * (2 * Math.PI / 44100) * 440);
			}
			AL.BufferData(buffer, ALFormat.StereoDoubleExt, bufferData, 44100);
			CheckALError();

			/*
			* these are audio sources
			* pretty self explanatory
			*/
			AL.GenSource(out var source);
			CheckALError();
			AL.Source(source, ALSourcei.Buffer, buffer);
			CheckALError();
			AL.Source(source, ALSourcef.Gain, .1f);
			CheckALError();
			// AL.Source(source, ALSourceb.Looping, true);
			AL.SourcePlay(source);
			CheckALError();
		}

		LoadSounds();

		// test 2
		if (true)
		{
			var clip = _audioClips.Last().Value;

			AL.GenSource(out var source);
			CheckALError();
			AL.Source(source, ALSourcei.Buffer, clip.Buffer);
			CheckALError();
			AL.Source(source, ALSourcef.Gain, (float)clip.Gain);
			CheckALError();
			AL.Source(source, ALSourcef.Pitch, (float)clip.Pitch);
			CheckALError();
			AL.SourcePlay(source);
			CheckALError();
		}
	}

	/// <summary>
	/// load all the audio data into buffers
	/// has to happen after init since context is set up there
	/// </summary>
	private static void LoadSounds()
	{
		// TODO: see https://github.com/UnderminersTeam/UndertaleModTool/blob/master/UndertaleModTool/Scripts/Resource%20Unpackers/ExportAllSounds.csx
		Console.Write($"Loading asset order...");

		var soundsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Sounds");
		var files = Directory.GetFiles(soundsFolder, "*.json");
		foreach (var file in files)
		{
			var text = File.ReadAllText(file);
			var asset = JsonConvert.DeserializeObject<SoundAsset>(text);

			float[] data;
			bool stereo;
			int freq;
			if (Path.GetExtension(asset.File) == ".wav")
			{
				var reader = new AudioFileReader(Path.Combine(soundsFolder, asset.File));
				data = new float[reader.Length * 8 / reader.WaveFormat.BitsPerSample]; // taken from owml
				reader.Read(data, 0, data.Length);
				stereo = reader.WaveFormat.Channels == 2;
				freq = reader.WaveFormat.SampleRate;
			}
			else if (Path.GetExtension(asset.File) == ".ogg")
			{
				var reader = new VorbisReader(Path.Combine(soundsFolder, asset.File));
				data = new float[reader.TotalSamples * reader.Channels]; // is this correct length?
				reader.ReadSamples(data, 0, data.Length);
				stereo = reader.Channels == 2;
				freq = reader.SampleRate;
			}
			else
			{
				DebugLog.LogError($"unknown audio file format {asset.File}");
				return;
			}

			var buffer = AL.GenBuffer();
			CheckALError();
			AL.BufferData(buffer, stereo ? ALFormat.StereoFloat32Ext : ALFormat.MonoFloat32Ext, data, freq);
			CheckALError();

			_audioClips[asset.AssetID] = new()
			{
				AssetIndex = asset.AssetID,
				Buffer = buffer,
				Gain = asset.Volume,
				Pitch = asset.Pitch,
			};
		}

		Console.WriteLine($" Done!");
	}

	public static void Dispose()
	{
		/*
		 * deallocate all the buffers
		 * and currently playing sources here
		 */
		foreach (var source in _audioSources)
		{
			AL.DeleteSource(source.Source);
			CheckALError();
		}
		foreach (var buffer in _audioClips.Values)
		{
			AL.DeleteBuffer(buffer.Buffer);
			CheckALError();
		}

		ALC.MakeContextCurrent(ALContext.Null);
		CheckALCError();
		ALC.DestroyContext(_context);
		CheckALCError();
		ALC.CloseDevice(_device);
		CheckALCError();
	}

	public static void Update()
	{
		/*
		 * we should have a pool of clips
		 * when playing, add to the pool. and when its done, remove it and deletesource
		 * alternatively, maybe reuse sources? dont think thats even needed and we can gensource each time
		 * i guess its not a pool at that point. more of an "active sources" thing.
		 * could do the same for buffers if theyre not all made on init
		 */
		foreach (var source in _audioSources) { }
	}

	private static void CheckALCError(
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		var e = ALC.GetError(_device);
		if (e != AlcError.NoError)
		{
			DebugLog.LogError($"[{memberName} at {sourceFilePath}:{sourceLineNumber}] ALC error: {e}");
		}
	}

	private static void CheckALError(
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		var e = AL.GetError();
		if (e != ALError.NoError)
		{
			DebugLog.LogError($"[{memberName} at {sourceFilePath}:{sourceLineNumber}] AL error: {e}");
		}
	}
}

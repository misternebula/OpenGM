using DELTARUNITYStandalone.SerializedFiles;
using DELTARUNITYStandalone.VirtualMachine;
using Newtonsoft.Json;
using OpenTK.Audio.OpenAL;

namespace DELTARUNITYStandalone;

// TODO: copy from https://github.com/misternebula/DELTARUNITY/blob/main/Assets/Scripts/AudioManager/AudioManager.cs
/*
 * you need to install openal from https://github.com/kcat/openal-soft following the README
 * 
 * resources i used:
 * https://indiegamedev.net/2020/02/15/the-complete-guide-to-openal-with-c-part-1-playing-a-sound/
 * https://gist.github.com/kamiyaowl/32fb397e0141c65792e1
 * https://www.openal.org/documentation/OpenAL_Programmers_Guide.pdf
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

	private static List<int> _activeSources = new();

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
	}

	/// <summary>
	/// load all the audio data into buffers
	/// has to happen after init since context is set up there
	/// </summary>
	private static void LoadSounds()
	{
		// TODO: see https://github.com/UnderminersTeam/UndertaleModTool/blob/master/UndertaleModTool/Scripts/Resource%20Unpackers/ExportAllSounds.csx
		Console.Write($"Loading asset order...");

		var soundsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Scripts");
		var files = Directory.GetFiles(soundsFolder);
		foreach (var file in files)
		{
			var text = File.ReadAllText(file);
			var asset = JsonConvert.DeserializeObject<SoundAsset>(text);
		}


		Console.WriteLine($" Done!");
	}

	public static void Dispose()
	{
		/*
		 * deallocate all the buffers
		 * and currently playing sources here
		 */

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
		foreach (var activeSource in _activeSources) { }
	}

	private static void CheckALCError()
	{
		var e = ALC.GetError(_device);
		if (e != AlcError.NoError)
		{
			DebugLog.LogError($"ALC error: {e}");
		}
	}

	private static void CheckALError()
	{
		var e = AL.GetError();
		if (e != ALError.NoError)
		{
			DebugLog.LogError($"AL error: {e}");
		}
	}
}

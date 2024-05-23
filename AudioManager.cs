using OpenTK.Audio.OpenAL;

namespace DELTARUNITYStandalone;

// TODO: copy from https://github.com/misternebula/DELTARUNITY/blob/main/Assets/Scripts/AudioManager/AudioManager.cs

/*
 * you need openal installed on your computer
 * 
 * resources i used:
 * https://indiegamedev.net/2020/02/15/the-complete-guide-to-openal-with-c-part-1-playing-a-sound/
 * https://gist.github.com/kamiyaowl/32fb397e0141c65792e1
 * https://www.openal.org/documentation/OpenAL_Programmers_Guide.pdf
 *
 * we should maybe error check at least for oom. but eh, kinda funny if we dont
 */

public static class AudioManager
{
	private static ALDevice _device;
	private static ALContext _context;

	public static void Init()
	{
		_device = ALC.OpenDevice(null);
		_context = ALC.CreateContext(_device, new ALContextAttributes());
		ALC.MakeContextCurrent(_context);

		// test
		/*
		 * these are like clips
		 * we can probably alloc one for each sound on init
		 * otherwise just alloc and dealloc as needed
		 */
		AL.GenBuffer(out var buffer);
		var bufferData = new byte[44100 * 2];
		Random.Shared.NextBytes(bufferData);
		AL.BufferData(buffer, ALFormat.Stereo8, bufferData, 44100);

		/*
		 * these are audio sources
		 * pretty self explanatory
		 */
		AL.GenSource(out var source);
		AL.Source(source, ALSourcei.Buffer, buffer);
		AL.Source(source, ALSourcef.Gain, .1f);
		// AL.Source(source, ALSourceb.Looping, true);
		AL.SourcePlay(source);
	}

	public static void Dispose()
	{
		/*
		 * deallocate all the buffers
		 * and currently playing sources here
		 */
		
		ALC.MakeContextCurrent(ALContext.Null);
		ALC.DestroyContext(_context);
		ALC.CloseDevice(_device);
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
	}
}

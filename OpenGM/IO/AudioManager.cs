using MemoryPack;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using NAudio.Wave;
using Newtonsoft.Json;
using NVorbis;
using OpenTK.Audio.OpenAL;
using System.Runtime.CompilerServices;

namespace OpenGM.IO;

/*
 * copied from https://github.com/misternebula/DELTARUNITY/blob/main/Assets/Scripts/AudioManager/AudioManager.cs
 * organization is eh, spread out between here and ScriptResolver. its fine
 * 
 * openal installation:
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
    public AudioAsset Asset = null!;
    public int SoundInstanceId;
    public int Source;
    public double Priority;
    public float Timer;
}

public class AudioAsset
{
    public int AssetIndex;
    public string Name = null!;
    public int Clip;
    public double Gain;
    public double Pitch;
    public double Offset;
}

public static class AudioManager
{
    private static ALDevice _device;
    private static ALContext _context;

    public static int AudioChannelNum = 128;

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
    }

    /// <summary>
    /// load all the audio data into buffers
    /// has to happen after init since context is set up there
    /// </summary>
    public static void LoadSounds(BinaryReader reader)
    {
        Console.Write($"Loading sounds...");

        var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var asset = reader.ReadMemoryPack<SoundAsset>();

            var bytesLength = reader.ReadInt32();
            var bytes = reader.ReadBytes(bytesLength);
            
            float[] data;
            bool stereo;
            int freq;
            if (Path.GetExtension(asset.File) == ".wav")
            {
                // write to the same file to get slightly more perf. need the extension so the reader knows what it is
                File.WriteAllBytes("temp.wav", bytes);
                
                try
                {
                    using var audioFileReader = new AudioFileReader("temp.wav");
                    data = new float[audioFileReader.Length * 8 / audioFileReader.WaveFormat.BitsPerSample]; // taken from owml
                    audioFileReader.Read(data, 0, data.Length);
                    stereo = audioFileReader.WaveFormat.Channels == 2;
                    freq = audioFileReader.WaveFormat.SampleRate;
                }
                catch (Exception)
                {
                    data = new float[] { };
                    freq = 1;
                    stereo = false;
                }
            }
            else if (Path.GetExtension(asset.File) == ".ogg")
            {
                using var stream = new MemoryStream(bytes);

                using var vorbisReader = new VorbisReader(stream);
                data = new float[vorbisReader.TotalSamples * vorbisReader.Channels]; // is this correct length?
                vorbisReader.ReadSamples(data, 0, data.Length);
                stereo = vorbisReader.Channels == 2;
                freq = vorbisReader.SampleRate;
            }
            else
            {
                throw new NotImplementedException($"unknown audio file format {asset.File}");
            }

            var buffer = AL.GenBuffer();
            CheckALError();
            AL.BufferData(buffer, stereo ? ALFormat.StereoFloat32Ext : ALFormat.MonoFloat32Ext, data, freq);
            CheckALError();

            _audioClips[asset.AssetID] = new()
            {
                AssetIndex = asset.AssetID,
                Name = asset.Name,
                Clip = buffer,
                Gain = asset.Volume,
                Pitch = asset.Pitch,
                Offset = 0,
            };
        }
        File.Delete("temp.wav");

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
        foreach (var clip in _audioClips.Values)
        {
            AL.DeleteBuffer(clip.Clip);
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
        for (var i = _audioSources.Count - 1; i >= 0; i--)
        {
            var source = _audioSources[i];
            var state = (ALSourceState)AL.GetSource(source.Source, ALGetSourcei.SourceState);
            CheckALError();
            // you cant re-play an existing instance, so stopped audio is used as a sign to clean up
            if (state == ALSourceState.Stopped)
            {
                //DebugLog.Log($"source {source.Source} {source.Asset.Name} stopped manually or done playing");
                AL.DeleteSource(source.Source);
                CheckALError();
                _audioSources.RemoveAt(i);
            }
        }
    }

    public static void CheckALCError()
    {
        var e = ALC.GetError(_device);
        if (e != AlcError.NoError)
        {
            throw new Exception($"ALC error: {e}");
        }
    }

    public static void CheckALError()
    {
        var e = AL.GetError();
        if (e != ALError.NoError)
        {
            throw new Exception($"AL error: {e}");
        }
    }


    public static int RegisterAudioClip(AudioAsset asset)
    {
        var index = AssetIndexManager.Register(AssetType.sounds, asset.Name);

        asset.AssetIndex = index;
        _audioClips.Add(index, asset);
        return index;
    }

    public static void UnregisterAudio(int index)
    {
        if (!_audioClips.ContainsKey(index))
        {
            DebugLog.LogWarning($"UnregisterAudio - couldnt find audio asset {index}");
            return;
        }

        var asset = _audioClips[index];

        // gotta remove sources playing this before we can delete it
        foreach (var source in _audioSources)
        {
            var buffer = AL.GetSource(source.Source, ALGetSourcei.Buffer);
            CheckALError();
            if (asset.Clip == buffer)
            {
                AL.SourceStop(source.Source);
                CheckALError();
            }
        }
        Update(); // hack: deletes the sources. maybe make official stop and delete function

        AL.DeleteBuffer(asset.Clip);
        CheckALError();

        _audioClips.Remove(index);
        AssetIndexManager.Unregister(AssetType.sounds, asset.Name);
    }

    public static AudioInstance? GetAudioInstance(int instanceId)
    {
        return _audioSources.FirstOrDefault(x => x.SoundInstanceId == instanceId);
    }

    public static AudioInstance[] GetAudioInstances(int assetIndex)
    {
        return _audioSources.Where(x => x.Asset.AssetIndex == assetIndex).ToArray();
    }

    public static void SetAssetGain(int assetIndex, double gain)
    {
        _audioClips[assetIndex].Gain = gain;
    }

    public static void SetAssetPitch(int assetIndex, double pitch)
    {
        _audioClips[assetIndex].Pitch = pitch;
    }

    public static void SetAssetOffset(int assetIndex, double time)
    {
        _audioClips[assetIndex].Offset = time;
    }

    public static double GetAssetOffset(int assetIndex)
    { 
	    return _audioClips[assetIndex].Offset;
    }


	public static AudioAsset GetAudioAsset(int assetIndex)
    {
        return _audioClips[assetIndex];
    }

    public static void StopAllAudio()
    {
        foreach (var item in _audioSources)
        {
            AL.SourceStop(item.Source);
            CheckALError();
        }
        Update(); // hack: deletes the sources. maybe make official stop and delete function
    }

    private static int _highestSoundInstanceId = GMConstants.FIRST_INSTANCE_ID;

    public static int audio_play_sound(int index, double priority, bool loop, double gain, double offset, double pitch)
    {
        //var name = AssetIndexManager.Instance.GetName(AssetType.sounds, index);

        if (_audioSources.Count == AudioChannelNum)
        {
            var oldSourceInstance = _audioSources.MinBy(x => x.Priority)!;
            var oldSource = oldSourceInstance.Source;

            DebugLog.LogWarning($"Went over audio source limit - re-using source playing {oldSourceInstance.Asset.Name}");

            AL.SourceStop(oldSource);
            CheckALError();
            AL.Source(oldSource, ALSourcei.Buffer, _audioClips[index].Clip);
            CheckALError();
            AL.Source(oldSource, ALSourceb.Looping, loop);
            CheckALError();
            AL.Source(oldSource, ALSourcef.Gain, (float)gain);
            CheckALError();
            AL.Source(oldSource, ALSourcef.SecOffset, (float)offset);
            CheckALError();
            AL.Source(oldSource, ALSourcef.Pitch, (float)pitch);
            CheckALError();
            AL.SourcePlay(oldSource);
            CheckALError();

            oldSourceInstance.SoundInstanceId = ++_highestSoundInstanceId;
            oldSourceInstance.Priority = priority;

            return oldSourceInstance.SoundInstanceId;
        }

        if (index == -1 || !_audioClips.ContainsKey(index))
        {
            DebugLog.LogError($"AudioDatabase doesn't contain {index}!");
            // Debug.Break();
        }

        var source = AL.GenSource();
        CheckALError();
        AL.Source(source, ALSourcei.Buffer, _audioClips[index].Clip);
        CheckALError();
        AL.Source(source, ALSourceb.Looping, loop);
        CheckALError();
        AL.Source(source, ALSourcef.Gain, (float)gain);
        CheckALError();
        AL.Source(source, ALSourcef.SecOffset, (float)offset);
        CheckALError();
        AL.Source(source, ALSourcef.Pitch, (float)pitch);
        CheckALError();
        AL.SourcePlay(source);
        CheckALError();

        var instance = new AudioInstance
        {
            Asset = _audioClips[index],
            SoundInstanceId = ++_highestSoundInstanceId,
            Source = source,
            Priority = priority,
        };

        _audioSources.Add(instance);

        return instance.SoundInstanceId;
    }

    public static void ChangeGain(int source, double volume, double milliseconds)
    {
        // todo implement lerping with timer
        AL.Source(source, ALSourcef.Gain, (float)volume);
        CheckALError();
    }
}

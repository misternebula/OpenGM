using NAudio.Wave;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using OpenTK.Audio.OpenAL;
using StbVorbisSharp;

namespace OpenGM.IO;

/*
 * copied from https://github.com/misternebula/DELTARUNITY/blob/main/Assets/Scripts/AudioManager/AudioManager.cs
 * organization is eh, spread out between here and ScriptResolver. its fine
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
    public static Dictionary<int, AudioAsset> _audioClips = new();

    private static bool _inited;

    public static void Init()
    {
        if (_inited)
        {
            return;
        }

        _device = ALC.OpenDevice(null);
        CheckALCError();
        _context = ALC.CreateContext(_device, new ALContextAttributes());
        CheckALCError();
        ALC.MakeContextCurrent(_context);
        CheckALCError();

        Console.WriteLine(AL.Get(ALGetString.Version));
        CheckALError();
        //Console.WriteLine(AL.Get(ALGetString.Vendor));
        //CheckALError();
        //Console.WriteLine(AL.Get(ALGetString.Extensions));
        //CheckALError();

        _inited = true;

    }

    public static void Dispose()
    {
        if (!_inited)
        {
            return;
        }
        
        /*
         * deallocate all the buffers
         * and currently playing sources here
         */
        foreach (var source in _audioSources)
        {
            AL.DeleteSource(source.Source);
            CheckALError();
        }
        _audioSources.Clear();
        foreach (var clip in _audioClips.Values)
        {
            AL.DeleteBuffer(clip.Clip);
            CheckALError();
        }
        _audioClips.Clear();

        ALC.MakeContextCurrent(ALContext.Null);
        CheckALCError();
        ALC.DestroyContext(_context);
        CheckALCError();
        _context = default;
        ALC.CloseDevice(_device);
        CheckALCError();
        _device = default;

        _inited = false;
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

    public static AudioInstance[] GetAllAudioInstances()
    {
        return _audioSources.ToArray();
    }

    public static void SetAssetGain(int assetIndex, double gain)
    {
        _audioClips[assetIndex].Gain = gain;
    }

    public static void SetAssetPitch(int assetIndex, double pitch)
    {
        _audioClips[assetIndex].Pitch = pitch;
    }

    public static double GetAssetPitch(int assetIndex)
    {
        return _audioClips[assetIndex].Pitch;
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
        volume = Math.Max(0, volume); // TODO : check if this is what GM does?

        // todo implement lerping with timer
        AL.Source(source, ALSourcef.Gain, (float)volume);
        CheckALError();
    }

    public static double GetClipLength(AudioAsset asset)
    {
        // TODO : should this account for offset? speed? idk!!!

        var buffer = asset.Clip;
        AL.GetBuffer(buffer, ALGetBufferi.Size, out var sizeInBytes);
        CheckALError();
        AL.GetBuffer(buffer, ALGetBufferi.Channels, out var channelCount);
        CheckALError();
        AL.GetBuffer(buffer, ALGetBufferi.Bits, out var bitDepth);
        CheckALError();
        AL.GetBuffer(buffer, ALGetBufferi.Frequency, out var frequency);
        CheckALError();

        var numberOfBits = sizeInBytes * 8;
        var sampleLength = numberOfBits / (channelCount * bitDepth);
        return sampleLength / (double)frequency;
    }
}

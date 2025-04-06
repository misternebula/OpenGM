using Microsoft.VisualStudio.TestTools.UnitTesting;
using NVorbis;
using OpenGM.IO;
using StbVorbisSharp;

namespace OpenGM.Tests;

[TestClass]
public class SoundTests
{
    [TestMethod]
    public unsafe void TestGoodOgg()
    {
        // audacity says 480374 * 2 = 960748 samples
        
        {
            using var vorbisReader = new VorbisReader("AUDIO_INTRONOISE.ogg");
            var samples = new float[vorbisReader.TotalSamples * vorbisReader.Channels];
            var count = vorbisReader.ReadSamples(samples, 0, samples.Length);
            // apparently this differs from total samples??? why??
            DebugLog.LogInfo($"buffer {samples.Length} samples, got {count} samples");
        }

        {
            using var vorbis = Vorbis.FromMemory(File.ReadAllBytes("AUDIO_INTRONOISE.ogg"));
            var samples = new float[vorbis.StbVorbis.total_samples * vorbis.Channels];
            int count;
            fixed (float* ptr = samples)
                count = StbVorbis.stb_vorbis_get_samples_float_interleaved(vorbis.StbVorbis, vorbis.Channels, ptr, samples.Length);
            count *= vorbis.Channels;
            DebugLog.LogInfo($"buffer {samples.Length} samples, got {count} samples");
        }
    }

    [TestMethod]
    public unsafe void TestBadOgg()
    {
        // audacity says 1411648 * 2 = 2823296 samples
        
        // nvorbis hangs
        
        using var vorbis = Vorbis.FromMemory(File.ReadAllBytes("mus_menu1.ogg"));
        var samples = new float[vorbis.StbVorbis.total_samples * vorbis.Channels];
        int count;
        fixed (float* ptr = samples)
            count = StbVorbis.stb_vorbis_get_samples_float_interleaved(vorbis.StbVorbis, vorbis.Channels, ptr, samples.Length);
        count *= vorbis.Channels;
        DebugLog.LogInfo($"buffer {samples.Length} samples, got {count} samples");
    }

    [TestMethod]
    public unsafe void VorbisVsStb()
    {
        {
            using var reader = new VorbisReader("AUDIO_INTRONOISE.ogg");
            var total = 0;
            var iter = 0;
            var buffer = new float[reader.SampleRate];
            while (true)
            {
                var count = reader.ReadSamples(buffer, 0, buffer.Length);
                total += count;
                iter++;
                DebugLog.Log($"read {count} iter {iter} total {total} of {reader.TotalSamples * reader.Channels}");
                if (count == 0) break;
            }
        }
    
        DebugLog.LogInfo("-----------------------------------------");
        DebugLog.LogInfo("-----------------------------------------");
        DebugLog.LogInfo("-----------------------------------------");

        {
            // https://nothings.org/stb_vorbis/samples/sample.c
            using var vorbis = Vorbis.FromMemory(File.ReadAllBytes("AUDIO_INTRONOISE.ogg"));
            var total = 0;
            var iter = 0;
            var buffer = new float[vorbis.SampleRate];
            while (true)
            {
                int count;
                fixed (float* ptr = buffer)
                    count = StbVorbis.stb_vorbis_get_samples_float_interleaved(vorbis.StbVorbis, vorbis.Channels, ptr, buffer.Length);
                count *= vorbis.Channels; // returns count per channel
                total += count;
                iter++;
                DebugLog.LogInfo($"read {count} iter {iter} total {total} of {vorbis.StbVorbis.total_samples * vorbis.Channels}");
                if (count == 0) break;
            }

        }
    }
}
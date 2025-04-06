using Microsoft.VisualStudio.TestTools.UnitTesting;
using NVorbis;
using OpenGM.IO;
using StbVorbisSharp;

namespace OpenGM.Tests;

[TestClass]
public class SoundTests
{
    [TestMethod]
    public void TestGoodOgg()
    {
        using var vorbisReader = new VorbisReader("AUDIO_INTRONOISE.ogg");
        var samples = new float[vorbisReader.TotalSamples * vorbisReader.Channels];
        vorbisReader.ReadSamples(samples, 0, samples.Length);
    }

    [TestMethod]
    public void TestBadOgg()
    {
        // this hangs rn
        using var vorbisReader = new VorbisReader("mus_menu1.ogg");
        var samples = new float[vorbisReader.TotalSamples * vorbisReader.Channels];
        // vorbisReader.ReadSamples(samples, 0, samples.Length);
    }

    [TestMethod]
    public unsafe void VorbisVsStb()
    {
        {
            using var reader = new VorbisReader("AUDIO_INTRONOISE.ogg");
            var total = 0;
            while (true)
            {
                var buffer = new float[1024];
                var count = reader.ReadSamples(buffer, 0, buffer.Length);
                DebugLog.Log($"read {count} total {total} of {reader.TotalSamples * reader.Channels}");
                total += count;
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
            vorbis.SubmitBuffer();
            var samples = new float[1024];
            while (true)
            {
                int count;
                fixed (float* ptr = samples)
                    count = StbVorbis.stb_vorbis_get_samples_float_interleaved(vorbis.StbVorbis, vorbis.Channels, ptr, samples.Length);
                total += count;
                DebugLog.LogInfo($"read {count} total {total} of {vorbis.StbVorbis.total_samples*vorbis.Channels}");
                if (count == 0) break;
            }

        }
    }
}
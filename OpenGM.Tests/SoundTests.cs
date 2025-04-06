using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NVorbis;
using OpenGM.VirtualMachine;

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
}
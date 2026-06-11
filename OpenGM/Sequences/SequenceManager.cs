using OpenGM.IO;
using OpenGM.Loading;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using OpenGM.VirtualMachine.BuiltInFunctions;
using OpenTK.Mathematics;
using UndertaleModLib.Models;

namespace OpenGM.Sequences;

public static class SequenceManager
{
    public enum KeyframeExecutionState
    {
        NotRunningTooEarly,
        NotRunningTooLate,
        Start,
        Running,
        End
    }

    public static int SequenceIndex;

    public static int LayerSequenceCreate(LayerContainer layer, float x, float y, RuntimeSequence seqStruct)
    {
        var item = new CLayerSequenceElement();
        item.Id = SequenceIndex++;
        item.Type = ElementType.Sequence;
        item.Layer = layer;
        item.ImageScaleX = 1;
        item.ImageScaleY = 1;
        item.X = x;
        item.Y = y;
        item.SeqID = GameLoader.Sequences.Values.ToList().IndexOf(seqStruct); // todo: ?? is this right?

        var sequence = new GMSequence(item);

        layer.ElementsToDraw.Add(sequence);

        return item.Id;
    }
}
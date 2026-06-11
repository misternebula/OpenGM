using OpenGM.IO;
using OpenGM.Loading;
using System;
using System.Collections.Generic;
using System.Text;
using OpenGM.Sequences;

namespace OpenGM.VirtualMachine.BuiltInFunctions;

public static class SequenceFunctions
{
    [GMLFunction("sequence_get")]
    public static object? sequence_get(object?[] args)
    {
        var sequence_index = args[0].Conv<int>();

        if (!GameLoader.Sequences.TryGetValue(sequence_index, out var val))
        {
            return -1;
        }

        return GameLoader.Sequences[sequence_index];
    }

    [GMLFunction("layer_sequence_create")]
    public static object? layer_sequence_create(object?[] args)
    {
        var layer_id = args[0];
        var x = args[1].Conv<float>();
        var y = args[2].Conv<float>();
        var sequence_id = args[3].Conv<int>();

        var layer = RoomManager.CurrentRoom.GetLayer(layer_id);
        var sequence = GameLoader.Sequences[sequence_id];

        return SequenceManager.LayerSequenceCreate(layer!, x, y, sequence);
    }

    private static GMSequence? GetLayerSequence(int sequence_element_id)
    {
        foreach (var (layerid, layer) in RoomManager.CurrentRoom.Layers)
        {
            foreach (var element in layer.ElementsToDraw)
            {
                if (element is GMSequence back && back.Element.Id == sequence_element_id)
                {
                    return back;
                }
            }
        }

        return null;
    }

    [GMLFunction("layer_sequence_get_instance")]
    public static object? layer_sequence_get_instance(object?[] args)
    {
        var sequence_element_id = args[0].Conv<int>();
        var sequence = GetLayerSequence(sequence_element_id);

        if (sequence == null)
        {
            throw new NotImplementedException();
        }

        return sequence.SequenceInstance;
    }

    [GMLFunction("layer_sequence_get_headpos")]
    public static object? layer_sequence_get_headpos(object?[] args)
    {
        var sequence_element_id = args[0].Conv<int>();
        var sequence = GetLayerSequence(sequence_element_id);

        if (sequence == null)
        {
            throw new NotImplementedException();
        }

        return sequence.SequenceInstance.headPosition;
    }

    [GMLFunction("layer_sequence_is_finished")]
    public static object? layer_sequence_is_finished(object?[] args)
    {
        var sequence_element_id = args[0].Conv<int>();
        var sequence = GetLayerSequence(sequence_element_id);

        if (sequence == null)
        {
            throw new NotImplementedException();
        }

        return sequence.SequenceInstance.finished;
    }

    [GMLFunction("layer_sequence_destroy")]
    public static object? layer_sequence_destroy(object?[] args)
    {
        var sequence_element_id = args[0].Conv<int>();
        var sequence = GetLayerSequence(sequence_element_id);

        if (sequence == null)
        {
            DebugLog.LogWarning($"Trying to destroy sequence element {sequence_element_id} which can't be found.");
            return null;
        }

        sequence.Element.Layer.ElementsToDraw.Remove(sequence);
        sequence.Destroy();
        return null;
    }
}

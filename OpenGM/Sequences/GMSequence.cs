using OpenGM.IO;
using OpenGM.Loading;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using OpenGM.VirtualMachine.BuiltInFunctions;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;
using UndertaleModLib.Models;

namespace OpenGM.Sequences;

public class GMSequence : DrawWithDepth
{
    public CLayerSequenceElement Element;

    public GMSequence(CLayerSequenceElement element)
    {
        Element = element;
        DrawManager.Register(this);

        var sequence = GameLoader.Sequences[element.SeqID];

        ImageSpeed = sequence.playbackSpeed;
        SpeedType = sequence.playbackSpeedType;

        SequenceInstance = new RuntimeSequenceInstance();
        SequenceInstance.sequence = element.SeqID;
        SequenceInstance.headPosition = 0;
        SequenceInstance.headDirection = 1; // right
        SequenceInstance.speedScale = 1;
        SequenceInstance.paused = false;
        SequenceInstance.finished = false;
        SequenceInstance.elementID = Element.Id;
        SequenceInstance.activeTracks = [];

        CreateInstances();
    }

    public RuntimeSequenceInstance SequenceInstance;

    public float ImageSpeed;
    public int SpeedType;
    public float ImageScaleX;
    public float ImageScaleY;
    public float ImageAngle;
    public uint ImageBlend;
    public float ImageAlpha;
    public float X;
    public float Y;

    public void CreateInstances()
    {
        var sequence = GameLoader.Sequences[Element.SeqID];

        SequenceInstance.activeTracks = new RuntimeTrackInstance[sequence.tracks.Length];

        for (var i = 0; i < sequence.tracks.Length; i++)
        {
            var track = sequence.tracks[i];

            var instance = new RuntimeTrackInstance();
            instance.posx = 0;
            instance.posy = 0;
            instance.rotation = 0;
            instance.xorigin = 0;
            instance.yorigin = 0;
            // matrix
            // parent
            instance.track = track;
            // activeTracks
            instance.scalex = 1;
            instance.scaley = 1;
            // colouradd
            instance.colormultiply = 16777215;

            instance.spriteIndex = -1;
            instance.imageindex = 0;
            instance.imagespeed = 0;

            instance.instanceID = -1;

            instance.soundIndex = -1;
            instance.emitterIndex = -1;
            instance.gain = 1;
            instance.pitch = 1;

            if (track.type == TrackType.Instance)
            {
                if (track.keyframes.Length == 0)
                {
                    continue;
                }

                var firstKeyframe = track.keyframes[0];
                var objectIndex = firstKeyframe.channels[0].objectIndex;

                instance.instanceID = InstanceManager.instance_create_layer(0, 0, Element.Layer, objectIndex, null);
            }

            if (track.type == TrackType.Audio)
            {
                if (track.keyframes.Length == 0)
                {
                    continue;
                }

                instance.emitterIndex = AudioManager.AudioEmitterCreate();
            }

            if (track.type == TrackType.Sequence)
            {
                if (track.keyframes.Length == 0)
                {
                    continue;
                }

                var firstKeyframe = track.keyframes[0];

                var ret = SequenceManager.LayerSequenceCreate(Element.Layer, 0, 0, firstKeyframe.channels[0].sequence);
                instance.sequenceElement = ((GMSequence)Element.Layer.ElementsToDraw.First(x => x is GMSequence seq && seq.Element.Id == ret)).Element;
                instance.sequenceID = GameLoader.Sequences.Values.ToList().IndexOf(firstKeyframe.channels[0].sequence);
            }

            SequenceInstance.activeTracks[i] = instance;
        }
    }

    public override void Draw()
    {
        if (SequenceInstance.activeTracks.Length == 0)
        {
            return;
        }

        if (SequenceInstance.finished)
        {
            return;
        }

        var sequence = GameLoader.Sequences[Element.SeqID];

        HandleMoments(sequence);

        EvaluateSequence(sequence);

        SequenceInstance.headPosition += SequenceInstance.headDirection;

        if (SequenceInstance.headPosition == sequence.length + 1 || SequenceInstance.headPosition == 0)
        {
            if (sequence.loopmode == UndertaleSequence.PlaybackType.Oneshot)
            {
                SequenceInstance.finished = true;
            }
            else if (sequence.loopmode == UndertaleSequence.PlaybackType.Loop)
            {
                SequenceInstance.headPosition = 0;
            }
            else if (sequence.loopmode == UndertaleSequence.PlaybackType.Pingpong)
            {
                SequenceInstance.headDirection = -SequenceInstance.headDirection;

                if (SequenceInstance.headPosition == sequence.length + 1)
                {
                    SequenceInstance.headPosition = sequence.length - 1;
                }
            }
        }
    }

    public void EvaluateSequence(RuntimeSequence sequence)
    {
        foreach (var track in sequence.tracks)
        {
            var trackIndex = sequence.tracks.IndexOf(track);

            foreach (var subtrack in track.tracks)
            {
                Evaluate(subtrack, subtrack.BackingTrack.BuiltinName, subtrack.type, SequenceInstance.activeTracks[trackIndex]);
            }

            Evaluate(track, track.BackingTrack.BuiltinName, track.type, SequenceInstance.activeTracks[trackIndex]);
        }
    }

    public void Evaluate(
        RuntimeTrack track,
        UndertaleSequence.Track.TrackBuiltinName trackName,
        TrackType trackType,
        RuntimeTrackInstance trackInstance)
    {
        if (track.BackingTrack.Interpolation == 1)
        {
            var (keyOne, keyTwo, blend) = EvaluateInterpolated(track.keyframes);

            ExecuteKeyframe(keyOne, keyTwo, trackName, trackType, trackInstance);
        }
        else
        {
            // TODO: stupid hack for imageIndex. is this the only case needed? 
            var keyframe = EvaluateDirect(track.keyframes, trackName == UndertaleSequence.Track.TrackBuiltinName.ImageIndex);

            if (keyframe == null)
            {
                return;
            }

            ExecuteKeyframe(keyframe, null, trackName, trackType, trackInstance);
        }
    }

    public RuntimeKeyframe? EvaluateDirect(RuntimeKeyframe[] keyframes, bool ignoreLength)
    {
        var head = SequenceInstance.headPosition;

        if (SequenceInstance.headDirection == 1)
        {
            foreach (var key in keyframes)
            {
                if (head >= key.frame && head <= key.frame + key.length)
                {
                    return key;
                }
            }
        }
        else
        {
            // iterate backwards to find the correct one when ignoring length
            for (var i = keyframes.Length - 1; i >= 0; i--)
            {
                var key = keyframes[i];

                if (head >= key.frame && (ignoreLength || head <= key.frame + key.length))
                {
                    return key;
                }
            }
        }

        return null;
    }

    public (RuntimeKeyframe keyOne, RuntimeKeyframe? keyTwo, float blend) EvaluateInterpolated(RuntimeKeyframe[] keyframes)
    {
        // TODO: keyframes *are* correctly ordered... right? i hope so

        var head = SequenceInstance.headPosition; // increases for forward, decreases for backward

        // --- check if we're at the beginning/end of the keyframes

        var firstKeyframe = keyframes[0];
        var lastKeyframe = keyframes[^1];

        var beforeFirst = firstKeyframe.frame > head;
        var afterLast = lastKeyframe.frame < head;

        if (beforeFirst)
        {
            return (firstKeyframe, null, 0);
        }

        if (afterLast)
        {
            return (lastKeyframe, null, 0);
        }

        // -- check if we're exactly on a keyframe

        foreach (var key in keyframes)
        {
            if (key.frame == head)
            {
                return (key, null, 0);
            }
        }

        // -- we're between two keyframes, find which ones

        var keyOne = keyframes.Last(x => x.frame < head);
        var keyTwo = keyframes.First(x => x.frame > head);

        // 0 is 100% keyOne, 1 is 100% keyTwo
        var blend = (head - keyOne.frame) / (keyTwo.frame - keyOne.frame);

        return (keyOne, keyTwo, blend);
    }

    public void HandleMoments(RuntimeSequence sequence)
    {
        foreach (var moment in sequence.momentKeyframes)
        {
            if (SequenceInstance.headPosition == moment.frame)
            {
                if (moment.channels.Length > 1)
                {
                    throw new NotImplementedException();
                }

                var script = ScriptResolver.ScriptsByIndex[moment.channels[0].@event];
                VMExecutor.ExecuteCode(script.GetCode(), null);
            }
        }
    }

    public SequenceManager.KeyframeExecutionState GetKeyframeExecutionState(RuntimeKeyframe keyframe)
    {
        if (SequenceInstance.headDirection == 1)
        {
            // right, positive

            if (SequenceInstance.headPosition == keyframe.frame)
            {
                return SequenceManager.KeyframeExecutionState.Start;
            }
            else if (SequenceInstance.headPosition == keyframe.frame + keyframe.length)
            {
                return SequenceManager.KeyframeExecutionState.End;
            }
            else if (SequenceInstance.headPosition < keyframe.frame)
            {
                return SequenceManager.KeyframeExecutionState.NotRunningTooEarly;
            }
            else if (SequenceInstance.headPosition > keyframe.frame + keyframe.length)
            {
                return SequenceManager.KeyframeExecutionState.NotRunningTooLate;
            }
            else
            {
                return SequenceManager.KeyframeExecutionState.Running;
            }
        }
        else
        {
            // left, negative

            // TODO: bad! this -1 makes things disappear for a frame... but it fixes things not appearing at all. gah!
            if (SequenceInstance.headPosition == keyframe.frame + keyframe.length - 1)
            {
                return SequenceManager.KeyframeExecutionState.Start;
            }
            else if (SequenceInstance.headPosition == keyframe.frame)
            {
                return SequenceManager.KeyframeExecutionState.End;
            }
            else if (SequenceInstance.headPosition > keyframe.frame + keyframe.length)
            {
                return SequenceManager.KeyframeExecutionState.NotRunningTooEarly;
            }
            else if (SequenceInstance.headPosition < keyframe.frame)
            {
                return SequenceManager.KeyframeExecutionState.NotRunningTooLate;
            }
            else
            {
                return SequenceManager.KeyframeExecutionState.Running;
            }
        }
    }

    public void ExecuteKeyframe(
        RuntimeKeyframe currentKeyframe,
        RuntimeKeyframe? nextKeyframe,
        UndertaleSequence.Track.TrackBuiltinName trackName,
        TrackType trackType,
        RuntimeTrackInstance trackInstance)
    {
        if (trackName == UndertaleSequence.Track.TrackBuiltinName.Position)
        {
            if (nextKeyframe != null)
            {
                var pos = InterpolateVector2Keyframe(currentKeyframe, nextKeyframe, SequenceInstance.headPosition);
                trackInstance.posx = pos.X;
                trackInstance.posy = pos.Y;
            }
            else
            {
                trackInstance.posx = currentKeyframe.channels.First(x => x.channel == 0).value;
                trackInstance.posy = currentKeyframe.channels.First(x => x.channel == 1).value;
            }
        }
        else if (trackName == UndertaleSequence.Track.TrackBuiltinName.Scale)
        {
            if (nextKeyframe != null)
            {
                var scale = InterpolateVector2Keyframe(currentKeyframe, nextKeyframe, SequenceInstance.headPosition);
                trackInstance.scalex = scale.X;
                trackInstance.scaley = scale.Y;
            }
            else
            {
                trackInstance.scalex = currentKeyframe.channels.First(x => x.channel == 0).value;
                trackInstance.scaley = currentKeyframe.channels.First(x => x.channel == 1).value;
            }
        }
        else if (trackName == UndertaleSequence.Track.TrackBuiltinName.Origin)
        {
            if (nextKeyframe != null)
            {
                var origin = InterpolateVector2Keyframe(currentKeyframe, nextKeyframe, SequenceInstance.headPosition);
                trackInstance.xorigin = origin.X;
                trackInstance.yorigin = origin.Y;
            }
            else
            {
                trackInstance.xorigin = currentKeyframe.channels.First(x => x.channel == 0).value;
                trackInstance.yorigin = currentKeyframe.channels.First(x => x.channel == 1).value;
            }
        }
        else if (trackName == UndertaleSequence.Track.TrackBuiltinName.Rotation)
        {
            if (nextKeyframe != null)
            {
                trackInstance.rotation = InterpolateFloatKeyframe(currentKeyframe, nextKeyframe, SequenceInstance.headPosition);
            }
            else
            {
                trackInstance.rotation = currentKeyframe.channels[0].value;
            }
        }
        else if (trackName == UndertaleSequence.Track.TrackBuiltinName.ImageIndex)
        {
            trackInstance.imageindex = currentKeyframe.channels[0].value;
        }
        else if (trackName == UndertaleSequence.Track.TrackBuiltinName.ImageSpeed)
        {
            trackInstance.imagespeed = currentKeyframe.channels[0].value;
        }
        else if (trackName == UndertaleSequence.Track.TrackBuiltinName.BlendMultiply)
        {
            if (nextKeyframe != null)
            {
                trackInstance.colourmultiply = InterpolateColorKeyframe(currentKeyframe, nextKeyframe, SequenceInstance.headPosition);
            }
            else
            {
                trackInstance.colourmultiply = (int)currentKeyframe.channels[0].value;
            }
        }
        else if (trackName == UndertaleSequence.Track.TrackBuiltinName.Gain)
        {
            if (nextKeyframe != null)
            {
                trackInstance.gain = InterpolateFloatKeyframe(currentKeyframe, nextKeyframe, SequenceInstance.headPosition);
            }
            else
            {
                trackInstance.gain = currentKeyframe.channels[0].value;
            }
        }
        else if (trackName == UndertaleSequence.Track.TrackBuiltinName.Pitch)
        {
            if (nextKeyframe != null)
            {
                trackInstance.pitch = InterpolateFloatKeyframe(currentKeyframe, nextKeyframe, SequenceInstance.headPosition);
            }
            else
            {
                trackInstance.pitch = currentKeyframe.channels[0].value;
            }
        }
        else if (trackName == UndertaleSequence.Track.TrackBuiltinName.CharacterSpacing)
        {
            if (nextKeyframe != null)
            {
                trackInstance.characterSpacing = InterpolateFloatKeyframe(currentKeyframe, nextKeyframe, SequenceInstance.headPosition);
            }
            else
            {
                trackInstance.characterSpacing = currentKeyframe.channels[0].value;
            }
        }
        else if (trackName == UndertaleSequence.Track.TrackBuiltinName.LineSpacing)
        {
            if (nextKeyframe != null)
            {
                trackInstance.lineSpacing = InterpolateFloatKeyframe(currentKeyframe, nextKeyframe, SequenceInstance.headPosition);
            }
            else
            {
                trackInstance.lineSpacing = currentKeyframe.channels[0].value;
            }
        }
        else if (trackName != 0)
        {
            DebugLog.LogWarning($"No case for handling track name {trackName}");
        }
        else
        {
            foreach (var channel in currentKeyframe.channels)
            {
                if (trackType == TrackType.Graphic)
                {
                    var sprite = SpriteManager.GetSpritePageItem(channel.spriteIndex, trackInstance.imageindex);
                    var c = trackInstance.colourmultiply.ABGRToCol4(1);

                    CustomWindow.Draw(new GMSpriteJob()
                    {
                        texture = sprite,
                        screenPos = new Vector2d(X + trackInstance.posx, Y + trackInstance.posy),
                        angle = trackInstance.rotation,
                        scale = new Vector2d(trackInstance.scalex, trackInstance.scaley),
                        Colors = [c, c, c, c],
                        origin = new Vector2(trackInstance.xorigin, trackInstance.yorigin),
                    });
                }
                else if (trackType == TrackType.Instance)
                {
                    var obj = InstanceManager.FindByInstanceId(trackInstance.instanceID, true);

                    if (obj == null)
                    {
                        throw new NotImplementedException();
                    }

                    obj.x = X + trackInstance.posx;
                    obj.y = Y + trackInstance.posy;
                    obj.image_speed = trackInstance.imagespeed;
                    obj.image_index = trackInstance.imageindex;
                    obj.image_blend = trackInstance.colourmultiply;

                    var state = GetKeyframeExecutionState(currentKeyframe);

                    if (state == SequenceManager.KeyframeExecutionState.Start)
                    {
                        // TODO: 1 frame too late... should be active THIS frame
                        obj.NextActive = true;
                    }
                    else if (state == SequenceManager.KeyframeExecutionState.End)
                    {
                        obj.NextActive = false;
                    }
                }
                else if (trackType == TrackType.Audio)
                {
                    SoundFunctions.audio_emitter_gain([trackInstance.emitterIndex, trackInstance.gain]);
                    SoundFunctions.audio_emitter_pitch([trackInstance.emitterIndex, trackInstance.pitch]);

                    var state = GetKeyframeExecutionState(currentKeyframe);

                    if (state == SequenceManager.KeyframeExecutionState.Start)
                    {
                        trackInstance.soundIndex = SoundFunctions.audio_play_sound_on([trackInstance.emitterIndex, channel.soundIndex, false, 0, trackInstance.gain, 0, trackInstance.pitch]).Conv<int>();
                    }
                    else if (state == SequenceManager.KeyframeExecutionState.End)
                    {
                        SoundFunctions.audio_stop_sound([trackInstance.soundIndex]);
                    }
                }
                else if (trackType == TrackType.Text)
                {
                    var c = trackInstance.colormultiply.ABGRToCol4(1);

                    GraphicFunctions.draw_set_font([channel.fontIndex]);

                    CustomWindow.Draw(new GMTextJob()
                    {
                        screenPos = new(X + trackInstance.posx + trackInstance.xorigin, Y + trackInstance.posy - trackInstance.yorigin),
                        asset = TextManager.fontAsset,
                        angle = trackInstance.rotation,
                        Colors = [c, c, c, c],
                        halign = TextManager.halign,
                        valign = TextManager.valign,
                        scale = new(trackInstance.scalex, trackInstance.scaley),
                        lineSep = TextManager.FontHeight() + trackInstance.lineSpacing,
                        text = channel.text
                    });
                }
                else
                {
                    DebugLog.LogWarning($"No track handler for {trackType}");
                }
            }
        }
    }

    public float InterpolateFloatKeyframe(RuntimeKeyframe keyOne, RuntimeKeyframe keyTwo, float headPosition)
    {
        var keyOneTime = keyOne.frame;
        var keyTwoTime = keyTwo.frame;

        if (keyOneTime > keyTwoTime)
        {
            (keyOne, keyTwo) = (keyTwo, keyOne);
        }

        var keyOneVal = keyOne.channels[0].value;
        var keyTwoVal = keyTwo.channels[0].value;

        /*if (headPosition < keyOneTime)
        {
            DebugLog.Log($"Trying to interpolate between keyframes at {keyOneTime} - {keyTwoTime} at position {headPosition}");
            return keyOneVal;
        }*/

        var timeDifference = keyTwoTime - keyOneTime;
        var deltaTime = headPosition - keyOneTime;

        var timeFrac = deltaTime / timeDifference;

        return (keyOneVal * (1 - timeFrac)) + (keyTwoVal * timeFrac);
    }

    public int InterpolateColorKeyframe(RuntimeKeyframe keyOne, RuntimeKeyframe keyTwo, float headPosition)
    {
        var keyOneTime = keyOne.frame;
        var keyTwoTime = keyTwo.frame;

        if (keyOneTime > keyTwoTime)
        {
            (keyOne, keyTwo) = (keyTwo, keyOne);
        }

        var keyOneVal = keyOne.channels[0].value;
        var keyTwoVal = keyTwo.channels[0].value;

        /*if (headPosition < keyOneTime)
        {
            DebugLog.Log($"Trying to interpolate between keyframes at {keyOneTime} - {keyTwoTime} at position {headPosition}");
            return (int)keyOneVal;
        }*/

        var timeDifference = keyTwoTime - keyOneTime;
        var deltaTime = headPosition - keyOneTime;

        var timeFrac = deltaTime / timeDifference;

        //return (keyOneVal * (1 - timeFrac)) + (keyTwoVal * timeFrac);

        return GraphicFunctions.merge_colour([keyOneVal, keyTwoVal, timeFrac]).Conv<int>();
    }

    public Vector2 InterpolateVector2Keyframe(RuntimeKeyframe keyOne, RuntimeKeyframe keyTwo, float headPosition)
    {
        var keyOneTime = keyOne.frame;
        var keyTwoTime = keyTwo.frame;

        if (keyOneTime > keyTwoTime)
        {
            (keyOne, keyTwo) = (keyTwo, keyOne);
        }

        var keyOnePos = new Vector2(keyOne.channels.First(x => x.channel == 0).value, keyOne.channels.First(x => x.channel == 1).value);
        var keyTwoPos = new Vector2(keyTwo.channels.First(x => x.channel == 0).value, keyTwo.channels.First(x => x.channel == 1).value);

        /*if (headPosition < keyOneTime)
        {
            DebugLog.Log($"Trying to interpolate between keyframes at {keyOneTime} - {keyTwoTime} at position {headPosition}");
            return keyOnePos;
        }*/

        var timeDifference = keyTwoTime - keyOneTime;
        var deltaTime = headPosition - keyOneTime;

        var timeFrac = deltaTime / timeDifference;

        return (keyOnePos * (1 - timeFrac)) + (keyTwoPos * timeFrac);
    }

    public override void Destroy()
    {
        DrawManager.Unregister(this);

        foreach (var item in SequenceInstance.activeTracks)
        {
            if (item.instanceID != -1)
            {
                var instance = InstanceManager.FindByInstanceId(item.instanceID, true);
                InstanceManager.MarkForDestruction(instance, true);
            }
        }
    }
}

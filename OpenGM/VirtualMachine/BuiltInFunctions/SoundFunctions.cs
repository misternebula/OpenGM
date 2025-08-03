﻿using OpenGM.IO;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using StbVorbisSharp;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class SoundFunctions
    {
        // MCI_command

        [GMLFunction("audio_listener_position")]
        public static object? audio_listener_position(object?[] args)
        {
            var x = args[0].Conv<float>();
            var y = args[1].Conv<float>();
            var z = args[2].Conv<float>();

            AudioManager.Listener.Position = new Vector3(x, y, z);
            return null;
        }

        // audio_listener_velocity
        // audio_listener_orientation

        [GMLFunction("audio_emitter_position")]
        public static object? audio_emitter_position(object?[] args)
        {
            var id = args[0].Conv<int>();
            var x = args[1].Conv<float>();
            var y = args[2].Conv<float>();
            var z = args[3].Conv<float>();

            var emitter = AudioManager.GetAudioEmitter(id);
            emitter.Position = new(x, y, z);
            return null;
        }

        [GMLFunction("audio_emitter_velocity")]
        public static object? audio_emitter_velocity(object?[] args)
        {
            var id = args[0].Conv<int>();
            var vx = args[1].Conv<float>();
            var vy = args[2].Conv<float>();
            var vz = args[3].Conv<float>();

            var emitter = AudioManager.GetAudioEmitter(id);
            emitter.Velocity = new(vx, vy, vz);
            return null;
        }

        // audio_system

        [GMLFunction("audio_emitter_create")]
        public static object? audio_emitter_create(object?[] args)
        {
            return AudioManager.AudioEmitterCreate();
        }

        // audio_emitter_free

        [GMLFunction("audio_play_sound")]
        public static object? audio_play_sound(object?[] args)
        {
            var index = args[0].Conv<int>();
            var priority = args[1].Conv<double>();
            var loop = args[2].Conv<bool>();
            var asset = AudioManager.GetAudioAsset(index);

            if (asset is null)
            {
                DebugLog.LogWarning($"Tried to play nonexistent sound {index}");
                return -1;
            }

            var gain = asset.Gain;
            var offset = asset.Offset;
            var pitch = asset.Pitch;
            var listener_mask = 0; // TODO : work out what the hell this is for
            if (args.Length > 3)
            {
                gain = args[3].Conv<double>();
            }

            if (args.Length > 4)
            {
                offset = args[4].Conv<double>();
            }

            if (args.Length > 5)
            {
                pitch = args[5].Conv<double>();
            }

            if (args.Length > 6)
            {
                listener_mask = args[6].Conv<int>();
            }

            var ret = AudioManager.audio_play_sound(index, priority, loop, gain, offset, pitch);
            return ret;
        }

        [GMLFunction("audio_play_sound_on")]
        public static object? audio_play_sound_on(object?[] args)
        {
            var emitter = args[0].Conv<int>();
            var sound = args[1].Conv<int>();
            var loop = args[2].Conv<bool>();
            var priority = args[3].Conv<double>();

            var gain = 1f;
            var offset = 0f;
            var pitch = 1f;
            var listener_mask = 1;

            if (args.Length > 4)
            {
                gain = args[4].Conv<float>();
            }

            if (args.Length > 5)
            {
                offset = args[5].Conv<float>();
            }

            if (args.Length > 6)
            {
                pitch = args[6].Conv<float>();
            }

            if (args.Length > 7)
            {
                listener_mask = args[7].Conv<int>();
            }

            return AudioManager.PlaySoundOnEmitter(AudioManager.GetAudioEmitter(emitter), sound, priority, loop, gain, offset, pitch, listener_mask);
        }

        // audio_play_sound_at

        [GMLFunction("audio_falloff_set_model")]
        public static object? audio_falloff_set_model(object?[] args)
        {
            var model = (FalloffModel)args[0].Conv<int>();

            var alModel = model switch
            {
                FalloffModel.NONE => ALDistanceModel.None,
                FalloffModel.INVERSE_DISTANCE => ALDistanceModel.InverseDistance,
                FalloffModel.INVERSE_DISTANCE_CLAMPED => ALDistanceModel.InverseDistanceClamped,
                FalloffModel.LINEAR_DISTANCE => ALDistanceModel.LinearDistance,
                FalloffModel.LINEAR_DISTANCE_CLAMPED => ALDistanceModel.LinearDistanceClamped,
                FalloffModel.EXPONENT_DISTANCE => ALDistanceModel.ExponentDistance,
                FalloffModel.EXPONENT_DISTANCE_CLAMPED => ALDistanceModel.ExponentDistanceClamped,
                FalloffModel.INVERSE_DISTANCE_SCALED => throw new NotImplementedException(),
                FalloffModel.EXPONENT_DISTANCE_SCALED => throw new NotImplementedException(),
                _ => throw new ArgumentOutOfRangeException()
            };

            AL.DistanceModel(alModel);
            AudioManager.CheckALError();
            return null;
        }

        [GMLFunction("audio_stop_sound")]
        public static object? audio_stop_sound(object?[] args)
        {
            var id = args[0].Conv<int>();

            if (id < GMConstants.FIRST_INSTANCE_ID)
            {
                foreach (var item in AudioManager.GetAudioInstances(id))
                {
                    AL.SourceStop(item.Source);
                    AudioManager.CheckALError();
                }
            }
            else
            {
                var soundAsset = AudioManager.GetAudioInstance(id);
                if (soundAsset == null)
                {
                    //DebugLog.LogWarning($"trying to stop sound {id} which does not exist.\n" +
                    //    $"it was probably either done playing or already stopped");
                    return null;
                }
                AL.SourceStop(soundAsset.Source);
                AudioManager.CheckALError();
            }
            AudioManager.Update(); // hack: deletes the sources. maybe make official stop and delete function

            return null;
        }

        [GMLFunction("audio_pause_sound")]
        public static object? audio_pause_sound(object?[] args)
        {
            var index = args[0].Conv<int>();

            if (index < GMConstants.FIRST_INSTANCE_ID)
            {
                foreach (var item in AudioManager.GetAudioInstances(index))
                {
                    AL.SourcePause(item.Source);
                    AudioManager.CheckALError();
                }
            }
            else
            {
                var instance = AudioManager.GetAudioInstance(index);
                if (instance != null)
                {
                    AL.SourcePause(instance.Source);
                    AudioManager.CheckALError();
                }

            }

            return null;
        }

        [GMLFunction("audio_resume_sound")]
        public static object? audio_resume_sound(object?[] args)
        {
            var index = args[0].Conv<int>();

            if (index < GMConstants.FIRST_INSTANCE_ID)
            {
                foreach (var item in AudioManager.GetAudioInstances(index))
                {
                    AL.SourcePlay(item.Source);
                    AudioManager.CheckALError();
                }
            }
            else
            {
                var instance = AudioManager.GetAudioInstance(index);
                if (instance != null)
                {
                    AL.SourcePlay(instance.Source);
                    AudioManager.CheckALError();
                }
            }

            return null;
        }

        [GMLFunction("audio_pause_all")]
        public static object? audio_pause_all(object?[] args)
        {
            foreach (var item in AudioManager.GetAllAudioInstances())
            {
                AL.SourcePause(item.Source);
                AudioManager.CheckALError();
            }

            return null;
        }

        [GMLFunction("audio_resume_all")]
        public static object? audio_resume_all(object?[] args)
        {
            foreach (var item in AudioManager.GetAllAudioInstances())
            {
                AL.SourcePlay(item.Source);
                AudioManager.CheckALError();
            }

            return null;
        }

        [GMLFunction("audio_is_playing")]
        public static object? audio_is_playing(object?[] args)
        {
            var index = args[0].Conv<int>();

            if (index < GMConstants.FIRST_INSTANCE_ID)
            {
                // playing = exists for us, so anything in here means something is playing
                foreach (var item in AudioManager.GetAudioInstances(index))
                {
                    if (item != null)
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                var instance = AudioManager.GetAudioInstance(index);
                return instance != null;
            }
        }

        [GMLFunction("audio_is_paused")]
        public static object? audio_is_paused(object?[] args)
        {
            return !(bool?)audio_is_playing(args);
        }

        [GMLFunction("audio_exists")]
        public static object? audio_exists(object?[] args)
        {
            var index = args[0].Conv<int>();

            if (index < GMConstants.FIRST_INSTANCE_ID)
            {
                return AudioManager.GetAudioInstances(index).Any();
            }
            else
            {
                return AudioManager.GetAudioInstance(index) != null;
            }
        }

        // audio_system_is_available
        // audio_sound_is_playable
        // audio_master_gain
        // audio_emitter_exists
        // audio_get_type
        // audio_emitter_gain
        // audio_emitter_pitch

        [GMLFunction("audio_emitter_falloff")]
        public static object? audio_emitter_falloff(object?[] args)
        {
            var id = args[0].Conv<int>();
            var falloff_ref = args[1].Conv<float>();
            var falloff_max = args[2].Conv<float>();
            var falloff_factor = args[3].Conv<float>();

            var emitter = AudioManager.GetAudioEmitter(id);
            emitter.FalloffRef = falloff_ref;
            emitter.FalloffMax = falloff_max;
            emitter.FalloffFac = falloff_factor;

            foreach (var sound in emitter.AttachedSounds)
            {
                AL.Source(sound.Source, ALSourcef.ReferenceDistance, falloff_ref);
                AudioManager.CheckALError();
                AL.Source(sound.Source, ALSourcef.MaxDistance, falloff_max);
                AudioManager.CheckALError();
                AL.Source(sound.Source, ALSourcef.RolloffFactor, falloff_factor);
                AudioManager.CheckALError();
            }

            return null;
        }

        [GMLFunction("audio_channel_num")]
        public static object? audio_channel_num(object?[] args)
        {
            var num = args[0].Conv<int>();

            AudioManager.StopAllAudio();
            AudioManager.AudioChannelNum = num;
            return null;
        }

        // audio_play_music
        // audio_stop_music
        // audio_pause_music
        // audio_resume_music
        // audio_music_is_playing
        // audio_music_gain

        [GMLFunction("audio_sound_gain")]
        public static object? audio_sound_gain(object?[] args)
        {
            var index = args[0].Conv<int>();
            var volume = args[1].Conv<double>();
            var time = args[2].Conv<double>();

            if (index < 0)
            {
                return null;
            }

            if (index >= GMConstants.FIRST_INSTANCE_ID)
            {
                // instance id
                var soundAsset = AudioManager.GetAudioInstance(index);
                if (soundAsset == null)
                {
                    return null;
                }

                AudioManager.ChangeGain(soundAsset.Source, volume, time);
            }
            else
            {
                // sound asset index
                AudioManager.SetAssetGain(index, volume);

                foreach (var item in AudioManager.GetAudioInstances(index))
                {
                    AudioManager.ChangeGain(item.Source, volume, time);
                }
            }

            return null;
        }

        [GMLFunction("audio_sound_pitch")]
        public static object? audio_sound_pitch(object?[] args)
        {
            var index = args[0].Conv<int>();
            var pitch = args[1].Conv<double>();

            pitch = Math.Clamp(pitch, 1.0 / 256.0, 256.0);

            if (index >= GMConstants.FIRST_INSTANCE_ID)
            {
                // instance id
                var soundAsset = AudioManager.GetAudioInstance(index);
                if (soundAsset == null)
                {
                    return null;
                }

                AL.Source(soundAsset.Source, ALSourcef.Pitch, (float)pitch);
                AudioManager.CheckALError();
            }
            else
            {
                // sound asset index
                AudioManager.SetAssetPitch(index, pitch);

                foreach (var item in AudioManager.GetAudioInstances(index))
                {
                    AL.Source(item.Source, ALSourcef.Pitch, (float)pitch);
                    AudioManager.CheckALError();
                }
            }

            return null;
        }

        [GMLFunction("audio_stop_all")]
        public static object? audio_stop_all(object?[] args)
        {
            AudioManager.StopAllAudio();
            return null;
        }

        [GMLFunction("audio_sound_length")]
        public static object audio_sound_length(object?[] args)
        {
            var index = args[0].Conv<int>();

            if (index < GMConstants.FIRST_INSTANCE_ID)
            {
                var asset = AudioManager.GetAudioAsset(index);
                if (asset != null)
                {
                    return AudioManager.GetClipLength(asset);
                }
                return -1;
            }
            else
            {
                var instance = AudioManager.GetAudioInstance(index);
                if (instance != null)
                {
                    return AudioManager.GetClipLength(instance.Asset);
                }
                return -1;
            }
        }

        // ...

        [GMLFunction("audio_set_master_gain")]
        public static object? audio_set_master_gain(object?[] args)
        {
            var listenerIndex = args[0].Conv<double>(); // deltarune doesnt use other listeners rn so i dont care
            var gain = args[1].Conv<double>();

            gain = Math.Max(0, gain);

            AL.Listener(ALListenerf.Gain, (float)gain);
            AudioManager.CheckALError();
            return null;
        }

        // audio_get_master_gain

        [GMLFunction("audio_sound_get_gain")]
        public static object? audio_sound_get_gain(object?[] args)
        {
            var index = args[0].Conv<int>();

            if (index >= GMConstants.FIRST_INSTANCE_ID)
            {
                var instance = AudioManager.GetAudioInstance(index);

                if (instance == null)
                {
                    return 0;
                }

                AL.GetSource(instance.Source, ALSourcef.Gain, out var gain);
                AudioManager.CheckALError();
                return gain;
            }
            else
            {
                var asset = AudioManager.GetAudioAsset(index);
                return asset?.Gain ?? 0;
            }
        }

        [GMLFunction("audio_sound_get_pitch")]
        public static object? audio_sound_get_pitch(object?[] args)
        {
            var index = args[0].Conv<int>();

            if (index >= GMConstants.FIRST_INSTANCE_ID)
            {
                // instance id
                var soundAsset = AudioManager.GetAudioInstance(index);
                if (soundAsset == null)
                {
                    return null;
                }

                var pitch = (double)AL.GetSource(soundAsset.Source, ALSourcef.Pitch);
                AudioManager.CheckALError();
                return pitch;
            }
            else
            {
                // sound asset index
                return AudioManager.GetAssetPitch(index);
            }
        }

        // audio_get_name

        [GMLFunction("audio_sound_set_track_position")]
        public static object? audio_sound_set_track_position(object?[] args)
        {
            var index = args[0].Conv<int>();
            var time = args[1].Conv<double>();

            if (index < GMConstants.FIRST_INSTANCE_ID)
            {
                AudioManager.SetAssetOffset(index, time);

                // unlike gain and pitch, this doesnt change currently playing instances
            }
            else
            {
                var instance = AudioManager.GetAudioInstance(index);
                if (instance != null)
                {
                    AL.Source(instance.Source, ALSourcef.SecOffset, (float)time);
                    AudioManager.CheckALError();
                }
            }

            return null;
        }

        [GMLFunction("audio_sound_get_track_position")]
        public static object audio_sound_get_track_position(object?[] args)
        {
            var index = args[0].Conv<int>();

            if (index < GMConstants.FIRST_INSTANCE_ID)
            {
                return AudioManager.GetAssetOffset(index);

                // unlike gain and pitch, this doesnt change currently playing instances
            }
            else
            {
                var instance = AudioManager.GetAudioInstance(index);
                if (instance != null)
                {
                    var offset = AL.GetSource(instance!.Source, ALSourcef.SecOffset);
                    AudioManager.CheckALError();
                    return offset;
                }
                return 0;
            }
        }

        [GMLFunction("audio_group_load", GMLFunctionFlags.Stub)]
        public static object audio_group_load(object?[] args)
        {
            // TODO : actually implement this properly? DELTARUNITY doesnt use audio groups or any GM storage files (yet?)
            return true;
        }

        // audio_group_unload

        [GMLFunction("audio_group_is_loaded", GMLFunctionFlags.Stub)]
        public static object audio_group_is_loaded(object?[] args)
        {
            // TODO : actually implement this properly? DELTARUNITY doesnt use audio groups or any GM storage files (yet?)
            return true;
        }

        [GMLFunction("audio_group_load_progress", GMLFunctionFlags.Stub)]
        public static object audio_group_load_progress(object?[] args)
        {
            return 100;
        }

        // audio_group_name
        // audio_group_stop_all

        [GMLFunction("audio_group_set_gain", GMLFunctionFlags.Stub)]
        public static object? audio_group_set_gain(object?[] args)
        {
            // TODO : actually implement this properly? DELTARUNITY doesnt use audio groups or any GM storage files (yet?)
            return null;
        }

        // audio_create_buffer_sound
        // audio_free_buffer_sound
        // audio_create_play_queue
        // audio_free_play_queue
        // audio_queue_sound
        // audio_start_recording
        // audio_stop_recording
        // audio_get_recorder_count

        [GMLFunction("audio_get_recorder_count")]
        public static object? audio_get_recorder_count(object?[] args)
        {
            return AudioManager.GetRecordingDeviceNames().Count();
        }

        // audio_get_recorder_info
        // audio_sound_get_listener_mask
        // audio_sound_set_listener_mask
        // audio_emitter_get_listener_mask
        // audio_emitter_set_listener_mask
        // audio_get_listener_mask
        // audio_set_listener_mask
        // audio_get_listener_info

        [GMLFunction("audio_get_listener_count", GMLFunctionFlags.Stub)]
        public static object? audio_get_listener_count(object?[] args)
        {
            return 0;
        }
        
        // audio_create_sync_group
        // audio_destroy_sync_group
        // audio_play_in_sync_group
        // audio_start_sync_group
        // audio_pause_sync_group
        // audio_resume_sync_group
        // audio_stop_sync_group
        // audio_sync_group_get_track_pos
        // audio_sync_group_debug
        // audio_sync_group_is_playing

        [GMLFunction("audio_create_stream")]
        public static object? audio_create_stream(object?[] args)
        {
            var filename = args[0].Conv<string>();
            filename = Path.Combine(Entry.DataWinFolder, filename);

            var assetName = Path.GetFileNameWithoutExtension(filename);
            var existingIndex = AssetIndexManager.GetIndex(AssetType.sounds, assetName);
            if (existingIndex != -1)
            {
                // happens in deltarune on battle.ogg
                DebugLog.LogWarning($"audio_create_stream on {filename} already registered with index {existingIndex}");
                return existingIndex;
            }

            // this should probably be put in AudioManager
            using var vorbis = Vorbis.FromMemory(File.ReadAllBytes(filename));
            var data = new float[vorbis.StbVorbis.total_samples * vorbis.Channels];
            unsafe
            {
                fixed (float* ptr = data)
                {
                    var realLength = StbVorbis.stb_vorbis_get_samples_float_interleaved(vorbis.StbVorbis, vorbis.Channels, ptr, data.Length);
                    realLength *= vorbis.Channels;
                    if (realLength != data.Length)
                    {
                        DebugLog.LogWarning($"{filename} length {realLength} != {data.Length}");
                    }
                }
            }
            var stereo = vorbis.Channels == 2;
            var freq = vorbis.SampleRate;

            var buffer = AL.GenBuffer();
            AudioManager.CheckALError();
            AL.BufferData(buffer, stereo ? ALFormat.StereoFloat32Ext : ALFormat.MonoFloat32Ext, data, freq);
            AudioManager.CheckALError();

            return AudioManager.RegisterAudioClip(new()
            {
                // RegisterAudioClip sets AssetIndex
                Name = assetName,
                Clip = buffer,
                Gain = 1,
                Pitch = 1,
                Offset = 0,
            });
        }

        [GMLFunction("audio_destroy_stream")]
        public static object? audio_destroy_stream(object?[] args)
        {
            var index = args[0].Conv<int>();
            AudioManager.UnregisterAudio(index);
            return null;
        }

        // audio_debug
    }
}

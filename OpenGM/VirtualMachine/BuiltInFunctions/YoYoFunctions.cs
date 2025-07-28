using System.Globalization;
using OpenGM.Loading;
using MotionTK;
using OpenGM.Rendering;
using OpenTK.Graphics.OpenGL;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class YoYoFunctions
    {
        // ...

        [GMLFunction("get_timer")]
        public static object get_timer(object?[] args)
        {
            return (int)(DateTime.Now - Entry.GameLoadTime).TotalMicroseconds; // TODO : is this floored? i assume it is
        }

        // os_get_config

        // os_get_info

        [GMLFunction("os_get_language", GMLFunctionFlags.Stub)]
        public static object os_get_language(object?[] args)
        {
            var lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            return (lang == "iv") ? "en" : lang; //just in case it returns iv
        }

        [GMLFunction("os_get_region", GMLFunctionFlags.Stub)]
        public static object os_get_region(object?[] args)
        {
            bool invariant = (CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "iv"); //just in case it returns iv
            return ((invariant) ? "GB" : (new RegionInfo(CultureInfo.CurrentCulture.Name).TwoLetterISORegionName));
        }

        // os_request_permission
        // os_check_permision

        [GMLFunction("os_is_paused", GMLFunctionFlags.Stub)]
        public static object os_is_paused(object?[] args)
        {
            return false;
        }

        [GMLFunction("code_is_compiled")]
        public static object code_is_compiled(object?[] args)
        {
            return GameLoader.GeneralInfo.IsYYC;
        }

        // used by PT
        [GMLFunction("switch_get_operation_mode", GMLFunctionFlags.Stub)]
        public static object? switch_get_operation_mode(object?[] args)
        {
            return null;
        }

        // ...

        private static DataSource? VideoDataSource;
        private static VideoPlayback VideoPlayback => VideoDataSource!.VideoPlayback;
        private static AudioPlayback AudioPlayback => VideoDataSource!.AudioPlayback;

        private static int VideoSurface;
        private static int VideoBuffer;
        private static int VideoW;
        private static int VideoH;

        [GMLFunction("video_open")]
        public static object? video_open(object?[] args)
        {
            var path = args[0].Conv<string>();
            VideoDataSource = new DataSource(path);
            VideoDataSource.Play();
            return null;
        }

        // video_close

        [GMLFunction("video_draw")]
        public static object? video_draw(object?[] args)
        {
            if (VideoDataSource == null)
            {
                throw new NotImplementedException();
            }

            var videoStatus = (VideoDataSource.State == PlayState.Playing) ? 0 : -1;

            if (videoStatus != 0)
            {
                // video is paused/stopped

                if (SurfaceManager.surface_exists(VideoSurface))
                {
                    SurfaceManager.FreeSurface(VideoSurface, true);
                }

                VideoSurface = -1;

                if (VideoBuffer >= 0)
                {
                    // buffer_delete
                    var buffer = BufferManager.Buffers[VideoBuffer];
                    buffer.Data = null!; // why
                    BufferManager.Buffers.Remove(VideoBuffer);
                }

                VideoBuffer = -1;
                VideoW = -1;
                VideoH = -1;

                return new int[] { -1, -1, -1 };
            }

            // video is playing

            if (!SurfaceManager.surface_exists(VideoSurface))
            {
                VideoW = 1;
                VideoH = 1;
                // TODO: C++ uses the arguments 1, 1, -1. which is correct?
                VideoSurface = SurfaceManager.CreateSurface(1, 1, 6); // eTextureFormat_A8R8G8B8

                if (VideoBuffer >= 0)
                {
                    // buffer_delete
                    var buffer = BufferManager.Buffers[VideoBuffer];
                    buffer.Data = null!; // why
                    BufferManager.Buffers.Remove(VideoBuffer);
                }

                VideoBuffer = BufferManager.CreateBuffer(4, BufferType.Fixed, 1);
            }

            var yyVideoW = VideoPlayback.Size.Width;
            var yyVideoH = VideoPlayback.Size.Height;

            if (yyVideoW != 0 && yyVideoH != 0)
            {
                if (VideoW != yyVideoW && VideoH != yyVideoH)
                {
                    VideoW = yyVideoW;
                    VideoH = yyVideoH;
                }

                if (SurfaceManager.surface_exists(VideoSurface))
                {
                    SurfaceManager.FreeSurface(VideoSurface, true);
                }

                if (VideoBuffer >= 0)
                {
                    // buffer_delete
                    var buffer = BufferManager.Buffers[VideoBuffer];
                    buffer.Data = null!; // why
                    BufferManager.Buffers.Remove(VideoBuffer);
                }

                // TODO: C++ uses the arguments 1, 1, -1. which is correct?
                VideoSurface = SurfaceManager.CreateSurface(VideoW, VideoH, 6);
                VideoBuffer = BufferManager.CreateBuffer(VideoW * VideoH * 4, BufferType.Fixed, 1);
            }

            var buff = BufferManager.Buffers[VideoBuffer];
            if (!YYVideoDraw(buff, VideoW, VideoH))
            {
                return new int[] { -1, -1, -1 };
            }

            BufferManager.BufferSetSurface(VideoBuffer, VideoSurface, 0);

            return new int[] { 0, VideoSurface, -1 };
        }

        private static bool YYVideoDraw(Buffer buffer, int w, int h)
        {
            // put current frame of the video into the buffer
            // TODO : we could probably simplify this a lot, since motiontk gives us a direct texture which we could copy to the surface.

            VideoDataSource!.Update();

            GL.BindTexture(TextureTarget.Texture2D, VideoPlayback.TextureHandle);
            var pixels = new byte[w * h * 4];
            unsafe
            {
                fixed (byte* ptr = pixels)
                    GL.ReadPixels(0, 0, w, h, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)ptr);
            }
            GL.BindTexture(TextureTarget.Texture2D, 0);

            if (buffer.Data.Length < pixels.Length)
            {
                return false;
            }

            for (var i = 0; i < pixels.Length; i++)
            {
                buffer.Data[i] = pixels[i];
            }

            buffer.UpdateUsedSize(pixels.Length);
            return true;
        }

        // video_set_volume
        // video_pause
        // video_resume
        // video_enable_loop
        // video_seek_to
        // video_get_duration
        // video_get_position
        // video_get_status
        // video_get_format
        // video_is_looping
        // video_get_volume

        // ...
    }
}

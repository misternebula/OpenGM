using OpenGM.IO;
using OpenGM.Rendering;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class CameraFunctions
    {
        // CCameraManager::SetupGMLFunctions

        [GMLFunction("camera_create")]
        public static object? camera_create(object?[] args)
        {
            var cam = CameraManager.CreateCamera();
            return cam.ID;
        }

        [GMLFunction("camera_create_view")]
        public static object? camera_create_view(object?[] args)
        {
            var room_x = args[0].Conv<float>();
            var room_y = args[1].Conv<float>();
            var width = args[2].Conv<float>();
            var height = args[3].Conv<float>();

            var angle = 0f;
            var _object = 0;
            var x_speed = 0f;
            var y_speed = 0f;
            var x_border = 0f;
            var y_border = 0f;

            if (args.Length >= 5)
            {
                angle = args[4].Conv<float>();
            }

            if (args.Length >= 6)
            {
                _object = args[5].Conv<int>();
            }

            if (args.Length >= 7)
            {
                x_speed = args[6].Conv<float>();
            }

            if (args.Length >= 8)
            {
                y_speed = args[7].Conv<float>();
            }

            if (args.Length >= 9)
            {
                x_border = args[8].Conv<float>();
            }

            if (args.Length >= 10)
            {
                y_border = args[9].Conv<float>();
            }

            var cam = CameraManager.CreateCameraView(
                room_x, room_y, 
                width, height, 
                angle, 
                _object, 
                x_speed, y_speed,
                x_border, y_border);

            return cam.ID;
        }

        [GMLFunction("camera_destroy")]
        public static object? camera_destroy(object?[] args)
        {
            var camera_id = args[0].Conv<int>();
            CameraManager.DestroyCamera(camera_id);
            return null;
        }

        // camera_apply
        // camera_get_active
        // camera_get_default
        // camera_set_default
        // camera_set_view_mat
        // camera_set_proj_mat
        // camera_set_update_script
        // camera_set_begin_script
        // camera_set_end_script

        [GMLFunction("camera_set_view_pos")]
        public static object? camera_set_view_pos(object?[] args)
        {
            var camera_id = args[0].Conv<int>();

            var camera = CameraManager.GetCamera(camera_id);
            if (camera == null)
            {
                return null;
            }

            var x = args[1].Conv<float>();
            var y = args[2].Conv<float>();

            camera.ViewX = x;
            camera.ViewY = y;
            camera.Build2DView(camera.ViewX + (camera.ViewWidth / 2), camera.ViewY + (camera.ViewHeight / 2));

            return null;
        }

        [GMLFunction("camera_set_view_size")]
        public static object? camera_set_view_size(object?[] args)
        {
            var camera_id = args[0].Conv<int>();

            var camera = CameraManager.GetCamera(camera_id);
            if (camera == null)
            {
                return null;
            }

            var w = args[1].Conv<float>();
            var h = args[2].Conv<float>();

            camera.ViewWidth = w;
            camera.ViewHeight = h;
            camera.Build2DView(camera.ViewX + (camera.ViewWidth / 2), camera.ViewY + (camera.ViewHeight / 2));

            return null;
        }

        // camera_set_view_speed
        // camera_set_view_border
        // camera_set_view_angle

        [GMLFunction("camera_set_view_target")]
        public static object? camera_set_view_target(object?[] args)
        {
            var camera_id = args[0].Conv<int>();
            var id = args[1].Conv<int>();

            var camera = CameraManager.GetCamera(camera_id);
            if (camera == null)
            {
                return null;
            }

            camera.TargetInstance = id;

            return null;
        }

        // camera_get_view_mat
        // camera_get_proj_mat
        // camera_get_update_script
        // camera_get_begin_script
        // camera_get_end_script

        [GMLFunction("camera_get_view_x")]
        public static object camera_get_view_x(object?[] args)
        {
            var camera_id = args[0].Conv<int>();

            var camera = CameraManager.GetCamera(camera_id);
            if (camera == null)
            {
                DebugLog.LogWarning($"camera_get_view_x : Couldn't find camera with id {camera_id}");
                return -1;
            }

            return camera.ViewX;
        }

        [GMLFunction("camera_get_view_y")]
        public static object camera_get_view_y(object?[] args)
        {
            var camera_id = args[0].Conv<int>();

            var camera = CameraManager.GetCamera(camera_id);
            if (camera == null)
            {
                return -1;
            }

            return camera.ViewY;
        }

        [GMLFunction("camera_get_view_width")]
        public static object camera_get_view_width(object?[] args)
        {
            var camera_id = args[0].Conv<int>();

            var camera = CameraManager.GetCamera(camera_id);
            if (camera == null)
            {
                return -1;
            }

            return camera.ViewWidth;
        }

        [GMLFunction("camera_get_view_height")]
        public static object camera_get_view_height(object?[] args)
        {
            var camera_id = args[0].Conv<int>();

            var camera = CameraManager.GetCamera(camera_id);
            if (camera == null)
            {
                return -1;
            }

            return camera.ViewHeight;
        }

        // camera_get_speed_x
        // camera_get_speed_y
        // camera_get_border_x
        // camera_get_border_y
        // camera_get_view_angle

        [GMLFunction("camera_get_view_target")]
        public static object? camera_get_view_target(object?[] args)
        {
            var camera_id = args[0].Conv<int>();

            var camera = CameraManager.GetCamera(camera_id);
            if (camera == null)
            {
                return null;
            }

            return camera.TargetInstance;
        }

        [GMLFunction("view_get_camera")]
        public static object view_get_camera(object?[] args)
        {
            var view_port = args[0].Conv<int>();

            // TODO : this quirk exists in 2022.500 C++, but is undocumented. does it still happen in latest?
            // also, HTML doesn't do this and just indexes out of bounds
            if (view_port is < 0 or > 7)
            {
                view_port = 0;
            }

            var camera = RoomManager.CurrentRoom.Views[view_port].Camera;

            if (camera == null)
            {
                return -1;
            }

            return camera.ID;
        }

        [GMLFunction("view_get_visible")]
        public static object? view_get_visible(object?[] args)
        {
            var view_port = args[0].Conv<int>();

            if (view_port is < 0 or > 7)
            {
                view_port = 0;
            }

            return RoomManager.CurrentRoom.Views[view_port].Visible;
        }

        [GMLFunction("view_get_xport")]
        public static object? view_get_xport(object?[] args)
        {
            var view_port = args[0].Conv<int>();

            if (view_port is < 0 or > 7)
            {
                view_port = 0;
            }

            return RoomManager.CurrentRoom.Views[view_port].PortPosition.X;
        }

        [GMLFunction("view_get_yport")]
        public static object? view_get_yport(object?[] args)
        {
            var view_port = args[0].Conv<int>();

            if (view_port is < 0 or > 7)
            {
                view_port = 0;
            }

            return RoomManager.CurrentRoom.Views[view_port].PortPosition.Y;
        }

        [GMLFunction("view_get_wport")]
        public static object? view_get_wport(object?[] args)
        {
            var view_port = args[0].Conv<int>();

            if (view_port is < 0 or > 7)
            {
                view_port = 0;
            }

            return RoomManager.CurrentRoom.Views[view_port].PortSize.X;
        }

        [GMLFunction("view_get_hport")]
        public static object? view_get_hport(object?[] args)
        {
            var view_port = args[0].Conv<int>();

            if (view_port is < 0 or > 7)
            {
                view_port = 0;
            }

            return RoomManager.CurrentRoom.Views[view_port].PortSize.Y;
        }

        [GMLFunction("view_get_surface_id")]
        public static object? view_get_surface_id(object?[] args)
        {
            var view_port = args[0].Conv<int>();

            if (view_port is < 0 or > 7)
            {
                view_port = 0;
            }

            return RoomManager.CurrentRoom.Views[view_port].SurfaceId;
        }

        // view_set_camera

        [GMLFunction("view_set_visible")]
        public static object? view_set_visible(object?[] args)
        {
            var view_port = args[0].Conv<int>();
            var visible = args[1].Conv<bool>();

            if (view_port is < 0 or > 7)
            {
                view_port = 0;
            }

            RoomManager.CurrentRoom.Views[view_port].Visible = visible;

            return null;
        }

        [GMLFunction("view_set_xport")]
        public static object? view_set_xport(object?[] args)
        {
            var view_port = args[0].Conv<int>();
            var pos = args[1].Conv<int>();

            if (view_port is < 0 or > 7)
            {
                view_port = 0;
            }

            RoomManager.CurrentRoom.Views[view_port].PortPosition.X = pos;

            return null;
        }

        [GMLFunction("view_set_yport")]
        public static object? view_set_yport(object?[] args)
        {
            var view_port = args[0].Conv<int>();
            var pos = args[1].Conv<int>();

            if (view_port is < 0 or > 7)
            {
                view_port = 0;
            }

            RoomManager.CurrentRoom.Views[view_port].PortPosition.Y = pos;

            return null;
        }

        [GMLFunction("view_set_wport")]
        public static object? view_set_wport(object?[] args)
        {
            var view_port = args[0].Conv<int>();
            var size = args[1].Conv<int>();

            if (view_port is < 0 or > 7)
            {
                view_port = 0;
            }

            RoomManager.CurrentRoom.Views[view_port].PortSize.X = size;

            return null;
        }

        [GMLFunction("view_set_hport")]
        public static object? view_set_hport(object?[] args)
        {
            var view_port = args[0].Conv<int>();
            var size = args[1].Conv<int>();

            if (view_port is < 0 or > 7)
            {
                view_port = 0;
            }

            RoomManager.CurrentRoom.Views[view_port].PortSize.Y = size;

            return null;
        }

        [GMLFunction("view_set_surface_id")]
        public static object? view_set_surface_id(object?[] args)
        {
            var view_port = args[0].Conv<int>();
            var surf = args[1].Conv<int>();

            if (view_port is < 0 or > 7)
            {
                view_port = 0;
            }

            RoomManager.CurrentRoom.Views[view_port].SurfaceId = surf;
            return null;
        }
    }
}

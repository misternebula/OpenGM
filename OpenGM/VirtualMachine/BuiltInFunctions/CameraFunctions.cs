using OpenGM.Rendering;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class CameraFunctions
    {
        // CCameraManager::SetupGMLFunctions

        // camera_create
        // camera_create_view
        // camera_destroy
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

            if (camera_id > 0)
            {
                // TODO : ughhh implement multiple cameras
                throw new NotImplementedException();
            }

            var x = args[1].Conv<double>();
            var y = args[2].Conv<double>();

            CustomWindow.Instance.SetPosition(x, y);

            return null;
        }

        [GMLFunction("camera_set_view_size", GMLFunctionFlags.Stub)]
        public static object? camera_set_view_size(object?[] args)
        {
            return null;
        }

        // camera_set_view_speed
        // camera_set_view_border
        // camera_set_view_angle

        [GMLFunction("camera_set_view_target")]
        public static object? camera_set_view_target(object?[] args)
        {
            var camera_id = args[0].Conv<int>();

            if (camera_id > 0)
            {
                // TODO : ughhh implement multiple cameras
                throw new NotImplementedException();
            }

            var id = args[1].Conv<int>();

            GamemakerObject? instance = null;

            if (id == GMConstants.noone)
            {
                // set view target to no one i guess
            }
            else if (id < GMConstants.FIRST_INSTANCE_ID)
            {
                instance = InstanceManager.FindByAssetId(id).FirstOrDefault();
            }
            else
            {
                instance = InstanceManager.FindByInstanceId(id);
            }

            CustomWindow.Instance.FollowInstance = instance;

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

            if (camera_id > 0)
            {
                // TODO : ughhh implement multiple cameras
                throw new NotImplementedException();
            }

            return CustomWindow.Instance.X;
        }

        [GMLFunction("camera_get_view_y")]
        public static object camera_get_view_y(object?[] args)
        {
            var camera_id = args[0].Conv<int>();

            if (camera_id > 0)
            {
                // TODO : ughhh implement multiple cameras
                throw new NotImplementedException();
            }

            return CustomWindow.Instance.Y;
        }

        [GMLFunction("camera_get_view_width")]
        public static object camera_get_view_width(object?[] args)
        {
            var camera_id = args[0].Conv<int>();

            if (camera_id > 0)
            {
                // TODO : ughhh implement multiple cameras
                throw new NotImplementedException();
            }

            return RoomManager.CurrentRoom.CameraWidth;
        }

        [GMLFunction("camera_get_view_height")]
        public static object camera_get_view_height(object?[] args)
        {
            var camera_id = args[0].Conv<int>();

            if (camera_id > 0)
            {
                // TODO : ughhh implement multiple cameras
                throw new NotImplementedException();
            }

            return RoomManager.CurrentRoom.CameraHeight;
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

            if (camera_id > 0)
            {
                // TODO : ughhh implement multiple cameras
                throw new NotImplementedException();
            }

            // TODO : this can apparently return either an instance id or object index????
            return CustomWindow.Instance.FollowInstance == null ? -1 : CustomWindow.Instance.FollowInstance.instanceId;
        }

        [GMLFunction("view_get_camera")]
        public static object view_get_camera(object?[] args)
        {
            // TODO : ughhh implement multiple cameras
            return 0;
        }

        // view_get_visible
        // view_get_xport
        // view_get_yport
        // view_get_wport
        // view_get_hport
        // view_get_surface_id
        // view_set_camera
        // view_set_visible
        // view_set_xport
        // view_set_yport
        // view_set_wport
        // view_set_hport
        // view_set_surface_id
    }
}

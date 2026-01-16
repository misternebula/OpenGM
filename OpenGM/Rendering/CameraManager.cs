using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.Rendering;
// TODO: actually handle cameras. currently we create one per view and leak
public static class CameraManager

{
    private static int _nextCameraId;
    private static Dictionary<int, Camera> _cameraDict = new();

    public static Camera? GetCamera(int id) => _cameraDict.TryGetValue(id, out var value) ? value : null;

    public static int RegisterCamera(Camera camera)
    {
        camera.ID = _nextCameraId++;
        _cameraDict.Add(camera.ID, camera);
        return camera.ID;
    }

    public static Camera CreateCamera()
    {
        var cam = new Camera();
        RegisterCamera(cam);
        return cam;
    }

    public static Camera CreateCameraView(
        float room_x,
        float room_y,
        float width,
        float height,
        float angle = 0,
        int _object = -1,
        float x_speed = -1,
        float y_speed = -1,
        float x_border = 0,
        float y_border = 0)
    {
        var cam = new Camera { 
            ViewX = room_x, 
            ViewY = room_y,
            ViewWidth = width,
            ViewHeight = height,
            ViewAngle = angle,
            TargetInstance = _object,
            SpeedX = x_speed,
            SpeedY = y_speed,
            BorderX = x_border,
            BorderY = y_border
        };
        RegisterCamera(cam);
        return cam;
    }
}

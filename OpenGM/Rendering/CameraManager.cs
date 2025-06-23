using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.Rendering;
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
}

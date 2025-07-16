using OpenGM.VirtualMachine;

namespace OpenGM.Rendering;
public class Camera
{
    public int ID;

    public float ViewX;
    public float ViewY;
    public float ViewWidth;
    public float ViewHeight;
    public float SpeedX;
    public float SpeedY;
    public float BorderX;
    public float BorderY;
    public float ViewAngle;
    public int TargetInstance = -1;

    public void Update()
    {
        if (TargetInstance < 0)
        {
            return;
        }

        GamemakerObject? instance = null;
        if (TargetInstance < GMConstants.FIRST_INSTANCE_ID)
        {
            instance = InstanceManager.FindByAssetId(TargetInstance).FirstOrDefault();
        }
        else
        {
            instance = InstanceManager.FindByInstanceId(TargetInstance);
        }

        if (instance == null)
        {
            return;
        }

        var halfWidth = ViewWidth / 2;
        var halfHeight = ViewHeight / 2;

        var flooredX = CustomMath.FloorToInt(instance.x);
        var flooredY = CustomMath.FloorToInt(instance.y);

        var l = ViewX;
        var t = ViewY;

        if (2 * BorderX >= ViewWidth)
        {
            l = flooredX - halfWidth;
        }
        else if (flooredX - BorderX < ViewX)
        {
            l = flooredX - BorderX;
        }
        else if (flooredX + BorderX > (ViewX + ViewWidth))
        {
            l = flooredX + BorderX - ViewWidth;
        }

        if (2 * BorderY >= ViewHeight)
        {
            t = flooredY - halfHeight;
        }
        else if (flooredY - BorderY < ViewY)
        {
            t = flooredY - BorderY;
        }
        else if (flooredY + BorderY > (ViewY + ViewHeight))
        {
            t = flooredY + BorderY - ViewHeight;
        }

        // Make sure it does not extend beyond the room
        if (l < 0)
        {
            l = 0;
        }

        if (l + ViewWidth > RoomManager.CurrentRoom.SizeX)
        {
            l = RoomManager.CurrentRoom.SizeX - ViewWidth;
        }

        if (t < 0)
        {
            t = 0;
        }

        if (t + ViewHeight > RoomManager.CurrentRoom.SizeY)
        {
            t = RoomManager.CurrentRoom.SizeY - ViewHeight;
        }

        // Restrict motion speed
        if (SpeedX >= 0)
        {
            if ((l < ViewX) && (ViewX - l > SpeedX))
                l = ViewX - SpeedX;
            if ((l > ViewX) && (l - ViewX > SpeedX))
                l = ViewX + SpeedX;
        }

        if (SpeedY >= 0)
        {
            if ((t < ViewY) && (ViewY - t > SpeedY))
                t = ViewY - SpeedY;
            if ((t > ViewY) && (t - ViewY > SpeedY))
                t = ViewY + SpeedY;
        }

        ViewX = l - ViewWidth;
        ViewY = t - ViewHeight;
    }
}

using OpenGM.IO;
using OpenGM.VirtualMachine;
using OpenTK.Mathematics;

namespace OpenGM.Rendering;
public class Camera
{
    public int ID = -1;

    public float ViewX;
    public float ViewY;
    public float ViewWidth;
    public float ViewHeight;
    public float SpeedX = -1;
    public float SpeedY = -1;
    public float BorderX;
    public float BorderY;
    public float ViewAngle;
    public int TargetInstance = -1;

    // matrices are updated when view pos/size change
    private Matrix4 _projectionMatrix;
    private Matrix4 _viewMatrix;
    private Matrix4 _viewProjectionMatrix;
    private Matrix4 _inverseProjectionMatrix;
    private Matrix4 _inverseViewMatrix;
    private Matrix4 _inverseViewProjectionMatrix;

    public Matrix4 ProjectionMatrix
    {
        get
        {
            if (!SurfaceManager.DrawingToBackbuffer())
            {
                // no flip when drawing to non-backbuffer
                return _projectionMatrix;
            }

            // flip when drawing to backbuffer
            var flipMat = Matrix4.Identity;
            flipMat[1, 1] = -1;

            return _projectionMatrix * flipMat; // TODO right way round?
        }
    }

    public Matrix4 ViewMatrix => _viewMatrix;
    public Matrix4 ViewProjectionMatrix => _viewProjectionMatrix;
    public Matrix4 InverseProjectionMatrix => _inverseProjectionMatrix;
    public Matrix4 InverseViewMatrix => _inverseViewMatrix;
    public Matrix4 InverseViewProjectionMatrix => _inverseViewProjectionMatrix;

    public void SetViewMat(Matrix4 viewMat)
    {
        _viewMatrix = viewMat;
        _inverseViewMatrix = _viewMatrix.Inverted();
        _viewProjectionMatrix = _viewMatrix * _projectionMatrix; // TODO right way round?
        _inverseViewProjectionMatrix = _viewProjectionMatrix.Inverted();
    }

    public void SetProjMat(Matrix4 projMat)
    {
        _projectionMatrix = projMat;
        _inverseProjectionMatrix = _projectionMatrix.Inverted();
        _viewProjectionMatrix = _viewMatrix * _projectionMatrix; // TODO right way round?
        _inverseViewProjectionMatrix = _viewProjectionMatrix.Inverted();
    }

    public Vector3d GetCamPos()
    {
        return new Vector3d(
            _inverseViewMatrix[3, 0],
            _inverseViewMatrix[3, 1],
            _inverseViewMatrix[3, 2]
        );
    }

    public Vector3d GetCamDir()
    {
        return new Vector3d(
            _viewMatrix[0, 2],
            _viewMatrix[1, 2],
            _viewMatrix[2, 2]
        ).Normalized();
    }

    public Vector3d GetCamUp()
    {
        return new Vector3d(
            _viewMatrix[0, 1],
            _viewMatrix[1, 1],
            _viewMatrix[2, 1]
        ).Normalized();
    }

    public Vector3d GetCamRight()
    {
        return new Vector3d(
            _viewMatrix[0, 0],
            _viewMatrix[1, 0],
            _viewMatrix[2, 0]
        ).Normalized();
    }
    
    // for now the matrices are basically unused. we're just using view xywh directly

    /// <summary>
    /// update matrices from view fields (x and y are usually also view fields)
    /// </summary>
    public void Build2DView(float x, float y)
    {
        var pos = new Vector3(x, y, -16000);
        var at = new Vector3(x, y, 0);
        var up = new Vector3((float)Math.Sin(-ViewAngle * CustomMath.Deg2Rad), (float)Math.Cos(-ViewAngle * CustomMath.Deg2Rad), 0);

        var viewMat = Matrix4.LookAt(at, pos, up);
        var projMat = Matrix4.CreateOrthographic(ViewWidth, ViewHeight, 0, 32000);

        SetViewMat(viewMat);
        SetProjMat(projMat);
    }

    /// <summary>
    /// apply camera matrices to uniforms
    /// </summary>
    public void ApplyMatrices()
    {
        // TODO: global view/room extent stuff, used for culling and for tiled stuff
        
        // rn this stuff is used cuz we set view area. this also sets view area. idk if its needed tho
        
        ShaderManager.LoadMatrices(this);
    }

    /// <summary>
    /// update instance following
    /// </summary>
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

        ViewX = l;
        ViewY = t;

        Build2DView(l + halfWidth, t + halfHeight);
    }

    public void Begin()
    {
        // TODO: evnevnet
    }

    public void End()
    {
        // TODO: evnet
    }
}

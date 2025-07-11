using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenTK.Mathematics;
using UndertaleModLib.Models;

namespace OpenGM;
public static class PathManager
{
    public static Dictionary<int, CPath> Paths = new();
    public static int HighestPathIndex;

    public static int PathAdd()
    {
        var number = HighestPathIndex++;
        var name = $"__newpath{number}";

        var path = new CPath(name);
        Paths.Add(number, path);

        return number;
    }

    public static void PathDelete(int index)
    {
        Paths.Remove(index);
    }

    public static void AddPoint(CPath path, float x, float y, float speed)
    {
        var point = new UndertalePath.PathPoint()
        {
            X = x,
            Y = y,
            Speed = speed
        };

        path.points.Add(point);
        path.count++;

        ComputeInternal(path);
    }

    public static void ComputeInternal(CPath path)
    {
        if (path.kind == 1)
        {
            ComputeCurved(path);
        }
        else
        {
            ComputeLinear(path);
        }

        ComputeLength(path);
    }

    public static void ComputeLinear(CPath path)
    {
        path.intcount = 0;
        Array.Resize(ref path.intpoints, path.intcount);

        if (path.count <= 0)
        {
            return;
        }

        for (var i = 0; i < path.count; i++)
        {
            var point = path.points[i];
            AddInternalPoint(path, point.X, point.Y, point.Speed);
        }

        if (path.closed)
        {
            var point = path.points[0];
            AddInternalPoint(path, point.X, point.Y, point.Speed);
        }
    }

    public static void ComputeCurved(CPath path)
    {
        var i = 0;
        var n = 0;
        path.intcount = 0;
        Array.Resize(ref path.intpoints, path.intcount);

        if (path.count <= 0)
        {
            return;
        }

        if (!path.closed)
        {
            AddInternalPoint(path, path.points[0].X, path.points[0].Y, path.points[0].Speed);
        }

        if (path.closed)
        {
            n = path.count - 1;
        }
        else
        {
            n = path.count - 3;
        }

        for (i = 0; i <= n; i++)
        {
            var point1 = path.points[i % path.count];
            var point2 = path.points[(i + 1) % path.count];
            var point3 = path.points[(i + 2) % path.count];
            HandlePiece(path, path.precision,
                (point1.X + point2.X) / 2f, (point1.Y + point2.Y) / 2f, (point1.Speed + point2.Speed) / 2f,
                point2.X, point2.Y, point2.Speed,
                (point2.X + point3.X) / 2f, (point2.Y + point3.Y) / 2f, (point2.Speed + point3.Speed) / 2f
            );
        }

        if (!path.closed)
        {
            AddInternalPoint(path, path.points[path.count - 1].X, path.points[path.count - 1].Y, path.points[path.count - 1].Speed);
        }
        else
        {
            AddInternalPoint(path, path.intpoints[0].X, path.intpoints[0].Y, path.intpoints[0].Speed);
        }
    }

    public static void HandlePiece(CPath path, int precision, float x1, float y1, float s1, float x2, float y2, float s2, float x3, float y3, float s3)
    {
        if (precision == 0)
        {
            return;
        }

        var mx = (x1 + x2 + x2 + x3) / 4f;
        var my = (y1 + y2 + y2 + y3) / 4f;
        var ms = (s1 + s2 + s2 + s3) / 4f;

        if (Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2) > 16.0)
        {
            HandlePiece(path, precision - 1, x1, y1, s1, (x2 + x1) / 2f, (y2 + y1) / 2f, (s2 + s1) / 2f, mx, my, ms);
        }

        AddInternalPoint(path, mx, my, ms);
        if (Math.Pow(x2 - x3, 2) + Math.Pow(y2 - y3, 2) > 16.0)
        {
            HandlePiece(path, precision - 1, mx, my, ms, (x3 + x2) / 2f, (y3 + y2) / 2f, (s3 + s2) / 2f, x3, y3, s3);
        }
    }

    public static void AddInternalPoint(CPath path, float x, float y, float speed)
    {
        path.intcount++;
        Array.Resize(ref path.intpoints, path.intcount);
        var point = new InternalPoint()
        {
            X = x,
            Y = y,
            Speed = speed
        };
        path.intpoints[path.intcount - 1] = point;
    }

    public static void ComputeLength(CPath path)
    {
        path.length = 0;
        if (path.intcount <= 0)
        {
            return;
        }

        static double Sqr(double n) => n * n;

        path.intpoints[0].l = 0;
        for (var i = 1; i < path.intcount; i++)
        {
            path.intpoints[i].l = path.length += (float)Math.Sqrt(Sqr(path.intpoints[i].X - path.intpoints[i - 1].X) + Sqr(path.intpoints[i].Y - path.intpoints[i - 1].Y));
        }
    }

    public static void DrawPath(CPath path, double x, double y, bool absolute)
    {
        var xoff = x;
        var yoff = y;

        var pPos = PathManager.GetPosition(path, 0);
        if (!absolute)
        {
            xoff -= pPos.X;
            yoff -= pPos.Y;
        }
        else
        {
            xoff = 0;
            yoff = 0;
        }

        var maxSteps = CustomMath.RoundToInt(path.length / 4);
        if (maxSteps == 0)
        {
            return;
        }

        var verts = new Vector2d[maxSteps + 1];
        var colors = new Color4[maxSteps + 1];

        var c = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha);

        for (var i = 0; i <= maxSteps; i++)
        {
            pPos = PathManager.GetPosition(path, (float)i / maxSteps);
            verts[i] = new Vector2d(pPos.X + xoff, pPos.Y + yoff);
            colors[i] = c;
        }

        CustomWindow.Draw(new GMLinesJob()
        {
            Vertices = verts,
            Colors = colors
        });
    }

    public static InternalPoint GetPosition(CPath path, float index)
    {
        // easy cases
        if (path.intcount <= 0)
        {
            return new InternalPoint() { X = 0, Y = 0, Speed = 0};
        }

        if ((path.intcount == 1) || (path.length == 0) || (index <= 0))
        {
            return path.intpoints[0]; // Just return the actual point- DO NOT MODIFY!!
        }

        if (index >= 1)
        {
            return path.intpoints[path.intcount - 1]; // Just return the actual point- DO NOT MODIFY!!
        }

        // get the right interval
        var l = path.length * index;
        var pos = 0;

        // MJD = looks slow.... whatever it is...
        // TODO: Use binary search ???
        while ((pos < path.intcount - 2) && (l >= path.intpoints[pos + 1].l))
        {
            pos++;
        }

        // find the right coordinate
        var pNode = path.intpoints[pos];
        l = l - pNode.l;
        var w = path.intpoints[pos + 1].l - pNode.l;

        var returnNode = new InternalPoint() { X = 0, Y = 0, Speed = 100 };

        if (w != 0)
        {
            pos++;
            returnNode.X = pNode.X + l * (path.intpoints[pos].X - pNode.X) / w;
            returnNode.Y = pNode.Y + l * (path.intpoints[pos].Y - pNode.Y) / w;
            returnNode.Speed = pNode.Speed + l * (path.intpoints[pos].Speed - pNode.Speed) / w;
            pNode = returnNode;
        }

        return pNode;
    }

    public static void Clear(CPath path)
    {
        path.points = new List<UndertalePath.PathPoint>();
        path.intpoints = Array.Empty<InternalPoint>();
        path.count = 0;
        path.intcount = 0;
        path.length = 0;
    }

    public static void Reverse(CPath path)
    {
        if (path.count <= 1)
            return;

        path.points.Reverse();
        ComputeInternal(path);
    }
}

public class CPath(string name)
{
    string name = name;

    public int count = 0;
    public List<UndertalePath.PathPoint> points = new();

    public int intcount = 0;
    public InternalPoint[] intpoints = null!;
    
    public int kind = 0;
    public bool closed = true;
    public int precision = 4;
    
    public float length;
    public float time;

    public float XPosition(float position) => PathManager.GetPosition(this, position).X;
    public float YPosition(float position) => PathManager.GetPosition(this, position).Y;
}

public class InternalPoint : UndertalePath.PathPoint
{
    public float l;
}

public enum PathEndAction
{
    path_action_stop,
    path_action_restart,
    path_action_continue,
    path_action_reverse
}
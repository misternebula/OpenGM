using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.IO;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenTK.Mathematics;
using static UndertaleModLib.Compiler.Compiler.Parser.ExpressionConstant;

namespace OpenGM;
public static class PathManager
{
	public static Dictionary<int, CPath> Paths = new();

	public static int PathAdd()
	{
		var number = Paths.Count;
		var name = $"__newpath{number}";

		var path = new CPath(name);
		Paths.Add(number, path);

		return number;
	}

	public static void AddPoint(CPath path, double x, double y, double speed)
	{
		var point = new PathPoint()
		{
			x = x,
			y = y,
			speed = speed
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
			AddInternalPoint(path, point.x, point.y, point.speed);
		}

		if (path.closed)
		{
			var point = path.points[0];
			AddInternalPoint(path, point.x, point.y, point.speed);
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
			AddInternalPoint(path, path.points[0].x, path.points[0].y, path.points[0].speed);
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
				(point1.x + point2.x) / 2.0, (point1.y + point2.y) / 2.0, (point1.speed + point2.speed) / 2.0,
				point2.x, point2.y, point2.speed,
				(point2.x + point3.x) / 2.0, (point2.y + point3.y) / 2.0, (point2.speed + point3.speed) / 2.0
			);
		}

		if (!path.closed)
		{
			AddInternalPoint(path, path.points[path.count - 1].x, path.points[path.count - 1].y, path.points[path.count - 1].speed);
		}
		else
		{
			AddInternalPoint(path, path.intpoints[0].x, path.intpoints[0].y, path.intpoints[0].speed);
		}
	}

	public static void HandlePiece(CPath path, int precision, double x1, double y1, double s1, double x2, double y2, double s2, double x3, double y3, double s3)
	{
		if (precision == 0)
		{
			return;
		}

		var mx = (x1 + x2 + x2 + x3) / 4.0;
		var my = (y1 + y2 + y2 + y3) / 4.0;
		var ms = (s1 + s2 + s2 + s3) / 4.0;

		if (Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2) > 16.0)
		{
			HandlePiece(path, precision - 1, x1, y1, s1, (x2 + x1) / 2.0, (y2 + y1) / 2.0, (s2 + s1) / 2.0, mx, my, ms);
		}

		AddInternalPoint(path, mx, my, ms);
		if (Math.Pow(x2 - x3, 2) + Math.Pow(y2 - y3, 2) > 16.0)
		{
			HandlePiece(path, precision - 1, mx, my, ms, (x3 + x2) / 2.0, (y3 + y2) / 2.0, (s3 + s2) / 2.0, x3, y3, s3);
		}
	}

	public static void AddInternalPoint(CPath path, double x, double y, double speed)
	{
		path.intcount++;
		Array.Resize(ref path.intpoints, path.intcount);
		var point = new InternalPoint()
		{
			x = x,
			y = y,
			speed = speed
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
			path.intpoints[i].l = path.length += (float)Math.Sqrt(Sqr(path.intpoints[i].x - path.intpoints[i - 1].x) + Sqr(path.intpoints[i].y - path.intpoints[i - 1].y));
		}
	}

	public static void DrawPath(CPath path, double x, double y, bool absolute)
	{
		var xoff = x;
		var yoff = y;

		var pPos = PathManager.GetPosition(path, 0);
		if (!absolute)
		{
			xoff -= pPos.x;
			yoff -= pPos.y;
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

		for (var i = 0; i <= maxSteps; i++)
		{
			pPos = PathManager.GetPosition(path, (float)i / maxSteps);
			verts[i] = new Vector2d(pPos.x + xoff, pPos.y + yoff);
		}

		CustomWindow.Draw(new GMLinesJob()
		{
			Vertices = verts,
			blend = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha)
		});
	}

	public static InternalPoint GetPosition(CPath path, double index)
	{
		// easy cases
		if (path.intcount <= 0)
		{
			return new InternalPoint() { x = 0, y = 0, speed = 0};
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

		var returnNode = new InternalPoint() { x = 0, y = 0, speed = 100 };

		if (w != 0)
		{
			pos++;
			returnNode.x = pNode.x + l * (path.intpoints[pos].x - pNode.x) / w;
			returnNode.y = pNode.y + l * (path.intpoints[pos].y - pNode.y) / w;
			returnNode.speed = pNode.speed + l * (path.intpoints[pos].speed - pNode.speed) / w;
			pNode = returnNode;
		}

		return pNode;
	}
}

public class CPath(string name)
{
	string name = name;

	public int count = 0;
	public List<PathPoint> points = new();

	public int intcount = 0;
	public InternalPoint[] intpoints = null!;
	
	public int kind = 0;
	public bool closed = true;
	public int precision = 4;
	
	public float length;
	public float time;

	public double XPosition(double position) => PathManager.GetPosition(this, position).x;
	public double YPosition(double position) => PathManager.GetPosition(this, position).y;
}

public class InternalPoint : PathPoint
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
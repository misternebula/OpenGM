using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.IO;
using OpenGM.Rendering;
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

	public static void AddPoint(CPath path, float x, float y, float speed)
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
			throw new NotImplementedException("AAAAAA CURVED PATH AAAAAAA");
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

	public static void AddInternalPoint(CPath path, float x, float y, float speed)
	{
		path.intcount++;
		var point = new InternalPoint()
		{
			x = x,
			y = y,
			speed = speed
		};
		path.intpoints.Add(point);
	}

	public static void ComputeLength(CPath path)
	{
		path.length = 0;
		if (path.intcount <= 0)
		{
			return;
		}

		static float Sqr(float n) => n * n;

		path.intpoints[0].l = 0;
		for (var i = 1; i < path.intcount; i++)
		{
			path.intpoints[i].l = path.length += (float)Math.Sqrt(Sqr(path.intpoints[i].x - path.intpoints[i - 1].x) + Sqr(path.intpoints[i].y - path.intpoints[i - 1].y));
		}
	}

	public static void DrawPath(CPath path, float x, float y, bool absolute)
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
			blend = SpriteManager.DrawColor.ABGRToCol4(),
			alpha = SpriteManager.DrawAlpha
		});
	}

	public static InternalPoint GetPosition(CPath path, float index)
	{
		if (path.intcount <= 0)
		{
			return new InternalPoint() { x = 0, y = 0, speed = 0};
		}

		if ((path.intcount == 1) || (path.length == 0) || (index <= 0))
		{
			return path.intpoints[0];
		}

		if (index >= 1)
		{
			return path.intpoints[path.intcount - 1];
		}

		var l = path.length * index;
		var pos = 0;

		while ((pos < path.intcount - 2) && (l >= path.intpoints[pos + 1].l))
		{
			pos++;
		}

		// find the right coordinate
		var pNode = path.intpoints[pos];
		l = l - pNode.l;
		var w = path.intpoints[pos + 1].l - pNode.l;

		if (w != 0)
		{
			pos++;
			return new InternalPoint()
			{
				x = pNode.x + l * (path.intpoints[pos].x - pNode.x) / w,
				y = pNode.y + l * (path.intpoints[pos].y - pNode.y) / w,
				speed = pNode.speed + l * (path.intpoints[pos].speed - pNode.speed) / w
			};
		}

		return new InternalPoint() { x = 0, y = 0, speed = 100 };
	}
}

public class CPath(string name)
{
	string name = name;

	public List<PathPoint> points = new();
	public List<InternalPoint> intpoints = new();
	public int count = 0;
	public int kind = 0;
	public bool closed = true;
	public int precision = 4;
	public int intcount = 0;
	public float length;
	public float time;
}

public class PathPoint
{
	public float x;
	public float y;
	public float speed;
}

public class InternalPoint : PathPoint
{
	public float l;
}
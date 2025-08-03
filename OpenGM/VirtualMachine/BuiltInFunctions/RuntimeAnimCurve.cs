using System.Collections;
using UndertaleModLib.Models;

namespace OpenGM.VirtualMachine.BuiltInFunctions;
public class RuntimeAnimCurve : GMLObject
{
    public string name
    {
        get => SelfVariables["name"].Conv<string>();
        set => SelfVariables["name"] = value.Conv<string>();
    }

    public RuntimeAnimCurveChannel[] channels
    {
        get => (RuntimeAnimCurveChannel[])SelfVariables["channels"].Conv<IList>();
        set => SelfVariables["channels"] = value.Conv<IList>();
    }
}

public class RuntimeAnimCurveChannel : GMLObject
{
    public string name
    {
        get => SelfVariables["name"].Conv<string>();
        set => SelfVariables["name"] = value.Conv<string>();
    }

    public UndertaleAnimationCurve.Channel.CurveType type
    {
        get => (UndertaleAnimationCurve.Channel.CurveType)SelfVariables["type"].Conv<int>();
        set
        {
            SelfVariables["type"] = value.Conv<int>();
            IsDirty = true;
        }
    }

    public int iterations
    {
        get => SelfVariables["iterations"].Conv<int>();
        set
        {
            SelfVariables["iterations"] = value.Conv<int>();
            IsDirty = true;
        }
    }

    public RuntimeAnimCurvePoint[] points
    {
        get => (RuntimeAnimCurvePoint[])SelfVariables["points"].Conv<IList>();
        set
        {
            SelfVariables["points"] = value.Conv<IList>();
            IsDirty = true;
        }
    }

    private bool IsDirty;
    private List<RuntimeAnimCurvePoint> CachedPoints = new();

    // https://github.com/YoYoGames/GameMaker-HTML5/blob/0497df1f57c4341f105fa4bec272cc43849f111c/scripts/yyAnimCurve.js#L171
    public float Evaluate(float x)
    {
        if (NeedsRegen())
        {
            UpdateCachedPoints();
        }

        if (x < 0)
        {
            x = 0;
        }

        if (x > 1)
        {
            x = 1;
        }

        var start = 0;
        var end = CachedPoints.Count - 1;
        var mid = end >> 1;

        while (mid != start)
        {
            if (CachedPoints[mid].posx > x)
            {
                end = mid;
            }
            else
            {
                start = mid;
            }

            mid = (start + end) >> 1;
        }

        var x1 = CachedPoints[mid].posx;
        var x2 = CachedPoints[mid + 1].posx;

        if (x1 == x2)   // these two points line up vertically and happen to exactly match the query value
        {
            return CachedPoints[mid].value;
        }

        var val1 = CachedPoints[mid].value;
        var val2 = CachedPoints[mid + 1].value;

        var ratio = (x - x1) / (x2 - x1);
        var val = ((val2 - val1) * ratio) + val1;

        return val;
    }

    public bool NeedsRegen()
    {
        if (CachedPoints.Count == 0)
        {
            return true;
        }
        else
        {
            return IsDirty || points.Any(x => x.IsDirty);
        }
    }

    public void UpdateCachedPoints(bool closed = false, bool clampX = true, bool normalizeY = true)
    {
        CachedPoints = new();

        if (type == UndertaleAnimationCurve.Channel.CurveType.Smooth)
        {
            ComputeCatmullRom(closed, clampX, normalizeY);
        }
        else if ((int)type == 2) // bezier TODO: change this when neb's PR makes it into a release
        {
            ComputeBezier();
        }
        else // linear
        {
            foreach (var point in points)
            {
                CachedPoints.Add(new()
                {
                    posx = point.posx,
                    value = point.value
                });
            }
        }
    }

    // https://github.com/YoYoGames/GameMaker-HTML5/blob/0497df1f57c4341f105fa4bec272cc43849f111c/scripts/yyAnimCurve.js#L265C10-L265C28
    public void ComputeCatmullRom(bool closed, bool clampX, bool normalizeY)
    {
        if (points.Length < 2)
        {
            throw new NotImplementedException();
        }

        var ptCount = points.Length + 2;
        var scale = 1f;
        var offset = 0f;

        var scratchPoints = new float[ptCount * 2];

        if (normalizeY)
        {
            var miny = points.MinBy(x => x.value)!.value;
            var maxy = points.MaxBy(x => x.value)!.value;

            var range = maxy - miny;
            offset = miny;
            if (range > 1.0)
                scale = range;
            var rcp_scale = 1f / scale;
            var ix = 2;

            foreach (var point in points)
            {
                scratchPoints[ix++] = point.posx;
                scratchPoints[ix++] = (point.value - offset) * rcp_scale;
            }
        }
        else
        {
            var ix = 2;

            foreach (var point in points)
            {
                scratchPoints[ix++] = point.posx;
                scratchPoints[ix++] = point.value;
            }
        }

        //double up end points
        scratchPoints[0] = scratchPoints[2];
        scratchPoints[1] = scratchPoints[3];
        var c = ptCount * 2;
        scratchPoints[c - 2] = scratchPoints[c - 4];
        scratchPoints[c - 1] = scratchPoints[c - 3];

        var alpha = 0.5f;
        var end = (closed) ? ptCount : ptCount - 3;

        for (var i = 0; i < end; ++i)
        {
            // Clamp/wrap points
            var i0 = i;
            var i1 = i + 1;
            var i2 = i + 2;
            var i3 = i + 3;

            var p0x = scratchPoints[i0 * 2];
            var p1x = scratchPoints[i1 * 2];
            var p2x = scratchPoints[i2 * 2];
            var p3x = scratchPoints[i3 * 2];

            var p0y = scratchPoints[i0 * 2 + 1];
            var p1y = scratchPoints[i1 * 2 + 1];
            var p2y = scratchPoints[i2 * 2 + 1];
            var p3y = scratchPoints[i3 * 2 + 1];

            var t1 = CatmullRomTime(0, p0x, p0y, p1x, p1y, alpha);
            var t2 = CatmullRomTime(t1, p1x, p1y, p2x, p2y, alpha);
            var t3 = CatmullRomTime(t2, p2x, p2y, p3x, p3y, alpha);

            var step = (t2 - t1) / iterations;
            var minX = p1x;
            var maxX = p2x;

            for (var t = t1; t <= t2; t += step)
            {
                var a1x = (t1 - t) / (t1) * p0x + (t) / (t1) * p1x;
                var a1y = (t1 - t) / (t1) * p0y + (t) / (t1) * p1y;
                var a2x = (t2 - t) / (t2 - t1) * p1x + (t - t1) / (t2 - t1) * p2x;
                var a2y = (t2 - t) / (t2 - t1) * p1y + (t - t1) / (t2 - t1) * p2y;
                var a3x = (t3 - t) / (t3 - t2) * p2x + (t - t2) / (t3 - t2) * p3x;
                var a3y = (t3 - t) / (t3 - t2) * p2y + (t - t2) / (t3 - t2) * p3y;

                var b1x = (t2 - t) / (t2) * a1x + (t) / (t2) * a2x;
                var b1y = (t2 - t) / (t2) * a1y + (t) / (t2) * a2y;

                var b2x = (t3 - t) / (t3 - t1) * a2x + (t - t1) / (t3 - t1) * a3x;
                var b2y = (t3 - t) / (t3 - t1) * a2y + (t - t1) / (t3 - t1) * a3y;

                var cx = (t2 - t) / (t2 - t1) * b1x + (t - t1) / (t2 - t1) * b2x;
                var cy = (t2 - t) / (t2 - t1) * b1y + (t - t1) / (t2 - t1) * b2y;

                //enforce +ve progression in x values (ie only single result for given input h )
                if (clampX)
                {
                    cx = CustomMath.Max(cx, minX);
                    cx = CustomMath.Min(cx, maxX);
                    minX = cx;
                }

                CachedPoints.Add(new()
                {
                    posx = cx,
                    value = (cy * scale) + offset
                });
            }
        }

        if (closed)
        {
            CachedPoints.Add(new()
            {
                posx = points.Last().posx,
                value = points.Last().value
            });
        }
    }

    public float CatmullRomTime(float t, float x1, float y1, float x2, float y2, float alpha)
    {
        var a = (x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1);
        if (a == 0.0)
            a = 0.0001f;

        var b = Math.Sqrt(a);
        var c = (float)Math.Pow(b, alpha);

        return c + t;
    }

    public void ComputeBezier()
    {
        if (points.Length < 2)
        {
            throw new NotImplementedException();
        }

        var _iterations = iterations * 2;
        var iter = 1f / _iterations;
        for (var i = 0; i < points.Length - 1; ++i)
        {
            var pt = points[i];
            var pt1 = points[i + 1];
            var p0x = pt.posx;
            var p0y = pt.value;
            var p1x = pt.posx + pt.BezierX1;
            var p1y = pt.value + pt.BezierY1;
            var p2x = pt1.posx + pt1.BezierX0;
            var p2y = pt1.value + pt1.BezierY0;
            var p3x = pt1.posx;
            var p3y = pt1.value;

            for (var j = 0; j < _iterations; ++j)
            {
                var t = j * iter;
                var t2 = t * t;
                var t3 = t2 * t;
                var mt = 1 - t;
                var mt2 = mt * mt;
                var mt3 = mt2 * mt;
                var vx = (p0x * mt3) + (3 * p1x * mt2 * t) + (3 * p2x * mt * t2) + (p3x * t3);
                var vy = (p0y * mt3) + (3 * p1y * mt2 * t) + (3 * p2y * mt * t2) + (p3y * t3);

                CachedPoints.Add(new()
                {
                    posx = vx,
                    value = vy
                });
            }
        }

        //add the final end point
        var pLast = points.Last();

        CachedPoints.Add(new()
        {
            posx = pLast.posx,
            value = pLast.value
        });
    }
}

public class RuntimeAnimCurvePoint : GMLObject
{
    public float posx
    {
        get => SelfVariables["posx"].Conv<float>();
        set
        {
            SelfVariables["posx"] = value.Conv<float>();
            IsDirty = true;
        }
    }

    public float value
    {
        get =>SelfVariables["posx"].Conv<float>();

        set
        {
            SelfVariables["value"] = value.Conv<float>();
            IsDirty = true;
        }
    }

    public bool IsDirty;

    public float BezierX0;
    public float BezierY0;
    public float BezierX1;
    public float BezierY1;
}

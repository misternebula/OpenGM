using OpenTK.Mathematics;
using System.Collections;

namespace OpenGM.VirtualMachine;

public static partial class ScriptResolver
{
	public static Matrix4 ViewMatrix;
	public static Matrix4 ProjectionMatrix;
	public static Matrix4 WorldMatrix;

	public static object? matrix_get(object?[] args)
	{
		var type = args[0].Conv<int>();
		
		switch (type)
		{
			case 0:
				return MatrixToList(ViewMatrix);
			case 1:
				return MatrixToList(ProjectionMatrix);
			case 2:
				return MatrixToList(WorldMatrix);
			default:
				throw new Exception("Illegal matrix type");
		}
	}

	public static object? matrix_set(object?[] args)
	{
		var type = args[0].Conv<int>();
		var value = ListToMatrix(args[1].Conv<IList>());

		switch (type)
		{
			case 0:
				ViewMatrix = value;
				break;
			case 1:
				ProjectionMatrix = value;
				break;
			case 2:
				WorldMatrix = value;
				break;
			default:
				throw new Exception("Illegal matrix type");
		}

		return null;
	}

	public static object? matrix_build(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var z = args[2].Conv<double>();
		var xrot = args[3].Conv<double>();
		var yrot = args[4].Conv<double>();
		var zrot = args[5].Conv<double>();
		var xscale = args[6].Conv<double>();
		var yscale = args[7].Conv<double>();
		var zscale = args[8].Conv<double>();

		// TODO : there is probably a built in function for this, but i forgor
		// https://github.com/YoYoGames/GameMaker-HTML5/blob/3560546dc80cb3c0ec4627021d720a08fc58a95f/scripts/functions/Function_D3D.js#L839

		xrot = -xrot * CustomMath.Deg2Rad;
		yrot = -yrot * CustomMath.Deg2Rad;
		zrot = -zrot * CustomMath.Deg2Rad;

		var sinx = Math.Sin(xrot);
		var cosx = Math.Cos(xrot);
		var siny = Math.Sin(yrot);
		var cosy = Math.Cos(yrot);
		var sinz = Math.Sin(zrot);
		var cosz = Math.Cos(zrot);

		var sinzsinx = -sinz * -sinx;
		var coszsinx = cosz * -sinx;

		var ret = new double[16];

		ret[0] = ((cosz * cosy) + (sinzsinx * -siny)) * xscale;
		ret[4] = -sinz * cosx * xscale;
		ret[8] = ((cosz * siny) + (sinzsinx * cosy)) * xscale;
		ret[12] = x;

		ret[1] = ((sinz * cosy) + (coszsinx * -siny)) * yscale;
		ret[5] = cosz * cosx * yscale;
		ret[9] = ((sinz * siny) + (coszsinx * cosy)) * yscale;
		ret[13] = y;

		ret[2] = cosx * -siny * zscale;
		ret[6] = sinx * zscale;
		ret[10] = cosx * cosy * zscale;
		ret[14] = z;

		ret[3] = ret[7] = ret[11] = 0.0;
		ret[15] = 1.0;

		return ret;
	}

	public static object? matrix_multiply(object?[] args)
	{
		var matrix1 = ListToMatrix(args[0].Conv<IList>());
		var matrix2 = ListToMatrix(args[1].Conv<IList>());

		return MatrixToList(Matrix4.Mult(matrix1, matrix2));
	}

	public static object? matrix_build_identity(object?[] args)
	{
		return MatrixToList(Matrix4.Identity);
	}

	public static object? matrix_build_lookat(object?[] args)
	{
		var xfrom = (float)args[0].Conv<double>();
		var yfrom = (float)args[1].Conv<double>();
		var zfrom = (float)args[2].Conv<double>();
		var xto = (float)args[3].Conv<double>();
		var yto = (float)args[4].Conv<double>();
		var zto = (float)args[5].Conv<double>();
		var xup = (float)args[6].Conv<double>();
		var yup = (float)args[7].Conv<double>();
		var zup = (float)args[8].Conv<double>();

		return MatrixToList(Matrix4.LookAt(xfrom, yfrom, zfrom, xto, yto, zto, xup, yup, zup));
	}

	public static object? matrix_build_projection_ortho(object?[] args)
	{
		var w = (float)args[0].Conv<double>();
		var h = (float)args[1].Conv<double>();
		var znear = (float)args[2].Conv<double>();
		var zfar = (float)args[3].Conv<double>();

		return MatrixToList(Matrix4.CreateOrthographic(w, h, znear, zfar));
	}

	public static object? matrix_build_projection_perspective(object?[] args)
	{
		var w = (float)args[0].Conv<double>();
		var h = (float)args[1].Conv<double>();
		var znear = (float)args[2].Conv<double>();
		var zfar = (float)args[3].Conv<double>();

		// https://github.com/YoYoGames/GameMaker-HTML5/blob/3560546dc80cb3c0ec4627021d720a08fc58a95f/scripts/Matrix.js#L240

		// TODO - built in function for this?
		// calculate FOV from w/h and near plane?
		// or just easier to do this?

		return new double[16]
		{
			(znear + znear) / w, 0, 0, 0,
			0, (znear + znear) / h, 0, 0,
			0, 0, zfar / (zfar - znear), 1,
			0, 0, -znear * zfar / (zfar - znear), 0
		};
	}

	public static object? matrix_build_projection_perspective_fov(object?[] args)
	{
		var fov = args[0].Conv<double>();
		var aspect = args[1].Conv<double>();
		var znear = (float)args[2].Conv<double>();
		var zfar = (float)args[3].Conv<double>();

		// https://github.com/YoYoGames/GameMaker-HTML5/blob/3560546dc80cb3c0ec4627021d720a08fc58a95f/scripts/Matrix.js#L200

		fov *= CustomMath.Deg2Rad;
		var yScale = 1 / Math.Tan(fov / 2);
		var xScale = yScale / aspect;

		return new double[16]
		{
			xScale, 0, 0, 0,
			0, yScale, 0, 0,
			0, 0, zfar / (zfar - znear), 1,
			0, 0, -znear * zfar / (zfar - znear), 0
		};
	}

	public static object? matrix_transform_vertex(object?[] args)
	{
		var matrix = ListToMatrix(args[0].Conv<IList>());
		var x = (float)args[1].Conv<double>();
		var y = (float)args[2].Conv<double>();
		var z = (float)args[3].Conv<double>();

		var vector = new Vector4(x, y, z, 1); // TODO : double check this is meant to be 1 bc im tired when writing this
		var ret = vector * matrix;
		return new double[] { ret.X, ret.Y, ret.Z };
	}

	public static Matrix4 ListToMatrix(IList list)
	{
		if (list.Count != 16)
		{
			throw new Exception("Array must contain exactly 16 elements");
		}

		var matrix = new Matrix4();
		for (var i = 0; i < 4; i++)
		{
			for (var j = 0; j < 4; j++)
			{
				matrix[i, j] = list[(i * 4) + j].Conv<float>();
			}
		}

		return matrix;
	}

	public static IList MatrixToList(Matrix4 matrix)
	{
		var array = new double[16];

		for (var i = 0; i < 4; i++)
		{
			for (var j = 0; j < 4; j++)
			{
				array[(i * 4) + j] = matrix[i, j];
			}
		}

		return array;
	}
}

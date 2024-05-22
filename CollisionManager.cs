using OpenTK.Mathematics;
using UndertaleModLib.Models;

namespace DELTARUNITYStandalone;

public class BBox
{
	public float left;
	public float right;
	public float top;
	public float bottom;
}

public class ColliderClass
{
	public GamemakerObject GMObject;

	public string spriteAssetName;
	public int collisionMaskIndex;

	private Vector2 _pos => new((float)GMObject.x, (float)-GMObject.y);
	private Vector2 _scale => new((float)GMObject.image_xscale, (float)GMObject.image_yscale);

	public Vector2i Origin;

	public ColliderClass(GamemakerObject obj)
	{
		GMObject = obj;
		Origin = SpriteManager.GetSpriteOrigin(GMObject.sprite_index);
	}

	public Vector4 Margins;

	public BBox BBox => new()
	{
		left = _pos.X + (Margins.X * _scale.X) - (Origin.X * _scale.X),
		top = _pos.Y - (Margins.W * _scale.Y) + (Origin.Y * _scale.Y),
		right = _pos.X + (Margins.Y * _scale.X) - (Origin.X * _scale.X),
		bottom = _pos.Y - (Margins.Z * _scale.Y) + (Origin.Y * _scale.Y)
	};

	public Vector3 BBCenter => new(
		(BBox.left + BBox.right) / 2,
		(BBox.top + BBox.bottom) / 2,
		0);

	public Vector3 BBSize => new(
		BBox.right - BBox.left,
		BBox.top - BBox.bottom,
		1);

	/// <summary>
	/// The position of the collider, in Unity space (up = +y)
	/// </summary>
	public Vector3 Position => new(
		_pos.X - (Origin.X * _scale.X),
		_pos.Y + (Origin.Y * _scale.Y),
		0);

	public Vector2 Scale => _scale;

	public UndertaleSprite.SepMaskType SepMasks;
	public uint BoundingBoxMode;
	public bool[,] CollisionMask;

	public bool[,] CachedRotatedMask = null;
	public Vector2i CachedRotatedMaskOffset;
}

public static class CollisionManager
{
	private static List<ColliderClass> colliders = new();

	public static void UpdateRotationMask(GamemakerObject obj)
	{
		var collider = colliders.Single(x => x.GMObject == obj);

		if (collider.SepMasks != UndertaleSprite.SepMaskType.Precise)
		{
			return;
		}

		(collider.CachedRotatedMask, collider.CachedRotatedMaskOffset) = RotateMask(collider.CollisionMask, collider.GMObject.image_angle, collider.Origin.X, collider.Origin.Y, collider.Scale.X, collider.Scale.Y);
	}

	public static (bool[,] buffer, Vector2i topLeftOffset) RotateMask(bool[,] mask, double angle, int pivotX, int pivotY, double xScale, double yScale)
	{
		/*
		 * Nearest-Neighbour algorithm for rotating a collision mask.
		 * Assume that the given mask is positioned at (0, 0), and that the given pivot is relative to (0, 0).
		 * We need to return the rotated mask in a new buffer, and where the top left of the new mask is, relative to (0, 0).
		 */

		var sin = Math.Sin(CustomMath.Deg2Rad * angle);
		var cos = Math.Cos(CustomMath.Deg2Rad * angle);

		var maskWidth = mask.GetLength(1);
		var maskHeight = mask.GetLength(0);

		void RotateAroundPoint(int pivotX, int pivotY, bool reverse, double x, double y, out float rotatedX, out float rotatedY)
		{
			x -= pivotX;
			y -= pivotY;

			var useSin = sin;
			if (reverse)
			{
				useSin = -sin;
			}

			var xnew = (x * cos) - (y * useSin);
			var ynew = (x * useSin) + (y * cos);

			rotatedX = (float)xnew + pivotX;
			rotatedY = (float)ynew + pivotY;
		}

		// Calculate where the corners of the given mask will be when rotated.
		RotateAroundPoint(pivotX, pivotY, false, 0, 0, out var newTLx, out var newTLy);
		RotateAroundPoint(pivotX, pivotY, false, (int)(maskWidth * xScale), 0, out var newTRx, out var newTRy);
		RotateAroundPoint(pivotX, pivotY, false, 0, -(int)(maskHeight * yScale), out var newBLx, out var newBLy);
		RotateAroundPoint(pivotX, pivotY, false, (int)(maskWidth * xScale), -(int)(maskHeight * yScale), out var newBRx, out var newBRy);

		// Calculate where the edges of the bounding box will be.
		var fMinX = CustomMath.Min(newTLx, newTRx, newBLx, newBRx);
		var fMaxX = CustomMath.Max(newTLx, newTRx, newBLx, newBRx);
		var fMinY = CustomMath.Min(newTLy, newTRy, newBLy, newBRy);
		var fMaxY = CustomMath.Max(newTLy, newTRy, newBLy, newBRy);

		// Get the minimum-sized bounding box in pixels.
		var iMinX = CustomMath.FloorToInt(fMinX);
		var iMaxX = CustomMath.CeilToInt(fMaxX);
		var iMinY = CustomMath.FloorToInt(fMinY);
		var iMaxY = CustomMath.CeilToInt(fMaxY);

		var bbWidth = iMaxX - iMinX;
		var bbHeight = iMaxY - iMinY;
		var returnBuffer = new bool[bbHeight, bbWidth];

		// For each pixel in the return buffer...
		for (var row = 0; row < bbHeight; row++)
		{
			for (var col = 0; col < bbWidth; col++)
			{
				// Get the position of the center of this pixel
				var pixelCenterX = iMinX + col + 0.5f;
				var pixelCenterY = iMaxY - row - 0.5f;

				// Rotate the center position backwards around the pivot to get a position in the original mask.
				RotateAroundPoint(pivotX, pivotY, true, pixelCenterX, pixelCenterY, out var centerRotatedX, out var centerRotatedY);

				// account for scaling
				var vectorX = centerRotatedX - pivotX;
				var vectorY = centerRotatedY - pivotY;
				vectorX /= (float)xScale;
				vectorY /= (float)yScale;

				centerRotatedX = vectorX;
				centerRotatedY = vectorY;

				centerRotatedX += pivotX / (float)xScale;
				centerRotatedY += pivotY / (float)yScale;

				// Force this position to be an (int, int), so we can sample the original mask.
				var snappedToGridX = CustomMath.FloorToInt(centerRotatedX);
				var snappedToGridY = CustomMath.CeilToInt(centerRotatedY);

				if (snappedToGridX < 0 ||
				    snappedToGridX > maskWidth - 1 ||
				    snappedToGridY > 0 ||
				    -snappedToGridY > maskHeight - 1)
				{
					// Sampling position is outside the original mask.
					continue;
				}

				try
				{
					returnBuffer[row, col] = mask[-snappedToGridY, snappedToGridX];
				}
				catch (IndexOutOfRangeException e)
				{
					DebugLog.LogError($"Mask size : ({mask.GetLength(0)}, {mask.GetLength(1)}) -snappedToGrid.y:{-snappedToGridY} snappedToGrid.x:{snappedToGridX}");
				}
			}
		}

		return (returnBuffer, new Vector2i(iMinX, iMaxY));
	}
}

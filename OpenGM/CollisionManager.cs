﻿using OpenGM.IO;
using OpenGM.Rendering;
using OpenGM.VirtualMachine;
using OpenTK.Mathematics;
using System.Collections;
using System.Drawing;
using UndertaleModLib.Models;

namespace OpenGM;

public class BBox
{
	/// <summary>
	/// X co-ordinate of the left-hand side of the bounding box.
	/// </summary>
	public double left;

	/// <summary>
	/// X co-ordinate of the right-hand side of the bounding box.
	/// </summary>
	public double right;

	/// <summary>
	/// Y co-ordinate of the top of the bounding box.
	/// </summary>
	public double top;

	/// <summary>
	/// Y co-ordinate of the top of the bounding box.
	/// </summary>
	public double bottom;
}

public class ColliderClass
{
	public GamemakerObject GMObject;

	public string spriteAssetName = null!;
	public int collisionMaskIndex;

	public Vector2i Origin;

	public ColliderClass(GamemakerObject obj)
	{
		GMObject = obj;
		Origin = SpriteManager.GetSpriteOrigin(GMObject.sprite_index);
	}

	public Vector4 Margins;

	public BBox BBox => CollisionManager.CalculateBoundingBox(GMObject);

	public Vector2d Scale => new(GMObject.image_xscale, GMObject.image_yscale);

	public UndertaleSprite.SepMaskType SepMasks;
	public int BoundingBoxMode;
	public bool[,] CollisionMask = null!;

	public bool[,] CachedRotatedMask = null!;
	public Vector2i CachedRotatedMaskOffset;
}

public static class CollisionManager
{
	public static List<ColliderClass> colliders = new();

	public static BBox CalculateBoundingBox(GamemakerObject gm)
	{
		// TODO : This is called a LOT. This needs to be as optimized as possible.

		var pos = new Vector2d(gm.x, gm.y);
		var origin = SpriteManager.GetSpriteOrigin(gm.sprite_index);

		var left = pos.X + (gm.margins.X * gm.image_xscale) - (origin.X * gm.image_xscale);
		var top = pos.Y + (gm.margins.W * gm.image_yscale) - (origin.Y * gm.image_yscale);
		var right = pos.X + ((gm.margins.Y + 1) * gm.image_xscale) - (origin.X * gm.image_xscale);
		var bottom = pos.Y + ((gm.margins.Z + 1) * gm.image_yscale) - (origin.Y * gm.image_yscale);

		// Dont bother rotating if not needed
		if (CustomMath.ApproxEqual(gm.image_angle % 360, 0))
		{
			return new BBox()
			{
				left = left,
				top = top,
				right = right,
				bottom = bottom
			};
		}

		// co-ords of verts of unrotated bbox
		var topLeft = new Vector2d(left, top);
		var topRight = new Vector2d(right, top);
		var bottomRight = new Vector2d(right, bottom);
		var bottomLeft = new Vector2d(left, bottom);

		// rotate co-ords
		topLeft = topLeft.RotateAroundPoint(pos, gm.image_angle);
		topRight = topRight.RotateAroundPoint(pos, gm.image_angle);
		bottomRight = bottomRight.RotateAroundPoint(pos, gm.image_angle);
		bottomLeft = bottomLeft.RotateAroundPoint(pos, gm.image_angle);

		return new BBox
		{
			left = CustomMath.Min(topLeft.X, topRight.X, bottomRight.X, bottomLeft.X),
			right = CustomMath.Max(topLeft.X, topRight.X, bottomRight.X, bottomLeft.X),
			top = CustomMath.Min(topLeft.Y, topRight.Y, bottomRight.Y, bottomLeft.Y),
			bottom = CustomMath.Max(topLeft.Y, topRight.Y, bottomRight.Y, bottomLeft.Y)
		};
	}

	/// <summary>
	/// Checks the collision of a single object at a certain position. 
	/// </summary>
	public static bool CheckColliderAgainstPoint(ColliderClass col, Vector2 position, bool precise)
	{
		if (col.SepMasks == UndertaleSprite.SepMaskType.AxisAlignedRect)
		{
			// Easiest collision. "precise" does not affect anything. Just check if pixel is inside bounding box.

			return position.X < col.BBox.right
			       && position.X > col.BBox.left
			       && position.Y < col.BBox.bottom
			       && position.Y > col.BBox.top;
		}
		else if (col.SepMasks == UndertaleSprite.SepMaskType.RotatedRect)
		{
			// Check inside rotated bounding box. "precise" does not affect anything.

			throw new NotImplementedException();

			// return false;
		}
		else
		{
			// Precise collision, my behated. If "precise" is true, we have to rotate the collision mask and other funky stuff.
			// If "precise" is false, then this is the same as AxisAlignedRect.

			throw new NotImplementedException();

			// return false;
		}
	}

	public static bool CheckColliderAgainstRectangle(ColliderClass collider, Vector2d v1, Vector2d v2, bool precise)
	{
		// Collisions here have to cover the center of pixels

		var left = v1.X;
		var top = v1.Y;
		var right = v2.X;
		var bottom = v2.Y;

		var boundingBoxesCollide = left < collider.BBox.right - 0.5 
		                           && right > collider.BBox.left + 0.5
		                           && top < collider.BBox.bottom - 0.5
		                           && bottom > collider.BBox.top + 0.5;

		if (ScriptResolver.DrawCollisionChecks)
		{
			var color = Color4.Red;
			var outline = true;
			if (boundingBoxesCollide)
			{
				color = Color4.Green;
				outline = false;
			}

			CustomWindow.DebugJobs.Add(new GMPolygonJob()
			{
				alpha = 1,
				blend = color,
				Outline = outline,
				Vertices = new Vector2d[]
				{
					new(collider.BBox.left, collider.BBox.top),
					new(collider.BBox.right, collider.BBox.top),
					new(collider.BBox.right, collider.BBox.bottom),
					new(collider.BBox.left, collider.BBox.bottom)
				}
			});

			CustomWindow.DebugJobs.Add(new GMPolygonJob()
			{
				alpha = 1,
				blend = color,
				Outline = outline,
				Vertices = new Vector2d[]
				{
					new(left, top),
					new(right, top),
					new(right, bottom),
					new(left, bottom)
				}
			});
		}

		if (collider.SepMasks == UndertaleSprite.SepMaskType.AxisAlignedRect)
		{
			// Easiest collision. "precise" does not affect anything. Just check if rectangles intersect..

			return boundingBoxesCollide;
		}
		else if (collider.SepMasks == UndertaleSprite.SepMaskType.RotatedRect)
		{
			// Check inside rotated bounding box. "precise" does not affect anything.

			// TODO: this seems to be placed incorrectly? double check this math

			if (!boundingBoxesCollide)
			{
				return false;
			}

			// -- GET CORNERS OF ROTATED BOUNDING BOX --

			var gm = collider.GMObject;
			var pos = new Vector2d(gm.x, gm.y);
			var origin = SpriteManager.GetSpriteOrigin(gm.sprite_index);

			var bleft = pos.X + (gm.margins.X * gm.image_xscale) - (origin.X * gm.image_xscale);
			var btop = pos.Y + (gm.margins.W * gm.image_yscale) - (origin.Y * gm.image_yscale);
			var bright = pos.X + ((gm.margins.Y + 1) * gm.image_xscale) - (origin.X * gm.image_xscale);
			var bbottom = pos.Y + ((gm.margins.Z + 1) * gm.image_yscale) - (origin.Y * gm.image_yscale);

			// co-ords of verts of unrotated bbox
			var bbv1 = new Vector2d(bleft, btop);
			var bbv2 = new Vector2d(bright, btop);
			var bbv3 = new Vector2d(bright, bbottom);
			var bbv4 = new Vector2d(bleft, bbottom);

			// rotate co-ords
			bbv1 = bbv1.RotateAroundPoint(pos, gm.image_angle);
			bbv2 = bbv2.RotateAroundPoint(pos, gm.image_angle);
			bbv3 = bbv3.RotateAroundPoint(pos, gm.image_angle);
			bbv4 = bbv4.RotateAroundPoint(pos, gm.image_angle);

			// -- GET CORNERS OF RECTANGLE --
			var rv1 = v1;
			var rv2 = new Vector2d(v2.X, v1.Y);
			var rv3 = v2;
			var rv4 = new Vector2d(v1.X, v2.Y);

			// -- GET NORMALS --

			var rNormals = new Vector2[] { new(0, -1), new(1, 0), new(0, 1), new(-1, 0) };

			var bbv12 = Vector2d.Normalize(bbv2 - bbv1);
			var bbv23 = Vector2d.Normalize(bbv3 - bbv2);
			var bbv34 = Vector2d.Normalize(bbv4 - bbv3);
			var bbv41 = Vector2d.Normalize(bbv1 - bbv4);

			var bbNormals = new Vector2d[] { new(bbv12.Y, -bbv12.X), new(bbv23.Y, -bbv23.X), new(bbv34.Y, -bbv34.X), new(bbv41.Y, -bbv41.X) };

			// https://gamedev.stackexchange.com/a/60225

			static void SATtest(Vector2d axis, Vector2d[] verts, out double minAlong, out double maxAlong)
			{
				minAlong = double.MaxValue;
				maxAlong = -double.MaxValue;
				foreach (var vert in verts)
				{
					var dotVal = Vector2d.Dot(vert, axis);
					if (dotVal < minAlong)
					{
						minAlong = dotVal;
					}
					if (dotVal > maxAlong)
					{
						maxAlong = dotVal;
					}
				}
			}

			static bool overlaps(double min1, double max1, double min2, double max2)
			{
				return isBetweenOrdered(min2, min1, max1) || isBetweenOrdered(min1, min2, max2);
			}

			static bool isBetweenOrdered(double val, double lowerBound, double upperBound)
			{
				return lowerBound <= val && val <= upperBound;
			}

			var rVerts = new Vector2d[] { rv1, rv2, rv3, rv4 };
			var bbVerts = new Vector2d[] { bbv1, bbv2, bbv3, bbv4 };

			foreach (var norm in rNormals)
			{
				SATtest(norm, rVerts, out var shape1Min, out var shape1Max);
				SATtest(norm, bbVerts, out var shape2Min, out var shape2Max);
				if (!overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
				{
					return false;
				}
			}

			foreach (var norm in bbNormals)
			{
				SATtest(norm, rVerts, out var shape1Min, out var shape1Max);
				SATtest(norm, bbVerts, out var shape2Min, out var shape2Max);
				if (!overlaps(shape1Min, shape1Max, shape2Min, shape2Max))
				{
					return false;
				}
			}

			return true;
		}
		else
		{
			// Precise collision, my behated. If "precise" is true, we have to rotate the collision mask and other funky stuff.
			// If "precise" is false, then this is the same as AxisAlignedRect.

			if (!precise)
			{
				return boundingBoxesCollide;
			}

			if (!boundingBoxesCollide)
			{
				return false;
			}

			var checkRotatedMask = collider.CachedRotatedMask;
			var checkOffset = collider.CachedRotatedMaskOffset;
			var checkMaskHeight = checkRotatedMask.GetLength(0);
			var checkMaskLength = checkRotatedMask.GetLength(1);

			// iterate through every pixel in the object's rotated mask
			for (var row = 0; row < checkMaskHeight; row++)
			{
				for (var col = 0; col < checkMaskLength; col++)
				{
					// if it's false, dont even both checking the position
					if (checkRotatedMask[row, col] == false)
					{
						continue;
					}

					// Get the world space position of the center of this pixel
					var currentPixelPos = new Vector2d(
						collider.GMObject.x + checkOffset.X + col + 0.5f,
						collider.GMObject.y + checkOffset.Y + row + 0.5f
						);

					// Check if position is inside rectangle
					if (currentPixelPos.X < right
					    && currentPixelPos.X > left
					    && currentPixelPos.Y > top
					    && currentPixelPos.Y < bottom)
					{
						return true;
					}
				}
			}

			return false;
		}
	}

	public static bool CheckColliderAgainstCollider(ColliderClass a, ColliderClass b)
	{
		var boxesOverlap = DoBoxesOverlap(a, b);

		if (ScriptResolver.DrawCollisionChecks)
		{
			var color = Color4.Red;
			var outline = true;
			if (boxesOverlap)
			{
				color = Color4.Green;
				outline = false;
			}

			CustomWindow.DebugJobs.Add(new GMPolygonJob()
			{
				alpha = 1,
				blend = color,
				Outline = outline,
				Vertices = new Vector2d[]
				{
					new(a.BBox.left, a.BBox.top),
					new(a.BBox.right, a.BBox.top),
					new(a.BBox.right, a.BBox.bottom),
					new(a.BBox.left, a.BBox.bottom)
				}
			});

			CustomWindow.DebugJobs.Add(new GMPolygonJob()
			{
				alpha = 1,
				blend = color,
				Outline = outline,
				Vertices = new Vector2d[]
				{
					new(b.BBox.left, b.BBox.top),
					new(b.BBox.right, b.BBox.top),
					new(b.BBox.right, b.BBox.bottom),
					new(b.BBox.left, b.BBox.bottom)
				}
			});
		}

		// TODO : This feels like RotatedRect should be counted as precise, but the docs are vauge. Check in GameMaker.

		if ((a.SepMasks == UndertaleSprite.SepMaskType.Precise && b.SepMasks == UndertaleSprite.SepMaskType.Precise)
		    || a.SepMasks != b.SepMasks) // TODO: what the fuck is this, why is this here
		{
			// check precise collision masks

			if (!boxesOverlap)
			{
				return false;
			}

			if (a.CachedRotatedMask == null)
			{
				(a.CachedRotatedMask, a.CachedRotatedMaskOffset) = RotateMask(a.CollisionMask, a.GMObject.image_angle, a.Origin.X, a.Origin.Y, a.Scale.X, a.Scale.Y);
			}

			if (b.CachedRotatedMask == null)
			{
				(b.CachedRotatedMask, b.CachedRotatedMaskOffset) = RotateMask(b.CollisionMask, b.GMObject.image_angle, b.Origin.X, b.Origin.Y, b.Scale.X, b.Scale.Y);
			}

			var currentRotatedMask = a.CachedRotatedMask;
			var currentOffset = a.CachedRotatedMaskOffset;
			var checkRotatedMask = b.CachedRotatedMask;
			var checkOffset = b.CachedRotatedMaskOffset;

			var currentMaskHeight = currentRotatedMask.GetLength(0);
			var currentMaskLength = currentRotatedMask.GetLength(1);
			var checkMaskHeight = checkRotatedMask.GetLength(0);
			var checkMaskLength = checkRotatedMask.GetLength(1);

			// iterate through every pixel in the current object's rotated mask
			for (var row = 0; row < currentMaskHeight; row++)
			{
				for (var col = 0; col < currentMaskLength; col++)
				{
					// if it's false, dont even both checking the value of the other mask
					if (currentRotatedMask[row, col] == false)
					{
						continue;
					}

					// Get the world space position of the center of this pixel
					var currentPixelPos = new Vector2d(a.GMObject.x + currentOffset.X + col + 0.5f, a.GMObject.y + currentOffset.Y + row + 0.5f);

					// Get the world space position of the top-left of the other rotated mask
					var checkMaskTopLeft = new Vector2d(b.GMObject.x + checkOffset.X, b.GMObject.y + checkOffset.Y);

					var placeInOtherMask = currentPixelPos - checkMaskTopLeft;

					var snappedToGrid = new Vector2i((int)Math.Floor(placeInOtherMask.X), (int)Math.Floor(placeInOtherMask.Y));

					if (snappedToGrid.X < 0 || snappedToGrid.X >= checkMaskLength)
					{
						continue;
					}

					if (snappedToGrid.Y < 0 || snappedToGrid.Y >= checkMaskHeight)
					{
						continue;
					}

					if (checkRotatedMask[snappedToGrid.Y, snappedToGrid.X])
					{
						return true;
					}
				}
			}

			return false;
		}
		else
		{
			// check bounding boxes
			return boxesOverlap;
		}
	}

	public static bool CheckColliderAgainstLine(ColliderClass col, Vector2d start, Vector2d end, bool precise)
	{
		// Simple checks - check if line entirely outside bbox

		if ((start.Y < col.BBox.top && end.Y < col.BBox.top) // line is entirely above bbox
			|| (start.Y > col.BBox.bottom && end.Y > col.BBox.bottom) // line is entirely below bbox
			|| (start.X < col.BBox.left && end.X < col.BBox.left) // line is entirely to the left of bbox
		    || (start.X > col.BBox.right && end.X > col.BBox.right)) // line is entirely to the right of bbox
		{
			return false;
		}

		// Line *could* intersect bbox. Test if it does with the slab method

		var low = new Vector2d(col.BBox.left, col.BBox.top);
		var high = new Vector2d(col.BBox.right, col.BBox.bottom);

		var rayDirection = end - start;

		var xlow = (low.X - start.X) / rayDirection.X;
		var ylow = (low.Y - start.Y) / rayDirection.Y;
		var xhigh = (high.X - start.X) / rayDirection.X;
		var yhigh = (high.Y - start.Y) / rayDirection.Y;

		var xclose = CustomMath.Min(xlow, xhigh);
		var yclose = CustomMath.Min(ylow, yhigh);
		var xfar = CustomMath.Max(xlow, xhigh);
		var yfar = CustomMath.Max(ylow, yhigh);

		var close = CustomMath.Max(xclose, yclose);
		var far = CustomMath.Max(xfar, yfar);

		var intersectsWithBBox = close <= far && (close >= 0);

		if (!intersectsWithBBox)
		{
			return false;
		}

		DebugLog.Log($"Collision detected");

		return true;
	}

	public static void RoomChange()
	{
		colliders = colliders.Where(x => x.GMObject != null && x.GMObject.persistent).ToList();
	}

	public static void RegisterCollider(GamemakerObject sprite, Vector4 margins)
	{
		var spriteAsset = sprite.mask_id == -1
			? SpriteManager.GetSpriteAsset(sprite.sprite_index)
			: SpriteManager.GetSpriteAsset(sprite.mask_id);

		if (spriteAsset == null)
		{
			DebugLog.LogError($"Couldn't find sprite for {(sprite.mask_id == -1 ? sprite.sprite_index : sprite.mask_id)}! (for obj {sprite.object_index})");
			return;
		}

		if (spriteAsset.CollisionMasks == null || spriteAsset.CollisionMasks.Count == 0)
		{
			DebugLog.LogError($"No collision masks defined for {spriteAsset.Name}!");
			return;
		}

		int colIndex = 0;
		byte[] byteData;

		if (spriteAsset.CollisionMasks.Count - 1 < (int)sprite.image_index)
		{
			byteData = spriteAsset.CollisionMasks[0];
			colIndex = 0;
		}
		else
		{
			byteData = spriteAsset.CollisionMasks[(int)sprite.image_index];
			colIndex = (int)sprite.image_index;
		}

		var collisionMask = new bool[spriteAsset.Height, spriteAsset.Width];

		var newByteArray = new byte[byteData.Length];

		for (int i = 0; i < byteData.Length; i++)
		{
			// https://softwarejuancarlos.com/2013/05/05/byte_bits_reverse/
			byte result = 0x00;
			for (var mask = 0x80; Convert.ToInt32(mask) > 0; mask >>= 1)
			{
				result = (byte)(result >> 1);
				var tempbyte = (byte)(byteData[i] & mask);
				if (tempbyte != 0x00)
				{
					result = (byte)(result | 0x80);
				}
			}
			newByteArray[i] = result;
		}

		byteData = newByteArray;

		var bitArray = new BitArray(byteData);

		// Collision mask is stored in a fucked format.
		// The byte array stored each pixel of the mask in its bits.
		// The pixels are defined from the top left, running along the row, until it loops back around to the start of the next row.
		// If a row ends mid-way through a byte, the rest of the byte is 0-ed out. The next byte starts the new row.
		// The bits in each byte need to be reversed first. For some fucking reason.

		var index = 0;
		for (var i = 0; i < spriteAsset.Height; i++)
		{
			for (var j = 0; j < spriteAsset.Width; j++)
			{
				var val = bitArray[index++];
				collisionMask[i, j] = val;
			}

			if (index % 8 == 0)
			{
				continue;
			}

			// need to align to the next multiple of 8
			var nextMultiple = ((index / 8) + 1) * 8;
			var difference = nextMultiple - index;
			index += difference;
		}

		if (colliders.Any(x => x.GMObject == sprite))
		{
			var collider = colliders.Single(x => x.GMObject == sprite);
			collider.Margins = margins;
			collider.SepMasks = spriteAsset.SepMasks;
			collider.BoundingBoxMode = spriteAsset.BBoxMode;
			collider.CollisionMask = collisionMask;
			collider.Origin = spriteAsset.Origin;

			if (collider.spriteAssetName != spriteAsset.Name
			    || collider.collisionMaskIndex != colIndex)
			{
				UpdateRotationMask(sprite);
			}

			collider.spriteAssetName = spriteAsset.Name;
			collider.collisionMaskIndex = colIndex;
			return;
		}

		colliders.Add(new ColliderClass(sprite)
		{
			Margins = margins,
			SepMasks = spriteAsset.SepMasks,
			BoundingBoxMode = spriteAsset.BBoxMode,
			CollisionMask = collisionMask,
			Origin = spriteAsset.Origin
		});
		UpdateRotationMask(sprite);
	}

	public static void UnregisterCollider(GamemakerObject sprite)
	{
		colliders.RemoveAll(x => x.GMObject == sprite || x.GMObject.Destroyed);
	}

	public static void UpdateRotationMask(GamemakerObject obj)
	{
		var collider = colliders.SingleOrDefault(x => x.GMObject == obj);

		if (collider == null)
		{
			DebugLog.LogWarning($"No collider found for {obj.instanceId} ({obj.object_index})");
			return;
		}

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

		var maskWidth = mask.GetLength(1);
		var maskHeight = mask.GetLength(0);

		/*
		 * The maths on these two functions took me a few hours and a lot of graph paper to work out.
		 *
		 * Rotating a point (x, y) around pivot (Px, Py) by angle θ can be found by solving this matrix equation R :
		 *
		 * [ x' ]    [ 1 0 Px ]  [ cosθ -sinθ 0 ]  [ 1 0 -Px ]  [ x ]
		 * [ y' ] =  [ 0 1 Py ]  [ sinθ  cosθ 0 ]  [ 0 1 -Py ]  [ y ]
		 * [ 1  ]    [ 0 0 1  ]  [  0	  0   1 ]  [ 0 0  1  ]  [ 1 ]
		 *
		 * Scaling a point (x, y) around pivot (Px, Py) by scale factor (Sx, Sy) can be found by solving this matrix equation S :
		 *
		 * [ x' ]    [ 1 0 Px ]  [ Sx 0  0 ]  [ 1 0 -Px ]  [ x ]
		 * [ y' ] =  [ 0 1 Py ]  [ 0  Sy 0 ]  [ 0 1 -Py ]  [ y ]
		 * [ 1  ]    [ 0 0 1  ]  [ 0  0  1 ]  [ 0 0  1  ]  [ 1 ]
		 *
		 * To scale then rotate, substitute (x', y') from S as the values of (x, y) into R.
		 * To rotate then scale, substitute (x', y') from R as the values of (x, y) into S.
		 */

		static void ScaleThenRotatePoint(double x, double y, int pivotX, int pivotY, double scaleX, double scaleY, double theta, out double xPrime, out double yPrime)
		{
			var sin = Math.Sin(CustomMath.Deg2Rad * -theta);
			var cos = Math.Cos(CustomMath.Deg2Rad * -theta);

			(x, y) = (x - pivotX, y - pivotY); // translate matrix
			(x, y) = (x * scaleX, y * scaleY); // scale matrix
			(x, y) = (x * cos - y * sin, x * sin + y * cos); // rotate matrix
			(xPrime, yPrime) = (x + pivotX, y + pivotY); // translate matrix
		}

		static void RotateThenScalePoint(double x, double y, int pivotX, int pivotY, double scaleX, double scaleY, double theta, out double xPrime, out double yPrime)
		{
			var sin = Math.Sin(CustomMath.Deg2Rad * -theta);
			var cos = Math.Cos(CustomMath.Deg2Rad * -theta);

			(x, y) = (x - pivotX, y - pivotY); // translate matrix
			(x, y) = (x * cos - y * sin, x * sin + y * cos); // rotate matrix
			(x, y) = (x * scaleX, y * scaleY); // scale matrix
			(xPrime, yPrime) = (x + pivotX, y + pivotY); // translate matrix
		}

		// Calculate where the corners of the given mask will be when rotated.
		ScaleThenRotatePoint(0, 0, pivotX, pivotY, xScale, yScale, angle, out var newTLx, out var newTLy);
		ScaleThenRotatePoint(maskWidth, 0, pivotX, pivotY, xScale, yScale, angle, out var newTRx, out var newTRy);
		ScaleThenRotatePoint(0, maskHeight, pivotX, pivotY, xScale, yScale, angle, out var newBLx, out var newBLy);
		ScaleThenRotatePoint(maskWidth, maskHeight, pivotX, pivotY, xScale, yScale, angle, out var newBRx, out var newBRy);

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
				var pixelCenterY = iMinY + row + 0.5f;

				// Rotate the center position backwards around the pivot to get a position in the original mask.
				RotateThenScalePoint(pixelCenterX, pixelCenterY, pivotX, pivotY, 1 / xScale, 1 / yScale, -angle, out var centerRotatedX, out var centerRotatedY);

				// Force this position to be an (int, int), so we can sample the original mask.
				var snappedToGridX = CustomMath.FloorToInt(centerRotatedX);
				var snappedToGridY = CustomMath.FloorToInt(centerRotatedY);

				if (snappedToGridX < 0 ||
				    snappedToGridX > maskWidth - 1 ||
				    snappedToGridY < 0 ||
				    snappedToGridY > maskHeight - 1)
				{
					// Sampling position is outside the original mask.
					continue;
				}

				try
				{
					returnBuffer[row, col] = mask[snappedToGridY, snappedToGridX];
				}
				catch (IndexOutOfRangeException)
				{
					DebugLog.LogError($"Mask size : ({mask.GetLength(0)}, {mask.GetLength(1)}) snappedToGrid.y:{snappedToGridY} snappedToGrid.x:{snappedToGridX}");
				}
			}
		}

		return (returnBuffer, new Vector2i(iMinX - pivotX , iMinY - pivotY));
	}

	public static int collision_rectangle_assetid(double topLeftX, double topLeftY, double bottomRightX, double bottomRightY, int assetId, bool precise, bool notme, GamemakerObject current)
	{
		// swap values if needed

		if (bottomRightX < topLeftX)
		{
			(bottomRightX, topLeftX) = (topLeftX, bottomRightX);
		}

		if (topLeftY > bottomRightY)
		{
			(bottomRightY, topLeftY) = (topLeftY, bottomRightY);
		}

		// sanity check
		colliders.RemoveAll(x => x.GMObject == null);

		foreach (var checkBox in colliders)
		{
			// Check if this or any parent matches
			if (checkBox.GMObject.Definition.AssetId != assetId)
			{
				var currentDefinition = checkBox.GMObject.Definition.parent;
				var matches = false;
				while (currentDefinition != null)
				{
					if (currentDefinition.AssetId == assetId)
					{
						matches = true;
						break;
					}

					currentDefinition = currentDefinition.parent;
				}

				if (!matches)
				{
					continue;
				}
			}

			// Check if testing object is current object
			if (notme && checkBox.GMObject == current)
			{
				continue;
			}

			var collision = CheckColliderAgainstRectangle(checkBox, new Vector2d(topLeftX, topLeftY), new Vector2d(bottomRightX, bottomRightY), precise);

			if (collision)
			{
				return (int)checkBox.GMObject.instanceId;
			}
		}

		return GMConstants.noone;
	}

	public static int collision_rectangle_instanceid(double topLeftX, double topLeftY, double bottomRightX, double bottomRightY, int instanceId, bool precise, bool notme, GamemakerObject current)
	{
		if (bottomRightX < topLeftX)
		{
			(bottomRightX, topLeftX) = (topLeftX, bottomRightX);
		}

		if (topLeftY > bottomRightY)
		{
			(bottomRightY, topLeftY) = (topLeftY, bottomRightY);
		}

		colliders.RemoveAll(x => x.GMObject == null);

		foreach (var checkBox in colliders)
		{
			if (checkBox.GMObject.instanceId != instanceId)
			{
				continue;
			}

			if (notme && checkBox.GMObject == current)
			{
				continue;
			}

			var collision = CheckColliderAgainstRectangle(checkBox, new Vector2d(topLeftX, topLeftY), new Vector2d(bottomRightX, bottomRightY), precise);

			if (collision)
			{
				return (int)checkBox.GMObject.instanceId;
			}
		}

		return GMConstants.noone;
	}

	public static bool place_meeting_assetid(double x, double y, int assetId, GamemakerObject current)
	{
		return instance_place_assetid(x, y, assetId, current) != null;
	}

	public static bool place_meeting_instanceid(double x, double y, int instanceId, GamemakerObject current)
	{
		return instance_place_instanceid(x, y, instanceId, current) != null;
	}

	public static GamemakerObject? instance_place_assetid(double x, double y, int assetId, GamemakerObject current)
	{
		// gamemaker floors the x/y coords
		x = Math.Floor(x);
		y = Math.Floor(y);

		var savedX = current.x;
		var savedY = current.y;
		current.x = x;
		current.y = y;

		var movedBox = colliders.SingleOrDefault(b => b.GMObject == current);

		if (movedBox == null)
		{
			DebugLog.LogError($"ERROR: Can't find collider for {current}!");
			current.x = savedX;
			current.y = savedY;
			return null;
		}

		foreach (var checkBox in colliders)
		{
			if (checkBox == null)
			{
				DebugLog.LogWarning($"Null collider in colliders!");
				continue;
			}

			if (checkBox.GMObject.Definition.AssetId != assetId)
			{
				var currentDefinition = checkBox.GMObject.Definition.parent;
				var matches = false;
				while (currentDefinition != null)
				{
					if (currentDefinition.AssetId == assetId)
					{
						matches = true;
						break;
					}

					currentDefinition = currentDefinition.parent;
				}

				if (!matches)
				{
					continue;
				}
			}

			if (CheckColliderAgainstCollider(movedBox, checkBox))
			{
				current.x = savedX;
				current.y = savedY;
				return checkBox.GMObject;
			}
		}

		current.x = savedX;
		current.y = savedY;
		return null;
	}

	public static GamemakerObject? instance_place_instanceid(double x, double y, int instanceId, GamemakerObject current)
	{
		// gamemaker floors the x/y coords
		x = Math.Floor(x);
		y = Math.Floor(y);

		var savedX = current.x;
		var savedY = current.y;
		current.x = x;
		current.y = y;

		var movedBox = colliders.Single(b => b.GMObject == current);

		foreach (var checkBox in colliders)
		{
			if (checkBox == null)
			{
				DebugLog.LogWarning($"Null collider in colliders!");
				continue;
			}

			if (checkBox.GMObject.instanceId != instanceId)
			{
				continue;
			}

			if (CheckColliderAgainstCollider(movedBox, checkBox))
			{
				current.x = savedX;
				current.y = savedY;
				return checkBox.GMObject;
			}
		}

		current.x = savedX;
		current.y = savedY;
		return null;
	}

	public static double DistanceToObject(GamemakerObject a, GamemakerObject b)
	{
		var aBox = colliders.First(x => x.GMObject == a);
		var bBox = colliders.First(x => x.GMObject == b);

		var deltaX = CustomMath.Max(aBox.BBox.left, bBox.BBox.left) - CustomMath.Min(aBox.BBox.right, bBox.BBox.right);
		var deltaY = CustomMath.Max(aBox.BBox.top, bBox.BBox.top) - CustomMath.Min(aBox.BBox.bottom, bBox.BBox.bottom);

		deltaX = CustomMath.Max(0, deltaX);
		deltaY = CustomMath.Max(0, deltaY);

		return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
	}

	private static bool DoBoxesOverlap(double left, double top, double right, double bottom, ColliderClass b)
		=> left <= (b.BBox.right - 1) 
		   && right - 1 >= b.BBox.left 
		   && top <= (b.BBox.bottom - 1) 
		   && bottom - 1 >= b.BBox.top;

	private static bool DoBoxesOverlap(ColliderClass a, ColliderClass b)
		=> a.BBox.left <= b.BBox.right
		   && a.BBox.right >= b.BBox.left
		   && a.BBox.top <= b.BBox.bottom
		   && a.BBox.bottom >= b.BBox.top;
}

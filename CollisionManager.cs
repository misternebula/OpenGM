using DELTARUNITYStandalone.VirtualMachine;
using OpenTK.Mathematics;
using System.Collections;
using System.Diagnostics;
using UndertaleModLib.Models;

namespace DELTARUNITYStandalone;

public class BBox
{
	/// <summary>
	/// X co-ordinate of the left-hand side of the bounding box.
	/// </summary>
	public float left;

	/// <summary>
	/// X co-ordinate of the right-hand side of the bounding box.
	/// </summary>
	public float right;

	/// <summary>
	/// Y co-ordinate of the top of the bounding box.
	/// </summary>
	public float top;

	/// <summary>
	/// Y co-ordinate of the top of the bounding box.
	/// </summary>
	public float bottom;
}

public class ColliderClass
{
	public GamemakerObject GMObject;

	public string spriteAssetName;
	public int collisionMaskIndex;

	private Vector2 _pos => new((float)GMObject.x, (float)GMObject.y);
	private Vector2 _scale => new((float)GMObject.image_xscale, (float)GMObject.image_yscale);

	public Vector2i Origin;

	public ColliderClass(GamemakerObject obj)
	{
		GMObject = obj;
		Origin = SpriteManager.GetSpriteOrigin(GMObject.sprite_index);
	}

	public Vector4 Margins;

	public BBox BBox => CollisionManager.CalculateBoundingBox(GMObject);

	public Vector3 BBCenter => new(
		(BBox.left + BBox.right) / 2,
		(BBox.top + BBox.bottom) / 2,
		0);

	public Vector3 BBSize => new(
		BBox.right - BBox.left,
		BBox.bottom - BBox.top,
		1);

	/// <summary>
	/// The position of the collider
	/// </summary>
	public Vector3 Position => new(
		_pos.X - (Origin.X * _scale.X),
		_pos.Y - (Origin.Y * _scale.Y),
		0);

	public Vector2 Scale => _scale;

	public UndertaleSprite.SepMaskType SepMasks;
	public uint BoundingBoxMode;
	public bool[,] CollisionMask;

	public bool[,] CachedRotatedMask = null;
	public Vector2 CachedRotatedMaskOffset;
}

public static class CollisionManager
{
	public static List<ColliderClass> colliders = new();

	public static BBox CalculateBoundingBox(GamemakerObject gm)
	{
		// TODO : This is called a LOT. This needs to be as optimized as possible.

		var pos = new Vector2((float)gm.x, (float)gm.y);
		var origin = SpriteManager.GetSpriteOrigin(gm.sprite_index);

		var left = (float)(pos.X + (gm.margins.X * gm.image_xscale) - (origin.X * gm.image_xscale));
		var top = (float)(pos.Y + (gm.margins.W * gm.image_yscale) - (origin.Y * gm.image_yscale));
		var right = (float)(pos.X + ((gm.margins.Y + 1) * gm.image_xscale) - (origin.X * gm.image_xscale));
		var bottom = (float)(pos.Y + ((gm.margins.Z + 1) * gm.image_yscale) - (origin.Y * gm.image_yscale));

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
		var topLeft = new Vector2(left, top);
		var topRight = new Vector2(right, top);
		var bottomRight = new Vector2(right, bottom);
		var bottomLeft = new Vector2(left, bottom);

		// rotate co-ords
		topLeft = topLeft.RotateAroundPoint(pos, gm.image_angle);
		topRight = topRight.RotateAroundPoint(pos, gm.image_angle);
		bottomRight = bottomRight.RotateAroundPoint(pos, gm.image_angle);
		bottomLeft = bottomLeft.RotateAroundPoint(pos, gm.image_angle);

		return new BBox
		{
			left = (float)CustomMath.Min(topLeft.X, topRight.X, bottomRight.X, bottomLeft.X),
			right = (float)CustomMath.Max(topLeft.X, topRight.X, bottomRight.X, bottomLeft.X),
			top = (float)CustomMath.Min(topLeft.Y, topRight.Y, bottomRight.Y, bottomLeft.Y),
			bottom = (float)CustomMath.Max(topLeft.Y, topRight.Y, bottomRight.Y, bottomLeft.Y)
		};
	}

	/// <summary>
	/// Checks the collision of a single object at a certain position. 
	/// </summary>
	public static bool CheckColliderAtPoint(ColliderClass col, Vector2 position, bool precise)
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

			return false;
		}
		else
		{
			// Precise collision, my behated. If "precise" is true, we have to rotate the collision mask and other funky stuff.
			// If "precise" is false, then this is the same as AxisAlignedRect.

			return false;
		}
	}

	public static bool CheckColliderWithRectagle(ColliderClass collider, Vector2 v1, Vector2 v2, bool precise)
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

		if (collider.SepMasks == UndertaleSprite.SepMaskType.AxisAlignedRect)
		{
			// Easiest collision. "precise" does not affect anything. Just check if rectangles intersect..

			return boundingBoxesCollide;
		}
		else if (collider.SepMasks == UndertaleSprite.SepMaskType.RotatedRect)
		{
			return false;
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
					var currentPixelPos = new Vector2(
						(float)collider.GMObject.x + checkOffset.X + col + 0.5f,
						(float)collider.GMObject.y + checkOffset.Y + row + 0.5f
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

	public static (bool[,] buffer, Vector2 topLeftOffset) RotateMask(bool[,] mask, double angle, int pivotX, int pivotY, double xScale, double yScale)
	{
		/*
		 * Nearest-Neighbour algorithm for rotating a collision mask.
		 * Assume that the given mask is positioned at (0, 0), and that the given pivot is relative to (0, 0).
		 * We need to return the rotated mask in a new buffer, and where the top left of the new mask is, relative to (0, 0).
		 */

		var sin = Math.Sin(CustomMath.Deg2Rad * -angle);
		var cos = Math.Cos(CustomMath.Deg2Rad * -angle);

		var maskWidth = mask.GetLength(1);
		var maskHeight = mask.GetLength(0);

		void RotateAroundPoint(int pivotX, int pivotY, bool reverse, double x, double y, out double rotatedX, out double rotatedY)
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

			if (!reverse)
			{
				xnew *= xScale;
				ynew *= yScale;
			}
			else
			{
				xnew /= xScale;
				ynew /= yScale;
			}
			

			rotatedX = xnew + pivotX;
			rotatedY = ynew + pivotY;
		}

		// Calculate where the corners of the given mask will be when rotated.
		RotateAroundPoint(pivotX, pivotY, false, 0, 0, out var newTLx, out var newTLy);
		RotateAroundPoint(pivotX, pivotY, false, maskWidth, 0, out var newTRx, out var newTRy);
		RotateAroundPoint(pivotX, pivotY, false, 0, maskHeight, out var newBLx, out var newBLy);
		RotateAroundPoint(pivotX, pivotY, false, maskWidth, maskHeight, out var newBRx, out var newBRy);

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
				RotateAroundPoint(pivotX, pivotY, true, pixelCenterX, pixelCenterY, out var centerRotatedX, out var centerRotatedY);

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
				catch (IndexOutOfRangeException e)
				{
					DebugLog.LogError($"Mask size : ({mask.GetLength(0)}, {mask.GetLength(1)}) snappedToGrid.y:{snappedToGridY} snappedToGrid.x:{snappedToGridX}");
				}
			}
		}

		return (returnBuffer, new Vector2(iMinX - (float)(pivotX) , iMinY - (float)(pivotY)));
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

			var collision = CheckColliderWithRectagle(checkBox, new Vector2((float)topLeftX, (float)topLeftY), new Vector2((float)bottomRightX, (float)bottomRightY), precise);

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

			var collision = CheckColliderWithRectagle(checkBox, new Vector2((float)topLeftX, (float)topLeftY), new Vector2((float)bottomRightX, (float)bottomRightY), precise);

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

	public static GamemakerObject instance_place_assetid(double x, double y, int assetId, GamemakerObject current)
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

			if ((checkBox.SepMasks == UndertaleSprite.SepMaskType.Precise
			     && movedBox.SepMasks == UndertaleSprite.SepMaskType.Precise)
			    || (checkBox.SepMasks != movedBox.SepMasks))
			{
				// precise collisions
				if (!DoBoxesOverlap(movedBox, checkBox))
				{
					// bounding boxes don't even overlap, don't bother testing precise collision
					continue;
				}

				if (movedBox.CachedRotatedMask == null)
				{
					(movedBox.CachedRotatedMask, movedBox.CachedRotatedMaskOffset) = RotateMask(movedBox.CollisionMask, movedBox.GMObject.image_angle, movedBox.Origin.X, movedBox.Origin.Y, movedBox.Scale.X, movedBox.Scale.Y);
				}

				if (checkBox.CachedRotatedMask == null)
				{
					(checkBox.CachedRotatedMask, checkBox.CachedRotatedMaskOffset) = RotateMask(checkBox.CollisionMask, checkBox.GMObject.image_angle, checkBox.Origin.X, checkBox.Origin.Y, checkBox.Scale.X, checkBox.Scale.Y);
				}

				var currentRotatedMask = movedBox.CachedRotatedMask;
				var currentOffset = movedBox.CachedRotatedMaskOffset;
				var checkRotatedMask = checkBox.CachedRotatedMask;
				var checkOffset = checkBox.CachedRotatedMaskOffset;

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
						var currentPixelPos = new Vector2((int)movedBox.Position.X + currentOffset.X + col + 0.5f, (int)movedBox.Position.Y + currentOffset.Y + row + 0.5f);

						// Get the world space position of the top-left of the other rotated mask
						var checkMaskTopLeft = new Vector2((int)checkBox.Position.X + checkOffset.X, (int)checkBox.Position.Y + checkOffset.Y);

						var placeInOtherMask = currentPixelPos - checkMaskTopLeft;

						var snappedToGrid = new Vector2i((int)Math.Floor(placeInOtherMask.X), (int)Math.Ceiling(placeInOtherMask.Y));

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
							current.x = savedX;
							current.y = savedY;
							return checkBox.GMObject;
						}
					}
				}
			}
			else
			{
				// bounding box collision
				if (DoBoxesOverlap(movedBox, checkBox))
				{
					current.x = savedX;
					current.y = savedY;
					return checkBox.GMObject;
				}
			}
		}

		current.x = savedX;
		current.y = savedY;
		return null;
	}

	public static GamemakerObject instance_place_instanceid(double x, double y, int instanceId, GamemakerObject current)
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

			// generate the collision mask if in editor
			if (checkBox.CollisionMask == null || movedBox.CollisionMask == null)
			{
				var spriteIndex = checkBox.CollisionMask == null ? checkBox.GMObject.sprite_index : movedBox.GMObject.sprite_index;
				throw new Exception($"collision mask not defined for {spriteIndex}");
			}

			if ((checkBox.SepMasks == UndertaleSprite.SepMaskType.Precise
				&& movedBox.SepMasks == UndertaleSprite.SepMaskType.Precise)
				|| (checkBox.SepMasks != movedBox.SepMasks))
			{
				// precise collisions

				if (!DoBoxesOverlap(movedBox, checkBox))
				{
					// bounding boxes don't even overlap, don't bother testing precise collision
					continue;
				}

				//var (currentRotatedMask, currentOffset) = RotateMask(movedBox.CollisionMask, movedBox.GMObject.image_angle, movedBox.Origin.x, movedBox.Origin.y, movedBox.Scale.x, movedBox.Scale.y);
				//var (checkRotatedMask, checkOffset) = RotateMask(checkBox.CollisionMask, checkBox.GMObject.image_angle, checkBox.Origin.x, checkBox.Origin.y, checkBox.Scale.x, checkBox.Scale.y);

				if (movedBox.CachedRotatedMask == null)
				{
					(movedBox.CachedRotatedMask, movedBox.CachedRotatedMaskOffset) = RotateMask(movedBox.CollisionMask, movedBox.GMObject.image_angle, movedBox.Origin.X, movedBox.Origin.Y, movedBox.Scale.X, movedBox.Scale.Y);
				}

				if (checkBox.CachedRotatedMask == null)
				{
					(checkBox.CachedRotatedMask, checkBox.CachedRotatedMaskOffset) = RotateMask(checkBox.CollisionMask, checkBox.GMObject.image_angle, checkBox.Origin.X, checkBox.Origin.Y, checkBox.Scale.X, checkBox.Scale.Y);
				}

				var currentRotatedMask = movedBox.CachedRotatedMask;
				var currentOffset = movedBox.CachedRotatedMaskOffset;
				var checkRotatedMask = checkBox.CachedRotatedMask;
				var checkOffset = checkBox.CachedRotatedMaskOffset;

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

						// Get the world space position of the center of this pixel (up = +y)
						var currentPixelPos = new Vector2((int)movedBox.Position.X + currentOffset.X + col + 0.5f, (int)movedBox.Position.Y + currentOffset.Y + row + 0.5f);

						// Get the world space position of the top-left of the other rotated mask (up = +y)
						var checkMaskTopLeft = new Vector2((int)checkBox.Position.X + checkOffset.X, (int)checkBox.Position.Y + checkOffset.Y);

						var placeInOtherMask = currentPixelPos - checkMaskTopLeft;

						var snappedToGrid = new Vector2i((int)Math.Floor(placeInOtherMask.X), (int)Math.Ceiling(placeInOtherMask.Y));

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
							current.x = savedX;
							current.y = savedY;
							return checkBox.GMObject;
						}
					}
				}
			}
			else
			{
				// bounding box collision
				if (DoBoxesOverlap(movedBox, checkBox))
				{
					current.x = savedX;
					current.y = savedY;
					return checkBox.GMObject;
				}
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

using DELTARUNITYStandalone.VirtualMachine;
using OpenTK.Mathematics;
using System;
using System.Collections;
using System.Diagnostics;
using UndertaleModLib.Models;
using static UndertaleModLib.Compiler.Compiler;

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

	public BBox BBox => new()
	{
		left = _pos.X + (Margins.X * _scale.X) - (Origin.X * _scale.X),
		top = _pos.Y + (Margins.W * _scale.Y) - (Origin.Y * _scale.Y),
		right = _pos.X + (Margins.Y * _scale.X) - (Origin.X * _scale.X),
		bottom = _pos.Y + (Margins.Z * _scale.Y) - (Origin.Y * _scale.Y)
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
	public Vector2i CachedRotatedMaskOffset;
}

public static class CollisionManager
{
	private static List<ColliderClass> colliders = new();

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
		var bitArray = new BitArray(byteData);

		// byte[] data is 1 bit per pixel, increasing in row

		var index = 0;
		for (var i = 0; i < spriteAsset.Height; i++)
		{
			for (var j = 0; j < spriteAsset.Width; j++)
			{
				var val = bitArray[index++];
				collisionMask[i, j] = val;
			}
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
			var ynew = (x * useSin) - (y * cos);

			rotatedX = (float)xnew + pivotX;
			rotatedY = (float)ynew + pivotY;
		}

		// Calculate where the corners of the given mask will be when rotated.
		RotateAroundPoint(pivotX, pivotY, false, 0, 0, out var newTLx, out var newTLy);
		RotateAroundPoint(pivotX, pivotY, false, (int)(maskWidth * xScale), 0, out var newTRx, out var newTRy);
		RotateAroundPoint(pivotX, pivotY, false, 0, (int)(maskHeight * yScale), out var newBLx, out var newBLy);
		RotateAroundPoint(pivotX, pivotY, false, (int)(maskWidth * xScale), (int)(maskHeight * yScale), out var newBRx, out var newBRy);

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
				var pixelCenterY = iMaxY + row + 0.5f;

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
				    snappedToGridY < 0 ||
				    snappedToGridY > maskHeight - 1)
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

	public static int collision_rectangle_assetid(double topLeftX, double topLeftY, double bottomRightX, double bottomRightY, int assetId, bool precise, bool notme, GamemakerObject current)
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

			if (notme && checkBox.GMObject == current)
			{
				continue;
			}

			var boxesOverlap = DoBoxesOverlap(topLeftX, topLeftY, bottomRightX, bottomRightY, checkBox);

			if (!boxesOverlap)
			{
				continue;
			}

			if (precise && checkBox.SepMasks == UndertaleSprite.SepMaskType.Precise)
			{
				var iTopLeftX = CustomMath.Max((float)topLeftX, checkBox.BBox.left);
				var iTopLeftY = CustomMath.Min((float)topLeftY, checkBox.BBox.top);
				var iBottomRightX = CustomMath.Min((float)bottomRightX, checkBox.BBox.right);
				var iBottomRightY = CustomMath.Max((float)bottomRightY, checkBox.BBox.bottom);

				var iWidth = CustomMath.FloorToInt(Math.Abs(iTopLeftX - iBottomRightX));
				var iHeight = CustomMath.FloorToInt(Math.Abs(iTopLeftY - iBottomRightY));

				for (var i = 0; i < iHeight; i++)
				{
					for (var j = 0; j < iWidth; j++)
					{
						if (topLeftX == iTopLeftX)
						{
							var deltaX = CustomMath.RoundToInt(Math.Abs(iTopLeftX - checkBox.Position.X)) + j;
							var deltaY = CustomMath.RoundToInt(Math.Abs(iTopLeftY - checkBox.Position.Y)) + i;
							deltaX /= (int)checkBox.Scale.X;
							deltaY /= (int)checkBox.Scale.Y;
							try
							{
								if (checkBox.CollisionMask[deltaY, deltaX])
								{
									return (int)checkBox.GMObject.instanceId;
								}
							}
							catch (IndexOutOfRangeException iex)
							{
								//Debug.Break();
							}
						}
						else
						{
							var yAdjust = CustomMath.RoundToInt(Math.Abs(checkBox.Position.Y - iTopLeftY));
							yAdjust = CustomMath.FloorToInt(yAdjust / checkBox.Scale.Y);
							try
							{
								if (checkBox.CollisionMask[yAdjust + i, CustomMath.FloorToInt(j / checkBox.Scale.X)])
								{
									return (int)checkBox.GMObject.instanceId;
								}
							}
							catch (IndexOutOfRangeException iex)
							{
								//Debug.Break();
							}
						}
					}
				}
			}

			if (boxesOverlap)
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

			var boxesOverlap = DoBoxesOverlap(topLeftX, topLeftY, bottomRightX, -bottomRightY, checkBox);

			if (!boxesOverlap)
			{
				continue;
			}

			if (precise && checkBox.SepMasks == UndertaleSprite.SepMaskType.Precise)
			{
				var iTopLeftX = CustomMath.Max((float)topLeftX, checkBox.BBox.left);
				var iTopLeftY = CustomMath.Min((float)topLeftY, checkBox.BBox.top);
				var iBottomRightX = CustomMath.Min((float)bottomRightX, checkBox.BBox.right);
				var iBottomRightY = CustomMath.Max((float)bottomRightY, checkBox.BBox.bottom);

				var iWidth = CustomMath.FloorToInt(Math.Abs(iTopLeftX - iBottomRightX));
				var iHeight = CustomMath.FloorToInt(Math.Abs(iTopLeftY - iBottomRightY));

				for (var i = 0; i < iHeight; i++)
				{
					for (var j = 0; j < iWidth; j++)
					{
						if (topLeftX == iTopLeftX)
						{
							var deltaX = CustomMath.RoundToInt(Math.Abs(iTopLeftX - checkBox.Position.X)) + j;
							var deltaY = CustomMath.RoundToInt(Math.Abs(iTopLeftY - checkBox.Position.Y)) + i;
							deltaX /= (int)checkBox.Scale.X;
							deltaY /= (int)checkBox.Scale.Y;
							try
							{
								if (checkBox.CollisionMask[deltaY, deltaX])
								{
									return (int)checkBox.GMObject.instanceId;
								}
							}
							catch (IndexOutOfRangeException iex)
							{
								//Debug.Break();
							}
						}
						else
						{
							var yAdjust = CustomMath.RoundToInt(Math.Abs(checkBox.Position.Y - iTopLeftY));
							yAdjust = CustomMath.FloorToInt(yAdjust / checkBox.Scale.Y);
							try
							{
								if (checkBox.CollisionMask[yAdjust + i, CustomMath.FloorToInt(j / checkBox.Scale.X)])
								{
									return (int)checkBox.GMObject.instanceId;
								}
							}
							catch (IndexOutOfRangeException iex)
							{
								//Debug.Break();
							}
						}
					}
				}
			}

			if (boxesOverlap)
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
						var checkMaskTopLeft = new Vector2i((int)checkBox.Position.X + checkOffset.X, (int)checkBox.Position.Y + checkOffset.Y);

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
						var checkMaskTopLeft = new Vector2i((int)checkBox.Position.X + checkOffset.X, (int)checkBox.Position.Y + checkOffset.Y);

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

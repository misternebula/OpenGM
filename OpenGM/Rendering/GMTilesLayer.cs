using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.Loading;
using OpenGM.SerializedFiles;
using OpenTK.Mathematics;

namespace OpenGM.Rendering;

internal class GMTilesLayer : DrawWithDepth
{
	private CLayerTilemapElement _element;

	public GMTilesLayer(CLayerTilemapElement element)
	{
		_element = element;
		DrawManager.Register(this);
		_timing = new Stopwatch();
		_timing.Start();
		_tileSet = GameLoader.TileSets[_element.BackgroundIndex];
	}

	private Stopwatch _timing;
	private TileSet _tileSet;
	private int _currentFrame = 0;

	public override void Draw()
	{
		if (_timing.Elapsed.Microseconds >= _tileSet.FrameTime)
		{
			_currentFrame++;
			if (_currentFrame >= _tileSet.FramesPerTile)
			{
				_currentFrame = 0;
			}
		}

		// TODO : Does tilemap animation still happen if the layer is not visible?
		// just move this if block above the previous if so
		if (!_element.Layer.Visible)
		{
			return;
		}

		for (var _y = 0; _y < _element.Height; _y++)
		{
			for (var _x = 0; _x < _element.Width; _x++)
			{
				var tile = _element.TilesData[_y, _x];

				if (tile.TileIndex == 0)
				{
					// There's a checkerboard graphic here in the tileset, so 0 represents no tile.
					// Guessing GM also does this check? Why didn't they just make the first tile always blank?!
					continue;
				}

				var indexIntoTileset = (tile.TileIndex * _tileSet.FramesPerTile) + _currentFrame;
				var tileId = _tileSet.TileIds[indexIntoTileset];

				// We now have how many tiles into the tileset the current tile to draw is.
				// 0 is at the top left, and it loops back around to the left of the next line.

				var tileSetRow = CustomMath.FloorToInt(tileId / (double)_tileSet.TileColumns);
				var tileSetColumn = tileId % _tileSet.TileColumns;

				var tileWidth = _tileSet.TileWidth + (_tileSet.OutputBorderX * 2); // Width of tile in tileset, not actual tile graphic
				var tileHeight = _tileSet.TileHeight + (_tileSet.OutputBorderY * 2); // ditto

				// TODO : Handle mirror, rotate, flip

				CustomWindow.RenderJobs.Add(new GMSpritePartJob()
				{
					texture = _tileSet.Texture,
					width = _tileSet.TileWidth,
					height = _tileSet.TileHeight,
					screenPos = new Vector2d(_x * _tileSet.TileWidth, _y * _tileSet.TileHeight),
					alpha = 1,
					blend = Color4.White,
					left = (tileSetColumn * tileWidth) + _tileSet.OutputBorderX,
					top = (tileSetRow * tileHeight) + _tileSet.OutputBorderX,
					scale = Vector2.One,
				});
			}
		}
	}

	public override void Destroy()
	{
		DrawManager.Unregister(this);
	}
}

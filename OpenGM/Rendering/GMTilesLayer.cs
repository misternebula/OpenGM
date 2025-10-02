using OpenGM.Loading;
using OpenGM.SerializedFiles;
using OpenTK.Mathematics;
using System.Diagnostics;

namespace OpenGM.Rendering;

internal class GMTilesLayer : DrawWithDepth
{
    public CLayerTilemapElement Element;

    public GMTilesLayer(CLayerTilemapElement element)
    {
        Element = element;
        DrawManager.Register(this);
        _timing = new Stopwatch();
        _timing.Start();
        _tileSet = GameLoader.TileSets[Element.BackgroundIndex];
    }

    private Stopwatch _timing;
    private TileSet _tileSet;
    private int _currentFrame = 0;

    public override void Draw()
    {
        if (_timing.Elapsed.TotalMicroseconds >= _tileSet.FrameTime)
        {
            _currentFrame++;
            _timing.Restart();
            if (_currentFrame >= _tileSet.FramesPerTile)
            {
                _currentFrame = 0;
            }
        }

        // TODO : Does tilemap animation still happen if the layer is not visible?
        // just move this if block above the previous if so
        if (!Element.Layer.Visible)
        {
            return;
        }

        for (var _y = 0; _y < Element.Height; _y++)
        {
            for (var _x = 0; _x < Element.Width; _x++)
            {
                var tile = Element.TilesData[_y, _x];

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

                var offset = new Vector2d();

                var angle = 0;
                if (tile.Rotate)
                {
                    angle = 90;
                    offset += new Vector2(_tileSet.TileHeight, 0);
                }

                var scale = Vector2.One;

                if (tile.Mirror)
                {
                    scale.X = -1;
                    offset += new Vector2(_tileSet.TileWidth, 0);
                }

                if (tile.Flip)
                {
                    scale.Y = -1;
                    offset += new Vector2(0, _tileSet.TileHeight);
                }

                CustomWindow.Draw(new GMSpritePartJob()
                {
                    texture = _tileSet.Texture,
                    width = _tileSet.TileWidth,
                    height = _tileSet.TileHeight,
                    screenPos = new Vector2d(Element.x + (_x * _tileSet.TileWidth) + Element.Layer.X, Element.y + (_y * _tileSet.TileHeight) + Element.Layer.Y) + offset,
                    Colors = [Color4.White, Color4.White, Color4.White, Color4.White],
                    left = (tileSetColumn * tileWidth) + _tileSet.OutputBorderX,
                    top = (tileSetRow * tileHeight) + _tileSet.OutputBorderX,
                    scale = scale,
                    angle = angle,
                    origin = Vector2.Zero
                });
            }
        }
    }

    public override void Destroy()
    {
        DrawManager.Unregister(this);
    }
}

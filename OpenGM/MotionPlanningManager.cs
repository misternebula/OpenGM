using OpenGM.VirtualMachine;

namespace OpenGM
{
    public static class MotionPlanningManager
    {
        public static int MPGridIndex;
        public static Dictionary<int, MPGrid> MPGrids = new();

        public static int GridCreate(int left, int top, int hcells, int vcells, int cellwidth, int cellheight)
        {
            var id = MPGridIndex++;
            MPGrids.Add(id, new(left, top, hcells, vcells, cellwidth, cellheight));
            return id;
        }

        public static void GridDestroy(int id)
        {
            MPGrids.Remove(id);
        }

        public static void GridClearCell(int id, int h, int v)
        {
            if (!MPGrids.ContainsKey(id))
            {
                throw new NotImplementedException();
            }

            var grid = MPGrids[id];
            grid.Poke(h, v, 0);
        }

        public static void GridAddCell(int id, int h, int v)
        {
            if (!MPGrids.ContainsKey(id))
            {
                throw new NotImplementedException();
            }

            var grid = MPGrids[id];
            grid.Poke(h, v, -1);
        }

        public static void GridAddInstances(int id, int obj, bool prec)
        {
            // https://github.com/YoYoGames/GameMaker-HTML5/blob/ab8f0019fd026d9a05dfd9d769aa3898326fb7e8/scripts/functions/Function_MotionPlanning.js#L1011

            if (!MPGrids.ContainsKey(id))
            {
                return;
            }

            var grid = MPGrids[id];

            if (grid == null)
            {
                return;
            }

            var col_delta = 0.0001; //To avoid leaching into the next cell
            if (CompatFlags.LegacyCollision)
            {
                col_delta = 0.0; //or in compat mode to go back to how it was
            }

            var instances = new List<GamemakerObject>();

            if (obj < GMConstants.FIRST_INSTANCE_ID)
            {
                instances = InstanceManager.FindByAssetId(obj);
            }
            else
            {
                instances = InstanceManager.FindByLegacyValue(obj);
            }

            foreach (var inst in instances)
            {
                if (inst.Marked || !inst.Active)
                {
                    continue;
                }

                if (inst.bbox_dirty)
                {
                    inst.bbox = CollisionManager.CalculateBoundingBox(inst);
                }

                var xx1 = CustomMath.DoubleTilde((inst.bbox_left + col_delta - grid.Left) / grid.CellWidth);
                if (xx1 < 0)
                    xx1 = 0;

                var xx2 = CustomMath.DoubleTilde((inst.bbox_right - col_delta - grid.Left) / grid.CellWidth);
                if (xx2 >= grid.HCells)
                    xx2 = grid.HCells - 1;

                var yy1 = CustomMath.DoubleTilde((inst.bbox.top + col_delta - grid.Top) / grid.CellHeight);
                if (yy1 < 0)
                    yy1 = 0;

                var yy2 = CustomMath.DoubleTilde((inst.bbox.bottom - col_delta - grid.Top) / grid.CellHeight);
                if (yy2 >= grid.VCells)
                    yy2 = grid.VCells - 1;

                for (var i = xx1; i <= xx2; i++)
                {
                    for (var j = yy1; j <= yy2; j++)
                    {
                        if (!prec)
                        {
                            grid.Cells[i * grid.VCells + j] = -1;
                            continue;
                        }

                        if (grid.Cells[i * grid.VCells + j] < 0)
                            continue;

                        if (CollisionManager.Collision_Rectangle(inst, 
                                grid.Left + i * grid.CellWidth,
                                grid.Top + j * grid.CellHeight, 
                                grid.Left + (i + 1) * grid.CellWidth - 1,
                                grid.Top + (j + 1) * grid.CellHeight - 1, true))
                        {
                            grid.Cells[i * grid.VCells + j] = -1;
                        }
                    }
                }
            }
        }

        public static bool GridPath(int id, int pathid, int xstart, int ystart, int xgoal, int ygoal, bool allowdiag)
        {
            // https://github.com/YoYoGames/GameMaker-HTML5/blob/ab8f0019fd026d9a05dfd9d769aa3898326fb7e8/scripts/functions/Function_MotionPlanning.js#L1129
            // this function is a pain
            // start and goal are in ROOM coordiantes, not grid coordinates

            if (!MPGrids.ContainsKey(id) || !PathManager.Paths.ContainsKey(pathid))
            {
                return false;
            }

            var grid = MPGrids[id];
            var path = PathManager.Paths[pathid];

            if (grid == null || path == null)
            {
                return false;
            }

            (bool result, int cx, int cy) CheckPosition(int x, int y)
            {
                if (x < grid.Left || x >= grid.Left + grid.HCells * grid.CellWidth)
                {
                    // x invalid
                    return (false, 0, 0);
                }

                if (y < grid.Top || y >= grid.Top + grid.VCells * grid.CellHeight)
                {
                    // y invalid
                    return (false, 0, 0);
                }

                var cx = CustomMath.DoubleTilde((x - grid.Left) / (double)grid.CellWidth);
                var cy = CustomMath.DoubleTilde((y - grid.Top) / (double)grid.CellHeight);

                if (grid.Cells[cx * grid.VCells + cy] < 0)
                {
                    return (false, 0, 0);
                }

                return (true, cx, cy);
            }

            var (results, cxs, cys) = CheckPosition(xstart, ystart);
            if (!results)
            {
                return false;
            }

            var (resultg, cxg, cyg) = CheckPosition(xgoal, ygoal);
            if (!resultg)
            {
                return false;
            }

            var result = false;

            // start the search
            grid.Cells[cxs * grid.VCells + cys] = 1;
            var queue = new Queue<int>();
            queue.Enqueue(cxs * grid.VCells + cys);

            while (queue.Count >= 1)
            {
                var val = queue.Dequeue();
                var xx = CustomMath.DoubleTilde((double)val / grid.VCells);
                var yy = CustomMath.DoubleTilde((double)val % grid.VCells);

                if (xx == cxg && yy == cyg)
                {
                    result = true;
                    break;
                }

                var d = grid.Cells[val] + 1;
                var f1 = xx > 0 && yy < grid.VCells - 1 && grid.Cells[(xx - 1) * grid.VCells + yy + 1] == 0;
                var f2 = yy < grid.VCells - 1 && grid.Cells[xx * grid.VCells + yy + 1] == 0;
                var f3 = xx < grid.HCells - 1 && yy < grid.VCells - 1 && grid.Cells[(xx + 1) * grid.VCells + yy + 1] == 0;
                var f4 = xx > 0 && grid.Cells[(xx - 1) * grid.VCells + yy] == 0;
                var f6 = xx < grid.HCells - 1 && grid.Cells[(xx + 1) * grid.VCells + yy] == 0;
                var f7 = xx > 0 && yy > 0 && grid.Cells[(xx - 1) * grid.VCells + (yy - 1)] == 0;
                var f8 = yy > 0 && grid.Cells[xx * grid.VCells + (yy - 1)] == 0;
                var f9 = xx < grid.HCells - 1 && yy > 0 && grid.Cells[(xx + 1) * grid.VCells + (yy - 1)] == 0;

                // Handle horizontal && vertical moves
                if (f4)
                {
                    grid.Cells[(xx - 1) * grid.VCells + yy] = d;
                    queue.Enqueue(~~((xx - 1) * grid.VCells + yy));
                }
                if (f6)
                {
                    grid.Cells[(xx + 1) * grid.VCells + yy] = d;
                    queue.Enqueue(~~((xx + 1) * grid.VCells + yy));
                }
                if (f8)
                {
                    grid.Cells[xx * grid.VCells + yy - 1] = d;
                    queue.Enqueue(~~(xx * grid.VCells + yy - 1));
                }
                if (f2)
                {
                    grid.Cells[xx * grid.VCells + yy + 1] = d;
                    queue.Enqueue(~~(xx * grid.VCells + yy + 1));
                }
                // Handle diagonal moves
                if (allowdiag && f1 && f2 && f4)
                {
                    grid.Cells[(xx - 1) * grid.VCells + yy + 1] = d;
                    queue.Enqueue(~~((xx - 1) * grid.VCells + yy + 1));
                }
                if (allowdiag && f7 && f8 && f4)
                {
                    grid.Cells[(xx - 1) * grid.VCells + yy - 1] = d;
                    queue.Enqueue(~~((xx - 1) * grid.VCells + yy - 1));
                }
                if (allowdiag && f3 && f2 && f6)
                {
                    grid.Cells[(xx + 1) * grid.VCells + yy + 1] = d;
                    queue.Enqueue(~~((xx + 1) * grid.VCells + yy + 1));
                }
                if (allowdiag && f9 && f8 && f6)
                {
                    grid.Cells[(xx + 1) * grid.VCells + yy - 1] = d;
                    queue.Enqueue(~~((xx + 1) * grid.VCells + yy - 1));
                }
            }

            queue = null;

            if (result)
            {
                PathManager.Clear(path);
                path.kind = 0; // straight
                path.closed = false;
                PathManager.AddPoint(path, xgoal, ygoal, 100);
                var xx = cxg;
                var yy = cyg;

                while (xx != cxs || yy != cys)
                {
                    var val = grid.Cells[xx * grid.VCells + yy];
                    var f1 = xx > 0 && yy < grid.VCells - 1 && grid.Cells[(xx - 1) * grid.VCells + yy + 1] == val - 1;
                    var f2 = yy < grid.VCells - 1 && grid.Cells[xx * grid.VCells + yy + 1] == val - 1;
                    var f3 = xx < grid.HCells - 1 && yy < grid.VCells - 1 && grid.Cells[(xx + 1) * grid.VCells + yy + 1] == val - 1;
                    var f4 = xx > 0 && grid.Cells[(xx - 1) * grid.VCells + yy] == val - 1;
                    var f6 = xx < grid.HCells - 1 && grid.Cells[(xx + 1) * grid.VCells + yy] == val - 1;
                    var f7 = xx > 0 && yy > 0 && grid.Cells[(xx - 1) * grid.VCells + (yy - 1)] == val - 1;
                    var f8 = yy > 0 && grid.Cells[xx * grid.VCells + (yy - 1)] == val - 1;
                    var f9 = xx < grid.HCells - 1 && yy > 0 && grid.Cells[(xx + 1) * grid.VCells + (yy - 1)] == val - 1;

                    // Four directions movement
                    if (f4)
                    {
                        xx--;
                    }
                    else if (f6)
                    {
                        xx += 1;
                    }
                    else if (f8)
                    {
                        yy -= 1;
                    }
                    else if (f2)
                    {
                        yy += 1;
                    }
                    else if (allowdiag)
                    {
                        if (f1)
                        {
                            xx -= 1;
                            yy += 1;
                        }
                        else if (f3)
                        {
                            xx += 1;
                            yy += 1;
                        }
                        else if (f7)
                        {
                            xx -= 1;
                            yy -= 1;
                        }
                        else if (f9)
                        {
                            xx += 1;
                            yy -= 1;
                        }
                    }

                    if (xx != cxs || yy != cys)
                    {
                        PathManager.AddPoint(
                            path,
                            CustomMath.DoubleTilde(grid.Left + xx * grid.CellWidth + grid.CellWidth / 2.0),
                            CustomMath.DoubleTilde(grid.Top + yy * grid.CellHeight + grid.CellHeight / 2.0),
                            100);
                    }
                }

                PathManager.AddPoint(path, xstart, ystart, 100);
                PathManager.Reverse(path);
            }

            for (var i = 0; i < grid.HCells; i++)
            {
                for (var j = 0; j < grid.VCells; j++)
                {
                    if (grid.Cells[i * grid.VCells + j] > 0)
                        grid.Cells[i * grid.VCells + j] = 0;
                }
            }

            return result;
        }
    }

    public class MPGrid
    {
        public int Left;
        public int Top;
        public int HCells;
        public int VCells;
        public int CellWidth;
        public int CellHeight;
        public int[] Cells;

        public MPGrid(int left, int top, int hcells, int vcells, int cellWidth, int cellHeight)
        {
            Left = left;
            Top = top;
            HCells = hcells;
            VCells = vcells;
            CellWidth = cellWidth;
            CellHeight = cellHeight;
            Cells = new int[HCells * VCells];
        }

        public void Clear()
        {
            Cells = new int[HCells * VCells];
        }

        public void Poke(int h, int v, int val)
        {
            if (h < 0 || h >= HCells)
            {
                return;
            }

            if (v < 0 || v >= VCells)
            {
                return;
            }

            Cells[(h * VCells) + v] = val;
        }
    }
}

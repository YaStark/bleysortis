using Bleysortis.Main.Objects;
using System;
using System.Collections.Generic;

namespace Bleysortis.Main
{
    public class Map
    {
        private readonly SunAndMoon _sun;
        private readonly Cell[,] _cellsField;
        private readonly List<BaseObject> _items = new List<BaseObject>();

        public Map()
        {
            int w = 50;
            int h = 50;
            int min = -5;
            int max = 5;
            int zero = 0;
            int delta = max - min;
            double landscapeFreq = w / 3;

            _cellsField = new Cell[w, h];
            var random = new Random();
            float s = 1;
            float dx = s * MathF.Sqrt(3) / 2;
            float dy = s * 0.75f;
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    float x = i * dx + (j % 2 == 0 ? dx / 2 : 0);
                    float y = j * dy;
                    var cell = new Cell(x, y, GroundType.Grass);
                    cell.Level = zero + (random.Next(100) < 35 ? -1 : 0);
                    _cellsField[i, j] = cell;
                    _items.Add(cell);
                }
            }

            SetupNeighbours();

            for (int i = 0; i < landscapeFreq; i++)
            {
                switch (random.Next(2))
                {
                    case 0:
                        var maxLakeVel = Math.Max(delta / 3, 3);
                        var minLakeVel = Math.Max(delta / 8, maxLakeVel - 1);
                        AddLake(_cellsField, zero, min, random.Next(minLakeVel, maxLakeVel), random);
                        break;

                    case 1:
                        var maxMouVel = Math.Max(delta / 2, 3);
                        var minMouVel = Math.Max(delta / 4, maxMouVel - 1);
                        AddMountain(_cellsField, zero, max, random.Next(minMouVel, maxMouVel), random);
                        break;
                }
            }

            _sun = new SunAndMoon(w / 2, h / 2, w * 2);
            _items.Add(_sun);
        }

        public IEnumerable<BaseObject> EnumerateItems()
        {
            return _items;
        }

        private void SetupNeighbours()
        {
            int w = _cellsField.GetLength(0);
            int h = _cellsField.GetLength(1);
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    var cell = _cellsField[i, j];
                    if (j < h - 1)
                    {
                        int i0 = j % 2 == 0 ? i + 1 : i;
                        if (i0 < w)
                        {
                            cell.SetNeighbour(HexDirection.NE, _cellsField[i0, j + 1]);
                        }

                        int i1 = (j % 2 == 0 ? i : i - 1);
                        if (i1 >= 0)
                        {
                            cell.SetNeighbour(HexDirection.NW, _cellsField[i1, j + 1]);
                        }
                    }

                    if (i < w - 1)
                    {
                        cell.SetNeighbour(HexDirection.E, _cellsField[i + 1, j]);
                    }
                }
            }
        }

        private static void AddMountain(Cell[,] field, int zeroLvl, int topLvl, int velocity, Random rnd)
        {
            int width = field.GetLength(0);
            int height = field.GetLength(1);
            int topX = rnd.Next(2, width - 1);
            int topY = rnd.Next(2, height - 1);

            Queue<(Cell cell, int lvl)> queue = new();
            HashSet<Cell> hash = new();
            queue.Enqueue((field[topX, topY], topLvl));
            hash.Add(field[topX, topY]);
            while (queue.Count > 0)
            {
                var (cell, lvl) = queue.Dequeue();
                if (cell.Level > lvl) continue;
                cell.Level = Math.Max(lvl, zeroLvl);
                cell.GroundType = lvl >= topLvl - rnd.Next(1, 3)
                    ? GroundType.Snow
                    : GroundType.Rock;
                if (lvl > zeroLvl)
                {
                    foreach (HexDirection dir in Enum.GetValues<HexDirection>())
                    {
                        var neighbour = cell.GetNeighbour(dir);
                        if (neighbour != null && !hash.Contains(neighbour))
                        {
                            hash.Add(neighbour);
                            queue.Enqueue((neighbour, lvl - rnd.Next(velocity)));
                        }
                    }
                }
            }
        }

        private static void AddLake(Cell[,] field, int zeroLvl, int minLvl, int velocity, Random rnd)
        {
            int width = field.GetLength(0);
            int height = field.GetLength(1);
            int topX = rnd.Next(width / 4, width * 3 / 4);
            int topY = rnd.Next(height / 4, height * 3 / 4);

            Queue<(Cell cell, int lvl, HexDirection dir)> queue = new();
            HashSet<Cell> hash = new();
            queue.Enqueue((
                field[topX, topY],
                minLvl,
                HexDirection.E.Next(rnd.Next(10))));
            hash.Add(field[topX, topY]);
            while (queue.Count > 0)
            {
                var (cell, lvl, dir) = queue.Dequeue();
                if (lvl >= zeroLvl - 1) continue;

                cell.Level = lvl;
                cell.GroundType = lvl <= minLvl + (zeroLvl - minLvl) / 2
                        ? GroundType.Sand
                        : GroundType.Dirt;

                var nextDir = dir.Next(rnd.Next(-1, 2));
                var neighbour = cell.GetNeighbour(dir);
                if (neighbour != null && !hash.Contains(neighbour))
                {
                    queue.Enqueue((neighbour, lvl + rnd.Next(velocity), nextDir));
                    hash.Add(neighbour);
                }

                var opDir = dir.Opposite();
                var nextOpDir = opDir.Next(rnd.Next(-1, 2));
                var opNeighbour = cell.GetNeighbour(opDir);
                if (opNeighbour != null && !hash.Contains(opNeighbour))
                {
                    queue.Enqueue((opNeighbour, lvl + rnd.Next(velocity), nextOpDir));
                    hash.Add(opNeighbour);
                }
            }
        }
    }
}

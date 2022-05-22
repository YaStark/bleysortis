using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bleysortis.Main
{
    public class Cell : BaseMeshObject
    {
        private const float LEVEL_STEP = 0.1f;
        private const float INNER_AREA_RATIO = 0.8f;
        private const float NOIZE_LEVEL = 0.65f;

        private static readonly Random _rnd = new Random();

        private readonly Dictionary<HexDirection, Cell> _neighbours = new();
        private readonly Dictionary<HexDirection, Triangle[]> _triangles = new();

        private float Z => Level * LEVEL_STEP;
        private int _level;
        public GroundType GroundType { get; set; }
        public int Level 
        { 
            get { return _level; } 
            set { _level = value; Center = new Vector3(Center.X, Center.Y, Z);  } 
        }

        public Cell(GroundType groundType) 
        {
            GroundType = groundType;
            GenerateMainTriangles();
        }

        public Cell(float x, float y, GroundType groundType)
            : this(groundType)
        {
            Center = new Vector3(x, y, Z);
        }

        public Cell GetNeighbour(HexDirection direction)
        {
            return _neighbours.TryGetValue(direction, out var c) ? c : null;
        }

        public void SetNeighbour(HexDirection direction, Cell cell)
        {
            if(!_neighbours.TryGetValue(direction, out var c) || c != cell)
            {
                _neighbours[direction] = cell;
                cell.SetNeighbour(direction.Opposite(), this);
            }
        }

        private static float NextRnd(double from, double to = 1)
        {
            return (float)(from + _rnd.NextDouble() * (to - from));
        }

        private void GenerateMainTriangles()
        {
            float delta2 = MathF.PI / 6;
            float irad = INNER_AREA_RATIO / 2;
            var zrnd = (1 - NOIZE_LEVEL) * LEVEL_STEP;
            var pti0 = new Vector3(0, 0, NextRnd(-zrnd, zrnd));
            var ptix = new Vector3(
                irad * MathF.Cos(-delta2) * NextRnd(NOIZE_LEVEL),
                irad * MathF.Sin(-delta2) * NextRnd(NOIZE_LEVEL),
                NextRnd(-zrnd, zrnd));
            var pti1 = ptix;

            for (int i = 0; i < 6; i++)
            {
                float alpha = i * delta2 * 2;
                var pti2 = i == 5
                    ? ptix
                    : new Vector3(
                        irad * MathF.Cos(alpha + delta2) * NextRnd(NOIZE_LEVEL),
                        irad * MathF.Sin(alpha + delta2) * NextRnd(NOIZE_LEVEL),
                        NextRnd(-zrnd, zrnd));
                var pti12 = new Vector3(
                    irad * MathF.Cos(alpha) * NextRnd(NOIZE_LEVEL * NOIZE_LEVEL),
                    irad * MathF.Sin(alpha) * NextRnd(NOIZE_LEVEL * NOIZE_LEVEL),
                    NextRnd(-zrnd, zrnd));
                var triangle1 = new Triangle(pti0, pti1, pti12);
                var triangle2 = new Triangle(pti0, pti12, pti2);
                var direction = HexDirectionExt.ByAndleDeg((int)MathHelper.RadiansToDegrees(alpha));
                _triangles[direction] = new[] { triangle1, triangle2 };
                pti1 = pti2;
            }
        }

        protected override Triangle[] OnGetMesh()
        {
            var triangles = new List<Triangle>();
            var col0 = GroundType.GetColor();
            for (int i = 0; i < 6; i++)
            {
                int alpha = i * 30 * 2;
                var direction = HexDirectionExt.ByAndleDeg(alpha);

                var tri0 = _triangles[direction];
                foreach (var tri in tri0)
                {
                    triangles.Add(tri.CloneWithPoints()
                        .SetDefaultNormale()
                        .SetColor(col0));
                }

                if (i < 3)
                {
                    if (!_neighbours.TryGetValue(direction, out var cell1))
                    {
                        continue;
                    }

                    var tri1Offset = Center - cell1.Center;
                    var col1 = cell1.GroundType.GetColor();
                    var tri1 = cell1._triangles[direction.Opposite()];

                    for (int j = 0; j < tri1.Length; j++)
                    {
                        var jr = tri1.Length - j - 1;
                        triangles.AddRange(GetBevel(
                            GetStepsCount(this, cell1),
                            tri0[jr].Points[1], tri0[jr].Points[2],
                            tri1[j].Points[2] - tri1Offset, tri1[j].Points[1] - tri1Offset,
                            col0, col1));
                    }

                    var direction2 = direction.Next();
                    if (_neighbours.TryGetValue(direction2, out var cell2))
                    {
                        var pti2 = tri0.Last().Points[2];
                        var pto1g = tri1.First().Points[1] - tri1Offset;
                        var tri2 = cell2._triangles[direction2.Opposite()];
                        var pts = tri2.Last().Points[2] - Center + cell2.Center;
                        triangles.AddRange(GetCorner(
                            GetStepsCount(this, cell1), 
                            GetStepsCount(this, cell2),
                            GetStepsCount(cell1, cell2),
                            pti2, pto1g, pts,
                            col0, col1, cell2.GroundType.GetColor()));
                    }
                }
            }

            return triangles.ToArray();
        }

        private static int GetStepsCount(Cell cell0, Cell cell1)
        {
            return Math.Abs(cell0.Level - cell1.Level);
        }

        private static IEnumerable<Triangle> GetBevel(
            int steps,
            Vector3 pt00, Vector3 pt01, Vector3 pt10, Vector3 pt11,
            Color color0, Color color1)
        {
            var bevel1 = GetBevel(pt00, pt10, steps);
            var bevel2 = GetBevel(pt01, pt11, steps);
            var colors = new Lazy<Color[]>(() => GetColors(color0, color1, steps));
            Func<int, Color> getColor = color0 == color1
                ? _ => color0
                : i => colors.Value[i];

            for (int i = 0; i < bevel1.Length - 1; i++)
            {
                yield return new Triangle(bevel1[i], bevel1[i + 1], bevel2[i])
                    .SetDefaultNormale()
                    .SetColors(getColor(i), getColor(i + 1), getColor(i));

                yield return new Triangle(bevel2[i], bevel1[i + 1], bevel2[i + 1])
                    .SetDefaultNormale()
                    .SetColors(getColor(i), getColor(i + 1), getColor(i + 1));
            }
        }

        private static Triangle[] GetCorner(
            int steps01, int steps02, int steps12,
            Vector3 pt0, Vector3 pt1, Vector3 pt2, 
            Color color0, Color color1, Color color2)
        {
            if(steps01 == 0 && steps02 == 0 && steps12 == 0)
            {
                return new[] { 
                    new Triangle(pt0, pt1, pt2)
                        .SetDefaultNormale()
                        .SetColors(color0, color1, color2) 
                };
            }

            if (steps01 == 0 && steps02 == steps12)
            {
                return GetFlatCorner12(pt2, pt0, pt1, steps02, color2, color0, color1);
            }

            if (steps02 == 0 && steps01 == steps12)
            {
                return GetFlatCorner12(pt1, pt2, pt0, steps01, color1, color2, color0);
            }

            if (steps12 == 0 && steps01 == steps02)
            {
                return GetFlatCorner12(pt0, pt1, pt2, steps01, color0, color1, color2);
            }

            if (steps01 > steps02 && steps01 > steps12)
            {
                return GetCorner12(pt0, pt1, pt2, steps01, steps02, steps12, color0, color1, color2);
            }

            if (steps02 > steps01 && steps02 > steps12)
            {
                return GetCorner12(pt2, pt0, pt1, steps02, steps12, steps01, color2, color0, color1);
            }

            return GetCorner12(pt1, pt2, pt0, steps12, steps01, steps02, color1, color2, color0);
        }

        private static Triangle[] GetFlatCorner12(
            Vector3 pt0, Vector3 pt1, Vector3 pt2, 
            int steps, 
            Color color0, Color color1, Color color2)
        {
            var bevel1 = GetBevel(pt0, pt1, steps);
            var bevel2 = GetBevel(pt0, pt2, steps);
            var colors1 = GetColors(color0, color1, steps);
            var colors2 = GetColors(color0, color2, steps);

            var result = new Triangle[2 * bevel1.Length - 3];
            result[0] = new Triangle(pt0, bevel1[1], bevel2[1])
                .SetDefaultNormale()
                .SetColors(color0, colors1[1], colors2[1]);

            for (int i = 1; i < bevel1.Length - 1; i++)
            {
                result[i * 2 - 1] = new Triangle(bevel1[i], bevel1[i + 1], bevel2[i])
                    .SetDefaultNormale()
                    .SetColors(colors1[i], colors1[i + 1], colors2[i]);

                result[i * 2] = new Triangle(bevel2[i], bevel1[i + 1], bevel2[i + 1])
                    .SetDefaultNormale()
                    .SetColors(colors2[i], colors1[i + 1], colors2[i + 1]);
            }

            return result;
        }


        private static Triangle[] GetCorner12(
            Vector3 pt0, Vector3 pt1, Vector3 pt2,
            int steps01, int steps02, int steps12,
            Color color0, Color color1, Color color2)
        {
            var bevel01 = GetBevel(pt0, pt1, steps01);
            var bevel02 = GetBevel(pt0, pt2, steps02);
            var bevel12 = GetBevel(pt2, pt1, steps12);
            var colors01 = GetColors(color0, color1, steps01);
            var colors02 = GetColors(color0, color2, steps02);
            var colors12 = GetColors(color2, color1, steps12);

            int count = (bevel02.Length - 1) * 2 + (bevel12.Length - 2) * 2 + 1;
            var result = new Triangle[count];
            int i01 = 0;
            for (int i02 = 0; i02 < bevel02.Length - 1; i02++, i01++)
            {
                result[i01 * 2] = new Triangle(bevel01[i01], bevel01[i01 + 1], bevel02[i02])
                    .SetDefaultNormale()
                    .SetColors(colors01[i01], colors01[i01 + 1], colors02[i02]);

                result[i01 * 2 + 1] = new Triangle(bevel02[i02], bevel01[i01 + 1], bevel02[i02 + 1])
                    .SetDefaultNormale()
                    .SetColors(colors02[i02], colors01[i01 + 1], colors02[i02 + 1]);
            }

            result[i01 * 2] = new Triangle(pt2, bevel01[i01], bevel12[1])
                .SetDefaultNormale()
                .SetColors(color2, colors01[i01], colors12[1]);

            for (int i12 = 1; i12 < bevel12.Length - 1; i12++, i01++)
            {
                result[i01 * 2 + 1] = new Triangle(bevel01[i01], bevel01[i01 + 1], bevel12[i12])
                    .SetDefaultNormale()
                    .SetColors(colors01[i01], colors01[i01 + 1], colors12[i12]);

                result[i01 * 2 + 2] = new Triangle(bevel12[i12], bevel01[i01 + 1], bevel12[i12 + 1])
                    .SetDefaultNormale()
                    .SetColors(colors12[i12], colors01[i01 + 1], colors12[i12 + 1]);
            }

            return result;
        }

        private static Color[] GetColors(Color start, Color end, int steps)
        {
            if (steps == 0)
            {
                return new[] { start, end };
            }

            var result = new Color[steps * 2 + 2];
            result[0] = start;
            float dc = 0.5f / steps;
            float da = (end.A - start.A) * dc;
            float dr = (end.R - start.R) * dc;
            float dg = (end.G - start.G) * dc;
            float db = (end.B - start.B) * dc;

            for (int i = 1; i <= 2 * steps; i++)
            {
                result[i] = Color.FromArgb(
                    (int)(start.A + i * da),
                    (int)(start.R + i * dr),
                    (int)(start.G + i * dg),
                    (int)(start.B + i * db));
            }

            result[steps * 2 + 1] = end;
            return result;
        }

        private static Vector3[] GetBevel(Vector3 start, Vector3 end, int steps)
        {
            if (steps == 0)
            {
                return new[] { start, end };
            }

            var path = end - start;
            var segment = steps + 1;
            var dir0x = new Vector3(path.X, path.Y, 0) / segment;
            var dir0y = new Vector3(path.X / segment, path.Y / segment, path.Z) / segment;

            var vec = start;
            var result = new Vector3[steps * 2 + 2];
            result[0] = start;
            for (int i = 0; i < steps; i++)
            {
                vec = vec + dir0y;
                result[2 * i + 1] = vec;
                vec = vec + dir0x;
                result[2 * i + 2] = vec;
            }

            result[steps * 2 + 1] = end;
            return result;
        }
    }
}

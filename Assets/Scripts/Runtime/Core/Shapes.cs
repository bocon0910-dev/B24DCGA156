using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockCrush
{
    public sealed class Shape
    {
        public readonly int Id;
        public readonly Vector2Int[] Cells; // x right, y down, normalised to (0,0)
        public readonly int Width;
        public readonly int Height;
        public readonly float Weight;

        public Shape(int id, Vector2Int[] cells, float weight)
        {
            Id = id;
            Cells = cells;
            Width = cells.Max(c => c.x) + 1;
            Height = cells.Max(c => c.y) + 1;
            Weight = weight;
        }

        public int Size => Cells.Length;
    }

    /// <summary>Every piece that can appear, in a fixed order (ids are used by save data).</summary>
    public static class Shapes
    {
        public static readonly List<Shape> All = new List<Shape>();

        static Shapes()
        {
            // Family weights are split evenly between the family's rotations / mirrors.
            AddFamily(2.5f, false, "X");
            AddFamily(5f, false, "XX");
            AddFamily(6f, false, "XXX");
            AddFamily(5f, false, "XXXX");
            AddFamily(3f, false, "XXXXX");
            AddFamily(6f, false, "XX", "XX");
            AddFamily(2.5f, false, "XXX", "XXX", "XXX");
            AddFamily(3.5f, false, "XXX", "XXX");
            AddFamily(6f, false, "XX", "X.");
            AddFamily(6.5f, true, "X.", "X.", "XX");
            AddFamily(4f, false, "XXX", ".X.");
            AddFamily(3.5f, true, ".XX", "XX.");
            AddFamily(3f, false, "X..", "X..", "XXX");
        }

        public static Shape Get(int id) => id >= 0 && id < All.Count ? All[id] : null;

        static void AddFamily(float weight, bool mirror, params string[] rows)
        {
            var baseCells = Parse(rows);
            var variants = new List<Vector2Int[]>();
            var keys = new HashSet<string>();
            var current = baseCells;
            for (int m = 0; m < (mirror ? 2 : 1); m++)
            {
                for (int r = 0; r < 4; r++)
                {
                    var norm = Normalise(current);
                    if (keys.Add(Key(norm))) variants.Add(norm);
                    current = Rotate(current);
                }
                current = Mirror(baseCells);
            }
            foreach (var v in variants)
                All.Add(new Shape(All.Count, v, weight / variants.Count));
        }

        static Vector2Int[] Parse(string[] rows)
        {
            var list = new List<Vector2Int>();
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    if (rows[y][x] == 'X') list.Add(new Vector2Int(x, y));
            return list.ToArray();
        }

        static Vector2Int[] Rotate(Vector2Int[] cells) => cells.Select(c => new Vector2Int(-c.y, c.x)).ToArray();

        static Vector2Int[] Mirror(Vector2Int[] cells) => cells.Select(c => new Vector2Int(-c.x, c.y)).ToArray();

        static Vector2Int[] Normalise(Vector2Int[] cells)
        {
            int mx = cells.Min(c => c.x), my = cells.Min(c => c.y);
            return cells.Select(c => new Vector2Int(c.x - mx, c.y - my))
                .OrderBy(c => c.y).ThenBy(c => c.x).ToArray();
        }

        static string Key(Vector2Int[] cells) => string.Join(";", cells.Select(c => c.x + "," + c.y));
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockCrush
{
    public struct ClearedCell
    {
        public int X, Y, Color;
    }

    /// <summary>Pure 8x8 board logic. A cell holds -1 when empty, otherwise a colour index.</summary>
    public class BoardModel
    {
        public const int Size = 8;
        public const int Empty = -1;

        public readonly int[] Cells = new int[Size * Size];

        public BoardModel() => Reset();

        public void Reset()
        {
            for (int i = 0; i < Cells.Length; i++) Cells[i] = Empty;
        }

        public void CopyFrom(BoardModel other) => Array.Copy(other.Cells, Cells, Cells.Length);

        public void Load(int[] cells)
        {
            Reset();
            if (cells == null || cells.Length != Cells.Length) return;
            for (int i = 0; i < Cells.Length; i++)
                Cells[i] = cells[i] >= 0 && cells[i] < BlockColors.Count ? cells[i] : Empty;
        }

        public int Get(int x, int y) => Cells[y * Size + x];

        public bool IsFilled(int x, int y) => Cells[y * Size + x] != Empty;

        public bool IsBoardEmpty
        {
            get
            {
                foreach (var c in Cells) if (c != Empty) return false;
                return true;
            }
        }

        public bool CanPlace(Shape shape, int ox, int oy)
        {
            if (shape == null || ox < 0 || oy < 0 || ox + shape.Width > Size || oy + shape.Height > Size) return false;
            foreach (var c in shape.Cells)
                if (Cells[(oy + c.y) * Size + ox + c.x] != Empty) return false;
            return true;
        }

        public bool CanPlaceAnywhere(Shape shape)
        {
            if (shape == null) return false;
            for (int y = 0; y <= Size - shape.Height; y++)
                for (int x = 0; x <= Size - shape.Width; x++)
                    if (CanPlace(shape, x, y)) return true;
            return false;
        }

        public void Place(Shape shape, int ox, int oy, int color)
        {
            foreach (var c in shape.Cells)
                Cells[(oy + c.y) * Size + ox + c.x] = color;
        }

        /// <summary>Rows / columns that are completely filled right now.</summary>
        public void FindFullLines(List<int> rows, List<int> cols) => FindLines(null, 0, 0, rows, cols);

        /// <summary>Rows / columns that would be complete if <paramref name="shape"/> were placed (board unchanged).</summary>
        public void FindLines(Shape shape, int ox, int oy, List<int> rows, List<int> cols)
        {
            rows.Clear();
            cols.Clear();
            Span<bool> filled = stackalloc bool[Size * Size];
            for (int i = 0; i < Cells.Length; i++) filled[i] = Cells[i] != Empty;
            if (shape != null)
                foreach (var c in shape.Cells)
                    filled[(oy + c.y) * Size + ox + c.x] = true;

            for (int y = 0; y < Size; y++)
            {
                bool full = true;
                for (int x = 0; x < Size && full; x++) full = filled[y * Size + x];
                if (full) rows.Add(y);
            }
            for (int x = 0; x < Size; x++)
            {
                bool full = true;
                for (int y = 0; y < Size && full; y++) full = filled[y * Size + x];
                if (full) cols.Add(x);
            }
        }

        /// <summary>Empties the given lines. Cells shared by a row and a column are reported once.</summary>
        public void ClearLines(List<int> rows, List<int> cols, List<ClearedCell> cleared)
        {
            cleared?.Clear();
            foreach (var y in rows)
                for (int x = 0; x < Size; x++)
                    Take(x, y, cleared);
            foreach (var x in cols)
                for (int y = 0; y < Size; y++)
                    Take(x, y, cleared);
        }

        void Take(int x, int y, List<ClearedCell> cleared)
        {
            int i = y * Size + x;
            if (Cells[i] == Empty) return;
            cleared?.Add(new ClearedCell { X = x, Y = y, Color = Cells[i] });
            Cells[i] = Empty;
        }

        /// <summary>Number of occupied neighbours / walls around a shape placement. Used to rate positions.</summary>
        public int Contact(Shape shape, int ox, int oy)
        {
            int score = 0;
            foreach (var c in shape.Cells)
            {
                int x = ox + c.x, y = oy + c.y;
                score += Solid(x - 1, y) + Solid(x + 1, y) + Solid(x, y - 1) + Solid(x, y + 1);
            }
            return score;
        }

        int Solid(int x, int y) => x < 0 || y < 0 || x >= Size || y >= Size || Cells[y * Size + x] != Empty ? 1 : 0;
    }
}

using System.Collections.Generic;

namespace BlockCrush
{
    /// <summary>
    /// Deals sets of three pieces. Sets are checked with a greedy simulation so that, whenever possible,
    /// all three pieces can actually be placed; sometimes the set that clears the most lines is preferred.
    /// </summary>
    public class PieceGenerator
    {
        const int Attempts = 48;
        const int WantedCandidates = 8;

        readonly System.Random rnd;
        readonly BoardModel sim = new BoardModel();
        readonly List<int> rows = new List<int>();
        readonly List<int> cols = new List<int>();
        readonly List<Candidate> candidates = new List<Candidate>();

        struct Candidate
        {
            public Shape[] Set;
            public int Clears;
        }

        public PieceGenerator(int seed) => rnd = new System.Random(seed);

        public int RandomColor() => rnd.Next(BlockColors.Count);

        /// <summary>Three distinct colours for a new set.</summary>
        public int[] RandomColors()
        {
            var pool = new List<int>();
            for (int i = 0; i < BlockColors.Count; i++) pool.Add(i);
            var result = new int[3];
            for (int i = 0; i < 3; i++)
            {
                int k = rnd.Next(pool.Count);
                result[i] = pool[k];
                pool.RemoveAt(k);
            }
            return result;
        }

        public Shape[] NextSet(BoardModel board, int score)
        {
            candidates.Clear();
            int filled = 0;
            foreach (var c in board.Cells) if (c != BoardModel.Empty) filled++;
            float crowd = filled / 64f;

            for (int a = 0; a < Attempts && candidates.Count < WantedCandidates; a++)
            {
                var set = new[] { Pick(score, crowd), Pick(score, crowd), Pick(score, crowd) };
                if (Solvable(board, set, out int clears))
                    candidates.Add(new Candidate { Set = set, Clears = clears });
            }

            if (candidates.Count > 0)
            {
                // The fuller the board, the more often we help the player with the best set.
                float help = 0.25f + crowd * 0.6f;
                if (rnd.NextDouble() < help)
                {
                    var best = candidates[0];
                    foreach (var c in candidates) if (c.Clears > best.Clears) best = c;
                    return best.Set;
                }
                return candidates[rnd.Next(candidates.Count)].Set;
            }

            // Nothing fully solvable: at least try to give pieces that fit somewhere.
            var fallback = new Shape[3];
            for (int i = 0; i < 3; i++)
            {
                Shape s = null;
                for (int a = 0; a < 40 && s == null; a++)
                {
                    var t = Pick(score, 1f);
                    if (board.CanPlaceAnywhere(t)) s = t;
                }
                fallback[i] = s ?? Pick(score, 1f);
            }
            return fallback;
        }

        Shape Pick(int score, float crowd)
        {
            // Big pieces get a little more common as the score rises, small ones when the board is crowded.
            float difficulty = UnityEngine.Mathf.Clamp01(score / 6000f);
            float total = 0;
            foreach (var s in Shapes.All) total += Weight(s, difficulty, crowd);
            double r = rnd.NextDouble() * total;
            foreach (var s in Shapes.All)
            {
                r -= Weight(s, difficulty, crowd);
                if (r <= 0) return s;
            }
            return Shapes.All[Shapes.All.Count - 1];
        }

        static float Weight(Shape s, float difficulty, float crowd)
        {
            float w = s.Weight;
            if (s.Size >= 5) w *= 0.8f + difficulty * 0.6f;
            if (s.Size <= 3) w *= 1f + crowd * 0.8f;
            if (s.Size >= 6) w *= 1f - crowd * 0.5f;
            return w;
        }

        // Try every order; place each piece greedily (most lines, then most contact).
        bool Solvable(BoardModel board, Shape[] set, out int bestClears)
        {
            bestClears = -1;
            int[][] orders = { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 }, new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } };
            foreach (var order in orders)
            {
                sim.CopyFrom(board);
                int clears = 0;
                bool ok = true;
                foreach (var idx in order)
                {
                    if (!PlaceGreedy(set[idx], ref clears)) { ok = false; break; }
                }
                if (ok && clears > bestClears) bestClears = clears;
            }
            return bestClears >= 0;
        }

        bool PlaceGreedy(Shape shape, ref int clears)
        {
            int bestX = -1, bestY = -1, bestScore = int.MinValue;
            for (int y = 0; y <= BoardModel.Size - shape.Height; y++)
                for (int x = 0; x <= BoardModel.Size - shape.Width; x++)
                {
                    if (!sim.CanPlace(shape, x, y)) continue;
                    sim.FindLines(shape, x, y, rows, cols);
                    int score = (rows.Count + cols.Count) * 100 + sim.Contact(shape, x, y);
                    if (score > bestScore) { bestScore = score; bestX = x; bestY = y; }
                }
            if (bestX < 0) return false;
            sim.Place(shape, bestX, bestY, 0);
            sim.FindFullLines(rows, cols);
            clears += rows.Count + cols.Count;
            sim.ClearLines(rows, cols, null);
            return true;
        }
    }
}

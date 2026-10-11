using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush
{
    /// <summary>Classic (endless) mode: board, tray, scoring, combos, saving and game over.</summary>
    public class GameView : MonoBehaviour
    {
        const float SnapTolerance = 0.9f; // in cells
        const int PointsPerLineCell = 10;
        const int PerfectBonus = 500;
        static readonly Color Yellow = new Color32(0xFF, 0xE6, 0x00, 0xFF);
        static readonly Color ScoreBlue = new Color32(0x19, 0xBD, 0xF0, 0xFF);
        static readonly Color ScoreWhite = new Color32(0xDC, 0xEB, 0xFF, 0xFF);
        static readonly Color Shade = new Color(0.02f, 0.07f, 0.25f, 0.55f);
        static readonly string[] Praises = { "", "", "Good!", "Great!", "Excellent!", "Amazing!", "Unbelievable!" };

        GameApp app;
        GameAssets a;
        readonly BoardModel model = new BoardModel();
        PieceGenerator generator;
        BoardView board;
        RectTransform popups;
        readonly TraySlot[] slots = new TraySlot[3];
        Text bestText;
        Text scoreText;
        Image crown;

        int score;
        float shownScore;
        int combo;
        int movesSinceClear;
        int bestAtStart;
        bool gameOver;
        bool bestBeaten;

        TraySlot dragged;
        bool hasSnap;
        int snapX, snapY;

        readonly List<int> rows = new List<int>();
        readonly List<int> cols = new List<int>();
        readonly List<ClearedCell> cleared = new List<ClearedCell>();

        public bool IsPlaying => !gameOver;
        public bool CanDrag => !gameOver && dragged == null && !app.PopupOpen;

        public void Build(GameApp owner, GameAssets assets)
        {
            app = owner;
            a = assets;
            generator = new PieceGenerator(System.Environment.TickCount);

            UIKit.CoverImage("Background", transform, a.gameBg);
            var safe = UIKit.Stretch(UIKit.Rect("Safe", transform));
            safe.gameObject.AddComponent<SafeArea>();

            // header: best score (crown) + settings gear
            var top = new Vector2(0f, 1f);
            crown = UIKit.Image("Crown", safe, a.iconCrown, top, new Vector2(140f, -78f), UIKit.SpriteSize(a.iconCrown, 92f));
            bestText = UIKit.Text("Best", safe, a.fontExtraBold, 60, Yellow, top, new Vector2(140f, -150f), new Vector2(320f, 80f), "0");
            UIKit.AddShadow(bestText, Shade, new Vector2(0f, -4f));
            var gear = UIKit.Button("Settings", safe, a.iconGear, new Vector2(1f, 1f), new Vector2(-100f, -96f), UIKit.SpriteSize(a.iconGear, 104f), app.OpenSettings);
            gear.GetComponent<Image>().type = Image.Type.Simple;

            // score
            var topCenter = new Vector2(0.5f, 1f);
            var label = UIKit.Text("ScoreLabel", safe, a.fontExtraBold, 50, ScoreBlue, topCenter, new Vector2(0f, -150f), new Vector2(400f, 70f), "SCORE");
            UIKit.AddShadow(label, Shade, new Vector2(0f, -3f));
            scoreText = UIKit.Text("Score", safe, a.fontExtraBold, 124, ScoreWhite, topCenter, new Vector2(0f, -248f), new Vector2(700f, 150f), "0");
            UIKit.AddShadow(scoreText, Shade, new Vector2(0f, -7f));

            // board
            var boardRT = UIKit.Rect("Board", safe, UIKit.Center, new Vector2(0f, 40f), Vector2.one * BoardView.TotalSize);
            board = boardRT.gameObject.AddComponent<BoardView>();
            board.Build(a);
            popups = UIKit.Stretch(UIKit.Rect("Popups", boardRT));

            // tray + drag layer (drag layer last so the held piece is drawn above everything)
            var tray = UIKit.Rect("Tray", safe, UIKit.Center, new Vector2(0f, -680f), new Vector2(1080f, 340f));
            var drag = UIKit.Stretch(UIKit.Rect("DragLayer", safe));
            for (int i = 0; i < 3; i++)
            {
                var slotRT = UIKit.Rect("Slot " + i, tray, UIKit.Center, new Vector2((i - 1) * 340f, 0f), new Vector2(340f, 340f));
                slots[i] = slotRT.gameObject.AddComponent<TraySlot>();
                slots[i].Build(this, a, i, drag);
            }
        }

        // ---------- session ----------

        public void NewGame()
        {
            StopAllCoroutines();
            model.Reset();
            score = 0;
            shownScore = 0;
            combo = 0;
            movesSinceClear = 0;
            bestAtStart = SaveSystem.Best;
            bestBeaten = false;
            gameOver = false;
            ResetVisuals();
            DealNewSet(true);
            Save();
        }

        public void Resume(SavedGame g)
        {
            StopAllCoroutines();
            model.Load(g.cells);
            score = Mathf.Max(0, g.score);
            shownScore = score;
            combo = Mathf.Max(0, g.combo);
            movesSinceClear = Mathf.Max(0, g.movesSinceClear);
            bestAtStart = g.bestAtStart;
            bestBeaten = score > bestAtStart;
            gameOver = false;
            ResetVisuals();

            bool any = false;
            for (int i = 0; i < 3; i++)
            {
                var shape = Shapes.Get(g.trayShapes[i]);
                int color = Mathf.Clamp(g.trayColors[i], 0, BlockColors.Count - 1);
                slots[i].SetPiece(shape, color);
                any |= shape != null;
            }
            if (!any) DealNewSet(true);
            else for (int i = 0; i < 3; i++) slots[i].PlayAppear(i * 0.06f);
            UpdateFits();
            if (!AnyPieceFits()) StartCoroutine(GameOverRoutine());
        }

        void ResetVisuals()
        {
            dragged = null;
            hasSnap = false;
            board.ResetFx();
            board.Refresh(model);
            for (int i = popups.childCount - 1; i >= 0; i--) Destroy(popups.GetChild(i).gameObject);
            crown.transform.localScale = Vector3.one;
            UpdateScoreTexts();
        }

        public void SaveIfPlaying()
        {
            if (!gameOver) Save();
        }

        void Save()
        {
            if (gameOver)
            {
                SaveSystem.ClearGame();
                return;
            }
            var g = new SavedGame
            {
                cells = (int[])model.Cells.Clone(),
                trayShapes = new int[3],
                trayColors = new int[3],
                score = score,
                combo = combo,
                movesSinceClear = movesSinceClear,
                bestAtStart = bestAtStart,
            };
            for (int i = 0; i < 3; i++)
            {
                g.trayShapes[i] = slots[i].Shape?.Id ?? -1;
                g.trayColors[i] = slots[i].ColorIndex;
            }
            SaveSystem.SaveGame(g);
        }

        /// <summary>Called when a popup opens: drop whatever piece is held.</summary>
        public void CancelDrag()
        {
            foreach (var s in slots) s.CancelDrag();
            dragged = null;
            hasSnap = false;
            board.Refresh(model);
        }

        // ---------- dragging (called by TraySlot) ----------

        public void OnPieceLifted(TraySlot slot)
        {
            dragged = slot;
            hasSnap = false;
            app.Audio.Play(Sfx.Pick);
        }

        public void OnPieceDragged(TraySlot slot)
        {
            if (slot != dragged) return;
            Vector2 cell = board.WorldToCell(slot.FirstCellWorld());
            int cx = Mathf.RoundToInt(cell.x), cy = Mathf.RoundToInt(cell.y);
            bool found = false;
            int bx = 0, by = 0;
            float bestDist = SnapTolerance;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    float d = Vector2.Distance(cell, new Vector2(x, y));
                    if (d >= bestDist || !model.CanPlace(slot.Shape, x, y)) continue;
                    bestDist = d;
                    bx = x;
                    by = y;
                    found = true;
                }

            if (found == hasSnap && (!found || (bx == snapX && by == snapY))) return;
            hasSnap = found;
            snapX = bx;
            snapY = by;
            if (found) board.ShowPreview(model, slot.Shape, bx, by, slot.ColorIndex);
            else board.Refresh(model);
        }

        public bool OnPieceDropped(TraySlot slot)
        {
            bool ok = slot == dragged && hasSnap && !gameOver;
            dragged = null;
            if (!ok)
            {
                hasSnap = false;
                board.Refresh(model);
                app.Audio.Play(Sfx.Invalid, 1f, 0.6f);
                return false;
            }
            hasSnap = false;
            Place(slot, snapX, snapY);
            return true;
        }

        // ---------- rules ----------

        void Place(TraySlot slot, int ox, int oy)
        {
            var shape = slot.Shape;
            model.Place(shape, ox, oy, slot.ColorIndex);
            slot.Clear();
            board.Refresh(model);
            board.PopCells(shape, ox, oy);
            app.Audio.Play(Sfx.Place);

            Vector2 center = BoardView.ShapeCenter(shape, ox, oy);
            int gained = shape.Size;
            model.FindFullLines(rows, cols);
            int lines = rows.Count + cols.Count;
            if (lines > 0)
            {
                combo++;
                movesSinceClear = 0;
                model.ClearLines(rows, cols, cleared);
                board.Refresh(model);
                board.AnimateClear(cleared, center);

                int points = PointsPerLineCell * BoardModel.Size * lines * lines * Mathf.Max(1, combo);
                bool perfect = model.IsBoardEmpty;
                if (perfect) points += PerfectBonus;
                gained += points;

                float pitch = 1f + Mathf.Min(combo - 1, 10) * 0.06f;
                app.Audio.Play(Sfx.Clear, pitch);
                Vector2 popupPos = new Vector2(0f, Mathf.Clamp(center.y, -260f, 300f));
                if (combo >= 2)
                {
                    ShowCombo(combo, popupPos);
                    app.Audio.Play(Sfx.Combo, pitch, 0.8f);
                }
                if (perfect) ShowPraise("Perfect!", popupPos + new Vector2(0f, 150f));
                else if (lines >= 2) ShowPraise(Praises[Mathf.Min(lines, Praises.Length - 1)], popupPos + new Vector2(0f, 150f));
            }
            else
            {
                movesSinceClear++;
                if (movesSinceClear >= 3) combo = 0;
            }

            AddScore(gained, center);

            bool trayEmpty = true;
            foreach (var s in slots) trayEmpty &= s.Shape == null;
            if (trayEmpty) DealNewSet(true);

            UpdateFits();
            if (!AnyPieceFits())
            {
                gameOver = true;
                StartCoroutine(GameOverRoutine());
            }
            Save();
        }

        void DealNewSet(bool animate)
        {
            var set = generator.NextSet(model, score);
            var colors = generator.RandomColors();
            for (int i = 0; i < 3; i++)
            {
                slots[i].SetPiece(set[i], colors[i]);
                if (animate) slots[i].PlayAppear(0.05f + i * 0.07f);
            }
            if (animate) app.Audio.Play(Sfx.Spawn, 1f, 0.7f);
            UpdateFits();
        }

        bool AnyPieceFits()
        {
            foreach (var s in slots)
                if (s.Shape != null && model.CanPlaceAnywhere(s.Shape)) return true;
            return false;
        }

        void UpdateFits()
        {
            foreach (var s in slots)
                if (s.Shape != null) s.SetFits(model.CanPlaceAnywhere(s.Shape));
        }

        void AddScore(int points, Vector2 at)
        {
            score += points;
            if (score > SaveSystem.Best) SaveSystem.Best = score;
            if (!bestBeaten && bestAtStart > 0 && score > bestAtStart)
            {
                bestBeaten = true;
                Tween.Run(this, 0.6f, t => crown.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.35f));
            }
            ShowScorePopup(points, at);
        }

        IEnumerator GameOverRoutine()
        {
            gameOver = true;
            SaveSystem.ClearGame();
            yield return new WaitForSecondsRealtime(0.45f);
            yield return board.GrayOut(model, app.Audio);
            app.Audio.Play(Sfx.GameOver);
            yield return new WaitForSecondsRealtime(0.9f);
            bool newBest = score > bestAtStart && score > 0;
            if (score > SaveSystem.Best) SaveSystem.Best = score;
            app.ShowGameOver(score, newBest);
        }

        // ---------- HUD ----------

        void Update()
        {
            if (Mathf.Abs(shownScore - score) > 0.01f)
            {
                float speed = Mathf.Max(40f, Mathf.Abs(score - shownScore) * 7f);
                shownScore = Mathf.MoveTowards(shownScore, score, speed * Time.unscaledDeltaTime);
                UpdateScoreTexts();
            }
        }

        void UpdateScoreTexts()
        {
            int shown = Mathf.RoundToInt(shownScore);
            scoreText.text = shown.ToString();
            bestText.text = Mathf.Max(SaveSystem.Best, shown).ToString();
        }

        void ShowScorePopup(int points, Vector2 at)
        {
            var t = UIKit.Text("+" + points, popups, a.fontExtraBold, points >= 100 ? 76 : 62, Color.white, UIKit.Center, at,
                new Vector2(400f, 100f), "+" + points);
            UIKit.AddOutline(t, new Color(0.08f, 0.15f, 0.5f, 0.9f), 4f);
            var rt = t.rectTransform;
            Tween.Run(this, 0.9f, k =>
            {
                rt.anchoredPosition = at + Vector2.up * (110f * Ease.OutCubic(k));
                rt.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(0.6f, 1.1f, k / 0.15f) : 1f);
                t.color = new Color(1f, 1f, 1f, k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
            }, null, 0f, () => Destroy(t.gameObject));
        }

        void ShowCombo(int value, Vector2 at)
        {
            var root = UIKit.Rect("Combo", popups, UIKit.Center, at, new Vector2(700f, 200f));
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var word = UIKit.Image("Word", root, a.comboWord, UIKit.Center, new Vector2(-80f, 0f), UIKit.SpriteSize(a.comboWord, 400f));
            UIKit.AddShadow(word, new Color(0f, 0f, 0.2f, 0.5f), new Vector2(0f, -6f));
            var num = UIKit.Text("Num", root, a.fontBlackItalic, 170, new Color32(0xFF, 0xE2, 0x34, 0xFF), UIKit.Center,
                new Vector2(205f, 10f), new Vector2(220f, 200f), value.ToString());
            UIKit.AddOutline(num, new Color32(0x4B, 0x1E, 0x9A, 0xFF), 6f);
            UIKit.AddShadow(num, new Color(0f, 0f, 0.2f, 0.5f), new Vector2(0f, -9f));
            PopAndFade(root, group, at, 1.15f);
        }

        void ShowPraise(string text, Vector2 at)
        {
            var root = UIKit.Rect("Praise", popups, UIKit.Center, at, new Vector2(800f, 140f));
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var t = UIKit.Text("Text", root, a.fontBlackItalic, 104, new Color32(0xFF, 0xF2, 0x7A, 0xFF), UIKit.Center, Vector2.zero,
                new Vector2(800f, 140f), text);
            UIKit.AddOutline(t, new Color32(0xC2, 0x3B, 0x0E, 0xFF), 5f);
            UIKit.AddShadow(t, new Color(0.15f, 0f, 0.1f, 0.5f), new Vector2(0f, -9f));
            PopAndFade(root, group, at, 1.0f);
        }

        void PopAndFade(RectTransform root, CanvasGroup group, Vector2 at, float hold)
        {
            Tween.Scale(this, root, Vector3.zero, Vector3.one, 0.32f, Ease.OutBack);
            Tween.Run(this, 0.35f, k =>
            {
                group.alpha = 1f - k;
                root.anchoredPosition = at + Vector2.up * (60f * k);
            }, Ease.InQuad, 0.32f + hold, () => Destroy(root.gameObject));
        }
    }
}

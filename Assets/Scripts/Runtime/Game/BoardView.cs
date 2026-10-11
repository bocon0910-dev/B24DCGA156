using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush
{
    /// <summary>Visual 8x8 board: empty cells, block tiles, placement preview and clear effects.</summary>
    public class BoardView : MonoBehaviour
    {
        public const float Pitch = 120f;
        const float InnerSize = Pitch * BoardModel.Size;
        const float FrameMargin = 12f;
        const float CellGap = 3f;
        static readonly Color CellColor = new Color32(0x0D, 0x27, 0x6B, 0xFF);

        GameAssets assets;
        RectTransform inner;
        RectTransform tileLayer;
        RectTransform fxLayer;
        readonly Image[] tiles = new Image[BoardModel.Size * BoardModel.Size];
        readonly List<Image> fxPool = new List<Image>();
        readonly List<int> rows = new List<int>();
        readonly List<int> cols = new List<int>();

        public RectTransform Inner => inner;
        public RectTransform FxLayer => fxLayer;
        public static float TotalSize => InnerSize + FrameMargin * 2f;

        public void Build(GameAssets a)
        {
            assets = a;
            var frame = gameObject.AddComponent<Image>();
            frame.sprite = a.boardFrame;
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;

            inner = UIKit.Rect("Inner", transform, UIKit.Center, Vector2.zero, new Vector2(InnerSize, InnerSize));
            var cellLayer = UIKit.Rect("Cells", inner);
            UIKit.Stretch(cellLayer);
            for (int y = 0; y < BoardModel.Size; y++)
                for (int x = 0; x < BoardModel.Size; x++)
                    UIKit.Image($"Cell {x},{y}", cellLayer, a.whiteSquare, UIKit.Center, CellPos(x, y),
                        new Vector2(Pitch - CellGap * 2, Pitch - CellGap * 2), CellColor);

            tileLayer = UIKit.Stretch(UIKit.Rect("Tiles", inner));
            for (int y = 0; y < BoardModel.Size; y++)
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    var img = UIKit.Image($"Tile {x},{y}", tileLayer, null, UIKit.Center, CellPos(x, y), new Vector2(Pitch, Pitch));
                    img.enabled = false;
                    tiles[y * BoardModel.Size + x] = img;
                }

            fxLayer = UIKit.Stretch(UIKit.Rect("FX", inner));
        }

        public static Vector2 CellPos(int x, int y) =>
            new Vector2(-InnerSize / 2f + Pitch * (x + 0.5f), InnerSize / 2f - Pitch * (y + 0.5f));

        /// <summary>Position (in Inner space) of the centre of a shape placed at (ox, oy).</summary>
        public static Vector2 ShapeCenter(Shape s, int ox, int oy) =>
            CellPos(ox, oy) + new Vector2((s.Width - 1) * Pitch * 0.5f, -(s.Height - 1) * Pitch * 0.5f);

        /// <summary>Converts a world position to fractional cell coordinates (0,0 = centre of the top-left cell).</summary>
        public Vector2 WorldToCell(Vector3 world)
        {
            Vector2 local = inner.InverseTransformPoint(world);
            return new Vector2((local.x + InnerSize / 2f) / Pitch - 0.5f, (InnerSize / 2f - local.y) / Pitch - 0.5f);
        }

        void SetTile(int x, int y, int color, float alpha = 1f)
        {
            var img = tiles[y * BoardModel.Size + x];
            if (color < 0)
            {
                img.enabled = false;
                return;
            }
            img.enabled = true;
            img.sprite = assets.blockTiles[color];
            img.color = new Color(1f, 1f, 1f, alpha);
            img.rectTransform.localScale = Vector3.one;
        }

        public void Refresh(BoardModel model)
        {
            for (int y = 0; y < BoardModel.Size; y++)
                for (int x = 0; x < BoardModel.Size; x++)
                    SetTile(x, y, model.Get(x, y));
        }

        /// <summary>Ghost of the dragged piece; lines it would complete are recoloured with the piece colour.</summary>
        public void ShowPreview(BoardModel model, Shape shape, int ox, int oy, int color)
        {
            Refresh(model);
            foreach (var c in shape.Cells) SetTile(ox + c.x, oy + c.y, color, 0.45f);
            model.FindLines(shape, ox, oy, rows, cols);
            foreach (var y in rows)
                for (int x = 0; x < BoardModel.Size; x++) SetTile(x, y, color);
            foreach (var x in cols)
                for (int y = 0; y < BoardModel.Size; y++) SetTile(x, y, color);
        }

        public void PopCells(Shape shape, int ox, int oy)
        {
            foreach (var c in shape.Cells)
            {
                var t = tiles[(oy + c.y) * BoardModel.Size + ox + c.x].transform;
                Tween.Scale(this, t, Vector3.one * 1.18f, Vector3.one, 0.22f, Ease.OutBack);
            }
        }

        public void AnimateClear(List<ClearedCell> cells, Vector2 origin)
        {
            foreach (var c in cells)
            {
                Vector2 pos = CellPos(c.X, c.Y);
                float delay = Vector2.Distance(pos, origin) / Pitch * 0.035f;
                var img = GetFx(assets.blockTiles[c.Color], pos, new Vector2(Pitch, Pitch), Color.white);
                var rt = img.rectTransform;
                Tween.Run(this, 0.38f, t =>
                {
                    float s = t < 0.25f ? Mathf.Lerp(1f, 1.15f, t / 0.25f) : Mathf.Lerp(1.15f, 0f, Ease.InQuad((t - 0.25f) / 0.75f));
                    rt.localScale = Vector3.one * s;
                    rt.localRotation = Quaternion.Euler(0, 0, t * 25f);
                    img.color = new Color(1, 1, 1, 1f - Ease.InCubic(t));
                }, null, delay, () => Release(img));

                var tint = BlockColors.Tints[c.Color];
                for (int i = 0; i < 2; i++) Sparkle(pos, tint, delay);
                var glow = GetFx(assets.fxGlow, pos, new Vector2(Pitch * 1.6f, Pitch * 1.6f), new Color(tint.r, tint.g, tint.b, 0f));
                Tween.Run(this, 0.4f, t =>
                {
                    glow.color = new Color(tint.r, tint.g, tint.b, Mathf.Sin(t * Mathf.PI) * 0.7f);
                }, null, delay, () => Release(glow));
            }
        }

        void Sparkle(Vector2 pos, Color tint, float delay)
        {
            float size = Random.Range(28f, 54f);
            var img = GetFx(assets.fxSparkle, pos, new Vector2(size, size), new Color(1, 1, 1, 0));
            var rt = img.rectTransform;
            Vector2 dir = Random.insideUnitCircle.normalized * Random.Range(90f, 210f) + Vector2.up * 60f;
            float spin = Random.Range(-200f, 200f);
            Color c = Color.Lerp(tint, Color.white, 0.45f);
            Tween.Run(this, Random.Range(0.45f, 0.7f), t =>
            {
                rt.anchoredPosition = pos + dir * Ease.OutCubic(t) + Vector2.down * (t * t * 80f);
                rt.localRotation = Quaternion.Euler(0, 0, spin * t);
                rt.localScale = Vector3.one * (1f - t * 0.6f);
                img.color = new Color(c.r, c.g, c.b, t < 0.1f ? t * 10f : 1f - Ease.InQuad((t - 0.1f) / 0.9f));
            }, null, delay, () => Release(img));
        }

        public IEnumerator GrayOut(BoardModel model, AudioManager audio)
        {
            for (int y = 0; y < BoardModel.Size; y++)
            {
                bool any = false;
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    if (!model.IsFilled(x, y)) continue;
                    any = true;
                    var img = tiles[y * BoardModel.Size + x];
                    img.sprite = assets.grayTile;
                    Tween.Scale(this, img.transform, Vector3.one * 0.85f, Vector3.one, 0.2f, Ease.OutBack);
                }
                if (any) audio.Play(Sfx.Pick, 0.7f + y * 0.04f, 0.35f);
                yield return new WaitForSecondsRealtime(0.06f);
            }
        }

        Image GetFx(Sprite sprite, Vector2 pos, Vector2 size, Color color)
        {
            Image img;
            if (fxPool.Count > 0)
            {
                img = fxPool[fxPool.Count - 1];
                fxPool.RemoveAt(fxPool.Count - 1);
                img.gameObject.SetActive(true);
            }
            else
            {
                img = UIKit.Image("Fx", fxLayer, sprite, UIKit.Center, pos, size);
            }
            img.sprite = sprite;
            img.color = color;
            var rt = img.rectTransform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.SetAsLastSibling();
            return img;
        }

        void Release(Image img)
        {
            if (!img) return;
            img.gameObject.SetActive(false);
            fxPool.Add(img);
        }

        /// <summary>Hides any effect left over from interrupted animations.</summary>
        public void ResetFx()
        {
            StopAllCoroutines();
            foreach (Transform child in fxLayer)
            {
                if (!child.gameObject.activeSelf) continue;
                var img = child.GetComponent<Image>();
                if (img) Release(img);
            }
            foreach (var t in tiles) t.rectTransform.localScale = Vector3.one;
        }
    }
}

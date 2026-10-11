using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockCrush
{
    /// <summary>One of the three pieces waiting under the board. Handles picking up and dragging.</summary>
    public class TraySlot : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler, IPointerUpHandler
    {
        const float TrayCellSize = 58f;
        const float MaxTrayExtent = 290f;
        const float LiftAboveFinger = 190f;
        const float FollowSharpness = 38f;

        GameView game;
        GameAssets assets;
        RectTransform dragLayer;
        RectTransform piece;
        readonly List<Image> pieceTiles = new List<Image>();
        CanvasGroup pieceGroup;

        public int Index { get; private set; }
        public Shape Shape { get; private set; }
        public int ColorIndex { get; private set; }
        public bool IsDragging { get; private set; }
        public RectTransform Piece => piece;

        int pointerId;
        Vector2 targetPos;
        float trayScale = 0.5f;
        Coroutine anim;

        public void Build(GameView owner, GameAssets a, int index, RectTransform drag)
        {
            game = owner;
            assets = a;
            Index = index;
            dragLayer = drag;
            var hit = gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0); // invisible but still receives touches
            piece = UIKit.Rect("Piece", transform, UIKit.Center, Vector2.zero, Vector2.zero);
            pieceGroup = piece.gameObject.AddComponent<CanvasGroup>();
            pieceGroup.blocksRaycasts = false;
        }

        public void SetPiece(Shape shape, int color)
        {
            CancelDrag();
            StopAnim();
            ResetToTray();
            Shape = shape;
            ColorIndex = color;
            foreach (var t in pieceTiles) Destroy(t.gameObject);
            pieceTiles.Clear();
            piece.gameObject.SetActive(shape != null);
            if (shape == null) return;

            float pitch = BoardView.Pitch;
            piece.sizeDelta = new Vector2(shape.Width * pitch, shape.Height * pitch);
            foreach (var c in shape.Cells)
            {
                var pos = new Vector2((c.x - (shape.Width - 1) * 0.5f) * pitch, -(c.y - (shape.Height - 1) * 0.5f) * pitch);
                pieceTiles.Add(UIKit.Image("Tile", piece, assets.blockTiles[color], UIKit.Center, pos, new Vector2(pitch, pitch)));
            }
            trayScale = Mathf.Min(TrayCellSize / pitch, MaxTrayExtent / (Mathf.Max(shape.Width, shape.Height) * pitch));
            ResetToTray();
            SetFits(true);
        }

        public void ResetToTray()
        {
            piece.SetParent(transform, false);
            piece.anchoredPosition = Vector2.zero;
            piece.localScale = Vector3.one * trayScale;
        }

        public void PlayAppear(float delay)
        {
            if (Shape == null) return;
            StopAnim();
            anim = Tween.Move(this, piece, new Vector2(700f, 0f), Vector2.zero, 0.38f, Ease.OutBack, delay);
        }

        public void SetFits(bool value)
        {
            pieceGroup.alpha = value ? 1f : 0.35f;
        }

        public void Clear() => SetPiece(null, 0);

        /// <summary>World position of the centre of the piece's top-left cell.</summary>
        public Vector3 FirstCellWorld()
        {
            float pitch = BoardView.Pitch;
            var local = new Vector3(-(Shape.Width - 1) * 0.5f * pitch, (Shape.Height - 1) * 0.5f * pitch, 0f);
            return piece.TransformPoint(local);
        }

        // ---------- input ----------

        public void OnPointerDown(PointerEventData e)
        {
            if (IsDragging || Shape == null || !game.CanDrag) return;
            StopAnim();
            IsDragging = true;
            pointerId = e.pointerId;
            piece.SetParent(dragLayer, true);
            piece.SetAsLastSibling();
            UpdateTarget(e.position);
            game.OnPieceLifted(this);
        }

        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;

        public void OnDrag(PointerEventData e)
        {
            if (IsDragging && e.pointerId == pointerId) UpdateTarget(e.position);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!IsDragging || e.pointerId != pointerId) return;
            IsDragging = false;
            if (!game.OnPieceDropped(this)) ReturnToTray();
        }

        void UpdateTarget(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, screen, null, out var local);
            targetPos = local + new Vector2(0f, LiftAboveFinger + piece.sizeDelta.y * 0.5f);
        }

        void Update()
        {
            if (!IsDragging) return;
            float k = 1f - Mathf.Exp(-FollowSharpness * Time.unscaledDeltaTime);
            piece.anchoredPosition = Vector2.Lerp(piece.anchoredPosition, targetPos, k);
            piece.localScale = Vector3.Lerp(piece.localScale, Vector3.one, k);
            game.OnPieceDragged(this);
        }

        void ReturnToTray()
        {
            piece.SetParent(transform, true);
            Vector2 from = piece.anchoredPosition;
            Vector3 fromScale = piece.localScale;
            anim = Tween.Run(this, 0.22f, t =>
            {
                piece.anchoredPosition = Vector2.LerpUnclamped(from, Vector2.zero, t);
                piece.localScale = Vector3.LerpUnclamped(fromScale, Vector3.one * trayScale, t);
            }, Ease.OutCubic);
        }

        void StopAnim()
        {
            if (anim != null) StopCoroutine(anim);
            anim = null;
        }

        public void CancelDrag()
        {
            if (!IsDragging) return;
            IsDragging = false;
            ResetToTray();
        }

        void OnDisable() => CancelDrag();
    }
}

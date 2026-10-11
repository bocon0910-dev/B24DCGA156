using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockCrush
{
    /// <summary>Helpers to build uGUI hierarchies from code.</summary>
    public static class UIKit
    {
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = Rect(name, parent);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = Center;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size,
            Color? color = null, bool raycast = false)
        {
            var img = Rect(name, parent, anchor, pos, size).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color ?? Color.white;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image FullImage(string name, Transform parent, Sprite sprite, Color? color = null)
        {
            var img = Stretch(Rect(name, parent)).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color ?? Color.white;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Full-screen background that keeps the sprite aspect and covers the whole parent.</summary>
        public static Image CoverImage(string name, Transform parent, Sprite sprite)
        {
            var holder = Stretch(Rect(name, parent));
            var img = Rect("Image", holder, Center, Vector2.zero, new Vector2(100, 100)).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            var fit = img.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = sprite ? sprite.rect.width / sprite.rect.height : 0.5f;
            return img;
        }

        public static Text Text(string name, Transform parent, Font font, int size, Color color, Vector2 anchor, Vector2 pos,
            Vector2 box, string value = "", TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = Rect(name, parent, anchor, pos, box).gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.text = value;
            return t;
        }

        public static Shadow AddShadow(Graphic g, Color color, Vector2 distance)
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = color;
            s.effectDistance = distance;
            return s;
        }

        public static Outline AddOutline(Graphic g, Color color, float size)
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(size, -size);
            return o;
        }

        public static Button Button(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size,
            Action onClick, float sliceScale = 1.4f)
        {
            var img = Image(name, parent, sprite, anchor, pos, size, null, true);
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = sliceScale;
            var btn = img.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick?.Invoke());
            img.gameObject.AddComponent<PressScale>();
            return btn;
        }

        public static Image Icon(Transform parent, Sprite sprite, float height, Vector2 pos = default)
        {
            float aspect = sprite ? sprite.rect.width / sprite.rect.height : 1f;
            var img = Image("Icon", parent, sprite, Center, pos, new Vector2(height * aspect, height));
            AddShadow(img, new Color(0f, 0.1f, 0.3f, 0.45f), new Vector2(0, -5));
            return img;
        }

        public static Vector2 SpriteSize(Sprite s, float width)
        {
            if (!s) return new Vector2(width, width);
            return new Vector2(width, width * s.rect.height / s.rect.width);
        }
    }

    /// <summary>Squash the button a bit while pressed.</summary>
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Vector3 baseScale = Vector3.one;
        bool pressed;

        void Awake() => baseScale = transform.localScale;

        public void OnPointerDown(PointerEventData e)
        {
            pressed = true;
            transform.localScale = baseScale * 0.92f;
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        void Release()
        {
            if (!pressed) return;
            pressed = false;
            Tween.Scale(this, transform, baseScale * 0.92f, baseScale, 0.18f, Ease.OutBack);
        }

        void OnDisable()
        {
            pressed = false;
            transform.localScale = baseScale;
        }
    }

    /// <summary>Keeps its RectTransform inside Screen.safeArea (notches, home indicator).</summary>
    public class SafeArea : MonoBehaviour
    {
        Rect last;
        Vector2Int lastScreen;

        void OnEnable() => Apply();
        void Update() => Apply();

        void Apply()
        {
            var area = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (area == last && screen == lastScreen) return;
            last = area;
            lastScreen = screen;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rt.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}

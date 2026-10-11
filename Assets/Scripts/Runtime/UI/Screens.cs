using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush
{
    public class SplashView : MonoBehaviour
    {
        const float Duration = 1.9f;
        RectTransform logo;
        Image fill;

        public void Build(GameAssets a)
        {
            UIKit.CoverImage("Background", transform, a.splashBg);
            var safe = UIKit.Stretch(UIKit.Rect("Safe", transform));
            safe.gameObject.AddComponent<SafeArea>();
            logo = UIKit.Image("Logo", safe, a.logo, new Vector2(0.5f, 1f), new Vector2(0f, -400f), UIKit.SpriteSize(a.logo, 860f)).rectTransform;

            var bar = UIKit.Image("Bar", safe, a.whiteRound, new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(620f, 30f),
                new Color(0f, 0.05f, 0.25f, 0.6f));
            bar.type = Image.Type.Sliced;
            fill = UIKit.Image("Fill", bar.transform, a.whiteRound, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(30f, 30f),
                new Color32(0xFF, 0xD8, 0x2E, 0xFF));
            fill.type = Image.Type.Sliced;
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        }

        public void Play(Action done)
        {
            Tween.Scale(this, logo, Vector3.one * 0.6f, Vector3.one, 0.6f, Ease.OutBack, 0.15f);
            Tween.Run(this, Duration, t => fill.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(30f, 620f, t), 30f), Ease.InOutQuad, 0.1f, done);
        }
    }

    public class HomeView : MonoBehaviour
    {
        RectTransform logo;
        Text best;

        public void Build(GameApp app, GameAssets a)
        {
            UIKit.FullImage("Background", transform, a.homeBg);
            var safe = UIKit.Stretch(UIKit.Rect("Safe", transform));
            safe.gameObject.AddComponent<SafeArea>();

            logo = UIKit.Image("Logo", safe, a.logo, UIKit.Center, new Vector2(0f, 420f), UIKit.SpriteSize(a.logo, 940f)).rectTransform;

            var bestRow = UIKit.Rect("BestRow", safe, UIKit.Center, new Vector2(0f, 20f), new Vector2(600f, 90f));
            UIKit.Image("Crown", bestRow, a.iconCrown, UIKit.Center, new Vector2(-150f, 6f), UIKit.SpriteSize(a.iconCrown, 84f));
            best = UIKit.Text("Best", bestRow, a.fontExtraBold, 64, new Color32(0xFF, 0xE6, 0x00, 0xFF), UIKit.Center,
                new Vector2(100f, 0f), new Vector2(360f, 90f), "0", TextAnchor.MiddleLeft);
            UIKit.AddShadow(best, new Color(0.05f, 0.05f, 0.3f, 0.5f), new Vector2(0f, -5f));

            var classic = UIKit.Button("Classic", safe, a.buttonBlue, UIKit.Center, new Vector2(0f, -280f), new Vector2(580f, 180f),
                app.PlayClassic);
            var label = UIKit.Text("Label", classic.transform, a.fontExtraBold, 78, Color.white, UIKit.Center, new Vector2(0f, 8f),
                new Vector2(560f, 140f), "Classic");
            UIKit.AddShadow(label, new Color(0.02f, 0.15f, 0.45f, 0.8f), new Vector2(0f, -6f));
        }

        public void Refresh() => best.text = SaveSystem.Best.ToString();

        void Update()
        {
            float t = Time.unscaledTime;
            logo.anchoredPosition = new Vector2(0f, 420f + Mathf.Sin(t * 1.8f) * 14f);
            logo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f) * 1.2f);
        }
    }

    public class GameOverView : MonoBehaviour
    {
        static readonly Color LabelBlue = new Color32(0x19, 0xBD, 0xF0, 0xFF);
        static readonly Color LabelYellow = new Color32(0xFF, 0xE6, 0x00, 0xFF);

        GameApp app;
        GameAssets assets;
        RectTransform panel;
        Text label;
        Text number;
        Text title;
        CanvasGroup buttons;
        RectTransform sparkles;

        public void Build(GameApp owner, GameAssets a)
        {
            app = owner;
            assets = a;
            UIKit.FullImage("Background", transform, a.gameOverBg);
            var safe = UIKit.Stretch(UIKit.Rect("Safe", transform));
            safe.gameObject.AddComponent<SafeArea>();

            title = UIKit.Text("Title", safe, a.fontExtraBold, 124, new Color32(0xA6, 0xCB, 0xEA, 0xFF), UIKit.Center,
                new Vector2(0f, 560f), new Vector2(1000f, 160f), "Game Over");
            UIKit.AddShadow(title, new Color32(0x1E, 0x35, 0x6A, 0xFF), new Vector2(0f, -9f));

            panel = UIKit.Image("Panel", safe, a.panel, UIKit.Center, new Vector2(0f, 90f), UIKit.SpriteSize(a.panel, 840f)).rectTransform;
            sparkles = UIKit.Stretch(UIKit.Rect("Sparkles", panel));
            label = UIKit.Text("Label", panel, a.fontExtraBold, 56, LabelBlue, UIKit.Center, new Vector2(0f, 82f), new Vector2(700f, 80f), "SCORE");
            number = UIKit.Text("Score", panel, a.fontExtraBold, 180, Color.white, UIKit.Center, new Vector2(0f, -45f), new Vector2(700f, 200f), "0");
            UIKit.AddShadow(number, new Color(0f, 0.02f, 0.15f, 0.6f), new Vector2(0f, -8f));

            var row = UIKit.Rect("Buttons", safe, UIKit.Center, new Vector2(0f, -430f), new Vector2(900f, 160f));
            buttons = row.gameObject.AddComponent<CanvasGroup>();
            var home = UIKit.Button("Home", row, a.buttonBlue, UIKit.Center, new Vector2(-205f, 0f), new Vector2(370f, 140f), app.GoHome);
            UIKit.Icon(home.transform, a.iconHome, 82f, new Vector2(0f, 6f));
            var play = UIKit.Button("Replay", row, a.buttonYellow, UIKit.Center, new Vector2(205f, 0f), new Vector2(370f, 140f), app.Replay);
            UIKit.Icon(play.transform, a.iconPlay, 82f, new Vector2(6f, 6f));
        }

        public void Show(int score, bool newBest)
        {
            label.text = newBest ? "NEW BEST SCORE" : "SCORE";
            label.color = newBest ? LabelYellow : LabelBlue;
            number.text = "0";
            for (int i = sparkles.childCount - 1; i >= 0; i--) Destroy(sparkles.GetChild(i).gameObject);

            Tween.Scale(this, title.transform, Vector3.one * 0.5f, Vector3.one, 0.45f, Ease.OutBack);
            Tween.Scale(this, panel, Vector3.one * 0.6f, Vector3.one, 0.45f, Ease.OutBack, 0.08f);
            buttons.interactable = false;
            Tween.Fade(this, buttons, 0f, 1f, 0.3f, Ease.OutQuad, 0.6f, () => buttons.interactable = true);
            Tween.Run(this, Mathf.Clamp(score / 900f, 0.5f, 1.2f), t => number.text = Mathf.RoundToInt(score * t).ToString(),
                Ease.OutCubic, 0.35f, () =>
                {
                    Tween.Scale(this, number.transform, Vector3.one * 1.18f, Vector3.one, 0.3f, Ease.OutBack);
                    if (newBest) Celebrate();
                });
        }

        void Celebrate()
        {
            app.Audio.Play(Sfx.NewBest);
            for (int i = 0; i < 18; i++)
            {
                var img = UIKit.Image("Sparkle", sparkles, assets.fxSparkle, UIKit.Center, Vector2.zero, Vector2.one * UnityEngine.Random.Range(40f, 80f),
                    new Color(1f, 0.92f, 0.4f, 0f));
                var rt = img.rectTransform;
                Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(260f, 480f);
                float delay = i * 0.03f;
                Tween.Run(this, 0.9f, t =>
                {
                    rt.anchoredPosition = dir * Ease.OutCubic(t);
                    rt.localRotation = Quaternion.Euler(0f, 0f, t * 180f);
                    img.color = new Color(1f, 0.92f, 0.4f, Mathf.Sin(t * Mathf.PI));
                }, null, delay, () => Destroy(img.gameObject));
            }
        }
    }

    public class SettingsPopup : MonoBehaviour
    {
        static readonly Color OffTint = new Color(0.55f, 0.6f, 0.7f, 1f);

        RectTransform content;
        Image soundButton, musicButton;
        Text soundLabel, musicLabel;
        CanvasGroup group;

        public void Build(GameApp app, GameAssets a)
        {
            group = gameObject.AddComponent<CanvasGroup>();
            UIKit.FullImage("Dim", transform, null, new Color(0.01f, 0.03f, 0.12f, 0.78f)).raycastTarget = true;

            var safe = UIKit.Stretch(UIKit.Rect("Safe", transform));
            safe.gameObject.AddComponent<SafeArea>();
            content = UIKit.Rect("Content", safe, UIKit.Center, Vector2.zero, new Vector2(1000f, 1400f));

            var title = UIKit.Text("Title", content, a.fontExtraBold, 116, new Color32(0xA6, 0xCB, 0xEA, 0xFF), UIKit.Center,
                new Vector2(0f, 500f), new Vector2(1000f, 150f), "Settings");
            UIKit.AddShadow(title, new Color32(0x1E, 0x35, 0x6A, 0xFF), new Vector2(0f, -9f));

            var panel = UIKit.Image("Panel", content, a.panel, UIKit.Center, new Vector2(0f, 80f), UIKit.SpriteSize(a.panel, 780f)).rectTransform;
            soundButton = Toggle(panel, a, a.iconSound, new Vector2(-150f, 25f), out soundLabel, () =>
            {
                SaveSystem.SoundOn = !SaveSystem.SoundOn;
                Refresh();
                app.Audio.Play(Sfx.Button);
            });
            musicButton = Toggle(panel, a, a.iconMusic, new Vector2(150f, 25f), out musicLabel, () =>
            {
                SaveSystem.MusicOn = !SaveSystem.MusicOn;
                app.Audio.ApplyMusicSetting();
                Refresh();
                app.Audio.Play(Sfx.Button);
            });

            var home = UIKit.Button("Home", content, a.buttonBlue, UIKit.Center, new Vector2(-205f, -420f), new Vector2(370f, 140f), app.GoHome);
            UIKit.Icon(home.transform, a.iconHome, 82f, new Vector2(0f, 6f));
            var resume = UIKit.Button("Resume", content, a.buttonYellow, UIKit.Center, new Vector2(205f, -420f), new Vector2(370f, 140f), app.CloseSettings);
            UIKit.Icon(resume.transform, a.iconPlay, 82f, new Vector2(6f, 6f));
        }

        static Image Toggle(Transform parent, GameAssets a, Sprite icon, Vector2 pos, out Text label, Action onClick)
        {
            var btn = UIKit.Button("Toggle", parent, a.buttonBlue, UIKit.Center, pos, new Vector2(230f, 200f), onClick);
            UIKit.Icon(btn.transform, icon, 104f, new Vector2(0f, 8f));
            label = UIKit.Text("State", parent, a.fontExtraBold, 46, Color.white, UIKit.Center, pos + new Vector2(0f, -165f),
                new Vector2(260f, 70f));
            return btn.GetComponent<Image>();
        }

        public void Refresh()
        {
            Apply(soundButton, soundLabel, SaveSystem.SoundOn, "SOUND");
            Apply(musicButton, musicLabel, SaveSystem.MusicOn, "MUSIC");
        }

        static void Apply(Image button, Text label, bool on, string name)
        {
            button.color = on ? Color.white : OffTint;
            button.transform.GetChild(0).GetComponent<Image>().color = on ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            label.text = name + (on ? " ON" : " OFF");
            label.color = on ? Color.white : new Color(0.7f, 0.75f, 0.85f);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            Refresh();
            group.interactable = true;
            Tween.Fade(this, group, 0f, 1f, 0.18f, Ease.OutQuad);
            Tween.Scale(this, content, Vector3.one * 0.8f, Vector3.one, 0.3f, Ease.OutBack);
        }

        public void Hide()
        {
            if (!gameObject.activeSelf) return;
            group.interactable = false;
            Tween.Fade(this, group, group.alpha, 0f, 0.15f, Ease.InQuad, 0f, () => gameObject.SetActive(false));
        }
    }
}

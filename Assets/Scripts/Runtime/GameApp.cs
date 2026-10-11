using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BlockCrush
{
    /// <summary>Entry point: builds the whole UI from <see cref="GameAssets"/> and switches between screens.</summary>
    public class GameApp : MonoBehaviour
    {
        public GameAssets assets;

        public AudioManager Audio { get; private set; }
        public bool PopupOpen => settings.gameObject.activeSelf;

        SplashView splash;
        HomeView home;
        GameView game;
        GameOverView gameOver;
        SettingsPopup settings;
        CanvasGroup current;

        void Awake()
        {
            if (assets == null)
            {
                Debug.LogError("Block Crush: GameAssets is not assigned. Run menu 'Block Crush/Rebuild Game Scene'.");
                enabled = false;
                return;
            }

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;

            Audio = new GameObject("Audio").AddComponent<AudioManager>();
            Audio.transform.SetParent(transform);
            Audio.Init(assets);

            EnsureEventSystem();
            var canvas = BuildCanvas();

            splash = CreateView<SplashView>(canvas, "Splash");
            splash.Build(assets);
            home = CreateView<HomeView>(canvas, "Home");
            home.Build(this, assets);
            game = CreateView<GameView>(canvas, "Game");
            game.Build(this, assets);
            gameOver = CreateView<GameOverView>(canvas, "GameOver");
            gameOver.Build(this, assets);
            settings = CreateView<SettingsPopup>(canvas, "Settings");
            settings.Build(this, assets);
            settings.gameObject.SetActive(false);
            home.gameObject.SetActive(false);
            game.gameObject.SetActive(false);
            gameOver.gameObject.SetActive(false);
        }

        void Start()
        {
            Show(splash, false);
            splash.Play(ShowHome);
        }

        // ---------- navigation ----------

        public void ShowHome()
        {
            home.Refresh();
            Show(home);
        }

        public void PlayClassic()
        {
            Audio.Play(Sfx.Button);
            Show(game);
            var saved = SaveSystem.LoadGame();
            if (saved != null) game.Resume(saved);
            else game.NewGame();
        }

        public void Replay()
        {
            Audio.Play(Sfx.Button);
            Show(game);
            game.NewGame();
        }

        public void GoHome()
        {
            Audio.Play(Sfx.Button);
            if (current == game.GetComponent<CanvasGroup>()) game.SaveIfPlaying();
            settings.Hide();
            ShowHome();
        }

        public void OpenSettings()
        {
            Audio.Play(Sfx.Button);
            game.CancelDrag();
            settings.transform.SetAsLastSibling();
            settings.Show();
        }

        public void CloseSettings()
        {
            Audio.Play(Sfx.Button);
            settings.Hide();
        }

        public void ShowGameOver(int score, bool newBest)
        {
            Show(gameOver);
            gameOver.Show(score, newBest);
        }

        void Show(MonoBehaviour view, bool fade = true)
        {
            var group = view.GetComponent<CanvasGroup>();
            var previous = current;
            current = group;
            if (previous != null && previous != group)
            {
                previous.interactable = false;
                previous.blocksRaycasts = false;
                if (fade)
                    Tween.Fade(this, previous, previous.alpha, 0f, 0.2f, Ease.InQuad, 0f, () =>
                    {
                        if (current != previous) previous.gameObject.SetActive(false);
                    });
                else previous.gameObject.SetActive(false);
            }
            view.gameObject.SetActive(true);
            view.transform.SetAsLastSibling();
            settings.transform.SetAsLastSibling();
            group.interactable = true;
            group.blocksRaycasts = true;
            if (fade) Tween.Fade(this, group, 0f, 1f, 0.25f, Ease.OutQuad);
            else group.alpha = 1f;
        }

        // ---------- lifecycle ----------

        void OnApplicationPause(bool paused)
        {
            if (paused && game != null && game.gameObject.activeSelf) game.SaveIfPlaying();
        }

        void OnApplicationQuit()
        {
            if (game != null && game.gameObject.activeSelf) game.SaveIfPlaying();
        }

        // ---------- construction ----------

        RectTransform BuildCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = 100f;
            return (RectTransform)go.transform;
        }

        static T CreateView<T>(RectTransform canvas, string name) where T : MonoBehaviour
        {
            var rt = UIKit.Stretch(UIKit.Rect(name, canvas));
            rt.gameObject.AddComponent<CanvasGroup>();
            rt.gameObject.SetActive(false);
            var view = rt.gameObject.AddComponent<T>();
            rt.gameObject.SetActive(true);
            return view;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            es.GetComponent<EventSystem>().pixelDragThreshold = 4;
        }
    }
}

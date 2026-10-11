using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace BlockCrush.EditorTools
{
    /// <summary>
    /// One-click setup: configures sprite import settings, bakes the procedural audio to WAV files,
    /// creates the GameAssets asset and the Main scene, and sets portrait player settings.
    /// Runs automatically the first time the project is opened (when Main.unity does not exist yet).
    /// </summary>
    [InitializeOnLoad]
    public static class BlockCrushBuilder
    {
        const string ArtDir = "Assets/Art";
        const string AudioDir = "Assets/Audio";
        const string DataPath = "Assets/Data/GameAssets.asset";
        const string ScenePath = "Assets/Scenes/Main.unity";

        static readonly string[] TileOrder = { "red", "orange", "yellow", "green", "cyan", "blue", "purple", "magenta" };

        static BlockCrushBuilder()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(ScenePath)) return;
                if (SessionState.GetBool("BlockCrushAutoBuild", false)) return;
                SessionState.SetBool("BlockCrushAutoBuild", true);
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Build();
            };
        }

        [MenuItem("Block Crush/Rebuild Game Scene", priority = 0)]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        [MenuItem("Block Crush/Reset Save Data (Best Score, Game)", priority = 20)]
        public static void ResetSave()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("Block Crush: PlayerPrefs cleared.");
        }

        /// <summary>Entry point, also usable from the command line with -executeMethod.</summary>
        public static void Build()
        {
            ConfigureTextures();
            BakeAudio(false);
            var data = CreateGameAssets();
            CreateScene(data);
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            Debug.Log("Block Crush: game scene built at " + ScenePath + ". Press Play!");
        }

        // ---------- textures ----------

        static void ConfigureTextures()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                bool background = path.Contains("/Backgrounds/");

                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = name.StartsWith("tile_"); // tiles are shown small in the tray

                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.filterMode = FilterMode.Bilinear;
                ti.maxTextureSize = background ? 4096 : 2048;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.spritePixelsPerUnit = 100;

                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(settings);

                ti.spriteBorder = name switch
                {
                    "btn_blue" => new Vector4(80, 70, 80, 60),
                    "btn_yellow" => new Vector4(80, 70, 80, 60),
                    "board_frame" => new Vector4(28, 28, 28, 28),
                    "white_round" => new Vector4(15, 15, 15, 15),
                    _ => Vector4.zero,
                };
                ti.SaveAndReimport();
            }
        }

        static Sprite S(string relative)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{relative}.png");
            if (sprite == null) Debug.LogWarning("Block Crush: missing sprite " + relative);
            return sprite;
        }

        // ---------- audio ----------

        [MenuItem("Block Crush/Regenerate Audio Files", priority = 21)]
        public static void RegenerateAudio()
        {
            BakeAudio(true);
            CreateGameAssets();
            AssetDatabase.SaveAssets();
        }

        static void BakeAudio(bool overwrite)
        {
            Directory.CreateDirectory(AudioDir);
            foreach (var id in SfxSynth.Ids)
            {
                string path = $"{AudioDir}/{FileName(id)}.wav";
                if (!overwrite && File.Exists(path)) continue;
                var samples = SfxSynth.Render(id, out int rate);
                WriteWav(path, samples, rate);
            }
            AssetDatabase.Refresh();
            foreach (var id in SfxSynth.Ids)
            {
                var path = $"{AudioDir}/{FileName(id)}.wav";
                if (!(AssetImporter.GetAtPath(path) is AudioImporter ai)) continue;
                var s = ai.defaultSampleSettings;
                bool music = id == "music";
                s.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = music ? 0.6f : 0.8f;
                ai.defaultSampleSettings = s;
                ai.forceToMono = true;
                ai.SaveAndReimport();
            }
        }

        static string FileName(string id) => id == "music" ? "music_loop" : "sfx_" + id;

        static AudioClip A(string id) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{FileName(id)}.wav");

        static void WriteWav(string path, float[] samples, int rate)
        {
            using var fs = new FileStream(path, FileMode.Create);
            using var w = new BinaryWriter(fs);
            int dataSize = samples.Length * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + dataSize);
            w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            w.Write(16);
            w.Write((short)1);      // PCM
            w.Write((short)1);      // mono
            w.Write(rate);
            w.Write(rate * 2);      // byte rate
            w.Write((short)2);      // block align
            w.Write((short)16);     // bits per sample
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(dataSize);
            foreach (var v in samples) w.Write((short)Mathf.RoundToInt(Mathf.Clamp(v, -1f, 1f) * 32767f));
        }

        // ---------- data asset ----------

        static GameAssets CreateGameAssets()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DataPath));
            var data = AssetDatabase.LoadAssetAtPath<GameAssets>(DataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<GameAssets>();
                AssetDatabase.CreateAsset(data, DataPath);
            }

            data.splashBg = S("Backgrounds/splash");
            data.homeBg = S("Backgrounds/home_bg");
            data.gameBg = S("Backgrounds/game_bg");
            data.gameOverBg = S("Backgrounds/gameover_bg");

            data.logo = S("Sprites/logo");
            data.panel = S("Sprites/panel");
            data.buttonBlue = S("Sprites/btn_blue");
            data.buttonYellow = S("Sprites/btn_yellow");
            data.boardFrame = S("Sprites/board_frame");
            data.whiteSquare = S("Sprites/white_square");
            data.whiteRound = S("Sprites/white_round");
            data.iconCrown = S("Sprites/icon_crown");
            data.iconGear = S("Sprites/icon_gear");
            data.iconHome = S("Sprites/icon_home");
            data.iconPlay = S("Sprites/icon_play");
            data.iconSound = S("Sprites/icon_sound");
            data.iconMusic = S("Sprites/icon_music");
            data.comboWord = S("Sprites/combo_word");
            data.blockTiles = TileOrder.Select(n => S("Sprites/tile_" + n)).ToArray();
            data.grayTile = S("Sprites/tile_gray");
            data.fxGlow = S("FX/fx_glow");
            data.fxSparkle = S("FX/fx_sparkle");

            data.fontBold = AssetDatabase.LoadAssetAtPath<Font>($"{ArtDir}/Fonts/Montserrat-Bold.ttf");
            data.fontExtraBold = AssetDatabase.LoadAssetAtPath<Font>($"{ArtDir}/Fonts/Montserrat-ExtraBold.ttf");
            data.fontBlackItalic = AssetDatabase.LoadAssetAtPath<Font>($"{ArtDir}/Fonts/Montserrat-BlackItalic.ttf");

            data.sfxPick = A("pick");
            data.sfxPlace = A("place");
            data.sfxClear = A("clear");
            data.sfxCombo = A("combo");
            data.sfxInvalid = A("invalid");
            data.sfxButton = A("button");
            data.sfxSpawn = A("spawn");
            data.sfxGameOver = A("gameover");
            data.sfxNewBest = A("newbest");
            data.music = A("music");

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            return data;
        }

        // ---------- scene ----------

        static void CreateScene(GameAssets data)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x0A, 0x2A, 0x8A, 0xFF);
            cam.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            es.GetComponent<EventSystem>().pixelDragThreshold = 4;

            var app = new GameObject("GameApp").AddComponent<GameApp>();
            app.assets = data;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).Select(s => { s.enabled = false; return s; }).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.productName = "Block Crush";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
        }
    }
}

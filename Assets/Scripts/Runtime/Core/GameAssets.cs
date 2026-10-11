using UnityEngine;

namespace BlockCrush
{
    /// <summary>All art / audio references used by the game. Filled automatically by the editor builder.</summary>
    [CreateAssetMenu(menuName = "Block Crush/Game Assets", fileName = "GameAssets")]
    public class GameAssets : ScriptableObject
    {
        [Header("Backgrounds")]
        public Sprite splashBg;
        public Sprite homeBg;
        public Sprite gameBg;
        public Sprite gameOverBg;

        [Header("UI")]
        public Sprite logo;
        public Sprite panel;
        public Sprite buttonBlue;
        public Sprite buttonYellow;
        public Sprite boardFrame;
        public Sprite whiteSquare;
        public Sprite whiteRound;
        public Sprite iconCrown;
        public Sprite iconGear;
        public Sprite iconHome;
        public Sprite iconPlay;
        public Sprite iconSound;
        public Sprite iconMusic;
        public Sprite comboWord;

        [Header("Blocks (order must match BlockColors.Tints)")]
        public Sprite[] blockTiles;
        public Sprite grayTile;

        [Header("FX")]
        public Sprite fxGlow;
        public Sprite fxSparkle;

        [Header("Fonts")]
        public Font fontBold;
        public Font fontExtraBold;
        public Font fontBlackItalic;

        [Header("Audio (procedurally generated when empty)")]
        public AudioClip sfxPick;
        public AudioClip sfxPlace;
        public AudioClip sfxClear;
        public AudioClip sfxCombo;
        public AudioClip sfxInvalid;
        public AudioClip sfxButton;
        public AudioClip sfxSpawn;
        public AudioClip sfxGameOver;
        public AudioClip sfxNewBest;
        public AudioClip music;
    }

    public static class BlockColors
    {
        // Particle tints matching GameAssets.blockTiles: red, orange, yellow, green, cyan, blue, purple, magenta.
        public static readonly Color[] Tints =
        {
            new Color32(0xE8, 0x3A, 0x3A, 0xFF),
            new Color32(0xF5, 0x7C, 0x10, 0xFF),
            new Color32(0xE6, 0xD8, 0x10, 0xFF),
            new Color32(0x3C, 0xC8, 0x1E, 0xFF),
            new Color32(0x1E, 0xDC, 0xD6, 0xFF),
            new Color32(0x1E, 0x7C, 0xFF, 0xFF),
            new Color32(0x9B, 0x2C, 0xF0, 0xFF),
            new Color32(0xE0, 0x3C, 0xD8, 0xFF),
        };

        public static int Count => Tints.Length;
    }
}

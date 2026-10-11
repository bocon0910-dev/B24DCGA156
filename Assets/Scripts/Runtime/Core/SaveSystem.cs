using System;
using UnityEngine;

namespace BlockCrush
{
    [Serializable]
    public class SavedGame
    {
        public int[] cells;
        public int[] trayShapes;   // shape id, -1 when the slot is already used
        public int[] trayColors;
        public int score;
        public int combo;
        public int movesSinceClear;
        public int bestAtStart;
    }

    /// <summary>PlayerPrefs-backed persistence: best score, settings and the classic game in progress.</summary>
    public static class SaveSystem
    {
        const string BestKey = "bc_best";
        const string GameKey = "bc_classic_game";
        const string SoundKey = "bc_sound";
        const string MusicKey = "bc_music";

        public static int Best
        {
            get => PlayerPrefs.GetInt(BestKey, 0);
            set { PlayerPrefs.SetInt(BestKey, value); PlayerPrefs.Save(); }
        }

        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set { PlayerPrefs.SetInt(SoundKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set { PlayerPrefs.SetInt(MusicKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool HasGame => PlayerPrefs.HasKey(GameKey);

        public static void SaveGame(SavedGame game)
        {
            PlayerPrefs.SetString(GameKey, JsonUtility.ToJson(game));
            PlayerPrefs.Save();
        }

        public static SavedGame LoadGame()
        {
            if (!HasGame) return null;
            try
            {
                var g = JsonUtility.FromJson<SavedGame>(PlayerPrefs.GetString(GameKey));
                if (g?.cells == null || g.cells.Length != 64 || g.trayShapes == null || g.trayShapes.Length != 3 ||
                    g.trayColors == null || g.trayColors.Length != 3) return null;
                return g;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Block Crush: corrupt save ignored. " + e.Message);
                return null;
            }
        }

        public static void ClearGame()
        {
            PlayerPrefs.DeleteKey(GameKey);
            PlayerPrefs.Save();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace BlockCrush
{
    public enum Sfx { Pick, Place, Clear, Combo, Invalid, Button, Spawn, GameOver, NewBest }

    public class AudioManager : MonoBehaviour
    {
        const int Voices = 8;
        const float MusicVolume = 0.35f;

        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        readonly List<AudioSource> voices = new List<AudioSource>();
        AudioSource musicSource;
        int nextVoice;

        public void Init(GameAssets a)
        {
            clips[Sfx.Pick] = a.sfxPick ? a.sfxPick : SfxSynth.CreateClip("pick");
            clips[Sfx.Place] = a.sfxPlace ? a.sfxPlace : SfxSynth.CreateClip("place");
            clips[Sfx.Clear] = a.sfxClear ? a.sfxClear : SfxSynth.CreateClip("clear");
            clips[Sfx.Combo] = a.sfxCombo ? a.sfxCombo : SfxSynth.CreateClip("combo");
            clips[Sfx.Invalid] = a.sfxInvalid ? a.sfxInvalid : SfxSynth.CreateClip("invalid");
            clips[Sfx.Button] = a.sfxButton ? a.sfxButton : SfxSynth.CreateClip("button");
            clips[Sfx.Spawn] = a.sfxSpawn ? a.sfxSpawn : SfxSynth.CreateClip("spawn");
            clips[Sfx.GameOver] = a.sfxGameOver ? a.sfxGameOver : SfxSynth.CreateClip("gameover");
            clips[Sfx.NewBest] = a.sfxNewBest ? a.sfxNewBest : SfxSynth.CreateClip("newbest");

            for (int i = 0; i < Voices; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                voices.Add(s);
            }

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = a.music ? a.music : SfxSynth.CreateClip("music");
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = MusicVolume;
            ApplyMusicSetting();
        }

        public void Play(Sfx id, float pitch = 1f, float volume = 1f)
        {
            if (!SaveSystem.SoundOn || !clips.TryGetValue(id, out var clip)) return;
            var v = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Count;
            v.pitch = pitch;
            v.volume = volume;
            v.clip = clip;
            v.Play();
        }

        public void ApplyMusicSetting()
        {
            if (musicSource == null) return;
            if (SaveSystem.MusicOn && !musicSource.isPlaying) musicSource.Play();
            else if (!SaveSystem.MusicOn && musicSource.isPlaying) musicSource.Stop();
        }

        public void DuckMusic(bool duck) => musicSource.volume = duck ? MusicVolume * 0.4f : MusicVolume;
    }
}

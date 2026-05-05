using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Boss Fight Sounds")]
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip roundClearSound;

    [Header("Player Sounds")]
    public AudioClip fallSound;
    public AudioClip respawnSound;
    public AudioClip moveSound;

    [Header("Music")]
    public AudioClip gameMusic;
    public AudioClip bossFightMusic;

    private AudioSource sfxSource;
    private AudioSource musicSource;

    private float _targetMusicVolume = 0.5f;
    private Coroutine _fadeCoroutine;

    // Next SoundManager instance starts music silent and fades in.
    public static bool PendingMusicFadeIn { get; set; }
    public static float PendingFadeInDuration { get; set; } = 8f;

    void Awake()
    {
        Instance = this;

        sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.5f;

        GameSettings.ApplyAudio();
        GameSettings.OnAudioChanged += OnAudioSettingsChanged;
    }

    void OnDestroy()
    {
        GameSettings.OnAudioChanged -= OnAudioSettingsChanged;
        if (Instance == this) Instance = null;
    }

    private void OnAudioSettingsChanged()
    {
        SetVolumes(GameSettings.MasterVolume, GameSettings.MusicVolume, GameSettings.SFXVolume);
    }

    void Start()
    {
        if (PendingMusicFadeIn)
        {
            PendingMusicFadeIn = false;
            float fadeDur = PendingFadeInDuration;

            // Start silent, fade in during the shimmer reveal
            musicSource.clip = gameMusic;
            musicSource.volume = 0f;
            musicSource.Play();
            _fadeCoroutine = StartCoroutine(FadeMusicCoroutine(0f, _targetMusicVolume, fadeDur));
        }
        else
        {
            PlayGameMusic();
        }
    }

    public void PlayWin()        { if (winSound != null) sfxSource.PlayOneShot(winSound); }
    public void PlayLose()       { if (loseSound != null) sfxSource.PlayOneShot(loseSound); }
    public void PlayRoundClear() { if (roundClearSound != null) sfxSource.PlayOneShot(roundClearSound); }
    public void PlayFall()       { if (fallSound != null) sfxSource.PlayOneShot(fallSound); }
    public void PlayRespawn()    { if (respawnSound != null) sfxSource.PlayOneShot(respawnSound); }
    public void PlayMove()       { if (moveSound != null) sfxSource.PlayOneShot(moveSound); }

    // Plays level music. Restores volume if it was faded out.
    public void PlayGameMusic()
    {
        if (gameMusic == null) return;
        if (musicSource.clip == gameMusic && musicSource.isPlaying && musicSource.volume > 0.01f)
            return;

        KillFade();
        PendingMusicFadeIn = false;
        musicSource.clip = gameMusic;
        musicSource.volume = _targetMusicVolume;
        musicSource.Play();
    }

    // Plays boss music. Restores volume if it was faded out.
    public void PlayBossMusic()
    {
        if (bossFightMusic == null) return;
        if (musicSource.clip == bossFightMusic && musicSource.isPlaying && musicSource.volume > 0.01f)
            return;

        KillFade();
        PendingMusicFadeIn = false;
        musicSource.clip = bossFightMusic;
        musicSource.volume = _targetMusicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        KillFade();
        musicSource.Stop();
    }

    public void FadeMusicOut(float duration)
    {
        KillFade();
        _fadeCoroutine = StartCoroutine(FadeMusicCoroutine(musicSource.volume, 0f, duration));
    }

    public void FadeMusicIn(float duration)
    {
        KillFade();
        if (!musicSource.isPlaying && musicSource.clip != null)
            musicSource.Play();
        _fadeCoroutine = StartCoroutine(FadeMusicCoroutine(musicSource.volume, _targetMusicVolume, duration));
    }

    public void SetVolumes(float master, float music, float sfx)
    {
        _targetMusicVolume = music * master;

        // Only apply immediately if no fade is in progress
        if (_fadeCoroutine == null && musicSource != null)
            musicSource.volume = _targetMusicVolume;

        if (sfxSource != null)
            sfxSource.volume = sfx * master;
    }

    private void KillFade()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
    }

    private IEnumerator FadeMusicCoroutine(float from, float to, float duration)
    {
        if (musicSource == null) yield break;

        float elapsed = 0f;
        musicSource.volume = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            musicSource.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }

        musicSource.volume = to;
        _fadeCoroutine = null;

        // Faded to silence — stop source so isPlaying reflects reality
        if (to <= 0.001f)
            musicSource.Stop();
    }
}

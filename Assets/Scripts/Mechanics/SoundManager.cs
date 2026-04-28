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

    /// <summary>Target music volume (master * music setting).</summary>
    private float _targetMusicVolume = 0.5f;

    /// <summary>
    /// When true, the next SoundManager instance starts music at zero volume
    /// and fades in. Set by ScreenTransitionManager before loading a scene.
    /// </summary>
    public static bool PendingMusicFadeIn { get; set; }

    /// <summary>Duration for the pending fade-in (matches shimmer sweep).</summary>
    public static float PendingFadeInDuration { get; set; } = 8f;

    void Awake()
    {
        Instance = this;

        sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.5f;

        // Apply saved volume settings
        GameSettings.ApplyAudio();
        GameSettings.OnAudioChanged += OnAudioSettingsChanged;
    }

    void OnDestroy()
    {
        GameSettings.OnAudioChanged -= OnAudioSettingsChanged;
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

            // Start music silently and fade in over the shimmer duration
            musicSource.volume = 0f;
            PlayGameMusic();
            StartCoroutine(FadeMusicCoroutine(0f, _targetMusicVolume, fadeDur));
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

    public void PlayGameMusic()
    {
        if (gameMusic == null) return;
        if (musicSource.clip == gameMusic && musicSource.isPlaying) return;
        musicSource.clip = gameMusic;
        musicSource.Play();
    }

    public void PlayBossMusic()
    {
        if (bossFightMusic == null) return;
        if (musicSource.clip == bossFightMusic && musicSource.isPlaying) return;
        musicSource.clip = bossFightMusic;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    /// <summary>Fades the music volume out over the given duration.</summary>
    public void FadeMusicOut(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(FadeMusicCoroutine(musicSource.volume, 0f, duration));
    }

    /// <summary>Fades the music volume in from zero to the target level.</summary>
    public void FadeMusicIn(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(FadeMusicCoroutine(0f, _targetMusicVolume, duration));
    }

    /// <summary>Applies volume levels from GameSettings.</summary>
    public void SetVolumes(float master, float music, float sfx)
    {
        _targetMusicVolume = music * master;
        if (musicSource != null)
            musicSource.volume = _targetMusicVolume;
        if (sfxSource != null)
            sfxSource.volume = sfx * master;
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
    }
}

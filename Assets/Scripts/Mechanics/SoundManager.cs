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

    void Awake()
    {
        Instance = this;

        sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.5f;
    }

    void Start()
    {
        PlayGameMusic();
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
}
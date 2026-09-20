using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [Header("音频源")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    
    [Header("音效")]
    public AudioClip shootSound;
    public AudioClip explosionSound;
    public AudioClip pickupSound;
    public AudioClip hitSound;
    public AudioClip gameOverSound;
    public AudioClip victorySound;
    public AudioClip buttonClickSound;
    
    [Header("音乐")]
    public AudioClip backgroundMusic;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 播放背景音乐
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }
    
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
    
    public void PlayShoot()
    {
        PlaySFX(shootSound);
    }
    
    public void PlayExplosion()
    {
        PlaySFX(explosionSound);
    }
    
    public void PlayPickup()
    {
        PlaySFX(pickupSound);
    }
    
    public void PlayHit()
    {
        PlaySFX(hitSound);
    }
    
    public void PlayGameOver()
    {
        PlaySFX(gameOverSound);
    }
    
    public void PlayVictory()
    {
        PlaySFX(victorySound);
    }
    
    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSound);
    }
    
    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = volume;
        }
    }
    
    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = volume;
        }
    }
}

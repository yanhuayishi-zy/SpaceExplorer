using UnityEngine;

/// 自动加载并播放 BGM / 射击 / 坠机 / 道具音效
public class AutoAudio : MonoBehaviour
{
    public static AutoAudio Instance { get; private set; }

    AudioSource bgmSource;
    AudioSource sfxSource;
    AudioClip shootClip;
    AudioClip specialClip;
    AudioClip hitClip;
    AudioClip crashClip;
    AudioClip powerClip;
    float lastShootTime = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindObjectOfType<AutoAudio>() != null) return;
        var go = new GameObject("AutoAudio");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<AutoAudio>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        bgmSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        sfxSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        sfxSource.spatialBlend = 0f;

        var bgm = Resources.Load<AudioClip>("BGM");
        shootClip = Resources.Load<AudioClip>("SFX_Shoot");
        specialClip = Resources.Load<AudioClip>("SFX_Special");
        hitClip = Resources.Load<AudioClip>("SFX_Hit");
        crashClip = Resources.Load<AudioClip>("SFX_Crash");
        powerClip = Resources.Load<AudioClip>("SFX_PowerUp");

        if (bgm != null)
        {
            bgmSource.clip = bgm;
            bgmSource.volume = 0.35f;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning("BGM 未找到");
        }
    }

    public static void PlayShoot()
    {
        if (Instance == null || Instance.shootClip == null) return;
        // 限制过密
        if (Time.unscaledTime - Instance.lastShootTime < 0.04f) return;
        Instance.lastShootTime = Time.unscaledTime;
        Instance.sfxSource.PlayOneShot(Instance.shootClip, 0.35f);
    }

    public static void PlaySpecial()
    {
        if (Instance == null || Instance.specialClip == null) return;
        Instance.sfxSource.PlayOneShot(Instance.specialClip, 0.55f);
    }

    public static void PlayHit()
    {
        if (Instance == null || Instance.hitClip == null) return;
        Instance.sfxSource.PlayOneShot(Instance.hitClip, 0.4f);
    }

    public static void PlayCrash()
    {
        if (Instance == null || Instance.crashClip == null) return;
        Instance.sfxSource.PlayOneShot(Instance.crashClip, 0.85f);
        // 结束后稍微压低 BGM
        Instance.bgmSource.volume = 0.12f;
    }

    public static void PlayPowerUp()
    {
        if (Instance == null || Instance.powerClip == null) return;
        Instance.sfxSource.PlayOneShot(Instance.powerClip, 0.6f);
    }

    public static void ResetBgmVolume()
    {
        if (Instance != null) Instance.bgmSource.volume = 0.35f;
    }

    static readonly System.Collections.Generic.Dictionary<int, AudioClip> levelClips =
        new System.Collections.Generic.Dictionary<int, AudioClip>();
    static int currentLevelBgm = -1;

    public static void PlayLevelBgm(int level)
    {
        if (Instance == null || Instance.bgmSource == null) return;
        if (level < 1 || level > 13) return;
        if (level == currentLevelBgm && Instance.bgmSource.isPlaying) return;

        if (!levelClips.TryGetValue(level, out var clip) || clip == null)
        {
            clip = Resources.Load<AudioClip>("BGM_L" + level.ToString("00"));
            if (clip == null) clip = Resources.Load<AudioClip>("BGM");
            if (clip != null) levelClips[level] = clip;
        }
        if (clip == null) return;

        currentLevelBgm = level;
        Instance.bgmSource.Stop();
        Instance.bgmSource.clip = clip;
        Instance.bgmSource.volume = 0.35f;
        Instance.bgmSource.pitch = level >= 13 ? 0.92f : 1f;
        Instance.bgmSource.Play();
    }

    public static void PlayDefaultBgm()
    {
        if (Instance == null) return;
        currentLevelBgm = -1;
        var bgm = Resources.Load<AudioClip>("BGM");
        if (bgm == null) return;
        Instance.bgmSource.Stop();
        Instance.bgmSource.clip = bgm;
        Instance.bgmSource.pitch = 1f;
        Instance.bgmSource.volume = 0.35f;
        Instance.bgmSource.Play();
    }
}

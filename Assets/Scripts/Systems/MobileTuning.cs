using UnityEngine;
using UnityEngine.UI;

/// 移动端统一参数。所有方法在非移动平台都返回传入的桌面端原值。
public static class MobileTuning
{
    public static bool Active => MobileControls.IsMobile;
    public static bool Portrait => Screen.height >= Screen.width;

    public static void CameraBounds(out float halfW, out float halfH)
    {
        var cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            halfH = cam.orthographicSize;
            halfW = halfH * cam.aspect;
            return;
        }
        halfW = Portrait ? 2.8f : 5f;
        halfH = 5f;
    }

    static float SpriteWidth(Sprite sprite, float fallback)
    {
        return sprite != null ? Mathf.Max(0.05f, sprite.bounds.size.x) : fallback;
    }

    public static float ScaleForWidth(Sprite sprite, float worldWidth, float fallbackWidth)
    {
        return worldWidth / SpriteWidth(sprite, fallbackWidth);
    }

    public static float PlayerScale(Sprite sprite, float desktopScale)
    {
        if (!Active) return desktopScale;
        CameraBounds(out float hw, out _);
        float width = Mathf.Clamp(hw * 2f * 0.19f, 1.0f, 1.28f);
        return ScaleForWidth(sprite, width, 3.84f);
    }

    public static float MinionScale(Sprite sprite, float desktopScale, bool elite)
    {
        if (!Active) return desktopScale;
        CameraBounds(out float hw, out _);
        float fraction = elite ? 0.22f : 0.17f;
        float width = Mathf.Clamp(hw * 2f * fraction, elite ? 1.05f : 0.78f, elite ? 1.35f : 1.05f);
        return ScaleForWidth(sprite, width, 2.56f);
    }

    public static float BossScale(Sprite sprite, float desktopScale)
    {
        if (!Active) return desktopScale;
        CameraBounds(out float hw, out _);
        float width = Mathf.Clamp(hw * 2f * 0.4f, 2.15f, 3.1f);
        return ScaleForWidth(sprite, width, 5.12f);
    }

    public static float PowerUpScale(Sprite sprite, float desktopScale, bool premium = false)
    {
        if (!Active) return desktopScale;
        CameraBounds(out float hw, out _);
        float width = Mathf.Clamp(hw * 2f * (premium ? 0.16f : 0.14f), premium ? 0.88f : 0.76f, 1.05f);
        return ScaleForWidth(sprite, width, 2.56f);
    }

    public static float EnemyBulletScale(Sprite sprite, float desktopScale)
    {
        if (!Active) return desktopScale;
        float width = Mathf.Clamp(0.24f + (desktopScale - 0.3f) * 0.45f, 0.22f, 0.36f);
        return ScaleForWidth(sprite, width, 0.64f);
    }

    public static float PlayerBulletScale(Sprite sprite, float desktopScale)
    {
        if (!Active) return desktopScale;
        float width = Mathf.Clamp(0.17f + (desktopScale - 0.25f) * 0.32f, 0.15f, 0.3f);
        return ScaleForWidth(sprite, width, 0.64f);
    }

    public static float MoveSpeed(float desktopSpeed)
    {
        return Active ? desktopSpeed * 0.8f : desktopSpeed;
    }

    public static Vector2 ProjectileVelocity(Vector2 desktopVelocity)
    {
        return Active ? desktopVelocity * 0.82f : desktopVelocity;
    }

    public static int BossContactDamage(int desktopDamage, int playerMaxHealth)
    {
        if (!Active) return desktopDamage;
        int mobileCap = Mathf.Max(1, Mathf.RoundToInt(playerMaxHealth * 0.18f));
        return Mathf.Min(desktopDamage, mobileCap);
    }

    public static Vector3 EnemySpawnPoint(Vector3 desktopPoint, float padding = 0.55f)
    {
        if (!Active) return desktopPoint;
        CameraBounds(out float hw, out float hh);
        int mode = Random.Range(0, 5);
        float x;
        float y;
        if (mode == 0)
        {
            x = -hw + padding;
            y = Random.Range(hh * 0.18f, hh * 0.72f);
        }
        else if (mode == 1)
        {
            x = hw - padding;
            y = Random.Range(hh * 0.18f, hh * 0.72f);
        }
        else
        {
            x = Random.Range(-hw + padding, hw - padding);
            y = Random.Range(hh * 0.58f, hh - padding);
        }
        return new Vector3(x, y, 0f);
    }

    public static Vector3 BossSpawnPoint(Vector3 desktopPoint)
    {
        if (!Active) return desktopPoint;
        CameraBounds(out _, out float hh);
        return new Vector3(0f, hh - 1.15f, 0f);
    }

    public static Vector3 PowerUpPoint(Vector3 desktopPoint, float padding = 0.55f)
    {
        if (!Active) return desktopPoint;
        CameraBounds(out float hw, out float hh);
        return new Vector3(
            Mathf.Clamp(desktopPoint.x, -hw + padding, hw - padding),
            Mathf.Clamp(desktopPoint.y, -hh + padding, hh - padding),
            0f);
    }

    public static Vector3 RandomPowerUpPoint(Vector3 desktopPoint)
    {
        if (!Active) return desktopPoint;
        CameraBounds(out float hw, out float hh);
        return new Vector3(Random.Range(-hw + 0.55f, hw - 0.55f), hh - 0.65f, 0f);
    }

    public static float ClampX(float x, float padding = 0.45f)
    {
        if (!Active) return x;
        CameraBounds(out float hw, out _);
        return Mathf.Clamp(x, -hw + padding, hw - padding);
    }

    public static float Top(float padding = 0.25f)
    {
        CameraBounds(out _, out float hh);
        return hh - padding;
    }

    public static float Bottom(float padding = 0.25f)
    {
        CameraBounds(out _, out float hh);
        return -hh + padding;
    }

    public static void ConfigureCanvas(CanvasScaler scaler)
    {
        if (scaler == null || !Active) return;
        scaler.referenceResolution = Portrait ? new Vector2(1080f, 1920f) : new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = Portrait ? 0f : 1f;
    }
}

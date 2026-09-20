using UnityEngine;

/// 头顶血条：挂在敌人上自动创建
public class EnemyHealthBar : MonoBehaviour
{
    public int maxHealth = 1;
    public int currentHealth;
    public Vector2 offset = new Vector2(0f, 0.7f);
    public Vector2 size = new Vector2(0.9f, 0.12f);

    Transform fill;
    SpriteRenderer fillSr;
    SpriteRenderer bgSr;
    static Sprite whiteSprite;

    public void Setup(int hp)
    {
        maxHealth = Mathf.Max(1, hp);
        currentHealth = maxHealth;
        Build();
        Refresh();
    }

    void Build()
    {
        if (whiteSprite == null)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        var bgGo = new GameObject("HP_BG");
        bgGo.transform.SetParent(transform, false);
        bgGo.transform.localPosition = (Vector3)offset;
        bgGo.transform.localScale = new Vector3(size.x, size.y, 1f);
        bgSr = bgGo.AddComponent<SpriteRenderer>();
        bgSr.sprite = whiteSprite;
        bgSr.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        bgSr.sortingOrder = 20;

        var fillGo = new GameObject("HP_Fill");
        fillGo.transform.SetParent(bgGo.transform, false);
        fillGo.transform.localPosition = new Vector3(-0.5f, 0f, 0f); // 左对齐
        fillGo.transform.localScale = new Vector3(1f, 0.75f, 1f);
        fillSr = fillGo.AddComponent<SpriteRenderer>();
        fillSr.sprite = whiteSprite;
        fillSr.color = new Color(0.2f, 0.95f, 0.35f, 1f);
        fillSr.sortingOrder = 21;
        fill = fillGo.transform;
    }

    void LateUpdate()
    {
        // 始终朝上，不跟着旋转
        transform.rotation = Quaternion.identity;
    }

    public void ApplyDamage(int dmg)
    {
        currentHealth = Mathf.Max(0, currentHealth - dmg);
        Refresh();
    }

    void Refresh()
    {
        if (fill == null) return;
        float ratio = maxHealth <= 0 ? 0f : (float)currentHealth / maxHealth;
        // fill parent local: bg is unit-ish after scale; child localScale.x from 0..1
        // bg scale is size; child sits at left edge (-0.5) so grow to the right
        float sx = Mathf.Clamp01(ratio);
        fill.localScale = new Vector3(sx, 0.75f, 1f);
        fill.localPosition = new Vector3(-0.5f + sx * 0.5f, 0f, 0f);

        if (fillSr != null)
        {
            fillSr.color = ratio > 0.5f
                ? new Color(0.2f, 0.95f, 0.35f, 1f)
                : (ratio > 0.25f
                    ? new Color(0.95f, 0.85f, 0.2f, 1f)
                    : new Color(0.95f, 0.25f, 0.2f, 1f));
        }
    }
}

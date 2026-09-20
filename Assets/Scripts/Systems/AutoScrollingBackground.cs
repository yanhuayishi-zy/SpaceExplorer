using UnityEngine;

/// 自动创建纵向无缝滚动星空背景
public class AutoScrollingBackground : MonoBehaviour
{
    public float scrollSpeed = 1.2f;

    Transform a;
    Transform b;
    float spriteHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindObjectOfType<AutoScrollingBackground>() != null) return;
        // 若场景里已有 SpaceBackground 且挂了本脚本则跳过
        var go = new GameObject("AutoScrollingBackground");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<AutoScrollingBackground>();
    }

    void Start()
    {
        var sprite = Resources.Load<Sprite>("SpaceBG");
        if (sprite == null)
        {
            Debug.LogWarning("未找到 Resources/SpaceBG，滚动背景未启用");
            Destroy(this);
            return;
        }

        // 用正交相机视野估算高度，保证铺满
        var cam = Camera.main;
        float camHeight = 10f;
        if (cam != null && cam.orthographic)
        {
            camHeight = cam.orthographicSize * 2f;
        }
        float camWidth = camHeight * (cam != null ? cam.aspect : 16f / 9f);

        // 精灵世界高度：2048px 图，Pixels Per Unit 通常 100
        float ppu = sprite.pixelsPerUnit;
        if (ppu <= 0f) ppu = 100f;
        float nativeH = sprite.rect.height / ppu;
        float nativeW = sprite.rect.width / ppu;

        // 放大到至少覆盖宽高
        float scaleX = Mathf.Max(1f, camWidth / nativeW) * 1.05f;
        float scaleY = Mathf.Max(1f, (camHeight * 2f) / nativeH);
        float scale = Mathf.Max(scaleX, scaleY);

        spriteHeight = nativeH * scale;
        float worldW = nativeW * scale;

        a = CreateQuad("BG_A", sprite, scale, 0f, -100);
        b = CreateQuad("BG_B", sprite, scale, spriteHeight, -100);

        // 隐藏旧的糊背景（若在相机前）
        HideOldBackgrounds();
    }

    Transform CreateQuad(string name, Sprite sprite, float scale, float y, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, y, 10f); // 稍远一点
        go.transform.localScale = new Vector3(scale, scale, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Simple;
        sr.sortingOrder = order;
        sr.color = Color.white;
        return go.transform;
    }

    void HideOldBackgrounds()
    {
        var olds = Object.FindObjectsOfType<SpriteRenderer>();
        foreach (var sr in olds)
        {
            if (sr == null) continue;
            var n = sr.gameObject.name.ToLowerInvariant();
            if (n.Contains("spacebackground") || n.Contains("background") || n.Contains("bg"))
            {
                if (sr.transform.IsChildOf(transform)) continue;
                sr.enabled = false;
            }
        }
    }

    void Update()
    {
        if (a == null || b == null) return;

        float dy = scrollSpeed * Time.deltaTime;
        a.localPosition += new Vector3(0f, -dy, 0f);
        b.localPosition += new Vector3(0f, -dy, 0f);

        // 无缝回绕
        if (a.localPosition.y <= -spriteHeight)
        {
            a.localPosition = new Vector3(a.localPosition.x, b.localPosition.y + spriteHeight, a.localPosition.z);
        }
        if (b.localPosition.y <= -spriteHeight)
        {
            b.localPosition = new Vector3(b.localPosition.x, a.localPosition.y + spriteHeight, b.localPosition.z);
        }
    }
}

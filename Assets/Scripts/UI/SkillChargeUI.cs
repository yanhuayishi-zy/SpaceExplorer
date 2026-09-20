using UnityEngine;

/// 旧底部技能条已废弃；现由 MobileControls 的圆形技能钮（右下）负责
public class SkillChargeUI : MonoBehaviour
{
    static SkillChargeUI instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindObjectOfType<SkillChargeUI>() != null) return;
        var go = new GameObject("SkillChargeUI");
        Object.DontDestroyOnLoad(go);
        instance = go.AddComponent<SkillChargeUI>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // 兼容旧场景残留：见到底部条就删
        foreach (var go in Object.FindObjectsOfType<Canvas>())
        {
            if (go != null && go.gameObject.name == "SkillHudCanvas")
                Destroy(go.gameObject);
        }
    }
}

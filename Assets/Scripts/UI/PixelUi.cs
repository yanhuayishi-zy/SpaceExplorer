using UnityEngine;

public static class PixelUi
{
    static Font cached;

    public static Font Font
    {
        get
        {
            if (cached != null) return cached;
            cached = Resources.Load<Font>("PixelFont");
            if (cached == null)
            {
                cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (cached == null)
                {
                    cached = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }
            return cached;
        }
    }
}

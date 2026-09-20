using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 10f;
    public float tiltAmount = 2f;

    [Header("边界设置")]
    public float minX = -8f;
    public float maxX = 8f;
    public float minY = -4f;
    public float maxY = 4f;

    [Header("触控滑动")]
    public float slideLerp = 18f;
    /// 手指落点与机位的偏移：拖动时不跳到手指上
    Vector2 slideOffset;
    bool dragging;
    int activeFingerId = -1;

    Vector2 moveInput;
    Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (MobileTuning.Active) slideLerp = Mathf.Min(slideLerp, 9.5f);
        FitBoundsToCamera();
    }

    void FitBoundsToCamera()
    {
        var cam = Camera.main;
        if (cam == null || !cam.orthographic) return;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        if (MobileTuning.Active)
        {
            var sr = GetComponent<SpriteRenderer>();
            float padX = sr != null ? Mathf.Max(0.45f, sr.bounds.extents.x * 0.75f) : 0.5f;
            float padY = sr != null ? Mathf.Max(0.55f, sr.bounds.extents.y * 0.75f) : 0.6f;
            minX = -halfW + padX;
            maxX = halfW - padX;
            minY = -halfH + Mathf.Max(0.75f, padY);
            maxY = halfH - Mathf.Max(0.8f, padY);
        }
        else
        {
            minX = -halfW + 0.45f;
            maxX = halfW - 0.45f;
            minY = -halfH + 0.45f;
            maxY = halfH - 0.45f;
        }
    }

    void Update()
    {
        var cam = Camera.main;
        bool usedSlide = false;

        // 触屏 / 编辑器模拟：始终用「滑动跟手」
        if (TrySlideInput(cam))
        {
            usedSlide = true;
        }
        else if (!MobileControls.IsMobile)
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Vertical");
            moveInput = new Vector2(moveX, moveY);
        }
        else
        {
            moveInput = Vector2.zero;
        }

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        transform.position = p;

        if (usedSlide)
        {
            // 滑动时轻微随速度倾斜
            float vx = moveInput.x;
            transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.Euler(0f, 0f, -vx * tiltAmount * 0.6f), Time.deltaTime * 6f);
        }
        else if (Mathf.Abs(moveInput.x) > 0.05f)
        {
            float tilt = -moveInput.x * tiltAmount;
            transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.Euler(0f, 0f, tilt), Time.deltaTime * 5f);
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.identity, Time.deltaTime * 5f);
        }
    }

    Vector3 ScreenToWorld(Vector3 screenPos, Camera cam)
    {
        if (cam == null) return transform.position;
        screenPos.z = -cam.transform.position.z;
        var w = cam.ScreenToWorldPoint(screenPos);
        w.z = 0f;
        return w;
    }

    /// 滑动跟手：按下时记住相对偏移，拖动时偏移不变（不跳机）
    bool TrySlideInput(Camera cam)
    {
        moveInput = Vector2.zero;
        if (cam == null) return false;

        // 触屏
        if (Input.touchSupported && Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    if (MobileControls.IsPointerOverUi(t.fingerId)) continue;
                    activeFingerId = t.fingerId;
                    Vector3 wp = ScreenToWorld(t.position, cam);
                    slideOffset = (Vector2)(transform.position - wp);
                    dragging = true;
                }
                if (t.fingerId != activeFingerId) continue;

                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    activeFingerId = -1;
                    dragging = false;
                    continue;
                }

                if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                {
                    Vector3 wp = ScreenToWorld(t.position, cam);
                    Vector3 target = wp + (Vector3)slideOffset;
                    Vector3 prev = transform.position;
                    float follow = MobileTuning.Active
                        ? 1f - Mathf.Exp(-slideLerp * Time.deltaTime)
                        : Time.deltaTime * slideLerp;
                    transform.position = Vector3.Lerp(prev, target, follow);
                    Vector3 d = (transform.position - prev) / Mathf.Max(Time.deltaTime, 0.0001f);
                    moveInput = Vector2.ClampMagnitude(new Vector2(d.x, d.y) / Mathf.Max(moveSpeed, 1f), 1f);
                    return true;
                }
            }
        }

        // 编辑器 / 桌面：按住左键滑动（避开 UI）
        if (Input.GetMouseButtonDown(0) && !MobileControls.IsPointerOverUi())
        {
            Vector3 wp = ScreenToWorld(Input.mousePosition, cam);
            slideOffset = (Vector2)(transform.position - wp);
            dragging = true;
        }
        if (Input.GetMouseButton(0) && dragging && !MobileControls.IsPointerOverUi())
        {
            Vector3 wp = ScreenToWorld(Input.mousePosition, cam);
            Vector3 target = wp + (Vector3)slideOffset;
            Vector3 prev = transform.position;
            float follow = MobileTuning.Active
                ? 1f - Mathf.Exp(-slideLerp * Time.deltaTime)
                : Time.deltaTime * slideLerp;
            transform.position = Vector3.Lerp(prev, target, follow);
            Vector3 d = (transform.position - prev) / Mathf.Max(Time.deltaTime, 0.0001f);
            moveInput = Vector2.ClampMagnitude(new Vector2(d.x, d.y) / Mathf.Max(moveSpeed, 1f), 1f);
            return true;
        }
        if (Input.GetMouseButtonUp(0)) dragging = false;

        return false;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // 滑动拖拽时：直接改 transform，清速度避免物理抢控制
        if (dragging)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // 键盘 / 手柄：用速度驱动（否则战机不会动）
        if (!MobileControls.IsMobile || !Input.touchSupported)
        {
            rb.velocity = moveInput * moveSpeed;
            return;
        }

        rb.velocity = Vector2.zero;
    }
}

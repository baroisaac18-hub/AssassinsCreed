using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// MobileTouchControls — يولّد تلقائياً واجهة تحكم اللمس عند تشغيل اللعبة على Android/iOS:
///   - جويستيك ديناميكي (يظهر مكان اللمس) لحركة اللاعب
///   - منطقة سحب (يمين الشاشة) لتدوير الكاميرا
///   - أزرار: إطلاق/هجوم، تصويب، قفز، التقاط، قائمة السلاح، تبديل الأسلحة (1-4)، جرعات (5/6)
///   - زر إيقاف مؤقت مع قائمة (متابعة / خروج)
/// على الكمبيوتر لا يُنشئ شيئاً (الإدخال الأصلي يعمل).
/// يُنشئ نفسه تلقائياً عند التشغيل على الموبايل — بدون أي ربط يدوي في المشهد.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class MobileTouchControls : MonoBehaviour
{
    [Header("تفعيل (على الموبايل تلقائي؛ في المحرر فعّل لاختبار اللمس)")]
    public bool forceMobileInEditor;

    [Header("الجويستيك")]
    public float joystickRadius = 120f;
    public float joystickKnobRadius = 52f;

    [Header("منطقة الكاميرا")]
    [Range(0f, 1f)] public float lookAreaRightFraction = 0.55f;

    [Header("تجربة (Haptics / FPS / حساسية)")]
    public bool enableHaptics = true;
    [Range(30, 120)] public int targetFrameRate = 60;
    [Range(0.1f, 3f)] public float cameraSensitivityScale = 1f;

    [Header("جويستيك ديناميكي (يظهر مكان اللمس)")]
    public bool dynamicJoystick = true;

    [Header("قائمة الإيقاف المؤقت")]
    public bool enablePauseMenu = true;

    bool _active;
    Canvas _canvas;
    JoystickArea _joystick;
    LookArea _look;
    static MobileTouchControls _instance;
    GameObject _pausePanel;
    bool _paused;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        _active = Application.isMobilePlatform || forceMobileInEditor;
        if (!_active) return;

        Application.targetFrameRate = targetFrameRate;
        TouchButton.HapticsEnabled = enableHaptics;

        // أضف مدير الحفظ التلقائي (المخزون + الموقع) على الموبايل
        if (GetComponent<AutoSaveManager>() == null)
        {
            gameObject.AddComponent<AutoSaveManager>();
        }

        EnsureEventSystem();
        CreateCanvas();

        // 1) الجويستيك — ديناميكي: يظهر مكان اللمس (النصف السفلي الأيسر)
        _joystick = JoystickArea.Create(_canvas.transform, joystickRadius, joystickKnobRadius,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(150f, 150f), dynamicJoystick);

        // 2) منطقة تدوير الكاميرا — يمين الشاشة
        _look = LookArea.Create(_canvas.transform, new Vector2(lookAreaRightFraction, 0f), new Vector2(1f, 1f));
        _look.sensitivity = cameraSensitivityScale;

        // 3) زر الإطلاق/الهجوم — أسفل اليمين (كبير)
        TouchButton.Create(_canvas.transform, "هجوم", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-150f, 170f), 140f,
            onDown: () => { MobileInputManager.Fire1Held = true; MobileInputManager.PressFire1(); },
            onUp: () => MobileInputManager.Fire1Held = false);

        // 4) زر القفز — فوق زر الهجوم جهة اليسار
        TouchButton.Create(_canvas.transform, "قفز", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-300f, 300f), 100f,
            onDown: () => { MobileInputManager.JumpHeld = true; MobileInputManager.PressJump(); },
            onUp: () => MobileInputManager.JumpHeld = false);

        // 5) زر التصويب — بجانب القفز
        TouchButton.Create(_canvas.transform, "تصويب", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-150f, 350f), 100f,
            onDown: () => { MobileInputManager.Fire2Held = true; MobileInputManager.PressFire2(); },
            onUp: () => MobileInputManager.Fire2Held = false);

        // 6) زر القائمة (Tab) — أعلى اليسار
        TouchButton.Create(_canvas.transform, "قائمة", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(130f, -120f), 90f,
            onDown: () => MobileInputManager.PressMenu());

        // 6b) زر الإيقاف المؤقت — أعلى اليسار (بجانب القائمة)
        if (enablePauseMenu)
        {
            TouchButton.Create(_canvas.transform, "إيقاف", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(250f, -120f), 90f,
                onDown: () => TogglePause());
        }

        // 7) زر الالتقاط (F) — أعلى اليمين
        TouchButton.Create(_canvas.transform, "التقاط", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-130f, -110f), 95f,
            onDown: () => MobileInputManager.PressPickup());

        // 8) أزرار الأسلحة 1-4 — الحافة اليمنى
        float[] wY = { 140f, 60f, -20f, -100f };
        for (int i = 0; i < 4; i++)
        {
            int slot = i + 1;
            TouchButton.Create(_canvas.transform, "سلاح" + slot, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-70f, wY[i]), 78f,
                onDown: () => MobileInputManager.PressWeapon(slot));
        }

        // 9) جرعة الصحة (5) والطاقة (6) — يمين أعلى
        TouchButton.Create(_canvas.transform, "صحة", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-130f, -230f), 78f,
            onDown: () => MobileInputManager.PressHealth());
        TouchButton.Create(_canvas.transform, "طاقة", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-230f, -230f), 78f,
            onDown: () => MobileInputManager.PressEnergy());
    }

    void OnDestroy()
    {
        // تنظيف الحالة اللمسية عند إيقاف التشغيل
        Time.timeScale = 1f;
        MobileInputManager.MoveAxis = Vector2.zero;
        MobileInputManager.LookDelta = Vector2.zero;
        MobileInputManager.JumpHeld = MobileInputManager.Fire1Held = MobileInputManager.Fire2Held = false;
    }

    void TogglePause()
    {
        _paused = !_paused;
        Time.timeScale = _paused ? 0f : 1f;
        if (_paused) ShowPausePanel();
        else HidePausePanel();
    }

    void ShowPausePanel()
    {
        if (_pausePanel != null) { _pausePanel.SetActive(true); return; }

        var go = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(_canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        _pausePanel = go;

        // العنوان
        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleGo.transform.SetParent(go.transform, false);
        var trt = (RectTransform)titleGo.transform;
        trt.anchorMin = new Vector2(0.5f, 0.72f);
        trt.anchorMax = new Vector2(0.5f, 0.72f);
        trt.sizeDelta = new Vector2(700f, 120f);
        var title = titleGo.GetComponent<Text>();
        title.text = "إيقاف مؤقت";
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 64;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;

        // متابعة
        TouchButton.Create(go.transform, "متابعة", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), 150f,
            onDown: () => TogglePause());

        // خروج من اللعبة
        TouchButton.Create(go.transform, "خروج", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), 130f,
            onDown: () => QuitGame());
    }

    void HidePausePanel()
    {
        if (_pausePanel != null) _pausePanel.SetActive(false);
    }

    void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_ANDROID
        using (var act = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
        {
            act.Call("finish");
        }
#else
        Application.Quit();
#endif
    }

    void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }

    void CreateCanvas()
    {
        var go = new GameObject("MobileTouchCanvas");
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        // تكييف مع منطقة الأمان (نوتش / شاشات منحنية)
        var safe = Screen.safeArea;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // إنشاء تلقائي على الموبايل — بدون أي ربط يدوي في المشهد
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Application.isMobilePlatform && _instance == null)
        {
            var go = new GameObject("MobileTouchControls");
            go.AddComponent<MobileTouchControls>();
        }
    }
}

// =====================================================================
//  منطقة الجويستيك: تلتقط اللمس على القرص وتكتب MobileInputManager.MoveAxis
//  الوضع الديناميكي: يظهر الجويستيك مكان اللمس (في النصف السفلي الأيسر)
// =====================================================================
public class JoystickArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public bool dynamicMode = true;

    RectTransform _baseRect;
    RectTransform _knobRect;
    float _radius;
    Vector2 _homePos;

    public static JoystickArea Create(Transform parent, float radius, float knobRadius, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, bool dynamicMode = true)
    {
        var go = new GameObject("Joystick", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(radius * 2f, radius * 2f);
        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.18f);
        img.raycastTarget = true;

        var knob = new GameObject("Knob", typeof(RectTransform), typeof(Image));
        knob.transform.SetParent(rt, false);
        var krt = (RectTransform)knob.transform;
        krt.anchorMin = krt.anchorMax = new Vector2(0.5f, 0.5f);
        krt.sizeDelta = new Vector2(knobRadius * 2f, knobRadius * 2f);
        var kimg = knob.GetComponent<Image>();
        kimg.color = new Color(1f, 1f, 1f, 0.45f);
        kimg.raycastTarget = false;

        var area = go.AddComponent<JoystickArea>();
        area._baseRect = rt;
        area._knobRect = krt;
        area._radius = radius - knobRadius;
        area._homePos = anchoredPos;
        area.dynamicMode = dynamicMode;
        return area;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (dynamicMode) MoveBaseToPointer(e);
        MoveKnob(e);
    }

    public void OnDrag(PointerEventData e)
    {
        MoveKnob(e);
    }

    public void OnPointerUp(PointerEventData e)
    {
        MobileInputManager.MoveAxis = Vector2.zero;
        _knobRect.anchoredPosition = Vector2.zero;
        if (dynamicMode) _baseRect.anchoredPosition = _homePos;
    }

    void MoveBaseToPointer(PointerEventData e)
    {
        var parentRt = (RectTransform)_baseRect.parent;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, e.position, null, out local);
        // أبقِ الجويستيك في النصف السفلي الأيسر من الشاشة
        local.x = Mathf.Clamp(local.x, -parentRt.rect.width * 0.45f, parentRt.rect.width * 0.45f);
        local.y = Mathf.Clamp(local.y, -parentRt.rect.height * 0.35f, parentRt.rect.height * 0.35f);
        _baseRect.anchoredPosition = local;
    }

    void MoveKnob(PointerEventData e)
    {
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_baseRect, e.position, null, out local);
        Vector2 dir = Vector2.ClampMagnitude(local, _radius);
        _knobRect.anchoredPosition = dir;
        MobileInputManager.MoveAxis = dir / _radius;
    }
}

// =====================================================================
//  منطقة السحب لتدوير الكاميرا (يمين الشاشة)
// =====================================================================
public class LookArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public float sensitivity = 1f;

    public static LookArea Create(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("LookArea", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f); // شفاف تماماً
        img.raycastTarget = true;
        return go.AddComponent<LookArea>();
    }

    public void OnPointerDown(PointerEventData e) { }
    public void OnDrag(PointerEventData e)
    {
        MobileInputManager.LookDelta += e.delta * sensitivity;
    }
    public void OnPointerUp(PointerEventData e) { }
}

// =====================================================================
//  زر لمسي عام (مع اهتزاز اختياري عند الضغط)
// =====================================================================
public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public static bool HapticsEnabled = true;

    System.Action _onDown;
    System.Action _onUp;

    public static TouchButton Create(Transform parent, string label, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, float diameter,
        System.Action onDown = null, System.Action onUp = null)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(diameter, diameter);
        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.30f);
        img.raycastTarget = true;

        var text = new GameObject("Text", typeof(RectTransform), typeof(Text));
        text.transform.SetParent(rt, false);
        var trt = (RectTransform)text.transform;
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(diameter, diameter);
        var t = text.GetComponent<Text>();
        t.text = label;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = (int)(diameter * 0.32f);
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;

        var btn = go.AddComponent<TouchButton>();
        btn._onDown = onDown;
        btn._onUp = onUp;
        return btn;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (HapticsEnabled) Handheld.Vibrate();
        _onDown?.Invoke();
    }
    public void OnPointerUp(PointerEventData e) { _onUp?.Invoke(); }
}

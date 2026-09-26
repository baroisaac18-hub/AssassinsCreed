using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// ============================================================
//  MobileMainMenu — شاشة بداية (Main Menu) للموبايل
//  在移动端自动生成全屏主菜单 UI：
//    - متابعة (Continue)    : 读档继续游戏（有存档时可用）
//    - لعبة جديدة (New Game): 删除旧档重新开始
//    - خروج (Exit)          : 退出游戏（Android 关闭 Activity）
//
//  自动创建条件：当前场景名命中 menuSceneNames 之一。
//  若你的主菜单场景名不同，改 menuSceneNames 或把本组件手动拖到主菜单场景任意对象上。
// ============================================================
public class MobileMainMenu : MonoBehaviour
{
    public string gameSceneName = "MainScene";

    // 主菜单场景名（自动创建时用于判断）
    public static readonly string[] menuSceneNames = new string[]
    {
        "MainMenu", "Menu", "Main", "Start", "MainMenuScene"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (!Application.isMobilePlatform) return;
        if (FindObjectOfType<MobileMainMenu>() != null) return;

        string scene = SceneManager.GetActiveScene().name;
        foreach (string name in menuSceneNames)
        {
            if (scene == name)
            {
                GameObject go = new GameObject("MobileMainMenu_UI");
                go.AddComponent<MobileMainMenu>();
                break;
            }
        }
    }

    private void Awake()
    {
        if (!Application.isMobilePlatform)
        {
            Destroy(gameObject);
            return;
        }
        BuildUI();
    }

    // ================= UI 构建 =================
    void BuildUI()
    {
        // ---- Canvas ----
        GameObject canvasGO = new GameObject("MobileMainMenu_Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        // 全屏背景
        Image bg = canvasGO.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.07f, 0.10f, 0.97f);

        // ---- EventSystem ----
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem",
                typeof(EventSystem), typeof(StandaloneInputModule));
            es.transform.SetParent(canvasGO.transform, false);
        }

        // ---- 标题 ----
        CreateText(canvasGO.transform, "العقّاب", new Vector2(0, 320), 96,
            TextAnchor.MiddleCenter, new Color(0.90f, 0.72f, 0.25f));
        CreateText(canvasGO.transform, "Assassin's Creed", new Vector2(0, 200), 38,
            TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.85f));

        // ---- 按钮 ----
        bool hasSave = SaveSystem.HasSave();

        CreateButton(canvasGO.transform, "متابعة", new Vector2(0, 20), hasSave,
            () => { SaveSystem.continueRequested = true; SceneManager.LoadScene(gameSceneName); });

        CreateButton(canvasGO.transform, "لعبة جديدة", new Vector2(0, -150), true,
            () => { SaveSystem.DeleteSave(); SaveSystem.continueRequested = false; SceneManager.LoadScene(gameSceneName); });

        CreateButton(canvasGO.transform, "خروج", new Vector2(0, -320), true, QuitGame);

        // 版本/提示
        CreateText(canvasGO.transform, "إصدار الموبايل — حفظ تلقائي مفعّل",
            new Vector2(0, -560), 28, TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.6f));
    }

    void QuitGame()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
            .GetStatic<AndroidJavaObject>("currentActivity");
        activity.Call("finish");
#else
        Application.Quit();
#endif
    }

    // ================= 组件工厂 =================
    Text CreateText(Transform parent, string content, Vector2 pos, int size,
        TextAnchor align, Color color)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(1000, 120);
        rt.anchoredPosition = pos;

        Text t = go.GetComponent<Text>();
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    void CreateButton(Transform parent, string label, Vector2 pos, bool interactable,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btn = new GameObject("Btn_" + label,
            typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)btn.transform;
        rt.sizeDelta = new Vector2(560, 110);
        rt.anchoredPosition = pos;

        Image img = btn.GetComponent<Image>();
        img.color = interactable
            ? new Color(0.82f, 0.62f, 0.18f, 1f)
            : new Color(0.45f, 0.45f, 0.45f, 0.65f);

        Button b = btn.GetComponent<Button>();
        b.targetGraphic = img;
        b.interactable = interactable;
        b.onClick.AddListener(onClick);

        Text t = CreateText(btn.transform, label, Vector2.zero, 46,
            TextAnchor.MiddleCenter, Color.white);
        RectTransform trt = (RectTransform)t.transform;
        trt.sizeDelta = new Vector2(560, 110);
    }
}

using UnityEngine;

/// <summary>
/// MobileInputManager — مركز الإدخال اللمسي (Android/iOS).
/// أزرار/جويستيك اللمس تكتب الحالة هنا، والسكريبتات الأصلية تقرأ عبر
/// GetAxis / GetButton / GetKeyDown / GetMouseButtonDown.
/// على الكمبيوتر (PC) كل الدوال ترجع تلقائياً إلى Unity Input الأصلي،
/// فالكود نفسه يعمل على المنصتين بدون أي تغيير.
/// </summary>
public static class MobileInputManager
{
    // =====================  حالة اللمس المستمرة (يكتبها MobileTouchControls) =====================
    public static Vector2 MoveAxis = Vector2.zero;   // الجويستيك (حركة اللاعب)
    public static Vector2 LookDelta = Vector2.zero;  // السحب لتدوير الكاميرا

    public static bool JumpHeld;
    public static bool Fire1Held;
    public static bool Fire2Held;

    // =====================  لحظات الضغط (تخزين رقم الإطار حتى القراءة بترتيب مستقل) =====================
    static int _jumpStamp = -1;
    static int _fire1Stamp = -1;
    static int _fire2Stamp = -1;
    static int _pickupStamp = -1;
    static int _menuStamp = -1;
    static int _weapon1Stamp = -1;
    static int _weapon2Stamp = -1;
    static int _weapon3Stamp = -1;
    static int _weapon4Stamp = -1;
    static int _healthStamp = -1;
    static int _energyStamp = -1;

    public static bool IsMobile => Application.isMobilePlatform;

    // =====================  واجهات كتابة (تستدعيها عناصر التحكم اللمسية) =====================
    public static void PressJump()   => _jumpStamp = Time.frameCount;
    public static void PressFire1()  => _fire1Stamp = Time.frameCount;
    public static void PressFire2()  => _fire2Stamp = Time.frameCount;
    public static void PressPickup() => _pickupStamp = Time.frameCount;
    public static void PressMenu()   => _menuStamp = Time.frameCount;
    public static void PressWeapon(int slot) { switch (slot) { case 1: _weapon1Stamp = Time.frameCount; break; case 2: _weapon2Stamp = Time.frameCount; break; case 3: _weapon3Stamp = Time.frameCount; break; case 4: _weapon4Stamp = Time.frameCount; break; } }
    public static void PressHealth() => _healthStamp = Time.frameCount;
    public static void PressEnergy() => _energyStamp = Time.frameCount;

    // =====================  قراءة موحّدة (الكمبيوتر => Input الأصلي) =====================

    public static float GetAxis(string axisName)
    {
        if (!IsMobile) return Input.GetAxis(axisName);

        switch (axisName)
        {
            case "Horizontal":
                return MoveAxis.sqrMagnitude > 0.0001f ? MoveAxis.x : Input.GetAxis(axisName);
            case "Vertical":
                return MoveAxis.sqrMagnitude > 0.0001f ? MoveAxis.y : Input.GetAxis(axisName);
            case "Mouse X":
                if (LookDelta.x != 0f) { float v = LookDelta.x; LookDelta.x = 0f; return v; }
                return Input.GetAxis(axisName);
            case "Mouse Y":
                if (LookDelta.y != 0f) { float v = LookDelta.y; LookDelta.y = 0f; return v; }
                return Input.GetAxis(axisName);
            default:
                return Input.GetAxis(axisName);
        }
    }

    public static bool GetButton(string buttonName)
    {
        if (!IsMobile) return Input.GetButton(buttonName);

        switch (buttonName)
        {
            case "Horizontal": return Mathf.Abs(MoveAxis.x) > 0.01f;
            case "Vertical":   return Mathf.Abs(MoveAxis.y) > 0.01f;
            case "Jump":       return JumpHeld || _jumpStamp == Time.frameCount;
            default:           return false;
        }
    }

    public static bool GetKeyDown(string name)
    {
        if (!IsMobile) return Input.GetKeyDown(name);

        switch (name.ToLowerInvariant())
        {
            case "1": return _weapon1Stamp == Time.frameCount;
            case "2": return _weapon2Stamp == Time.frameCount;
            case "3": return _weapon3Stamp == Time.frameCount;
            case "4": return _weapon4Stamp == Time.frameCount;
            case "5": return _healthStamp == Time.frameCount;
            case "6": return _energyStamp == Time.frameCount;
            case "f": return _pickupStamp == Time.frameCount;
            default:  return false;
        }
    }

    public static bool GetKeyDown(KeyCode key)
    {
        if (!IsMobile) return Input.GetKeyDown(key);

        switch (key)
        {
            case KeyCode.Tab:    return _menuStamp == Time.frameCount;
            case KeyCode.Alpha1: return _weapon1Stamp == Time.frameCount;
            case KeyCode.Alpha2: return _weapon2Stamp == Time.frameCount;
            case KeyCode.Alpha3: return _weapon3Stamp == Time.frameCount;
            case KeyCode.Alpha4: return _weapon4Stamp == Time.frameCount;
            case KeyCode.Alpha5: return _healthStamp == Time.frameCount;
            case KeyCode.Alpha6: return _energyStamp == Time.frameCount;
            case KeyCode.F:      return _pickupStamp == Time.frameCount;
            default:             return false;
        }
    }

    public static bool GetMouseButtonDown(int button)
    {
        if (!IsMobile) return Input.GetMouseButtonDown(button);

        switch (button)
        {
            case 0: return _fire1Stamp == Time.frameCount;
            case 1: return _fire2Stamp == Time.frameCount;
            default: return false;
        }
    }
}

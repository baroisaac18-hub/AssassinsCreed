using System.Collections;
using UnityEngine;

// ============================================================
//  AutoSaveManager — الحفظ التلقائي للمخزون والموقع
//  - 每 autosaveInterval 秒自动保存
//  - 切后台 / 退出时保存
//  - 从主菜单点「متابعة」进入时自动恢复存档
//
//  挂载方式：
//    1) 移动端由 MobileTouchControls 自动挂载（无需手动操作）
//    2) 也可手动挂到任意场景的空物体上
// ============================================================
public class AutoSaveManager : MonoBehaviour
{
    [Header("Auto Save Settings")]
    public float autosaveInterval = 30f;   // 每 30 秒自动保存一次
    public bool saveOnPause = true;        // 切后台保存
    public bool saveOnQuit = true;         // 退出保存

    private float _timer;
    private bool _loaded;

    private void Start()
    {
        if (Application.isMobilePlatform && SaveSystem.continueRequested && SaveSystem.HasSave())
        {
            StartCoroutine(LoadAfterFrame());
        }
    }

    private void Update()
    {
        // 仅移动端自动定时保存；PC 端由玩家通过菜单/暂停保存
        if (!Application.isMobilePlatform) return;

        _timer += Time.deltaTime;
        if (_timer >= autosaveInterval)
        {
            _timer = 0f;
            SaveNow();
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause && saveOnPause && Application.isMobilePlatform)
        {
            SaveNow();
        }
    }

    private void OnApplicationQuit()
    {
        if (saveOnQuit) SaveNow();
    }

    // ================= 保存 =================
    public void SaveNow()
    {
        PlayerScript player = FindObjectOfType<PlayerScript>();
        if (player == null) return;

        Inventory inv = FindObjectOfType<Inventory>();
        GameManager gm = FindObjectOfType<GameManager>();

        SaveData data = new SaveData();

        // ---- 库存 ----
        if (inv != null)
        {
            data.weaponsPicked[0] = inv.isWeapon1Picked;
            data.weaponsPicked[1] = inv.isWeapon2Picked;
            data.weaponsPicked[2] = inv.isWeapon3Picked;
            data.weaponsPicked[3] = inv.isWeapon4Picked;

            data.weaponsActive[0] = inv.isWeapon1Active;
            data.weaponsActive[1] = inv.isWeapon2Active;
            data.weaponsActive[2] = inv.isWeapon3Active;
            data.weaponsActive[3] = inv.isWeapon4Active;

            data.fistFightMode = inv.fistFightMode;
        }

        if (gm != null)
        {
            data.grenades = gm.numberofGrenades;
            data.healthPotions = gm.numberofHealth;
            data.energyPotions = gm.numberofEnergy;
        }

        // ---- 玩家 ----
        data.health = player.presentHealth;
        data.energy = player.presentEnergy;
        data.posX = player.transform.position.x;
        data.posY = player.transform.position.y;
        data.posZ = player.transform.position.z;
        data.rotX = player.transform.rotation.x;
        data.rotY = player.transform.rotation.y;
        data.rotZ = player.transform.rotation.z;
        data.rotW = player.transform.rotation.w;

        data.savedAtTicks = System.DateTime.UtcNow.Ticks;

        SaveSystem.Save(data);
    }

    // ================= 恢复 =================
    IEnumerator LoadAfterFrame()
    {
        // 等一帧，确保玩家与 UI 已生成
        yield return null;

        SaveData data = SaveSystem.Load();
        SaveSystem.continueRequested = false;
        if (data == null) yield break;

        PlayerScript player = FindObjectOfType<PlayerScript>();
        Inventory inv = FindObjectOfType<Inventory>();
        GameManager gm = FindObjectOfType<GameManager>();

        if (player != null)
        {
            // 位置与朝向
            player.transform.position = new Vector3(data.posX, data.posY, data.posZ);
            player.transform.rotation = new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW);

            // 血量与能量
            player.presentHealth = data.health;
            player.presentEnergy = data.energy;
            if (player.healthbar != null) player.healthbar.SetHealth(data.health);
            if (player.energybar != null) player.energybar.SetEnergy(data.energy);
        }

        if (inv != null)
        {
            inv.ApplyLoadedState(data.weaponsPicked, data.weaponsActive, data.fistFightMode);
        }

        if (gm != null)
        {
            gm.numberofGrenades = data.grenades;
            gm.numberofHealth = data.healthPotions;
            gm.numberofEnergy = data.energyPotions;
        }

        Debug.Log("[AutoSaveManager] تم استرجاع اللعبة بنجاح");
    }
}

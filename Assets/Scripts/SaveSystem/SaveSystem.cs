using System;
using System.IO;
using UnityEngine;

// ============================================================
//  SaveSystem — نظام الحفظ التلقائي (المخزون + موقع اللاعب)
//  存档位置: Application.persistentDataPath/assassins_creed_save.json
//  平台: Android / iOS / PC 通用
//
//  用法:
//    SaveSystem.Save(data)           -> 写入存档
//    SaveSystem.Load()               -> 读取存档（无存档返回 null）
//    SaveSystem.HasSave()            -> 是否存在存档
//    SaveSystem.DeleteSave()         -> 删除存档
//    SaveSystem.continueRequested    -> 从主菜单点「متابعة」时置 true
// ============================================================
public static class SaveSystem
{
    public const string SaveFileName = "assassins_creed_save.json";

    // 从主菜单点「متابعة / Continue」时置 true，游戏场景加载后自动恢复
    public static bool continueRequested = false;

    public static string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, SaveFileName); }
    }

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static void Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log("[SaveSystem] تم حفظ اللعبة -> " + SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveSystem] فشل الحفظ: " + e.Message);
        }
    }

    public static SaveData Load()
    {
        if (!HasSave()) return null;
        try
        {
            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("[SaveSystem] تم تحميل اللعبة");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveSystem] فشل التحميل: " + e.Message);
            return null;
        }
    }

    public static void DeleteSave()
    {
        if (HasSave())
        {
            File.Delete(SavePath);
            Debug.Log("[SaveSystem] تم حذف الحفظ القديم");
        }
    }
}

// ============================================================
//  SaveData — 存档数据
// ============================================================
[System.Serializable]
public class SaveData
{
    // ---- 库存 Inventory ----
    public bool[] weaponsPicked = new bool[4];   // 已拾取武器 1~4
    public bool[] weaponsActive = new bool[4];   // 当前装备武器 1~4
    public bool fistFightMode;                    // 拳头模式
    public int grenades;                          // 手雷数量
    public int healthPotions;                     // 血瓶数量
    public int energyPotions;                     // 能量瓶数量

    // ---- 玩家 Player ----
    public float health;                          // 当前血量
    public float energy;                          // 当前能量
    public float posX, posY, posZ;                // 位置
    public float rotX, rotY, rotZ, rotW;          // 朝向（四元数）

    // ---- 元信息 ----
    public string sceneName = "MainScene";
    public long savedAtTicks;                     // 保存时间
}

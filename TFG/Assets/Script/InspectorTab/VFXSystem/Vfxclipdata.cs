using UnityEngine;

// ==========================================
// VFX Clip 基础数据类
// 如果要做一个独特的 VFX（比如爆炸、烟雾），
// 只需新建一个类继承 VFXClipData，加上额外字段即可。
//
// 使用方式：
//   class ExplosionClipData : VFXClipData { public float blastRadius = 5f; }
// ==========================================
[System.Serializable]
public class VFXClipData
{
    // ---------- 变换 ----------
    public float posX = 0f;
    public float posY = 3f;
    public float posZ = 0f;

    public float rotX = 0f;
    public float rotY = 0f;
    public float rotZ = 0f;

    public float scaleX = 1f;
    public float scaleY = 1f;
    public float scaleZ = 1f;

    // ---------- 外观 ----------
    public Color color = Color.white;

    // ---------- 播放 ----------
    public float playSpeed = 1f;   // 0.1 ~ 3.0
    public bool loop = true; // true = 在 clip 内循环；false = 播一次后停留

    // ---------- Slider 范围（在 Inspector 预制体上配置）----------
    [Header("Position 范围")]
    public float posXMin = -20f; public float posXMax = 20f;
    public float posYMin = -5f; public float posYMax = 15f;
    public float posZMin = -20f; public float posZMax = 20f;

    [Header("Rotation 范围")]
    public float rotMin = 0f; public float rotMax = 360f;

    [Header("Scale 范围")]
    public float scaleMin = 0.1f; public float scaleMax = 5f;

    [Header("PlaySpeed 范围")]
    public float speedMin = 0.1f; public float speedMax = 3f;

    // ---------- 快捷属性 ----------
    public Vector3 Position => new Vector3(posX, posY, posZ);
    public Quaternion Rotation => Quaternion.Euler(rotX, rotY, rotZ);
    public Vector3 Scale => new Vector3(scaleX, scaleY, scaleZ);

    // ---------- 运行时关联（由 VFXPlaybackSystem 注入）----------
    [System.NonSerialized] public GameObject runtimeInstance = null;
    [System.NonSerialized] public string vfxPrefabName = "";  // 记录来自哪个 Prefab
}
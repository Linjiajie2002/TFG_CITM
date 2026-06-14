using UnityEngine;

// ==========================================
// VFX Clip 统一数据类（已融合 NewVFXClipData）
//
// 同时服务于：
//   - VFXClipInspectorPanel     （原始 VFX 面板）
//   - NewVFXClipInspectorPanel  （父节点多子节点控制 + 颜色面板）
//   - VFXPlaybackSystem         （播放系统，无需修改）
//
// 扩展方式：
//   直接在本类加字段即可，不再需要子类。
// ==========================================
[System.Serializable]
public class VFXClipData
{
    // ─────────────────────────────────────
    // 变换
    // ─────────────────────────────────────
    public float posX = 0f;
    public float posY = 3f;
    public float posZ = 0f;

    public float rotX = 0f;
    public float rotY = 0f;
    public float rotZ = 0f;

    public float scaleX = 1f;
    public float scaleY = 1f;
    public float scaleZ = 1f;

    // ─────────────────────────────────────
    // 外观
    // ─────────────────────────────────────
    public Color color = Color.white;

    // ─────────────────────────────────────
    // 播放
    // ─────────────────────────────────────
    public float playSpeed = 1f;    // 0.1 ~ 3.0
    public bool loop = true;  // true = clip 内循环；false = 播一次后停留

    // ─────────────────────────────────────
    // Slider 范围（在 Inspector 预制体上配置，运行时由面板同步写入）
    // ─────────────────────────────────────
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

    // ─────────────────────────────────────
    // 快捷属性
    // ─────────────────────────────────────
    public Vector3 Position => new Vector3(posX, posY, posZ);
    public Quaternion Rotation => Quaternion.Euler(rotX, rotY, rotZ);
    public Vector3 Scale => new Vector3(scaleX, scaleY, scaleZ);

    // ─────────────────────────────────────
    // 运行时关联（由 VFXPlaybackSystem 注入，不参与序列化）
    // ─────────────────────────────────────
    [System.NonSerialized] public GameObject runtimeInstance = null;   // 实例化的 Prefab GO（VFXClipInspectorPanel 用）
    [System.NonSerialized] public string vfxPrefabName = "";     // 来源 Prefab 名称（调试用）
}
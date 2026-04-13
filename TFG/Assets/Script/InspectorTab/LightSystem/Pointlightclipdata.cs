using UnityEngine;

// ==========================================
// 每个 Point Light Clip 的参数数据
// 由 PointLightClipInspectorPanel 写入
// 由 LightPlaybackSystem 读取
// ==========================================
[System.Serializable]
public class PointLightClipData
{
    // ---------- 位置 ----------
    public float posX = 0f;
    public float posY = 3f;
    public float posZ = 0f;

    // ---------- 颜色 ----------
    public Color color = Color.white;

    // ---------- 强度 ----------
    public float intensity = 1f;

    // ---------- 范围 ----------
    public float range = 10f;

    // ---------- Slider 范围（在 Inspector 里配置，运行时不变）----------
    [Header("Position 范围")]
    public float posXMin = -20f; public float posXMax = 20f;
    public float posYMin = 0f; public float posYMax = 15f;
    public float posZMin = -20f; public float posZMax = 20f;

    [Header("Intensity 范围")]
    public float intensityMin = 0f;
    public float intensityMax = 10f;

    [Header("Range 范围")]
    public float rangeMin = 1f;
    public float rangeMax = 30f;

    // ---------- 运行时关联的 Light 对象（由 LightPlaybackSystem 注入）----------
    [System.NonSerialized] public Light runtimeLight = null;

    public Vector3 Position => new Vector3(posX, posY, posZ);
}
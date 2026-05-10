using UnityEngine;

[System.Serializable]
public class OutlineClipData
{
    // ---------- Shader 参数 ----------
    public Color outlineColor = Color.white;
    public float colorThreshold = 1f;
    public float normalThreshold = 1f;

    // ---------- Slider 范围（分开）----------
    public float colorThresholdMin = 0.1f; public float colorThresholdMax = 2f;
    public float normalThresholdMin = 0.1f; public float normalThresholdMax = 3f;

    // ---------- 运行时 ----------
    [System.NonSerialized] public Material runtimeMaterial = null;

    // ---------- 写入 Shader ----------
    public void ApplyToMaterial(Material mat)
    {
        if (mat == null) return;
        mat.SetColor("_OutlineColor", outlineColor);
        mat.SetFloat("_ColorThreshold", colorThreshold);
        mat.SetFloat("_NormalThreshold", normalThreshold);
    }
}
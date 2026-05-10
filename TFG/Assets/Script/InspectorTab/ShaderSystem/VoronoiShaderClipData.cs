using UnityEngine;

[System.Serializable]
public class VoronoiShaderClipData
{
    // ---------- 渐变设置 ----------
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;
    public float fadeMin = 0f;
    public float fadeMax = 3f;

    // ---------- 颜色 ----------
    public Color color = new Color(0.2f, 0.5f, 1f, 1f);

    // ---------- Voronoi ----------
    public float voronoiSpeed = 3f;
    public float voronoiScale = 100f;
    public float voronoiPower = 3f;

    // ---------- Vignette ----------
    public float vignetteRadiusPower = 6f;
    public float vignetteIntensity = 1f;

    // ---------- Glow ----------
    public float glowPower = 1f;

    // ---------- Slider 范围 ----------
    public float voronoiSpeedMin = 0f; public float voronoiSpeedMax = 10f;
    public float voronoiScaleMin = 0.1f; public float voronoiScaleMax = 20f;
    public float voronoiPowerMin = 0f; public float voronoiPowerMax = 5f;
    public float vignetteRadiusMin = 0.5f; public float vignetteRadiusMax = 10f;
    public float vignetteIntMin = 0f; public float vignetteIntMax = 2f;
    public float glowMin = 0f; public float glowMax = 5f;

    // ---------- 运行时（不序列化）----------
    [System.NonSerialized] public float currentAlpha = 0f;
    [System.NonSerialized] public Material runtimeMaterial = null;
    [System.NonSerialized] public string shaderEntryName = "";

    // ---------- 写入 Shader ----------
    public void ApplyToMaterial(Material mat, float alpha)
    {
        if (mat == null) return;
        if (mat.HasProperty("_FullIntensity")) mat.SetFloat("_FullIntensity", alpha);

        if (mat.HasProperty("_Color"))
        {
            Color c = color;
            c.a = Mathf.Clamp01(c.a * alpha);
            mat.SetColor("_Color", c);
        }

        if (mat.HasProperty("_VoronoiSpeed")) mat.SetFloat("_VoronoiSpeed", voronoiSpeed);
        if (mat.HasProperty("_VoronoiScale")) mat.SetFloat("_VoronoiScale", voronoiScale);
        if (mat.HasProperty("_VoronoiPower")) mat.SetFloat("_VoronoiPower", voronoiPower);
        if (mat.HasProperty("_VignetteRadius")) mat.SetFloat("_VignetteRadius", vignetteRadiusPower);
        if (mat.HasProperty("_VignetteIntensity")) mat.SetFloat("_VignetteIntensity", vignetteIntensity);
        if (mat.HasProperty("_GlowPower")) mat.SetFloat("_GlowPower", glowPower);
    }
}
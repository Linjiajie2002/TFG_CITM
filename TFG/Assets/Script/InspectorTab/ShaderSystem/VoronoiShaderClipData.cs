using UnityEngine;

// ==========================================
// Voronoi + Vignette + Glow 全屏 Shader 的参数数据
// ==========================================
[System.Serializable]
public class VoronoiShaderClipData : ShaderClipData
{
    // ---------- 颜色 ----------
    public Color color = new Color(0.2f, 0.5f, 1f, 1f);

    // ---------- Voronoi ----------
    public float voronoiSpeed = 3f;
    public float voronoiScale = 100f;
    public float voronoiPower = 3f;

    // ---------- Vignette ----------
    public float vignetteRadiusPower = 6f;
    public float vignetteIntensity   = 1f;

    // ---------- Glow ----------
    public float glowPower = 1f;

    // ---------- Slider 范围（在面板预制体里配置）----------
    [Header("Voronoi 范围")]
    public float voronoiSpeedMin = 0f;  public float voronoiSpeedMax = 10f;
    public float voronoiScaleMin = 0.1f; public float voronoiScaleMax = 20f;
    public float voronoiPowerMin = 0f;  public float voronoiPowerMax = 5f;

    [Header("Vignette 范围")]
    public float vignetteRadiusMin = 0.5f; public float vignetteRadiusMax = 10f;
    public float vignetteIntMin    = 0f;   public float vignetteIntMax    = 2f;

    [Header("Glow 范围")]
    public float glowMin = 0f; public float glowMax = 5f;

    // ==========================================
    // 重写：把所有参数写入 material
    // ==========================================
    public override void ApplyToMaterial(Material mat, float alpha)
    {
        base.ApplyToMaterial(mat, alpha); // 先写 _FullIntensity

        if (mat == null) return;

        // 颜色（带当前 alpha 混合，让颜色也随渐入渐出变化）
        if (mat.HasProperty("_Color"))
        {
            Color c = color;
            c.a = Mathf.Clamp01(c.a * alpha);
            mat.SetColor("_Color", c);
        }

        if (mat.HasProperty("_VoronoiSpeed"))  mat.SetFloat("_VoronoiSpeed", voronoiSpeed);
        if (mat.HasProperty("_VoronoiScale"))  mat.SetFloat("_VoronoiScale", voronoiScale);
        if (mat.HasProperty("_VoronoiPower"))  mat.SetFloat("_VoronoiPower", voronoiPower);

        if (mat.HasProperty("_VignetteRadius")) mat.SetFloat("_VignetteRadius", vignetteRadiusPower);
        if (mat.HasProperty("_VignetteIntensity")) mat.SetFloat("_VignetteIntensity", vignetteIntensity);

        if (mat.HasProperty("_GlowPower")) mat.SetFloat("_GlowPower", glowPower);
    }
}

// ==========================================
// 【示例】如何在 3 步内添加一个全新的 Shader 类型
// 例如：色差/故障艺术风 Glitch Shader
// ==========================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ── 步骤1：继承 ShaderClipData，加入专属参数 ──
[System.Serializable]
public class GlitchShaderClipData : ShaderClipData
{
    public float glitchFrequency = 5f;
    public float glitchStrength  = 0.1f;
    public Color glitchColor     = Color.cyan;

    public float freqMin = 0f; public float freqMax = 30f;
    public float strMin  = 0f; public float strMax  = 1f;

    // 重写：把参数写入 material
    public override void ApplyToMaterial(Material mat, float alpha)
    {
        base.ApplyToMaterial(mat, alpha); // 先写 _FullIntensity

        if (mat == null) return;
        if (mat.HasProperty("_GlitchFrequency")) mat.SetFloat("_GlitchFrequency", glitchFrequency);
        if (mat.HasProperty("_GlitchStrength"))  mat.SetFloat("_GlitchStrength",  glitchStrength * alpha);
        if (mat.HasProperty("_GlitchColor"))     mat.SetColor("_GlitchColor",     glitchColor);
    }
}

// ── 步骤2：继承 ShaderClipInspectorPanel，加入专属 Slider ──
public class GlitchShaderClipInspectorPanel : ShaderClipInspectorPanel
{
    [Header("=== Glitch ===")]
    public Slider sliderFrequency;
    public Slider sliderStrength;
    public TextMeshProUGUI freqText;
    public TextMeshProUGUI strText;

    public float freqMin = 0f; public float freqMax = 30f;
    public float strMin  = 0f; public float strMax  = 1f;

    private GlitchShaderClipData gData;

    protected override ShaderClipData CreateShaderData() => new GlitchShaderClipData();

    protected override void OnBindShaderExtra()
    {
        gData = shaderData as GlitchShaderClipData;
        if (gData == null) return;

        InitSlider(sliderFrequency, freqMin, freqMax, gData.glitchFrequency);
        InitSlider(sliderStrength,  strMin,  strMax,  gData.glitchStrength);

        if (sliderFrequency != null) sliderFrequency.onValueChanged.AddListener(v => { gData.glitchFrequency = v; OnDataChanged(); });
        if (sliderStrength  != null) sliderStrength.onValueChanged.AddListener(v  => { gData.glitchStrength  = v; OnDataChanged(); });
    }

    protected override void OnRefreshShaderExtra()
    {
        if (gData == null) return;
        SetLabel(freqText, $"{gData.glitchFrequency:F1} Hz");
        SetLabel(strText,  $"{gData.glitchStrength:F2}");
    }
}

// ── 步骤3（Unity 编辑器里做，不用写代码）──
// 1. 把 GlitchShaderClipInspectorPanel 做成面板 Prefab
// 2. 在 DynamicModuleSystem 里 Shader 模块的 featurePanelMaps 加一条：
//      featureName = "Glitch"    prefab = ClipPanel_GlitchShader
// 3. 在 ShaderPlaybackSystem 的 shaderEntries 加一条：
//      trackName = "Shader"   material = 你的 GlitchMaterial
// 完成！渐入渐出自动处理，不需要修改任何系统代码

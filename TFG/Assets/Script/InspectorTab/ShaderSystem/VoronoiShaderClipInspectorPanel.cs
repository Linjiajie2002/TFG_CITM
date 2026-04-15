using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// Voronoi Shader 专属 Inspector 面板
//
// 预制体结构（ClipPanel_VoronoiShader）：
//   ├── Header (clipNameText + deleteButton + backButton)
//   ├── TimeInfo (startTimeText + durationText + endTimeText)
//   ├── Section_Fade
//   │   ├── Row_FadeIn  → Slider(sliderFadeIn)  + Text(fadeInText)
//   │   └── Row_FadeOut → Slider(sliderFadeOut) + Text(fadeOutText)
//   ├── Section_Color
//   │   └── ColorPickerPanel(colorPicker)
//   ├── Section_Voronoi
//   │   ├── Row_Speed → Label "Speed" + Slider(sliderVoronoiSpeed) + Text(voronoiSpeedText)
//   │   ├── Row_Scale → Label "Scale" + Slider(sliderVoronoiScale) + Text(voronoiScaleText)
//   │   └── Row_Power → Label "Power" + Slider(sliderVoronoiPower) + Text(voronoiPowerText)
//   ├── Section_Vignette
//   │   ├── Row_Radius    → Slider(sliderVigRadius)    + Text(vigRadiusText)
//   │   └── Row_Intensity → Slider(sliderVigIntensity) + Text(vigIntensityText)
//   └── Section_Glow
//       └── Row_Glow → Slider(sliderGlowPower) + Text(glowPowerText)
// ==========================================
public class VoronoiShaderClipInspectorPanel : ShaderClipInspectorPanel
{
    [Header("=== Color ===")]
    public ColorPickerPanel colorPicker;

    [Header("=== Voronoi ===")]
    public Slider sliderVoronoiSpeed;
    public Slider sliderVoronoiScale;
    public Slider sliderVoronoiPower;
    public TextMeshProUGUI voronoiSpeedText;
    public TextMeshProUGUI voronoiScaleText;
    public TextMeshProUGUI voronoiPowerText;

    [Header("Voronoi 范围")]
    public float voronoiSpeedMin = 0f;  public float voronoiSpeedMax = 10f;
    public float voronoiScaleMin = 0.1f; public float voronoiScaleMax = 20f;
    public float voronoiPowerMin = 0f;  public float voronoiPowerMax = 5f;

    [Header("=== Vignette ===")]
    public Slider sliderVigRadius;
    public Slider sliderVigIntensity;
    public TextMeshProUGUI vigRadiusText;
    public TextMeshProUGUI vigIntensityText;

    [Header("Vignette 范围")]
    public float vigRadiusMin = 0.5f; public float vigRadiusMax = 10f;
    public float vigIntMin    = 0f;   public float vigIntMax    = 2f;

    [Header("=== Glow ===")]
    public Slider sliderGlowPower;
    public TextMeshProUGUI glowPowerText;

    [Header("Glow 范围")]
    public float glowMin = 0f; public float glowMax = 5f;

    // 本地强类型引用
    private VoronoiShaderClipData vData;

    // ==========================================
    // 创建正确的数据类型
    protected override ShaderClipData CreateShaderData() => new VoronoiShaderClipData();

    // ==========================================
    // 绑定所有专属 Slider
    protected override void OnBindShaderExtra()
    {
        vData = shaderData as VoronoiShaderClipData;
        if (vData == null) return;

        // 同步范围到数据
        vData.voronoiSpeedMin = voronoiSpeedMin; vData.voronoiSpeedMax = voronoiSpeedMax;
        vData.voronoiScaleMin = voronoiScaleMin; vData.voronoiScaleMax = voronoiScaleMax;
        vData.voronoiPowerMin = voronoiPowerMin; vData.voronoiPowerMax = voronoiPowerMax;
        vData.vignetteRadiusMin = vigRadiusMin; vData.vignetteRadiusMax = vigRadiusMax;
        vData.vignetteIntMin    = vigIntMin;    vData.vignetteIntMax    = vigIntMax;
        vData.glowMin = glowMin; vData.glowMax = glowMax;

        // 初始化 Slider
        InitSlider(sliderVoronoiSpeed, voronoiSpeedMin, voronoiSpeedMax, vData.voronoiSpeed);
        InitSlider(sliderVoronoiScale, voronoiScaleMin, voronoiScaleMax, vData.voronoiScale);
        InitSlider(sliderVoronoiPower, voronoiPowerMin, voronoiPowerMax, vData.voronoiPower);
        InitSlider(sliderVigRadius,    vigRadiusMin,    vigRadiusMax,    vData.vignetteRadiusPower);
        InitSlider(sliderVigIntensity, vigIntMin,       vigIntMax,       vData.vignetteIntensity);
        InitSlider(sliderGlowPower,    glowMin,         glowMax,         vData.glowPower);

        // 监听
        Reg(sliderVoronoiSpeed, v => { vData.voronoiSpeed         = v; OnDataChanged(); });
        Reg(sliderVoronoiScale, v => { vData.voronoiScale         = v; OnDataChanged(); });
        Reg(sliderVoronoiPower, v => { vData.voronoiPower         = v; OnDataChanged(); });
        Reg(sliderVigRadius,    v => { vData.vignetteRadiusPower  = v; OnDataChanged(); });
        Reg(sliderVigIntensity, v => { vData.vignetteIntensity    = v; OnDataChanged(); });
        Reg(sliderGlowPower,    v => { vData.glowPower            = v; OnDataChanged(); });

        // Color Picker
        if (colorPicker != null)
        {
            colorPicker.SetColor(vData.color, notify: false);
            colorPicker.onColorChanged.AddListener(c => { vData.color = c; OnDataChanged(); });
        }
    }

    // ==========================================
    // RefreshDisplay 时刷新标签
    protected override void OnRefreshShaderExtra()
    {
        if (vData == null) return;
        SetLabel(voronoiSpeedText, $"{vData.voronoiSpeed:F2}");
        SetLabel(voronoiScaleText, $"{vData.voronoiScale:F2}");
        SetLabel(voronoiPowerText, $"{vData.voronoiPower:F2}");
        SetLabel(vigRadiusText,    $"{vData.vignetteRadiusPower:F2}");
        SetLabel(vigIntensityText, $"{vData.vignetteIntensity:F2}");
        SetLabel(glowPowerText,    $"{vData.glowPower:F2}");
    }

    // ==========================================
    private void Reg(Slider s, UnityEngine.Events.UnityAction<float> cb)
    {
        if (s != null) s.onValueChanged.AddListener(cb);
    }
}

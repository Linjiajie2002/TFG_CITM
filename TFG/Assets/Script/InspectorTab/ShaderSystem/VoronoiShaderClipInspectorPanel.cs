using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoronoiShaderClipInspectorPanel : ClipInspectorPanel
{
    [Header("=== Fade ===")]
    public Slider sliderFadeIn, sliderFadeOut;
    public TextMeshProUGUI fadeInText, fadeOutText;
    public float fadeMin = 0f; public float fadeMax = 3f;

    [Header("=== Color ===")]
    public ColorPickerPanel colorPicker;

    [Header("=== Voronoi ===")]
    public Slider sliderVoronoiSpeed, sliderVoronoiScale, sliderVoronoiPower;
    public TextMeshProUGUI voronoiSpeedText, voronoiScaleText, voronoiPowerText;
    public float voronoiSpeedMin = 0f; public float voronoiSpeedMax = 10f;
    public float voronoiScaleMin = 0.1f; public float voronoiScaleMax = 20f;
    public float voronoiPowerMin = 0f; public float voronoiPowerMax = 5f;

    [Header("=== Vignette ===")]
    public Slider sliderVigRadius, sliderVigIntensity;
    public TextMeshProUGUI vigRadiusText, vigIntensityText;
    public float vigRadiusMin = 0.5f; public float vigRadiusMax = 10f;
    public float vigIntMin = 0f; public float vigIntMax = 2f;

    [Header("=== Glow ===")]
    public Slider sliderGlowPower;
    public TextMeshProUGUI glowPowerText;
    public float glowMin = 0f; public float glowMax = 5f;

    private VoronoiShaderClipData vData;
    private bool isReady = false;

    protected override void Awake() => base.Awake();

    public override void BindClip(TimelineEventData clip, TimelineManager mgr)
    {
        isReady = false;
        base.BindClip(clip, mgr);

        vData = clip.customData is VoronoiShaderClipData ex ? ex : new VoronoiShaderClipData();
        clip.customData = vData;

        vData.fadeMin = fadeMin; vData.fadeMax = fadeMax;
        vData.voronoiSpeedMin = voronoiSpeedMin; vData.voronoiSpeedMax = voronoiSpeedMax;
        vData.voronoiScaleMin = voronoiScaleMin; vData.voronoiScaleMax = voronoiScaleMax;
        vData.voronoiPowerMin = voronoiPowerMin; vData.voronoiPowerMax = voronoiPowerMax;
        vData.vignetteRadiusMin = vigRadiusMin; vData.vignetteRadiusMax = vigRadiusMax;
        vData.vignetteIntMin = vigIntMin; vData.vignetteIntMax = vigIntMax;
        vData.glowMin = glowMin; vData.glowMax = glowMax;

        InitSlider(sliderFadeIn, fadeMin, fadeMax, vData.fadeInDuration);
        InitSlider(sliderFadeOut, fadeMin, fadeMax, vData.fadeOutDuration);
        InitSlider(sliderVoronoiSpeed, voronoiSpeedMin, voronoiSpeedMax, vData.voronoiSpeed);
        InitSlider(sliderVoronoiScale, voronoiScaleMin, voronoiScaleMax, vData.voronoiScale);
        InitSlider(sliderVoronoiPower, voronoiPowerMin, voronoiPowerMax, vData.voronoiPower);
        InitSlider(sliderVigRadius, vigRadiusMin, vigRadiusMax, vData.vignetteRadiusPower);
        InitSlider(sliderVigIntensity, vigIntMin, vigIntMax, vData.vignetteIntensity);
        InitSlider(sliderGlowPower, glowMin, glowMax, vData.glowPower);

        Reg(sliderFadeIn, v => { vData.fadeInDuration = v; Refresh(); });
        Reg(sliderFadeOut, v => { vData.fadeOutDuration = v; Refresh(); });
        Reg(sliderVoronoiSpeed, v => { vData.voronoiSpeed = v; Refresh(); });
        Reg(sliderVoronoiScale, v => { vData.voronoiScale = v; Refresh(); });
        Reg(sliderVoronoiPower, v => { vData.voronoiPower = v; Refresh(); });
        Reg(sliderVigRadius, v => { vData.vignetteRadiusPower = v; Refresh(); });
        Reg(sliderVigIntensity, v => { vData.vignetteIntensity = v; Refresh(); });
        Reg(sliderGlowPower, v => { vData.glowPower = v; Refresh(); });

        if (colorPicker != null)
        {
            colorPicker.onColorChanged.RemoveAllListeners();
            colorPicker.SetColor(vData.color, notify: false);
            colorPicker.onColorChanged.AddListener(c => { vData.color = c; Refresh(); });
        }

        isReady = true;
        RefreshDisplay();
    }

    public override void RefreshDisplay()
    {
        base.RefreshDisplay();
        if (!isReady || vData == null) return;
        if (fadeInText) fadeInText.text = $"{vData.fadeInDuration:F2}s";
        if (fadeOutText) fadeOutText.text = $"{vData.fadeOutDuration:F2}s";
        if (voronoiSpeedText) voronoiSpeedText.text = $"{vData.voronoiSpeed:F2}";
        if (voronoiScaleText) voronoiScaleText.text = $"{vData.voronoiScale:F2}";
        if (voronoiPowerText) voronoiPowerText.text = $"{vData.voronoiPower:F2}";
        if (vigRadiusText) vigRadiusText.text = $"{vData.vignetteRadiusPower:F2}";
        if (vigIntensityText) vigIntensityText.text = $"{vData.vignetteIntensity:F2}";
        if (glowPowerText) glowPowerText.text = $"{vData.glowPower:F2}";
    }

    private void InitSlider(Slider s, float min, float max, float val)
    {
        if (s == null) return;
        s.onValueChanged.RemoveAllListeners();
        s.minValue = min; s.maxValue = max;
        s.value = Mathf.Clamp(val, min, max);
    }

    private void Reg(Slider s, UnityEngine.Events.UnityAction<float> cb)
    {
        if (s != null) s.onValueChanged.AddListener(cb);
    }

    private void Refresh() => RefreshDisplay();
}
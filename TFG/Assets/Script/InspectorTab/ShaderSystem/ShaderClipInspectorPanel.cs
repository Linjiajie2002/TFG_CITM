using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// Shader Clip Inspector 面板 — 基类（修复版）
//
// 修复：删除 RefreshDisplay 里的 PushToMaterial(1f)
//   原来每帧都以 alpha=1f 写入 material，完全绕过了渐变计算。
//   现在材质完全由 ShaderPlaybackSystem 的 CalculateAlpha 控制。
//   Inspector 面板只负责修改数据，不直接操控材质强度。
// ==========================================
public class ShaderClipInspectorPanel : ClipInspectorPanel
{
    [Header("=== 渐入渐出 Sliders ===")]
    public Slider sliderFadeIn;
    public Slider sliderFadeOut;
    public TextMeshProUGUI fadeInText;
    public TextMeshProUGUI fadeOutText;

    [Header("渐变时长范围 (秒)")]
    public float fadeMin = 0f;
    public float fadeMax = 3f;

    protected ShaderClipData shaderData;
    private bool isReady = false;

    protected override void Awake() { base.Awake(); }

    public override void BindClip(TimelineEventData clip, TimelineManager mgr)
    {
        base.BindClip(clip, mgr);

        shaderData = clip.customData as ShaderClipData ?? CreateShaderData();
        if (clip.customData == null) clip.customData = shaderData;

        shaderData.fadeMin = fadeMin;
        shaderData.fadeMax = fadeMax;

        InitSlider(sliderFadeIn, fadeMin, fadeMax, shaderData.fadeInDuration);
        InitSlider(sliderFadeOut, fadeMin, fadeMax, shaderData.fadeOutDuration);

        if (sliderFadeIn != null) sliderFadeIn.onValueChanged.AddListener(v =>
        {
            shaderData.fadeInDuration = v;
            UpdateFadeLabels();
        });
        if (sliderFadeOut != null) sliderFadeOut.onValueChanged.AddListener(v =>
        {
            shaderData.fadeOutDuration = v;
            UpdateFadeLabels();
        });

        isReady = true;
        OnBindShaderExtra();
        RefreshDisplay();
    }

    public override void RefreshDisplay()
    {
        base.RefreshDisplay();
        if (!isReady || shaderData == null) return;

        UpdateFadeLabels();
        OnRefreshShaderExtra();

        // 【修复2】：不再调用 PushToMaterial(1f)
        // 材质由 ShaderPlaybackSystem 根据时间计算 alpha 后统一写入
        // 这里只刷新 UI 显示，不直接操控材质
    }

    private void UpdateFadeLabels()
    {
        if (fadeInText != null) fadeInText.text = $"{shaderData.fadeInDuration:F2}s";
        if (fadeOutText != null) fadeOutText.text = $"{shaderData.fadeOutDuration:F2}s";
    }

    // ==========================================
    // 子类重写区域
    // ==========================================
    protected virtual ShaderClipData CreateShaderData() => new ShaderClipData();
    protected virtual void OnBindShaderExtra() { }
    protected virtual void OnShaderDataChangedExtra() { }
    protected virtual void OnRefreshShaderExtra() { }

    protected void OnDataChanged()
    {
        UpdateFadeLabels();
        OnRefreshShaderExtra();
        OnShaderDataChangedExtra();
        // 【修复2】：不写材质，由 ShaderPlaybackSystem 负责
    }

    // ==========================================
    protected void InitSlider(Slider s, float min, float max, float val)
    {
        if (s == null) return;
        s.onValueChanged.RemoveAllListeners();
        s.minValue = min;
        s.maxValue = max;
        s.value = Mathf.Clamp(val, min, max);
    }

    protected void SetLabel(TextMeshProUGUI t, string text) { if (t != null) t.text = text; }
}
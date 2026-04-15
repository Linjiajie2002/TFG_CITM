using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// Shader Clip Inspector 面板 — 基类
//
// 【扩展方式】：
//   继承本类，重写三个虚方法：
//     protected virtual void OnBindShaderExtra()        → 绑定子类 Slider
//     protected virtual void OnShaderDataChangedExtra() → 数据变化后子类处理
//     protected virtual void OnRefreshShaderExtra()     → RefreshDisplay 时子类刷新
//
// 预制体结构（ClipPanel_Shader_Base）：
//   ├── Header (clipNameText + deleteButton + backButton)
//   ├── TimeInfo (startTimeText + durationText + endTimeText)
//   ├── Section_Fade
//   │   ├── Row_FadeIn  → Label "渐入" + Slider(sliderFadeIn)  + Text(fadeInText)
//   │   └── Row_FadeOut → Label "渐出" + Slider(sliderFadeOut) + Text(fadeOutText)
//   └── Extra_Content ← 子类面板在这里追加专属 Slider
// ==========================================
public class ShaderClipInspectorPanel : ClipInspectorPanel
{
    [Header("=== 渐入渐出 Sliders ===")]
    public Slider          sliderFadeIn;
    public Slider          sliderFadeOut;
    public TextMeshProUGUI fadeInText;
    public TextMeshProUGUI fadeOutText;

    [Header("渐变时长范围 (秒)")]
    public float fadeMin = 0f;
    public float fadeMax = 3f;

    protected ShaderClipData shaderData;
    private   bool           isReady = false;

    // ==========================================
    protected override void Awake() { base.Awake(); }

    public override void BindClip(TimelineEventData clip, TimelineManager mgr)
    {
        base.BindClip(clip, mgr);

        shaderData = clip.customData as ShaderClipData ?? CreateShaderData();
        if (clip.customData == null) clip.customData = shaderData;

        shaderData.fadeMin = fadeMin;
        shaderData.fadeMax = fadeMax;

        InitSlider(sliderFadeIn,  fadeMin, fadeMax, shaderData.fadeInDuration);
        InitSlider(sliderFadeOut, fadeMin, fadeMax, shaderData.fadeOutDuration);

        if (sliderFadeIn  != null) sliderFadeIn.onValueChanged.AddListener(v  => { shaderData.fadeInDuration  = v; OnDataChanged(); });
        if (sliderFadeOut != null) sliderFadeOut.onValueChanged.AddListener(v => { shaderData.fadeOutDuration = v; OnDataChanged(); });

        isReady = true;
        OnBindShaderExtra();
        RefreshDisplay();
    }

    public override void RefreshDisplay()
    {
        base.RefreshDisplay();
        if (!isReady || shaderData == null) return;

        if (fadeInText  != null) fadeInText.text  = $"{shaderData.fadeInDuration:F2}s";
        if (fadeOutText != null) fadeOutText.text = $"{shaderData.fadeOutDuration:F2}s";

        OnRefreshShaderExtra();
        PushToMaterial();
    }

    // ==========================================
    // 子类重写区域
    // ==========================================
    protected virtual ShaderClipData CreateShaderData()    => new ShaderClipData();
    protected virtual void OnBindShaderExtra()             { }
    protected virtual void OnShaderDataChangedExtra()      { }
    protected virtual void OnRefreshShaderExtra()          { }

    // ==========================================
    protected void OnDataChanged()
    {
        RefreshDisplay();
        OnShaderDataChangedExtra();
    }

    // 把数据推给运行时 Material（Edit 模式实时预览）
    protected void PushToMaterial()
    {
        if (shaderData?.runtimeMaterial == null) return;
        shaderData.ApplyToMaterial(shaderData.runtimeMaterial, 1f); // 编辑时用完整强度预览
    }

    // ==========================================
    protected void InitSlider(Slider s, float min, float max, float val)
    {
        if (s == null) return;
        s.onValueChanged.RemoveAllListeners();
        s.minValue = min;
        s.maxValue = max;
        s.value    = Mathf.Clamp(val, min, max);
    }

    protected void SetLabel(TextMeshProUGUI t, string text) { if (t != null) t.text = text; }
}

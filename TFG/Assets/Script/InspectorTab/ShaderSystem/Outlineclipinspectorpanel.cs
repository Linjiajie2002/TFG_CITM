using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OutlineClipInspectorPanel : ClipInspectorPanel
{
    [Header("=== Outline Color ===")]
    public ColorPickerPanel colorPickerOutline;

    [Header("=== Color Threshold ===")]
    public Slider sliderColorThreshold;
    public TextMeshProUGUI colorThresholdText;

    [Header("=== Normal Threshold ===")]
    public Slider sliderNormalThreshold;
    public TextMeshProUGUI normalThresholdText;

    private OutlineClipData outlineData;
    private bool isReady = false;

    protected override void Awake() => base.Awake();

    public override void BindClip(TimelineEventData clip, TimelineManager mgr)
    {
        isReady = false;
        base.BindClip(clip, mgr);

        outlineData = clip.customData is OutlineClipData ex ? ex : new OutlineClipData();
        clip.customData = outlineData;

        SetupSlider(sliderColorThreshold, outlineData.colorThresholdMin, outlineData.colorThresholdMax, outlineData.colorThreshold);
        SetupSlider(sliderNormalThreshold, outlineData.normalThresholdMin, outlineData.normalThresholdMax, outlineData.normalThreshold);

        if (colorPickerOutline != null)
        {
            colorPickerOutline.onColorChanged.RemoveAllListeners();
            colorPickerOutline.SetColor(outlineData.outlineColor, notify: false);
            colorPickerOutline.onColorChanged.AddListener(c => { outlineData.outlineColor = c; Refresh(); });
        }

        isReady = true;
        RegisterListeners();
        RefreshDisplay();
    }

    public override void RefreshDisplay()
    {
        base.RefreshDisplay();
        if (!isReady || outlineData == null) return;
        UpdateLabels();
        UpdatePreview();
    }

    private void SetupSlider(Slider s, float min, float max, float val)
    {
        if (s == null) return;
        s.onValueChanged.RemoveAllListeners();
        s.minValue = min;
        s.maxValue = max;
        s.value = Mathf.Clamp(val, min, max);
    }

    private void RegisterListeners()
    {
        if (sliderColorThreshold != null) sliderColorThreshold.onValueChanged.AddListener(v => { outlineData.colorThreshold = v; Refresh(); });
        if (sliderNormalThreshold != null) sliderNormalThreshold.onValueChanged.AddListener(v => { outlineData.normalThreshold = v; Refresh(); });
    }

    private void Refresh()
    {
        UpdateLabels();
        UpdatePreview();
    }

    private void UpdateLabels()
    {
        if (colorThresholdText) colorThresholdText.text = $"{outlineData.colorThreshold:F2}";
        if (normalThresholdText) normalThresholdText.text = $"{outlineData.normalThreshold:F2}";
    }

    private void UpdatePreview()
    {
        if (outlineData?.runtimeMaterial != null)
            outlineData.ApplyToMaterial(outlineData.runtimeMaterial);
    }
}
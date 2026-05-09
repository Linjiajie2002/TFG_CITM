using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpotLightClipInspectorPanel : ClipInspectorPanel
{
    private static readonly int ID_GlobalAlpha = Shader.PropertyToID("_Global_Alpha");
    private static readonly int ID_BreathSpeed = Shader.PropertyToID("_Breath_Speed");
    private static readonly int ID_ColorTop = Shader.PropertyToID("_Color_Top");
    private static readonly int ID_ColorBottom = Shader.PropertyToID("_Color_Bottom");

    [Header("=== Position ===")]
    public Slider sliderPosX, sliderPosY, sliderPosZ;
    public TextMeshProUGUI posXText, posYText, posZText;

    [Header("=== Rotation ===")]
    public Slider sliderRotX, sliderRotY, sliderRotZ;
    public TextMeshProUGUI rotXText, rotYText, rotZText;

    [Header("=== Scale ===")]
    public Slider sliderScaleX, sliderScaleY, sliderScaleZ;
    public TextMeshProUGUI scaleXText, scaleYText, scaleZText;

    [Header("=== Rotation Animation ===")]
    public Toggle toggleRotation;
    public GameObject rotSpeedRow;
    public Slider sliderRotSpeed;
    public TextMeshProUGUI rotSpeedText;

    [Header("=== Shader ===")]
    public Slider sliderAlpha;
    public TextMeshProUGUI alphaText;
    public Slider sliderBreathSpeed;
    public TextMeshProUGUI breathSpeedText;

    [Header("=== Colors ===")]
    public ColorPickerPanel colorPickerTop;    // → _Color_Top
    public ColorPickerPanel colorPickerBottom; // → _Color_Bottom

    private SpotLightClipData spotData;
    private bool isReady = false;

    protected override void Awake() => base.Awake();

    public override void BindClip(TimelineEventData clip, TimelineManager mgr)
    {
        isReady = false;
        base.BindClip(clip, mgr);

        spotData = clip.customData is SpotLightClipData ex ? ex : new SpotLightClipData();
        clip.customData = spotData;

        SetupSlider(sliderPosX, spotData.posXMin, spotData.posXMax, spotData.posX);
        SetupSlider(sliderPosY, spotData.posYMin, spotData.posYMax, spotData.posY);
        SetupSlider(sliderPosZ, spotData.posZMin, spotData.posZMax, spotData.posZ);
        SetupSlider(sliderRotX, spotData.rotMin, spotData.rotMax, spotData.rotX);
        SetupSlider(sliderRotY, spotData.rotMin, spotData.rotMax, spotData.rotY);
        SetupSlider(sliderRotZ, spotData.rotMin, spotData.rotMax, spotData.rotZ);
        SetupSlider(sliderScaleX, spotData.scaleMin, spotData.scaleMax, spotData.scaleX);
        SetupSlider(sliderScaleY, spotData.scaleMin, spotData.scaleMax, spotData.scaleY);
        SetupSlider(sliderScaleZ, spotData.scaleMin, spotData.scaleMax, spotData.scaleZ);
        SetupSlider(sliderRotSpeed, spotData.rotSpeedMin, spotData.rotSpeedMax, spotData.rotationSpeed);
        SetupSlider(sliderAlpha, spotData.alphaMin, spotData.alphaMax, spotData.alpha);
        SetupSlider(sliderBreathSpeed, spotData.breathSpeedMin, spotData.breathSpeedMax, spotData.breathSpeed);

        if (toggleRotation != null)
        {
            toggleRotation.onValueChanged.RemoveAllListeners();
            toggleRotation.isOn = spotData.isRotating;
            SetRotSpeedRowVisible(spotData.isRotating);
            toggleRotation.onValueChanged.AddListener(v =>
            {
                spotData.isRotating = v;
                SetRotSpeedRowVisible(v);
                Refresh();
            });
        }

        if (colorPickerTop != null)
        {
            colorPickerTop.onColorChanged.RemoveAllListeners();
            colorPickerTop.SetColor(spotData.colorTop, notify: false);
            colorPickerTop.onColorChanged.AddListener(c => { spotData.colorTop = c; Refresh(); });
        }

        if (colorPickerBottom != null)
        {
            colorPickerBottom.onColorChanged.RemoveAllListeners();
            colorPickerBottom.SetColor(spotData.colorBottom, notify: false);
            colorPickerBottom.onColorChanged.AddListener(c => { spotData.colorBottom = c; Refresh(); });
        }

        isReady = true;
        RegisterListeners();
        RefreshDisplay();
    }

    public override void RefreshDisplay()
    {
        base.RefreshDisplay();
        if (!isReady || spotData == null) return;
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
        if (sliderPosX != null) sliderPosX.onValueChanged.AddListener(v => { spotData.posX = v; Refresh(); });
        if (sliderPosY != null) sliderPosY.onValueChanged.AddListener(v => { spotData.posY = v; Refresh(); });
        if (sliderPosZ != null) sliderPosZ.onValueChanged.AddListener(v => { spotData.posZ = v; Refresh(); });
        if (sliderRotX != null) sliderRotX.onValueChanged.AddListener(v => { spotData.rotX = v; Refresh(); });
        if (sliderRotY != null) sliderRotY.onValueChanged.AddListener(v => { spotData.rotY = v; Refresh(); });
        if (sliderRotZ != null) sliderRotZ.onValueChanged.AddListener(v => { spotData.rotZ = v; Refresh(); });
        if (sliderScaleX != null) sliderScaleX.onValueChanged.AddListener(v => { spotData.scaleX = v; Refresh(); });
        if (sliderScaleY != null) sliderScaleY.onValueChanged.AddListener(v => { spotData.scaleY = v; Refresh(); });
        if (sliderScaleZ != null) sliderScaleZ.onValueChanged.AddListener(v => { spotData.scaleZ = v; Refresh(); });
        if (sliderRotSpeed != null) sliderRotSpeed.onValueChanged.AddListener(v => { spotData.rotationSpeed = v; Refresh(); });
        if (sliderAlpha != null) sliderAlpha.onValueChanged.AddListener(v => { spotData.alpha = v; Refresh(); });
        if (sliderBreathSpeed != null) sliderBreathSpeed.onValueChanged.AddListener(v => { spotData.breathSpeed = v; Refresh(); });
    }

    private void Refresh()
    {
        UpdateLabels();
        UpdatePreview();
    }

    private void UpdateLabels()
    {
        if (posXText) posXText.text = $"{spotData.posX:F1}";
        if (posYText) posYText.text = $"{spotData.posY:F1}";
        if (posZText) posZText.text = $"{spotData.posZ:F1}";
        if (rotXText) rotXText.text = $"{spotData.rotX:F0}°";
        if (rotYText) rotYText.text = $"{spotData.rotY:F0}°";
        if (rotZText) rotZText.text = $"{spotData.rotZ:F0}°";
        if (scaleXText) scaleXText.text = $"{spotData.scaleX:F2}";
        if (scaleYText) scaleYText.text = $"{spotData.scaleY:F2}";
        if (scaleZText) scaleZText.text = $"{spotData.scaleZ:F2}";
        if (rotSpeedText) rotSpeedText.text = $"{spotData.rotationSpeed:F0}°/s";
        if (alphaText) alphaText.text = $"{spotData.alpha:F2}";
        if (breathSpeedText) breathSpeedText.text = $"{spotData.breathSpeed:F2}";
    }

    private void UpdatePreview()
    {
        if (spotData?.runtimeInstance == null) return;

        // Transform 实时预览
        spotData.runtimeInstance.transform.position = spotData.Position;
        spotData.runtimeInstance.transform.localScale = spotData.Scale;
        spotData.runtimeInstance.transform.rotation = spotData.Rotation;

        // Shader 实时预览（通过独立材质实例）
        if (spotData.runtimeMaterial != null)
        {
            spotData.runtimeMaterial.SetFloat(ID_GlobalAlpha, spotData.alpha);
            spotData.runtimeMaterial.SetFloat(ID_BreathSpeed, spotData.breathSpeed);
            spotData.runtimeMaterial.SetColor(ID_ColorTop, spotData.colorTop);
            spotData.runtimeMaterial.SetColor(ID_ColorBottom, spotData.colorBottom);
        }

        // 物理 Light 实时预览
        Light lt = spotData.runtimeInstance.GetComponentInChildren<Light>();
        if (lt != null)
        {
            lt.range = spotData.range;
            lt.color = spotData.colorTop;
            lt.intensity = spotData.alpha * 5f;
        }
    }

    private void SetRotSpeedRowVisible(bool v)
    {
        if (rotSpeedRow != null) rotSpeedRow.SetActive(v);
    }
}
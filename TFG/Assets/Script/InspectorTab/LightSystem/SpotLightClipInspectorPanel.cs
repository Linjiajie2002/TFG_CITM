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

    [Header("=== Circle Rotation Animation ===")]
    public Toggle toggleRotation;   // 开/关旋转

    public GameObject rotSpeedRow;
    public Slider sliderRotSpeed;
    public TextMeshProUGUI rotSpeedText;

    // circleRadius Slider：开启时显示保存值，关闭时 Slider 回到 0（但数值保留）
    public Slider sliderCircleRadius;
    public TextMeshProUGUI circleRadiusText;

    [Header("=== Shader ===")]
    public Slider sliderAlpha;
    public TextMeshProUGUI alphaText;
    public Slider sliderBreathSpeed;
    public TextMeshProUGUI breathSpeedText;

    [Header("=== Colors ===")]
    public ColorPickerPanel colorPickerTop;
    public ColorPickerPanel colorPickerBottom;

    private SpotLightClipData spotData;
    private bool isReady = false;
    private float savedCircleRadius = 0f; // 关闭时暂存 circleRadius

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

        // circleRadius：开启时显示保存值，关闭时显示 0
        float radiusDisplay = spotData.isRotating ? spotData.circleRadius : 0f;
        SetupSlider(sliderCircleRadius, spotData.circleRadiusMin, spotData.circleRadiusMax, radiusDisplay);

        // Toggle
        if (toggleRotation != null)
        {
            toggleRotation.onValueChanged.RemoveAllListeners();
            toggleRotation.isOn = spotData.isRotating;
            toggleRotation.onValueChanged.AddListener(OnToggleChanged);
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

    // ==========================================
    // Toggle 回调
    // ==========================================
    private void OnToggleChanged(bool isOn)
    {
        if (!isReady || spotData == null) return;
        spotData.isRotating = isOn;

        if (isOn)
        {
            // 开启：从 savedCircleRadius 恢复值
            spotData.circleRadius = savedCircleRadius;
            SetupSlider(sliderCircleRadius, spotData.circleRadiusMin, spotData.circleRadiusMax, spotData.circleRadius);
            if (sliderCircleRadius != null)
                sliderCircleRadius.onValueChanged.AddListener(v => { spotData.circleRadius = v; Refresh(); });
        }
        else
        {
            // 关闭：把当前值存到 savedCircleRadius，然后把 circleRadius 和 Slider 都归零
            savedCircleRadius = sliderCircleRadius != null ? sliderCircleRadius.value : spotData.circleRadius;
            spotData.circleRadius = 0f;

            if (sliderCircleRadius != null)
            {
                sliderCircleRadius.onValueChanged.RemoveAllListeners();
                sliderCircleRadius.value = 0f;
                sliderCircleRadius.interactable = false;
            }
        }

        SetRotationRowsVisible(isOn);
    }

    private void SetRotationRowsVisible(bool v)
    {
        if (rotSpeedRow != null) rotSpeedRow.SetActive(v);
        // circleRadius 行跟随显隐（如果你有单独的行容器可以在这里控制，没有就删这行）
        if (sliderCircleRadius != null) sliderCircleRadius.interactable = v;
    }

    public override void RefreshDisplay()
    {
        base.RefreshDisplay();
        if (!isReady || spotData == null) return;
        if (toggleRotation != null) toggleRotation.isOn = spotData.isRotating;
        SetRotationRowsVisible(spotData.isRotating);
        UpdateLabels();
        UpdatePreview();
    }

    private void SetupSlider(Slider s, float min, float max, float val)
    {
        if (s == null) return;
        s.onValueChanged.RemoveAllListeners();
        s.interactable = true;
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

        // circleRadius 只在开启时注册（关闭时由 OnToggleChanged 处理）
        if (sliderCircleRadius != null && spotData.isRotating)
            sliderCircleRadius.onValueChanged.AddListener(v => { spotData.circleRadius = v; Refresh(); });
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
        // 关闭时显示 0，开启时显示实际保存值
        if (circleRadiusText) circleRadiusText.text = spotData.isRotating ? $"{spotData.circleRadius:F1}°" : "0°";
    }

    private void UpdatePreview()
    {
        if (spotData?.runtimeInstance == null) return;

        spotData.runtimeInstance.transform.position = spotData.Position;
        spotData.runtimeInstance.transform.localScale = spotData.Scale;
        spotData.runtimeInstance.transform.rotation = spotData.Rotation;

        if (spotData.runtimeMaterial != null)
        {
            spotData.runtimeMaterial.SetFloat(ID_GlobalAlpha, spotData.alpha);
            spotData.runtimeMaterial.SetFloat(ID_BreathSpeed, spotData.breathSpeed);
            spotData.runtimeMaterial.SetColor(ID_ColorTop, spotData.colorTop);
            spotData.runtimeMaterial.SetColor(ID_ColorBottom, spotData.colorBottom);
        }

        Light lt = spotData.runtimeInstance.GetComponentInChildren<Light>();
        if (lt != null)
        {
            lt.range = spotData.range;
            lt.color = spotData.colorTop;
            lt.intensity = spotData.alpha * 5f;
        }
    }
}
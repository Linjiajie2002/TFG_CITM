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
    public Button btnToggleRotation;
    public TextMeshProUGUI btnRotationLabel;

    public GameObject circleRadiusRow;
    public Slider sliderCircleRadius;
    public TextMeshProUGUI circleRadiusText;

    public GameObject rotSpeedRow;
    public Slider sliderRotSpeed;
    public TextMeshProUGUI rotSpeedText;

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

    protected override void Awake() => base.Awake();

    // ==========================================
    // 每帧：旋转中层 Empty 的 RotY
    // ==========================================
    void Update()
    {
        if (!isReady || spotData == null) return;
        if (!spotData.isRotating) return;
        if (spotData.runtimeMiddleEmpty == null) return;

        float spinY = (Time.time * spotData.rotationSpeed) % 360f;
        spotData.runtimeMiddleEmpty.localEulerAngles = new Vector3(0f, spinY, spotData.circleRadius);
    }

    // ==========================================
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
        SetupSlider(sliderCircleRadius, spotData.circleRadiusMin, spotData.circleRadiusMax, spotData.circleRadius);

        if (btnToggleRotation != null)
        {
            btnToggleRotation.onClick.RemoveAllListeners();
            btnToggleRotation.onClick.AddListener(OnToggleRotationClicked);
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
    // 按钮：开/停旋转
    // ==========================================
    private void OnToggleRotationClicked()
    {
        if (spotData == null) return;
        spotData.isRotating = !spotData.isRotating;

        // 停止时把中层 RotY 归零
        if (!spotData.isRotating && spotData.runtimeMiddleEmpty != null)
            spotData.runtimeMiddleEmpty.localEulerAngles = new Vector3(0f, 0f, spotData.circleRadius);

        SetRotationRowsVisible(spotData.isRotating);
        UpdateRotationButtonLabel();
    }

    private void UpdateRotationButtonLabel()
    {
        if (btnRotationLabel == null) return;
        btnRotationLabel.text = spotData.isRotating ? "停止旋转" : "开始旋转";
    }

    private void SetRotationRowsVisible(bool v)
    {
        if (rotSpeedRow != null) rotSpeedRow.SetActive(v);
        if (circleRadiusRow != null) circleRadiusRow.SetActive(v);
    }

    // ==========================================
    public override void RefreshDisplay()
    {
        base.RefreshDisplay();
        if (!isReady || spotData == null) return;
        SetRotationRowsVisible(spotData.isRotating);
        UpdateRotationButtonLabel();
        UpdateLabels();
        UpdatePreview();
    }

    // ==========================================
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
        if (sliderCircleRadius != null) sliderCircleRadius.onValueChanged.AddListener(v => { spotData.circleRadius = v; Refresh(); });
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
        if (circleRadiusText) circleRadiusText.text = $"{spotData.circleRadius:F1}°";
    }

    private void UpdatePreview()
    {
        if (spotData?.runtimeInstance == null) return;

        // 外层：位置 / 缩放 / 静态朝向
        spotData.runtimeInstance.transform.position = spotData.Position;
        spotData.runtimeInstance.transform.localScale = spotData.Scale;
        spotData.runtimeInstance.transform.rotation = spotData.Rotation;

        // 中层：停止时只写半径，旋转中由 Update() 负责不覆盖
        if (!spotData.isRotating && spotData.runtimeMiddleEmpty != null)
            spotData.runtimeMiddleEmpty.localEulerAngles = new Vector3(0f, 0f, spotData.circleRadius);

        // Shader
        if (spotData.runtimeMaterial != null)
        {
            spotData.runtimeMaterial.SetFloat(ID_GlobalAlpha, spotData.alpha);
            spotData.runtimeMaterial.SetFloat(ID_BreathSpeed, spotData.breathSpeed);
            spotData.runtimeMaterial.SetColor(ID_ColorTop, spotData.colorTop);
            spotData.runtimeMaterial.SetColor(ID_ColorBottom, spotData.colorBottom);
        }

        // 物理 Light
        Light lt = spotData.runtimeInstance.GetComponentInChildren<Light>();
        if (lt != null)
        {
            lt.range = spotData.range;
            lt.color = spotData.colorTop;
            lt.intensity = spotData.alpha * 5f;
        }
    }
}
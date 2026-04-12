using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// 硬切摄像头的专属 Inspector 面板
// 继承 ClipInspectorPanel，添加 6 个 Slider + 预览图标
//
// 预制体结构：
//   ClipPanel_Camera (挂此脚本)
//   ├── Header (可选，含 clipNameText、deleteButton、backButton)
//   ├── TimeInfo (含 startTimeText、durationText、endTimeText)
//   ├── Section_Position
//   │   ├── Row_X → [Label "X"] [Slider sliderPosX] [Label posXValueText]
//   │   ├── Row_Y → [Label "Y"] [Slider sliderPosY] [Label posYValueText]
//   │   └── Row_Z → [Label "Z"] [Slider sliderPosZ] [Label posZValueText]
//   ├── Section_Rotation
//   │   ├── Row_X → [Label "X"] [Slider sliderRotX] [Label rotXValueText]
//   │   ├── Row_Y → [Label "Y"] [Slider sliderRotY] [Label rotYValueText]
//   │   └── Row_Z → [Label "Z"] [Slider sliderRotZ] [Label rotZValueText]
//   └── PreviewArea
//       ├── PreviewBg (Image，深色背景)
//       ├── GridLines (Image，可选的十字线)
//       └── CameraIcon (Image，摄像头位置的小圆点/图标)
// ==========================================
public class CameraClipInspectorPanel : ClipInspectorPanel
{
    // ---------- 位置 Slider ----------
    [Header("=== Position Sliders ===")]
    public Slider sliderPosX;
    public Slider sliderPosY;
    public Slider sliderPosZ;
    public TextMeshProUGUI posXValueText;
    public TextMeshProUGUI posYValueText;
    public TextMeshProUGUI posZValueText;

    // ---------- Slider 范围（在 Unity Inspector 里配置，出厂默认值） ----------
    [Header("=== Position Slider Ranges ===")]
    public float posXMin = -20f; public float posXMax = 20f;
    public float posYMin = 0f; public float posYMax = 15f;
    public float posZMin = -20f; public float posZMax = 5f;

    // ---------- 旋转 Slider ----------
    [Header("=== Rotation Sliders ===")]
    public Slider sliderRotX;
    public Slider sliderRotY;
    public Slider sliderRotZ;
    public TextMeshProUGUI rotXValueText;
    public TextMeshProUGUI rotYValueText;
    public TextMeshProUGUI rotZValueText;

    // ---------- 预览图标 ----------
    [Header("=== Preview Area ===")]
    [Tooltip("预览区域的背景 RectTransform，用于计算 icon 位置")]
    public RectTransform previewArea;
    [Tooltip("小圆点/摄像头 icon，代表摄像头在 XZ 平面上的位置")]
    public RectTransform cameraIcon;
    [Tooltip("演出时用于预览的摄像头（Edit 用，只在 Edit 模式下更新位置）")]
    public Camera previewCamera;

    // 本地缓存数据
    private CameraClipData camData;
    private bool isReady = false;

    // ==========================================
    protected override void Awake()
    {
        base.Awake();
    }

    // ==========================================
    // 绑定 Clip 数据，初始化 Slider 范围和监听
    // ==========================================
    public override void BindClip(TimelineEventData clip, TimelineManager mgr)
    {
        base.BindClip(clip, mgr);

        // 从 customData 取出或新建 CameraClipData
        if (clip.customData is CameraClipData existing)
        {
            camData = existing;
            // 同步 range 设置到 data
            SyncRangesToData();
        }
        else
        {
            camData = new CameraClipData
            {
                posX = -0.3f,
                posY = 0.9f,
                posZ = -6.9f,
                rotX = 1.5f,
                rotY = 171.7f,
                rotZ = 0f,
                posXMin = posXMin,
                posXMax = posXMax,
                posYMin = posYMin,
                posYMax = posYMax,
                posZMin = posZMin,
                posZMax = posZMax
            };
            clip.customData = camData;
        }

        // 初始化 Slider 范围
        SetupSlider(sliderPosX, posXMin, posXMax, camData.posX);
        SetupSlider(sliderPosY, posYMin, posYMax, camData.posY);
        SetupSlider(sliderPosZ, posZMin, posZMax, camData.posZ);
        SetupSlider(sliderRotX, 0f, 359f, camData.rotX);
        SetupSlider(sliderRotY, 0f, 359f, camData.rotY);
        SetupSlider(sliderRotZ, 0f, 359f, camData.rotZ);

        isReady = true;

        // 注册监听（设置完范围和值后再注册，避免触发回调）
        RegisterListeners();

        RefreshDisplay();
        UpdatePreviewIcon();
    }

    // ==========================================
    // 刷新时间信息（每帧由 TimelineManager 调用）
    // ==========================================
    public override void RefreshDisplay()
    {
        base.RefreshDisplay();
        if (!isReady || camData == null) return;
        UpdateValueLabels();
    }

    // ==========================================
    // Slider 工具
    // ==========================================
    private void SetupSlider(Slider s, float min, float max, float value)
    {
        if (s == null) return;
        s.onValueChanged.RemoveAllListeners();
        s.minValue = min;
        s.maxValue = max;
        s.value = Mathf.Clamp(value, min, max);
    }

    private void RegisterListeners()
    {
        if (sliderPosX != null) sliderPosX.onValueChanged.AddListener(v => { camData.posX = v; OnDataChanged(); });
        if (sliderPosY != null) sliderPosY.onValueChanged.AddListener(v => { camData.posY = v; OnDataChanged(); });
        if (sliderPosZ != null) sliderPosZ.onValueChanged.AddListener(v => { camData.posZ = v; OnDataChanged(); });
        if (sliderRotX != null) sliderRotX.onValueChanged.AddListener(v => { camData.rotX = v; OnDataChanged(); });
        if (sliderRotY != null) sliderRotY.onValueChanged.AddListener(v => { camData.rotY = v; OnDataChanged(); });
        if (sliderRotZ != null) sliderRotZ.onValueChanged.AddListener(v => { camData.rotZ = v; OnDataChanged(); });
    }

    // Slider 变化时统一调用
    private void OnDataChanged()
    {
        UpdateValueLabels();
        UpdatePreviewIcon();
        UpdatePreviewCamera();
    }

    private void UpdateValueLabels()
    {
        if (camData == null) return;
        if (posXValueText != null) posXValueText.text = $"{camData.posX:F1}";
        if (posYValueText != null) posYValueText.text = $"{camData.posY:F1}";
        if (posZValueText != null) posZValueText.text = $"{camData.posZ:F1}";
        if (rotXValueText != null) rotXValueText.text = $"{camData.rotX:F0}°";
        if (rotYValueText != null) rotYValueText.text = $"{camData.rotY:F0}°";
        if (rotZValueText != null) rotZValueText.text = $"{camData.rotZ:F0}°";
    }

    // ==========================================
    // 预览图标：根据 X/Z 值更新圆点在预览区内的位置（俯视图）
    // ==========================================
    private void UpdatePreviewIcon()
    {
        if (previewArea == null || cameraIcon == null || camData == null) return;

        float w = previewArea.rect.width;
        float h = previewArea.rect.height;

        // 把 posX、posZ 映射到预览区 [0,1] 范围
        float normX = Mathf.InverseLerp(posXMin, posXMax, camData.posX);
        float normZ = Mathf.InverseLerp(posZMin, posZMax, camData.posZ);

        // 转为本地坐标（左下角为原点）
        float localX = normX * w - w * 0.5f;
        float localZ = normZ * h - h * 0.5f;   // Z 轴对应纵方向

        cameraIcon.anchoredPosition = new Vector2(localX, localZ);

        // 用旋转 Y 轴转动图标方向（让用户看到摄像头朝向）
        cameraIcon.localRotation = Quaternion.Euler(0f, 0f, -camData.rotY);
    }

    // ==========================================
    // Edit 模式预览：把 previewCamera 移到当前参数位置
    // ==========================================
    private void UpdatePreviewCamera()
    {
        if (previewCamera == null || camData == null) return;
        previewCamera.transform.position = camData.Position;
        previewCamera.transform.rotation = camData.Rotation;
    }

    // ==========================================
    private void SyncRangesToData()
    {
        if (camData == null) return;
        camData.posXMin = posXMin; camData.posXMax = posXMax;
        camData.posYMin = posYMin; camData.posYMax = posYMax;
        camData.posZMin = posZMin; camData.posZMax = posZMax;
    }
}
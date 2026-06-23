using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// Point Light Clip 的专属 Inspector 面板
// 继承 ClipInspectorPanel
//
// 预制体结构（ClipPanel_PointLight）：
//   ├── Header
//   │   ├── clipNameText
//   │   ├── deleteButton
//   │   └── backButton
//   ├── TimeInfo
//   │   ├── startTimeText
//   │   ├── durationText
//   │   └── endTimeText
//   ├── Section_Position
//   │   ├── Row_X → Slider(sliderPosX) + ValueText(posXValueText)
//   │   ├── Row_Y → Slider(sliderPosY) + ValueText(posYValueText)
//   │   └── Row_Z → Slider(sliderPosZ) + ValueText(posZValueText)
//   ├── Section_Light
//   │   ├── Row_Intensity → Slider(sliderIntensity) + ValueText(intensityValueText)
//   │   └── Row_Range     → Slider(sliderRange)     + ValueText(rangeValueText)
//   ├── Section_Color
//   │   └── ColorPickerPanel (挂 ColorPickerPanel.cs)
//   └── Overlap_Warning (Image + TextMeshProUGUI，默认隐藏，超出3个灯时显示)
// ==========================================
public class PointLightClipInspectorPanel : ClipInspectorPanel
{
    [Header("=== Position Sliders ===")]
    public Slider sliderPosX;
    public Slider sliderPosY;
    public Slider sliderPosZ;
    public TextMeshProUGUI posXValueText;
    public TextMeshProUGUI posYValueText;
    public TextMeshProUGUI posZValueText;

    [Header("=== Position Slider 范围 ===")]
    public float posXMin = -20f; public float posXMax = 20f;
    public float posYMin = 0f; public float posYMax = 15f;
    public float posZMin = -20f; public float posZMax = 20f;

    [Header("=== Light 参数 ===")]
    public Slider sliderIntensity;
    public Slider sliderRange;
    public TextMeshProUGUI intensityValueText;
    public TextMeshProUGUI rangeValueText;

    [Header("=== Light 参数范围 ===")]
    public float intensityMin = 0f; public float intensityMax = 10f;
    public float rangeMin = 1f; public float rangeMax = 30f;

    [Header("=== Color Picker ===")]
    public ColorPickerPanel colorPicker;

    [Header("=== 灯光替身 (3D Dummy) ===")]
    public GameObject lightDummyPrefab;
    public Color selectedColor = new Color(1f, 0.5f, 0f, 1f);
    private GameObject myDummyInstance;
    private MeshRenderer[] dummyRenderers;
    private Color[] originalColors;

    [Header("=== 灯光播放系统引用 ===")]
    public LightPlaybackSystem lightPlaybackSystem;

    // 本地缓存数据
    private PointLightClipData lightData;
    private bool isReady = false;

    protected override void Awake()
    {
        base.Awake();
    }

    private void OnEnable()
    {
        if (dummyRenderers != null)
            foreach (var r in dummyRenderers) r.material.color = selectedColor;
    }

    private void OnDisable()
    {
        if (dummyRenderers != null)
            for (int i = 0; i < dummyRenderers.Length; i++)
                dummyRenderers[i].material.color = originalColors[i];
    }

    // ==========================================
    public override void BindClip(TimelineEventData clip, TimelineManager mgr)
    {
        base.BindClip(clip, mgr);

        if (clip.customData is PointLightClipData existing)
        {
            lightData = existing;
            SyncRangesToData();
        }
        else
        {
            lightData = new PointLightClipData
            {
                posX = 0f,
                posY = 3f,
                posZ = 0f,
                color = Color.white,
                intensity = 25f,
                range = 10f,
                posXMin = posXMin,
                posXMax = posXMax,
                posYMin = posYMin,
                posYMax = posYMax,
                posZMin = posZMin,
                posZMax = posZMax,
                intensityMin = intensityMin,
                intensityMax = intensityMax,
                rangeMin = rangeMin,
                rangeMax = rangeMax
            };
            clip.customData = lightData;
        }

        // 初始化 Slider
        SetupSlider(sliderPosX, posXMin, posXMax, lightData.posX);
        SetupSlider(sliderPosY, posYMin, posYMax, lightData.posY);
        SetupSlider(sliderPosZ, posZMin, posZMax, lightData.posZ);
        SetupSlider(sliderIntensity, intensityMin, intensityMax, lightData.intensity);
        SetupSlider(sliderRange, rangeMin, rangeMax, lightData.range);

        // 颜色选择器
        if (colorPicker != null)
        {
            // 【核心修复】：先清空之前的监听器，防止多点几次后发生事件叠加
            colorPicker.onColorChanged.RemoveAllListeners();

            // 把当前方块的数据传给拾色器
            colorPicker.SetColor(lightData.color, notify: false);

            // 重新绑定监听
            colorPicker.onColorChanged.AddListener(OnColorChanged);
        }

        // 克隆 3D 替身
        if (myDummyInstance == null && lightDummyPrefab != null)
        {
            myDummyInstance = Instantiate(lightDummyPrefab);
            dummyRenderers = myDummyInstance.GetComponentsInChildren<MeshRenderer>();
            originalColors = new Color[dummyRenderers.Length];
            for (int i = 0; i < dummyRenderers.Length; i++)
                originalColors[i] = dummyRenderers[i].material.color;
            SetLayerRecursively(myDummyInstance, LayerMask.NameToLayer("EditorOnly"));
        }

        isReady = true;
        RegisterListeners();
        RefreshDisplay();
        UpdateVisuals();
    }

    // ==========================================
    public override void RefreshDisplay()
    {
        base.RefreshDisplay();
        if (!isReady || lightData == null) return;

        UpdateValueLabels();
        UpdateLightPreview();
        UpdateVisuals();
    }

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
        if (sliderPosX != null) sliderPosX.onValueChanged.AddListener(v => { lightData.posX = v; OnDataChanged(); });
        if (sliderPosY != null) sliderPosY.onValueChanged.AddListener(v => { lightData.posY = v; OnDataChanged(); });
        if (sliderPosZ != null) sliderPosZ.onValueChanged.AddListener(v => { lightData.posZ = v; OnDataChanged(); });
        if (sliderIntensity != null) sliderIntensity.onValueChanged.AddListener(v => { lightData.intensity = v; OnDataChanged(); });
        if (sliderRange != null) sliderRange.onValueChanged.AddListener(v => { lightData.range = v; OnDataChanged(); });
    }

    private void OnColorChanged(Color c)
    {
        if (lightData == null) return;
        lightData.color = c;
        OnDataChanged();
    }

    private void OnDataChanged()
    {
        UpdateValueLabels();
        UpdateLightPreview();
        UpdateVisuals();
    }

    private void UpdateValueLabels()
    {
        if (lightData == null) return;
        if (posXValueText != null) posXValueText.text = $"{lightData.posX:F1}";
        if (posYValueText != null) posYValueText.text = $"{lightData.posY:F1}";
        if (posZValueText != null) posZValueText.text = $"{lightData.posZ:F1}";
        if (intensityValueText != null) intensityValueText.text = $"{lightData.intensity:F2}";
        if (rangeValueText != null) rangeValueText.text = $"{lightData.range:F1}";
    }

    // 实时更新场景里关联的 Light（如果已创建）
    private void UpdateLightPreview()
    {
        if (lightData?.runtimeLight == null) return;
        lightData.runtimeLight.transform.position = lightData.Position;
        lightData.runtimeLight.color = lightData.color;
        lightData.runtimeLight.intensity = lightData.intensity;
        lightData.runtimeLight.range = lightData.range;
    }

    private void SyncRangesToData()
    {
        if (lightData == null) return;
        lightData.posXMin = posXMin; lightData.posXMax = posXMax;
        lightData.posYMin = posYMin; lightData.posYMax = posYMax;
        lightData.posZMin = posZMin; lightData.posZMax = posZMax;
        lightData.intensityMin = intensityMin; lightData.intensityMax = intensityMax;
        lightData.rangeMin = rangeMin; lightData.rangeMax = rangeMax;
    }

    // 把 Dummy 移到灯光当前的坐标
    private void UpdateVisuals()
    {
        if (myDummyInstance != null && lightData != null)
            myDummyInstance.transform.position = lightData.Position;
    }

    private void OnDestroy()
    {
        if (myDummyInstance != null) Destroy(myDummyInstance);
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }
}
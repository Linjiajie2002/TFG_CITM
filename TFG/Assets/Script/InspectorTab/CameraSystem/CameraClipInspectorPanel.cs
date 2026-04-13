using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// 硬切摄像头的专属 Inspector 面板 (纯净回归版)
// 已移除所有反向修正，回归标准世界坐标逻辑
// ==========================================
public class CameraClipInspectorPanel : ClipInspectorPanel
{
    [Header("=== 机位模型管理 (3D替身) ===")]
    public GameObject cameraDummyPrefab;
    private GameObject myDummyInstance;
    private MeshRenderer dummyRenderer;
    private Color originalColor;
    public Color selectedColor = new Color(1f, 0.5f, 0f, 1f);

    [Header("=== 位置 Sliders ===")]
    public Slider sliderPosX;
    public Slider sliderPosY;
    public Slider sliderPosZ;
    public TextMeshProUGUI posXValueText;
    public TextMeshProUGUI posYValueText;
    public TextMeshProUGUI posZValueText;

    [Header("=== 默认范围 (可根据场景大小调整) ===")]
    public float posXMin = -10f; public float posXMax = 10f;
    public float posYMin = 0f; public float posYMax = 10f;
    public float posZMin = -20f; public float posZMax = 10f;

    [Header("=== 旋转 Sliders ===")]
    public Slider sliderRotX;
    public Slider sliderRotY;
    public Slider sliderRotZ;
    public TextMeshProUGUI rotXValueText;
    public TextMeshProUGUI rotYValueText;
    public TextMeshProUGUI rotZValueText;

    [Header("=== 真实预览相机 ===")]
    public Camera previewCamera;

    private CameraClipData camData;
    private bool isReady = false;

    private void OnEnable()
    {
        if (dummyRenderer != null) dummyRenderer.material.color = selectedColor;
        // 选中时，相机立刻跳到该方块保存的位置
        UpdatePreviewCamera();
    }

    private void OnDisable()
    {
        if (dummyRenderer != null) dummyRenderer.material.color = originalColor;
    }

    // ==========================================
    // 绑定逻辑
    // ==========================================
    public override void BindClip(TimelineEventData clip, TimelineManager mgr)
    {
        base.BindClip(clip, mgr);

        if (clip.customData is CameraClipData existing)
        {
            camData = existing;
        }
        else
        {
            // 使用你提供的新坐标作为默认值
            camData = new CameraClipData
            {
                posX = 0.0f,
                posY = 1.0f,
                posZ = -4.8f,
                rotX = 0f,
                rotY = 0f,
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

        // 初始化所有 Slider，直接对应原始数值
        SetupSlider(sliderPosX, posXMin, posXMax, camData.posX);
        SetupSlider(sliderPosY, posYMin, posYMax, camData.posY);
        SetupSlider(sliderPosZ, posZMin, posZMax, camData.posZ);
        SetupSlider(sliderRotX, -180f, 180f, camData.rotX);
        SetupSlider(sliderRotY, -180f, 180f, camData.rotY);
        SetupSlider(sliderRotZ, -180f, 180f, camData.rotZ);

        // 克隆 3D 替身
        if (myDummyInstance == null && cameraDummyPrefab != null)
        {
            myDummyInstance = Instantiate(cameraDummyPrefab);
            dummyRenderer = myDummyInstance.GetComponentInChildren<MeshRenderer>();
            if (dummyRenderer != null) originalColor = dummyRenderer.material.color;
            SetLayerRecursively(myDummyInstance, LayerMask.NameToLayer("EditorOnly"));
        }

        isReady = true;
        RegisterListeners();
        RefreshDisplay();
        UpdateVisuals();
    }

    private void RegisterListeners()
    {
        // 纯净绑定：滑块是什么，数据就是什么
        if (sliderPosX != null) sliderPosX.onValueChanged.AddListener(v => { camData.posX = v; OnDataChanged(); });
        if (sliderPosY != null) sliderPosY.onValueChanged.AddListener(v => { camData.posY = v; OnDataChanged(); });
        if (sliderPosZ != null) sliderPosZ.onValueChanged.AddListener(v => { camData.posZ = v; OnDataChanged(); });
        if (sliderRotX != null) sliderRotX.onValueChanged.AddListener(v => { camData.rotX = v; OnDataChanged(); });
        if (sliderRotY != null) sliderRotY.onValueChanged.AddListener(v => { camData.rotY = v; OnDataChanged(); });
        if (sliderRotZ != null) sliderRotZ.onValueChanged.AddListener(v => { camData.rotZ = v; OnDataChanged(); });
    }

    private void OnDataChanged()
    {
        UpdateValueLabels();
        UpdateVisuals();
        UpdatePreviewCamera(); // 这就是为什么相机被“固定”的原因：滑块在控轴
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

    private void UpdateVisuals()
    {
        if (myDummyInstance != null && camData != null)
        {
            myDummyInstance.transform.position = camData.Position;
            myDummyInstance.transform.rotation = camData.Rotation;
        }
    }

    private void UpdatePreviewCamera()
    {
        if (previewCamera != null && camData != null)
        {
            previewCamera.transform.position = camData.Position;
            previewCamera.transform.rotation = camData.Rotation;
        }
    }

    private void SetupSlider(Slider s, float min, float max, float value)
    {
        if (s == null) return;
        s.onValueChanged.RemoveAllListeners();
        s.minValue = min;
        s.maxValue = max;
        s.value = value;
    }

    private void OnDestroy() { if (myDummyInstance != null) Destroy(myDummyInstance); }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }

    // ==========================================
    // 💡 给你的额外礼物：右键点击组件，可以把场景里的相机位置“吸”进数据里
    // 这样你就不用手动调滑块了，在场景里摆好相机，点一下就行
    // ==========================================
    [ContextMenu("将当前预览相机位置同步到方块")]
    public void SyncFromSceneCamera()
    {
        if (previewCamera == null || camData == null) return;

        camData.posX = previewCamera.transform.position.x;
        camData.posY = previewCamera.transform.position.y;
        camData.posZ = previewCamera.transform.position.z;

        Vector3 rot = previewCamera.transform.eulerAngles;
        // 规范化角度到 -180~180
        camData.rotX = (rot.x > 180) ? rot.x - 360 : rot.x;
        camData.rotY = (rot.y > 180) ? rot.y - 360 : rot.y;
        camData.rotZ = (rot.z > 180) ? rot.z - 360 : rot.z;

        // 刷新一下 UI 显示
        isReady = false; // 暂时关闭监听防止干扰
        SetupSlider(sliderPosX, posXMin, posXMax, camData.posX);
        SetupSlider(sliderPosY, posYMin, posYMax, camData.posY);
        SetupSlider(sliderPosZ, posZMin, posZMax, camData.posZ);
        SetupSlider(sliderRotX, -180f, 180f, camData.rotX);
        SetupSlider(sliderRotY, -180f, 180f, camData.rotY);
        SetupSlider(sliderRotZ, -180f, 180f, camData.rotZ);
        isReady = true;
        RegisterListeners();
        OnDataChanged();
    }
}
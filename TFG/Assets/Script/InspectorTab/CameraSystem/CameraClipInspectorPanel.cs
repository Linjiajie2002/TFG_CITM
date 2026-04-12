using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// 硬切摄像头的专属 Inspector 面板 (真 3D 旗舰版)
// 包含功能：UX防呆反转、自动克隆机位替身、选中变色反馈、自动垃圾回收
// ==========================================
public class CameraClipInspectorPanel : ClipInspectorPanel
{
    [Header("=== UX 修正 (解决反向滑块问题) ===")]
    [Tooltip("勾选后，代码会自动帮你把X轴滑块反转，滑块向右 = 视觉向右")]
    public bool invertXAxis = true;

    [Header("=== 机位模型管理 (3D替身) ===")]
    [Tooltip("这里请拖入你的 3D 相机模型【预制体】")]
    public GameObject cameraDummyPrefab;

    // 内部管理的专属克隆体
    private GameObject myDummyInstance;
    private MeshRenderer dummyRenderer;
    private Color originalColor;
    public Color selectedColor = new Color(1f, 0.5f, 0f, 1f); // 选中时的橘色

    [Header("=== 位置 Sliders ===")]
    public Slider sliderPosX;
    public Slider sliderPosY;
    public Slider sliderPosZ;
    public TextMeshProUGUI posXValueText;
    public TextMeshProUGUI posYValueText;
    public TextMeshProUGUI posZValueText;

    [Header("=== 位置 Slider 范围 ===")]
    public float posXMin = -3.6f; public float posXMax = 3.6f;
    public float posYMin = 0f; public float posYMax = 15f;
    public float posZMin = -20f; public float posZMax = 5f;

    [Header("=== 旋转 Sliders ===")]
    public Slider sliderRotX;
    public Slider sliderRotY;
    public Slider sliderRotZ;
    public TextMeshProUGUI rotXValueText;
    public TextMeshProUGUI rotYValueText;
    public TextMeshProUGUI rotZValueText;

    [Header("=== 真实预览相机 ===")]
    [Tooltip("演出时用于预览的真实摄像头 EditCamera / PreviewCamera")]
    public Camera previewCamera;

    // 本地缓存数据
    private CameraClipData camData;
    private bool isReady = false;

    // ==========================================
    // 生命周期：UI 面板开关时的颜色反馈
    // ==========================================
    private void OnEnable()
    {
        // 面板打开（方块被选中）时，替身变橘色
        if (dummyRenderer != null)
            dummyRenderer.material.color = selectedColor;

        // 每次点开面板，也让主预览相机瞬间归位
        UpdatePreviewCamera();
    }

    private void OnDisable()
    {
        // 面板关闭（方块取消选中）时，替身变回原色
        if (dummyRenderer != null)
            dummyRenderer.material.color = originalColor;
    }

    protected override void Awake()
    {
        base.Awake();
    }

    // ==========================================
    // 核心绑定逻辑
    // ==========================================
    public override void BindClip(TimelineEventData clip, TimelineManager mgr)
    {
        base.BindClip(clip, mgr);

        // 1. 数据装载 (读取或新建)
        if (clip.customData is CameraClipData existing)
        {
            camData = existing;
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

        // 2. 替身模型管理：自动克隆专属 3D 相机模型
        if (myDummyInstance == null && cameraDummyPrefab != null)
        {
            myDummyInstance = Instantiate(cameraDummyPrefab);
            myDummyInstance.name = $"Dummy_Camera_{clip.startTime:F2}s";

            // 获取材质用于变色
            dummyRenderer = myDummyInstance.GetComponentInChildren<MeshRenderer>();
            if (dummyRenderer != null) originalColor = dummyRenderer.material.color;

            // 丢进 EditorOnly 图层，防止演出时穿帮！
            SetLayerRecursively(myDummyInstance, LayerMask.NameToLayer("EditorOnly"));

            // 因为在 Bind 的时候面板通常是打开状态，所以立刻染成橘色
            if (dummyRenderer != null && gameObject.activeInHierarchy)
                dummyRenderer.material.color = selectedColor;
        }

        // 3. 初始化 Slider (包含 X 轴反向的视觉修正)
        float currentX = invertXAxis ? -camData.posX : camData.posX;
        float minX = invertXAxis ? -posXMax : posXMin;
        float maxX = invertXAxis ? -posXMin : posXMax;

        SetupSlider(sliderPosX, minX, maxX, currentX);
        SetupSlider(sliderPosY, posYMin, posYMax, camData.posY);
        SetupSlider(sliderPosZ, posZMin, posZMax, camData.posZ);
        SetupSlider(sliderRotX, 0f, 359f, camData.rotX);
        SetupSlider(sliderRotY, 0f, 359f, camData.rotY);
        SetupSlider(sliderRotZ, 0f, 359f, camData.rotZ);

        isReady = true;
        RegisterListeners();
        RefreshDisplay();
        UpdateVisuals();
    }

    public override void RefreshDisplay()
    {
        base.RefreshDisplay();
        if (!isReady || camData == null) return;
        UpdateValueLabels();
    }

    // ==========================================
    // Slider 与监听器
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
        // 写入数据时：如果 X 轴反向了，存回数据时要翻转为负数
        if (sliderPosX != null) sliderPosX.onValueChanged.AddListener(v => { camData.posX = invertXAxis ? -v : v; OnDataChanged(); });
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
        UpdatePreviewCamera();
    }

    private void UpdateValueLabels()
    {
        if (camData == null) return;

        // 显示数字给用户看时也要骗过去
        float displayX = invertXAxis ? -camData.posX : camData.posX;

        if (posXValueText != null) posXValueText.text = $"{displayX:F1}";
        if (posYValueText != null) posYValueText.text = $"{camData.posY:F1}";
        if (posZValueText != null) posZValueText.text = $"{camData.posZ:F1}";
        if (rotXValueText != null) rotXValueText.text = $"{camData.rotX:F0}°";
        if (rotYValueText != null) rotYValueText.text = $"{camData.rotY:F0}°";
        if (rotZValueText != null) rotZValueText.text = $"{camData.rotZ:F0}°";
    }

    // ==========================================
    // 视觉刷新同步
    // ==========================================
    private void UpdateVisuals()
    {
        // 让场景里的 3D 小相机替身移动
        if (myDummyInstance != null && camData != null)
        {
            myDummyInstance.transform.position = camData.Position;
            myDummyInstance.transform.rotation = camData.Rotation;
        }
    }

    private void UpdatePreviewCamera()
    {
        // 让真正的预览画面跟随移动
        if (previewCamera != null && camData != null)
        {
            previewCamera.transform.position = camData.Position;
            previewCamera.transform.rotation = camData.Rotation;
        }
    }

    private void SyncRangesToData()
    {
        if (camData == null) return;
        camData.posXMin = posXMin; camData.posXMax = posXMax;
        camData.posYMin = posYMin; camData.posYMax = posYMax;
        camData.posZMin = posZMin; camData.posZMax = posZMax;
    }

    // ==========================================
    // 垃圾回收：如果方块被删掉，模型也必须销毁
    // ==========================================
    private void OnDestroy()
    {
        if (myDummyInstance != null)
        {
            Destroy(myDummyInstance);
        }
    }

    // 工具：递归设置物体的 Layer（处理预制体里有多层子物体的情况）
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}
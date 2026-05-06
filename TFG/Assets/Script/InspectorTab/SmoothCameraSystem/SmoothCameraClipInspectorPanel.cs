using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// 平滑移动摄像头的专属 Inspector 面板 (3D模型回归 + 严谨的视角同步)
// ==========================================
public class SmoothCameraClipInspectorPanel : ClipInspectorPanel
{
    [Header("=== Tab 切换按钮 ===")]
    public Button btnCam1;
    public Button btnCam2;
    public Button btnMid;
    public Color tabActiveColor = new Color(0f, 1f, 0.8f, 1f);
    public Color tabInactiveColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Header("=== 中间点开关 ===")]
    public Toggle midPointToggle;

    [Header("=== 核心 6 滑块 ===")]
    public Slider sliderPosX; public Slider sliderPosY; public Slider sliderPosZ;
    public Slider sliderRotX; public Slider sliderRotY; public Slider sliderRotZ;
    public TextMeshProUGUI posXText; public TextMeshProUGUI posYText; public TextMeshProUGUI posZText;
    public TextMeshProUGUI rotXText; public TextMeshProUGUI rotYText; public TextMeshProUGUI rotZText;

    [Header("滑块范围配置")]
    public float posXMin = -20f; public float posXMax = 20f;
    public float posYMin = 0f; public float posYMax = 15f;
    public float posZMin = -20f; public float posZMax = 5f;

    [Header("=== 场景交互引用 ===")]
    public Camera previewCamera;

    [Header("=== 3D 替身模型 (找回丢失的模型) ===")]
    public GameObject cameraDummyPrefab;
    private GameObject startDummy;
    private GameObject endDummy;
    private GameObject midDummy;

    public Color startColor = new Color(0f, 1f, 0f, 0.6f); // 绿
    public Color endColor = new Color(1f, 0f, 0f, 0.6f);   // 红
    public Color midColor = new Color(1f, 1f, 0f, 0.6f);   // 黄

    // 内部变量
    private int selectedTab = 0;
    private bool isReady = false;
    private bool suppressCallbacks = false;
    private SmoothCameraClipData camData;

    private void OnEnable()
    {
        // 选中时显示模型
        if (startDummy != null) startDummy.SetActive(true);
        if (endDummy != null) endDummy.SetActive(true);
        if (midDummy != null && camData != null && camData.useMidPoint) midDummy.SetActive(true);

        if (isReady) SyncPreviewCameraToCurrentPoint();
    }

    private void OnDisable()
    {
        // 取消选中时隐藏模型
        if (startDummy != null) startDummy.SetActive(false);
        if (endDummy != null) endDummy.SetActive(false);
        if (midDummy != null) midDummy.SetActive(false);
    }

    public override void BindClip(TimelineEventData clip, TimelineManager mgr)
    {
        base.BindClip(clip, mgr);

        camData = clip.customData as SmoothCameraClipData ?? new SmoothCameraClipData();
        if (clip.customData == null) clip.customData = camData;

        // 初始化按钮
        if (btnMid != null) btnMid.gameObject.SetActive(camData.useMidPoint);
        btnCam1?.onClick.AddListener(() => SwitchTab(0));
        btnCam2?.onClick.AddListener(() => SwitchTab(1));
        btnMid?.onClick.AddListener(() => SwitchTab(2));

        if (midPointToggle != null)
        {
            midPointToggle.SetIsOnWithoutNotify(camData.useMidPoint);
            midPointToggle.onValueChanged.AddListener(OnMidPointToggleChanged);
        }

        // 注册滑块
        RegSlider(sliderPosX, v => WritePoint(p => p.posX = v));
        RegSlider(sliderPosY, v => WritePoint(p => p.posY = v));
        RegSlider(sliderPosZ, v => WritePoint(p => p.posZ = v));
        RegSlider(sliderRotX, v => WritePoint(p => p.rotX = v));
        RegSlider(sliderRotY, v => WritePoint(p => p.rotY = v));
        RegSlider(sliderRotZ, v => WritePoint(p => p.rotZ = v));

        // 生成模型
        CreateDummies();

        isReady = true;
        SwitchTab(0);
    }

    private void CreateDummies()
    {
        if (cameraDummyPrefab == null) return;

        if (startDummy == null) { startDummy = Instantiate(cameraDummyPrefab); startDummy.name = "StartDummy"; SetDummyColor(startDummy, startColor); }
        if (endDummy == null) { endDummy = Instantiate(cameraDummyPrefab); endDummy.name = "EndDummy"; SetDummyColor(endDummy, endColor); }
        if (midDummy == null) { midDummy = Instantiate(cameraDummyPrefab); midDummy.name = "MidDummy"; SetDummyColor(midDummy, midColor); }

        SetLayerRecursively(startDummy, LayerMask.NameToLayer("EditorOnly"));
        SetLayerRecursively(endDummy, LayerMask.NameToLayer("EditorOnly"));
        SetLayerRecursively(midDummy, LayerMask.NameToLayer("EditorOnly"));

        UpdateVisuals();
    }

    private void SwitchTab(int tab)
    {
        if (tab == 2 && !camData.useMidPoint) return;
        selectedTab = tab;

        SetTabColor(btnCam1, tab == 0);
        SetTabColor(btnCam2, tab == 1);
        SetTabColor(btnMid, tab == 2);

        LoadPointToSliders(GetCurrentPoint());

        // 只有在主动点击 Tab 切换时，才强制相机跳过去
        SyncPreviewCameraToCurrentPoint();
    }

    private void OnMidPointToggleChanged(bool on)
    {
        camData.useMidPoint = on;
        if (midDummy != null) midDummy.SetActive(on);

        if (on)
        {
            if (btnMid != null) btnMid.gameObject.SetActive(true);
            SwitchTab(2);
        }
        else
        {
            if (selectedTab == 2) SwitchTab(0);
            if (btnMid != null) btnMid.gameObject.SetActive(false);
        }
        UpdateVisuals();
    }

    private SmoothCameraClipData.CamPoint GetCurrentPoint()
    {
        return selectedTab switch { 0 => camData.point1, 1 => camData.point2, 2 => camData.midPoint, _ => camData.point1 };
    }

    private void WritePoint(System.Action<SmoothCameraClipData.CamPoint> setter)
    {
        if (suppressCallbacks || camData == null) return;
        setter(GetCurrentPoint());

        // 拖动滑块时，实时更新相机和模型
        SyncPreviewCameraToCurrentPoint();
        UpdateVisuals();
        UpdateLabels();
    }

    // ==========================================
    // 刷新场景内的摄像机替身模型
    // ==========================================
    private void UpdateVisuals()
    {
        if (camData == null) return;
        if (startDummy != null) { startDummy.transform.position = camData.point1.Position; startDummy.transform.rotation = camData.point1.Rotation; }
        if (endDummy != null) { endDummy.transform.position = camData.point2.Position; endDummy.transform.rotation = camData.point2.Rotation; }
        if (midDummy != null && camData.useMidPoint) { midDummy.transform.position = camData.midPoint.Position; midDummy.transform.rotation = camData.midPoint.Rotation; }
    }

    private void SyncPreviewCameraToCurrentPoint()
    {
        if (previewCamera == null) return;
        var p = GetCurrentPoint();
        previewCamera.transform.position = p.Position;
        previewCamera.transform.rotation = p.Rotation;
    }

    public void RecordFromCamera()
    {
        if (previewCamera == null || camData == null) return;
        var p = GetCurrentPoint();
        p.posX = previewCamera.transform.position.x;
        p.posY = previewCamera.transform.position.y;
        p.posZ = previewCamera.transform.position.z;
        Vector3 rot = previewCamera.transform.eulerAngles;
        p.rotX = (rot.x > 180) ? rot.x - 360 : rot.x;
        p.rotY = (rot.y > 180) ? rot.y - 360 : rot.y;
        p.rotZ = (rot.z > 180) ? rot.z - 360 : rot.z;

        LoadPointToSliders(p);
        UpdateVisuals();
    }

    public void PreviewCamera() { SyncPreviewCameraToCurrentPoint(); }

    private void LoadPointToSliders(SmoothCameraClipData.CamPoint p)
    {
        suppressCallbacks = true;
        SetSlider(sliderPosX, posXMin, posXMax, p.posX);
        SetSlider(sliderPosY, posYMin, posYMax, p.posY);
        SetSlider(sliderPosZ, posZMin, posZMax, p.posZ);
        SetSlider(sliderRotX, -180f, 180f, p.rotX);
        SetSlider(sliderRotY, -180f, 180f, p.rotY);
        SetSlider(sliderRotZ, -180f, 180f, p.rotZ);
        suppressCallbacks = false;
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        var p = GetCurrentPoint();
        if (posXText != null) posXText.text = $"{p.posX:F1}";
        if (posYText != null) posYText.text = $"{p.posY:F1}";
        if (posZText != null) posZText.text = $"{p.posZ:F1}";
        if (rotXText != null) rotXText.text = $"{p.rotX:F0}°";
        if (rotYText != null) rotYText.text = $"{p.rotY:F0}°";
        if (rotZText != null) rotZText.text = $"{p.rotZ:F0}°";
    }

    private void SetSlider(Slider s, float min, float max, float val)
    {
        if (s == null) return;
        s.minValue = min; s.maxValue = max;
        s.SetValueWithoutNotify(val);
    }

    private void RegSlider(Slider s, System.Action<float> cb)
    {
        if (s == null) return;
        s.onValueChanged.RemoveAllListeners();
        s.onValueChanged.AddListener(v => { if (!suppressCallbacks) cb(v); });
    }

    private void SetTabColor(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? tabActiveColor : tabInactiveColor;
    }

    private void SetDummyColor(GameObject dummy, Color c)
    {
        MeshRenderer[] renderers = dummy.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers) if (r.material != null) r.material.color = c;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }

    private void OnDestroy()
    {
        if (startDummy != null) Destroy(startDummy);
        if (endDummy != null) Destroy(endDummy);
        if (midDummy != null) Destroy(midDummy);
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// VFX Clip Inspector 面板 — 基类
//
// 【扩展方法】：
//   新建类继承本类，重写这三个虚方法即可加入特有逻辑：
//     protected virtual void OnBindExtra()       → 绑定时初始化额外 Slider/Toggle
//     protected virtual void OnDataChangedExtra()→ 数据变化时刷新额外参数
//     protected virtual void OnRefreshExtra()    → RefreshDisplay 时刷新额外 UI
//
// 预制体结构（ClipPanel_VFX_Base）：
//   ├── Header (clipNameText + deleteButton + backButton)
//   ├── TimeInfo (startTimeText + durationText + endTimeText)
//   ├── Section_Position
//   │   ├── Row_X → Slider + Text
//   │   ├── Row_Y → Slider + Text
//   │   └── Row_Z → Slider + Text
//   ├── Section_Rotation
//   │   ├── Row_X → Slider + Text
//   │   ├── Row_Y → Slider + Text
//   │   └── Row_Z → Slider + Text
//   ├── Section_Scale
//   │   ├── Row_X → Slider + Text
//   │   ├── Row_Y → Slider + Text
//   │   └── Row_Z → Slider + Text
//   ├── Section_Color
//   │   └── ColorPickerPanel
//   ├── Section_Playback
//   │   ├── Row_Speed → Slider(sliderSpeed) + Text(speedValueText)
//   │   └── Row_Loop  → Toggle(toggleLoop) + Label "循环播放"
//   └── Extra_Content (空容器，子类的额外 UI 放这里)
// ==========================================
public class VFXClipInspectorPanel : ClipInspectorPanel
{
    // ─── Position ───
    [Header("=== Position ===")]
    public Slider sliderPosX, sliderPosY, sliderPosZ;
    public TextMeshProUGUI posXText, posYText, posZText;

    [Header("Position 范围")]
    public float posXMin = -20f; public float posXMax = 20f;
    public float posYMin = -5f; public float posYMax = 15f;
    public float posZMin = -20f; public float posZMax = 20f;

    // ─── Rotation ───
    [Header("=== Rotation ===")]
    public Slider sliderRotX, sliderRotY, sliderRotZ;
    public TextMeshProUGUI rotXText, rotYText, rotZText;

    [Header("Rotation 范围")]
    public float rotMin = 0f; public float rotMax = 360f;

    // ─── Scale ───
    [Header("=== Scale ===")]
    public Slider sliderScaleX, sliderScaleY, sliderScaleZ;
    public TextMeshProUGUI scaleXText, scaleYText, scaleZText;

    [Header("Scale 范围")]
    public float scaleMin = 0.1f; public float scaleMax = 5f;

    // ─── Color ───
    [Header("=== Color ===")]
    public ColorPickerPanel colorPicker;

    // ─── Playback ───
    [Header("=== Playback ===")]
    public Slider sliderSpeed;
    public TextMeshProUGUI speedValueText;
    public Toggle toggleLoop;

    [Header("PlaySpeed 范围")]
    public float speedMin = 0.1f; public float speedMax = 3f;

    // 内部
    protected VFXClipData vfxData;
    private bool isReady = false;

    // ==========================================
    protected override void Awake() { base.Awake(); }

    // ==========================================
    public override void BindClip(TimelineEventData clip, TimelineManager mgr)
    {
        base.BindClip(clip, mgr);

        // 取出或新建 VFXClipData（子类可以 new 出自己的子类型）
        vfxData = clip.customData as VFXClipData ?? CreateData();
        if (clip.customData == null) clip.customData = vfxData;

        SyncRangesToData();

        // 初始化所有 Slider
        InitSlider(sliderPosX, posXMin, posXMax, vfxData.posX);
        InitSlider(sliderPosY, posYMin, posYMax, vfxData.posY);
        InitSlider(sliderPosZ, posZMin, posZMax, vfxData.posZ);
        InitSlider(sliderRotX, rotMin, rotMax, vfxData.rotX);
        InitSlider(sliderRotY, rotMin, rotMax, vfxData.rotY);
        InitSlider(sliderRotZ, rotMin, rotMax, vfxData.rotZ);
        InitSlider(sliderScaleX, scaleMin, scaleMax, vfxData.scaleX);
        InitSlider(sliderScaleY, scaleMin, scaleMax, vfxData.scaleY);
        InitSlider(sliderScaleZ, scaleMin, scaleMax, vfxData.scaleZ);
        InitSlider(sliderSpeed, speedMin, speedMax, vfxData.playSpeed);

        if (toggleLoop != null)
        {
            toggleLoop.SetIsOnWithoutNotify(vfxData.loop);
            toggleLoop.onValueChanged.AddListener(v => { vfxData.loop = v; OnDataChanged(); });
        }

        if (colorPicker != null)
        {
            colorPicker.SetColor(vfxData.color, notify: false);
            colorPicker.onColorChanged.AddListener(c => { vfxData.color = c; OnDataChanged(); });
        }

        isReady = true;
        RegisterListeners();

        // 子类额外绑定
        OnBindExtra();

        RefreshDisplay();
    }

    // ==========================================
    public override void RefreshDisplay()
    {
        base.RefreshDisplay();
        if (!isReady || vfxData == null) return;
        UpdateLabels();
        OnRefreshExtra();
        ApplyToRuntime();
    }

    // ==========================================
    // 子类重写区域
    // ==========================================

    // 子类可以返回自己的数据类型（继承 VFXClipData 即可）
    protected virtual VFXClipData CreateData() => new VFXClipData();

    // 子类在这里绑定额外的 Slider / Toggle
    protected virtual void OnBindExtra() { }

    // 数据变化时子类额外处理
    protected virtual void OnDataChangedExtra() { }

    // RefreshDisplay 时子类额外刷新
    protected virtual void OnRefreshExtra() { }

    // ==========================================
    // 所有 Slider 监听注册
    // ==========================================
    private void RegisterListeners()
    {
        Reg(sliderPosX, v => { vfxData.posX = v; OnDataChanged(); });
        Reg(sliderPosY, v => { vfxData.posY = v; OnDataChanged(); });
        Reg(sliderPosZ, v => { vfxData.posZ = v; OnDataChanged(); });
        Reg(sliderRotX, v => { vfxData.rotX = v; OnDataChanged(); });
        Reg(sliderRotY, v => { vfxData.rotY = v; OnDataChanged(); });
        Reg(sliderRotZ, v => { vfxData.rotZ = v; OnDataChanged(); });
        Reg(sliderScaleX, v => { vfxData.scaleX = v; OnDataChanged(); });
        Reg(sliderScaleY, v => { vfxData.scaleY = v; OnDataChanged(); });
        Reg(sliderScaleZ, v => { vfxData.scaleZ = v; OnDataChanged(); });
        Reg(sliderSpeed, v => { vfxData.playSpeed = v; OnDataChanged(); });
    }

    private void Reg(Slider s, UnityEngine.Events.UnityAction<float> cb)
    {
        if (s != null) s.onValueChanged.AddListener(cb);
    }

    private void OnDataChanged()
    {
        UpdateLabels();
        ApplyToRuntime();
        OnDataChangedExtra();
    }

    // ==========================================
    // 更新文字标签
    // ==========================================
    private void UpdateLabels()
    {
        if (vfxData == null) return;
        SetLabel(posXText, $"{vfxData.posX:F1}");
        SetLabel(posYText, $"{vfxData.posY:F1}");
        SetLabel(posZText, $"{vfxData.posZ:F1}");
        SetLabel(rotXText, $"{vfxData.rotX:F0}°");
        SetLabel(rotYText, $"{vfxData.rotY:F0}°");
        SetLabel(rotZText, $"{vfxData.rotZ:F0}°");
        SetLabel(scaleXText, $"{vfxData.scaleX:F2}");
        SetLabel(scaleYText, $"{vfxData.scaleY:F2}");
        SetLabel(scaleZText, $"{vfxData.scaleZ:F2}");
        SetLabel(speedValueText, $"x{vfxData.playSpeed:F2}");
    }

    private void SetLabel(TextMeshProUGUI t, string s) { if (t != null) t.text = s; }

    // ==========================================
    // 把数据实时应用到运行时 VFX 实例（Edit 模式预览）
    // ==========================================
    private void ApplyToRuntime()
    {
        if (vfxData?.runtimeInstance == null) return;
        ApplyVFXData(vfxData.runtimeInstance, vfxData);
    }

    // 静态工具：供 VFXPlaybackSystem 也调用
    public static void ApplyVFXData(GameObject go, VFXClipData data)
    {
        if (go == null || data == null) return;

        go.transform.position = data.Position;
        go.transform.rotation = data.Rotation;
        go.transform.localScale = data.Scale;

        // ---------- ParticleSystem ----------
        var ps = go.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.simulationSpeed = data.playSpeed;
            main.loop = data.loop;
            main.startColor = new ParticleSystem.MinMaxGradient(data.color);

            // 如果 ps 已经停止但 clip 内要求循环，重新播放
            if (data.loop && !ps.isPlaying) ps.Play();
        }

        // ---------- VisualEffect (URP VFX Graph) ----------
#if UNITY_VFX_GRAPH
        var vfxGraph = go.GetComponentInChildren<UnityEngine.VFX.VisualEffect>();
        if (vfxGraph != null)
        {
            vfxGraph.playRate = data.playSpeed;
            // 颜色需要在 VFX Graph 里暴露 "Color" 属性
            if (vfxGraph.HasVector4("Color"))
                vfxGraph.SetVector4("Color", data.color);
        }
#endif
    }

    // ==========================================
    // Slider 工具
    // ==========================================
    private void InitSlider(Slider s, float min, float max, float val)
    {
        if (s == null) return;
        s.onValueChanged.RemoveAllListeners();
        s.minValue = min;
        s.maxValue = max;
        s.value = Mathf.Clamp(val, min, max);
    }

    private void SyncRangesToData()
    {
        if (vfxData == null) return;
        vfxData.posXMin = posXMin; vfxData.posXMax = posXMax;
        vfxData.posYMin = posYMin; vfxData.posYMax = posYMax;
        vfxData.posZMin = posZMin; vfxData.posZMax = posZMax;
        vfxData.rotMin = rotMin; vfxData.rotMax = rotMax;
        vfxData.scaleMin = scaleMin; vfxData.scaleMax = scaleMax;
        vfxData.speedMin = speedMin; vfxData.speedMax = speedMax;
    }
}
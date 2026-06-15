using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// NewVFX Clip Inspector 面板
// 继承 VFXClipInspectorPanel
//
// 修复：导入存档后不点击 clip 也能正确还原颜色/scale/循环
// 原理：SaveLoadSystem 在 ForceRefresh 后补调 VFXChildApplier.Apply()
//       VFXChildApplier 由 SaveLoadSystem 在 4.5 步骤里动态挂载并执行
// ==========================================
public class NewVFXClipInspectorPanel : VFXClipInspectorPanel
{
    [Header("=== 父节点（拖入含多个子 ParticleSystem 的 GO）===")]
    public GameObject parentTarget;

    [Header("=== 隐藏不用的父类 UI ===")]
    public GameObject sectionRotation;

    [Header("=== Color Picker ===")]
    public ColorPickerPanel colorPicker;

    protected override VFXClipData CreateData() => new VFXClipData();

    protected override void OnBindExtra()
    {
        if (vfxData == null) return;

        // 隐藏 Rotation UI
        if (sectionRotation != null)
            sectionRotation.SetActive(false);

        // 把拖入的父节点注入 runtimeInstance
        // 注意：导入存档时此处可能为 null（ForceRefresh 还没跑）
        // SaveLoadSystem 的 4.5 步骤会在之后补处理
        if (parentTarget != null)
            vfxData.runtimeInstance = parentTarget;

        // 如果 runtimeInstance 已经有值（正常手动点击流程），立即挂上 Applier
        if (vfxData.runtimeInstance != null)
            EnsureApplier(vfxData.runtimeInstance);

        // 初始化颜色选择器
        if (colorPicker != null)
        {
            colorPicker.onColorChanged.RemoveAllListeners();
            colorPicker.SetColor(vfxData.color, notify: false);
            colorPicker.onColorChanged.AddListener(OnColorChanged);
        }
    }

    private void OnColorChanged(Color c)
    {
        if (vfxData == null) return;
        vfxData.color = c;
        ApplyToChildren();
    }

    protected override void OnRefreshExtra()
    {
        if (vfxData == null) return;
        if (colorPicker != null)
            colorPicker.SetColor(vfxData.color, notify: false);
        ApplyToChildren();
    }

    protected override void OnDataChangedExtra()
    {
        ApplyToChildren();
    }

    private void ApplyToChildren()
    {
        GameObject root = vfxData?.runtimeInstance ?? parentTarget;
        if (root == null) return;

        root.transform.position = vfxData.Position;

        foreach (Transform child in root.transform)
        {
            child.localScale = vfxData.Scale;

            var ps = child.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(vfxData.color);
                main.simulationSpeed = vfxData.playSpeed;
                main.loop = vfxData.loop;
                if (vfxData.loop && !ps.isPlaying) ps.Play();
            }

            var lt = child.GetComponent<Light>();
            if (lt != null)
                lt.color = vfxData.color;

            var rend = child.GetComponent<Renderer>();
            if (rend != null && rend.material != null)
                rend.material.color = vfxData.color;

#if UNITY_VFX_GRAPH
            var vfxGraph = child.GetComponent<UnityEngine.VFX.VisualEffect>();
            if (vfxGraph != null)
            {
                vfxGraph.playRate = vfxData.playSpeed;
                if (vfxGraph.HasVector4("Color"))
                    vfxGraph.SetVector4("Color", data.color);
            }
#endif
        }
    }

    // runtimeInstance 上挂 VFXChildApplier，让 SaveLoadSystem 能找到它
    private void EnsureApplier(GameObject root)
    {
        var applier = root.GetComponent<VFXChildApplier>();
        if (applier == null)
            applier = root.AddComponent<VFXChildApplier>();
        applier.data = vfxData;
    }
}

// ==========================================
// VFXChildApplier
// 挂在 runtimeInstance（NewVFX 父节点 GO）上
// SaveLoadSystem 导入后在 4.5 步骤里调 Apply() 补同步子节点
// ==========================================
public class VFXChildApplier : MonoBehaviour
{
    [HideInInspector] public VFXClipData data;

    public void Apply()
    {
        if (data == null) return;

        transform.position = data.Position;

        foreach (Transform child in transform)
        {
            child.localScale = data.Scale;

            var ps = child.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(data.color);
                main.simulationSpeed = data.playSpeed;
                main.loop = data.loop;
                if (data.loop && !ps.isPlaying) ps.Play();
            }

            var lt = child.GetComponent<Light>();
            if (lt != null)
                lt.color = data.color;

            var rend = child.GetComponent<Renderer>();
            if (rend != null && rend.material != null)
                rend.material.color = data.color;

#if UNITY_VFX_GRAPH
            var vfxGraph = child.GetComponent<UnityEngine.VFX.VisualEffect>();
            if (vfxGraph != null)
            {
                vfxGraph.playRate = data.playSpeed;
                if (vfxGraph.HasVector4("Color"))
                    vfxGraph.SetVector4("Color", data.color);
            }
#endif
        }
    }
}
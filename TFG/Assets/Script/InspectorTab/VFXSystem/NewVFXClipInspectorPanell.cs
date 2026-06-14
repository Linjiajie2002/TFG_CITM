using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// NewVFX Clip Inspector 面板
// 继承 VFXClipInspectorPanel
//
// 与父类的差异：
//   - Rotation：不使用，Section_Rotation 在 Awake 时自动隐藏
//   - Scale：不控制父节点，而是批量设置所有子节点的 localScale
//   - Position：控制父节点的世界坐标
//   - ColorPickerPanel：新增颜色控制，应用到所有子节点
//   - parentTarget：Inspector 里拖入父节点 GO（如 Hit），
//                   面板对其所有直接子节点统一生效
//
// Inspector 字段配置：
//   parentTarget      ← 把父节点（Hit）拖进来
//   colorPicker       ← 把 ColorPickerPanel 拖进来
//   sectionRotation   ← 把预制体里 Section_Rotation 的 GameObject 拖进来（自动隐藏）
// ==========================================
public class NewVFXClipInspectorPanel : VFXClipInspectorPanel
{
    [Header("=== 父节点（拖入含多个子 ParticleSystem 的 Prefab）===")]
    public GameObject parentTarget;       // ← 在 Inspector 里拖入 Hit 这个 Prefab/GO

    [Header("=== 隐藏不用的父类 UI ===")]
    public GameObject sectionRotation;    // ← 把预制体里 Section_Rotation 拖进来，自动隐藏

    [Header("=== Color Picker ===")]
    public ColorPickerPanel colorPicker;

    // ==========================================
    // CreateData：直接用 VFXClipData
    // ==========================================
    protected override VFXClipData CreateData() => new VFXClipData();

    // ==========================================
    // BindClip 完成后：
    //   1. 把 parentTarget 注入到 vfxData.runtimeInstance（供父类 ApplyVFXData 使用）
    //   2. 初始化 ColorPickerPanel
    // ==========================================
    protected override void OnBindExtra()
    {
        if (vfxData == null) return;

        // Rotation 本类不使用，隐藏对应 UI
        if (sectionRotation != null)
            sectionRotation.SetActive(false);

        // 把手动拖入的父节点同步给 vfxData，
        // 这样父类的 ApplyVFXData 也能拿到它
        if (parentTarget != null)
            vfxData.runtimeInstance = parentTarget;

        // 初始化颜色选择器（参考 PointLightClipInspectorPanel）
        if (colorPicker != null)
        {
            colorPicker.onColorChanged.RemoveAllListeners();
            colorPicker.SetColor(vfxData.color, notify: false);
            colorPicker.onColorChanged.AddListener(OnColorChanged);
        }
    }

    // ==========================================
    // 颜色改变回调
    // ==========================================
    private void OnColorChanged(Color c)
    {
        if (vfxData == null) return;
        vfxData.color = c;
        ApplyToChildren();
    }

    // ==========================================
    // RefreshDisplay 时同步颜色选择器 + 刷新子节点
    // ==========================================
    protected override void OnRefreshExtra()
    {
        if (vfxData == null) return;

        if (colorPicker != null)
            colorPicker.SetColor(vfxData.color, notify: false);

        ApplyToChildren();
    }

    // ==========================================
    // Slider 变化时额外刷新子节点
    // ==========================================
    protected override void OnDataChangedExtra()
    {
        ApplyToChildren();
    }

    // ==========================================
    // 核心：把 vfxData 应用到 parentTarget 的所有直接子节点
    //
    // 子节点变换：使用 localPosition / localRotation / localScale
    //   → 保留各子节点相对父节点的偏移，只改缩放和旋转
    //   → 位置由父节点世界坐标控制，子节点保持 local 偏移不变
    //
    // 如果你想让所有子节点也跟着移动，把 localPosition 改成 position 即可。
    //
    // 支持的子节点组件：
    //   ParticleSystem / Light / Renderer / VFX Graph
    // ==========================================
    private void ApplyToChildren()
    {
        // 优先用 vfxData 里的 runtimeInstance，没有就用 Inspector 拖入的 parentTarget
        GameObject root = vfxData?.runtimeInstance ?? parentTarget;
        if (root == null) return;

        // 父节点自身：只控制位置
        root.transform.position = vfxData.Position;

        // 遍历所有直接子节点
        foreach (Transform child in root.transform)
        {
            // ── Scale 应用到每个子节点 ──
            child.localScale = vfxData.Scale;

            // ── ParticleSystem ──
            var ps = child.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(vfxData.color);
                main.simulationSpeed = vfxData.playSpeed;
                main.loop = vfxData.loop;

                if (vfxData.loop && !ps.isPlaying) ps.Play();
            }

            // ── Light ──
            var lt = child.GetComponent<Light>();
            if (lt != null)
                lt.color = vfxData.color;

            // ── Renderer（MeshRenderer / SpriteRenderer 等）──
            var rend = child.GetComponent<Renderer>();
            if (rend != null && rend.material != null)
                rend.material.color = vfxData.color;

#if UNITY_VFX_GRAPH
            // ── VFX Graph ──
            var vfxGraph = child.GetComponent<UnityEngine.VFX.VisualEffect>();
            if (vfxGraph != null)
            {
                vfxGraph.playRate = vfxData.playSpeed;
                if (vfxGraph.HasVector4("Color"))
                    vfxGraph.SetVector4("Color", vfxData.color);
            }
#endif
        }
    }
}
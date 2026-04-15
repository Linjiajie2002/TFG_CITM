// ==========================================
// 【示例文件】如何为特定 VFX 创建专属数据类和面板
//
// 假设你有一个"爆炸闪光"VFX，需要额外的"爆炸半径"参数
// 只需要：
//   1. 继承 VFXClipData → 加字段
//   2. 继承 VFXClipInspectorPanel → 重写三个虚方法
//   把新面板做成 Prefab，在 DynamicModuleSystem 的 featurePanelMaps 里注册即可
//   VFXPlaybackSystem 无需修改任何代码！
// ==========================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ── 步骤1：扩展数据类 ──────────────────────────────────────
[System.Serializable]
public class ExplosionVFXClipData : VFXClipData
{
    public float blastRadius = 5f;       // 额外字段：爆炸半径
    public float blastRadiusMin = 1f;
    public float blastRadiusMax = 20f;
}

// ── 步骤2：扩展面板类 ──────────────────────────────────────
public class ExplosionVFXClipInspectorPanel : VFXClipInspectorPanel
{
    [Header("=== 爆炸专属 ===")]
    public Slider sliderBlastRadius;
    public TextMeshProUGUI blastRadiusText;

    private ExplosionVFXClipData exData;

    // 重写：创建正确的数据类型
    protected override VFXClipData CreateData() => new ExplosionVFXClipData();

    // 重写：绑定额外 Slider
    protected override void OnBindExtra()
    {
        exData = vfxData as ExplosionVFXClipData;
        if (exData == null || sliderBlastRadius == null) return;

        sliderBlastRadius.minValue = exData.blastRadiusMin;
        sliderBlastRadius.maxValue = exData.blastRadiusMax;
        sliderBlastRadius.value = exData.blastRadius;
        sliderBlastRadius.onValueChanged.AddListener(v =>
        {
            exData.blastRadius = v;
            OnDataChangedExtra();
        });
    }

    // 重写：数据变化时更新额外显示
    protected override void OnDataChangedExtra()
    {
        if (blastRadiusText != null && exData != null)
            blastRadiusText.text = $"{exData.blastRadius:F1}m";
    }

    // 重写：RefreshDisplay 时刷新额外 UI
    protected override void OnRefreshExtra()
    {
        if (sliderBlastRadius != null && exData != null)
            sliderBlastRadius.SetValueWithoutNotify(exData.blastRadius);
        OnDataChangedExtra();
    }
}

// ==========================================
// 使用步骤总结（以后每加一个 VFX 都这么做）：
//
// 1. 复制这个文件，重命名为 FireVFXClipData.cs 之类
// 2. 把类名和额外字段改成对应 VFX 的参数
// 3. 做预制体，挂新面板类，拖好字段
// 4. 在 DynamicModuleSystem 的对应 Module 的 featurePanelMaps 里注册
// 5. 在 VFXPlaybackSystem 的 vfxEntries 里加一条记录（trackName + prefab）
// 6. 完成！VFXPlaybackSystem 自动处理播放逻辑
// ==========================================
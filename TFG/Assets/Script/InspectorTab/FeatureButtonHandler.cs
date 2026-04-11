using UnityEngine;
using UnityEngine.UI;

// ==========================================
// 挂到 Inspector 面板里的每个功能按钮上
// 点击 → 在当前选中轨道的红线位置生成一个 Clip
// ==========================================
public class FeatureButtonHandler : MonoBehaviour
{
    [Header("=== 引用 ===")]
    public DynamicModuleSystem moduleSystem;

    [Header("=== 这个按钮代表的功能名（显示在 Clip 方块上）===")]
    public string featureName = "新功能";

    [Header("=== Clip 默认时长（秒）===")]
    public float defaultDuration = 5f;

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (moduleSystem != null)
            moduleSystem.AddClipToCurrentTrack(featureName, defaultDuration);
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// 挂到每个 Clip 面板的预制体上
// 自动同步显示 Clip 的 startTime / duration
// 并提供删除该 Clip 的按钮接口
// ==========================================
public class ClipInspectorPanel : MonoBehaviour
{
    [Header("=== 数据显示（拖入预制体内的 TMP 文字）===")]
    public TextMeshProUGUI clipNameText;       // 显示 Clip 功能名
    public TextMeshProUGUI startTimeText;      // 显示开始时间
    public TextMeshProUGUI durationText;       // 显示持续时长
    public TextMeshProUGUI endTimeText;        // 显示结束时间（可选）

    [Header("=== 操作按钮 ===")]
    public Button deleteButton;               // 点击删除该 Clip
    public Button backButton;                 // 【新增】点击返回上一页（隐藏当前面板）

    // 内部绑定数据
    private TimelineEventData boundClip;
    private TimelineManager manager;

    private void Awake()
    {
        // 绑定返回按钮事件
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    // ==========================================
    // 由 DynamicModuleSystem 调用，绑定 Clip 数据
    // ==========================================
    public void BindClip(TimelineEventData clip, TimelineManager mgr)
    {
        boundClip = clip;
        manager = mgr;

        // 绑定删除按钮
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }

        RefreshDisplay();
    }

    // ==========================================
    // 刷新显示（TimelineManager.Update 每帧调用）
    // ==========================================
    public void RefreshDisplay()
    {
        if (boundClip == null) return;

        if (clipNameText != null) clipNameText.text = boundClip.eventName;
        if (startTimeText != null) startTimeText.text = $"Start:{FormatTime(boundClip.startTime)}";
        if (durationText != null) durationText.text = $"Duration:{boundClip.duration:F2}";
        if (endTimeText != null) endTimeText.text = $"End:{FormatTime(boundClip.startTime + boundClip.duration)}";
    }

    // ==========================================
    // 【新增】返回按钮点击 → 隐藏当前面板
    // ==========================================
    private void OnBackButtonClicked()
    {
        gameObject.SetActive(false);
    }

    // ==========================================
    // 删除按钮点击 → 删除对应的 Clip
    // ==========================================
    private void OnDeleteButtonClicked()
    {
        if (manager == null || boundClip == null) return;

        // 告诉 TimelineManager 选中并删除该 Clip
        manager.SelectClip(boundClip);
        manager.DeleteSelectedClip();
        // 面板本身会被 Destroy（在 DeleteSelectedClip 里）
    }

    // ==========================================
    // 时间格式化辅助（mm:ss:ff）
    // ==========================================
    private string FormatTime(float t)
    {
        int m = Mathf.FloorToInt(t / 60f);
        int s = Mathf.FloorToInt(t % 60f);
        int f = Mathf.FloorToInt((t % 1f) * 60f);
        return string.Format("{0:00}:{1:00}:{2:00}", m, s, f);
    }

    // ==========================================
    // 以下是预留的扩展槽，后续添加具体功能参数
    // ==========================================
    // public void OnColorChanged(Color c) { ... }
    // public void OnIntensityChanged(float v) { ... }
}
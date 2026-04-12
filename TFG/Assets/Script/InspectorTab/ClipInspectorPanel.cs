using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// 基类：所有 Clip 面板都继承这个
// BindClip / RefreshDisplay 改为 virtual，子类可 override
// ==========================================
public class ClipInspectorPanel : MonoBehaviour
{
    [Header("=== 通用数据显示 ===")]
    public TextMeshProUGUI clipNameText;
    public TextMeshProUGUI startTimeText;
    public TextMeshProUGUI durationText;
    public TextMeshProUGUI endTimeText;

    [Header("=== 操作按钮 ===")]
    public Button deleteButton;
    public Button backButton;

    protected TimelineEventData boundClip;
    protected TimelineManager manager;

    protected virtual void Awake()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);
    }

    // virtual：子类可以 override 并在最后调用 base.BindClip
    public virtual void BindClip(TimelineEventData clip, TimelineManager mgr)
    {
        boundClip = clip;
        manager = mgr;

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }

        RefreshDisplay();
    }

    // virtual：子类 override 时先调用 base.RefreshDisplay() 刷新通用信息
    public virtual void RefreshDisplay()
    {
        if (boundClip == null) return;
        if (clipNameText != null) clipNameText.text = boundClip.eventName;
        if (startTimeText != null) startTimeText.text = $"Start: {FormatTime(boundClip.startTime)}";
        if (durationText != null) durationText.text = $"Duration: {boundClip.duration:F2}s";
        if (endTimeText != null) endTimeText.text = $"End: {FormatTime(boundClip.startTime + boundClip.duration)}";
    }

    private void OnBackButtonClicked() { gameObject.SetActive(false); }

    private void OnDeleteButtonClicked()
    {
        if (manager == null || boundClip == null) return;
        manager.SelectClip(boundClip);
        manager.DeleteSelectedClip();
    }

    protected string FormatTime(float t)
    {
        int m = Mathf.FloorToInt(t / 60f);
        int s = Mathf.FloorToInt(t % 60f);
        int f = Mathf.FloorToInt((t % 1f) * 60f);
        return string.Format("{0:00}:{1:00}:{2:00}", m, s, f);
    }
}
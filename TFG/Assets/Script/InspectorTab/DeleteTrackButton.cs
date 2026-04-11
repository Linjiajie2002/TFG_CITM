using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// 挂到"删除整条轨道"按钮上
// 支持两种模式：
//   直接删除（confirmFirst = false）
//   二次确认（confirmFirst = true）→ 第一次点变红"确认删除？"，3 秒后恢复
// ==========================================
public class DeleteTrackButton : MonoBehaviour
{
    [Header("=== 引用 ===")]
    public TimelineManager timeline;

    [Header("=== 二次确认（防误删）===")]
    public bool confirmFirst = true;          // 是否需要二次确认
    public float confirmTimeout = 3f;         // 几秒后自动取消确认状态

    [Header("=== 按钮文字（可选）===")]
    public TextMeshProUGUI buttonText;
    public string normalText = "删除轨道";
    public string confirmText = "确认删除？";

    [Header("=== 按钮颜色（可选）===")]
    public Image buttonImage;
    public Color normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color confirmColor = new Color(0.8f, 0.1f, 0.1f, 1f);

    private bool waitingConfirm = false;
    private float confirmTimer = 0f;

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(OnButtonClicked);

        // 自动找引用
        if (timeline == null) timeline = FindObjectOfType<TimelineManager>();
        if (buttonText == null) buttonText = GetComponentInChildren<TextMeshProUGUI>();
        if (buttonImage == null) buttonImage = GetComponent<Image>();

        ResetState();
    }

    void Update()
    {
        if (!waitingConfirm) return;
        confirmTimer -= Time.deltaTime;
        if (confirmTimer <= 0f) ResetState();
    }

    void OnButtonClicked()
    {
        if (timeline == null) return;
        if (timeline.selectedTrackIndex < 0)
        {
            Debug.Log("[DeleteTrackButton] 没有选中任何轨道。");
            return;
        }

        if (!confirmFirst)
        {
            // 直接删
            timeline.DeleteSelectedTrack();
            return;
        }

        if (!waitingConfirm)
        {
            // 第一次点：进入确认状态
            waitingConfirm = true;
            confirmTimer = confirmTimeout;
            if (buttonText != null) buttonText.text = confirmText;
            if (buttonImage != null) buttonImage.color = confirmColor;
        }
        else
        {
            // 第二次点：真正删除
            timeline.DeleteSelectedTrack();
            ResetState();
        }
    }

    void ResetState()
    {
        waitingConfirm = false;
        confirmTimer = 0f;
        if (buttonText != null) buttonText.text = normalText;
        if (buttonImage != null) buttonImage.color = normalColor;
    }
}
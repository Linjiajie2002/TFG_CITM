using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// 挂到"删除整条轨道"按钮上
// 全新机制：点击 -> 强制倒数3秒(期间点击无效) -> 变成确认删除 -> 若不点则恢复原样
// 删除后：自动顺延选中相邻的轨道，不会变成什么都没选的空白状态
// ==========================================
public class DeleteTrackButton : MonoBehaviour
{
    // 按钮的三个核心状态
    public enum ButtonState
    {
        Normal,         // 平常状态
        Countdown,      // 强制倒计时状态（点击无效）
        WaitingConfirm  // 等待确认状态（点击执行删除）
    }

    [Header("=== 引用 ===")]
    public TimelineManager timeline;

    [Header("=== 二次确认（防呆冷静期）===")]
    public bool confirmFirst = true;          // 是否开启防误删保护
    public float forceWaitTime = 3f;          // 【新增】点击后强制等待的秒数（冷静期）
    public float confirmTimeout = 3f;         // 倒数结束后，给你几秒钟的时间点击确认

    [Header("=== 按钮文字 ===")]
    public TextMeshProUGUI buttonText;
    public string normalText = "Delete Track";
    public string confirmText = "Confirm Delete！";

    [Header("=== 按钮颜色 ===")]
    public Image buttonImage;
    public Color normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color countdownColor = new Color(0.8f, 0.5f, 0.1f, 1f); // 【新增】倒数时的颜色（橙色警告）
    public Color confirmColor = new Color(0.8f, 0.1f, 0.1f, 1f);   // 可以删除时的颜色（红色危险）

    private ButtonState currentState = ButtonState.Normal;
    private float timer = 0f;

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
        if (currentState == ButtonState.Normal) return;

        timer -= Time.deltaTime;

        // 状态 1：正在强制倒数
        if (currentState == ButtonState.Countdown)
        {
            if (buttonText != null)
                buttonText.text = $"{Mathf.CeilToInt(timer)}";

            // 倒数结束，进入“可确认删除”状态
            if (timer <= 0f)
            {
                currentState = ButtonState.WaitingConfirm;
                timer = confirmTimeout; // 重置计时器为确认窗口期时间

                if (buttonText != null) buttonText.text = confirmText;
                if (buttonImage != null) buttonImage.color = confirmColor;
            }
        }
        // 状态 2：等待玩家确认
        else if (currentState == ButtonState.WaitingConfirm)
        {
            // 如果玩家迟迟不点，超时后恢复原样
            if (timer <= 0f)
            {
                ResetState();
            }
        }
    }

    void OnButtonClicked()
    {
        if (timeline == null) return;
        if (timeline.selectedTrackIndex < 0)
        {
            Debug.LogWarning("[DeleteTrackButton] 当前没有任何轨道被选中。");
            return;
        }

        // 如果关了保护，直接删
        if (!confirmFirst)
        {
            ExecuteDeleteAndReselect();
            return;
        }

        // 第一次点：进入强制倒数状态
        if (currentState == ButtonState.Normal)
        {
            currentState = ButtonState.Countdown;
            timer = forceWaitTime;
            if (buttonImage != null) buttonImage.color = countdownColor;
        }
        // 倒数期间：狂点也没有用！强行拦截！
        else if (currentState == ButtonState.Countdown)
        {
            return;
        }
        // 第二次点（在可确认的窗口期内）：真正删除
        else if (currentState == ButtonState.WaitingConfirm)
        {
            ExecuteDeleteAndReselect();
            ResetState();
        }
    }

    // ==========================================
    // 执行删除，并智能选中下一条轨道
    // ==========================================
    void ExecuteDeleteAndReselect()
    {
        // 1. 记下当前被删的轨道是第几条
        int targetIndex = timeline.selectedTrackIndex;

        // 2. 无情抹杀当前轨道
        timeline.DeleteSelectedTrack();

        // 3. 删除后，如果有剩余轨道，自动选中相邻的！
        if (timeline.trackCount > 0)
        {
            // Mathf.Clamp 会保证：如果你删了最后一条，它会自动选中现在的倒数第一条
            // 如果你删了中间的，下面的轨道会上移，原来的 index 刚好对应新的下一条轨道
            int nextIndexToSelect = Mathf.Clamp(targetIndex, 0, timeline.trackCount - 1);

            // 呼叫 Timeline 选中轨道（这会连带触发你的 Inspector Tab 自动刷新！）
            timeline.SelectTrack(nextIndexToSelect);
        }
        else
        {
            // 如果删光了，清空一切选中状态
            timeline.DeselectAll();
        }
    }

    void ResetState()
    {
        currentState = ButtonState.Normal;
        timer = 0f;
        if (buttonText != null) buttonText.text = normalText;
        if (buttonImage != null) buttonImage.color = normalColor;
    }
}
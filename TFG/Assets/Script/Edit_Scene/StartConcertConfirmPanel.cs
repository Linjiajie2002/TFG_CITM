using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartConcertConfirmPanel : MonoBehaviour
{
    public enum PanelState
    {
        Locked,
        ReadyToConfirm
    }

    [Header("=== 引用 ===")]
    public StageManager stageManager;

    [Header("=== 面板物体 ===")]
    public GameObject panelRoot;

    [Header("=== 按钮 ===")]
    public Button cancelButton;
    public Button confirmButton;

    [Header("=== 强制等待 ===")]
    public float forceWaitTime = 3f;

    [Header("=== 确认按钮文字 ===")]
    public TextMeshProUGUI confirmButtonText;
    public string confirmReadyText = "确认开始演出";

    [Header("=== 确认按钮颜色 ===")]
    // 【修改点】：去掉了 public Image，直接用 3 种颜色控制 Button
    public Color confirmLockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);  // 锁定中：灰色
    public Color confirmReadyColor = new Color(0.8f, 0.1f, 0.1f, 1f);   // 可点击：红色警告

    [Header("=== 提醒文字 ===")]
    public TextMeshProUGUI reminderText;
    public string reminderMessage = "演出开始后将无法再修改Clip数据，请确保已保存！";

    private PanelState currentState = PanelState.Locked;
    private float timer = 0f;

    void Awake()
    {
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);

        // 【修改点】：删除了 panelRoot.SetActive(false)！解决第一次按两次的 Bug
        // 请确保在 Unity 编辑器中，把 panelRoot 默认设置为隐藏（取消勾选）。
    }

    void Update()
    {
        if (currentState != PanelState.Locked) return;
        if (panelRoot != null && !panelRoot.activeSelf) return;

        timer -= Time.deltaTime;

        if (confirmButtonText != null)
            confirmButtonText.text = $"{Mathf.CeilToInt(Mathf.Max(timer, 0f))}";

        if (timer <= 0f)
        {
            currentState = PanelState.ReadyToConfirm;

            if (confirmButton != null)
            {
                confirmButton.interactable = true;
                SetButtonColor(confirmButton, confirmReadyColor);
            }
            if (confirmButtonText != null) confirmButtonText.text = confirmReadyText;
        }
    }

    public void OpenPanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        if (reminderText != null) reminderText.text = reminderMessage;

        currentState = PanelState.Locked;
        timer = forceWaitTime;

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            SetButtonColor(confirmButton, confirmLockedColor);
        }
        if (confirmButtonText != null) confirmButtonText.text = $"{Mathf.CeilToInt(forceWaitTime)}";
    }

    void OnCancelClicked()
    {
        ClosePanel();
    }

    void OnConfirmClicked()
    {
        if (currentState != PanelState.ReadyToConfirm) return;

        ClosePanel();

        if (stageManager != null)
        {
            stageManager.StartConcert();
        }
    }

    void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        currentState = PanelState.Locked;
        timer = 0f;
    }

    // 【新增】：直接修改 Button 组件颜色的核心方法
    private void SetButtonColor(Button btn, Color targetColor)
    {
        if (btn == null) return;

        // 1. 修改按钮的图形颜色（适用于没有手动挂载 Image 引用的情况）
        if (btn.targetGraphic != null)
        {
            btn.targetGraphic.color = targetColor;
        }

        // 2. 同时修改按钮的过渡颜色，防止鼠标悬浮/点击时变回原来的颜色
        ColorBlock cb = btn.colors;
        cb.normalColor = targetColor;
        cb.disabledColor = targetColor;
        cb.selectedColor = targetColor;
        btn.colors = cb;
    }
}
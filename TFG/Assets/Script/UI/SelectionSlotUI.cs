using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// ==========================================
// 右下角单个选择槽
// 显示已选中的角色/场景/音乐缩略图和名字
// 每个槽独立，有自己的取消按钮
// ==========================================
public class SelectionSlotUI : MonoBehaviour
{
    [Header("=== UI 组件 ===")]
    public Image            thumbnail;         // 缩略图
    public TextMeshProUGUI  nameLabel;         // 名字文字
    public Button           cancelButton;      // 取消按钮
    public GameObject       emptyIndicator;    // 空状态显示（"＋"图标或空框）
    public GameObject       filledIndicator;   // 已选状态容器（thumbnail + name）

    [Header("=== 空槽占位图（可选）===")]
    public Sprite defaultSprite;               // 没有贴图时用的灰色占位图

    // 外部注册的取消回调
    private Action onCancelCallback;

    // ==========================================
    void Awake()
    {
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
    }

    // ==========================================
    // 设置已选状态
    // ==========================================
    public void SetSelected(Sprite sprite, string displayName, Action onCancel)
    {
        onCancelCallback = onCancel;

        if (thumbnail != null)
        {
            thumbnail.sprite  = sprite != null ? sprite : defaultSprite;
            thumbnail.enabled = true;
        }

        if (nameLabel != null) nameLabel.text = displayName;

        if (emptyIndicator  != null) emptyIndicator.SetActive(false);
        if (filledIndicator != null) filledIndicator.SetActive(true);
    }

    // ==========================================
    // 重置为空状态
    // ==========================================
    public void SetEmpty()
    {
        onCancelCallback = null;

        if (thumbnail != null) thumbnail.sprite = defaultSprite;
        if (nameLabel != null) nameLabel.text   = "";

        if (emptyIndicator  != null) emptyIndicator.SetActive(true);
        if (filledIndicator != null) filledIndicator.SetActive(false);
    }

    // ==========================================
    private void OnCancelClicked()
    {
        onCancelCallback?.Invoke();
    }
}

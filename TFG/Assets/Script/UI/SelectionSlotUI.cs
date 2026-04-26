using UnityEngine;
using UnityEngine.UI;
using System;

// ==========================================
// 右下角单个选择槽
// 显示已选中的角色/场景/音乐缩略图
// 每个槽独立，有自己的取消按钮
// ==========================================
public class SelectionSlotUI : MonoBehaviour
{
    [Header("=== UI 组件 ===")]
    public Image thumbnail;         // 缩略图
    public Button cancelButton;      // 取消按钮
    public GameObject emptyIndicator;    // 空状态显示（"＋"图标或空框）
    public GameObject filledIndicator;   // 已选状态容器（包含 thumbnail 和 cancelButton）

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
    public void SetSelected(Sprite sprite, Action onCancel)
    {
        onCancelCallback = onCancel;

        if (thumbnail != null)
        {
            thumbnail.sprite = sprite;
            thumbnail.enabled = sprite != null; // 确保有图片时才显示
        }

        if (emptyIndicator != null) emptyIndicator.SetActive(false);
        if (filledIndicator != null) filledIndicator.SetActive(true);
    }

    // ==========================================
    // 重置为空状态
    // ==========================================
    public void SetEmpty()
    {
        onCancelCallback = null;

        if (thumbnail != null)
        {
            thumbnail.sprite = null;
            thumbnail.enabled = false; // 清空时隐藏 Image组件，比默认占位图更好
        }

        if (emptyIndicator != null) emptyIndicator.SetActive(true);
        if (filledIndicator != null) filledIndicator.SetActive(false);
    }

    // ==========================================
    private void OnCancelClicked()
    {
        onCancelCallback?.Invoke();
    }
}
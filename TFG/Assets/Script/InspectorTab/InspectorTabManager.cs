using UnityEngine;
using UnityEngine.UI;
using TMPro; // 必须引入这个才能控制 TMP 文字
using System.Collections.Generic;

[System.Serializable]
public class TabInfo
{
    public string tabName = "新标签";
    public Button tabButton;             // 按钮本身
    public TextMeshProUGUI tabText;      // 【新增】按钮里的文字
    public GameObject activeUnderline;   // 【新增】底部的青色下划线
    public GameObject contentPanel;
}

public class InspectorTabManager : MonoBehaviour
{
    [Header("=== 标签页配置 ===")]
    public List<TabInfo> tabs = new List<TabInfo>();

    [Header("=== 选中状态视觉 (Active) ===")]
    public Color activeBgColor = new Color(0.05f, 0.18f, 0.15f, 1f); // 深青色背景
    public Color activeTextColor = new Color(0f, 1f, 0.8f, 1f);      // 亮青色文字

    [Header("=== 未选中状态视觉 (Inactive) ===")]
    public Color inactiveBgColor = new Color(0.05f, 0.05f, 0.05f, 1f); // 纯暗黑背景
    public Color inactiveTextColor = new Color(0.5f, 0.5f, 0.5f, 1f);  // 灰色文字

    void Start()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i;
            if (tabs[i].tabButton != null)
            {
                // 彻底去掉 Unity Button 自带的颜色过渡（防止打架）
                ColorBlock cb = tabs[i].tabButton.colors;
                cb.colorMultiplier = 1;
                cb.fadeDuration = 0;
                tabs[i].tabButton.colors = cb;
                tabs[i].tabButton.transition = Selectable.Transition.None;

                tabs[i].tabButton.onClick.AddListener(() => SwitchTab(index));
            }
        }

        if (tabs.Count > 0) SwitchTab(0);
    }

    public void SwitchTab(int tabIndex)
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            bool isActive = (i == tabIndex);

            // 1. 切换面板
            if (tabs[i].contentPanel != null)
                tabs[i].contentPanel.SetActive(isActive);

            // 2. 切换背景色
            if (tabs[i].tabButton != null)
            {
                Image bgImage = tabs[i].tabButton.GetComponent<Image>();
                if (bgImage != null) bgImage.color = isActive ? activeBgColor : inactiveBgColor;
            }

            // 3. 切换文字颜色
            if (tabs[i].tabText != null)
                tabs[i].tabText.color = isActive ? activeTextColor : inactiveTextColor;

            // 4. 开关高亮下划线
            if (tabs[i].activeUnderline != null)
                tabs[i].activeUnderline.SetActive(isActive);
        }
    }
}
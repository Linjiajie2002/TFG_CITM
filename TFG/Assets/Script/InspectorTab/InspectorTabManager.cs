using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class TabInfo
{
    public string tabName = "新标签";
    public Button tabButton;
    public TextMeshProUGUI tabText;
    public GameObject activeUnderline;
    public GameObject contentPanel;
}

public class InspectorTabManager : MonoBehaviour
{
    [Header("=== 全局引用 ===")]
    public DynamicModuleSystem moduleSystem; // 【新增】让 Tab 知道大管家的存在

    [Header("=== 标签页配置 ===")]
    public List<TabInfo> tabs = new List<TabInfo>();

    [Header("=== 选中状态视觉 (Active) ===")]
    public Color activeBgColor = new Color(0.05f, 0.18f, 0.15f, 1f);
    public Color activeTextColor = new Color(0f, 1f, 0.8f, 1f);

    [Header("=== 未选中状态视觉 (Inactive) ===")]
    public Color inactiveBgColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    public Color inactiveTextColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    void Start()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i;
            if (tabs[i].tabButton != null)
            {
                ColorBlock cb = tabs[i].tabButton.colors;
                cb.colorMultiplier = 1;
                cb.fadeDuration = 0;
                tabs[i].tabButton.colors = cb;
                tabs[i].tabButton.transition = Selectable.Transition.None;

                // 【改动】：玩家点击 Tab 时，走专门的连带触发方法
                tabs[i].tabButton.onClick.AddListener(() => OnUserClickedTab(index));
            }
        }

        if (tabs.Count > 0) SwitchTab(0);
    }

    // ==========================================
    // 玩家手动点击 Tab 时触发
    // ==========================================
    private void OnUserClickedTab(int tabIndex)
    {
        SwitchTab(tabIndex); // 1. 先切换右侧 UI

        // 2. 【核心新增】通知模块系统，连带选中左侧的轨道！
        if (moduleSystem != null)
        {
            moduleSystem.SelectTrackByTabIndex(tabIndex);
        }
    }

    // ==========================================
    // 纯代码切换 UI
    // ==========================================
    public void SwitchTab(int tabIndex)
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            bool isActive = (i == tabIndex);

            if (tabs[i].contentPanel != null) tabs[i].contentPanel.SetActive(isActive);

            if (tabs[i].tabButton != null)
            {
                Image bgImage = tabs[i].tabButton.GetComponent<Image>();
                if (bgImage != null) bgImage.color = isActive ? activeBgColor : inactiveBgColor;
            }

            if (tabs[i].tabText != null) tabs[i].tabText.color = isActive ? activeTextColor : inactiveTextColor;
            if (tabs[i].activeUnderline != null) tabs[i].activeUnderline.SetActive(isActive);
        }
    }
}
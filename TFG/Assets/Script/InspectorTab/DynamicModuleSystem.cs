using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class ModuleData
{
    public string moduleName = "New Module";
    public Button addButton;
    public GameObject startPanel;
    public GameObject targetInspector;       // 保留引用，但不在这里控制它了
    public int tabIndex = 0;                 // 【新增】：它对应你 Tab 管理器里的第几个按钮？(0=Light, 1=Camera...)
    public float defaultDuration = 60f;
}

public class DynamicModuleSystem : MonoBehaviour
{
    [Header("=== 全局核心引用 ===")]
    public TimelineManager timeline;
    public InspectorTabManager tabManager;   // 【新增】：接入你的神器 Tab 管理器！
    public Transform headerArea;
    public GameObject headerPrefab;
    public GameObject defaultInspector;

    [Header("=== 你的所有模块列表 ===")]
    public List<ModuleData> allModules = new List<ModuleData>();

    void Start()
    {
        foreach (var module in allModules)
        {
            if (module.addButton != null)
            {
                ModuleData currentModule = module;
                currentModule.addButton.onClick.AddListener(() => OnModuleAddClicked(currentModule));
            }
        }
    }

    void OnModuleAddClicked(ModuleData module)
    {
        if (module.startPanel != null) module.startPanel.SetActive(false);

        if (headerArea != null && headerPrefab != null)
        {
            GameObject newHeader = Instantiate(headerPrefab, headerArea);
            TextMeshProUGUI txt = newHeader.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = module.moduleName;
            Destroy(newHeader);
        }

        if (timeline != null) timeline.AddDynamicTrack(module.moduleName, module.defaultDuration);

        ShowInspector(module.moduleName);
    }

    // ==========================================
    // 【核心同步】：告诉 Tab 管理器去切换按钮和面板！
    // ==========================================
    public void ShowInspector(string moduleName)
    {
        foreach (var m in allModules)
        {
            if (m.moduleName == moduleName)
            {
                // 一句话解决同步问题：直接调用你的 Tab 系统！
                if (tabManager != null) tabManager.SwitchTab(m.tabIndex);
                break;
            }
        }
    }

    public void ShowDefaultInspector()
    {
        // 废弃之前的瞎隐藏逻辑，保持当前 Tab 状态即可
    }

    public void RestoreCover(string moduleName)
    {
        foreach (var m in allModules)
        {
            if (m.moduleName == moduleName)
            {
                if (m.startPanel != null) m.startPanel.SetActive(true); // 盖子盖上
            }
        }
    }
}
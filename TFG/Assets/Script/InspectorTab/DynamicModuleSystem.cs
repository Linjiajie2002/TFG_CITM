using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// ==========================================
// 【新增结构】：用来把“功能名字”和“专属面板”绑定在一起
// ==========================================
[System.Serializable]
public class FeaturePanelMapping
{
    [Tooltip("功能的名字，必须和 UI 按钮传入的名字完全一致（例如：赛博粉、爆闪开关）")]
    public string featureName;
    [Tooltip("当生成这个功能时，弹出的专属 UI 面板预制体")]
    public GameObject specificInspectorPrefab;
}

[System.Serializable]
public class ModuleData
{
    public string moduleName = "New Module";
    public Button addButton;
    public GameObject startPanel;
    public GameObject targetInspector;
    public int tabIndex = 0;
    public float defaultDuration = 60f;

    [Header("=== 默认的 Clip 面板（兜底用） ===")]
    public GameObject defaultClipInspectorPrefab;

    [Header("=== 各个功能的专属 Clip 面板映射 ===")]
    // 【修改点】：变成了一个列表！你可以在这里加无数个特定功能的面板
    public List<FeaturePanelMapping> featurePanelMaps = new List<FeaturePanelMapping>();
}

public class DynamicModuleSystem : MonoBehaviour
{
    [Header("=== 全局核心引用 ===")]
    public TimelineManager timeline;
    public InspectorTabManager tabManager;

    [Header("=== Clip 面板放置容器 ===")]
    public Transform clipPanelContainer;

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
                ModuleData current = module;
                current.addButton.onClick.AddListener(() => OnModuleAddClicked(current));
            }
        }
    }

    void OnModuleAddClicked(ModuleData module)
    {
        if (module.startPanel != null) module.startPanel.SetActive(false);
        if (timeline != null) timeline.AddDynamicTrack(module.moduleName, module.defaultDuration);
        ShowInspector(module.moduleName);
    }

    // ==========================================
    // Inspector 里的功能按钮调用此方法
    // ==========================================
    public void AddClipToCurrentTrack(string featureName, float duration = 5f)
    {
        if (timeline == null) return;
        int trackIndex = timeline.selectedTrackIndex;

        if (trackIndex < 0)
        {
            Debug.LogWarning("[DynamicModuleSystem] 请先选中一个轨道！");
            return;
        }

        // 创建 Clip 方块
        TimelineEventData evt = timeline.AddClipToTrack(trackIndex, featureName, duration);
        if (evt == null) return;

        // 找到该轨道对应的模块
        ModuleData mod = FindModuleByTrackIndex(trackIndex);
        if (mod != null && clipPanelContainer != null)
        {
            // 【核心逻辑】：查找是否有这个功能的专属面板
            GameObject prefabToInstantiate = mod.defaultClipInspectorPrefab; // 先拿兜底的面板准备好

            foreach (var mapping in mod.featurePanelMaps)
            {
                // 如果传入的功能名对上了，就把要生成的面板替换成专属面板！
                if (mapping.featureName == featureName && mapping.specificInspectorPrefab != null)
                {
                    prefabToInstantiate = mapping.specificInspectorPrefab;
                    break;
                }
            }

            // 开始生成面板
            if (prefabToInstantiate != null)
            {
                GameObject panel = Instantiate(prefabToInstantiate, clipPanelContainer);
                panel.SetActive(false); // 默认隐藏

                // 挂载数据绑定脚本
                ClipInspectorPanel panelScript = panel.GetComponent<ClipInspectorPanel>();
                if (panelScript == null) panelScript = panel.AddComponent<ClipInspectorPanel>();
                panelScript.BindClip(evt, timeline);

                // 记录面板引用到事件数据
                evt.inspectorPanel = panel;
            }
            else
            {
                Debug.LogWarning($"[DynamicModuleSystem] 功能 '{featureName}' 既没有专属面板，也没有默认面板！");
            }
        }

        // 立刻选中这个新创建的 Clip，显示它的面板
        timeline.SelectClip(evt);
    }

    // ==========================================
    // 剩下的辅助方法（保持不变）
    // ==========================================
    private ModuleData FindModuleByTrackIndex(int trackIndex)
    {
        TrackData track = timeline.allTracks.Find(t => t.trackIndex == trackIndex);
        if (track == null) return null;
        return allModules.Find(m => m.moduleName == track.trackName);
    }

    public void ShowInspector(string moduleName)
    {
        foreach (var m in allModules)
        {
            if (m.moduleName == moduleName)
            {
                if (tabManager != null) tabManager.SwitchTab(m.tabIndex);
                break;
            }
        }
    }

    public void ShowDefaultInspector() { }

    public void RestoreCover(string moduleName)
    {
        foreach (var m in allModules)
            if (m.moduleName == moduleName && m.startPanel != null)
                m.startPanel.SetActive(true);
    }
}
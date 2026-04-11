using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class FeaturePanelMapping
{
    public string featureName;
    public GameObject specificInspectorPrefab;
}

[System.Serializable]
public class ModuleData
{
    public string moduleName = "New Module";

    [Header("=== 开发者设置（游戏内不可见）===")]
    public bool allowOverlap = true;

    [Space(10)]
    public Button addButton;
    public GameObject startPanel;
    public GameObject targetInspector;
    public int tabIndex = 0;
    public float defaultDuration = 60f;

    [Header("=== 默认的 Clip 面板（兜底用） ===")]
    public GameObject defaultClipInspectorPrefab;

    [Header("=== 各个功能的专属 Clip 面板映射 ===")]
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
        if (timeline != null) timeline.AddDynamicTrack(module.moduleName, module.defaultDuration, module.allowOverlap);
        ShowInspector(module.moduleName);
    }

    // ==========================================
    // 【核心新增】：玩家点击 Tab 时，连带选中时间轴轨道！
    // ==========================================
    public void SelectTrackByTabIndex(int tabIndex)
    {
        if (timeline == null) return;

        // 1. 找出这个 Tab 属于哪个 Module
        ModuleData mod = allModules.Find(m => m.tabIndex == tabIndex);
        if (mod != null)
        {
            // 2. 去时间轴上找有没有这个名字的轨道
            TrackData track = timeline.allTracks.Find(t => t.trackName == mod.moduleName);
            if (track != null)
            {
                // 3. 选中轨道！（传 true 进去代表跳过反向通知 Tab，防止无限死循环）
                // Timeline 内部会自动执行：取消当前 Clip 选中 -> 隐藏 Clip 专属面板 -> 选中新轨道
                timeline.SelectTrack(track.trackIndex, true);
            }
            else
            {
                // 如果玩家点了 Camera Tab，但是还没建 Camera 轨道，那就把左边全清空
                timeline.DeselectAll();
            }
        }
    }

    public void AddClipToCurrentTrack(string featureName, float duration = 5f)
    {
        if (timeline == null) return;
        int trackIndex = timeline.selectedTrackIndex;

        if (trackIndex < 0) return;

        TimelineEventData evt = timeline.AddClipToTrack(trackIndex, featureName, duration);
        if (evt == null) return;

        ModuleData mod = FindModuleByTrackIndex(trackIndex);
        if (mod != null && clipPanelContainer != null)
        {
            GameObject prefabToInstantiate = mod.defaultClipInspectorPrefab;
            foreach (var mapping in mod.featurePanelMaps)
            {
                if (mapping.featureName == featureName && mapping.specificInspectorPrefab != null)
                {
                    prefabToInstantiate = mapping.specificInspectorPrefab;
                    break;
                }
            }

            if (prefabToInstantiate != null)
            {
                GameObject panel = Instantiate(prefabToInstantiate, clipPanelContainer);
                panel.SetActive(false);
                ClipInspectorPanel panelScript = panel.GetComponent<ClipInspectorPanel>();
                if (panelScript == null) panelScript = panel.AddComponent<ClipInspectorPanel>();
                panelScript.BindClip(evt, timeline);
                evt.inspectorPanel = panel;
            }
        }

        timeline.SelectClip(evt);
    }

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
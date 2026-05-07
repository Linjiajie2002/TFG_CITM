using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections.Generic;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ==========================================
// MDF Save/Load System
//
// 挂载位置：System_Manager 或任意常驻 GameObject
//
// 【扩展方式】：
//   如果新增了一种 Clip 数据类型（如 SpotLight），
//   只需在 SerializeCustomData / DeserializeCustomData 两个方法里
//   各加一个 case 分支即可，其余代码不动。
// ==========================================
public class SaveLoadSystem : MonoBehaviour
{
    [Header("=== 系统引用 ===")]
    public TimelineManager timeline;
    public DynamicModuleSystem moduleSystem;
    public CameraPlaybackSystem cameraSystem;
    public LightPlaybackSystem lightSystem;
    public VFXPlaybackSystem vfxSystem;
    public ShaderPlaybackSystem shaderSystem;

    [Header("=== UI 按钮 ===")]
    public Button exportButton;
    public Button importButton;

    [Header("=== 状态提示文字（可选）===")]
    public TextMeshProUGUI statusText;

    // 默认文件名
    private const string DEFAULT_FILENAME = "MyShow";
    private const string FILE_EXT = "mdf";

    void Start()
    {
        if (exportButton != null) exportButton.onClick.AddListener(OnExportClicked);
        if (importButton != null) importButton.onClick.AddListener(OnImportClicked);
    }

    // ==========================================
    // 导出按钮
    // ==========================================
    public void OnExportClicked()
    {
        string path = GetSavePath();
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            MDFSaveData data = CollectSaveData();
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(path, json, System.Text.Encoding.UTF8);
            ShowStatus($"✓ 已导出：{Path.GetFileName(path)}", success: true);
        }
        catch (Exception e)
        {
            ShowStatus($"✗ 导出失败：{e.Message}", success: false);
            Debug.LogError($"[SaveLoadSystem] Export error: {e}");
        }
    }

    // ==========================================
    // 导入按钮
    // ==========================================
    public void OnImportClicked()
    {
        string path = GetLoadPath();
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            MDFSaveData data = JsonUtility.FromJson<MDFSaveData>(json);
            ApplySaveData(data);
            ShowStatus($"✓ 已导入：{Path.GetFileName(path)}", success: true);
        }
        catch (Exception e)
        {
            ShowStatus($"✗ 导入失败：{e.Message}", success: false);
            Debug.LogError($"[SaveLoadSystem] Import error: {e}");
        }
    }

    // ==========================================
    // 收集当前所有数据 → MDFSaveData
    // ==========================================
    private MDFSaveData CollectSaveData()
    {
        var save = new MDFSaveData
        {
            version = "1.0",
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        };

        // ── 记录哪些模块已激活（startPanel 已关）──
        if (moduleSystem != null)
        {
            foreach (var mod in moduleSystem.allModules)
            {
                // startPanel 为 null 或已禁用 = 已激活
                bool activated = mod.startPanel == null || !mod.startPanel.activeSelf;
                if (activated) save.activatedModules.Add(mod.moduleName);
            }
        }

        // ── 轨道 ──
        if (timeline?.allTracks != null)
        {
            foreach (var track in timeline.allTracks)
            {
                save.tracks.Add(new TrackSaveData
                {
                    trackName = track.trackName,
                    trackIndex = track.trackIndex,
                    allowOverlap = false // 若 TrackData 有该字段请替换
                });
            }
        }

        // ── Clip ──
        if (timeline?.allEvents != null)
        {
            foreach (var evt in timeline.allEvents)
            {
                var clip = new ClipSaveData
                {
                    eventName = evt.eventName,
                    trackIndex = evt.trackIndex,
                    startTime = evt.startTime,
                    duration = evt.duration
                };
                SerializeCustomData(evt.customData, clip);
                save.clips.Add(clip);
            }
        }

        return save;
    }

    // ==========================================
    // 把 customData 序列化到 ClipSaveData
    // 【扩展点】：新增类型在这里加 case
    // ==========================================
    private void SerializeCustomData(object customData, ClipSaveData clip)
    {
        if (customData == null) { clip.customDataType = ""; return; }

        switch (customData)
        {
            case CameraClipData cam:
                clip.customDataType = "Camera";
                clip.customDataJson = JsonUtility.ToJson(new CameraClipSave
                {
                    posX = cam.posX,
                    posY = cam.posY,
                    posZ = cam.posZ,
                    rotX = cam.rotX,
                    rotY = cam.rotY,
                    rotZ = cam.rotZ,
                    posXMin = cam.posXMin,
                    posXMax = cam.posXMax,
                    posYMin = cam.posYMin,
                    posYMax = cam.posYMax,
                    posZMin = cam.posZMin,
                    posZMax = cam.posZMax
                });
                break;

            case SmoothCameraClipData smCam:
                clip.customDataType = "SmoothCamera";
                var saveObj = new SmoothCameraClipSave();

                // 基础设置
                saveObj.useMidPoint = smCam.useMidPoint;
                saveObj.curveAmount = smCam.curveAmount;
                saveObj.posXMin = smCam.posXMin; saveObj.posXMax = smCam.posXMax;
                saveObj.posYMin = smCam.posYMin; saveObj.posYMax = smCam.posYMax;
                saveObj.posZMin = smCam.posZMin; saveObj.posZMax = smCam.posZMax;

                // 起点
                saveObj.point1.posX = smCam.point1.posX; saveObj.point1.posY = smCam.point1.posY; saveObj.point1.posZ = smCam.point1.posZ;
                saveObj.point1.rotX = smCam.point1.rotX; saveObj.point1.rotY = smCam.point1.rotY; saveObj.point1.rotZ = smCam.point1.rotZ;

                // 终点
                saveObj.point2.posX = smCam.point2.posX; saveObj.point2.posY = smCam.point2.posY; saveObj.point2.posZ = smCam.point2.posZ;
                saveObj.point2.rotX = smCam.point2.rotX; saveObj.point2.rotY = smCam.point2.rotY; saveObj.point2.rotZ = smCam.point2.rotZ;

                // 中间点
                saveObj.midPoint.posX = smCam.midPoint.posX; saveObj.midPoint.posY = smCam.midPoint.posY; saveObj.midPoint.posZ = smCam.midPoint.posZ;
                saveObj.midPoint.rotX = smCam.midPoint.rotX; saveObj.midPoint.rotY = smCam.midPoint.rotY; saveObj.midPoint.rotZ = smCam.midPoint.rotZ;

                clip.customDataJson = JsonUtility.ToJson(saveObj);
                break;

            case PointLightClipData lt:
                clip.customDataType = "PointLight";
                clip.customDataJson = JsonUtility.ToJson(new PointLightClipSave
                {
                    posX = lt.posX,
                    posY = lt.posY,
                    posZ = lt.posZ,
                    r = lt.color.r,
                    g = lt.color.g,
                    b = lt.color.b,
                    a = lt.color.a,
                    intensity = lt.intensity,
                    range = lt.range,
                    posXMin = lt.posXMin,
                    posXMax = lt.posXMax,
                    posYMin = lt.posYMin,
                    posYMax = lt.posYMax,
                    posZMin = lt.posZMin,
                    posZMax = lt.posZMax,
                    intensityMin = lt.intensityMin,
                    intensityMax = lt.intensityMax,
                    rangeMin = lt.rangeMin,
                    rangeMax = lt.rangeMax
                });
                break;

            case VFXClipData vfx:
                clip.customDataType = "VFX";
                clip.customDataJson = JsonUtility.ToJson(new VFXClipSave
                {
                    posX = vfx.posX,
                    posY = vfx.posY,
                    posZ = vfx.posZ,
                    rotX = vfx.rotX,
                    rotY = vfx.rotY,
                    rotZ = vfx.rotZ,
                    scaleX = vfx.scaleX,
                    scaleY = vfx.scaleY,
                    scaleZ = vfx.scaleZ,
                    r = vfx.color.r,
                    g = vfx.color.g,
                    b = vfx.color.b,
                    a = vfx.color.a,
                    playSpeed = vfx.playSpeed,
                    loop = vfx.loop,
                    vfxPrefabName = vfx.vfxPrefabName,
                    posXMin = vfx.posXMin,
                    posXMax = vfx.posXMax,
                    posYMin = vfx.posYMin,
                    posYMax = vfx.posYMax,
                    posZMin = vfx.posZMin,
                    posZMax = vfx.posZMax,
                    rotMin = vfx.rotMin,
                    rotMax = vfx.rotMax,
                    scaleMin = vfx.scaleMin,
                    scaleMax = vfx.scaleMax,
                    speedMin = vfx.speedMin,
                    speedMax = vfx.speedMax
                });
                break;

            case VoronoiShaderClipData voro:
                clip.customDataType = "Shader_Voronoi";
                clip.customDataJson = JsonUtility.ToJson(new VoronoiShaderClipSave
                {
                    fadeInDuration = voro.fadeInDuration,
                    fadeOutDuration = voro.fadeOutDuration,
                    r = voro.color.r,
                    g = voro.color.g,
                    b = voro.color.b,
                    a = voro.color.a,
                    voronoiSpeed = voro.voronoiSpeed,
                    voronoiScale = voro.voronoiScale,
                    voronoiPower = voro.voronoiPower,
                    vignetteRadiusPower = voro.vignetteRadiusPower,
                    vignetteIntensity = voro.vignetteIntensity,
                    glowPower = voro.glowPower,
                    voronoiSpeedMin = voro.voronoiSpeedMin,
                    voronoiSpeedMax = voro.voronoiSpeedMax,
                    voronoiScaleMin = voro.voronoiScaleMin,
                    voronoiScaleMax = voro.voronoiScaleMax,
                    voronoiPowerMin = voro.voronoiPowerMin,
                    voronoiPowerMax = voro.voronoiPowerMax,
                    vignetteRadiusMin = voro.vignetteRadiusMin,
                    vignetteRadiusMax = voro.vignetteRadiusMax,
                    vignetteIntMin = voro.vignetteIntMin,
                    vignetteIntMax = voro.vignetteIntMax,
                    glowMin = voro.glowMin,
                    glowMax = voro.glowMax
                });
                break;

            case ShaderClipData sh:
                // 基础 shader（没有子类特化的）
                clip.customDataType = "Shader_Base";
                clip.customDataJson = JsonUtility.ToJson(new ShaderClipBaseSave
                {
                    fadeInDuration = sh.fadeInDuration,
                    fadeOutDuration = sh.fadeOutDuration
                });
                break;
        }
    }

    // ==========================================
    // 把 MDFSaveData 还原到当前场景
    // ==========================================
    private void ApplySaveData(MDFSaveData save)
    {
        if (save == null) return;

        // 1. 清空当前所有轨道和 clip
        ClearCurrentScene();

        // 2. 按 trackIndex 排序后，逐条重建轨道
        save.tracks.Sort((a, b) => a.trackIndex.CompareTo(b.trackIndex));
        foreach (var trackSave in save.tracks)
        {
            // 告诉 moduleSystem 这个模块已激活（关闭 startPanel）
            if (moduleSystem != null)
            {
                var mod = moduleSystem.allModules.Find(m => m.moduleName == trackSave.trackName);
                if (mod != null && mod.startPanel != null)
                    mod.startPanel.SetActive(false);
            }

            // 在 timeline 里建轨道（不自动选中，避免 Tab 跳动）
            if (timeline != null)
                timeline.AddDynamicTrackSilent(trackSave.trackName, 60f);
        }

        // 3. 按 startTime 排序后，逐条重建 Clip
        save.clips.Sort((a, b) => a.startTime.CompareTo(b.startTime));
        foreach (var clipSave in save.clips)
        {
            if (timeline == null) continue;

            // 反序列化 customData
            object customData = DeserializeCustomData(clipSave);

            // 在时间轴上创建 Clip（直接调底层，不经过红线位置判断）
            TimelineEventData evt = timeline.CreateClip(
                clipSave.eventName,
                clipSave.trackIndex,
                clipSave.startTime,
                clipSave.duration);

            evt.customData = customData;

            // 创建对应的 Inspector 面板并绑定
            SpawnInspectorPanel(evt, clipSave);
        }

        // 4. 通知各子系统重建运行时对象
        lightSystem?.ForceRefresh();
        vfxSystem?.ForceRefresh();
        shaderSystem?.ForceRefresh();

        // 5. 恢复 Tab 状态（选中第一条轨道）
        if (timeline?.allTracks?.Count > 0)
            timeline.SelectTrack(0);

        Debug.Log($"[SaveLoadSystem] 导入完成，{save.tracks.Count} 条轨道，{save.clips.Count} 个 Clip");
    }

    // ==========================================
    // 反序列化 customData
    // 【扩展点】：新增类型在这里加 case
    // ==========================================
    private object DeserializeCustomData(ClipSaveData clip)
    {
        if (string.IsNullOrEmpty(clip.customDataType) ||
            string.IsNullOrEmpty(clip.customDataJson)) return null;

        switch (clip.customDataType)
        {
            case "Camera":
                {
                    var s = JsonUtility.FromJson<CameraClipSave>(clip.customDataJson);
                    return new CameraClipData
                    {
                        posX = s.posX,
                        posY = s.posY,
                        posZ = s.posZ,
                        rotX = s.rotX,
                        rotY = s.rotY,
                        rotZ = s.rotZ,
                        posXMin = s.posXMin,
                        posXMax = s.posXMax,
                        posYMin = s.posYMin,
                        posYMax = s.posYMax,
                        posZMin = s.posZMin,
                        posZMax = s.posZMax
                    };
                }

            case "SmoothCamera":
                {
                    var s = JsonUtility.FromJson<SmoothCameraClipSave>(clip.customDataJson);
                    var newData = new SmoothCameraClipData();

                    // 基础设置
                    newData.useMidPoint = s.useMidPoint;
                    newData.curveAmount = s.curveAmount;
                    newData.posXMin = s.posXMin; newData.posXMax = s.posXMax;
                    newData.posYMin = s.posYMin; newData.posYMax = s.posYMax;
                    newData.posZMin = s.posZMin; newData.posZMax = s.posZMax;

                    // 起点
                    newData.point1.posX = s.point1.posX; newData.point1.posY = s.point1.posY; newData.point1.posZ = s.point1.posZ;
                    newData.point1.rotX = s.point1.rotX; newData.point1.rotY = s.point1.rotY; newData.point1.rotZ = s.point1.rotZ;

                    // 终点
                    newData.point2.posX = s.point2.posX; newData.point2.posY = s.point2.posY; newData.point2.posZ = s.point2.posZ;
                    newData.point2.rotX = s.point2.rotX; newData.point2.rotY = s.point2.rotY; newData.point2.rotZ = s.point2.rotZ;

                    // 中间点
                    newData.midPoint.posX = s.midPoint.posX; newData.midPoint.posY = s.midPoint.posY; newData.midPoint.posZ = s.midPoint.posZ;
                    newData.midPoint.rotX = s.midPoint.rotX; newData.midPoint.rotY = s.midPoint.rotY; newData.midPoint.rotZ = s.midPoint.rotZ;

                    return newData;
                }

            case "PointLight":
                {
                    var s = JsonUtility.FromJson<PointLightClipSave>(clip.customDataJson);
                    return new PointLightClipData
                    {
                        posX = s.posX,
                        posY = s.posY,
                        posZ = s.posZ,
                        color = new Color(s.r, s.g, s.b, s.a),
                        intensity = s.intensity,
                        range = s.range,
                        posXMin = s.posXMin,
                        posXMax = s.posXMax,
                        posYMin = s.posYMin,
                        posYMax = s.posYMax,
                        posZMin = s.posZMin,
                        posZMax = s.posZMax,
                        intensityMin = s.intensityMin,
                        intensityMax = s.intensityMax,
                        rangeMin = s.rangeMin,
                        rangeMax = s.rangeMax
                    };
                }

            case "VFX":
                {
                    var s = JsonUtility.FromJson<VFXClipSave>(clip.customDataJson);
                    return new VFXClipData
                    {
                        posX = s.posX,
                        posY = s.posY,
                        posZ = s.posZ,
                        rotX = s.rotX,
                        rotY = s.rotY,
                        rotZ = s.rotZ,
                        scaleX = s.scaleX,
                        scaleY = s.scaleY,
                        scaleZ = s.scaleZ,
                        color = new Color(s.r, s.g, s.b, s.a),
                        playSpeed = s.playSpeed,
                        loop = s.loop,
                        vfxPrefabName = s.vfxPrefabName,
                        posXMin = s.posXMin,
                        posXMax = s.posXMax,
                        posYMin = s.posYMin,
                        posYMax = s.posYMax,
                        posZMin = s.posZMin,
                        posZMax = s.posZMax,
                        rotMin = s.rotMin,
                        rotMax = s.rotMax,
                        scaleMin = s.scaleMin,
                        scaleMax = s.scaleMax,
                        speedMin = s.speedMin,
                        speedMax = s.speedMax
                    };
                }

            case "Shader_Voronoi":
                {
                    var s = JsonUtility.FromJson<VoronoiShaderClipSave>(clip.customDataJson);
                    return new VoronoiShaderClipData
                    {
                        fadeInDuration = s.fadeInDuration,
                        fadeOutDuration = s.fadeOutDuration,
                        color = new Color(s.r, s.g, s.b, s.a),
                        voronoiSpeed = s.voronoiSpeed,
                        voronoiScale = s.voronoiScale,
                        voronoiPower = s.voronoiPower,
                        vignetteRadiusPower = s.vignetteRadiusPower,
                        vignetteIntensity = s.vignetteIntensity,
                        glowPower = s.glowPower,
                        voronoiSpeedMin = s.voronoiSpeedMin,
                        voronoiSpeedMax = s.voronoiSpeedMax,
                        voronoiScaleMin = s.voronoiScaleMin,
                        voronoiScaleMax = s.voronoiScaleMax,
                        voronoiPowerMin = s.voronoiPowerMin,
                        voronoiPowerMax = s.voronoiPowerMax,
                        vignetteRadiusMin = s.vignetteRadiusMin,
                        vignetteRadiusMax = s.vignetteRadiusMax,
                        vignetteIntMin = s.vignetteIntMin,
                        vignetteIntMax = s.vignetteIntMax,
                        glowMin = s.glowMin,
                        glowMax = s.glowMax
                    };
                }

            case "Shader_Base":
                {
                    var s = JsonUtility.FromJson<ShaderClipBaseSave>(clip.customDataJson);
                    return new ShaderClipData
                    {
                        fadeInDuration = s.fadeInDuration,
                        fadeOutDuration = s.fadeOutDuration
                    };
                }

            default:
                Debug.LogWarning($"[SaveLoadSystem] 未知 customDataType: {clip.customDataType}");
                return null;
        }
    }

    // ==========================================
    // 根据 customData 类型，实例化对应的 Inspector 面板 Prefab
    // ==========================================
    private void SpawnInspectorPanel(TimelineEventData evt, ClipSaveData clipSave)
    {
        if (moduleSystem == null || moduleSystem.clipPanelContainer == null) return;

        // 找到对应模块
        TrackData track = timeline.allTracks.Find(t => t.trackIndex == evt.trackIndex);
        if (track == null) return;

        var mod = moduleSystem.allModules.Find(m => m.moduleName == track.trackName);
        if (mod == null) return;

        // 选 Prefab：先查 featurePanelMaps，再用 defaultClipInspectorPrefab
        GameObject prefab = mod.defaultClipInspectorPrefab;
        foreach (var mapping in mod.featurePanelMaps)
        {
            if (mapping.featureName == evt.eventName && mapping.specificInspectorPrefab != null)
            {
                prefab = mapping.specificInspectorPrefab;
                break;
            }
        }

        if (prefab == null) return;

        GameObject panel = Instantiate(prefab, moduleSystem.clipPanelContainer);
        panel.SetActive(false);

        ClipInspectorPanel panelScript = panel.GetComponent<ClipInspectorPanel>();
        if (panelScript == null) panelScript = panel.AddComponent<ClipInspectorPanel>();
        panelScript.BindClip(evt, timeline);

        evt.inspectorPanel = panel;
    }

    // ==========================================
    // 清空当前场景所有轨道和 Clip
    // ==========================================
    private void ClearCurrentScene()
    {
        if (timeline == null) return;

        // 从后往前删，避免索引混乱
        var tracksCopy = new List<TrackData>(timeline.allTracks);
        for (int i = tracksCopy.Count - 1; i >= 0; i--)
        {
            timeline.selectedTrackIndex = tracksCopy[i].trackIndex;
            timeline.DeleteSelectedTrack();
        }

        // 以防万一，强制清空列表
        timeline.allEvents.Clear();
        timeline.allTracks.Clear();
        timeline.trackCount = 0;

        // 恢复所有模块的 startPanel
        if (moduleSystem != null)
        {
            foreach (var mod in moduleSystem.allModules)
                if (mod.startPanel != null) mod.startPanel.SetActive(true);
        }
    }

    // ==========================================
    // 文件对话框
    // ==========================================
    private string GetSavePath()
    {
#if UNITY_EDITOR
        string path = EditorUtility.SaveFilePanel(
            "导出演出数据", "", DEFAULT_FILENAME, FILE_EXT);
        return path;
#else
        // 运行时：固定存到 Application.persistentDataPath
        return Path.Combine(Application.persistentDataPath,
                            DEFAULT_FILENAME + "." + FILE_EXT);
#endif
    }

    private string GetLoadPath()
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel(
            "导入演出数据", "", FILE_EXT);
        return path;
#else
        string path = Path.Combine(Application.persistentDataPath,
                                   DEFAULT_FILENAME + "." + FILE_EXT);
        if (!File.Exists(path))
        {
            ShowStatus($"✗ 找不到文件：{path}", false);
            return "";
        }
        return path;
#endif
    }

    // ==========================================
    // 状态提示
    // ==========================================
    private void ShowStatus(string msg, bool success)
    {
        Debug.Log($"[SaveLoadSystem] {msg}");
        if (statusText == null) return;
        statusText.text = msg;
        statusText.color = success ? new Color(0f, 1f, 0.6f) : new Color(1f, 0.3f, 0.3f);
        CancelInvoke(nameof(ClearStatus));
        Invoke(nameof(ClearStatus), 4f);
    }

    private void ClearStatus() { if (statusText != null) statusText.text = ""; }
}
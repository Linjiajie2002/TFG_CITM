using UnityEngine;
using System.Collections.Generic;

// ==========================================
// VFX 播放系统
//
// 挂载位置：System_Manager 或任意常驻 GameObject
//
// 职责：
//   1. Edit + Play 模式下，Scrub/播放时根据时间轴 Clip 激活/关闭 VFX
//   2. 为每个 VFXClipData 管理一个真实的 VFX GameObject
//   3. 支持循环 / 单次播放
//   4. 支持多种 VFX Prefab（在 vfxEntries 列表里配置）
//
// 【扩展方式】：
//   如果你有新的 VFX 类型（例如 Fire、Snow），只需在 Inspector 的
//   vfxEntries 列表里加一条记录，填上 trackName 和对应 prefab 即可，
//   不需要修改任何代码。
// ==========================================
public class VFXPlaybackSystem : MonoBehaviour
{
    // ==========================================
    // VFX 类型注册表：trackName → Prefab
    // ==========================================
    [System.Serializable]
    public class VFXEntry
    {
        [Tooltip("轨道名，必须与 DynamicModuleSystem 里的 moduleName 完全一致")]
        public string trackName = "VFX";

        [Tooltip("该类型 VFX 使用的 Prefab")]
        public GameObject prefab;

        [Tooltip("同一时刻该类型最多实例数（0 = 不限制）")]
        public int maxSimultaneous = 0;
    }

    [Header("=== 时间轴引用 ===")]
    public TimelineManager timeline;

    [Header("=== VFX 类型配置表 ===")]
    public List<VFXEntry> vfxEntries = new List<VFXEntry>();

    [Header("=== VFX 挂载父节点 ===")]
    [Tooltip("所有生成的 VFX GameObject 都挂到这里，方便管理")]
    public Transform vfxContainer;

    // ── 内部：clip → (GO, ParticleSystem) ──
    private class RuntimeVFX
    {
        public GameObject go;
        public ParticleSystem ps;      // 可能为 null（VFX Graph 类型）
        public bool wasActive = false; // 上一帧是否激活
    }

    private Dictionary<TimelineEventData, RuntimeVFX> pool
        = new Dictionary<TimelineEventData, RuntimeVFX>();

    private float lastCheckedTime = -999f;
    private bool lastWasPlaying = false;

    // ==========================================
    void Start() { RebuildPool(); }

    void Update()
    {
        if (timeline == null) return;

        bool playing = timeline.musicSource != null && timeline.musicSource.isPlaying;
        float currentTime = timeline.GetCurrentTime();

        // 播放状态切换时强制刷新
        if (playing != lastWasPlaying)
        {
            lastWasPlaying = playing;
            lastCheckedTime = -999f;

            if (!playing)
            {
                // 停止演出：将所有 VFX 状态重置（保留 GO，但停止粒子）
                foreach (var kvp in pool) DeactivateVFX(kvp.Key, kvp.Value);
            }
        }

        // 每帧检查（包括 Scrub）
        bool timeChanged = Mathf.Abs(currentTime - lastCheckedTime) > 0.008f;
        if (timeChanged || playing)
        {
            lastCheckedTime = currentTime;
            RebuildPool();    // 新增/删除 Clip 后同步
            TickAll(currentTime);
        }
    }

    // ==========================================
    // 核心：每帧更新所有 VFX
    // ==========================================
    private void TickAll(float currentTime)
    {
        bool playing = timeline.musicSource != null && timeline.musicSource.isPlaying;

        // 统计每种 trackName 当前已激活数量（用于 maxSimultaneous 限制）
        var activeCounts = new Dictionary<string, int>();

        foreach (var kvp in pool)
        {
            TimelineEventData evt = kvp.Key;
            RuntimeVFX rvfx = kvp.Value;
            VFXClipData data = evt.customData as VFXClipData;
            if (data == null) continue;

            bool inRange = currentTime >= evt.startTime &&
                           currentTime < evt.startTime + evt.duration;

            // 检查 maxSimultaneous
            string trackName = GetTrackName(evt.trackIndex);
            VFXEntry entry = FindEntry(trackName);
            if (inRange && entry != null && entry.maxSimultaneous > 0)
            {
                activeCounts.TryGetValue(trackName, out int count);
                if (count >= entry.maxSimultaneous) inRange = false;
                else activeCounts[trackName] = count + 1;
            }

            if (inRange)
                ActivateVFX(evt, rvfx, data, playing);
            else
                DeactivateVFX(evt, rvfx);
        }
    }

    // ==========================================
    // 激活 VFX
    // ==========================================
    private void ActivateVFX(TimelineEventData evt, RuntimeVFX rvfx, VFXClipData data, bool playing)
    {
        if (rvfx.go == null) return;

        // 刚进入区间
        if (!rvfx.wasActive)
        {
            rvfx.go.SetActive(true);
            rvfx.wasActive = true;

            if (rvfx.ps != null)
            {
                rvfx.ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                rvfx.ps.Play();
            }
        }

        // 每帧同步参数（Slider 拖动时实时反映）
        VFXClipInspectorPanel.ApplyVFXData(rvfx.go, data);

        // 单次播放：粒子播完后停止发射（但 GO 保持激活到 clip 结束）
        if (!data.loop && rvfx.ps != null && !rvfx.ps.IsAlive())
        {
            rvfx.ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    // ==========================================
    // 关闭 VFX
    // ==========================================
    private void DeactivateVFX(TimelineEventData evt, RuntimeVFX rvfx)
    {
        if (rvfx.go == null) return;

        if (rvfx.wasActive)
        {
            if (rvfx.ps != null)
                rvfx.ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            rvfx.go.SetActive(false);
            rvfx.wasActive = false;
        }
    }

    // ==========================================
    // 同步对象池：新 Clip 创建 GO，删除的 Clip 销毁 GO
    // ==========================================
    public void RebuildPool()
    {
        if (timeline?.allEvents == null) return;

        // 新增
        foreach (var evt in timeline.allEvents)
        {
            if (!(evt.customData is VFXClipData data)) continue;
            if (pool.ContainsKey(evt) && pool[evt].go != null) continue;

            string trackName = GetTrackName(evt.trackIndex);
            VFXEntry entry = FindEntry(trackName);
            if (entry?.prefab == null) continue;

            GameObject go = Instantiate(entry.prefab, vfxContainer);
            go.name = $"{evt.eventName}_{evt.trackIndex}_VFX";
            go.SetActive(false);

            var rvfx = new RuntimeVFX
            {
                go = go,
                ps = go.GetComponentInChildren<ParticleSystem>()
            };

            pool[evt] = rvfx;
            data.runtimeInstance = go;
            data.vfxPrefabName = entry.prefab.name;
        }

        // 清理已删除的 Clip
        var toRemove = new List<TimelineEventData>();
        foreach (var kvp in pool)
        {
            if (!timeline.allEvents.Contains(kvp.Key))
            {
                if (kvp.Value.go != null) Destroy(kvp.Value.go);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var k in toRemove) pool.Remove(k);
    }

    // ==========================================
    // 工具
    // ==========================================
    private string GetTrackName(int trackIndex)
    {
        if (timeline?.allTracks == null) return "";
        var track = timeline.allTracks.Find(t => t.trackIndex == trackIndex);
        return track?.trackName ?? "";
    }

    private VFXEntry FindEntry(string trackName)
    {
        foreach (var e in vfxEntries)
            if (!string.IsNullOrEmpty(e.trackName) && trackName.StartsWith(e.trackName))
                return e;
        return null;
    }

    // 供外部（新建 Clip 后）调用立即刷新
    public void ForceRefresh()
    {
        lastCheckedTime = -999f;
        RebuildPool();
    }
}
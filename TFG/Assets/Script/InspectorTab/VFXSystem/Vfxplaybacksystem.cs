using UnityEngine;
using System.Collections.Generic;

// ==========================================
// VFX 播放系统（修复版）
//
// 修复：
//   1. 删除 Clip 时立即销毁对应 VFX（不等下一帧）
//   2. 演出开始时（Edit play 或正式演出）立即清空所有残留 VFX
//
// 使用方式：
//   在 TimelineManager.DeleteSelectedClip() 里调用 vfxSystem.OnClipDeleted(evt)
//   在播放开始时调用 vfxSystem.OnPlayStarted()
// ==========================================
public class VFXPlaybackSystem : MonoBehaviour
{
    [System.Serializable]
    public class VFXEntry
    {
        [Tooltip("轨道名，必须与 DynamicModuleSystem 里的 moduleName 一致")]
        public string trackName = "VFX";

        [Tooltip("该类型 VFX 使用的 Prefab")]
        public GameObject prefab;

        [Tooltip("同一时刻最多实例数，0 = 不限制")]
        public int maxSimultaneous = 0;
    }

    [Header("=== 时间轴引用 ===")]
    public TimelineManager timeline;

    [Header("=== VFX 类型配置表 ===")]
    public List<VFXEntry> vfxEntries = new List<VFXEntry>();

    [Header("=== VFX 挂载父节点 ===")]
    public Transform vfxContainer;

    // ── 内部对象池 ──
    private class RuntimeVFX
    {
        public GameObject go;
        public ParticleSystem ps;
        public bool wasActive = false;
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

        // 播放状态切换
        if (playing != lastWasPlaying)
        {
            lastWasPlaying = playing;

            if (playing)
            {
                // 【修复2】：演出/Edit 开始播放时，立即清空所有 VFX 残留
                DeactivateAll();
                lastCheckedTime = -999f;
            }
            else
            {
                // 停止时也清空
                DeactivateAll();
            }
        }

        bool timeChanged = Mathf.Abs(currentTime - lastCheckedTime) > 0.008f;
        if (timeChanged || playing)
        {
            lastCheckedTime = currentTime;
            RebuildPool();
            TickAll(currentTime);
        }
    }

    // ==========================================
    // 【修复2】：供 TimelineManager 的播放开始时调用
    // ==========================================
    public void OnPlayStarted()
    {
        DeactivateAll();
        lastCheckedTime = -999f;
    }

    // ==========================================
    // 【修复1】：Clip 被删除时立即调用
    // 在 TimelineManager.DeleteSelectedClip() 里加上：
    //   if (moduleSystem?.GetComponent<VFXPlaybackSystem>() != null) ...
    // 或者直接在 TimelineManager 里持有 VFXPlaybackSystem 引用
    // ==========================================
    public void OnClipDeleted(TimelineEventData evt)
    {
        if (evt == null) return;
        if (pool.TryGetValue(evt, out RuntimeVFX rvfx))
        {
            if (rvfx.go != null) Destroy(rvfx.go);
            pool.Remove(evt);
        }
    }

    // ==========================================
    private void TickAll(float currentTime)
    {
        bool playing = timeline.musicSource != null && timeline.musicSource.isPlaying;

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

            if (inRange) ActivateVFX(evt, rvfx, data, playing);
            else DeactivateVFX(rvfx);
        }
    }

    private void ActivateVFX(TimelineEventData evt, RuntimeVFX rvfx, VFXClipData data, bool playing)
    {
        if (rvfx.go == null) return;

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

        VFXClipInspectorPanel.ApplyVFXData(rvfx.go, data);

        if (!data.loop && rvfx.ps != null && !rvfx.ps.IsAlive())
            rvfx.ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void DeactivateVFX(RuntimeVFX rvfx)
    {
        if (rvfx.go == null || !rvfx.wasActive) return;
        if (rvfx.ps != null) rvfx.ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        rvfx.go.SetActive(false);
        rvfx.wasActive = false;
    }

    // 关闭所有 VFX
    private void DeactivateAll()
    {
        foreach (var kvp in pool)
            DeactivateVFX(kvp.Value);
    }

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

            var rvfx = new RuntimeVFX { go = go, ps = go.GetComponentInChildren<ParticleSystem>() };
            pool[evt] = rvfx;
            data.runtimeInstance = go;
            data.vfxPrefabName = entry.prefab.name;
        }

        // 【修复1】：清理已不存在于 allEvents 的条目，立即销毁 GO
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

    public void ForceRefresh()
    {
        lastCheckedTime = -999f;
        RebuildPool();
    }

    private string GetTrackName(int trackIndex)
    {
        var track = timeline?.allTracks?.Find(t => t.trackIndex == trackIndex);
        return track?.trackName ?? "";
    }

    private VFXEntry FindEntry(string trackName)
    {
        foreach (var e in vfxEntries)
            if (!string.IsNullOrEmpty(e.trackName) && trackName.StartsWith(e.trackName))
                return e;
        return null;
    }
}
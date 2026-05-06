using UnityEngine;
using System.Collections.Generic;

// ==========================================
// VFX 播放系统（FeatureName 匹配版）
//
// 升级：
//   不再根据轨道(TrackName)生成特效，而是根据方块的名称(FeatureName / eventName)
//   实现同一轨道内混排多种不同的 VFX 特效！
// ==========================================
public class VFXPlaybackSystem : MonoBehaviour
{
    [System.Serializable]
    public class VFXEntry
    {
        [Tooltip("Clip的名称，必须与 DynamicModuleSystem -> Feature Panel Maps 里的 Feature Name 一致")]
        public string featureName = "NewVFX";

        [Tooltip("该类型 VFX 使用的 3D 粒子 Prefab")]
        public GameObject prefab;

        [Tooltip("同一时刻同类特效最多实例数，0 = 不限制")]
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
                // 演出/Edit 开始播放时，立即清空所有 VFX 残留
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
    // 供 TimelineManager 的播放开始时调用
    // ==========================================
    public void OnPlayStarted()
    {
        DeactivateAll();
        lastCheckedTime = -999f;
    }

    // ==========================================
    // Clip 被删除时立即调用
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

            // 🌟 核心升级：通过方块名字 (FeatureName) 检查限制
            string featureName = evt.eventName;
            VFXEntry entry = FindEntry(featureName);

            if (inRange && entry != null && entry.maxSimultaneous > 0)
            {
                activeCounts.TryGetValue(featureName, out int count);
                if (count >= entry.maxSimultaneous) inRange = false;
                else activeCounts[featureName] = count + 1;
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

        // 调用面板里的方法同步数据（位置、缩放等）
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

    private void DeactivateAll()
    {
        foreach (var kvp in pool)
            DeactivateVFX(kvp.Value);
    }

    // ==========================================
    public void RebuildPool()
    {
        if (timeline?.allEvents == null) return;

        foreach (var evt in timeline.allEvents)
        {
            if (!(evt.customData is VFXClipData data)) continue;
            if (pool.ContainsKey(evt) && pool[evt].go != null) continue;

            // 🌟 核心升级：读取方块本身的名字
            string featureName = evt.eventName;
            VFXEntry entry = FindEntry(featureName);
            if (entry?.prefab == null) continue;

            GameObject go = Instantiate(entry.prefab, vfxContainer);
            go.name = $"{featureName}_{evt.trackIndex}_VFX";
            go.SetActive(false);

            var rvfx = new RuntimeVFX { go = go, ps = go.GetComponentInChildren<ParticleSystem>() };
            pool[evt] = rvfx;
            data.runtimeInstance = go;
            data.vfxPrefabName = entry.prefab.name;
        }

        // 清理已不存在于 allEvents 的条目，立即销毁 GO
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

    // 🌟 核心升级：根据 featureName 在配置表中寻找对应的预制体
    private VFXEntry FindEntry(string featureName)
    {
        foreach (var e in vfxEntries)
        {
            // 精确匹配方块名字
            if (!string.IsNullOrEmpty(e.featureName) && e.featureName == featureName)
                return e;
        }
        return null;
    }
}
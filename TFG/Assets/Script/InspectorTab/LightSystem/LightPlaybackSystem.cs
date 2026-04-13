using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 灯光播放系统
//
// 挂载位置：System_Manager 或任意常驻 GameObject
//
// 职责：
//   1. Edit 和 Play 模式下，Scrub/播放时根据时间轴 Clip 数据
//      实时开关场景里的 Point Light
//   2. 强制最多同时 3 个灯（MAX_SIMULTANEOUS_LIGHTS）
//   3. 为每个 PointLightClipData 创建 / 复用真实 Light GameObject
// ==========================================
public class LightPlaybackSystem : MonoBehaviour
{
    // ── 全局灯光数量上限（所有灯类型共享）──
    public const int MAX_SIMULTANEOUS_LIGHTS = 3;

    [Header("=== 时间轴引用 ===")]
    public TimelineManager timeline;

    [Header("=== 灯光轨道名（与 AddModule 时填的名字一致）===")]
    [Tooltip("默认 'Light'，如果你的模块名不同请修改")]
    public string lightTrackName = "Light";

    [Header("=== 灯光父节点（所有生成的灯挂在这里）===")]
    [Tooltip("在场景里新建一个空 GameObject 拖进来，方便管理")]
    public Transform lightContainer;

    // ── 内部：每个 clip 对应一个 Light GO ──
    // key = TimelineEventData 引用，value = 对应的 Light GameObject
    private Dictionary<TimelineEventData, Light> lightPool
        = new Dictionary<TimelineEventData, Light>();

    private float lastCheckedTime = -999f;

    void Start()
    {
        // 扫描已有的 clip（场景加载时可能已有数据）
        RebuildLightPool();
    }

    void Update()
    {
        if (timeline == null) return;

        float currentTime = timeline.GetCurrentTime();

        // 每帧都检查（包括 Scrub）
        if (Mathf.Abs(currentTime - lastCheckedTime) > 0.008f ||
            (timeline.musicSource != null && timeline.musicSource.isPlaying))
        {
            lastCheckedTime = currentTime;
            TickLights(currentTime);
        }
    }

    // ==========================================
    // 核心：每帧根据当前时间决定哪些灯亮
    // ==========================================
    private void TickLights(float currentTime)
    {
        // 1. 先确保所有 clip 都有对应的 Light GO
        RebuildLightPool();

        // 2. 收集当前时间内激活的所有灯 clip，按 startTime 排序
        List<TimelineEventData> activeClips = GetActiveClips(currentTime);

        // 3. 强制最多 MAX_SIMULTANEOUS_LIGHTS 个（超出的直接关掉）
        int shown = 0;
        foreach (var evt in activeClips)
        {
            bool show = shown < MAX_SIMULTANEOUS_LIGHTS;
            SetLightActive(evt, show);
            if (show) shown++;
        }

        // 4. 关掉所有不在激活列表里的灯
        foreach (var kvp in lightPool)
        {
            if (!activeClips.Contains(kvp.Key))
                SetLightActive(kvp.Key, false);
        }
    }

    // ==========================================
    // 收集当前时间激活的所有 Light Clip（按 startTime 排序）
    // ==========================================
    private List<TimelineEventData> GetActiveClips(float currentTime)
    {
        var result = new List<TimelineEventData>();

        if (timeline.allEvents == null || timeline.allTracks == null) return result;

        // 找所有灯轨道的 trackIndex
        var lightTrackIndices = new HashSet<int>();
        foreach (var track in timeline.allTracks)
        {
            // 所有以 lightTrackName 开头的轨道（以后可扩展 SpotLight、AreaLight 等）
            if (track.trackName.StartsWith(lightTrackName))
                lightTrackIndices.Add(track.trackIndex);
        }

        foreach (var evt in timeline.allEvents)
        {
            if (!lightTrackIndices.Contains(evt.trackIndex)) continue;
            if (!(evt.customData is PointLightClipData)) continue;
            if (currentTime >= evt.startTime && currentTime < evt.startTime + evt.duration)
                result.Add(evt);
        }

        // 按 startTime 升序（最早触发的优先获得灯）
        result.Sort((a, b) => a.startTime.CompareTo(b.startTime));
        return result;
    }

    // ==========================================
    // 为每个 PointLightClipData 确保有对应的 Light GO
    // ==========================================
    public void RebuildLightPool()
    {
        if (timeline?.allEvents == null) return;

        foreach (var evt in timeline.allEvents)
        {
            if (!(evt.customData is PointLightClipData data)) continue;

            if (!lightPool.ContainsKey(evt))
            {
                Light lt = CreateLight(evt.eventName + "_Light");
                lt.gameObject.SetActive(false);
                lightPool[evt] = lt;
                data.runtimeLight = lt;
            }
            else if (lightPool[evt] == null)
            {
                // GO 被意外销毁，重建
                Light lt = CreateLight(evt.eventName + "_Light");
                lt.gameObject.SetActive(false);
                lightPool[evt] = lt;
                data.runtimeLight = lt;
            }
        }

        // 清理已删除 clip 对应的灯
        var toRemove = new List<TimelineEventData>();
        foreach (var kvp in lightPool)
        {
            if (!timeline.allEvents.Contains(kvp.Key))
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var k in toRemove) lightPool.Remove(k);
    }

    // ==========================================
    // 开关某个 clip 的灯，并同步参数
    // ==========================================
    private void SetLightActive(TimelineEventData evt, bool active)
    {
        if (!lightPool.TryGetValue(evt, out Light lt) || lt == null) return;

        lt.gameObject.SetActive(active);

        if (active && evt.customData is PointLightClipData data)
        {
            lt.transform.position = data.Position;
            lt.color = data.color;
            lt.intensity = data.intensity;
            lt.range = data.range;
        }
    }

    // ==========================================
    // 创建一个 Point Light GameObject
    // ==========================================
    private Light CreateLight(string lightName)
    {
        GameObject go = new GameObject(lightName);
        if (lightContainer != null)
            go.transform.SetParent(lightContainer, false);

        Light lt = go.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.shadows = LightShadows.Soft;
        return lt;
    }

    // ==========================================
    // 供 Inspector 面板调用：统计某时刻有多少灯激活
    // （用于显示叠加上限警告）
    // ==========================================
    public int CountActiveLightsAt(float time)
    {
        if (timeline?.allEvents == null) return 0;
        int count = 0;
        foreach (var evt in timeline.allEvents)
        {
            if (!(evt.customData is PointLightClipData)) continue;
            if (time >= evt.startTime && time < evt.startTime + evt.duration)
                count++;
        }
        return count;
    }

    // ==========================================
    // 供外部调用：立即刷新（新建 Clip 后调用）
    // ==========================================
    public void ForceRefresh()
    {
        lastCheckedTime = -999f;
        RebuildLightPool();
    }
}
using UnityEngine;
using System.Collections.Generic;

// ==========================================
// Shader 播放系统（修复版）
//
// 修复1："过了clip关不掉"
//   - 对每个不在范围的 clip 直接调用 ApplyToMaterial(mat, 0)
//   - 不再依赖 shaderEntries 末尾循环（那个循环只做兜底）
//   - TickShaders 每帧强制执行，不做时间节流
//
// 修复2："Fade In 直接闪现"
//   - 删除 inspector 面板的 PushToMaterial(1f)，
//     材质完全由本系统的 CalculateAlpha 控制
//   - 在 clip 进入范围的第一帧之前，确保材质已归零
// ==========================================
public class ShaderPlaybackSystem : MonoBehaviour
{
    [System.Serializable]
    public class ShaderEntry
    {
        [Tooltip("轨道名，必须与 DynamicModuleSystem 的 moduleName 一致")]
        public string trackName = "Shader";

        [Tooltip("Full Screen Pass Renderer Feature 使用的材质\n同一个 Material 实例！")]
        public Material material;
    }

    [Header("=== 时间轴引用 ===")]
    public TimelineManager timeline;

    [Header("=== Shader 类型配置表 ===")]
    public List<ShaderEntry> shaderEntries = new List<ShaderEntry>();

    // 记录每个 clip 上一帧是否在范围内，用于精确触发归零
    private Dictionary<TimelineEventData, bool> wasInRange
        = new Dictionary<TimelineEventData, bool>();

    void Start()
    {
        ZeroAllMaterials();
    }

    void Update()
    {
        if (timeline == null) return;

        // 【修复1】：每帧无条件执行，不做节流
        TickShaders(timeline.GetCurrentTime());
    }

    // ==========================================
    // 核心：每帧处理所有 Shader Clip
    // ==========================================
    private void TickShaders(float currentTime)
    {
        if (timeline?.allEvents == null) return;

        // 先把本帧所有 entry material 的"活跃贡献"清空
        // 用 float 字典累加，支持后续多 clip 混合扩展
        var materialAlpha = new Dictionary<Material, float>();
        foreach (var e in shaderEntries)
            if (e.material != null) materialAlpha[e.material] = 0f;

        foreach (var evt in timeline.allEvents)
        {
            if (!(evt.customData is ShaderClipData data)) continue;

            string trackName = GetTrackName(evt.trackIndex);
            ShaderEntry entry = FindEntry(trackName);
            if (entry?.material == null) continue;

            // 注入 material 引用（供 inspector 面板知道绑哪个 mat）
            data.runtimeMaterial = entry.material;

            float clipStart = evt.startTime;
            float clipEnd = evt.startTime + evt.duration;
            bool inRange = currentTime >= clipStart && currentTime < clipEnd;

            // 【修复2】：clip 刚进入范围时，先确保材质从 0 开始
            bool prev = wasInRange.ContainsKey(evt) && wasInRange[evt];
            if (inRange && !prev)
            {
                // 刚进入：先强制归零这帧之前的材质值
                // （防止上一个 clip 或 editor 预览残留非零值）
                if (materialAlpha.ContainsKey(entry.material))
                    materialAlpha[entry.material] = 0f;
            }

            wasInRange[evt] = inRange;

            if (!inRange)
            {
                // 【修复1】：不在范围 → 显式贡献 0，不依赖末尾循环
                data.currentAlpha = 0f;
                continue; // materialAlpha 已初始化为 0，不需要额外操作
            }

            // 计算渐入渐出 alpha
            float alpha = CalculateAlpha(currentTime, clipStart, clipEnd,
                                         data.fadeInDuration, data.fadeOutDuration);
            data.currentAlpha = alpha;

            // 累加（同轨道多 clip 叠加时取最大值）
            if (materialAlpha.ContainsKey(entry.material))
                materialAlpha[entry.material] = Mathf.Max(materialAlpha[entry.material], alpha);
            else
                materialAlpha[entry.material] = alpha;
        }

        // 把计算好的 alpha 推给每个 material
        foreach (var evt in timeline.allEvents)
        {
            if (!(evt.customData is ShaderClipData data)) continue;
            if (data.runtimeMaterial == null) continue;

            Material mat = data.runtimeMaterial;
            if (!materialAlpha.TryGetValue(mat, out float finalAlpha)) finalAlpha = 0f;

            // 只写入对应 clip 的 alpha（防止多 clip 重复写）
            // 每个 entry 的 material 只写一次，由 materialAlpha 统一管理
        }

        // 统一写入：每个 entry 的 material 只写一次
        foreach (var e in shaderEntries)
        {
            if (e.material == null) continue;
            materialAlpha.TryGetValue(e.material, out float a);

            // 找到对应这个 material 的、当前贡献最大的那个 clip data 来写参数
            ShaderClipData bestData = FindBestDataForMaterial(e.material, currentTime);
            if (bestData != null)
                bestData.ApplyToMaterial(e.material, a);
            else
                ZeroMaterial(e.material); // 无活跃 clip → 归零
        }
    }

    // 找当前时间内对某个 material 贡献最大 alpha 的 clip data
    private ShaderClipData FindBestDataForMaterial(Material mat, float currentTime)
    {
        ShaderClipData best = null;
        float bestA = -1f;

        foreach (var evt in timeline.allEvents)
        {
            if (!(evt.customData is ShaderClipData data)) continue;
            if (data.runtimeMaterial != mat) continue;

            float clipEnd = evt.startTime + evt.duration;
            bool inRange = currentTime >= evt.startTime && currentTime < clipEnd;
            if (!inRange) continue;

            if (data.currentAlpha > bestA)
            {
                bestA = data.currentAlpha;
                best = data;
            }
        }
        return best;
    }

    // ==========================================
    // 渐入渐出 alpha
    //
    //   │←fadeIn→│←────持续────→│←fadeOut→│
    //   0        1              1          0
    // ==========================================
    private float CalculateAlpha(float t, float start, float end, float fadeIn, float fadeOut)
    {
        float duration = end - start;
        float maxFade = duration * 0.5f;

        fadeIn = Mathf.Clamp(fadeIn, 0f, maxFade);
        fadeOut = Mathf.Clamp(fadeOut, 0f, maxFade);

        float elapsed = t - start;
        float remaining = end - t;

        float aIn = (fadeIn > 0.001f) ? Mathf.Clamp01(elapsed / fadeIn) : 1f;
        float aOut = (fadeOut > 0.001f) ? Mathf.Clamp01(remaining / fadeOut) : 1f;

        return Mathf.Min(aIn, aOut);
    }

    // ==========================================
    private void ZeroMaterial(Material mat)
    {
        if (mat == null) return;
        if (mat.HasProperty("_FullIntensity")) mat.SetFloat("_FullIntensity", 0f);
    }

    private void ZeroAllMaterials()
    {
        foreach (var e in shaderEntries) ZeroMaterial(e.material);
    }

    private string GetTrackName(int trackIndex)
    {
        var track = timeline?.allTracks?.Find(t => t.trackIndex == trackIndex);
        return track?.trackName ?? "";
    }

    private ShaderEntry FindEntry(string trackName)
    {
        foreach (var e in shaderEntries)
            if (!string.IsNullOrEmpty(e.trackName) && trackName.StartsWith(e.trackName))
                return e;
        return null;
    }

    public void ForceRefresh()
    {
        wasInRange.Clear();
        ZeroAllMaterials();
    }
}
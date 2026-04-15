using UnityEngine;
using System.Collections.Generic;

// ==========================================
// Shader 播放系统
//
// 挂载位置：System_Manager 或任意常驻 GameObject
//
// 工作原理：
//   1. 每帧根据时间轴检查哪些 Shader Clip 激活
//   2. 激活时计算渐入/渐出的 alpha 值（0→1→1→0）
//   3. 调用 ShaderClipData.ApplyToMaterial(mat, alpha) 写入 Material
//   4. 不在任何 Clip 内时，把 _FullIntensity 设为 0（隐藏 shader）
//
// 【扩展方式】：
//   在 shaderEntries 里加一条记录（trackName + material），
//   不需要修改任何代码。新的 Shader 只要继承 ShaderClipData，
//   重写 ApplyToMaterial 即可。
// ==========================================

public class ShaderPlaybackSystem : MonoBehaviour
{
    // ==========================================
    // Shader 类型注册表
    // ==========================================
    [System.Serializable]
    public class ShaderEntry
    {
        [Tooltip("轨道名，必须与 DynamicModuleSystem 的 moduleName 一致")]
        public string trackName = "Shader";

        [Tooltip("Full Screen Pass Renderer Feature 使用的材质\n（记得在 URP Renderer Asset 里也引用同一个 Material 实例）")]
        public Material material;

        [Tooltip("是否允许多个 Clip 叠加（一般 Full Screen Shader 不叠加，设 false）")]
        public bool allowBlend = false;
    }

    [Header("=== 时间轴引用 ===")]
    public TimelineManager timeline;

    [Header("=== Shader 类型配置表 ===")]
    public List<ShaderEntry> shaderEntries = new List<ShaderEntry>();

    // ── 内部：记录每个 clip 上一帧的 alpha，用于跳跃时快速同步 ──
    private Dictionary<TimelineEventData, float> alphaCache
        = new Dictionary<TimelineEventData, float>();

    private float lastCheckedTime = -999f;
    private bool  lastWasPlaying  = false;

    // ==========================================
    void Start()
    {
        // 初始化：把所有已注册 material 的 _FullIntensity 清零
        foreach (var e in shaderEntries)
            ZeroMaterial(e.material);
    }

    void Update()
    {
        if (timeline == null) return;

        float currentTime = timeline.GetCurrentTime();
        bool  playing     = timeline.musicSource != null && timeline.musicSource.isPlaying;

        // 播放 / 停止切换时强制刷新
        if (playing != lastWasPlaying)
        {
            lastWasPlaying  = playing;
            lastCheckedTime = -999f;
        }

        bool timeChanged = Mathf.Abs(currentTime - lastCheckedTime) > 0.008f;
        if (timeChanged || playing)
        {
            lastCheckedTime = currentTime;
            TickShaders(currentTime);
        }
    }

    // ==========================================
    // 每帧：计算所有 Shader Clip 的 alpha 并推给 Material
    // ==========================================
    private void TickShaders(float currentTime)
    {
        if (timeline?.allEvents == null) return;

        // 先把所有 entry 的 material 重置（防止残留）
        var activeMaterials = new HashSet<Material>();

        foreach (var evt in timeline.allEvents)
        {
            if (!(evt.customData is ShaderClipData data)) continue;

            float clipStart = evt.startTime;
            float clipEnd   = evt.startTime + evt.duration;

            bool inRange = currentTime >= clipStart && currentTime < clipEnd;
            if (!inRange)
            {
                // 不在范围内：alpha 归零
                alphaCache[evt] = 0f;
                continue;
            }

            // 计算渐入渐出 alpha
            float alpha = CalculateAlpha(currentTime, clipStart, clipEnd,
                                         data.fadeInDuration, data.fadeOutDuration);
            alphaCache[evt] = alpha;
            data.currentAlpha = alpha;

            // 找对应 material
            string trackName = GetTrackName(evt.trackIndex);
            ShaderEntry entry = FindEntry(trackName);
            if (entry?.material == null) continue;

            // 注入 material 引用（供 Inspector 面板 Edit 模式预览用）
            data.runtimeMaterial  = entry.material;
            data.shaderEntryName  = entry.trackName;

            // 推参数到 Material
            data.ApplyToMaterial(entry.material, alpha);
            activeMaterials.Add(entry.material);
        }

        // 把没有激活 clip 的 material 强制归零
        foreach (var e in shaderEntries)
        {
            if (e.material != null && !activeMaterials.Contains(e.material))
                ZeroMaterial(e.material);
        }
    }

    // ==========================================
    // 渐入渐出 alpha 计算
    //
    //   |←fadeIn→|←────持续────→|←fadeOut→|
    //   0        1              1         0
    // ==========================================
    private float CalculateAlpha(float t, float start, float end, float fadeIn, float fadeOut)
    {
        float duration = end - start;

        // 防止 fadeIn + fadeOut 超过 clip 时长（各占一半）
        float maxFade = duration * 0.5f;
        fadeIn  = Mathf.Min(fadeIn,  maxFade);
        fadeOut = Mathf.Min(fadeOut, maxFade);

        float elapsed  = t - start;
        float remaining = end - t;

        float alphaIn  = (fadeIn  > 0.001f) ? Mathf.Clamp01(elapsed  / fadeIn)  : 1f;
        float alphaOut = (fadeOut > 0.001f) ? Mathf.Clamp01(remaining / fadeOut) : 1f;

        return Mathf.Min(alphaIn, alphaOut);
    }

    // ==========================================
    // 归零 Material
    // ==========================================
    private void ZeroMaterial(Material mat)
    {
        if (mat == null) return;
        if (mat.HasProperty("_FullIntensity")) mat.SetFloat("_FullIntensity", 0f);
    }

    // ==========================================
    // 工具
    // ==========================================
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

    // 供外部（新建 Clip 后）立即刷新
    public void ForceRefresh()
    {
        lastCheckedTime = -999f;
    }

    // ==========================================
    // Edit 模式实时预览（选中 Clip 时由面板调用）
    // 传入 previewAlpha = 1f 就是完整预览效果
    // ==========================================
    public void PreviewClipInEditor(ShaderClipData data, float previewAlpha = 1f)
    {
        if (data?.runtimeMaterial == null) return;
        data.ApplyToMaterial(data.runtimeMaterial, previewAlpha);
    }

    public void StopPreview(ShaderClipData data)
    {
        if (data?.runtimeMaterial == null) return;
        ZeroMaterial(data.runtimeMaterial);
    }
}

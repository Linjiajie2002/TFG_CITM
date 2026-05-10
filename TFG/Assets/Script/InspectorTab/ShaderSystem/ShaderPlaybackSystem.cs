using UnityEngine;
using System.Collections.Generic;

public class ShaderPlaybackSystem : MonoBehaviour
{
    [System.Serializable]
    public class ShaderEntry
    {
        [Tooltip("轨道名，与 DynamicModuleSystem moduleName 一致（给 ShaderClipData 用）")]
        public string trackName = "Shader";
        public Material material;
    }

    [System.Serializable]
    public class OutlineEntry
    {
        [Tooltip("Feature Name，与 DynamicModuleSystem Feature Name 一致（给 OutlineClipData 用）")]
        public string featureName = "Outline";
        public Material material;
    }

    [Header("=== 时间轴引用 ===")]
    public TimelineManager timeline;

    [Header("=== Shader 配置表（trackName 匹配）===")]
    public List<ShaderEntry> shaderEntries = new List<ShaderEntry>();

    [Header("=== Outline 配置表（featureName 匹配）===")]
    public List<OutlineEntry> outlineEntries = new List<OutlineEntry>();

    private Dictionary<TimelineEventData, bool> wasInRange = new Dictionary<TimelineEventData, bool>();

    void Start() { ZeroAllMaterials(); }
    void Update() { if (timeline != null) TickShaders(timeline.GetCurrentTime()); }

    private void TickShaders(float currentTime)
    {
        if (timeline?.allEvents == null) return;

        // ── ShaderClipData：trackName 匹配，原逻辑不动 ──
        var shaderAlpha = new Dictionary<Material, float>();
        var shaderBest = new Dictionary<Material, VoronoiShaderClipData>();

        foreach (var evt in timeline.allEvents)
        {
            if (!(evt.customData is VoronoiShaderClipData data)) continue;
            ShaderEntry entry = FindShaderEntry(GetTrackName(evt.trackIndex));
            if (entry?.material == null) continue;

            data.runtimeMaterial = entry.material;
            if (!shaderAlpha.ContainsKey(entry.material)) shaderAlpha[entry.material] = 0f;

            float clipEnd = evt.startTime + evt.duration;
            bool inRange = currentTime >= evt.startTime && currentTime < clipEnd;

            bool prev = wasInRange.ContainsKey(evt) && wasInRange[evt];
            if (inRange && !prev) shaderAlpha[entry.material] = 0f;
            wasInRange[evt] = inRange;

            if (!inRange) { data.currentAlpha = 0f; continue; }

            float alpha = CalculateAlpha(currentTime, evt.startTime, clipEnd,
                                         data.fadeInDuration, data.fadeOutDuration);
            data.currentAlpha = alpha;

            if (alpha > shaderAlpha[entry.material])
            {
                shaderAlpha[entry.material] = alpha;
                shaderBest[entry.material] = data;
            }
        }

        foreach (var kvp in shaderAlpha)
        {
            shaderBest.TryGetValue(kvp.Key, out VoronoiShaderClipData best);
            if (best != null) best.ApplyToMaterial(kvp.Key, kvp.Value);
            else ZeroShaderMaterial(kvp.Key);
        }

        // ── OutlineClipData：featureName 匹配，clip 外用阈值隐藏 ──
        var outlineActive = new Dictionary<Material, OutlineClipData>();
        foreach (var e in outlineEntries)
            if (e.material != null) outlineActive[e.material] = null;

        foreach (var evt in timeline.allEvents)
        {
            if (!(evt.customData is OutlineClipData data)) continue;
            OutlineEntry entry = FindOutlineEntry(evt.eventName);
            if (entry?.material == null) continue;

            data.runtimeMaterial = entry.material;

            float clipEnd = evt.startTime + evt.duration;
            bool inRange = currentTime >= evt.startTime && currentTime < clipEnd;
            wasInRange[evt] = inRange;

            if (inRange) outlineActive[entry.material] = data;
        }

        foreach (var kvp in outlineActive)
        {
            if (kvp.Value != null) kvp.Value.ApplyToMaterial(kvp.Key);
            else HideOutlineMaterial(kvp.Key);
        }
    }

    private float CalculateAlpha(float t, float start, float end, float fadeIn, float fadeOut)
    {
        float maxFade = (end - start) * 0.5f;
        fadeIn = Mathf.Clamp(fadeIn, 0f, maxFade);
        fadeOut = Mathf.Clamp(fadeOut, 0f, maxFade);
        float aIn = fadeIn > 0.001f ? Mathf.Clamp01((t - start) / fadeIn) : 1f;
        float aOut = fadeOut > 0.001f ? Mathf.Clamp01((end - t) / fadeOut) : 1f;
        return Mathf.Min(aIn, aOut);
    }

    private void ZeroShaderMaterial(Material mat)
    {
        if (mat == null) return;
        if (mat.HasProperty("_FullIntensity")) mat.SetFloat("_FullIntensity", 0f);
    }

    private void HideOutlineMaterial(Material mat)
    {
        if (mat == null) return;
        if (mat.HasProperty("_ColorThreshold")) mat.SetFloat("_ColorThreshold", 2f);
        if (mat.HasProperty("_NormalThreshold")) mat.SetFloat("_NormalThreshold", 3f);
    }

    private void ZeroAllMaterials()
    {
        foreach (var e in shaderEntries) ZeroShaderMaterial(e.material);
        foreach (var e in outlineEntries) HideOutlineMaterial(e.material);
    }

    private string GetTrackName(int trackIndex)
    {
        var track = timeline?.allTracks?.Find(t => t.trackIndex == trackIndex);
        return track?.trackName ?? "";
    }

    private ShaderEntry FindShaderEntry(string trackName)
    {
        foreach (var e in shaderEntries)
            if (!string.IsNullOrEmpty(e.trackName) && trackName.StartsWith(e.trackName))
                return e;
        return null;
    }

    private OutlineEntry FindOutlineEntry(string featureName)
    {
        foreach (var e in outlineEntries)
            if (!string.IsNullOrEmpty(e.featureName) && e.featureName == featureName)
                return e;
        return null;
    }

    public void ForceRefresh()
    {
        wasInRange.Clear();
        ZeroAllMaterials();
    }
}
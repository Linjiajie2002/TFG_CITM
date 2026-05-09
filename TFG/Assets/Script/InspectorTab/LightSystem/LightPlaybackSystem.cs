using UnityEngine;
using System.Collections.Generic;

public class LightPlaybackSystem : MonoBehaviour
{
    [Header("=== 核心引用 ===")]
    public TimelineManager timeline;

    [Header("=== PointLight 配置 ===")]
    public string lightTrackName = "Light";
    public Transform pointLightContainer;

    [Header("=== SpotLight 配置 ===")]
    public string spotLightTrackName = "SpotLight";
    public GameObject spotLightPrefab;
    public Transform spotLightContainer;

    // Shader 属性 ID（缓存，避免每帧字符串查找）
    private static readonly int ID_GlobalAlpha = Shader.PropertyToID("_Global_Alpha");
    private static readonly int ID_BreathSpeed = Shader.PropertyToID("_Breath_Speed");
    private static readonly int ID_ColorTop = Shader.PropertyToID("_Color_Top");
    private static readonly int ID_ColorBottom = Shader.PropertyToID("_Color_Bottom");

    private Dictionary<TimelineEventData, Light> pointLightPool = new();
    private Dictionary<TimelineEventData, GameObject> spotLightPool = new();

    private float lastCheckedTime = -999f;
    private bool isPlaying = false;

    void Start()
    {
        RebuildPools();
    }

    void Update()
    {
        if (timeline == null) return;

        bool nowPlaying = timeline.musicSource != null && timeline.musicSource.isPlaying;
        float currentTime = timeline.GetCurrentTime();

        if (nowPlaying != isPlaying)
        {
            isPlaying = nowPlaying;
            if (!isPlaying)
            {
                DeactivateAll();
                lastCheckedTime = -999f;
            }
        }

        if (Mathf.Abs(currentTime - lastCheckedTime) > 0.008f || isPlaying)
        {
            lastCheckedTime = currentTime;
            RebuildPools();
            TickLights(currentTime);
        }
    }

    // ==========================================
    private void TickLights(float currentTime)
    {
        // ── PointLight ──
        foreach (var kvp in pointLightPool)
        {
            TimelineEventData evt = kvp.Key;
            Light lt = kvp.Value;
            PointLightClipData data = evt.customData as PointLightClipData;
            if (lt == null || data == null) continue;

            bool active = currentTime >= evt.startTime && currentTime < evt.startTime + evt.duration;
            if (active)
            {
                if (!lt.gameObject.activeSelf) lt.gameObject.SetActive(true);
                lt.transform.position = data.Position;
                lt.color = data.color;
                lt.intensity = data.intensity;
                lt.range = data.range;
            }
            else
            {
                if (lt.gameObject.activeSelf) lt.gameObject.SetActive(false);
            }
        }

        // ── SpotLight ──
        foreach (var kvp in spotLightPool)
        {
            TimelineEventData evt = kvp.Key;
            GameObject go = kvp.Value;
            SpotLightClipData data = evt.customData as SpotLightClipData;
            if (go == null || data == null) continue;

            bool active = currentTime >= evt.startTime && currentTime < evt.startTime + evt.duration;
            if (active)
            {
                if (!go.activeSelf) go.SetActive(true);

                // ── 外层 Empty：位置 + 缩放 + 静态朝向 ──
                go.transform.position = data.Position;
                go.transform.localScale = data.Scale;
                go.transform.rotation = data.Rotation;   // rotX / rotY / rotZ（Inspector 控制）

                // ── 中层 Empty：Y 轴循环旋转 ──
                if (data.runtimeMiddleEmpty != null)
                {
                    float spinY = data.isRotating ? (currentTime * data.rotationSpeed) % 360f : 0f;
                    data.runtimeMiddleEmpty.localEulerAngles = new Vector3(0f, spinY, data.circleRadius);
                }

                // ── Shader（通过独立材质实例，每个灯互不干扰）──
                ApplyShaderParams(data);

                // ── 物理 Light ──
                Light lt = go.GetComponentInChildren<Light>();
                if (lt != null)
                {
                    lt.range = data.range;
                    lt.color = data.colorTop;
                    lt.intensity = data.alpha * 5f;
                }
            }
            else
            {
                if (go.activeSelf) go.SetActive(false);
            }
        }
    }

    // ==========================================
    // 向独立材质实例写入 Shader 参数
    // ==========================================
    private void ApplyShaderParams(SpotLightClipData data)
    {
        if (data.runtimeMaterial == null) return;
        data.runtimeMaterial.SetFloat(ID_GlobalAlpha, data.alpha);
        data.runtimeMaterial.SetFloat(ID_BreathSpeed, data.breathSpeed);
        data.runtimeMaterial.SetColor(ID_ColorTop, data.colorTop);
        data.runtimeMaterial.SetColor(ID_ColorBottom, data.colorBottom);
    }

    // ==========================================
    // 对象池维护
    // ==========================================
    public void RebuildPools()
    {
        if (timeline?.allEvents == null) return;

        foreach (var evt in timeline.allEvents)
        {
            // PointLight
            if (evt.customData is PointLightClipData pData && !pointLightPool.ContainsKey(evt))
            {
                Light lt = CreatePointLight($"PointLight_{evt.trackIndex}");
                pointLightPool[evt] = lt;
                pData.runtimeLight = lt;
            }

            // SpotLight
            if (evt.customData is SpotLightClipData sData && !spotLightPool.ContainsKey(evt))
            {
                if (spotLightPrefab != null)
                {
                    GameObject go = Instantiate(spotLightPrefab, spotLightContainer);
                    go.name = $"SpotLight_{evt.startTime}";
                    go.SetActive(false);
                    spotLightPool[evt] = go;
                    sData.runtimeInstance = go;

                    // 存中层 Empty（Prefab 第一个子物体）
                    if (go.transform.childCount > 0)
                        sData.runtimeMiddleEmpty = go.transform.GetChild(0);

                    // ★ 为每个实例创建独立材质，避免多灯共享同一材质
                    MeshRenderer rend = go.GetComponentInChildren<MeshRenderer>();
                    if (rend != null)
                        sData.runtimeMaterial = rend.material; // renderer.material 自动 clone
                }
            }
        }

        CleanupRemovedClips();
    }

    private void CleanupRemovedClips()
    {
        var deadPoint = new List<TimelineEventData>();
        foreach (var k in pointLightPool.Keys)
            if (!timeline.allEvents.Contains(k)) deadPoint.Add(k);
        foreach (var k in deadPoint)
        {
            if (pointLightPool[k] != null) Destroy(pointLightPool[k].gameObject);
            pointLightPool.Remove(k);
        }

        var deadSpot = new List<TimelineEventData>();
        foreach (var k in spotLightPool.Keys)
            if (!timeline.allEvents.Contains(k)) deadSpot.Add(k);
        foreach (var k in deadSpot)
        {
            if (spotLightPool[k] != null)
            {
                SpotLightClipData d = k.customData as SpotLightClipData;
                if (d?.runtimeMaterial != null) Destroy(d.runtimeMaterial);
                Destroy(spotLightPool[k]);
            }
            spotLightPool.Remove(k);
        }
    }

    private Light CreatePointLight(string name)
    {
        GameObject go = new GameObject(name);
        if (pointLightContainer != null) go.transform.SetParent(pointLightContainer);
        Light lt = go.AddComponent<Light>();
        lt.type = LightType.Point;
        go.SetActive(false);
        return lt;
    }

    private void DeactivateAll()
    {
        foreach (var lt in pointLightPool.Values) if (lt) lt.gameObject.SetActive(false);
        foreach (var go in spotLightPool.Values) if (go) go.SetActive(false);
    }

    public void ForceRefresh()
    {
        lastCheckedTime = -999f;
        RebuildPools();
    }
}
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

    private static readonly int ID_GlobalAlpha = Shader.PropertyToID("_Global_Alpha");
    private static readonly int ID_BreathSpeed = Shader.PropertyToID("_Breath_Speed");
    private static readonly int ID_ColorTop = Shader.PropertyToID("_Color_Top");
    private static readonly int ID_ColorBottom = Shader.PropertyToID("_Color_Bottom");

    private Dictionary<TimelineEventData, Light> pointLightPool = new();
    private Dictionary<TimelineEventData, GameObject> spotLightPool = new();

    void Start()
    {
        RebuildPools();
    }

    void Update()
    {
        if (timeline == null) return;

        RebuildPools();
        TickLights(timeline.GetCurrentTime());
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

                // 外层：位置 + 缩放 + 静态朝向
                go.transform.position = data.Position;
                go.transform.localScale = data.Scale;
                go.transform.rotation = data.Rotation;

                // 中层：旋转中 Y 轴转圈+RotZ 半径；关闭时全归零
                if (data.runtimeMiddleEmpty != null)
                {
                    if (data.isRotating)
                    {
                        float spinY = (Time.time * data.rotationSpeed) % 360f;
                        data.runtimeMiddleEmpty.localEulerAngles = new Vector3(0f, spinY, data.circleRadius);
                    }
                    else
                    {
                        data.runtimeMiddleEmpty.localEulerAngles = Vector3.zero;
                    }
                }

                ApplyShaderParams(data);

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
            if (evt.customData is PointLightClipData pData && !pointLightPool.ContainsKey(evt))
            {
                Light lt = CreatePointLight($"PointLight_{evt.trackIndex}");
                pointLightPool[evt] = lt;
                pData.runtimeLight = lt;
            }

            if (evt.customData is SpotLightClipData sData && !spotLightPool.ContainsKey(evt))
            {
                if (spotLightPrefab != null)
                {
                    GameObject go = Instantiate(spotLightPrefab, spotLightContainer);
                    go.name = $"SpotLight_{evt.startTime}";
                    go.SetActive(false);
                    spotLightPool[evt] = go;
                    sData.runtimeInstance = go;

                    if (go.transform.childCount > 0)
                        sData.runtimeMiddleEmpty = go.transform.GetChild(0);

                    MeshRenderer rend = go.GetComponentInChildren<MeshRenderer>();
                    if (rend != null)
                        sData.runtimeMaterial = rend.material;
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

    public void ForceRefresh() => RebuildPools();
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class TimelineEventData
{
    public string eventName;
    public int trackIndex;
    public float startTime;
    public float duration;
}

public class TimelineManager : MonoBehaviour
{
    [Header("=== 数据控制中心 ===")]
    public Animator characterAnimator;
    public List<TimelineEventData> allEvents = new List<TimelineEventData>();

    [Header("=== 核心组件 ===")]
    public AudioSource musicSource;
    public RectTransform contentParent;
    public Slider playheadSlider; // 你的完美Handle，绝对不动它的内部结构
    public TextMeshProUGUI timeDisplayText;
    public TextMeshProUGUI playPauseButtonText;

    [Header("=== 预制体 ===")]
    public GameObject clipPrefab;
    public GameObject tickPrefab;
    public GameObject dividerPrefab;

    [Header("=== 现代排版配置 ===")]
    public float pixelsPerSecond = 100f;
    public int trackCount = 3;             // 轨道数量
    public float baseTrackHeight = 60f;    // 每条轨道固定高度，防止方块无限膨胀
    public float rulerInterval = 5.0f;
    public float rulerHeight = 30f;

    [Header("=== 位移烘焙数据 (底层引擎) ===")]
    private Vector3[] bakedPositions;
    private Quaternion[] bakedRotations;
    private float bakeFPS = 30f;

    private float totalDuration = 60f;
    private bool isDraggingSlider = false;
    private bool isInitialized = false;

    void Start()
    {
        if (playheadSlider != null)
            playheadSlider.onValueChanged.AddListener(OnSliderDrag);

        if (!isInitialized && musicSource != null && musicSource.clip != null)
        {
            SetupDynamicTimeline(characterAnimator, musicSource, "UI_Test_Dance");
        }
    }

    public void SetupDynamicTimeline(Animator spawnedAnimator, AudioSource assignedAudio, string danceName)
    {
        isInitialized = true;

        if (spawnedAnimator != null) characterAnimator = spawnedAnimator;
        if (assignedAudio != null) musicSource = assignedAudio;

        if (musicSource != null && musicSource.clip != null)
            totalDuration = musicSource.clip.length;
        else
            totalDuration = 60f;

        allEvents.Clear();

        // 强行锁死 Content 为左上角原点
        contentParent.pivot = new Vector2(0, 1);
        contentParent.anchorMin = new Vector2(0, 1);
        contentParent.anchorMax = new Vector2(0, 1);

        ResizeContent();
        GenerateGridLines();
        GenerateRuler();

        // 默认生成在最后一条轨道
        int bottomTrackIndex = trackCount - 1;
        if (bottomTrackIndex < 0) bottomTrackIndex = 0;
        CreateClip(danceName, bottomTrackIndex, 0f, totalDuration);

        if (playPauseButtonText != null)
            playPauseButtonText.text = "▶";

        // 保证 Handle 在最上层，并且强行拉长覆盖全区域！
        if (playheadSlider != null)
        {
            playheadSlider.transform.SetAsLastSibling();

            // 【终极修复：光剑向下延伸】
            // 无论父物体多短，强行把 Slider 底部往下拽 2000 像素，完美覆盖蓝框！
            RectTransform sliderRt = playheadSlider.GetComponent<RectTransform>();
            sliderRt.offsetMin = new Vector2(sliderRt.offsetMin.x, -2000f);
        }

        BakeRootMotion(danceName, totalDuration);
    }

    // ==========================================
    // 视觉排版引擎 (固定高度，图3风格)
    // ==========================================

    void ResizeContent()
    {
        // 根据时间和轨道数量，精确计算 Content 的宽和高
        float totalWidth = totalDuration * pixelsPerSecond;
        float totalHeight = rulerHeight + (trackCount * baseTrackHeight);
        contentParent.sizeDelta = new Vector2(totalWidth, totalHeight);
    }

    void GenerateGridLines()
    {
        ClearOldObjects("Divider_Template");

        // 只在刻度尺的正下方生成一条长长的白线
        float tracksStartY = -rulerHeight;

        GameObject line = Instantiate(dividerPrefab, contentParent);
        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(0, tracksStartY);
        rt.sizeDelta = new Vector2(contentParent.sizeDelta.x, 2f);
        line.transform.SetAsFirstSibling();
    }

    void GenerateRuler()
    {
        ClearOldObjects("Tick_Template");
        float tracksStartY = -rulerHeight;

        for (float time = 0; time <= totalDuration; time += rulerInterval)
        {
            GameObject tick = Instantiate(tickPrefab, contentParent);
            float xPos = time * pixelsPerSecond;
            RectTransform rt = tick.GetComponent<RectTransform>();

            // 短刻度线：立在白线上，高度10像素
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(2f, 10f);
            rt.anchoredPosition = new Vector2(xPos, tracksStartY);

            TextMeshProUGUI txt = tick.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = FormatTime(time);
                txt.alignment = TextAlignmentOptions.BottomLeft;

                // 数字：贴在横线上，稍微靠右
                RectTransform txtRt = txt.GetComponent<RectTransform>();
                txtRt.anchorMin = new Vector2(0, 0);
                txtRt.anchorMax = new Vector2(0, 0);
                txtRt.pivot = new Vector2(0f, 0f);
                txtRt.anchoredPosition = new Vector2(5f, 0f);
            }

            tick.transform.SetAsLastSibling();
        }
    }

    public void CreateClip(string name, int trackIndex, float startTime, float duration)
    {
        if (trackIndex >= trackCount) return;

        TimelineEventData newEvent = new TimelineEventData
        {
            eventName = name,
            trackIndex = trackIndex,
            startTime = startTime,
            duration = duration
        };
        allEvents.Add(newEvent);

        GameObject newClip = Instantiate(clipPrefab, contentParent);
        RectTransform rt = newClip.GetComponent<RectTransform>();

        // 精确计算方块位置 (原点左上角)
        float xPos = startTime * pixelsPerSecond;
        float yPos = -rulerHeight - (trackIndex * baseTrackHeight) - (baseTrackHeight / 2f);

        // 强行锁死左侧中心
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(xPos, yPos);

        // 留出空隙：高度为轨道高度的 80%
        float clipHeight = baseTrackHeight * 0.8f;
        rt.sizeDelta = new Vector2(duration * pixelsPerSecond, clipHeight);

        TextMeshProUGUI text = newClip.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = name;
    }

    // ==========================================
    // 底层引擎与控制逻辑 (完全保留，不碰你的Handle内部)
    // ==========================================

    void BakeRootMotion(string stateName, float duration)
    {
        if (characterAnimator == null) return;
        int totalFrames = Mathf.CeilToInt(duration * bakeFPS);
        bakedPositions = new Vector3[totalFrames];
        bakedRotations = new Quaternion[totalFrames];
        Vector3 initialSpawnPos = characterAnimator.transform.position;
        Quaternion initialSpawnRot = characterAnimator.transform.rotation;
        characterAnimator.transform.position = initialSpawnPos;
        characterAnimator.transform.rotation = initialSpawnRot;
        characterAnimator.Play(stateName, 0, 0f);
        characterAnimator.Update(0f);
        for (int i = 0; i < totalFrames; i++)
        {
            bakedPositions[i] = characterAnimator.transform.position;
            bakedRotations[i] = characterAnimator.transform.rotation;
            characterAnimator.Update(1f / bakeFPS);
        }
        characterAnimator.transform.position = initialSpawnPos;
        characterAnimator.transform.rotation = initialSpawnRot;
        characterAnimator.Play(stateName, 0, 0f);
        characterAnimator.Update(0f);
    }

    void ApplyBakedRootMotion(float time)
    {
        if (bakedPositions == null || bakedPositions.Length == 0 || characterAnimator == null) return;
        int frame = Mathf.FloorToInt(time * bakeFPS);
        frame = Mathf.Clamp(frame, 0, bakedPositions.Length - 1);
        characterAnimator.transform.position = bakedPositions[frame];
        characterAnimator.transform.rotation = bakedRotations[frame];
    }

    void ClearOldObjects(string nameKeyword) { for (int i = contentParent.childCount - 1; i >= 0; i--) { Transform child = contentParent.GetChild(i); if (child.name.Contains(nameKeyword) && child.name.Contains("Clone")) Destroy(child.gameObject); } }

    void Update()
    {
        if (musicSource == null || musicSource.clip == null) return;
        float currentTime = musicSource.time;

        if (!isDraggingSlider && playheadSlider != null)
            playheadSlider.value = currentTime / totalDuration;

        if (timeDisplayText != null)
            timeDisplayText.text = $"{FormatTime(currentTime)} / {FormatTime(totalDuration)}";

        SyncTimelineEvents(currentTime);
    }

    void SyncTimelineEvents(float currentTime)
    {
        foreach (var evt in allEvents)
        {
            if (currentTime >= evt.startTime && currentTime <= (evt.startTime + evt.duration))
            {
                if (evt.trackIndex == trackCount - 1 && characterAnimator != null)
                {
                    float localTime = currentTime - evt.startTime;
                    float normalizedTime = localTime / evt.duration;

                    if (musicSource.isPlaying)
                    {
                        if (characterAnimator.speed != 1f)
                        {
                            characterAnimator.Play(evt.eventName, 0, normalizedTime);
                            characterAnimator.speed = 1f;
                            ApplyBakedRootMotion(localTime);
                        }
                    }
                    else
                    {
                        characterAnimator.Play(evt.eventName, 0, normalizedTime);
                        characterAnimator.speed = 0f;
                        characterAnimator.Update(0f);
                        ApplyBakedRootMotion(localTime);
                    }
                }
            }
        }
    }

    string FormatTime(float t) { int m = Mathf.FloorToInt(t / 60F); int s = Mathf.FloorToInt(t % 60F); return string.Format("{0:00}:{1:00}", m, s); }

    public void OnSliderDrag(float value)
    {
        if (musicSource == null || musicSource.clip == null) return;
        float targetTime = value * totalDuration;

        if (Mathf.Abs(musicSource.time - targetTime) > 0.05f)
        {
            musicSource.time = targetTime;
            if (characterAnimator != null) characterAnimator.speed = 0f;
            SyncTimelineEvents(targetTime);
        }
    }

    public void TogglePlayPause()
    {
        if (musicSource == null || musicSource.clip == null) return;

        if (musicSource.isPlaying)
        {
            musicSource.Pause();
            if (playPauseButtonText != null) playPauseButtonText.text = "▶";
        }
        else
        {
            musicSource.Play();
            if (playPauseButtonText != null) playPauseButtonText.text = "❚❚";
        }
    }
}
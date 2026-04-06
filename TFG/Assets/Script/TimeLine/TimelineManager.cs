using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Timeline;

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
    public Slider playheadSlider;
    public TextMeshProUGUI timeDisplayText;
    public TextMeshProUGUI playPauseButtonText;

    [Header("=== 垂直滚动条配置 ===")]
    public Scrollbar verticalScrollbar;
    public RectTransform headerArea;

    [Header("=== 预制体 ===")]
    public GameObject clipPrefab;
    public GameObject tickPrefab;
    public GameObject dividerPrefab;

    [Header("=== 现代排版配置 ===")]
    public float pixelsPerSecond = 100f;
    public int trackCount = 1;
    public float baseTrackHeight = 60f;
    public float trackSpacing = 5f;
    public float rulerInterval = 5.0f;
    public float rulerHeight = 30f;

    [Header("=== 位移烘焙数据 (底层引擎) ===")]
    private Vector3[] bakedPositions;
    private Quaternion[] bakedRotations;
    private float bakeFPS = 30f;

    private float totalDuration = 60f;
    private bool isDraggingSlider = false;
    private bool isInitialized = false;

    // 【终极滚动双层架构】
    private RectTransform trackViewport;
    private RectTransform trackContainer;
    private float originalHeaderY;
    private bool isHeaderYStored = false;

    void Start()
    {
        if (verticalScrollbar != null)
        {
            verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;
            verticalScrollbar.value = 1f;
        }

        if (playheadSlider != null)
            playheadSlider.onValueChanged.AddListener(OnSliderDrag);

        if (!isInitialized)
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
        trackCount = 1;

        contentParent.pivot = new Vector2(0, 1);
        contentParent.anchorMin = new Vector2(0, 1);
        contentParent.anchorMax = new Vector2(0, 1);

        // ==========================================
        // 【核心大招】：自动生成带遮罩的相框 (Viewport) 和 画布 (Container)
        // ==========================================
        if (trackViewport == null)
        {
            // 1. 创建固定的相框，顶部被强制压到标尺(rulerHeight)的下方！
            GameObject tv = new GameObject("TrackViewport");
            trackViewport = tv.AddComponent<RectTransform>();
            trackViewport.SetParent(contentParent, false);
            trackViewport.anchorMin = new Vector2(0, 0);
            trackViewport.anchorMax = new Vector2(1, 1);
            trackViewport.offsetMin = Vector2.zero;
            trackViewport.offsetMax = new Vector2(0, -rulerHeight); // 核心：空出顶部的标尺区域
            tv.AddComponent<RectMask2D>(); // 给相框加上绝对遮罩
            trackViewport.SetSiblingIndex(0);

            // 2. 创建真正上下滑动的画布，放在相框内部
            GameObject tc = new GameObject("TrackContainer");
            trackContainer = tc.AddComponent<RectTransform>();
            trackContainer.SetParent(trackViewport, false);
            trackContainer.anchorMin = new Vector2(0, 0);
            trackContainer.anchorMax = new Vector2(1, 1);
            trackContainer.offsetMin = Vector2.zero;
            trackContainer.offsetMax = Vector2.zero;
        }

        ResizeContent();
        GenerateGridLines();
        GenerateRuler();

        CreateClip("Music Track", 0, 0f, totalDuration);

        if (playPauseButtonText != null)
            playPauseButtonText.text = "▶";

        if (playheadSlider != null)
        {
            playheadSlider.transform.SetAsLastSibling();
            RectTransform sliderRt = playheadSlider.GetComponent<RectTransform>();
            sliderRt.offsetMin = new Vector2(sliderRt.offsetMin.x, -2000f);
        }

        BakeRootMotion(danceName, totalDuration);
    }

    public void AddDynamicTrack(string name, float duration)
    {
        int newTrackIndex = trackCount;
        trackCount++;

        ResizeContent();
        CreateClip(name, newTrackIndex, 0f, duration);

        if (playheadSlider != null)
        {
            playheadSlider.transform.SetAsLastSibling();
            RectTransform sliderRt = playheadSlider.GetComponent<RectTransform>();
            sliderRt.offsetMin = new Vector2(sliderRt.offsetMin.x, -2000f);
        }
    }

    void ResizeContent()
    {
        float totalWidth = totalDuration * pixelsPerSecond;
        contentParent.sizeDelta = new Vector2(totalWidth, contentParent.sizeDelta.y);
    }

    void GenerateGridLines()
    {
        ClearOldObjects("Divider_Template");

        float tracksStartY = -rulerHeight;

        GameObject line = Instantiate(dividerPrefab, contentParent);

        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(0, tracksStartY);
        rt.sizeDelta = new Vector2(contentParent.sizeDelta.x, 2f);

        // 【核心修改】：把 First 改成 Last，让它排在最后渲染，稳稳地压在所有方块的上面！
        line.transform.SetAsLastSibling();
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
        TimelineEventData newEvent = new TimelineEventData
        {
            eventName = name,
            trackIndex = trackIndex,
            startTime = startTime,
            duration = duration
        };
        allEvents.Add(newEvent);

        GameObject newClip = Instantiate(clipPrefab, trackContainer);
        RectTransform rt = newClip.GetComponent<RectTransform>();

        TimelineClipUI clipUI = newClip.AddComponent<TimelineClipUI>();
        clipUI.manager = this;
        clipUI.eventData = newEvent;

        float xPos = startTime * pixelsPerSecond;
        float offset = -5f + (trackIndex * 10f);

        // 因为放入了相框内部，去掉了 -rulerHeight 的额外计算
        float yPos = -(trackIndex * (baseTrackHeight + trackSpacing)) - (baseTrackHeight / 2f) + offset;

        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(xPos, yPos);

        float clipHeight = baseTrackHeight * 0.8f;
        rt.sizeDelta = new Vector2(duration * pixelsPerSecond, clipHeight);

        TextMeshProUGUI text = newClip.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = name;
    }

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

    void ClearOldObjects(string nameKeyword)
    {
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Transform child = contentParent.GetChild(i);
            if (child.name.Contains(nameKeyword) && child.name.Contains("Clone")) Destroy(child.gameObject);
        }

        if (trackContainer != null)
        {
            for (int i = trackContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = trackContainer.GetChild(i);
                if (child.name.Contains(nameKeyword) && child.name.Contains("Clone")) Destroy(child.gameObject);
            }
        }
    }

    void Update()
    {
        SyncVerticalScroll();

        if (musicSource == null || musicSource.clip == null) return;
        float currentTime = musicSource.time;

        if (!isDraggingSlider && playheadSlider != null)
            playheadSlider.value = currentTime / totalDuration;

        if (timeDisplayText != null)
            timeDisplayText.text = $"{FormatTime(currentTime)} / {FormatTime(totalDuration)}";

        SyncTimelineEvents(currentTime);
    }

    void SyncVerticalScroll()
    {
        if (verticalScrollbar != null && headerArea != null && trackContainer != null)
        {
            float visibleHeight = contentParent.rect.height;
            if (visibleHeight < 10f) return;

            float totalTracksHeight = trackCount * (baseTrackHeight + trackSpacing);
            float maxScroll = Mathf.Max(0, totalTracksHeight - visibleHeight + rulerHeight + 20f);

            bool needsScroll = maxScroll > 0.1f;
            if (verticalScrollbar.gameObject.activeSelf != needsScroll)
            {
                verticalScrollbar.gameObject.SetActive(needsScroll);
            }

            if (!needsScroll)
            {
                trackContainer.anchoredPosition = Vector2.zero;
                if (isHeaderYStored) headerArea.anchoredPosition = new Vector2(headerArea.anchoredPosition.x, originalHeaderY);
                return;
            }

            float sizeRatio = visibleHeight / (totalTracksHeight + rulerHeight);
            verticalScrollbar.size = Mathf.Clamp(sizeRatio, 0.05f, 1f);

            float scrollOffset = (1f - verticalScrollbar.value) * maxScroll;

            trackContainer.anchoredPosition = new Vector2(0, scrollOffset);

            if (!isHeaderYStored)
            {
                originalHeaderY = headerArea.anchoredPosition.y;
                isHeaderYStored = true;
            }
            headerArea.anchoredPosition = new Vector2(headerArea.anchoredPosition.x, originalHeaderY + scrollOffset);
        }
    }

    void SyncTimelineEvents(float currentTime)
    {
        foreach (var evt in allEvents)
        {
            if (currentTime >= evt.startTime && currentTime <= (evt.startTime + evt.duration))
            {
                if (evt.trackIndex == 0 && characterAnimator != null)
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
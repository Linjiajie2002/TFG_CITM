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
    public Slider playheadSlider;
    public TextMeshProUGUI timeDisplayText;
    public TextMeshProUGUI playPauseButtonText;

    [Header("=== 预制体 ===")]
    public GameObject clipPrefab;
    public GameObject tickPrefab;
    public GameObject dividerPrefab;

    [Header("=== 配置 ===")]
    public float pixelsPerSecond = 100f;
    public int trackCount = 3;
    public float rulerInterval = 5.0f;
    public float rulerHeight = 30f;

    [Header("=== 位移烘焙数据 (底层引擎) ===")]
    private Vector3[] bakedPositions;
    private Quaternion[] bakedRotations;
    private float bakeFPS = 30f; // 一秒记录30次位置

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
        ResizeContentWidth();
        GenerateGridLines();
        GenerateRuler();

        int bottomTrackIndex = trackCount - 1;
        CreateClip(danceName, bottomTrackIndex, 0f, totalDuration);

        if (playPauseButtonText != null)
            playPauseButtonText.text = "▶";

        if (playheadSlider != null)
        {
            playheadSlider.transform.SetAsLastSibling();
        }

        // 【超级核心】开始烘焙角色的位移轨迹！
        BakeRootMotion(danceName, totalDuration);
    }

    // ==========================================
    // 引擎底层：预烘焙算法
    // ==========================================
    void BakeRootMotion(string stateName, float duration)
    {
        if (characterAnimator == null) return;

        int totalFrames = Mathf.CeilToInt(duration * bakeFPS);
        bakedPositions = new Vector3[totalFrames];
        bakedRotations = new Quaternion[totalFrames];

        // 记录刚生成时的原点位置（StageManager里的SpawnPoint）
        Vector3 initialSpawnPos = characterAnimator.transform.position;
        Quaternion initialSpawnRot = characterAnimator.transform.rotation;

        // 强制归零到原点，并准备从0秒开始
        characterAnimator.transform.position = initialSpawnPos;
        characterAnimator.transform.rotation = initialSpawnRot;
        characterAnimator.Play(stateName, 0, 0f);
        characterAnimator.Update(0f);

        // 用光速模拟未来的每一帧，并记录坐标
        for (int i = 0; i < totalFrames; i++)
        {
            bakedPositions[i] = characterAnimator.transform.position;
            bakedRotations[i] = characterAnimator.transform.rotation;

            // 往未来推进 1/30 秒
            characterAnimator.Update(1f / bakeFPS);
        }

        // 记录完毕！把角色神不知鬼不觉地复原回原点
        characterAnimator.transform.position = initialSpawnPos;
        characterAnimator.transform.rotation = initialSpawnRot;
        characterAnimator.Play(stateName, 0, 0f);
        characterAnimator.Update(0f);
    }

    // 根据时间提取烘焙好的坐标
    void ApplyBakedRootMotion(float time)
    {
        if (bakedPositions == null || bakedPositions.Length == 0 || characterAnimator == null) return;

        int frame = Mathf.FloorToInt(time * bakeFPS);
        frame = Mathf.Clamp(frame, 0, bakedPositions.Length - 1);

        characterAnimator.transform.position = bakedPositions[frame];
        characterAnimator.transform.rotation = bakedRotations[frame];
    }
    // ==========================================


    void ResizeContentWidth()
    {
        float totalWidth = totalDuration * pixelsPerSecond;
        contentParent.sizeDelta = new Vector2(totalWidth, contentParent.sizeDelta.y);
    }

    void GenerateGridLines()
    {
        ClearOldObjects("Divider_Template");
        float totalWidth = contentParent.sizeDelta.x;
        float totalHeight = contentParent.sizeDelta.y;
        float trackAreaHeight = totalHeight - rulerHeight;
        float singleTrackHeight = trackAreaHeight / (float)trackCount;
        float topEdge = totalHeight / 2f;
        float tracksStartY = topEdge - rulerHeight;

        SpawnLine(0, tracksStartY, totalWidth);
        for (int i = 1; i <= trackCount; i++)
        {
            float yPos = tracksStartY - (i * singleTrackHeight);
            SpawnLine(i, yPos, totalWidth);
        }
    }

    void SpawnLine(int index, float yPos, float width)
    {
        GameObject line = Instantiate(dividerPrefab, contentParent);
        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, yPos);
        rt.sizeDelta = new Vector2(width, 2f);
        line.transform.SetAsFirstSibling();
    }

    void GenerateRuler()
    {
        ClearOldObjects("Tick_Template");
        float totalHeight = contentParent.sizeDelta.y;
        float topEdge = totalHeight / 2f;

        for (float time = 0; time <= totalDuration; time += rulerInterval)
        {
            GameObject tick = Instantiate(tickPrefab, contentParent);
            float xPos = time * pixelsPerSecond;
            RectTransform rt = tick.GetComponent<RectTransform>();

            float rulerCenterY = topEdge - (rulerHeight / 2f);
            rt.anchoredPosition = new Vector2(xPos, rulerCenterY);

            TextMeshProUGUI txt = tick.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = FormatTime(time);

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
        float totalHeight = contentParent.sizeDelta.y;
        float trackAreaHeight = totalHeight - rulerHeight;
        float singleTrackHeight = trackAreaHeight / (float)trackCount;
        float topEdge = totalHeight / 2f;
        float tracksStartY = topEdge - rulerHeight;
        float yPos = tracksStartY - (trackIndex * singleTrackHeight) - (singleTrackHeight / 2f);
        float xPos = startTime * pixelsPerSecond;

        RectTransform rt = newClip.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(xPos, yPos);
        float clipHeight = singleTrackHeight - 10f;
        if (clipHeight < 5f) clipHeight = 5f;
        rt.sizeDelta = new Vector2(duration * pixelsPerSecond, clipHeight);

        TextMeshProUGUI text = newClip.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = name;
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

                            // 刚刚从拖拽中恢复播放瞬间，强制对齐一次绝对坐标！
                            ApplyBakedRootMotion(localTime);
                        }
                    }
                    else
                    {
                        characterAnimator.Play(evt.eventName, 0, normalizedTime);
                        characterAnimator.speed = 0f;
                        characterAnimator.Update(0f);

                        // 【核心改动】：处于暂停且被拖拽时，强行把坐标瞬移过去！
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
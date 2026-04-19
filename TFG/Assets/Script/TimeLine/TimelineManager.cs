using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[System.Serializable]
public class TimelineEventData
{
    public string eventName;
    public int trackIndex;
    public float startTime;
    public float duration;
    public GameObject clipObject;
    public GameObject headerObject;
    public GameObject inspectorPanel;
    public object customData;
}

[System.Serializable]
public class TrackData
{
    public string trackName;
    public int trackIndex;
    public GameObject headerObject;
    public bool allowOverlap;
}

public class TimelineManager : MonoBehaviour
{
    [Header("=== 数据 ===")]
    public Animator characterAnimator;
    public List<TimelineEventData> allEvents = new List<TimelineEventData>();
    public List<TrackData> allTracks = new List<TrackData>();

    [Header("=== 模块系统 ===")]
    public DynamicModuleSystem moduleSystem;

    [HideInInspector] public int selectedTrackIndex = -1;
    [HideInInspector] public TimelineEventData selectedClip = null;

    private Color defaultHeaderColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    private Color defaultClipColor = new Color(0f, 1f, 1f, 1f);
    private Color highlightColor = new Color(0.2f, 0.5f, 0.8f, 1f);
    private Color selectedClipColor = new Color(1f, 0.5f, 0f, 1f);

    [Header("=== 核心组件 ===")]
    public AudioSource musicSource;
    public RectTransform contentParent;
    public Slider playheadSlider;
    public TextMeshProUGUI timeDisplayText;
    public TextMeshProUGUI playPauseButtonText;
    public TMP_InputField timeInputField;

    [Header("=== 垂直滚动 ===")]
    public Scrollbar verticalScrollbar;
    public RectTransform headerArea;

    [Header("=== 预制体 ===")]
    public GameObject clipPrefab;
    public GameObject tickPrefab;
    public GameObject dividerPrefab;

    [Header("=== 排版 ===")]
    public float pixelsPerSecond = 100f;
    public int trackCount = 0;
    public float baseTrackHeight = 60f;
    public float trackSpacing = 5f;
    public float rulerInterval = 5.0f;
    public float rulerHeight = 30f;

    private float bakeFPS = 60f;
    private float bakedAnimationLength = 1f;
    private float originalAnimatorSpeed = 1f;
    [HideInInspector] public float totalDuration = 60f;
    private bool isDraggingSlider = false;
    private bool isInitialized = false;
    private string currentDanceName = "UI_Test_Dance";
    private bool wasPlaying = false;
    private bool requiresInitialSync = true;
    private float lastEvaluatedTime = -1f;
    private RectTransform trackViewport;
    private RectTransform trackContainer;
    private float originalHeaderY;
    private bool isHeaderYStored = false;
    private Vector3[] bakedPositions;
    private Quaternion[] bakedRotations;

    void Awake() { if (musicSource != null) { musicSource.playOnAwake = false; musicSource.Pause(); } if (characterAnimator != null) { originalAnimatorSpeed = characterAnimator.speed; if (originalAnimatorSpeed <= 0.01f) originalAnimatorSpeed = 1f; characterAnimator.speed = 0f; } }
    void Start() { if (verticalScrollbar != null) { verticalScrollbar.direction = Scrollbar.Direction.BottomToTop; verticalScrollbar.value = 1f; } if (playheadSlider != null) { EventTrigger trigger = playheadSlider.gameObject.GetComponent<EventTrigger>() ?? playheadSlider.gameObject.AddComponent<EventTrigger>(); trigger.triggers.Clear(); var eDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown }; eDown.callback.AddListener((d) => { isDraggingSlider = true; }); trigger.triggers.Add(eDown); var eUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp }; eUp.callback.AddListener((d) => { isDraggingSlider = false; }); trigger.triggers.Add(eUp); } if (timeInputField != null) { timeInputField.onValueChanged.AddListener(OnTimeInputValueChanged); timeInputField.onEndEdit.AddListener(OnTimeInputSubmit); } if (!isInitialized) SetupDynamicTimeline(characterAnimator, musicSource, "UI_Test_Dance"); }

    public void SetupDynamicTimeline(Animator spawnedAnimator, AudioSource assignedAudio, string danceName)
    {
        isInitialized = true; currentDanceName = danceName; wasPlaying = false; requiresInitialSync = true; lastEvaluatedTime = -1f;
        if (spawnedAnimator != null) characterAnimator = spawnedAnimator;
        if (assignedAudio != null) musicSource = assignedAudio;
        if (musicSource != null) { musicSource.playOnAwake = false; musicSource.Pause(); musicSource.time = 0f; totalDuration = (musicSource.clip != null) ? musicSource.clip.length : 60f; }
        allEvents.Clear(); allTracks.Clear(); trackCount = 0; selectedTrackIndex = -1; selectedClip = null;
        if (headerArea != null && headerArea.childCount > 0) headerArea.GetChild(0).gameObject.SetActive(false);
        contentParent.pivot = new Vector2(0, 1); contentParent.anchorMin = new Vector2(0, 1); contentParent.anchorMax = new Vector2(0, 1);
        if (trackViewport == null) { GameObject tv = new GameObject("TrackViewport"); trackViewport = tv.AddComponent<RectTransform>(); trackViewport.SetParent(contentParent, false); trackViewport.anchorMin = Vector2.zero; trackViewport.anchorMax = Vector2.one; trackViewport.offsetMin = Vector2.zero; trackViewport.offsetMax = new Vector2(0, -rulerHeight); tv.AddComponent<RectMask2D>(); trackViewport.SetSiblingIndex(0); GameObject tc = new GameObject("TrackContainer"); trackContainer = tc.AddComponent<RectTransform>(); trackContainer.SetParent(trackViewport, false); trackContainer.anchorMin = Vector2.zero; trackContainer.anchorMax = Vector2.one; trackContainer.offsetMin = Vector2.zero; trackContainer.offsetMax = Vector2.zero; }
        ResizeContent(); GenerateGridLines(); GenerateRuler();
        if (playPauseButtonText != null) playPauseButtonText.text = "▶";
        if (playheadSlider != null) { playheadSlider.transform.SetAsLastSibling(); var sliderRt = playheadSlider.GetComponent<RectTransform>(); sliderRt.offsetMin = new Vector2(sliderRt.offsetMin.x, -2000f); }
        BakeRootMotion(danceName, totalDuration); DeselectAll();
    }

    public void AddDynamicTrackSilent(string name, float duration, bool allowOverlap = true)
    {
        int newTrackIndex = trackCount++;
        ResizeContent();
        CreateTrackHeader(name, newTrackIndex, allowOverlap); // ← 加第三个参数

        if (playheadSlider != null)
        {
            playheadSlider.transform.SetAsLastSibling();
            var sliderRt = playheadSlider.GetComponent<RectTransform>();
            sliderRt.offsetMin = new Vector2(sliderRt.offsetMin.x, -2000f);
        }
        // 静默建轨道，不调用 SelectTrack
    }

    public void AddDynamicTrack(string name, float duration, bool allowOverlap = true)
    {
        int newTrackIndex = trackCount++;
        ResizeContent();
        CreateTrackHeader(name, newTrackIndex, allowOverlap);

        if (playheadSlider != null)
        {
            playheadSlider.transform.SetAsLastSibling();
            var sliderRt = playheadSlider.GetComponent<RectTransform>();
            sliderRt.offsetMin = new Vector2(sliderRt.offsetMin.x, -2000f);
        }

        SelectTrack(newTrackIndex); // 建完自动选中（非静默版）
    }

    // ==========================================
    // 容量管理配置中心 (新增)
    // ==========================================
    public int GetCapacityLimit(string trackName)
    {
        // 只要是名字带 Light 的轨道，最多允许 3 个方块在同一时间叠加
        if (trackName.Contains("Light")) return 3;
        return -1; // -1 代表无限制，遵循普通的 allowOverlap 规则
    }
    // ==========================================
    // 【核心升级】：自动寻找空位生成方块！
    // ==========================================
    // ==========================================
    // 替换 1：添加方块核心逻辑
    // ==========================================
    public TimelineEventData AddClipToTrack(int trackIndex, string featureName, float defaultDuration = 5f)
    {
        if (trackIndex < 0 || trackIndex >= trackCount) return null;
        float startTime = GetCurrentTime();

        TrackData track = allTracks.Find(t => t.trackIndex == trackIndex);
        bool trackNeedsCheck = (track != null && !track.allowOverlap);
        int capacityLimit = track != null ? GetCapacityLimit(track.trackName) : -1;

        if (trackNeedsCheck || capacityLimit > 0)
        {
            if (IsOverlappingOrOverCapacity(trackIndex, startTime, defaultDuration, trackNeedsCheck, capacityLimit))
            {
                float autoFitTime = FindNearestAvailableTimeWithCapacity(trackIndex, startTime, defaultDuration, trackNeedsCheck, capacityLimit);

                if (autoFitTime < 0f)
                {
                    Debug.LogWarning($"[{featureName}] 空间严重不足！实在挤不下 {defaultDuration} 秒的方块了。");
                    return null;
                }
                else
                {
                    Debug.Log($"[{featureName}] 触发容量限制！已自动吸附到最近空位：{autoFitTime}");
                    startTime = autoFitTime;
                }
            }
        }

        TimelineEventData evt = CreateClip(featureName, trackIndex, startTime, defaultDuration);
        if (playheadSlider != null) playheadSlider.transform.SetAsLastSibling();
        return evt;
    }

    // ==========================================
    // 替换 2：容量墙碰撞检测
    // ==========================================
    private bool IsOverlappingOrOverCapacity(int trackIndex, float start, float duration, bool checkTrackOverlap, int capacityLimit)
    {
        float step = 0.05f;
        for (float t = start; t < start + duration; t += step)
        {
            if (IsPointBlocked(t, null, trackIndex, checkTrackOverlap, capacityLimit))
                return true;
        }
        return false;
    }

    // ==========================================
    // 替换 3：精确判断某个时间点是否被堵死
    // ==========================================
    private bool IsPointBlocked(float t, TimelineEventData ignoreClip, int trackIndex, bool checkTrackOverlap, int capacityLimit)
    {
        int countAtTime = 0;
        foreach (var evt in allEvents)
        {
            if (evt == ignoreClip || evt.clipObject == null || evt.trackIndex != trackIndex) continue;

            // 只要有一点点重合就计数 (扣除0.01f的浮点误差防止边缘误判)
            if (t > evt.startTime + 0.01f && t < evt.startTime + evt.duration - 0.01f)
            {
                countAtTime++;
            }
        }

        if (checkTrackOverlap && countAtTime > 0) return true; // 如果轨道设置了不许重叠，1个就堵死
        if (capacityLimit > 0 && countAtTime >= capacityLimit) return true; // 达到了咱们设置的上限（3个），堵死

        return false;
    }

    // ==========================================
    // 替换 4：带容量判断的雷达寻路
    // ==========================================
    private float FindNearestAvailableTimeWithCapacity(int trackIndex, float desiredStart, float duration, bool checkTrackOverlap, int capacityLimit)
    {
        List<float> candidates = new List<float> { 0f, totalDuration - duration };
        foreach (var evt in allEvents)
        {
            if (evt.trackIndex == trackIndex && evt.clipObject != null)
            {
                candidates.Add(evt.startTime + evt.duration); // 试着贴在屁股后面
                candidates.Add(evt.startTime - duration);     // 试着贴在头前面
            }
        }

        float bestTime = -1f;
        float minDistance = float.MaxValue;

        foreach (float startTest in candidates)
        {
            if (startTest < 0f || startTest > totalDuration - duration + 0.001f) continue;

            if (!IsOverlappingOrOverCapacity(trackIndex, startTest, duration, checkTrackOverlap, capacityLimit))
            {
                float distance = Mathf.Abs(startTest - desiredStart);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestTime = startTest;
                }
            }
        }
        return bestTime;
    }

    // ==========================================
    // 替换 5：拖拽时的物理墙壁雷达
    // ==========================================
    public void GetAllowedTimeRange(TimelineEventData clip, float originalStart, out float minTime, out float maxTime)
    {
        minTime = 0f; maxTime = totalDuration;

        TrackData track = allTracks.Find(t => t.trackIndex == clip.trackIndex);
        bool checkTrackOverlap = (track != null && !track.allowOverlap);
        int capacityLimit = track != null ? GetCapacityLimit(track.trackName) : -1;

        if (!checkTrackOverlap && capacityLimit <= 0) return; // 没限制随便拖

        float step = 0.05f;

        // 向左扫描找墙壁
        float capMin = 0f;
        for (float t = originalStart; t >= 0f; t -= step)
        {
            if (IsPointBlocked(t, clip, clip.trackIndex, checkTrackOverlap, capacityLimit))
            {
                capMin = t + step;
                break;
            }
        }

        // 向右扫描找墙壁
        float capMax = totalDuration;
        for (float t = originalStart; t <= totalDuration; t += step)
        {
            if (IsPointBlocked(t, clip, clip.trackIndex, checkTrackOverlap, capacityLimit))
            {
                capMax = t;
                break;
            }
        }

        minTime = Mathf.Max(minTime, capMin);
        maxTime = Mathf.Min(maxTime, capMax);
    }

    public float GetCurrentTime()
    {
        // 播放中：读音乐真实进度（否则 CameraPlaybackSystem 会读到旧的 slider 值）
        if (musicSource != null && musicSource.isPlaying) return musicSource.time;
        // 停止/scrub：读 slider
        if (playheadSlider != null) return playheadSlider.value * totalDuration;
        if (musicSource != null) return musicSource.time;
        return 0f;
    }
    private void CreateTrackHeader(string name, int trackIndex, bool allowOverlap)
    {
        GameObject newHeader = null;
        if (headerArea != null && headerArea.childCount > 0)
        {
            newHeader = Instantiate(headerArea.GetChild(0).gameObject, headerArea);
            newHeader.name = "Header_" + name; newHeader.SetActive(true);
            var txt = newHeader.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = name;
            var btn = newHeader.GetComponent<Button>() ?? newHeader.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            int i = trackIndex; btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => SelectTrack(i));
        }
        allTracks.Add(new TrackData { trackName = name, trackIndex = trackIndex, headerObject = newHeader, allowOverlap = allowOverlap });
    }

    public TimelineEventData CreateClip(string featureName, int trackIndex, float startTime, float duration) { GameObject newClip = Instantiate(clipPrefab, trackContainer); RectTransform rt = newClip.GetComponent<RectTransform>(); var evt = new TimelineEventData { eventName = featureName, trackIndex = trackIndex, startTime = startTime, duration = duration, clipObject = newClip }; allEvents.Add(evt); var clipUI = newClip.AddComponent<TimelineClipUI>(); clipUI.manager = this; clipUI.eventData = evt; float xPos = startTime * pixelsPerSecond; float yPos = -(trackIndex * (baseTrackHeight + trackSpacing)) - baseTrackHeight / 2f + (-5f + trackIndex * 10f); rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 0.5f); rt.anchoredPosition = new Vector2(xPos, yPos); rt.sizeDelta = new Vector2(duration * pixelsPerSecond, baseTrackHeight * 0.8f); var text = newClip.GetComponentInChildren<TextMeshProUGUI>(); if (text != null) text.text = featureName; return evt; }
    public void SelectClip(TimelineEventData clip) { if (clip == null) return; SetClipColor(selectedClip, defaultClipColor); selectedClip = clip; SetClipColor(clip, selectedClipColor); SetHeaderColor(selectedTrackIndex, defaultHeaderColor); selectedTrackIndex = clip.trackIndex; SetHeaderColor(selectedTrackIndex, highlightColor); TrackData track = allTracks.Find(t => t.trackIndex == clip.trackIndex); if (track != null && moduleSystem != null) moduleSystem.ShowInspector(track.trackName); HideAllClipPanels(); if (clip.inspectorPanel != null) clip.inspectorPanel.SetActive(true); var panel = clip.inspectorPanel?.GetComponent<ClipInspectorPanel>(); if (panel != null) panel.RefreshDisplay(); if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); }
    public void SelectTrack(int index, bool skipTabSync = false) { if (index < 0) return; SetClipColor(selectedClip, defaultClipColor); selectedClip = null; HideAllClipPanels(); SetHeaderColor(selectedTrackIndex, defaultHeaderColor); selectedTrackIndex = index; SetHeaderColor(selectedTrackIndex, highlightColor); if (!skipTabSync) { TrackData track = allTracks.Find(t => t.trackIndex == index); if (track != null && moduleSystem != null) moduleSystem.ShowInspector(track.trackName); } if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); }
    public void HandleDeleteKey() { if (selectedClip != null) DeleteSelectedClip(); }
    public void DeleteSelectedClip() { if (selectedClip == null) return; if (selectedClip.inspectorPanel != null) Destroy(selectedClip.inspectorPanel); if (selectedClip.clipObject != null) Destroy(selectedClip.clipObject); allEvents.Remove(selectedClip); selectedClip = null; if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); }
    public void DeleteSelectedTrack() { if (selectedTrackIndex < 0) return; int idx = selectedTrackIndex; var track = allTracks.Find(t => t.trackIndex == idx); if (track != null) { if (moduleSystem != null) moduleSystem.RestoreCover(track.trackName); if (track.headerObject != null) Destroy(track.headerObject); allTracks.Remove(track); } var toDelete = allEvents.FindAll(e => e.trackIndex == idx); foreach (var e in toDelete) { if (e.inspectorPanel != null) Destroy(e.inspectorPanel); if (e.clipObject != null) Destroy(e.clipObject); } allEvents.RemoveAll(e => e.trackIndex == idx); trackCount--; foreach (var t in allTracks) { if (t.trackIndex > idx) { t.trackIndex--; var btn = t.headerObject?.GetComponent<Button>(); if (btn != null) { int ni = t.trackIndex; btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => SelectTrack(ni)); } } } foreach (var e in allEvents) { if (e.trackIndex > idx) e.trackIndex--; } selectedTrackIndex = -1; selectedClip = null; RefreshClipPositions(); ResizeContent(); if (moduleSystem != null) moduleSystem.ShowDefaultInspector(); if (allTracks.Count > 0 && moduleSystem?.tabManager != null) moduleSystem.tabManager.SwitchTab(0); }
    private void SetHeaderColor(int index, Color color) { if (index < 0) return; var t = allTracks.Find(x => x.trackIndex == index); if (t?.headerObject != null) { var img = t.headerObject.GetComponent<Image>(); if (img != null) img.color = color; } }
    private void SetClipColor(TimelineEventData clip, Color color) { if (clip?.clipObject == null) return; var img = clip.clipObject.GetComponent<Image>(); if (img != null) img.color = color; }
    public void DeselectAll() { SetClipColor(selectedClip, defaultClipColor); selectedClip = null; HideAllClipPanels(); SetHeaderColor(selectedTrackIndex, defaultHeaderColor); selectedTrackIndex = -1; if (moduleSystem != null) moduleSystem.ShowDefaultInspector(); }
    private void HideAllClipPanels() { foreach (var e in allEvents) if (e.inspectorPanel != null) e.inspectorPanel.SetActive(false); }
    void RefreshClipPositions() { foreach (var evt in allEvents) { if (evt.clipObject != null) { var rt = evt.clipObject.GetComponent<RectTransform>(); float yPos = -(evt.trackIndex * (baseTrackHeight + trackSpacing)) - baseTrackHeight / 2f + (-5f + evt.trackIndex * 10f); rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, yPos); } } }
    void ResizeContent() { contentParent.sizeDelta = new Vector2(totalDuration * pixelsPerSecond, contentParent.sizeDelta.y); }
    void GenerateGridLines() { ClearOldObjects("Divider_Template"); var line = Instantiate(dividerPrefab, contentParent); var rt = line.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(0, -rulerHeight); rt.sizeDelta = new Vector2(contentParent.sizeDelta.x, 2f); line.transform.SetAsLastSibling(); }
    void GenerateRuler() { ClearOldObjects("Tick_Template"); for (float time = 0; time <= totalDuration; time += rulerInterval) { var tick = Instantiate(tickPrefab, contentParent); var rt = tick.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0.5f, 0f); rt.sizeDelta = new Vector2(2f, 10f); rt.anchoredPosition = new Vector2(time * pixelsPerSecond, -rulerHeight); var txt = tick.GetComponentInChildren<TextMeshProUGUI>(); if (txt != null) { txt.text = FormatTime(time); txt.alignment = TextAlignmentOptions.BottomLeft; var tr = txt.GetComponent<RectTransform>(); tr.anchorMin = tr.anchorMax = tr.pivot = Vector2.zero; tr.anchoredPosition = new Vector2(5f, 0f); } tick.transform.SetAsLastSibling(); } }
    void BakeRootMotion(string stateName, float duration) { if (characterAnimator == null) return; int frames = Mathf.CeilToInt(duration * bakeFPS); bakedPositions = new Vector3[frames]; bakedRotations = new Quaternion[frames]; Vector3 pos0 = characterAnimator.transform.position; Quaternion rot0 = characterAnimator.transform.rotation; characterAnimator.Play(stateName, 0, 0f); characterAnimator.Update(0f); bakedAnimationLength = characterAnimator.GetCurrentAnimatorStateInfo(0).length; if (bakedAnimationLength <= 0.1f) bakedAnimationLength = 1f; for (int i = 0; i < frames; i++) { bakedPositions[i] = characterAnimator.transform.position; bakedRotations[i] = characterAnimator.transform.rotation; characterAnimator.Update(1f / bakeFPS); } characterAnimator.transform.position = pos0; characterAnimator.transform.rotation = rot0; characterAnimator.Play(stateName, 0, 0f); characterAnimator.Update(0f); characterAnimator.speed = 0f; }
    void ApplyBakedRootMotion(float time) { if (bakedPositions == null || bakedPositions.Length == 0 || characterAnimator == null) return; float ff = time * bakeFPS; int f1 = Mathf.Clamp(Mathf.FloorToInt(ff), 0, bakedPositions.Length - 1); int f2 = Mathf.Clamp(Mathf.CeilToInt(ff), 0, bakedPositions.Length - 1); float t = ff - f1; characterAnimator.transform.position = Vector3.Lerp(bakedPositions[f1], bakedPositions[f2], t); characterAnimator.transform.rotation = Quaternion.Slerp(bakedRotations[f1], bakedRotations[f2], t); }
    void ClearOldObjects(string keyword) { for (int i = contentParent.childCount - 1; i >= 0; i--) { var c = contentParent.GetChild(i); if (c.name.Contains(keyword) && c.name.Contains("Clone")) Destroy(c.gameObject); } if (trackContainer != null) for (int i = trackContainer.childCount - 1; i >= 0; i--) { var c = trackContainer.GetChild(i); if (c.name.Contains(keyword) && c.name.Contains("Clone")) Destroy(c.gameObject); } }

    void Update()
    {
        if (Keyboard.current != null && (Keyboard.current.deleteKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame)) HandleDeleteKey();
        if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame) { Vector2 mp = Mouse.current.position.ReadValue(); var vp = contentParent.parent.GetComponent<RectTransform>(); if (RectTransformUtility.RectangleContainsScreenPoint(vp, mp) && RectTransformUtility.ScreenPointToLocalPointInRectangle(contentParent, mp, null, out Vector2 lp)) { float t = Mathf.Clamp(lp.x / pixelsPerSecond, 0, totalDuration); t = Mathf.RoundToInt(t * bakeFPS) / bakeFPS; if (musicSource != null) musicSource.time = t; if (playheadSlider != null) playheadSlider.SetValueWithoutNotify(t / totalDuration); SyncTimelineEvents(t, true); } }
        if (Mouse.current != null && verticalScrollbar != null && verticalScrollbar.gameObject.activeSelf) { float sd = Mouse.current.scroll.ReadValue().y; if (Mathf.Abs(sd) > 0.1f) verticalScrollbar.value = Mathf.Clamp01(verticalScrollbar.value + sd * 0.2f); }
        SyncVerticalScroll();

        float currentTime = 0f;
        if (isDraggingSlider) { if (playheadSlider != null) { float raw = playheadSlider.value * totalDuration; currentTime = Mathf.RoundToInt(raw * bakeFPS) / bakeFPS; if (musicSource != null && Mathf.Abs(musicSource.time - currentTime) > 0.035f) musicSource.time = currentTime; } DoEdgeScroll(); }
        else if (musicSource != null && musicSource.clip != null) { if (musicSource.isPlaying) { currentTime = musicSource.time; if (currentTime >= totalDuration - 0.05f) { musicSource.Pause(); musicSource.time = 0f; currentTime = 0f; } if (playheadSlider != null) playheadSlider.SetValueWithoutNotify(currentTime / totalDuration); AutoScrollToPlayhead(currentTime); } else { currentTime = (playheadSlider != null) ? Mathf.RoundToInt(playheadSlider.value * totalDuration * bakeFPS) / bakeFPS : musicSource.time; if (Mathf.Abs(musicSource.time - currentTime) > 0.035f) musicSource.time = currentTime; } }

        if (timeDisplayText != null) timeDisplayText.text = $"{FormatTime(currentTime)} / {FormatTime(totalDuration)}";
        if (timeInputField != null && !timeInputField.isFocused) timeInputField.SetTextWithoutNotify(FormatTime(currentTime));
        if (playPauseButtonText != null) playPauseButtonText.text = (musicSource != null && musicSource.isPlaying) ? "Stop" : "Play";

        if (selectedClip != null) { var p = selectedClip.inspectorPanel?.GetComponent<ClipInspectorPanel>(); if (p != null) p.RefreshDisplay(); }
        bool forceUpdate = requiresInitialSync; if (requiresInitialSync) requiresInitialSync = false;
        SyncTimelineEvents(currentTime, forceUpdate);
    }

    void AutoScrollToPlayhead(float t) { if (contentParent?.parent == null) return; var vp = contentParent.parent.GetComponent<RectTransform>(); float vw = vp.rect.width; if (vw < 10f) return; float px = t * pixelsPerSecond, cx = -contentParent.anchoredPosition.x, max = Mathf.Max(0, contentParent.rect.width - vw); if (px > cx + vw || px < cx) { float nx = Mathf.Clamp(-px, -max, 0); contentParent.anchoredPosition = new Vector2(nx, contentParent.anchoredPosition.y); var sr = contentParent.GetComponentInParent<ScrollRect>(); if (sr != null && sr.horizontal && max > 0) sr.horizontalNormalizedPosition = Mathf.Clamp01(-nx / max); } }
    void FocusOnTime(float targetTime) { if (contentParent?.parent == null) return; var vp = contentParent.parent.GetComponent<RectTransform>(); float vw = vp.rect.width, max = Mathf.Max(0, contentParent.rect.width - vw); float nx = Mathf.Clamp(-(targetTime * pixelsPerSecond - vw / 2f), -max, 0); contentParent.anchoredPosition = new Vector2(nx, contentParent.anchoredPosition.y); var sr = contentParent.GetComponentInParent<ScrollRect>(); if (sr != null && sr.horizontal && max > 0) sr.horizontalNormalizedPosition = Mathf.Clamp01(-nx / max); }
    void DoEdgeScroll() { if (contentParent?.parent == null) return; var vp = contentParent.parent.GetComponent<RectTransform>(); if (RectTransformUtility.ScreenPointToLocalPointInRectangle(vp, Mouse.current.position.ReadValue(), null, out Vector2 lm)) { float speed = 1500f * Time.deltaTime, delta = 0f, nx2 = (lm.x - vp.rect.xMin) / vp.rect.width; if (nx2 > 0.95f) delta = -speed; else if (nx2 < 0.05f) delta = speed; if (delta != 0f) { float max = Mathf.Max(0, contentParent.rect.width - vp.rect.width); float nx = Mathf.Clamp(contentParent.anchoredPosition.x + delta, -max, 0); contentParent.anchoredPosition = new Vector2(nx, contentParent.anchoredPosition.y); var sr = contentParent.GetComponentInParent<ScrollRect>(); if (sr != null && sr.horizontal && max > 0) sr.horizontalNormalizedPosition = Mathf.Clamp01(-nx / max); } } }
    public void OnTimeInputValueChanged(string rawStr) { string d = ""; foreach (char c in rawStr) if (char.IsDigit(c)) d += c; if (string.IsNullOrEmpty(d)) d = "000000"; if (d.Length > 6) d = d.Substring(d.Length - 6); else d = d.PadLeft(6, '0'); string f = $"{d.Substring(0, 2)}:{d.Substring(2, 2)}:{d.Substring(4, 2)}"; timeInputField.SetTextWithoutNotify(f); timeInputField.caretPosition = f.Length; }
    public void OnTimeInputSubmit(string input) { string[] p = input.Split(':'); if (p.Length == 3 && int.TryParse(p[0], out int m) && int.TryParse(p[1], out int s) && int.TryParse(p[2], out int fr)) { float t = Mathf.Clamp((m * 60f) + Mathf.Clamp(s, 0, 59) + (Mathf.Clamp(fr, 0, Mathf.FloorToInt(bakeFPS - 1)) / bakeFPS), 0, totalDuration); FocusOnTime(t); } if (timeInputField != null) timeInputField.SetTextWithoutNotify(FormatTime(musicSource != null ? musicSource.time : 0f)); if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); }
    void SyncVerticalScroll() { if (verticalScrollbar == null || headerArea == null || trackContainer == null) return; float vh = contentParent.rect.height; if (vh < 10f) return; float th = trackCount * (baseTrackHeight + trackSpacing), max = Mathf.Max(0, th - vh + rulerHeight + 20f); bool need = max > 0.1f; if (verticalScrollbar.gameObject.activeSelf != need) verticalScrollbar.gameObject.SetActive(need); if (!need) { trackContainer.anchoredPosition = Vector2.zero; if (isHeaderYStored) headerArea.anchoredPosition = new Vector2(headerArea.anchoredPosition.x, originalHeaderY); return; } verticalScrollbar.size = Mathf.Clamp(vh / (th + rulerHeight), 0.05f, 1f); float off = (1f - verticalScrollbar.value) * max; trackContainer.anchoredPosition = new Vector2(0, off); if (!isHeaderYStored) { originalHeaderY = headerArea.anchoredPosition.y; isHeaderYStored = true; } headerArea.anchoredPosition = new Vector2(headerArea.anchoredPosition.x, originalHeaderY + off); }
    void SyncTimelineEvents(float currentTime, bool forceSync = false)
    {
        bool playing = (musicSource != null && musicSource.isPlaying);
        bool changed = (playing != wasPlaying);
        wasPlaying = playing;
        lastEvaluatedTime = currentTime;

        if (characterAnimator == null) return;
        float norm = (currentTime % bakedAnimationLength) / bakedAnimationLength;

        if (playing)
        {
            // 播放中：正常播放；但拖动进度条时强制重新对齐动画帧
            if (characterAnimator.speed == 0f || changed || forceSync || isDraggingSlider)
            {
                characterAnimator.Play(currentDanceName, 0, norm);
                characterAnimator.speed = originalAnimatorSpeed;
                ApplyBakedRootMotion(currentTime);
            }
        }
        else
        {
            // 非播放状态：无条件每帧刷新 → click / 拖动 / 无操作都能响应
            characterAnimator.Play(currentDanceName, 0, norm);
            characterAnimator.Update(0f);
            ApplyBakedRootMotion(currentTime);
            if (characterAnimator.speed != 0f) characterAnimator.speed = 0f;
        }
    }
    string FormatTime(float t) { int m = Mathf.FloorToInt(t / 60f), s = Mathf.FloorToInt(t % 60f), f = Mathf.Min(Mathf.FloorToInt((t % 1f) * bakeFPS), Mathf.FloorToInt(bakeFPS - 1)); return string.Format("{0:00}:{1:00}:{2:00}", m, s, f); }
    public void OnSliderDrag(float value) { }
    public void TogglePlayPause() { if (musicSource == null || musicSource.clip == null) return; if (musicSource.isPlaying) musicSource.Pause(); else musicSource.Play(); }
}
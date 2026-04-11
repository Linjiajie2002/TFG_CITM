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
}

[System.Serializable]
public class TrackData
{
    public string trackName;
    public int trackIndex;
    public GameObject headerObject;
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
    private float totalDuration = 60f;
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

    void Awake()
    {
        if (musicSource != null) { musicSource.playOnAwake = false; musicSource.Pause(); }
        if (characterAnimator != null)
        {
            originalAnimatorSpeed = characterAnimator.speed;
            if (originalAnimatorSpeed <= 0.01f) originalAnimatorSpeed = 1f;
            characterAnimator.speed = 0f;
        }
    }

    void Start()
    {
        if (verticalScrollbar != null) { verticalScrollbar.direction = Scrollbar.Direction.BottomToTop; verticalScrollbar.value = 1f; }

        if (playheadSlider != null)
        {
            EventTrigger trigger = playheadSlider.gameObject.GetComponent<EventTrigger>() ?? playheadSlider.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();
            var eDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            eDown.callback.AddListener((d) => { isDraggingSlider = true; });
            trigger.triggers.Add(eDown);
            var eUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            eUp.callback.AddListener((d) => { isDraggingSlider = false; });
            trigger.triggers.Add(eUp);
        }

        if (timeInputField != null)
        {
            timeInputField.onValueChanged.AddListener(OnTimeInputValueChanged);
            timeInputField.onEndEdit.AddListener(OnTimeInputSubmit);
        }

        if (!isInitialized) SetupDynamicTimeline(characterAnimator, musicSource, "UI_Test_Dance");
    }

    public void SetupDynamicTimeline(Animator spawnedAnimator, AudioSource assignedAudio, string danceName)
    {
        isInitialized = true; currentDanceName = danceName;
        wasPlaying = false; requiresInitialSync = true; lastEvaluatedTime = -1f;
        if (spawnedAnimator != null) characterAnimator = spawnedAnimator;
        if (assignedAudio != null) musicSource = assignedAudio;
        if (musicSource != null)
        {
            musicSource.playOnAwake = false; musicSource.Pause(); musicSource.time = 0f;
            totalDuration = (musicSource.clip != null) ? musicSource.clip.length : 60f;
        }
        allEvents.Clear(); allTracks.Clear(); trackCount = 0; selectedTrackIndex = -1; selectedClip = null;
        if (headerArea != null && headerArea.childCount > 0) headerArea.GetChild(0).gameObject.SetActive(false);
        contentParent.pivot = new Vector2(0, 1); contentParent.anchorMin = new Vector2(0, 1); contentParent.anchorMax = new Vector2(0, 1);

        if (trackViewport == null)
        {
            GameObject tv = new GameObject("TrackViewport"); trackViewport = tv.AddComponent<RectTransform>();
            trackViewport.SetParent(contentParent, false); trackViewport.anchorMin = Vector2.zero; trackViewport.anchorMax = Vector2.one;
            trackViewport.offsetMin = Vector2.zero; trackViewport.offsetMax = new Vector2(0, -rulerHeight);
            tv.AddComponent<RectMask2D>(); trackViewport.SetSiblingIndex(0);
            GameObject tc = new GameObject("TrackContainer"); trackContainer = tc.AddComponent<RectTransform>();
            trackContainer.SetParent(trackViewport, false); trackContainer.anchorMin = Vector2.zero; trackContainer.anchorMax = Vector2.one;
            trackContainer.offsetMin = Vector2.zero; trackContainer.offsetMax = Vector2.zero;
        }
        ResizeContent(); GenerateGridLines(); GenerateRuler();
        if (playPauseButtonText != null) playPauseButtonText.text = "▶";
        if (playheadSlider != null)
        {
            playheadSlider.transform.SetAsLastSibling();
            var sliderRt = playheadSlider.GetComponent<RectTransform>();
            sliderRt.offsetMin = new Vector2(sliderRt.offsetMin.x, -2000f);
        }
        BakeRootMotion(danceName, totalDuration); DeselectAll();
    }

    // FIX4: AddDynamicTrack 建完轨道立即自动选中
    public void AddDynamicTrack(string name, float duration)
    {
        int idx = trackCount++;
        ResizeContent();
        CreateTrackHeader(name, idx);
        if (playheadSlider != null)
        {
            playheadSlider.transform.SetAsLastSibling();
            var sliderRt = playheadSlider.GetComponent<RectTransform>();
            sliderRt.offsetMin = new Vector2(sliderRt.offsetMin.x, -2000f);
        }
        SelectTrack(idx); // 自动选中新轨道
    }

    // FIX1: 在红线位置添加 Clip
    public TimelineEventData AddClipToTrack(int trackIndex, string featureName, float defaultDuration = 5f)
    {
        if (trackIndex < 0 || trackIndex >= trackCount) return null;
        float startTime = GetCurrentTime(); // 红线时间
        TimelineEventData evt = CreateClip(featureName, trackIndex, startTime, defaultDuration);
        if (playheadSlider != null) playheadSlider.transform.SetAsLastSibling();
        return evt;
    }

    public float GetCurrentTime()
    {
        if (musicSource != null && musicSource.clip != null) return musicSource.time;
        if (playheadSlider != null) return playheadSlider.value * totalDuration;
        return 0f;
    }

    private void CreateTrackHeader(string name, int trackIndex)
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
        allTracks.Add(new TrackData { trackName = name, trackIndex = trackIndex, headerObject = newHeader });
    }

    public TimelineEventData CreateClip(string featureName, int trackIndex, float startTime, float duration)
    {
        GameObject newClip = Instantiate(clipPrefab, trackContainer);
        RectTransform rt = newClip.GetComponent<RectTransform>();
        var evt = new TimelineEventData { eventName = featureName, trackIndex = trackIndex, startTime = startTime, duration = duration, clipObject = newClip };
        allEvents.Add(evt);
        var clipUI = newClip.AddComponent<TimelineClipUI>(); clipUI.manager = this; clipUI.eventData = evt;
        float xPos = startTime * pixelsPerSecond;
        float yPos = -(trackIndex * (baseTrackHeight + trackSpacing)) - baseTrackHeight / 2f + (-5f + trackIndex * 10f);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(xPos, yPos); rt.sizeDelta = new Vector2(duration * pixelsPerSecond, baseTrackHeight * 0.8f);
        var text = newClip.GetComponentInChildren<TextMeshProUGUI>(); if (text != null) text.text = featureName;
        return evt;
    }

    // FIX2+3: SelectClip — 只高亮 clip 所属轨道，强制同步 Tab
    public void SelectClip(TimelineEventData clip)
    {
        if (clip == null) return;
        SetClipColor(selectedClip, defaultClipColor); // 取消旧 clip 颜色
        selectedClip = clip;
        SetClipColor(clip, selectedClipColor); // 新 clip 橙色
        // FIX2: 取消所有轨道高亮，只高亮 clip 所属轨道
        SetHeaderColor(selectedTrackIndex, defaultHeaderColor);
        selectedTrackIndex = clip.trackIndex;
        SetHeaderColor(selectedTrackIndex, highlightColor);
        // FIX3: 同步 Inspector Tab
        TrackData track = allTracks.Find(t => t.trackIndex == clip.trackIndex);
        if (track != null && moduleSystem != null) moduleSystem.ShowInspector(track.trackName);
        // 显示专属面板
        HideAllClipPanels();
        if (clip.inspectorPanel != null) clip.inspectorPanel.SetActive(true);
        var panel = clip.inspectorPanel?.GetComponent<ClipInspectorPanel>();
        if (panel != null) panel.RefreshDisplay();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    // FIX3: SelectTrack — skipTabSync 防止与 InspectorTabManager 互相循环
    public void SelectTrack(int index, bool skipTabSync = false)
    {
        if (index < 0) return;
        SetClipColor(selectedClip, defaultClipColor); selectedClip = null;
        HideAllClipPanels();
        SetHeaderColor(selectedTrackIndex, defaultHeaderColor);
        selectedTrackIndex = index;
        SetHeaderColor(selectedTrackIndex, highlightColor);
        if (!skipTabSync)
        {
            TrackData track = allTracks.Find(t => t.trackIndex == index);
            if (track != null && moduleSystem != null) moduleSystem.ShowInspector(track.trackName);
        }
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    // FIX5: Delete 键只删 Clip，不删轨道
    public void HandleDeleteKey()
    {
        if (selectedClip != null) DeleteSelectedClip();
    }

    public void DeleteSelectedClip()
    {
        if (selectedClip == null) return;
        if (selectedClip.inspectorPanel != null) Destroy(selectedClip.inspectorPanel);
        if (selectedClip.clipObject != null) Destroy(selectedClip.clipObject);
        allEvents.Remove(selectedClip); selectedClip = null;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    // FIX5: DeleteSelectedTrack 只能由专用按钮调用
    public void DeleteSelectedTrack()
    {
        if (selectedTrackIndex < 0) return;
        int idx = selectedTrackIndex;
        var track = allTracks.Find(t => t.trackIndex == idx);
        if (track != null)
        {
            if (moduleSystem != null) moduleSystem.RestoreCover(track.trackName);
            if (track.headerObject != null) Destroy(track.headerObject);
            allTracks.Remove(track);
        }
        var toDelete = allEvents.FindAll(e => e.trackIndex == idx);
        foreach (var e in toDelete) { if (e.inspectorPanel != null) Destroy(e.inspectorPanel); if (e.clipObject != null) Destroy(e.clipObject); }
        allEvents.RemoveAll(e => e.trackIndex == idx); trackCount--;
        foreach (var t in allTracks) { if (t.trackIndex > idx) { t.trackIndex--; var btn = t.headerObject?.GetComponent<Button>(); if (btn != null) { int ni = t.trackIndex; btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => SelectTrack(ni)); } } }
        foreach (var e in allEvents) { if (e.trackIndex > idx) e.trackIndex--; }
        selectedTrackIndex = -1; selectedClip = null;
        RefreshClipPositions(); ResizeContent();
        if (moduleSystem != null) moduleSystem.ShowDefaultInspector();
        if (allTracks.Count > 0 && moduleSystem?.tabManager != null) moduleSystem.tabManager.SwitchTab(0);
    }

    private void SetHeaderColor(int index, Color color)
    {
        if (index < 0) return;
        var t = allTracks.Find(x => x.trackIndex == index);
        if (t?.headerObject != null) { var img = t.headerObject.GetComponent<Image>(); if (img != null) img.color = color; }
    }

    private void SetClipColor(TimelineEventData clip, Color color)
    {
        if (clip?.clipObject == null) return;
        var img = clip.clipObject.GetComponent<Image>(); if (img != null) img.color = color;
    }

    public void DeselectAll()
    {
        SetClipColor(selectedClip, defaultClipColor); selectedClip = null;
        HideAllClipPanels();
        SetHeaderColor(selectedTrackIndex, defaultHeaderColor); selectedTrackIndex = -1;
        if (moduleSystem != null) moduleSystem.ShowDefaultInspector();
    }

    private void HideAllClipPanels()
    {
        foreach (var e in allEvents) if (e.inspectorPanel != null) e.inspectorPanel.SetActive(false);
    }

    void RefreshClipPositions()
    {
        foreach (var evt in allEvents)
        {
            if (evt.clipObject != null)
            {
                var rt = evt.clipObject.GetComponent<RectTransform>();
                float yPos = -(evt.trackIndex * (baseTrackHeight + trackSpacing)) - baseTrackHeight / 2f + (-5f + evt.trackIndex * 10f);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, yPos);
            }
        }
    }

    void ResizeContent() { contentParent.sizeDelta = new Vector2(totalDuration * pixelsPerSecond, contentParent.sizeDelta.y); }

    void GenerateGridLines()
    {
        ClearOldObjects("Divider_Template");
        var line = Instantiate(dividerPrefab, contentParent); var rt = line.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(0, -rulerHeight); rt.sizeDelta = new Vector2(contentParent.sizeDelta.x, 2f);
        line.transform.SetAsLastSibling();
    }

    void GenerateRuler()
    {
        ClearOldObjects("Tick_Template");
        for (float time = 0; time <= totalDuration; time += rulerInterval)
        {
            var tick = Instantiate(tickPrefab, contentParent); var rt = tick.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(2f, 10f); rt.anchoredPosition = new Vector2(time * pixelsPerSecond, -rulerHeight);
            var txt = tick.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) { txt.text = FormatTime(time); txt.alignment = TextAlignmentOptions.BottomLeft; var tr = txt.GetComponent<RectTransform>(); tr.anchorMin = tr.anchorMax = tr.pivot = Vector2.zero; tr.anchoredPosition = new Vector2(5f, 0f); }
            tick.transform.SetAsLastSibling();
        }
    }

    void BakeRootMotion(string stateName, float duration)
    {
        if (characterAnimator == null) return;
        int frames = Mathf.CeilToInt(duration * bakeFPS); bakedPositions = new Vector3[frames]; bakedRotations = new Quaternion[frames];
        Vector3 pos0 = characterAnimator.transform.position; Quaternion rot0 = characterAnimator.transform.rotation;
        characterAnimator.Play(stateName, 0, 0f); characterAnimator.Update(0f);
        bakedAnimationLength = characterAnimator.GetCurrentAnimatorStateInfo(0).length;
        if (bakedAnimationLength <= 0.1f) bakedAnimationLength = 1f;
        for (int i = 0; i < frames; i++) { bakedPositions[i] = characterAnimator.transform.position; bakedRotations[i] = characterAnimator.transform.rotation; characterAnimator.Update(1f / bakeFPS); }
        characterAnimator.transform.position = pos0; characterAnimator.transform.rotation = rot0;
        characterAnimator.Play(stateName, 0, 0f); characterAnimator.Update(0f); characterAnimator.speed = 0f;
    }

    void ApplyBakedRootMotion(float time)
    {
        if (bakedPositions == null || bakedPositions.Length == 0 || characterAnimator == null) return;
        float ff = time * bakeFPS; int f1 = Mathf.Clamp(Mathf.FloorToInt(ff), 0, bakedPositions.Length - 1); int f2 = Mathf.Clamp(Mathf.CeilToInt(ff), 0, bakedPositions.Length - 1); float t = ff - f1;
        characterAnimator.transform.position = Vector3.Lerp(bakedPositions[f1], bakedPositions[f2], t);
        characterAnimator.transform.rotation = Quaternion.Slerp(bakedRotations[f1], bakedRotations[f2], t);
    }

    void ClearOldObjects(string keyword)
    {
        for (int i = contentParent.childCount - 1; i >= 0; i--) { var c = contentParent.GetChild(i); if (c.name.Contains(keyword) && c.name.Contains("Clone")) Destroy(c.gameObject); }
        if (trackContainer != null) for (int i = trackContainer.childCount - 1; i >= 0; i--) { var c = trackContainer.GetChild(i); if (c.name.Contains(keyword) && c.name.Contains("Clone")) Destroy(c.gameObject); }
    }

    void Update()
    {
        // FIX5: 只删 Clip，不删轨道
        if (Keyboard.current != null && (Keyboard.current.deleteKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame)) HandleDeleteKey();

        if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
        {
            Vector2 mp = Mouse.current.position.ReadValue(); var vp = contentParent.parent.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(vp, mp) && RectTransformUtility.ScreenPointToLocalPointInRectangle(contentParent, mp, null, out Vector2 lp))
            { float t = Mathf.Clamp(lp.x / pixelsPerSecond, 0, totalDuration); t = Mathf.RoundToInt(t * bakeFPS) / bakeFPS; if (musicSource != null) musicSource.time = t; if (playheadSlider != null) playheadSlider.SetValueWithoutNotify(t / totalDuration); SyncTimelineEvents(t, true); }
        }

        if (Mouse.current != null && verticalScrollbar != null && verticalScrollbar.gameObject.activeSelf)
        { float sd = Mouse.current.scroll.ReadValue().y; if (Mathf.Abs(sd) > 0.1f) verticalScrollbar.value = Mathf.Clamp01(verticalScrollbar.value + sd * 0.2f); }

        SyncVerticalScroll();

        float currentTime = 0f;
        if (isDraggingSlider)
        {
            if (playheadSlider != null) { float raw = playheadSlider.value * totalDuration; currentTime = Mathf.RoundToInt(raw * bakeFPS) / bakeFPS; if (musicSource != null && Mathf.Abs(musicSource.time - currentTime) > 0.035f) musicSource.time = currentTime; }
            DoEdgeScroll();
        }
        else if (musicSource != null && musicSource.clip != null)
        {
            if (musicSource.isPlaying) { currentTime = musicSource.time; if (currentTime >= totalDuration - 0.05f) { musicSource.Pause(); musicSource.time = 0f; currentTime = 0f; } if (playheadSlider != null) playheadSlider.SetValueWithoutNotify(currentTime / totalDuration); AutoScrollToPlayhead(currentTime); }
            else { currentTime = (playheadSlider != null) ? Mathf.RoundToInt(playheadSlider.value * totalDuration * bakeFPS) / bakeFPS : musicSource.time; if (Mathf.Abs(musicSource.time - currentTime) > 0.035f) musicSource.time = currentTime; }
        }

        if (timeDisplayText != null) timeDisplayText.text = $"{FormatTime(currentTime)} / {FormatTime(totalDuration)}";
        if (timeInputField != null && !timeInputField.isFocused) timeInputField.SetTextWithoutNotify(FormatTime(currentTime));
        if (playPauseButtonText != null) playPauseButtonText.text = (musicSource != null && musicSource.isPlaying) ? "❚❚" : "▶";

        if (selectedClip != null) { var p = selectedClip.inspectorPanel?.GetComponent<ClipInspectorPanel>(); if (p != null) p.RefreshDisplay(); }

        bool forceUpdate = requiresInitialSync; if (requiresInitialSync) requiresInitialSync = false;
        SyncTimelineEvents(currentTime, forceUpdate);
    }

    void AutoScrollToPlayhead(float t)
    {
        if (contentParent?.parent == null) return;
        var vp = contentParent.parent.GetComponent<RectTransform>(); float vw = vp.rect.width; if (vw < 10f) return;
        float px = t * pixelsPerSecond, cx = -contentParent.anchoredPosition.x, max = Mathf.Max(0, contentParent.rect.width - vw);
        if (px > cx + vw || px < cx) { float nx = Mathf.Clamp(-px, -max, 0); contentParent.anchoredPosition = new Vector2(nx, contentParent.anchoredPosition.y); var sr = contentParent.GetComponentInParent<ScrollRect>(); if (sr != null && sr.horizontal && max > 0) sr.horizontalNormalizedPosition = Mathf.Clamp01(-nx / max); }
    }

    void FocusOnTime(float targetTime)
    {
        if (contentParent?.parent == null) return;
        var vp = contentParent.parent.GetComponent<RectTransform>(); float vw = vp.rect.width, max = Mathf.Max(0, contentParent.rect.width - vw);
        float nx = Mathf.Clamp(-(targetTime * pixelsPerSecond - vw / 2f), -max, 0); contentParent.anchoredPosition = new Vector2(nx, contentParent.anchoredPosition.y);
        var sr = contentParent.GetComponentInParent<ScrollRect>(); if (sr != null && sr.horizontal && max > 0) sr.horizontalNormalizedPosition = Mathf.Clamp01(-nx / max);
    }

    void DoEdgeScroll()
    {
        if (contentParent?.parent == null) return;
        var vp = contentParent.parent.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(vp, Mouse.current.position.ReadValue(), null, out Vector2 lm))
        {
            float speed = 1500f * Time.deltaTime, delta = 0f, nx2 = (lm.x - vp.rect.xMin) / vp.rect.width;
            if (nx2 > 0.95f) delta = -speed; else if (nx2 < 0.05f) delta = speed;
            if (delta != 0f) { float max = Mathf.Max(0, contentParent.rect.width - vp.rect.width); float nx = Mathf.Clamp(contentParent.anchoredPosition.x + delta, -max, 0); contentParent.anchoredPosition = new Vector2(nx, contentParent.anchoredPosition.y); var sr = contentParent.GetComponentInParent<ScrollRect>(); if (sr != null && sr.horizontal && max > 0) sr.horizontalNormalizedPosition = Mathf.Clamp01(-nx / max); }
        }
    }

    public void OnTimeInputValueChanged(string rawStr)
    {
        string d = ""; foreach (char c in rawStr) if (char.IsDigit(c)) d += c;
        if (string.IsNullOrEmpty(d)) d = "000000"; if (d.Length > 6) d = d.Substring(d.Length - 6); else d = d.PadLeft(6, '0');
        string f = $"{d.Substring(0, 2)}:{d.Substring(2, 2)}:{d.Substring(4, 2)}"; timeInputField.SetTextWithoutNotify(f); timeInputField.caretPosition = f.Length;
    }

    public void OnTimeInputSubmit(string input)
    {
        string[] p = input.Split(':');
        if (p.Length == 3 && int.TryParse(p[0], out int m) && int.TryParse(p[1], out int s) && int.TryParse(p[2], out int fr))
        { float t = Mathf.Clamp((m * 60f) + Mathf.Clamp(s, 0, 59) + (Mathf.Clamp(fr, 0, Mathf.FloorToInt(bakeFPS - 1)) / bakeFPS), 0, totalDuration); FocusOnTime(t); }
        if (timeInputField != null) timeInputField.SetTextWithoutNotify(FormatTime(musicSource != null ? musicSource.time : 0f));
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void SyncVerticalScroll()
    {
        if (verticalScrollbar == null || headerArea == null || trackContainer == null) return;
        float vh = contentParent.rect.height; if (vh < 10f) return;
        float th = trackCount * (baseTrackHeight + trackSpacing), max = Mathf.Max(0, th - vh + rulerHeight + 20f); bool need = max > 0.1f;
        if (verticalScrollbar.gameObject.activeSelf != need) verticalScrollbar.gameObject.SetActive(need);
        if (!need) { trackContainer.anchoredPosition = Vector2.zero; if (isHeaderYStored) headerArea.anchoredPosition = new Vector2(headerArea.anchoredPosition.x, originalHeaderY); return; }
        verticalScrollbar.size = Mathf.Clamp(vh / (th + rulerHeight), 0.05f, 1f);
        float off = (1f - verticalScrollbar.value) * max; trackContainer.anchoredPosition = new Vector2(0, off);
        if (!isHeaderYStored) { originalHeaderY = headerArea.anchoredPosition.y; isHeaderYStored = true; }
        headerArea.anchoredPosition = new Vector2(headerArea.anchoredPosition.x, originalHeaderY + off);
    }

    void SyncTimelineEvents(float currentTime, bool forceSync = false)
    {
        bool playing = (musicSource != null && musicSource.isPlaying), changed = (playing != wasPlaying); wasPlaying = playing;
        bool timeMoved = Mathf.Abs(currentTime - lastEvaluatedTime) > 0.001f; lastEvaluatedTime = currentTime;
        if (characterAnimator == null) return;
        float norm = (currentTime % bakedAnimationLength) / bakedAnimationLength;
        if (playing) { if (characterAnimator.speed == 0f || changed || forceSync) { characterAnimator.Play(currentDanceName, 0, norm); characterAnimator.speed = originalAnimatorSpeed; ApplyBakedRootMotion(currentTime); } }
        else { if (changed || isDraggingSlider || timeMoved || forceSync) { characterAnimator.Play(currentDanceName, 0, norm); characterAnimator.Update(0f); ApplyBakedRootMotion(currentTime); } if (characterAnimator.speed != 0f) characterAnimator.speed = 0f; }
    }

    string FormatTime(float t)
    {
        int m = Mathf.FloorToInt(t / 60f), s = Mathf.FloorToInt(t % 60f), f = Mathf.Min(Mathf.FloorToInt((t % 1f) * bakeFPS), Mathf.FloorToInt(bakeFPS - 1));
        return string.Format("{0:00}:{1:00}:{2:00}", m, s, f);
    }

    public void OnSliderDrag(float value) { }
    public void TogglePlayPause() { if (musicSource == null || musicSource.clip == null) return; if (musicSource.isPlaying) musicSource.Pause(); else musicSource.Play(); }
}
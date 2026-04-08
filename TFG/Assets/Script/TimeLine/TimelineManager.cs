using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Timeline;
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
}

public class TimelineManager : MonoBehaviour
{
    [Header("=== 数据控制中心 ===")]
    public Animator characterAnimator;
    public List<TimelineEventData> allEvents = new List<TimelineEventData>();

    [Header("=== 对接你的模块系统 ===")]
    public DynamicModuleSystem moduleSystem;

    private int selectedTrackIndex = -1;
    private Color originalHeaderColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    private Color originalClipColor = new Color(0f, 1f, 1f, 1f);
    private Color highlightColor = new Color(0.2f, 0.5f, 0.8f, 1f);

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

    [Header("=== 引擎数据 ===")]
    private Vector3[] bakedPositions;
    private Quaternion[] bakedRotations;
    private float bakeFPS = 30f;

    private float bakedAnimationLength = 1f;
    private float originalAnimatorSpeed = 1f;
    private float totalDuration = 60f;

    private bool isDraggingSlider = false;
    private bool isInitialized = false;
    private string currentDanceName = "UI_Test_Dance";

    private bool wasPlaying = false;
    private bool requiresInitialSync = true;

    private RectTransform trackViewport;
    private RectTransform trackContainer;
    private float originalHeaderY;
    private bool isHeaderYStored = false;

    void Awake()
    {
        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.Stop();
        }
        if (characterAnimator != null)
        {
            originalAnimatorSpeed = characterAnimator.speed;
            if (originalAnimatorSpeed <= 0.01f) originalAnimatorSpeed = 1f;
            characterAnimator.speed = 0f; // 开局强行冰冻！
        }
    }

    void Start()
    {
        if (verticalScrollbar != null)
        {
            verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;
            verticalScrollbar.value = 1f;
        }

        if (playheadSlider != null)
        {
            EventTrigger trigger = playheadSlider.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = playheadSlider.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entryDown.callback.AddListener((data) => { isDraggingSlider = true; });
            trigger.triggers.Add(entryDown);

            EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entryUp.callback.AddListener((data) => {
                isDraggingSlider = false;
                OnSliderDrag(playheadSlider.value);
            });
            trigger.triggers.Add(entryUp);

            playheadSlider.onValueChanged.RemoveAllListeners();
            playheadSlider.onValueChanged.AddListener(OnSliderDrag);
        }

        if (!isInitialized) SetupDynamicTimeline(characterAnimator, musicSource, "UI_Test_Dance");
    }

    public void SetupDynamicTimeline(Animator spawnedAnimator, AudioSource assignedAudio, string danceName)
    {
        isInitialized = true;
        currentDanceName = danceName;
        wasPlaying = false;
        requiresInitialSync = true;

        if (spawnedAnimator != null) characterAnimator = spawnedAnimator;
        if (assignedAudio != null) musicSource = assignedAudio;

        if (musicSource != null)
        {
            musicSource.time = 0f;
            if (musicSource.clip != null) totalDuration = musicSource.clip.length;
            else totalDuration = 60f;
        }

        allEvents.Clear();
        trackCount = 1;

        contentParent.pivot = new Vector2(0, 1);
        contentParent.anchorMin = new Vector2(0, 1);
        contentParent.anchorMax = new Vector2(0, 1);

        if (trackViewport == null)
        {
            GameObject tv = new GameObject("TrackViewport");
            trackViewport = tv.AddComponent<RectTransform>();
            trackViewport.SetParent(contentParent, false);
            trackViewport.anchorMin = new Vector2(0, 0);
            trackViewport.anchorMax = new Vector2(1, 1);
            trackViewport.offsetMin = Vector2.zero;
            trackViewport.offsetMax = new Vector2(0, -rulerHeight);
            tv.AddComponent<RectMask2D>();
            trackViewport.SetSiblingIndex(0);

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

        if (playPauseButtonText != null) playPauseButtonText.text = "▶";

        if (playheadSlider != null)
        {
            playheadSlider.transform.SetAsLastSibling();
            RectTransform sliderRt = playheadSlider.GetComponent<RectTransform>();
            sliderRt.offsetMin = new Vector2(sliderRt.offsetMin.x, -2000f);
        }

        BakeRootMotion(danceName, totalDuration);
        DeselectTrack();
    }

    public void AddDynamicTrack(string name, float duration) { int newTrackIndex = trackCount; trackCount++; ResizeContent(); CreateClip(name, newTrackIndex, 0f, duration); if (playheadSlider != null) { playheadSlider.transform.SetAsLastSibling(); RectTransform sliderRt = playheadSlider.GetComponent<RectTransform>(); sliderRt.offsetMin = new Vector2(sliderRt.offsetMin.x, -2000f); } }
    public void SelectTrack(int index) { if (index <= 0) return; if (selectedTrackIndex == index) return; DeselectTrack(); selectedTrackIndex = index; TimelineEventData trackData = allEvents.Find(e => e.trackIndex == index); if (trackData == null) return; if (trackData.headerObject != null) { Image headerImg = trackData.headerObject.GetComponent<Image>(); if (headerImg != null) { originalHeaderColor = headerImg.color; headerImg.color = highlightColor; } } if (trackData.clipObject != null) { Image clipImg = trackData.clipObject.GetComponent<Image>(); if (clipImg != null) { originalClipColor = clipImg.color; clipImg.color = highlightColor; } } if (moduleSystem != null) moduleSystem.ShowInspector(trackData.eventName); if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); }
    public void DeselectTrack() { if (selectedTrackIndex > 0) { TimelineEventData oldTrack = allEvents.Find(e => e.trackIndex == selectedTrackIndex); if (oldTrack != null) { if (oldTrack.headerObject != null) { Image headerImg = oldTrack.headerObject.GetComponent<Image>(); if (headerImg != null) headerImg.color = originalHeaderColor; } if (oldTrack.clipObject != null) { Image clipImg = oldTrack.clipObject.GetComponent<Image>(); if (clipImg != null) clipImg.color = originalClipColor; } } } selectedTrackIndex = -1; if (moduleSystem != null) moduleSystem.ShowDefaultInspector(); }
    public void DeleteSelectedTrack() { if (selectedTrackIndex <= 0) return; int indexToDelete = selectedTrackIndex; TimelineEventData trackData = allEvents.Find(e => e.trackIndex == indexToDelete); if (trackData != null) { if (moduleSystem != null) moduleSystem.RestoreCover(trackData.eventName); if (trackData.clipObject != null) Destroy(trackData.clipObject); if (trackData.headerObject != null) Destroy(trackData.headerObject); allEvents.Remove(trackData); } trackCount--; foreach (var evt in allEvents) { if (evt.trackIndex > indexToDelete) { evt.trackIndex--; if (evt.headerObject != null) { Button btn = evt.headerObject.GetComponent<Button>(); if (btn != null) { btn.onClick.RemoveAllListeners(); int newIndex = evt.trackIndex; btn.onClick.AddListener(() => SelectTrack(newIndex)); } } } } DeselectTrack(); RefreshClipPositions(); ResizeContent(); if (allEvents.Count > 1) { SelectTrack(allEvents[allEvents.Count - 1].trackIndex); } else { if (moduleSystem != null && moduleSystem.tabManager != null) { moduleSystem.tabManager.SwitchTab(0); } } }

    void RefreshClipPositions() { foreach (var evt in allEvents) { if (evt.clipObject != null) { RectTransform rt = evt.clipObject.GetComponent<RectTransform>(); float offset = -5f + (evt.trackIndex * 10f); float yPos = -(evt.trackIndex * (baseTrackHeight + trackSpacing)) - (baseTrackHeight / 2f) + offset; rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, yPos); } } }
    void ResizeContent() { float totalWidth = totalDuration * pixelsPerSecond; contentParent.sizeDelta = new Vector2(totalWidth, contentParent.sizeDelta.y); }
    void GenerateGridLines() { ClearOldObjects("Divider_Template"); float tracksStartY = -rulerHeight; GameObject line = Instantiate(dividerPrefab, contentParent); RectTransform rt = line.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(0, tracksStartY); rt.sizeDelta = new Vector2(contentParent.sizeDelta.x, 2f); line.transform.SetAsLastSibling(); }
    void GenerateRuler() { ClearOldObjects("Tick_Template"); float tracksStartY = -rulerHeight; for (float time = 0; time <= totalDuration; time += rulerInterval) { GameObject tick = Instantiate(tickPrefab, contentParent); float xPos = time * pixelsPerSecond; RectTransform rt = tick.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0.5f, 0f); rt.sizeDelta = new Vector2(2f, 10f); rt.anchoredPosition = new Vector2(xPos, tracksStartY); TextMeshProUGUI txt = tick.GetComponentInChildren<TextMeshProUGUI>(); if (txt != null) { txt.text = FormatTime(time); txt.alignment = TextAlignmentOptions.BottomLeft; RectTransform txtRt = txt.GetComponent<RectTransform>(); txtRt.anchorMin = new Vector2(0, 0); txtRt.anchorMax = new Vector2(0, 0); txtRt.pivot = new Vector2(0f, 0f); txtRt.anchoredPosition = new Vector2(5f, 0f); } tick.transform.SetAsLastSibling(); } }
    public void CreateClip(string name, int trackIndex, float startTime, float duration) { GameObject newClip = Instantiate(clipPrefab, trackContainer); RectTransform rt = newClip.GetComponent<RectTransform>(); GameObject newHeader = null; if (trackIndex == 0) { if (headerArea != null && headerArea.childCount > 0) newHeader = headerArea.GetChild(0).gameObject; } else { if (headerArea != null && headerArea.childCount > 0) { newHeader = Instantiate(headerArea.GetChild(0).gameObject, headerArea); newHeader.name = "Header_" + name; newHeader.SetActive(true); TextMeshProUGUI headerText = newHeader.GetComponentInChildren<TextMeshProUGUI>(); if (headerText != null) headerText.text = name; Button btn = newHeader.GetComponent<Button>(); if (btn == null) btn = newHeader.AddComponent<Button>(); btn.transition = Selectable.Transition.None; int boundIndex = trackIndex; btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => SelectTrack(boundIndex)); } } TimelineEventData newEvent = new TimelineEventData { eventName = name, trackIndex = trackIndex, startTime = startTime, duration = duration, clipObject = newClip, headerObject = newHeader }; allEvents.Add(newEvent); TimelineClipUI clipUI = newClip.AddComponent<TimelineClipUI>(); clipUI.manager = this; clipUI.eventData = newEvent; float xPos = startTime * pixelsPerSecond; float offset = -5f + (trackIndex * 10f); float yPos = -(trackIndex * (baseTrackHeight + trackSpacing)) - (baseTrackHeight / 2f) + offset; rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 0.5f); rt.anchoredPosition = new Vector2(xPos, yPos); float clipHeight = baseTrackHeight * 0.8f; rt.sizeDelta = new Vector2(duration * pixelsPerSecond, clipHeight); TextMeshProUGUI text = newClip.GetComponentInChildren<TextMeshProUGUI>(); if (text != null) text.text = name; }

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

        AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);
        bakedAnimationLength = stateInfo.length;
        if (bakedAnimationLength <= 0.1f) bakedAnimationLength = 1f;

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
        characterAnimator.speed = 0f;
    }

    void ApplyBakedRootMotion(float time)
    {
        if (bakedPositions == null || bakedPositions.Length == 0 || characterAnimator == null) return;

        float frameF = time * bakeFPS;
        int frame1 = Mathf.FloorToInt(frameF);
        int frame2 = Mathf.CeilToInt(frameF);

        frame1 = Mathf.Clamp(frame1, 0, bakedPositions.Length - 1);
        frame2 = Mathf.Clamp(frame2, 0, bakedPositions.Length - 1);

        float t = frameF - frame1;
        characterAnimator.transform.position = Vector3.Lerp(bakedPositions[frame1], bakedPositions[frame2], t);
        characterAnimator.transform.rotation = Quaternion.Slerp(bakedRotations[frame1], bakedRotations[frame2], t);
    }

    void ClearOldObjects(string nameKeyword) { for (int i = contentParent.childCount - 1; i >= 0; i--) { Transform child = contentParent.GetChild(i); if (child.name.Contains(nameKeyword) && child.name.Contains("Clone")) Destroy(child.gameObject); } if (trackContainer != null) { for (int i = trackContainer.childCount - 1; i >= 0; i--) { Transform child = trackContainer.GetChild(i); if (child.name.Contains(nameKeyword) && child.name.Contains("Clone")) Destroy(child.gameObject); } } }

    void Update()
    {
        if (Keyboard.current != null && (Keyboard.current.deleteKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame)) DeleteSelectedTrack();

        if (Mouse.current != null && verticalScrollbar != null && verticalScrollbar.gameObject.activeSelf)
        {
            float scrollDelta = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollDelta) > 0.1f)
            {
                float sensitivity = 0.2f;
                verticalScrollbar.value += scrollDelta * sensitivity;
                verticalScrollbar.value = Mathf.Clamp01(verticalScrollbar.value);
            }
        }

        SyncVerticalScroll();

        float currentTime = 0f;
        if (isDraggingSlider && playheadSlider != null)
        {
            currentTime = playheadSlider.value * totalDuration;
        }
        else if (musicSource != null && musicSource.clip != null)
        {
            currentTime = musicSource.time;

            if (playheadSlider != null && !isDraggingSlider)
            {
                playheadSlider.SetValueWithoutNotify(currentTime / totalDuration);
            }
        }

        if (timeDisplayText != null)
            timeDisplayText.text = $"{FormatTime(currentTime)} / {FormatTime(totalDuration)}";

        // 【新增自动 UI 联动】：无论你点哪个按钮，只要音乐状态变了，图标自动跟着变！
        if (playPauseButtonText != null)
        {
            playPauseButtonText.text = (musicSource != null && musicSource.isPlaying) ? "❚❚" : "▶";
        }

        bool forceUpdate = requiresInitialSync;
        if (requiresInitialSync) requiresInitialSync = false;

        SyncTimelineEvents(currentTime, forceUpdate);
    }

    void SyncVerticalScroll() { if (verticalScrollbar != null && headerArea != null && trackContainer != null) { float visibleHeight = contentParent.rect.height; if (visibleHeight < 10f) return; float totalTracksHeight = trackCount * (baseTrackHeight + trackSpacing); float maxScroll = Mathf.Max(0, totalTracksHeight - visibleHeight + rulerHeight + 20f); bool needsScroll = maxScroll > 0.1f; if (verticalScrollbar.gameObject.activeSelf != needsScroll) { verticalScrollbar.gameObject.SetActive(needsScroll); } if (!needsScroll) { trackContainer.anchoredPosition = Vector2.zero; if (isHeaderYStored) headerArea.anchoredPosition = new Vector2(headerArea.anchoredPosition.x, originalHeaderY); return; } float sizeRatio = visibleHeight / (totalTracksHeight + rulerHeight); verticalScrollbar.size = Mathf.Clamp(sizeRatio, 0.05f, 1f); float scrollOffset = (1f - verticalScrollbar.value) * maxScroll; trackContainer.anchoredPosition = new Vector2(0, scrollOffset); if (!isHeaderYStored) { originalHeaderY = headerArea.anchoredPosition.y; isHeaderYStored = true; } headerArea.anchoredPosition = new Vector2(headerArea.anchoredPosition.x, originalHeaderY + scrollOffset); } }

    void SyncTimelineEvents(float currentTime, bool forceSync = false)
    {
        // 彻底移除了授权锁，现在只需要判断音乐是否在播，你场景里的 Start 按钮彻底自由了！
        bool isPlaying = (musicSource != null && musicSource.isPlaying);
        bool stateChanged = (isPlaying != wasPlaying);
        wasPlaying = isPlaying;

        foreach (var evt in allEvents)
        {
            if (currentTime >= evt.startTime && currentTime <= (evt.startTime + evt.duration))
            {
                if (evt.trackIndex == 0 && characterAnimator != null)
                {
                    float localTime = currentTime - evt.startTime;
                    float normalizedTime = (localTime % bakedAnimationLength) / bakedAnimationLength;

                    if (isPlaying)
                    {
                        if (characterAnimator.speed == 0f || stateChanged || forceSync)
                        {
                            characterAnimator.Play(currentDanceName, 0, normalizedTime);
                            characterAnimator.speed = originalAnimatorSpeed;
                            ApplyBakedRootMotion(localTime);
                        }
                    }
                    else
                    {
                        if (stateChanged || isDraggingSlider || forceSync)
                        {
                            characterAnimator.Play(currentDanceName, 0, normalizedTime);
                            characterAnimator.Update(0f);
                            ApplyBakedRootMotion(localTime);
                        }

                        // 【最完美的防抖底线】：只要没在播放，永远强制锁死速度 0
                        if (characterAnimator.speed != 0f) characterAnimator.speed = 0f;
                    }
                }
            }
        }
    }

    string FormatTime(float t)
    {
        int m = Mathf.FloorToInt(t / 60F);
        int s = Mathf.FloorToInt(t % 60F);
        int f = Mathf.FloorToInt((t % 1f) * bakeFPS);
        return string.Format("{0:00}:{1:00}:{2:00}", m, s, f);
    }

    public void OnSliderDrag(float value)
    {
        if (!isDraggingSlider) return;

        if (musicSource == null || musicSource.clip == null) return;
        float targetTime = value * totalDuration;

        int targetFrame = Mathf.RoundToInt(targetTime * bakeFPS);
        targetTime = targetFrame / bakeFPS;

        if (Mathf.Abs(musicSource.time - targetTime) > 0.001f)
        {
            musicSource.time = targetTime;
            if (characterAnimator != null) characterAnimator.speed = 0f;
            SyncTimelineEvents(targetTime);
        }
    }

    public void TogglePlayPause()
    {
        if (musicSource == null || musicSource.clip == null) return;

        // 这里的文本切换逻辑我移到了 Update 里自动处理，你不再需要手动点它来授权了
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
        }
        else
        {
            musicSource.Play();
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ==========================================
// 左右拉伸手柄
// ==========================================
public class TimelineClipHandle : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public bool isLeftHandle;
    public TimelineClipUI parentClip;

    public void OnPointerDown(PointerEventData eventData) { parentClip.OnHandlePointerDown(isLeftHandle, eventData); }
    public void OnDrag(PointerEventData eventData) { parentClip.OnHandleDrag(isLeftHandle, eventData); }
}

// ==========================================
// Clip 方块 UI 行为
// ==========================================
public class TimelineClipUI : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [HideInInspector] public TimelineManager manager;
    [HideInInspector] public TimelineEventData eventData;

    private RectTransform rectTransform;
    private bool isDraggingBody = false;
    private float originalPointerX;
    private float originalStartTime;
    private float originalDuration;
    private Vector2 originalPosition;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        CreateHandle("Handle_Left", true);
        CreateHandle("Handle_Right", false);
    }

    void CreateHandle(string name, bool isLeft)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(this.transform, false);

        Image img = obj.AddComponent<Image>();
        img.color = new Color(1f, 0.9f, 0f, 0.6f);
        img.raycastTarget = true;

        RectTransform rt = obj.GetComponent<RectTransform>();
        if (isLeft)
        {
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
        }
        else
        {
            rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 0.5f);
        }
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(25f, 0);

        TimelineClipHandle h = obj.AddComponent<TimelineClipHandle>();
        h.isLeftHandle = isLeft;
        h.parentClip = this;
    }

    // ==========================================
    // 点击 Clip 主体 → 选中该 Clip（不是选轨道）
    // ==========================================
    public void OnPointerDown(PointerEventData data)
    {
        isDraggingBody = true;
        RecordOriginalData(data);

        // 【改动】：选中 Clip，而非选轨道
        if (manager != null && eventData != null)
            manager.SelectClip(eventData);
    }

    public void OnDrag(PointerEventData data)
    {
        if (!isDraggingBody || manager == null || eventData == null) return;

        float deltaTime = GetDeltaX(data) / manager.pixelsPerSecond;
        float newStartTime = Mathf.Max(0f, originalStartTime + deltaTime);

        eventData.startTime = newStartTime;
        rectTransform.anchoredPosition = new Vector2(newStartTime * manager.pixelsPerSecond, originalPosition.y);
    }

    // ==========================================
    // 手柄按下 → 也选中该 Clip
    // ==========================================
    public void OnHandlePointerDown(bool isLeft, PointerEventData data)
    {
        isDraggingBody = false;
        RecordOriginalData(data);

        if (manager != null && eventData != null)
            manager.SelectClip(eventData);
    }

    public void OnHandleDrag(bool isLeft, PointerEventData data)
    {
        if (manager == null || eventData == null) return;

        float deltaTime = GetDeltaX(data) / manager.pixelsPerSecond;
        float minDur = 0.5f;

        if (!isLeft) // 右手柄：改时长
        {
            eventData.duration = Mathf.Max(minDur, originalDuration + deltaTime);
            rectTransform.sizeDelta = new Vector2(eventData.duration * manager.pixelsPerSecond, rectTransform.sizeDelta.y);
        }
        else // 左手柄：改起始时间 + 时长
        {
            float newStart = originalStartTime + deltaTime;
            float newDur = originalDuration - deltaTime;

            if (newDur < minDur) { newDur = minDur; newStart = originalStartTime + (originalDuration - minDur); }
            if (newStart < 0f) { newStart = 0f; newDur = originalStartTime + originalDuration; }

            eventData.startTime = newStart;
            eventData.duration = newDur;
            rectTransform.anchoredPosition = new Vector2(newStart * manager.pixelsPerSecond, originalPosition.y);
            rectTransform.sizeDelta = new Vector2(newDur * manager.pixelsPerSecond, rectTransform.sizeDelta.y);
        }
    }

    private void RecordOriginalData(PointerEventData data)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform, data.position, data.pressEventCamera, out Vector2 p);
        originalPointerX = p.x;
        originalStartTime = eventData.startTime;
        originalDuration = eventData.duration;
        originalPosition = rectTransform.anchoredPosition;
    }

    private float GetDeltaX(PointerEventData data)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform, data.position, data.pressEventCamera, out Vector2 p);
        return p.x - originalPointerX;
    }
}
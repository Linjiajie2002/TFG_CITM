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
    // 点击 Clip 主体 → 选中该 Clip
    // ==========================================
    public void OnPointerDown(PointerEventData data)
    {
        isDraggingBody = true;
        RecordOriginalData(data);

        if (manager != null && eventData != null)
            manager.SelectClip(eventData);
    }

    // ==========================================
    // 拖拽 Clip 主体 → 防穿模逻辑恢复！
    // ==========================================
    public void OnDrag(PointerEventData data)
    {
        if (!isDraggingBody || manager == null || eventData == null) return;

        float deltaX = GetDeltaX(data);
        float deltaTime = deltaX / manager.pixelsPerSecond;
        float newStartTime = originalStartTime + deltaTime;

        // 【关键恢复】：向大管家询问物理墙壁的位置！
        manager.GetAllowedTimeRange(eventData, originalStartTime, out float minTime, out float maxTime);

        // 死死卡在墙壁中间，不能越界！
        newStartTime = Mathf.Clamp(newStartTime, minTime, maxTime - originalDuration);

        eventData.startTime = newStartTime;
        rectTransform.anchoredPosition = new Vector2(newStartTime * manager.pixelsPerSecond, originalPosition.y);
    }

    // ==========================================
    // 手柄按下 → 选中该 Clip
    // ==========================================
    public void OnHandlePointerDown(bool isLeft, PointerEventData data)
    {
        isDraggingBody = false;
        RecordOriginalData(data);

        if (manager != null && eventData != null)
            manager.SelectClip(eventData);
    }

    // ==========================================
    // 拖拽边缘手柄 → 防穿模逻辑恢复！
    // ==========================================
    public void OnHandleDrag(bool isLeft, PointerEventData data)
    {
        if (manager == null || eventData == null) return;

        float deltaX = GetDeltaX(data);
        float deltaTime = deltaX / manager.pixelsPerSecond;
        float minDur = 0.5f;

        // 【关键恢复】：向大管家询问物理墙壁的位置！
        manager.GetAllowedTimeRange(eventData, originalStartTime, out float minTime, out float maxTime);

        if (!isLeft) // 右手柄：改时长
        {
            float newDuration = originalDuration + deltaTime;
            float maxAllowedDuration = maxTime - originalStartTime; // 向右拉不能超过右边方块的开头

            // 夹在最小长度和最大允许长度之间
            newDuration = Mathf.Clamp(newDuration, minDur, maxAllowedDuration);

            eventData.duration = newDuration;
            rectTransform.sizeDelta = new Vector2(newDuration * manager.pixelsPerSecond, rectTransform.sizeDelta.y);
        }
        else // 左手柄：改起始时间 + 时长
        {
            float newStart = originalStartTime + deltaTime;
            float newDur = originalDuration - deltaTime;

            // 向左拉撞到了墙壁（如0秒，或左侧的其他方块）
            if (newStart < minTime)
            {
                newStart = minTime;
                newDur = originalStartTime + originalDuration - minTime;
            }

            // 撞到了自己的最小长度
            if (newDur < minDur)
            {
                newDur = minDur;
                newStart = originalStartTime + (originalDuration - minDur);
            }

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
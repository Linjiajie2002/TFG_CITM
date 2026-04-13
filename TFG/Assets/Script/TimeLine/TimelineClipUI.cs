using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ==========================================
// 左右拉伸手柄（新增 IPointerUpHandler 监听松开）
// ==========================================
public class TimelineClipHandle : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public bool isLeftHandle;
    public TimelineClipUI parentClip;

    public void OnPointerDown(PointerEventData eventData) { parentClip.OnHandlePointerDown(isLeftHandle, eventData); }
    public void OnDrag(PointerEventData eventData) { parentClip.OnHandleDrag(isLeftHandle, eventData); }
    public void OnPointerUp(PointerEventData eventData) { parentClip.OnPointerUp(eventData); }
}

// ==========================================
// Clip 方块 UI 行为（新增边缘滚动 & Update 循环）
// ==========================================
public class TimelineClipUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [HideInInspector] public TimelineManager manager;
    [HideInInspector] public TimelineEventData eventData;

    private RectTransform rectTransform;

    // --- 拖拽与滚动状态 ---
    private bool isDragging = false;
    private bool isDraggingBody = false;
    private bool isHandleDragging = false;
    private bool isLeftHandleDragging = false;
    private PointerEventData currentPointerData;

    // --- 原始数据 ---
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
    // Update 循环：负责在鼠标按住静止时，持续滚动时间轴并更新 Clip 位置
    // ==========================================
    void Update()
    {
        if (isDragging && currentPointerData != null && manager != null && manager.contentParent != null)
        {
            // 1. 探测屏幕边缘并让大背景滚动
            DoEdgeScroll();

            // 2. 背景滚动会导致本地坐标变化，强制重新计算防穿模
            if (isDraggingBody) ProcessBodyDrag();
            else if (isHandleDragging) ProcessHandleDrag(isLeftHandleDragging);
        }
    }

    // ==========================================
    // 鼠标输入事件响应
    // ==========================================
    public void OnPointerDown(PointerEventData data)
    {
        isDraggingBody = true;
        isDragging = true;
        currentPointerData = data;
        RecordOriginalData(data);

        if (manager != null && eventData != null)
            manager.SelectClip(eventData);
    }

    public void OnHandlePointerDown(bool isLeft, PointerEventData data)
    {
        isDraggingBody = false;
        isHandleDragging = true;
        isLeftHandleDragging = isLeft;
        isDragging = true;
        currentPointerData = data;
        RecordOriginalData(data);

        if (manager != null && eventData != null)
            manager.SelectClip(eventData);
    }

    public void OnPointerUp(PointerEventData data)
    {
        isDragging = false;
        isDraggingBody = false;
        isHandleDragging = false;
        currentPointerData = null;
    }

    public void OnDrag(PointerEventData data)
    {
        currentPointerData = data;
        ProcessBodyDrag();
    }

    public void OnHandleDrag(bool isLeft, PointerEventData data)
    {
        currentPointerData = data;
        ProcessHandleDrag(isLeft);
    }

    // ==========================================
    // 核心物理与拖拽逻辑
    // ==========================================
    private void ProcessBodyDrag()
    {
        if (!isDraggingBody || manager == null || eventData == null || currentPointerData == null) return;

        float deltaX = GetDeltaX(currentPointerData);
        float deltaTime = deltaX / manager.pixelsPerSecond;
        float newStartTime = originalStartTime + deltaTime;

        // 向大管家询问当前的碰撞墙壁边界
        manager.GetAllowedTimeRange(eventData, originalStartTime, out float minTime, out float maxTime);
        newStartTime = Mathf.Clamp(newStartTime, minTime, maxTime - originalDuration);

        eventData.startTime = newStartTime;
        rectTransform.anchoredPosition = new Vector2(newStartTime * manager.pixelsPerSecond, originalPosition.y);
    }

    private void ProcessHandleDrag(bool isLeft)
    {
        if (manager == null || eventData == null || currentPointerData == null) return;

        float deltaX = GetDeltaX(currentPointerData);
        float deltaTime = deltaX / manager.pixelsPerSecond;
        float minDur = 0.5f;

        manager.GetAllowedTimeRange(eventData, originalStartTime, out float minTime, out float maxTime);

        if (!isLeft) // 右手柄
        {
            float newDuration = originalDuration + deltaTime;
            float maxAllowedDuration = maxTime - originalStartTime;
            newDuration = Mathf.Clamp(newDuration, minDur, maxAllowedDuration);

            eventData.duration = newDuration;
            rectTransform.sizeDelta = new Vector2(newDuration * manager.pixelsPerSecond, rectTransform.sizeDelta.y);
        }
        else // 左手柄
        {
            float newStart = originalStartTime + deltaTime;
            float newDur = originalDuration - deltaTime;

            if (newStart < minTime)
            {
                newStart = minTime;
                newDur = originalStartTime + originalDuration - minTime;
            }
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

    // ==========================================
    // 边缘滚动算法 (完美兼容 InputSystem 和传统鼠标/触摸屏)
    // ==========================================
    private void DoEdgeScroll()
    {
        var vp = manager.contentParent.parent.GetComponent<RectTransform>();
        if (vp == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(vp, currentPointerData.position, currentPointerData.pressEventCamera, out Vector2 lm))
        {
            float speed = 1500f * Time.deltaTime;
            float delta = 0f;

            // 计算鼠标在显示视口内的相对横向比例 (0 = 最左，1 = 最右)
            float nx2 = (lm.x - vp.rect.xMin) / vp.rect.width;

            // 触发区域：左右最边缘的 5%
            if (nx2 > 0.95f) delta = -speed;
            else if (nx2 < 0.05f) delta = speed;

            if (delta != 0f)
            {
                float max = Mathf.Max(0, manager.contentParent.rect.width - vp.rect.width);
                float nx = Mathf.Clamp(manager.contentParent.anchoredPosition.x + delta, -max, 0);
                manager.contentParent.anchoredPosition = new Vector2(nx, manager.contentParent.anchoredPosition.y);

                // 同步下方底部的滚动条位置
                var sr = manager.contentParent.GetComponentInParent<ScrollRect>();
                if (sr != null && sr.horizontal && max > 0)
                    sr.horizontalNormalizedPosition = Mathf.Clamp01(-nx / max);
            }
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
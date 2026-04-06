using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ==========================================
// 【新增黑科技】：独立的把手感应器！
// 它专门负责挂在黄色色块上，100% 拦截鼠标拖拽，绝不会误判为中间！
// ==========================================
public class TimelineClipHandle : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public bool isLeftHandle;
    public TimelineClipUI parentClip;

    public void OnPointerDown(PointerEventData eventData)
    {
        parentClip.OnHandlePointerDown(isLeftHandle, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        parentClip.OnHandleDrag(isLeftHandle, eventData);
    }
}

// ==========================================
// 原本的方块大脑
// ==========================================
public class TimelineClipUI : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [HideInInspector] public TimelineManager manager;
    [HideInInspector] public TimelineEventData eventData;

    private RectTransform rectTransform;

    // 状态标记
    private bool isDraggingBody = false;
    private float originalPointerX;
    private float originalStartTime;
    private float originalDuration;
    private Vector2 originalPosition;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        // 生成自带“物理防弹衣”的真实把手！
        CreateRealHandle("Handle_Left", true);
        CreateRealHandle("Handle_Right", false);
    }

    // 纯代码生成带触发器的黄色把手
    void CreateRealHandle(string name, bool isLeft)
    {
        GameObject handleObj = new GameObject(name);
        handleObj.transform.SetParent(this.transform, false);

        Image img = handleObj.AddComponent<Image>();
        img.color = new Color(1f, 0.9f, 0f, 0.6f);

        // 【极度关键】：设为 true！让黄色色块变成真实的实体，拦截鼠标！
        img.raycastTarget = true;

        RectTransform rt = handleObj.GetComponent<RectTransform>();
        if (isLeft)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
        }
        else
        {
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 0.5f);
        }

        rt.anchoredPosition = Vector2.zero;

        // 把手加宽到 25 像素，闭着眼睛都能点到！
        rt.sizeDelta = new Vector2(25f, 0);

        // 将刚刚写好的专属感应器挂给它
        TimelineClipHandle handleScript = handleObj.AddComponent<TimelineClipHandle>();
        handleScript.isLeftHandle = isLeft;
        handleScript.parentClip = this;
    }

    // ===================================
    // 模式一：你点击了中间的蓝色区域（平移）
    // ===================================
    public void OnPointerDown(PointerEventData data)
    {
        isDraggingBody = true;
        RecordOriginalData(data);

        // 【新增】：点击方块主体时，通知系统选中！
        if (manager != null && eventData != null) manager.SelectTrack(eventData.trackIndex);
    }

    public void OnDrag(PointerEventData data)
    {
        if (!isDraggingBody || manager == null || eventData == null) return;

        float deltaX = GetDeltaX(data);
        float deltaTime = deltaX / manager.pixelsPerSecond;

        float newStartTime = originalStartTime + deltaTime;
        if (newStartTime < 0f) newStartTime = 0f;

        eventData.startTime = newStartTime;
        rectTransform.anchoredPosition = new Vector2(newStartTime * manager.pixelsPerSecond, originalPosition.y);
    }

    // ===================================
    // 模式二：你点击了黄色的把手（由 TimelineClipHandle 触发）
    // ===================================
    public void OnHandlePointerDown(bool isLeft, PointerEventData data)
    {
        isDraggingBody = false; // 标记现在绝不是在拖主体
        RecordOriginalData(data);

        // 【新增】：点击方块边缘把手时，也通知系统选中！
        if (manager != null && eventData != null) manager.SelectTrack(eventData.trackIndex);
    }

    public void OnHandleDrag(bool isLeft, PointerEventData data)
    {
        if (manager == null || eventData == null) return;

        float deltaX = GetDeltaX(data);
        float deltaTime = deltaX / manager.pixelsPerSecond;
        float minDuration = 0.5f;

        if (!isLeft) // 拉右边的黄条
        {
            float newDuration = originalDuration + deltaTime;
            if (newDuration < minDuration) newDuration = minDuration;

            eventData.duration = newDuration;
            rectTransform.sizeDelta = new Vector2(newDuration * manager.pixelsPerSecond, rectTransform.sizeDelta.y);
        }
        else // 拉左边的黄条
        {
            float newStartTime = originalStartTime + deltaTime;
            float newDuration = originalDuration - deltaTime;

            if (newDuration < minDuration)
            {
                newDuration = minDuration;
                newStartTime = originalStartTime + (originalDuration - minDuration);
            }
            if (newStartTime < 0f)
            {
                newStartTime = 0f;
                newDuration = originalStartTime + originalDuration;
            }

            eventData.startTime = newStartTime;
            eventData.duration = newDuration;

            rectTransform.anchoredPosition = new Vector2(newStartTime * manager.pixelsPerSecond, originalPosition.y);
            rectTransform.sizeDelta = new Vector2(newDuration * manager.pixelsPerSecond, rectTransform.sizeDelta.y);
        }
    }

    // ===================================
    // 内部计算工具
    // ===================================
    private void RecordOriginalData(PointerEventData data)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            data.position,
            data.pressEventCamera,
            out Vector2 parentLocalClickPos);

        originalPointerX = parentLocalClickPos.x;
        originalStartTime = eventData.startTime;
        originalDuration = eventData.duration;
        originalPosition = rectTransform.anchoredPosition;
    }

    private float GetDeltaX(PointerEventData data)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            data.position,
            data.pressEventCamera,
            out Vector2 parentLocalClickPos);

        return parentLocalClickPos.x - originalPointerX;
    }
}
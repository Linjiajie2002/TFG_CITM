using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TimelineManager : MonoBehaviour
{
    [Header("=== 核心组件 ===")]
    public AudioSource musicSource;
    public RectTransform contentParent;
    public Slider playheadSlider;
    public TextMeshProUGUI timeDisplayText;

    [Header("=== 预制体 ===")]
    public GameObject clipPrefab;
    public GameObject tickPrefab;
    public GameObject dividerPrefab;

    [Header("=== 配置 ===")]
    public float pixelsPerSecond = 100f;
    public int trackCount = 3;
    public float rulerInterval = 5.0f;

    [Header("=== 布局微调 ===")]
    public float rulerHeight = 30f; // 【新增】刻度尺的专属高度 (25-30比较舒服)

    // 内部变量
    private float totalDuration = 60f;
    private bool isDraggingSlider = false;

    void Start()
    {
        if (musicSource != null && musicSource.clip != null)
            totalDuration = musicSource.clip.length;

        InitializeTimeline();

        if (playheadSlider != null)
            playheadSlider.onValueChanged.AddListener(OnSliderDrag);
    }

    void InitializeTimeline()
    {
        ResizeContentWidth();
        GenerateGridLines(); // 画线逻辑变了
        GenerateRuler();     // 刻度逻辑变了

        // 生成测试
        CreateClip("MIKU_01", 0, 1.0f, 4.0f);
        CreateClip("Red_Alert", 1, 0.0f, 10.0f);
        if (trackCount > 2) CreateClip("Star_Burst", 2, 5.5f, 2.0f);
    }

    void ResizeContentWidth()
    {
        float totalWidth = totalDuration * pixelsPerSecond;
        contentParent.sizeDelta = new Vector2(totalWidth, contentParent.sizeDelta.y);
    }

    // --- 【修改 1】: 分割线算法 ---
    void GenerateGridLines()
    {
        ClearOldObjects("Divider_Template");

        float totalWidth = contentParent.sizeDelta.x;
        float totalHeight = contentParent.sizeDelta.y;

        // 1. 算出轨道区域有多高 (总高 - 刻度尺高)
        float trackAreaHeight = totalHeight - rulerHeight;

        // 2. 算出每条轨道分到多少
        float singleTrackHeight = trackAreaHeight / (float)trackCount;

        // 3. 计算起始点 (注意：Pivot Y=0.5，所以顶端是 totalHeight/2)
        float topEdge = totalHeight / 2f;

        // 轨道是从 刻度尺下面 开始算的
        float tracksStartY = topEdge - rulerHeight;

        // --- 第一步：画一条刻度尺和轨道之间的分界线 ---
        SpawnLine(0, tracksStartY, totalWidth);

        // --- 第二步：画轨道之间的线 ---
        for (int i = 1; i <= trackCount; i++)
        {
            // 往下数 i 行
            float yPos = tracksStartY - (i * singleTrackHeight);
            SpawnLine(i, yPos, totalWidth);
        }
    }

    // 辅助画线函数
    void SpawnLine(int index, float yPos, float width)
    {
        GameObject line = Instantiate(dividerPrefab, contentParent);
        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, yPos);
        rt.sizeDelta = new Vector2(width, 2f);
        line.transform.SetAsFirstSibling();
    }

    // --- 【修改 2】: 刻度尺生成在专属区域 ---
    void GenerateRuler()
    {
        ClearOldObjects("Tick_Template");
        float totalHeight = contentParent.sizeDelta.y;
        float rulerTopY = totalHeight / 2f; // 这是最顶端的坐标

        for (float time = 0; time <= totalDuration; time += rulerInterval)
        {
            GameObject tick = Instantiate(tickPrefab, contentParent);
            float xPos = time * pixelsPerSecond;
            RectTransform rt = tick.GetComponent<RectTransform>();

            // 【修改这里】
            // 原来是 -5f，如果你觉得低了，就改小一点，甚至改成正数试试
            // 比如改成 +10f 看看是不是就飞上去了？
            // 或者检查你的 Ruler Height 是不是设太小了（建议设为 40）
            rt.anchoredPosition = new Vector2(xPos, rulerTopY + 25f);

            TextMeshProUGUI txt = tick.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = FormatTime(time);

            tick.transform.SetAsFirstSibling();
        }
    }

    // --- 【修改 3】: Clip 生成逻辑适配新高度 ---
    public void CreateClip(string name, int trackIndex, float startTime, float duration)
    {
        if (trackIndex >= trackCount) return;

        GameObject newClip = Instantiate(clipPrefab, contentParent);

        // 重新计算轨道高度参数
        float totalHeight = contentParent.sizeDelta.y;
        float trackAreaHeight = totalHeight - rulerHeight; // 减去刻度尺
        float singleTrackHeight = trackAreaHeight / (float)trackCount; // 剩下的均分

        float topEdge = totalHeight / 2f;
        float tracksStartY = topEdge - rulerHeight; // 轨道起始线

        // Y = 轨道起始线 - (跳过前面几行) - (半行居中)
        float yPos = tracksStartY - (trackIndex * singleTrackHeight) - (singleTrackHeight / 2f);

        float xPos = startTime * pixelsPerSecond;
        RectTransform rt = newClip.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(xPos, yPos);

        // 高度留白
        float clipHeight = singleTrackHeight - 10f;
        if (clipHeight < 5f) clipHeight = 5f;

        rt.sizeDelta = new Vector2(duration * pixelsPerSecond, clipHeight);

        TextMeshProUGUI text = newClip.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = name;
    }

    // ... (ClearOldObjects, Update, FormatTime 等保持不变) ...
    void ClearOldObjects(string nameKeyword) { for (int i = contentParent.childCount - 1; i >= 0; i--) { Transform child = contentParent.GetChild(i); if (child.name.Contains(nameKeyword) && child.name.Contains("Clone")) Destroy(child.gameObject); } }
    void Update() { if (musicSource == null || musicSource.clip == null) return; if (!isDraggingSlider && playheadSlider != null) playheadSlider.value = musicSource.time / totalDuration; if (timeDisplayText != null) timeDisplayText.text = $"{FormatTime(musicSource.time)} / {FormatTime(totalDuration)}"; }
    string FormatTime(float t) { int m = Mathf.FloorToInt(t / 60F); int s = Mathf.FloorToInt(t % 60F); return string.Format("{0:00}:{1:00}", m, s); }
    public void OnSliderDrag(float value) { }
}
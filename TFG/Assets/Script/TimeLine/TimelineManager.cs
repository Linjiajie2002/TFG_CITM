using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TimelineManager : MonoBehaviour
{
    public AudioSource musicSource;
    public RectTransform contentParent;
    public Slider playheadSlider;
    public TextMeshProUGUI timeDisplayText;


    public GameObject clipPrefab;
    public GameObject tickPrefab;
    public GameObject dividerPrefab;

    public float pixelsPerSecond = 100f;
    public int trackCount = 3;
    public float rulerInterval = 5.0f;


    public float rulerHeight = 30f;

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
        GenerateGridLines(); 
        GenerateRuler();

        // generate sample clips
        CreateClip("MIKU_01", 0, 1.0f, 4.0f);
        CreateClip("Red_Alert", 1, 0.0f, 10.0f);
        if (trackCount > 2) CreateClip("Star_Burst", 2, 5.5f, 2.0f);
    }

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

        // Calculate track area height (excluding ruler)
        float trackAreaHeight = totalHeight - rulerHeight;

        // Calculate single track height
        float singleTrackHeight = trackAreaHeight / (float)trackCount;

        // calculate top edge y position
        float topEdge = totalHeight / 2f;

        // calculate tracks start y position
        float tracksStartY = topEdge - rulerHeight;

        // Draw the top line of the tracks
        SpawnLine(0, tracksStartY, totalWidth);

        // Draw lines between tracks
        for (int i = 1; i <= trackCount; i++)
        {
           
            float yPos = tracksStartY - (i * singleTrackHeight);
            SpawnLine(i, yPos, totalWidth);
        }
    }

    // Spawns a horizontal line at given y position with specified width
    void SpawnLine(int index, float yPos, float width)
    {
        GameObject line = Instantiate(dividerPrefab, contentParent);
        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, yPos);
        rt.sizeDelta = new Vector2(width, 2f);
        line.transform.SetAsFirstSibling();
    }

    //Tick marks generation
    void GenerateRuler()
    {
        ClearOldObjects("Tick_Template");
        float totalHeight = contentParent.sizeDelta.y;
        float rulerTopY = totalHeight / 2f; // top edge y position

        for (float time = 0; time <= totalDuration; time += rulerInterval)
        {
            GameObject tick = Instantiate(tickPrefab, contentParent);
            float xPos = time * pixelsPerSecond;
            RectTransform rt = tick.GetComponent<RectTransform>();

            rt.anchoredPosition = new Vector2(xPos, rulerTopY + 25f);

            TextMeshProUGUI txt = tick.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = FormatTime(time);

            tick.transform.SetAsFirstSibling();
        }
    }

    // Generate clip on timeline
    public void CreateClip(string name, int trackIndex, float startTime, float duration)
    {
        if (trackIndex >= trackCount) return;

        GameObject newClip = Instantiate(clipPrefab, contentParent);

        // Calculate position and size
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
    void Update() { if (musicSource == null || musicSource.clip == null) return; if (!isDraggingSlider && playheadSlider != null) playheadSlider.value = musicSource.time / totalDuration; if (timeDisplayText != null) timeDisplayText.text = $"{FormatTime(musicSource.time)} / {FormatTime(totalDuration)}"; }
    string FormatTime(float t) { int m = Mathf.FloorToInt(t / 60F); int s = Mathf.FloorToInt(t % 60F); return string.Format("{0:00}:{1:00}", m, s); }
    public void OnSliderDrag(float value) { }
}
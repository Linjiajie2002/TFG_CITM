using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SelectionUIManager : MonoBehaviour
{
    // ── Tab ───────────────────────────────────────────────────
    [Header("=== Tab 按钮 ===")]
    public Button tabCharacter;
    public Button tabScene;
    public Button tabMusic;

    [Header("=== 滑动动画 ===")]
    public RectTransform slidingIndicator;
    public float slideDuration = 0.2f;

    private Coroutine slideCoroutine;

    [Header("Tab 视觉")]
    public TextMeshProUGUI charTabText;
    public TextMeshProUGUI sceneTabText;
    public TextMeshProUGUI musicTabText;

    public Color tabActiveTextColor = new Color(0f, 0.8f, 0.7f, 1f);
    public Color tabInactiveTextColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    // ── 轮播区 ───────────────────────────────────────────────
    [Header("=== 轮播区 ===")]
    public Button btnLeft;
    public Button btnRight;
    public Image imageA;
    public Image imageB;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI indexLeftText;
    public TextMeshProUGUI indexRightText;

    [Header("轮播动画")]
    public float fadeDuration = 0.25f;

    // ── 选择按钮 ─────────────────────────────────────────────
    [Header("=== 选择按钮 ===")]
    public Button btnSelect;
    public TextMeshProUGUI selectBtnText;
    public Image selectBtnImage;

    public Color selectReadyColor = new Color(0f, 0.85f, 0.7f, 1f);
    public Color selectAllDoneColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    public Color selectDisabledColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    // ── 右下角槽 ─────────────────────────────────────────────
    [Header("=== 选择槽（右下角）===")]
    public SelectionSlotUI slotCharacter;
    public SelectionSlotUI slotScene;
    public SelectionSlotUI slotMusic;

    // ── 内部状态 ─────────────────────────────────────────────
    private enum TabType { Character, Scene, Music }
    private TabType currentTab = TabType.Character;
    private int currentIndex = 0;

    private bool isAnimating = false;
    private bool showingA = true;

    // ==========================================
    void Start()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.ClearCharacter();
            gm.ClearStage();
            gm.ClearMusic();
        }

        if (tabCharacter != null) tabCharacter.onClick.AddListener(() => SwitchTab(TabType.Character));
        if (tabScene != null) tabScene.onClick.AddListener(() => SwitchTab(TabType.Scene));
        if (tabMusic != null) tabMusic.onClick.AddListener(() => SwitchTab(TabType.Music));

        if (btnLeft != null) btnLeft.onClick.AddListener(() => Navigate(-1));
        if (btnRight != null) btnRight.onClick.AddListener(() => Navigate(1));

        if (btnSelect != null) btnSelect.onClick.AddListener(OnSelectClicked);

        if (slotCharacter != null) slotCharacter.SetEmpty();
        if (slotScene != null) slotScene.SetEmpty();
        if (slotMusic != null) slotMusic.SetEmpty();

        StartCoroutine(InitUI());
    }

    private IEnumerator InitUI()
    {
        yield return new WaitForEndOfFrame();
        SwitchTab(TabType.Character, true);
        RefreshStartButton();
    }

    private void SwitchTab(TabType tab, bool instant = false)
    {
        currentTab = tab;
        currentIndex = 0;

        bool isChar = (tab == TabType.Character);
        bool isScene = (tab == TabType.Scene);
        bool isMusic = (tab == TabType.Music);

        if (charTabText != null) charTabText.color = isChar ? tabActiveTextColor : tabInactiveTextColor;
        if (sceneTabText != null) sceneTabText.color = isScene ? tabActiveTextColor : tabInactiveTextColor;
        if (musicTabText != null) musicTabText.color = isMusic ? tabActiveTextColor : tabInactiveTextColor;

        RectTransform targetRect = null;
        if (isChar) targetRect = tabCharacter.GetComponent<RectTransform>();
        if (isScene) targetRect = tabScene.GetComponent<RectTransform>();
        if (isMusic) targetRect = tabMusic.GetComponent<RectTransform>();

        if (targetRect != null && slidingIndicator != null)
        {
            if (slideCoroutine != null)
            {
                StopCoroutine(slideCoroutine);
            }

            if (instant)
            {
                slidingIndicator.anchoredPosition = new Vector2(targetRect.anchoredPosition.x, slidingIndicator.anchoredPosition.y);
            }
            else
            {
                slideCoroutine = StartCoroutine(SlideToPosition(targetRect.anchoredPosition.x));
            }
        }

        RefreshCarouselImmediate();
    }

    private void Navigate(int dir)
    {
        if (isAnimating) return;
        int count = GetCurrentCount();
        if (count == 0) return;

        int nextIndex = (currentIndex + dir + count) % count;
        StartCoroutine(AnimateCarousel(nextIndex));
    }

    private IEnumerator AnimateCarousel(int nextIndex)
    {
        isAnimating = true;

        Image current = showingA ? imageA : imageB;
        Image next = showingA ? imageB : imageA;

        SetImageSprite(next, nextIndex);
        SetAlpha(next, 0f);

        currentIndex = nextIndex;
        RefreshNameAndIndex();

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            SetAlpha(current, 1f - t);
            SetAlpha(next, t);
            yield return null;
        }

        SetAlpha(current, 0f);
        SetAlpha(next, 1f);

        showingA = !showingA;
        isAnimating = false;
    }

    private void RefreshCarouselImmediate()
    {
        showingA = true;
        SetImageSprite(imageA, currentIndex);
        SetAlpha(imageA, 1f);
        SetAlpha(imageB, 0f);

        RefreshNameAndIndex();
    }

    private void RefreshNameAndIndex()
    {
        int count = GetCurrentCount();
        if (count == 0)
        {
            if (itemNameText != null) itemNameText.text = "—";
            if (indexLeftText != null) indexLeftText.text = "";
            if (indexRightText != null) indexRightText.text = "";
            return;
        }

        if (itemNameText != null)
            itemNameText.text = GetCurrentName(currentIndex);

        int leftIdx = (currentIndex - 1 + count) % count;
        int rightIdx = (currentIndex + 1) % count;

        if (indexLeftText != null) indexLeftText.text = $"{leftIdx + 1:D2}.";
        if (indexRightText != null) indexRightText.text = $"{rightIdx + 1:D2}.";
    }

    private void OnSelectClicked()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.AllSelected)
        {
            gm.LoadSelectedStage();
            return;
        }

        switch (currentTab)
        {
            case TabType.Character:
                gm.selectedCharIndex = currentIndex;
                if (slotCharacter != null)
                    slotCharacter.SetSelected(
                        gm.GetCharAvatar(currentIndex), // 👈 核心修改：这里使用 GetCharAvatar 代替 GetCharSprite
                        gm.GetCharName(currentIndex),
                        () => { gm.ClearCharacter(); slotCharacter.SetEmpty(); RefreshStartButton(); });
                break;

            case TabType.Scene:
                gm.selectedStageIndex = currentIndex;
                if (slotScene != null)
                    slotScene.SetSelected(
                        gm.GetStageSprite(currentIndex),
                        gm.GetStageName(currentIndex),
                        () => { gm.ClearStage(); slotScene.SetEmpty(); RefreshStartButton(); });
                break;

            case TabType.Music:
                gm.selectedMusicIndex = currentIndex;
                if (slotMusic != null)
                    slotMusic.SetSelected(
                        gm.GetMusicSprite(currentIndex),
                        gm.GetMusicName(currentIndex),
                        () => { gm.ClearMusic(); slotMusic.SetEmpty(); RefreshStartButton(); });
                break;
        }

        RefreshStartButton();
    }

    private void RefreshStartButton()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        bool allDone = gm.AllSelected;

        if (selectBtnImage != null)
            selectBtnImage.color = allDone ? selectAllDoneColor : selectReadyColor;

        if (selectBtnText != null)
            selectBtnText.text = allDone ? "START" : "SELECT";
    }

    private int GetCurrentCount()
    {
        var gm = GameManager.Instance;
        if (gm == null) return 0;
        return currentTab switch
        {
            TabType.Character => gm.GetCharCount(),
            TabType.Scene => gm.GetStageCount(),
            TabType.Music => gm.GetMusicCount(),
            _ => 0
        };
    }

    private string GetCurrentName(int i)
    {
        var gm = GameManager.Instance;
        if (gm == null) return "";
        return currentTab switch
        {
            TabType.Character => gm.GetCharName(i),
            TabType.Scene => gm.GetStageName(i),
            TabType.Music => gm.GetMusicName(i),
            _ => ""
        };
    }

    private Sprite GetCurrentSprite(int i)
    {
        var gm = GameManager.Instance;
        if (gm == null) return null;
        return currentTab switch
        {
            TabType.Character => gm.GetCharSprite(i),
            TabType.Scene => gm.GetStageSprite(i),
            TabType.Music => gm.GetMusicSprite(i),
            _ => null
        };
    }

    private void SetImageSprite(Image img, int index)
    {
        if (img == null) return;
        img.sprite = GetCurrentSprite(index);
        img.enabled = img.sprite != null;
    }

    private void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    private IEnumerator SlideToPosition(float targetX)
    {
        float time = 0;
        Vector2 startPos = slidingIndicator.anchoredPosition;
        Vector2 targetPos = new Vector2(targetX, startPos.y);

        while (time < slideDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, time / slideDuration);
            slidingIndicator.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        slidingIndicator.anchoredPosition = targetPos;
    }
}
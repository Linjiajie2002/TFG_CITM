using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Events;

// ==========================================
// 专业级颜色选择器 (主类放在最上面，防止 Unity 识别错误)
// ==========================================
public class ColorPickerPanel : MonoBehaviour,
    IPointerDownHandler, IDragHandler
{
    [Header("=== 折叠态 ===")]
    public Button swatchButton;           // 点击展开
    public Image swatchImage;            // 显示当前颜色

    [Header("=== 完整选色板 ===")]
    public GameObject pickerPanel;        // 整个展开面板

    [Header("=== SV 方形区域 ===")]
    public RawImage svSquare;             // 饱和度/明度
    public RectTransform svCursor;        // 小圆圈光标

    [Header("=== 色相条 ===")]
    public RawImage hueBar;
    public RectTransform hueCursor;

    [Header("=== 透明度条 ===")]
    public RawImage alphaBar;
    public RectTransform alphaCursor;

    [Header("=== 预览 ===")]
    public Image previewOld;
    public Image previewNew;

    [Header("=== RGBA 滑条 ===")]
    public Slider sliderR;
    public Slider sliderG;
    public Slider sliderB;
    public Slider sliderA;
    public TextMeshProUGUI labelR;
    public TextMeshProUGUI labelG;
    public TextMeshProUGUI labelB;
    public TextMeshProUGUI labelA;

    [Header("=== Hex 输入框 ===")]
    public TMP_InputField hexInput;

    [Header("=== 关闭按钮 ===")]
    public Button closeButton;

    // 对外事件
    [Header("=== 事件 ===")]
    public UnityEvent<Color> onColorChanged;

    // ── 内部状态 ──
    private float h = 0f, s = 1f, v = 1f, a = 1f;
    private bool isOpen = false;
    private Color oldColor = Color.white;
    private bool suppressCallbacks = false;

    // 纹理分辨率
    private const int SV_SIZE = 128;
    private const int HUE_SIZE = 128;
    private const int BAR_W = 128;
    private const int BAR_H = 16;

    private Texture2D svTex;
    private Texture2D hueTex;
    private Texture2D alphaTex;

    // 记录当前正在拖哪个区域
    private enum DragTarget { None, SV, Hue, Alpha }
    private DragTarget dragTarget = DragTarget.None;

    // 记录父级的 ScrollRect 原始状态
    private ScrollRect parentScroll;
    private bool originalVertical;
    private bool originalHorizontal;

    // ==========================================
    void Awake()
    {
        // 【动态添加事件转发器】
        AttachForwarder(svSquare);
        AttachForwarder(hueBar);
        AttachForwarder(alphaBar);

        BuildTextures();

        if (swatchButton != null) swatchButton.onClick.AddListener(TogglePicker);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePicker);

        RegisterSliderListeners();
        if (hexInput != null) hexInput.onEndEdit.AddListener(OnHexSubmit);

        SetColor(Color.white, notify: false);
        if (pickerPanel != null) pickerPanel.SetActive(false);
    }

    // 给分离到外部的 UI 挂载转发器
    private void AttachForwarder(RawImage targetImage)
    {
        if (targetImage != null)
        {
            targetImage.raycastTarget = true;
            var forwarder = targetImage.gameObject.AddComponent<ColorPickerEventForwarder>();
            forwarder.targetPanel = this;
        }
    }

    // ==========================================
    // 外部调用：设置颜色
    // ==========================================
    public void SetColor(Color c, bool notify = true)
    {
        suppressCallbacks = true;

        Color.RGBToHSV(c, out h, out s, out v);
        a = c.a;

        RefreshAllUI();

        if (notify)
        {
            onColorChanged?.Invoke(CurrentColor());
            if (previewOld != null) previewOld.color = CurrentColor();
        }
        else
        {
            oldColor = c;
            if (previewOld != null) previewOld.color = oldColor;
        }

        suppressCallbacks = false;
    }

    public Color GetColor() => CurrentColor();

    // ==========================================
    void TogglePicker()
    {
        isOpen = !isOpen;
        if (pickerPanel != null)
        {
            pickerPanel.SetActive(isOpen);
            if (isOpen) pickerPanel.transform.SetAsLastSibling();
        }

        ToggleParentScroll(!isOpen);

        if (isOpen)
        {
            oldColor = CurrentColor();
            if (previewOld != null) previewOld.color = oldColor;
        }
    }

    void ClosePicker()
    {
        isOpen = false;
        if (pickerPanel != null) pickerPanel.SetActive(false);
        ToggleParentScroll(true);
    }

    private void ToggleParentScroll(bool canScroll)
    {
        if (parentScroll == null)
        {
            parentScroll = GetComponentInParent<ScrollRect>();
            if (parentScroll != null)
            {
                originalVertical = parentScroll.vertical;
                originalHorizontal = parentScroll.horizontal;
            }
        }

        if (parentScroll != null)
        {
            if (canScroll)
            {
                parentScroll.vertical = originalVertical;
                parentScroll.horizontal = originalHorizontal;
            }
            else
            {
                parentScroll.vertical = false;
                parentScroll.horizontal = false;
            }
        }
    }

    // ==========================================
    // 鼠标/拖拽
    // ==========================================
    public void OnPointerDown(PointerEventData data)
    {
        dragTarget = GetDragTarget(data);
        HandleDrag(data);
    }

    public void OnDrag(PointerEventData data)
    {
        HandleDrag(data);
    }

    private DragTarget GetDragTarget(PointerEventData data)
    {
        if (svSquare != null && RectTransformUtility.RectangleContainsScreenPoint(
            svSquare.rectTransform, data.position, data.pressEventCamera)) return DragTarget.SV;
        if (hueBar != null && RectTransformUtility.RectangleContainsScreenPoint(
            hueBar.rectTransform, data.position, data.pressEventCamera)) return DragTarget.Hue;
        if (alphaBar != null && RectTransformUtility.RectangleContainsScreenPoint(
            alphaBar.rectTransform, data.position, data.pressEventCamera)) return DragTarget.Alpha;
        return DragTarget.None;
    }

    private void HandleDrag(PointerEventData data)
    {
        switch (dragTarget)
        {
            case DragTarget.SV: HandleSVDrag(data); break;
            case DragTarget.Hue: HandleHueDrag(data); break;
            case DragTarget.Alpha: HandleAlphaDrag(data); break;
        }
    }

    private void HandleSVDrag(PointerEventData data)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            svSquare.rectTransform, data.position, data.pressEventCamera, out Vector2 local)) return;

        Rect r = svSquare.rectTransform.rect;
        s = Mathf.Clamp01((local.x - r.xMin) / r.width);
        v = Mathf.Clamp01((local.y - r.yMin) / r.height);
        OnHSVChanged();
    }

    private void HandleHueDrag(PointerEventData data)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hueBar.rectTransform, data.position, data.pressEventCamera, out Vector2 local)) return;

        Rect r = hueBar.rectTransform.rect;
        h = Mathf.Clamp01((local.x - r.xMin) / r.width);
        RebuildSVTexture();
        OnHSVChanged();
    }

    private void HandleAlphaDrag(PointerEventData data)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            alphaBar.rectTransform, data.position, data.pressEventCamera, out Vector2 local)) return;

        Rect r = alphaBar.rectTransform.rect;
        a = Mathf.Clamp01((local.x - r.xMin) / r.width);
        OnHSVChanged();
    }

    // ==========================================
    // HSV/A 变化后统一更新所有 UI
    // ==========================================
    private void OnHSVChanged()
    {
        suppressCallbacks = true;
        RefreshAllUI();
        suppressCallbacks = false;

        Color c = CurrentColor();
        if (swatchImage != null) swatchImage.color = c;
        if (previewNew != null) previewNew.color = c;
        onColorChanged?.Invoke(c);
    }

    private void RefreshAllUI()
    {
        Color c = CurrentColor();

        if (swatchImage != null) swatchImage.color = c;
        if (previewNew != null) previewNew.color = c;

        if (svCursor != null && svSquare != null)
        {
            Rect r = svSquare.rectTransform.rect;
            svCursor.anchoredPosition = new Vector2(
                r.xMin + s * r.width,
                r.yMin + v * r.height);
        }

        if (hueCursor != null && hueBar != null)
        {
            Rect r = hueBar.rectTransform.rect;
            hueCursor.anchoredPosition = new Vector2(r.xMin + h * r.width, 0f);
        }

        if (alphaCursor != null && alphaBar != null)
        {
            Rect r = alphaBar.rectTransform.rect;
            alphaCursor.anchoredPosition = new Vector2(r.xMin + a * r.width, 0f);
        }
        RebuildAlphaTexture();

        SetSliderSilent(sliderR, c.r);
        SetSliderSilent(sliderG, c.g);
        SetSliderSilent(sliderB, c.b);
        SetSliderSilent(sliderA, a);

        if (labelR != null) labelR.text = Mathf.RoundToInt(c.r * 255).ToString();
        if (labelG != null) labelG.text = Mathf.RoundToInt(c.g * 255).ToString();
        if (labelB != null) labelB.text = Mathf.RoundToInt(c.b * 255).ToString();
        if (labelA != null) labelA.text = Mathf.RoundToInt(a * 255).ToString();

        if (hexInput != null && !hexInput.isFocused)
            hexInput.SetTextWithoutNotify(ColorUtility.ToHtmlStringRGBA(c));
    }

    // ==========================================
    // RGBA 滑条监听
    // ==========================================
    private void RegisterSliderListeners()
    {
        if (sliderR != null) sliderR.onValueChanged.AddListener(v2 => { if (!suppressCallbacks) { Color.RGBToHSV(new Color(v2, sliderG?.value ?? 0f, sliderB?.value ?? 0f), out h, out s, out v); RebuildSVTexture(); OnHSVChanged(); } });
        if (sliderG != null) sliderG.onValueChanged.AddListener(v2 => { if (!suppressCallbacks) { Color.RGBToHSV(new Color(sliderR?.value ?? 0f, v2, sliderB?.value ?? 0f), out h, out s, out v); RebuildSVTexture(); OnHSVChanged(); } });
        if (sliderB != null) sliderB.onValueChanged.AddListener(v2 => { if (!suppressCallbacks) { Color.RGBToHSV(new Color(sliderR?.value ?? 0f, sliderG?.value ?? 0f, v2), out h, out s, out v); RebuildSVTexture(); OnHSVChanged(); } });
        if (sliderA != null) sliderA.onValueChanged.AddListener(v2 => { if (!suppressCallbacks) { a = v2; OnHSVChanged(); } });
    }

    private void OnHexSubmit(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color c))
        {
            Color.RGBToHSV(c, out h, out s, out v);
            a = c.a;
            RebuildSVTexture();
            OnHSVChanged();
        }
    }

    private void SetSliderSilent(Slider sl, float val)
    {
        if (sl == null) return;
        sl.SetValueWithoutNotify(Mathf.Clamp01(val));
    }

    // ==========================================
    // 纹理生成
    // ==========================================
    private void BuildTextures()
    {
        svTex = new Texture2D(SV_SIZE, SV_SIZE, TextureFormat.RGB24, false);
        svTex.filterMode = FilterMode.Bilinear;
        RebuildSVTexture();
        if (svSquare != null) svSquare.texture = svTex;

        hueTex = new Texture2D(HUE_SIZE, 1, TextureFormat.RGB24, false);
        hueTex.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < HUE_SIZE; x++)
            hueTex.SetPixel(x, 0, Color.HSVToRGB((float)x / HUE_SIZE, 1f, 1f));
        hueTex.Apply();
        if (hueBar != null) hueBar.texture = hueTex;

        alphaTex = new Texture2D(BAR_W, 1, TextureFormat.RGBA32, false);
        alphaTex.filterMode = FilterMode.Bilinear;
        RebuildAlphaTexture();
        if (alphaBar != null) alphaBar.texture = alphaTex;
    }

    private void RebuildSVTexture()
    {
        if (svTex == null) return;
        Color baseHue = Color.HSVToRGB(h, 1f, 1f);
        for (int x = 0; x < SV_SIZE; x++)
        {
            float sat = (float)x / (SV_SIZE - 1);
            for (int y = 0; y < SV_SIZE; y++)
            {
                float val = (float)y / (SV_SIZE - 1);
                Color col = Color.HSVToRGB(h, sat, val);
                svTex.SetPixel(x, y, col);
            }
        }
        svTex.Apply();
    }

    private void RebuildAlphaTexture()
    {
        if (alphaTex == null) return;
        Color opaque = Color.HSVToRGB(h, s, v);
        for (int x = 0; x < BAR_W; x++)
        {
            float t = (float)x / (BAR_W - 1);
            alphaTex.SetPixel(x, 0, new Color(opaque.r, opaque.g, opaque.b, t));
        }
        alphaTex.Apply();
        if (alphaBar != null) alphaBar.texture = alphaTex;
    }

    private Color CurrentColor()
    {
        Color c = Color.HSVToRGB(h, s, v);
        c.a = a;
        return c;
    }
}

// ==========================================
// 跨层级事件转发器 (辅助类放到下面)
// ==========================================
public class ColorPickerEventForwarder : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public ColorPickerPanel targetPanel;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetPanel != null) targetPanel.OnPointerDown(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (targetPanel != null) targetPanel.OnDrag(eventData);
    }
}